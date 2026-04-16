// File    : DynamicLookupRepository.cs
// Module  : Lookup
// Layer   : Infrastructure
// Purpose : Dapper implementation của IDynamicLookupRepository.
//           Đọc cấu hình Ui_Field_Lookup rồi build + execute parameterized SQL an toàn.

using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Thực thi dynamic lookup query theo cấu hình trong <c>Ui_Field_Lookup</c>.
/// </summary>
/// <remarks>
/// SQL builder tuân thủ các quy tắc an toàn:
/// <list type="bullet">
///   <item>SourceName, column names: chỉ [a-zA-Z0-9_.] — ngăn SQL injection qua identifier.</item>
///   <item>FilterSql / OrderBy từ Config DB (admin trust) nhưng vẫn bị block nếu có DDL/DML keyword.</item>
///   <item>ContextValues từ frontend luôn truyền qua Dapper params — không concat vào SQL.</item>
/// </list>
/// </remarks>
public sealed partial class DynamicLookupRepository : IDynamicLookupRepository
{
    private readonly IDbConnectionFactory     _configDb;
    private readonly IDataDbConnectionFactory _dataDb;

    // Regex kiểm tra tên identifier an toàn: a-z, A-Z, 0-9, _, dấu chấm (schema.table)
    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_.]*$", RegexOptions.Compiled)]
    private static partial Regex SafeIdentifierRegex();

    // Các keyword DDL/DML nguy hiểm — không cho phép trong FilterSql / OrderBy
    private static readonly string[] DangerousKeywords =
        ["DROP", "DELETE", "INSERT", "UPDATE", "EXEC", "EXECUTE", "TRUNCATE", "ALTER", "CREATE", "MERGE", "--", ";"];

    public DynamicLookupRepository(IDbConnectionFactory configDb, IDataDbConnectionFactory dataDb)
    {
        _configDb = configDb;
        _dataDb   = dataDb;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IDictionary<string, object>>> QueryAsync(
        int fieldId,
        int tenantId,
        Dictionary<string, object?> contextValues,
        CancellationToken ct = default)
    {
        // ── Bước 1: Đọc cấu hình Ui_Field_Lookup theo FieldId ─────────────────
        // Verify tenant qua Ui_Form → Sys_Table.Tenant_Id để ngăn cross-tenant query
        // Bao gồm cột Migration 014: CodeField dùng để mở rộng SELECT khi EditBoxMode = CodeAndName
        const string cfgSql = """
            SELECT fl.Query_Mode                           AS QueryMode,
                   fl.Source_Name                         AS SourceName,
                   fl.Value_Column                         AS ValueColumn,
                   fl.Display_Column                       AS DisplayColumn,
                   fl.Filter_Sql                           AS FilterSql,
                   fl.Order_By                             AS OrderBy,
                   fl.Popup_Columns_Json                   AS PopupColumnsJson,
                   fl.Code_Field                           AS CodeField
            FROM   dbo.Ui_Field_Lookup fl
            JOIN   dbo.Ui_Field        fi ON fi.Field_Id = fl.Field_Id
            JOIN   dbo.Ui_Form         fm ON fm.Form_Id  = fi.Form_Id
            JOIN   dbo.Sys_Table       t  ON t.Table_Id  = fm.Table_Id
            WHERE  fl.Field_Id = @FieldId
              AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
            """;

        // Bước 1 đọc cấu hình từ Config DB
        using var configConn = _configDb.CreateConnection();

        var cfg = await configConn.QueryFirstOrDefaultAsync<LookupCfgRow>(
            new CommandDefinition(cfgSql, new { FieldId = fieldId, TenantId = tenantId },
                cancellationToken: ct));

        // Không có cấu hình, hoặc cấu hình chưa hoàn chỉnh (SourceName rỗng) → trả rỗng
        if (cfg is null || string.IsNullOrWhiteSpace(cfg.SourceName))
            return [];

        // ── Bước 2: Validate identifiers để ngăn SQL injection ────────────────
        var querySql = BuildSafeSql(cfg, out var error);
        if (querySql is null)
            throw new InvalidOperationException(
                $"DynamicLookup FieldId={fieldId}: cấu hình không hợp lệ — {error}");

        // ── Bước 3: Build Dapper params: @TenantId + ContextValues ────────────
        var dp = new DynamicParameters();
        dp.Add("TenantId", tenantId);
        foreach (var (key, val) in contextValues)
        {
            // Chỉ thêm param nếu tên an toàn — ngăn override @TenantId từ client
            if (SafeIdentifierRegex().IsMatch(key) && !key.Equals("TenantId", StringComparison.OrdinalIgnoreCase))
                dp.Add(key, val);
        }

        // ── Bước 4: Execute query trên Data DB (DB nghiệp vụ — DM_PhongBan, v.v.) ──
        using var dataConn = _dataDb.CreateConnection();
        var rows = await dataConn.QueryAsync(
            new CommandDefinition(querySql, dp, cancellationToken: ct));

        // Dapper trả ExpandoObject khi không chỉ định type — ExpandoObject implements IDictionary<string, object>
        return rows
            .Select(r => (IDictionary<string, object>)r)
            .ToList()
            .AsReadOnly();
    }

    // ── SQL Builder ──────────────────────────────────────────────────────────

    private static string? BuildSafeSql(LookupCfgRow cfg, out string? error)
    {
        error = null;

        var mode = (cfg.QueryMode ?? "table").ToLower();

        if (mode == "custom_sql")
        {
            // SQL tùy chỉnh từ admin — chỉ block DDL/DML keyword
            if (ContainsDangerousKeyword(cfg.SourceName))
            {
                error = "custom_sql chứa keyword DDL/DML không được phép.";
                return null;
            }
            return cfg.SourceName;
        }

        // table / tvf: validate tên nguồn
        if (!SafeIdentifierRegex().IsMatch(cfg.SourceName ?? ""))
        {
            error = $"SourceName '{cfg.SourceName}' chứa ký tự không hợp lệ.";
            return null;
        }

        // Validate column names
        if (!IsValidColumnList(cfg.ValueColumn, out var colErr))
        {
            error = $"ValueColumn: {colErr}";
            return null;
        }
        if (!IsValidColumnList(cfg.DisplayColumn, out colErr))
        {
            error = $"DisplayColumn: {colErr}";
            return null;
        }

        // Validate FilterSql (nếu có)
        if (!string.IsNullOrWhiteSpace(cfg.FilterSql) && ContainsDangerousKeyword(cfg.FilterSql))
        {
            error = "FilterSql chứa keyword DDL/DML không được phép.";
            return null;
        }

        // Validate OrderBy (nếu có)
        if (!string.IsNullOrWhiteSpace(cfg.OrderBy) && ContainsDangerousKeyword(cfg.OrderBy))
        {
            error = "OrderBy chứa keyword DDL/DML không được phép.";
            return null;
        }

        // Build SELECT — gồm ValueColumn, DisplayColumn, CodeField (nếu có), và các cột từ PopupColumnsJson
        var selectCols = BuildSelectColumns(cfg);
        var sql = $"SELECT {selectCols} FROM {cfg.SourceName}";

        // WHERE — FilterSql thường có @TenantId nên luôn thêm (nếu không có FilterSql, thêm WHERE 1=1)
        if (!string.IsNullOrWhiteSpace(cfg.FilterSql))
            sql += $" WHERE {cfg.FilterSql}";

        // ORDER BY
        if (!string.IsNullOrWhiteSpace(cfg.OrderBy))
            sql += $" ORDER BY {cfg.OrderBy}";

        return sql;
    }

    /// <summary>
    /// Xây dựng danh sách cột SELECT đầy đủ — bao gồm ValueColumn, DisplayColumn,
    /// CodeField (nếu set), và tất cả cột trong PopupColumnsJson.
    /// Loại bỏ trùng lặp, bỏ qua cột không hợp lệ.
    /// </summary>
    private static string BuildSelectColumns(LookupCfgRow cfg)
    {
        // Dùng LinkedHashSet để giữ thứ tự + không trùng
        var cols = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCol(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var col = name.Trim();
            if (SafeIdentifierRegex().IsMatch(col) && seen.Add(col))
                cols.Add(col);
        }

        // Luôn có ValueColumn và DisplayColumn
        AddCol(cfg.ValueColumn);
        AddCol(cfg.DisplayColumn);

        // CodeField — dùng khi EditBoxMode = CodeAndName
        AddCol(cfg.CodeField);

        // Popup columns từ JSON: [{"fieldName":"MaPhongBan","caption":"Mã",...}, ...]
        if (!string.IsNullOrWhiteSpace(cfg.PopupColumnsJson))
        {
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var popupCols = JsonSerializer.Deserialize<List<PopupColEntry>>(cfg.PopupColumnsJson, opts);
                foreach (var pc in popupCols ?? [])
                    AddCol(pc.Column);
            }
            catch
            {
                // JSON không hợp lệ → bỏ qua, dùng ValueColumn + DisplayColumn là đủ
            }
        }

        return string.Join(", ", cols);
    }

    private static bool IsValidColumnList(string? cols, out string? err)
    {
        err = null;
        if (string.IsNullOrWhiteSpace(cols))
        {
            err = "Tên cột rỗng.";
            return false;
        }
        // Hỗ trợ nhiều cột cách nhau dấu phẩy: "MaPhong, TenPhong"
        foreach (var part in cols.Split(','))
        {
            var col = part.Trim();
            if (!SafeIdentifierRegex().IsMatch(col))
            {
                err = $"Tên cột '{col}' chứa ký tự không hợp lệ.";
                return false;
            }
        }
        return true;
    }

    private static bool ContainsDangerousKeyword(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return false;
        var upper = sql.ToUpperInvariant();
        return DangerousKeywords.Any(kw => upper.Contains(kw));
    }

    // ── Internal DTOs ─────────────────────────────────────────────────────────

    /// <summary>Dapper mapping cho config row từ Ui_Field_Lookup.</summary>
    private sealed class LookupCfgRow
    {
        public string  QueryMode        { get; init; } = "table";
        public string  SourceName       { get; init; } = "";
        public string  ValueColumn      { get; init; } = "";
        public string  DisplayColumn    { get; init; } = "";
        public string? FilterSql        { get; init; }
        public string? OrderBy          { get; init; }
        public string? PopupColumnsJson { get; init; }
        /// <summary>Cột code — dùng khi EditBoxMode = CodeAndName (Migration 014).</summary>
        public string? CodeField        { get; init; }
    }

    /// <summary>
    /// Một entry trong PopupColumnsJson array — chỉ cần FieldName để build SELECT.
    /// JSON từ WPF ConfigStudio dùng key "fieldName" + "captionKey" (i18n).
    /// MetadataEngine đã resolve captionKey → caption trước khi lưu vào cache;
    /// DynamicLookupRepository chỉ cần FieldName để xây SELECT nên không cần các field khác.
    /// </summary>
    private sealed class PopupColEntry
    {
        [System.Text.Json.Serialization.JsonPropertyName("fieldName")]
        public string Column { get; init; } = "";
        // captionKey / caption không cần cho SQL builder — chỉ cần tên cột DB
        public int Width { get; init; }
    }
}
