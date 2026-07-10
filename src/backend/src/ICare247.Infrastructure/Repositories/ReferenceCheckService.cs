// File    : ReferenceCheckService.cs
// Module  : MasterData
// Layer   : Infrastructure
// Purpose : Soft-check tham chiếu khóa ngoại trước khi xóa cứng.
//           Quan hệ CHỈ từ 2 nguồn KHAI BÁO TƯỜNG MINH — KHÔNG suy luận theo tên cột:
//             (1) registry Sys_Relation (Detail_FK_Column) — cấu hình qua ConfigStudio;
//             (2) FK vật lý khai trong Data DB (sys.foreign_keys).
//           Bảng không khai ở nguồn nào ⇒ không có tham chiếu ⇒ cho xóa.
//           Nhánh "dò theo quy ước tên PK" ĐÃ GỠ 2026-07-10 (chặn nhầm — xem CheckUsageAsync).

using System.Text.RegularExpressions;
using Dapper;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Kiểm tra tham chiếu "mềm" trước khi xóa cứng 1 bản ghi danh mục.
/// </summary>
/// <remarks>
/// Thứ tự: (1) tra <c>Sys_Relation</c> theo Master_Table_Id → mỗi quan hệ cho cột FK ở bảng con
/// (chính xác, không đoán). (2) Nếu bảng chưa khai quan hệ nào → fallback quy ước tên PK
/// (<c>CongTyID</c> → cột <c>CongTyID</c> hoặc <c>%_CongTyID</c>). Mỗi candidate query trong
/// try/catch để cột/bảng đã drop vật lý không làm hỏng cả thao tác.
/// </remarks>
public sealed partial class ReferenceCheckService : IReferenceCheckService
{
    private readonly IDbConnectionFactory     _configDb;
    private readonly IDataDbConnectionFactory _dataDb;
    private readonly ILogger<ReferenceCheckService> _logger;

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex SafeIdentifierRegex();

