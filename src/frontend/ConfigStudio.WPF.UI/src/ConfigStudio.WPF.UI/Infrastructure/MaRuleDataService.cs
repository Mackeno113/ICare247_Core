// File    : MaRuleDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : CRUD Sys_Ma_Rule + Sys_Ma_Rule_Segment trên Config DB bằng Dapper (MA-6, ADR-036).
//           Ghi quy tắc + đoạn trong 1 transaction (đoạn = xóa-ghi lại theo cha).

using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>Data service Dapper cho màn Quy tắc sinh mã.</summary>
public sealed class MaRuleDataService : IMaRuleDataService
{
    private static readonly HashSet<string> SegmentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "LITERAL", "DATE", "FIELD", "LOOKUP", "SEQ" };

    private readonly IAppConfigService _config;

    public MaRuleDataService(IAppConfigService config) => _config = config;

    /// <inheritdoc />
    public async Task<IReadOnlyList<MaRuleRecord>> GetRulesAsync(CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT Rule_Id       AS RuleId,
                   Table_Code    AS TableCode,
                   Column_Code   AS ColumnCode,
                   Step          AS Step,
                   Allow_Manual  AS AllowManual,
                   Is_Active     AS IsActive,
                   Description   AS Description,
                   Is_System     AS IsSystem,
                   Is_Customized AS IsCustomized,
                   Synced_At     AS SyncedAt,
                   Source_Ver    AS SourceVer
            FROM dbo.Sys_Ma_Rule
            ORDER BY Table_Code, Column_Code
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var rows = await conn.QueryAsync<MaRuleRecord>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MaRuleSegmentRecord>> GetSegmentsAsync(
        int ruleId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || ruleId <= 0) return [];

        const string sql = """
            SELECT Segment_Id      AS SegmentId,
                   Rule_Id         AS RuleId,
                   Order_No        AS OrderNo,
                   Segment_Type    AS SegmentType,
                   Text_Value      AS TextValue,
                   Field_Code      AS FieldCode,
                   Lookup_Table    AS LookupTable,
                   Lookup_Key_Col  AS LookupKeyCol,
                   Lookup_Val_Col  AS LookupValCol,
                   Substring_Start AS SubstringStart,
                   Length          AS Length,
                   Pad_Char        AS PadChar,
                   Pad_Side        AS PadSide,
                   Text_Transform  AS TextTransform
            FROM dbo.Sys_Ma_Rule_Segment
            WHERE Rule_Id = @RuleId AND Is_Active = 1
            ORDER BY Order_No
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var rows = await conn.QueryAsync<MaRuleSegmentRecord>(
            new CommandDefinition(sql, new { RuleId = ruleId }, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MaRuleSegmentRecord>> GetAllSegmentsAsync(CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT Segment_Id      AS SegmentId,
                   Rule_Id         AS RuleId,
                   Order_No        AS OrderNo,
                   Segment_Type    AS SegmentType,
                   Text_Value      AS TextValue,
                   Field_Code      AS FieldCode,
                   Lookup_Table    AS LookupTable,
                   Lookup_Key_Col  AS LookupKeyCol,
                   Lookup_Val_Col  AS LookupValCol,
                   Substring_Start AS SubstringStart,
                   Length          AS Length,
                   Pad_Char        AS PadChar,
                   Pad_Side        AS PadSide,
                   Text_Transform  AS TextTransform
            FROM dbo.Sys_Ma_Rule_Segment
            WHERE Is_Active = 1
            ORDER BY Rule_Id, Order_No
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var rows = await conn.QueryAsync<MaRuleSegmentRecord>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<int> SaveRuleAsync(MaRuleUpsertRequest request, CancellationToken ct = default)
    {
        EnsureConfigured();
        Validate(request);

        var tableCode = request.TableCode.Trim();
        var columnCode = request.ColumnCode.Trim();

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            // Chống trùng cặp (bảng, cột) — unique index đã chặn ở DB, báo lỗi thân thiện sớm.
            const string dupSql = """
                SELECT TOP (1) 1 FROM dbo.Sys_Ma_Rule
                WHERE Table_Code = @TableCode AND Column_Code = @ColumnCode
                  AND Rule_Id <> @RuleId
                """;
            var dup = await conn.ExecuteScalarAsync<int?>(new CommandDefinition(
                dupSql, new { TableCode = tableCode, ColumnCode = columnCode, RuleId = request.RuleId ?? 0 },
                transaction: tx, cancellationToken: ct));
            if (dup.HasValue)
                throw new InvalidOperationException(
                    $"Đã có quy tắc cho {tableCode}.{columnCode}. Mỗi cặp bảng+cột chỉ 1 quy tắc.");

            int ruleId;
            var ruleParams = new
            {
                request.RuleId,
                TableCode = tableCode,
                ColumnCode = columnCode,
                request.Step,
                request.AllowManual,
                request.IsActive,
                Description = NullIfEmpty(request.Description),
            };

            if (request.RuleId is null or 0)
            {
                const string insertSql = """
                    INSERT INTO dbo.Sys_Ma_Rule
                        (Table_Code, Column_Code, Step, Allow_Manual, Is_Active, Description, Is_System, Is_Customized)
                    OUTPUT INSERTED.Rule_Id
                    VALUES (@TableCode, @ColumnCode, @Step, @AllowManual, @IsActive, @Description, 0, 1)
                    """;
                ruleId = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(insertSql, ruleParams, transaction: tx, cancellationToken: ct));
            }
            else
            {
                const string updateSql = """
                    UPDATE dbo.Sys_Ma_Rule
                    SET Table_Code   = @TableCode,
                        Column_Code  = @ColumnCode,
                        Step         = @Step,
                        Allow_Manual = @AllowManual,
                        Is_Active    = @IsActive,
                        Description  = @Description,
                        Is_Customized = 1
                    WHERE Rule_Id = @RuleId
                    """;
                var affected = await conn.ExecuteAsync(
                    new CommandDefinition(updateSql, ruleParams, transaction: tx, cancellationToken: ct));
                if (affected == 0)
                    throw new InvalidOperationException("Không tìm thấy quy tắc để cập nhật.");
                ruleId = request.RuleId.Value;

                // Đoạn = xóa-ghi lại theo cha (Segment_Id không ổn định qua các lần lưu, không sao).
                await conn.ExecuteAsync(new CommandDefinition(
                    "DELETE FROM dbo.Sys_Ma_Rule_Segment WHERE Rule_Id = @RuleId",
                    new { RuleId = ruleId }, transaction: tx, cancellationToken: ct));
            }

            const string segInsert = """
                INSERT INTO dbo.Sys_Ma_Rule_Segment
                    (Rule_Id, Order_No, Segment_Type, Text_Value, Field_Code,
                     Lookup_Table, Lookup_Key_Col, Lookup_Val_Col,
                     Substring_Start, Length, Pad_Char, Pad_Side, Text_Transform)
                VALUES
                    (@RuleId, @OrderNo, @SegmentType, @TextValue, @FieldCode,
                     @LookupTable, @LookupKeyCol, @LookupValCol,
                     @SubstringStart, @Length, @PadChar, @PadSide, @TextTransform)
                """;

            var order = 0;
            foreach (var s in request.Segments)
            {
                order++;
                await conn.ExecuteAsync(new CommandDefinition(segInsert, new
                {
                    RuleId = ruleId,
                    OrderNo = order,
                    SegmentType = s.SegmentType.Trim().ToUpperInvariant(),
                    TextValue = NullIfEmpty(s.TextValue),
                    FieldCode = NullIfEmpty(s.FieldCode),
                    LookupTable = NullIfEmpty(s.LookupTable),
                    LookupKeyCol = NullIfEmpty(s.LookupKeyCol),
                    LookupValCol = NullIfEmpty(s.LookupValCol),
                    s.SubstringStart,
                    Length = s.Length is > 0 ? s.Length : null,
                    PadChar = NullIfEmpty(s.PadChar),
                    PadSide = NullIfEmpty(s.PadSide),
                    TextTransform = NullIfEmpty(s.TextTransform),
                }, transaction: tx, cancellationToken: ct));
            }

            await tx.CommitAsync(ct);
            return ruleId;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteRuleAsync(int ruleId, CancellationToken ct = default)
    {
        EnsureConfigured();

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            // Chỉ xóa quy tắc tenant tự thêm (Is_System = 0); bản hệ thống giữ lại.
            var affected = await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM dbo.Sys_Ma_Rule WHERE Rule_Id = @RuleId AND Is_System = 0",
                new { RuleId = ruleId }, transaction: tx, cancellationToken: ct));
            if (affected == 0)
                throw new InvalidOperationException("Không thể xóa: quy tắc hệ thống hoặc không tồn tại.");

            await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM dbo.Sys_Ma_Rule_Segment WHERE Rule_Id = @RuleId",
                new { RuleId = ruleId }, transaction: tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TableLookupRecord>> GetTablesAsync(CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT Table_Id AS TableId, Table_Code AS TableCode,
                   ISNULL(Table_Name, '') AS TableName, ISNULL(Schema_Name, 'dbo') AS SchemaName
            FROM   dbo.Sys_Table
            WHERE  Is_Active = 1
            ORDER BY Table_Code
            """;
        await using var conn = new SqlConnection(_config.ConnectionString);
        var rows = await conn.QueryAsync<TableLookupRecord>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetColumnsAsync(string tableCode, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || string.IsNullOrWhiteSpace(tableCode)) return [];

        const string sql = """
            SELECT c.Column_Code
            FROM   dbo.Sys_Column c
            JOIN   dbo.Sys_Table  t ON t.Table_Id = c.Table_Id
            WHERE  t.Table_Code = @TableCode AND c.Is_Active = 1
            ORDER BY c.Column_Code
            """;
        await using var conn = new SqlConnection(_config.ConnectionString);
        var rows = await conn.QueryAsync<string>(
            new CommandDefinition(sql, new { TableCode = tableCode.Trim() }, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<int> CountOverlappingEventsAsync(
        string tableCode, string columnCode, CancellationToken ct = default)
    {
        if (!_config.IsConfigured
            || string.IsNullOrWhiteSpace(tableCode) || string.IsNullOrWhiteSpace(columnCode))
            return 0;

        // Best-effort: đếm event SET_VALUE/CLEAR_VALUE trên form của bảng này mà Action_Param_Json
        // có targetField = cột đang cấu hình. LIKE tolerant khoảng trắng JSON. Schema Evt_* thiếu → 0.
        const string sql = """
            SELECT COUNT(1)
            FROM   dbo.Evt_Action     a
            JOIN   dbo.Evt_Definition d ON d.Event_Id = a.Event_Id
            JOIN   dbo.Ui_Form        f ON f.Form_Id  = d.Form_Id
            JOIN   dbo.Sys_Table      t ON t.Table_Id = f.Table_Id
            WHERE  t.Table_Code = @TableCode
              AND  a.Action_Code IN ('SET_VALUE', 'CLEAR_VALUE')
              AND  a.Action_Param_Json LIKE @Needle
            """;
        var needle = "%\"targetField%" + columnCode.Trim() + "%";
        try
        {
            await using var conn = new SqlConnection(_config.ConnectionString);
            return await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, new { TableCode = tableCode.Trim(), Needle = needle },
                    cancellationToken: ct));
        }
        catch
        {
            return 0;   // Evt_* chưa có / khác schema → coi như không chồng, không chặn người dùng
        }
    }

    private void EnsureConfigured()
    {
        if (!_config.IsConfigured)
            throw new InvalidOperationException("DB chưa được cấu hình. Vui lòng vào Settings.");
    }

    /// <summary>Kiểm 2 ràng buộc §3.3 (đúng 1 SEQ; mọi đoạn trước SEQ có độ dài xác định) + dữ liệu đoạn.</summary>
    private static void Validate(MaRuleUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableCode))
            throw new InvalidOperationException("Phải chọn bảng đích.");
        if (string.IsNullOrWhiteSpace(request.ColumnCode))
            throw new InvalidOperationException("Phải chọn cột mã.");
        if (request.Step < 1)
            throw new InvalidOperationException("Bước nhảy phải ≥ 1.");
        if (request.Segments.Count == 0)
            throw new InvalidOperationException("Quy tắc phải có ít nhất một đoạn.");

        var seqCount = 0;
        var seqSeen = false;
        var idx = 0;
        foreach (var s in request.Segments)
        {
            idx++;
            var type = s.SegmentType?.Trim().ToUpperInvariant() ?? "";
            if (!SegmentTypes.Contains(type))
                throw new InvalidOperationException($"Đoạn {idx}: loại '{s.SegmentType}' không hợp lệ.");

            switch (type)
            {
                case "LITERAL":
                    if (string.IsNullOrEmpty(s.TextValue))
                        throw new InvalidOperationException($"Đoạn {idx} (LITERAL): thiếu chữ cố định.");
                    break;
                case "DATE":
                    if (string.IsNullOrWhiteSpace(s.TextValue))
                        throw new InvalidOperationException($"Đoạn {idx} (DATE): thiếu định dạng ngày.");
                    break;
                case "FIELD":
                    if (string.IsNullOrWhiteSpace(s.FieldCode))
                        throw new InvalidOperationException($"Đoạn {idx} (FIELD): thiếu field nguồn.");
                    break;
                case "LOOKUP":
                    if (string.IsNullOrWhiteSpace(s.FieldCode) || string.IsNullOrWhiteSpace(s.LookupTable)
                        || string.IsNullOrWhiteSpace(s.LookupKeyCol) || string.IsNullOrWhiteSpace(s.LookupValCol))
                        throw new InvalidOperationException(
                            $"Đoạn {idx} (LOOKUP): cần đủ field nguồn, bảng tra, cột khóa và cột lấy giá trị.");
                    break;
                case "SEQ":
                    seqCount++;
                    seqSeen = true;
                    break;
            }

            // Ràng buộc §3.3.2: mọi đoạn ĐỨNG TRƯỚC SEQ phải có độ dài xác định.
            if (!seqSeen && type is "DATE" or "FIELD" or "LOOKUP"
                && !(s.Length is > 0) && !HasFixedDate(type, s.TextValue))
                throw new InvalidOperationException(
                    $"Đoạn {idx} ({type}) đứng trước SEQ nên phải đặt 'Độ rộng' > 0 (để cắt được phần số khi dò mã lớn nhất).");
        }

        if (seqCount != 1)
            throw new InvalidOperationException(
                seqCount == 0
                    ? "Quy tắc phải có đúng 1 đoạn SEQ (số thứ tự)."
                    : "Chỉ được có đúng 1 đoạn SEQ.");
    }

    /// <summary>DATE có độ dài cố định theo định dạng (yyyy=4, yy=2, MM=2, dd=2, yyyyMM=6…) → coi như đủ rộng.</summary>
    private static bool HasFixedDate(string type, string? format)
        => type == "DATE" && !string.IsNullOrWhiteSpace(format);

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
