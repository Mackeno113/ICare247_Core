// File    : MasterDataRepository.cs
// Module  : MasterData
// Layer   : Infrastructure
// Purpose : Dapper implementation của IMasterDataRepository — CRUD generic, metadata-driven.
//           Đọc bảng đích từ Ui_Form → Sys_Table (Config DB), thực thi CRUD trên Data DB.

using System.Text.RegularExpressions;
using Dapper;
using ICare247.Application.Interfaces;

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

        // Form + bảng đích (verify tenant qua Sys_Table.Tenant_Id)
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
              AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
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
        pkColumn ??= "";

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
        string formCode, int tenantId, string column, object? value, object? excludeId,
        CancellationToken ct = default)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.ToString())) return false;

        var info = await GetFormInfoAsync(formCode, tenantId, ct)
                   ?? throw new InvalidOperationException($"MasterData: form '{formCode}' không tồn tại.");
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

    // ── Insert ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<object?> InsertAsync(
        string formCode, int tenantId, Dictionary<string, object?> values, CancellationToken ct = default)
    {
        var info = await GetFormInfoAsync(formCode, tenantId, ct)
                   ?? throw new InvalidOperationException($"MasterData: form '{formCode}' không tồn tại.");
        var table = QualifiedTable(info);
        var pk    = SafeCol(info.PkColumn, "PK");

        var (cols, dp) = BuildColumnParams(info, values, excludeCol: pk);
        if (cols.Count == 0)
            throw new InvalidOperationException("MasterData Insert: không có cột hợp lệ để thêm.");

        var colList   = string.Join(", ", cols.Select(Bracket));
        var paramList = string.Join(", ", cols.Select(c => "@" + c));
        var sql = $"INSERT INTO {table} ({colList}) OUTPUT INSERTED.{Bracket(pk)} AS NewId VALUES ({paramList})";

        using var data = _dataDb.CreateConnection();
        return await data.ExecuteScalarAsync<object>(new CommandDefinition(sql, dp, cancellationToken: ct));
    }

    // ── Update ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<int> UpdateAsync(
        string formCode, int tenantId, object id, Dictionary<string, object?> values, CancellationToken ct = default)
    {
        var info = await GetFormInfoAsync(formCode, tenantId, ct)
                   ?? throw new InvalidOperationException($"MasterData: form '{formCode}' không tồn tại.");
        var table = QualifiedTable(info);
        var pk    = SafeCol(info.PkColumn, "PK");

        var (cols, dp) = BuildColumnParams(info, values, excludeCol: pk);
        if (cols.Count == 0)
            throw new InvalidOperationException("MasterData Update: không có cột hợp lệ để cập nhật.");

        dp.Add("__Id", id);
        var setList = string.Join(", ", cols.Select(c => $"{Bracket(c)} = @{c}"));
        var sql = $"UPDATE {table} SET {setList} WHERE {Bracket(pk)} = @__Id";

        using var data = _dataDb.CreateConnection();
        return await data.ExecuteAsync(new CommandDefinition(sql, dp, cancellationToken: ct));
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