    public ReferenceCheckService(
        IDbConnectionFactory configDb,
        IDataDbConnectionFactory dataDb,
        ILogger<ReferenceCheckService> logger)
    {
        _configDb = configDb;
        _dataDb   = dataDb;
        _logger   = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Quan hệ chỉ đến từ HAI nguồn KHAI BÁO TƯỜNG MINH — <b>KHÔNG suy luận theo tên cột</b>:
    /// <list type="number">
    ///   <item>Registry <c>Sys_Relation</c> (cấu hình qua ConfigStudio).</item>
    ///   <item>FK vật lý khai trong Data DB (<c>sys.foreign_keys</c>).</item>
    /// </list>
    /// Bảng không khai ở nguồn nào ⇒ coi như KHÔNG có tham chiếu ⇒ cho xóa.
    /// <para>
    /// Nhánh "dò theo quy ước tên PK" đã bị GỠ (2026-07-10). ADR-019 đổi PK thành <c>Id</c> cho
    /// mọi bảng, nên <c>Column_Code = 'Id' OR LIKE '%_Id'</c> khớp gần như MỌI cột trong Data DB
    /// → xóa 1 ngân hàng <c>Id=5</c> bị chặn vì bảng khác tình cờ có dòng <c>Id=5</c> hoặc
    /// <c>QuocGia_Id=5</c>. Chặn nhầm, im lặng. Chính ADR-019 đã dặn "KHÔNG đoán theo tên".
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<ReferenceUsage>> CheckUsageAsync(
        int catalogTableId, object? pkValue, CancellationToken ct = default)
    {
        if (pkValue is null) return [];

        using var cfg  = _configDb.CreateConnection();
        using var data = _dataDb.CreateConnection();

        // ── ① Sys_Relation (khai qua ConfigStudio) ─────────────────────────────
        var candidates = await GetCandidatesFromRelationsAsync(cfg, catalogTableId, ct);

        // ── ② FK vật lý khai trong Data DB ─────────────────────────────────────
        var master = await GetMasterTableAsync(cfg, catalogTableId, ct);
        if (master is not null)
        {
            var physical = await GetCandidatesFromPhysicalFksAsync(
                data, master.SchemaName, master.TableName, ct);

            // Hợp nhất, khử trùng theo (schema, bảng, cột) — 1 quan hệ có thể khai ở cả hai nơi.
            var seen = new HashSet<string>(
                candidates.Select(c => $"{c.SchemaName}.{c.TableName}.{c.ColumnName}"),
                StringComparer.OrdinalIgnoreCase);
            foreach (var p in physical)
                if (seen.Add($"{p.SchemaName}.{p.TableName}.{p.ColumnName}"))
                    candidates.Add(p);
        }

        if (candidates.Count == 0)
        {
            _logger.LogInformation(
                "ReferenceCheck: Table_Id={TableId} chưa khai quan hệ nào (Sys_Relation lẫn FK vật lý) — cho phép xóa.",
                catalogTableId);
            return [];
        }

        // ── ③ Đếm usage từng candidate trên Data DB ─────────────────────────────
        var usages = await CountUsagesAsync(data, candidates, pkValue, ct);

        if (usages.Count > 0)
        {
            var totalRows = usages.Sum(u => u.RowCount);
            _logger.LogWarning(
                "ReferenceCheck: Table_Id={TableId} value={Value} bị khóa bởi {Places} nơi, tổng {Rows} dòng.",
                catalogTableId, pkValue, usages.Count, totalRows);
        }

        return usages;
    }

    /// <summary>Schema + tên bảng master từ <c>Sys_Table</c> — để tra FK vật lý trên Data DB.</summary>
    private static async Task<CandidateRow?> GetMasterTableAsync(
        System.Data.IDbConnection cfg, int catalogTableId, CancellationToken ct)
    {
        const string sql =
            "SELECT Schema_Name AS SchemaName, Table_Code AS TableName FROM dbo.Sys_Table WHERE Table_Id = @TableId";
        return await cfg.QueryFirstOrDefaultAsync<CandidateRow>(
            new CommandDefinition(sql, new { TableId = catalogTableId }, cancellationToken: ct));
    }

    /// <summary>
    /// Candidate từ FK VẬT LÝ khai trong Data DB: mọi cột con trỏ tới bảng master.
    /// Nguồn sự thật thứ hai bên cạnh <c>Sys_Relation</c> — bảng nào đã có FK thật thì không
    /// cần khai lại ở registry.
    /// <para>Sự kiện theo sau: caller gộp với candidate của Sys_Relation rồi đếm usage.</para>
    /// </summary>
    private async Task<List<CandidateRow>> GetCandidatesFromPhysicalFksAsync(
        System.Data.IDbConnection data, string schema, string table, CancellationToken ct)
    {
        const string sql = """
            SELECT ps.name AS SchemaName,
                   pt.name AS TableName,
                   pc.name AS ColumnName,
                   0       AS IsLegacy
            FROM   sys.foreign_keys fk
            JOIN   sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            JOIN   sys.tables  pt ON pt.object_id = fkc.parent_object_id
            JOIN   sys.schemas ps ON ps.schema_id = pt.schema_id
            JOIN   sys.columns pc ON pc.object_id = fkc.parent_object_id
                                 AND pc.column_id = fkc.parent_column_id
            JOIN   sys.tables  rt ON rt.object_id = fkc.referenced_object_id
            JOIN   sys.schemas rs ON rs.schema_id = rt.schema_id
            WHERE  rs.name = @Schema AND rt.name = @Table
            """;
        try
        {
            var rows = await data.QueryAsync<CandidateRow>(
                new CommandDefinition(sql, new { Schema = schema, Table = table }, cancellationToken: ct));
            return rows.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ReferenceCheck: không đọc được FK vật lý của {Schema}.{Table} — chỉ dùng Sys_Relation.",
                schema, table);
            return [];
        }
    }

