// File    : MasterDataRepository.cs
// Module  : MasterData
// Layer   : Infrastructure
// Purpose : Dapper implementation của IMasterDataRepository — CRUD generic, metadata-driven.
//           Đọc bảng đích từ Ui_Form → Sys_Table (Config DB), thực thi CRUD trên Data DB.

using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Domain.Exceptions;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// CRUD dữ liệu danh mục theo cấu hình metadata (Ui_Form/Ui_Field/Sys_Table/Sys_Column).
/// </summary>
/// <remarks>
/// An toàn injection: mọi identifier (schema, table, column) validate qua
/// <see cref="SafeIdentifierRegex"/>; mọi giá trị truyền qua Dapper params.
/// Bảng vật lý = <c>[Schema_Name].[Table_Code]</c> (Table_Code là tên bảng thật, theo convention WPF).
/// </remarks>
public sealed partial class MasterDataRepository : IMasterDataRepository
{
    private readonly IDbConnectionFactory     _configDb;
    private readonly IDataDbConnectionFactory _dataDb;

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex SafeIdentifierRegex();

    public MasterDataRepository(IDbConnectionFactory configDb, IDataDbConnectionFactory dataDb)
    {
        _configDb = configDb;
        _dataDb   = dataDb;
    }

    // ── Form info (bảng đích + cột) ─────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<MasterDataFormInfo?> GetFormInfoAsync(
        string formCode, int tenantId, CancellationToken ct = default)
    {
        using var cfg = _configDb.CreateConnection();

        // Form + bảng đích (cô lập tenant ở tầng connection — ADR-035)
        const string formSql = """
            SELECT fm.Form_Id      AS FormId,
                   fm.Form_Code    AS FormCode,
                   fm.Table_Id     AS TableId,
                   fm.Display_Mode AS DisplayMode,
                   t.Schema_Name   AS SchemaName,
                   t.Table_Code    AS TableName
            FROM   dbo.Ui_Form  fm
            JOIN   dbo.Sys_Table t ON t.Table_Id = fm.Table_Id
            WHERE  fm.Form_Code = @FormCode
              AND  fm.Is_Active = 1
            """;

        var head = await cfg.QueryFirstOrDefaultAsync<FormHeadRow>(
            new CommandDefinition(formSql, new { FormCode = formCode, TenantId = tenantId },
                cancellationToken: ct));
        if (head is null) return null;

        // PK của bảng đích — ưu tiên Sys_Column.Is_PK; nếu rỗng (metadata chưa set hoặc
        // PK không đăng ký field) → đọc PK VẬT LÝ từ Data DB (INFORMATION_SCHEMA).
        const string pkSql = """
            SELECT TOP 1 Column_Code
            FROM   dbo.Sys_Column
            WHERE  Table_Id = @TableId AND Is_PK = 1
            ORDER  BY Column_Id
            """;
        var pkColumn = await cfg.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(pkSql, new { head.TableId }, cancellationToken: ct));

        if (string.IsNullOrWhiteSpace(pkColumn))
        {
            using var data = _dataDb.CreateConnection();
            pkColumn = await GetPhysicalPkAsync(data, head.SchemaName, head.TableName, ct);
        }

        // Không có PK ở cả metadata (Sys_Column.Is_PK) lẫn vật lý (INFORMATION_SCHEMA) → form
        // không thể CRUD (mọi thao tác cần khóa định danh dòng). Ném lỗi cấu hình CÓ MÃ thay vì để
        // SafeCol("") văng "ký tự không hợp lệ" mơ hồ → client hiển thị thông báo i18n rõ ràng.
        if (string.IsNullOrWhiteSpace(pkColumn))
            throw new MetadataConfigurationException(
                MetadataConfigurationException.NoPrimaryKey, formCode,
                $"Bảng dữ liệu '{head.SchemaName}.{head.TableName}' của form '{formCode}' " +
                "chưa có khóa chính (PRIMARY KEY).");

        // Danh sách field của form (join Sys_Column lấy tên cột DB + net type).
        // Label: resolve Label_Key qua Sys_Resource (vi) → COALESCE về key nếu chưa có bản dịch.
        const string colSql = """
            SELECT sc.Column_Code AS ColumnCode,
                   sc.Net_Type    AS NetType,
                   uf.Editor_Type AS EditorType,
                   COALESCE(r.Resource_Value, uf.Label_Key) AS Label,
                   uf.Show_In_List AS ShowInList,
                   uf.Is_ReadOnly  AS IsReadOnly,
                   uf.Is_Unique    AS IsUnique,
                   uf.Order_No     AS OrderNo
            FROM   dbo.Ui_Field uf
            JOIN   dbo.Sys_Column sc ON sc.Column_Id = uf.Column_Id
            LEFT   JOIN dbo.Sys_Resource r
                   ON r.Resource_Key = uf.Label_Key AND r.Lang_Code = 'vi'
            WHERE  uf.Form_Id = @FormId
              AND  uf.Is_Visible = 1
              AND  uf.Is_Virtual = 0   -- virtual field UI-only, không map cột DB → loại khỏi lưới/list/save
            ORDER  BY uf.Order_No
            """;
        var cols = (await cfg.QueryAsync<MasterDataColumn>(
            new CommandDefinition(colSql, new { head.FormId }, cancellationToken: ct))).ToList();

        return new MasterDataFormInfo
        {
            FormId      = head.FormId,
            FormCode    = head.FormCode,
            TableId     = head.TableId,
            SchemaName  = head.SchemaName,
            TableName   = head.TableName,
            PkColumn    = pkColumn,
            DisplayMode = head.DisplayMode,
            Columns     = cols
        };
    }

    // ── Exists (unique check) ─────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<bool> ExistsValueAsync(
        MasterDataFormInfo info, string column, object? value, object? excludeId,
        CancellationToken ct = default)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.ToString())) return false;

        // info do caller (handler) truyền vào — KHÔNG GetFormInfoAsync mỗi field unique.
        var table = QualifiedTable(info);
        var col   = SafeCol(column, "column");
        var pk    = SafeCol(info.PkColumn, "PK");

        var dp = new DynamicParameters();
        dp.Add("Val", value);
        var sql = $"SELECT COUNT(*) FROM {table} WHERE {Bracket(col)} = @Val";
        if (excludeId is not null)
        {
            sql += $" AND {Bracket(pk)} <> @ExcludeId";
            dp.Add("ExcludeId", excludeId);
        }

        using var data = _dataDb.CreateConnection();
        var count = await data.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, dp, cancellationToken: ct));
        return count > 0;
    }

    // ── List ────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<MasterDataListResult> GetListAsync(
        string formCode, int tenantId,
        string? search = null, bool? activeOnly = null,
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var info = await GetFormInfoAsync(formCode, tenantId, ct)
                   ?? throw new InvalidOperationException($"MasterData: form '{formCode}' không tồn tại.");
        var table = QualifiedTable(info);
        var pk    = SafeCol(info.PkColumn, "PK");

        // Cột SELECT cho lưới: PK + các cột Show_In_List.
        // Fallback: nếu CHƯA field nào bật Show_In_List → lấy toàn bộ field column
        // (khớp với fallback hiển thị bên MasterDataListPage để không lệch cột ↔ dữ liệu).
        var shown = info.Columns.Where(c => c.ShowInList).ToList();
        if (shown.Count == 0) shown = info.Columns.ToList();
        var listCols = shown.Select(c => c.ColumnCode)
                            .Where(c => SafeIdentifierRegex().IsMatch(c))
                            .ToList();
        var selectSet = new List<string> { pk };
        selectSet.AddRange(listCols.Where(c => !c.Equals(pk, StringComparison.OrdinalIgnoreCase)));
        var selectCols = string.Join(", ", selectSet.Select(Bracket));

        // WHERE: search (LIKE trên các cột text Show_In_List) + active filter
        var where = new List<string>();
        var dp    = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var textCols = shown
                .Where(c => c.NetType.Equals("string", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.ColumnCode)
                .Where(c => SafeIdentifierRegex().IsMatch(c))
                .ToList();
            if (textCols.Count > 0)
            {
                where.Add("(" + string.Join(" OR ", textCols.Select(c => $"{Bracket(c)} LIKE @Search")) + ")");
                dp.Add("Search", $"%{search.Trim()}%");
            }
        }

        if (activeOnly == true && HasColumn(info, "Is_Active"))
            where.Add("[Is_Active] = 1");

        var whereSql = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";

        var skip = Math.Max(0, (page - 1) * pageSize);
        dp.Add("Skip", skip);
        dp.Add("Take", pageSize);

        var listSql =
            $"SELECT {selectCols} FROM {table}{whereSql} " +
            $"ORDER BY {Bracket(pk)} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
        var countSql = $"SELECT COUNT(*) FROM {table}{whereSql}";

        using var data = _dataDb.CreateConnection();
        var rows = await data.QueryAsync(new CommandDefinition(listSql, dp, cancellationToken: ct));
        var total = await data.ExecuteScalarAsync<int>(new CommandDefinition(countSql, dp, cancellationToken: ct));

        return new MasterDataListResult
        {
            Items = rows.Select(r => (IDictionary<string, object?>)
                            ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => (object?)v.Value))
                        .ToList(),
            TotalCount = total
        };
    }

    // ── Get by id ───────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IDictionary<string, object?>?> GetByIdAsync(
        string formCode, int tenantId, object id, CancellationToken ct = default)
    {
        var info = await GetFormInfoAsync(formCode, tenantId, ct)
                   ?? throw new InvalidOperationException($"MasterData: form '{formCode}' không tồn tại.");
        var table = QualifiedTable(info);
        var pk    = SafeCol(info.PkColumn, "PK");

        var sql = $"SELECT * FROM {table} WHERE {Bracket(pk)} = @Id";
        using var data = _dataDb.CreateConnection();
        var row = await data.QueryFirstOrDefaultAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        if (row is null) return null;

        return ((IDictionary<string, object>)row).ToDictionary(k => k.Key, v => (object?)v.Value);
    }

    // ── Derived value (virtual cascade field) ────────────────────────────────────

    /// <inheritdoc />
    public async Task<object?> ResolveDerivedValueAsync(
        string sourceName, string selectColumn, string whereColumn, object? whereValue,
        CancellationToken ct = default)
    {
        if (whereValue is null) return null;
        if (!SafeIdentifierRegex().IsMatch(sourceName) ||
            !SafeIdentifierRegex().IsMatch(selectColumn) ||
            !SafeIdentifierRegex().IsMatch(whereColumn))
            return null;

        var sql = $"SELECT {Bracket(selectColumn)} FROM {sourceName} WHERE {Bracket(whereColumn)} = @Val";
        using var data = _dataDb.CreateConnection();
        return await data.ExecuteScalarAsync<object?>(
            new CommandDefinition(sql, new { Val = whereValue }, cancellationToken: ct));
    }

    // ── Insert ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<object?> InsertAsync(
        string formCode, int tenantId, Dictionary<string, object?> values,
        long? userId = null, CancellationToken ct = default)
    {
        var info = await GetFormInfoAsync(formCode, tenantId, ct)
                   ?? throw new InvalidOperationException($"MasterData: form '{formCode}' không tồn tại.");
        using var data = _dataDb.CreateConnection();
        return await InsertCoreAsync(data, tx: null, info, values, userId, ct);
    }

    /// <summary>
    /// INSERT lõi — dùng connection (+transaction nếu có) truyền vào, để chia sẻ với
    /// <see cref="SaveWithHooksAsync"/> (ghi trong CÙNG transaction với hook store).
    /// </summary>
    private async Task<object?> InsertCoreAsync(
        IDbConnection data, IDbTransaction? tx,
        MasterDataFormInfo info, Dictionary<string, object?> values, long? userId, CancellationToken ct)
    {
        var table = QualifiedTable(info);
        var pk    = SafeCol(info.PkColumn, "PK");

        var (cols, dp) = BuildColumnParams(info, values, excludeCol: pk);
        if (cols.Count == 0)
            throw new InvalidOperationException("MasterData Insert: không có cột hợp lệ để thêm.");

        var audit = await GetAuditColumnsAsync(data, info.SchemaName, info.TableName, ct, tx);

        // (cột, biểu thức giá trị) — field = @param; audit = bơm tự động (CreatedBy/At nếu bảng có).
        var insCols = cols.Select(Bracket).ToList();
        var insVals = cols.Select(c => "@" + c).ToList();
        if (audit.Contains("CreatedBy") && userId is not null)
        {
            insCols.Add(Bracket("CreatedBy")); insVals.Add("@__CreatedBy"); dp.Add("__CreatedBy", userId.Value);
        }
        if (audit.Contains("CreatedAt")) { insCols.Add(Bracket("CreatedAt")); insVals.Add("SYSUTCDATETIME()"); }

        var sql = $"INSERT INTO {table} ({string.Join(", ", insCols)}) " +
                  $"OUTPUT INSERTED.{Bracket(pk)} AS NewId VALUES ({string.Join(", ", insVals)})";

        return await data.ExecuteScalarAsync<object>(
            new CommandDefinition(sql, dp, transaction: tx, cancellationToken: ct));
    }

    // ── Update ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<int> UpdateAsync(
        string formCode, int tenantId, object id, Dictionary<string, object?> values,
        long? userId = null, CancellationToken ct = default)
    {
        var info = await GetFormInfoAsync(formCode, tenantId, ct)
                   ?? throw new InvalidOperationException($"MasterData: form '{formCode}' không tồn tại.");
        using var data = _dataDb.CreateConnection();
        return await UpdateCoreAsync(data, tx: null, info, id, values, userId, ct);
    }

    /// <summary>
    /// UPDATE lõi — dùng connection (+transaction nếu có) truyền vào (chia sẻ với <see cref="SaveWithHooksAsync"/>).
    /// </summary>
    private async Task<int> UpdateCoreAsync(
        IDbConnection data, IDbTransaction? tx,
        MasterDataFormInfo info, object id, Dictionary<string, object?> values, long? userId, CancellationToken ct)
    {
        var table = QualifiedTable(info);
        var pk    = SafeCol(info.PkColumn, "PK");

        var (cols, dp) = BuildColumnParams(info, values, excludeCol: pk);
        if (cols.Count == 0)
            throw new InvalidOperationException("MasterData Update: không có cột hợp lệ để cập nhật.");

        var audit = await GetAuditColumnsAsync(data, info.SchemaName, info.TableName, ct, tx);

        var setParts = cols.Select(c => $"{Bracket(c)} = @{c}").ToList();
        if (audit.Contains("UpdatedBy") && userId is not null)
        {
            setParts.Add($"{Bracket("UpdatedBy")} = @__UpdatedBy"); dp.Add("__UpdatedBy", userId.Value);
        }
        if (audit.Contains("UpdatedAt")) { setParts.Add($"{Bracket("UpdatedAt")} = SYSUTCDATETIME()"); }

        dp.Add("__Id", id);
        var sql = $"UPDATE {table} SET {string.Join(", ", setParts)} WHERE {Bracket(pk)} = @__Id";

        return await data.ExecuteAsync(new CommandDefinition(sql, dp, transaction: tx, cancellationToken: ct));
    }

    // ── Save qua hook store (validate trước → ghi → after-save, 1 transaction) ─────

    /// <inheritdoc />
    public async Task<MasterDataHookSaveResult> SaveWithHooksAsync(
        MasterDataFormInfo info, int tenantId, object? id,
        Dictionary<string, object?> values, long? userId, string langCode,
        bool hasValidateProc, bool hasAfterSaveProc,
        CancellationToken ct = default,
        string source = "MANUAL", Guid? importSessionId = null)
    {
        // info do caller (handler) truyền vào — KHÔNG GetFormInfoAsync lần 2 trong save path.
        var tableName = SafeCol(info.TableName, "Table");   // dùng dựng tên store, validate identifier

        using var data = _dataDb.CreateConnection();
        if (data.State != ConnectionState.Open) data.Open();   // BeginTransaction cần connection mở
        using var tx = data.BeginTransaction();

        // 1) Validate store (opt-in qua catalog — KHÔNG query OBJECT_ID lúc lưu).
        //    Có lỗi → KHÔNG commit → rollback khi dispose, KHÔNG ghi.
        //    KHÔNG truyền ngữ cảnh import cho spc_ (contract validate giữ nguyên).
        if (hasValidateProc)
        {
            var errors = await RunHookProcAsync(data, tx, $"spc_Grid_{tableName}",
                id, tenantId, userId, langCode, values, ct);
            if (errors.Count > 0)
                return new MasterDataHookSaveResult { Success = false, Id = null, Errors = errors };
        }

        // 2) Ghi DB trong CÙNG transaction.
        object? resultId;
        if (id is null)
            resultId = await InsertCoreAsync(data, tx, info, values, userId, ct);
        else
        {
            await UpdateCoreAsync(data, tx, info, id, values, userId, ct);
            resultId = id;
        }

        // 3) After-save store (opt-in). Trả lỗi / RAISERROR → rollback cả bản ghi vừa ghi.
        if (hasAfterSaveProc)
        {
            // Chỉ nhánh after-save nhận ngữ cảnh import (@Source/@ImportSessionId) — và CHỈ khi import
            // (importSessionId != null) mới thêm vào EXEC ⇒ save tay giữ contract cũ, proc cũ không vỡ.
            var afterErrors = await RunHookProcAsync(data, tx, $"sp_AfterSave_Grid_{tableName}",
                resultId, tenantId, userId, langCode, values, ct, source, importSessionId);
            if (afterErrors.Count > 0)
                return new MasterDataHookSaveResult { Success = false, Id = null, Errors = afterErrors };
        }

        tx.Commit();
        return new MasterDataHookSaveResult { Success = true, Id = resultId, Errors = [] };
    }

    /// <summary>
    /// Gọi 1 hook store (EXEC) trong transaction, đọc result set lỗi nếu có.
    /// Tham số: field động → @PayloadJson; context cố định → param rời. Trả rỗng = không lỗi.
    /// </summary>
    private static async Task<List<ProcError>> RunHookProcAsync(
        IDbConnection data, IDbTransaction tx, string procName,
        object? id, int tenantId, long? userId, string langCode,
        Dictionary<string, object?> values, CancellationToken ct,
        string? source = null, Guid? importSessionId = null)
    {
        var dp = new DynamicParameters();
        dp.Add("Id", id is null ? 0L : id);          // null (Insert) → 0 theo quy ước ID=0 là thêm mới
        dp.Add("TenantId", tenantId);
        dp.Add("NguoiDungID", userId);               // null → SQL NULL (token chuẩn — xem spec 19)
        dp.Add("LangCode", string.IsNullOrWhiteSpace(langCode) ? "vi" : langCode);
        dp.Add("PayloadJson", JsonSerializer.Serialize(values));

        var sql = $"EXEC dbo.{Bracket(procName)} " +
                  "@Id=@Id, @TenantId=@TenantId, @NguoiDungID=@NguoiDungID, " +
                  "@LangCode=@LangCode, @PayloadJson=@PayloadJson";

        // Ngữ cảnh import (spec 25 §12.1): CHỈ thêm khi import → after-save proc v2 (@Source/@ImportSessionId
        // có DEFAULT). Save tay (importSessionId=null) giữ EXEC cũ ⇒ proc chưa nâng cấp không bị "too many arguments".
        if (importSessionId is not null)
        {
            dp.Add("Source", string.IsNullOrWhiteSpace(source) ? "IMPORT" : source);
            dp.Add("ImportSessionId", importSessionId);
            sql += ", @Source=@Source, @ImportSessionId=@ImportSessionId";
        }

        var rows = await data.QueryAsync(new CommandDefinition(sql, dp, transaction: tx, cancellationToken: ct));

        var errors = new List<ProcError>();
        foreach (var row in rows)
        {
            if (row is not IDictionary<string, object> d) continue;
            var key = RowStr(d, "error_key");
            if (string.IsNullOrWhiteSpace(key)) continue;   // dòng rỗng / không đúng contract → bỏ
            errors.Add(new ProcError(
                key!,
                RowStr(d, "args_json"),
                RowStr(d, "field_name"),
                RowStr(d, "severity") ?? "error"));
        }
        return errors;
    }

    /// <summary>Đọc 1 ô của Dapper row theo tên cột (case-insensitive), null khi thiếu / DBNull.</summary>
    private static string? RowStr(IDictionary<string, object> row, string col)
        => row.TryGetValue(col, out var v) && v is not null and not DBNull ? v.ToString() : null;

    /// <summary>
    /// Cột audit (CreatedBy/CreatedAt/UpdatedBy/UpdatedAt) THỰC SỰ có trên bảng đích — để engine
    /// chỉ bơm cột tồn tại (bảng cũ không có cột audit thì bỏ qua, không vỡ).
    /// </summary>
    private static async Task<HashSet<string>> GetAuditColumnsAsync(
        IDbConnection data, string schema, string table, CancellationToken ct, IDbTransaction? tx = null)
    {
        const string sql = """
            SELECT COLUMN_NAME
            FROM   INFORMATION_SCHEMA.COLUMNS
            WHERE  TABLE_SCHEMA = @Schema AND TABLE_NAME = @Table
              AND  COLUMN_NAME IN ('CreatedBy','CreatedAt','UpdatedBy','UpdatedAt')
            """;
        var rows = await data.QueryAsync<string>(
            new CommandDefinition(sql, new { Schema = schema, Table = table }, transaction: tx, cancellationToken: ct));
        return rows.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // ── Delete (hard) ───────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<int> DeleteAsync(
        string formCode, int tenantId, object id, CancellationToken ct = default)
    {
        var info = await GetFormInfoAsync(formCode, tenantId, ct)
                   ?? throw new InvalidOperationException($"MasterData: form '{formCode}' không tồn tại.");
        var table = QualifiedTable(info);
        var pk    = SafeCol(info.PkColumn, "PK");

        var sql = $"DELETE FROM {table} WHERE {Bracket(pk)} = @Id";
        using var data = _dataDb.CreateConnection();
        return await data.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    /// <summary>Lọc values theo các cột field của form (chống cột lạ) + build Dapper params.</summary>
    private static (List<string> Cols, DynamicParameters Dp) BuildColumnParams(
        MasterDataFormInfo info, Dictionary<string, object?> values, string excludeCol)
    {
        // Tập cột cho phép ghi = field của form, không readonly, không phải PK
        var allowed = info.Columns
            .Where(c => !c.IsReadOnly)
            .Select(c => c.ColumnCode)
            .Where(c => SafeIdentifierRegex().IsMatch(c))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var cols = new List<string>();
        var dp   = new DynamicParameters();
        foreach (var (key, val) in values)
        {
            if (!SafeIdentifierRegex().IsMatch(key)) continue;
            if (key.Equals(excludeCol, StringComparison.OrdinalIgnoreCase)) continue;
            if (!allowed.Contains(key)) continue;
            cols.Add(key);
            dp.Add(key, val);
        }
        return (cols, dp);
    }

    /// <summary>
    /// Đọc tên cột PK vật lý của bảng từ Data DB qua INFORMATION_SCHEMA.
    /// Dùng khi Sys_Column.Is_PK chưa được set (metadata không đáng tin).
    /// </summary>
    internal static async Task<string?> GetPhysicalPkAsync(
        System.Data.IDbConnection data, string schema, string table, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1 ku.COLUMN_NAME
            FROM   INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
            JOIN   INFORMATION_SCHEMA.KEY_COLUMN_USAGE  ku ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
            WHERE  tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
              AND  tc.TABLE_SCHEMA = @Schema AND tc.TABLE_NAME = @Table
            ORDER  BY ku.ORDINAL_POSITION
            """;
        return await data.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(sql, new { Schema = schema, Table = table }, cancellationToken: ct));
    }

    private static string QualifiedTable(MasterDataFormInfo info)
    {
        var schema = SafeCol(info.SchemaName, "Schema");
        var table  = SafeCol(info.TableName,  "Table");
        return $"{Bracket(schema)}.{Bracket(table)}";
    }

    private static bool HasColumn(MasterDataFormInfo info, string col) =>
        info.Columns.Any(c => c.ColumnCode.Equals(col, StringComparison.OrdinalIgnoreCase));

    private static string SafeCol(string name, string label)
    {
        var v = (name ?? "").Trim();
        if (!SafeIdentifierRegex().IsMatch(v))
            throw new InvalidOperationException($"MasterData: {label} '{name}' chứa ký tự không hợp lệ.");
        return v;
    }

    private static string Bracket(string identifier) => $"[{identifier}]";

    /// <summary>Dapper mapping cho header form + bảng đích.</summary>
    private sealed class FormHeadRow
    {
        public int    FormId      { get; init; }
        public string FormCode    { get; init; } = "";
        public int    TableId     { get; init; }
        public string DisplayMode { get; init; } = "Popup";
        public string SchemaName  { get; init; } = "dbo";
        public string TableName   { get; init; } = "";
    }
}