    /// <summary>
    /// Lấy candidate tham chiếu từ <c>Sys_Relation</c> — mỗi quan hệ active có Detail_FK_Column
    /// cho ra (schema + bảng con + cột FK). Đây là đường chính xác (không đoán theo tên).
    /// </summary>
    /// <param name="cfg">Kết nối Config DB.</param>
    /// <param name="catalogTableId">Table_Id của bảng đang xóa (vai trò master).</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>Danh sách candidate; rỗng nếu chưa khai quan hệ hoặc schema chưa migrate.</returns>
    /// <remarks>
    /// Bọc try/catch: nếu cột Detail_FK_Column chưa tồn tại (migration 035 chưa chạy trên DB này)
    /// → trả rỗng để caller fallback sang quy ước tên, không làm hỏng thao tác xóa.
    /// </remarks>
    private async Task<List<CandidateRow>> GetCandidatesFromRelationsAsync(
        System.Data.IDbConnection cfg, int catalogTableId, CancellationToken ct)
    {
        const string sql = """
            SELECT dt.Schema_Name  AS SchemaName,
                   dt.Table_Code   AS TableName,
                   r.Detail_FK_Column AS ColumnName,
                   CASE WHEN dt.Is_Active = 0 THEN 1 ELSE 0 END AS IsLegacy
            FROM   dbo.Sys_Relation r
            JOIN   dbo.Sys_Table   dt ON dt.Table_Id = r.Detail_Table_Id
            WHERE  r.Master_Table_Id = @TableId
              AND  r.Is_Active = 1
              AND  r.Detail_FK_Column IS NOT NULL
            """;
        try
        {
            var rows = await cfg.QueryAsync<CandidateRow>(
                new CommandDefinition(sql, new { TableId = catalogTableId }, cancellationToken: ct));
            return rows.ToList();
        }
        catch (Exception ex)
        {
            // Sys_Relation chưa mở rộng (migration 035) hoặc lỗi khác → vẫn còn nguồn FK vật lý.
            _logger.LogWarning(ex,
                "ReferenceCheck: không đọc được Sys_Relation cho Table_Id={TableId} — chỉ dùng FK vật lý.",
                catalogTableId);
            return [];
        }
    }

    /// <summary>
    /// Đếm số dòng tham chiếu của từng candidate trên Data DB.
    /// </summary>
    /// <param name="data">Kết nối Data DB.</param>
    /// <param name="candidates">Danh sách (schema, bảng, cột) cần đếm.</param>
    /// <param name="pkValue">Giá trị khóa của bản ghi đang xóa.</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>Danh sách nơi đang dùng (RowCount &gt; 0).</returns>
    /// <remarks>Mỗi candidate query trong try/catch — cột/bảng có thể đã drop vật lý dù metadata còn.</remarks>
    private async Task<List<ReferenceUsage>> CountUsagesAsync(
        System.Data.IDbConnection data, List<CandidateRow> candidates, object pkValue, CancellationToken ct)
    {
        var usages = new List<ReferenceUsage>();

        foreach (var c in candidates)
        {
            if (!SafeIdentifierRegex().IsMatch(c.SchemaName)
                || !SafeIdentifierRegex().IsMatch(c.TableName)
                || !SafeIdentifierRegex().IsMatch(c.ColumnName))
                continue;

            var sql = $"SELECT COUNT(*) FROM [{c.SchemaName}].[{c.TableName}] WHERE [{c.ColumnName}] = @V";
            try
            {
                var count = await data.ExecuteScalarAsync<int>(
                    new CommandDefinition(sql, new { V = pkValue }, cancellationToken: ct));
                if (count > 0)
                {
                    usages.Add(new ReferenceUsage(
                        c.SchemaName, c.TableName, c.ColumnName, count, c.IsLegacy == 1));
                    _logger.LogInformation(
                        "ReferenceCheck BLOCK: {Schema}.{Table}.{Column} = {Value} → {Count} dòng{Legacy}.",
                        c.SchemaName, c.TableName, c.ColumnName, pkValue, count,
                        c.IsLegacy == 1 ? " [dữ liệu cũ]" : "");
                }
            }
            catch (Exception ex)
            {
                // Cột/bảng có thể đã bị drop vật lý dù metadata còn → log + bỏ qua, không nổ
                _logger.LogWarning(ex,
                    "ReferenceCheck: bỏ qua {Schema}.{Table}.{Column} (truy vấn lỗi — có thể đã drop).",
                    c.SchemaName, c.TableName, c.ColumnName);
            }
        }

        return usages;
    }

    private sealed class CandidateRow
    {
        public string SchemaName { get; init; } = "dbo";
        public string TableName  { get; init; } = "";
        public string ColumnName { get; init; } = "";
        public int    IsLegacy   { get; init; }
    }
}
