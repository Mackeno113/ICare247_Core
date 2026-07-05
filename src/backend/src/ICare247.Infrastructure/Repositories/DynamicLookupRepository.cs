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

    // Tách @param trong SQL (bỏ @@global). Dùng để chẩn đoán tham số chưa bind.
    [GeneratedRegex(@"(?<!@)@(\w+)", RegexOptions.Compiled)]
    private static partial Regex SqlParamRegex();

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
                   fl.Code_Field                           AS CodeField,
                   fl.Parent_Column                        AS ParentColumn
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
                dp.Add(key, UnwrapParamValue(val));
        }

        // ── Chẩn đoán: @param trong SQL chưa được bind → báo lỗi RÕ thay vì SqlException
        //    "Must declare the scalar variable @X" khó hiểu. Thường do field cha (VD field ảo
        //    Tỉnh/Ngân hàng) không có trên form, hoặc Field Code không trùng tên @param. ──
        var boundNames = new HashSet<string>(dp.ParameterNames, StringComparer.OrdinalIgnoreCase);
        var missingParams = SqlParamRegex().Matches(querySql)
            .Select(m => m.Groups[1].Value)
            .Where(p => !boundNames.Contains(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missingParams.Count > 0)
        {
            var ctxKeys = contextValues.Count > 0
                ? string.Join(", ", contextValues.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                : "(rỗng)";
            throw new InvalidOperationException(
                $"DynamicLookup FieldId={fieldId}: Filter SQL cần tham số CHƯA CÓ trên form: " +
                $"{string.Join(", ", missingParams.Select(p => "@" + p))}. " +
                $"Cần 1 field (thường là field ảo cha, VD Tỉnh/Ngân hàng) có Field Code trùng đúng tên đó. " +
                $"Field trên form hiện có: [{ctxKeys}].");
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
        if (!IsValidColumnExpr(cfg.ValueColumn, strict: true, out var colErr))
        {
            error = $"ValueColumn: {colErr}";
            return null;
        }
        // DisplayColumn cho phép SQL expression (CONCAT, alias...) — chỉ block DDL/DML
        if (!IsValidColumnExpr(cfg.DisplayColumn, strict: false, out colErr))
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
        var cols = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Cột đơn giản — phải pass SafeIdentifierRegex
        void AddSimpleCol(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var col = name.Trim();
            if (SafeIdentifierRegex().IsMatch(col) && seen.Add(col))
                cols.Add(col);
        }

        // Expression hoặc cột — nếu là expression (có dấu ngoặc/space) thêm thẳng,
        // nếu là cột đơn thì check identifier. Key = alias (nếu có) để dedup chính xác.
        void AddColOrExpr(string? expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return;
            var col = expr.Trim();
            // Dedup theo alias: "CONCAT(...) AS TenTinh" và "TenTinh" coi là cùng cột
            var key = ExtractAliasKey(col).ToUpperInvariant();
            if (!seen.Add(key)) return;
            // Expression: chứa '(' hoặc space → thêm thẳng (đã validate không có DDL/DML)
            if (col.Contains('(') || col.Contains(' ') || col.Contains('\''))
                cols.Add(col);
            else if (SafeIdentifierRegex().IsMatch(col))
                cols.Add(col);
        }

        // ValueColumn: luôn là cột đơn (lưu vào DB)
        AddSimpleCol(cfg.ValueColumn);
        // DisplayColumn: có thể là expression như CONCAT(...)
        AddColOrExpr(cfg.DisplayColumn);

        // CodeField — dùng khi EditBoxMode = CodeAndName
        AddSimpleCol(cfg.CodeField);

        // Popup columns từ JSON: [{"fieldName":"MaPhongBan","caption":"Mã",...}, ...]
        if (!string.IsNullOrWhiteSpace(cfg.PopupColumnsJson))
        {
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var popupCols = JsonSerializer.Deserialize<List<PopupColEntry>>(cfg.PopupColumnsJson, opts);
                foreach (var pc in popupCols ?? [])
                    AddSimpleCol(pc.Column);
            }
            catch
            {
                // JSON không hợp lệ → bỏ qua, dùng ValueColumn + DisplayColumn là đủ
            }
        }

        return string.Join(", ", cols);
    }

    /// <summary>
    /// Validate column/expression.
    /// strict=true (ValueColumn): phải là identifier đơn [a-zA-Z0-9_.].
    /// strict=false (DisplayColumn): cho phép SQL expression — chỉ block DDL/DML keyword.
    /// </summary>
    private static bool IsValidColumnExpr(string? expr, bool strict, out string? err)
    {
        err = null;
        if (string.IsNullOrWhiteSpace(expr))
        {
            err = "Tên cột rỗng.";
            return false;
        }

        if (ContainsDangerousKeyword(expr))
        {
            err = $"'{expr}' chứa keyword DDL/DML không được phép.";
            return false;
        }

        if (strict)
        {
            // ValueColumn: identifier đơn hoặc danh sách identifier
            foreach (var part in expr.Split(','))
            {
                var col = part.Trim();
                if (!SafeIdentifierRegex().IsMatch(col))
                {
                    err = $"Tên cột '{col}' chứa ký tự không hợp lệ.";
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Trích alias để dedup cột trong SELECT.
    /// "CONCAT(...) AS TenTinh" → "TenTinh"
    /// "CONCAT(...) TenTinh"    → "TenTinh"
    /// "TenTinh"                → "TenTinh"
    /// </summary>
    private static string ExtractAliasKey(string expr)
    {
        var t = expr.Trim();
        var asMatch = System.Text.RegularExpressions.Regex.Match(
            t, @"\bAS\s+(\w+)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (asMatch.Success) return asMatch.Groups[1].Value;
        if (t.Contains('('))
        {
            var lastSpace = t.LastIndexOf(' ');
            if (lastSpace >= 0) return t[(lastSpace + 1)..].Trim();
        }
        return t;
    }

    private static bool ContainsDangerousKeyword(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return false;
        var upper = sql.ToUpperInvariant();
        return DangerousKeywords.Any(kw => upper.Contains(kw));
    }

    /// <summary>
    /// Chuyển giá trị context về kiểu nguyên thuỷ mà Dapper bind được.
    /// ContextValues từ API là <see cref="Dictionary{TKey,TValue}"/> giá trị object?,
    /// nhưng System.Text.Json deserialize mỗi value thành <see cref="JsonElement"/> —
    /// Dapper ném NotSupportedException nếu add thẳng. Hàm này unwrap về string/long/double/bool/null.
    /// Giá trị không phải JsonElement (đã là primitive) được giữ nguyên.
    /// Sự kiện theo sau: kết quả được add vào <c>DynamicParameters</c> cho câu lookup parameterized.
    /// </summary>
    private static object? UnwrapParamValue(object? val)
    {
        if (val is not JsonElement el) return val;

        return el.ValueKind switch
        {
            JsonValueKind.String    => el.GetString(),
            JsonValueKind.Number    => el.TryGetInt64(out var i)  ? i
                                     : el.TryGetDouble(out var d) ? d
                                     : (object?)el.GetRawText(),
            JsonValueKind.True      => true,
            JsonValueKind.False     => false,
            JsonValueKind.Null      => null,
            JsonValueKind.Undefined => null,
            _                       => el.GetRawText()
        };
    }

    // ── InsertAsync (thêm mới entity từ LookupBox) ──────────────────────────────

    /// <inheritdoc />
    public async Task<IDictionary<string, object?>?> InsertAsync(
        int fieldId,
        int tenantId,
        Dictionary<string, object?> values,
        CancellationToken ct = default)
    {
        // ── Đọc config + verify tenant (giống QueryAsync) ─────────────────────
        const string cfgSql = """
            SELECT fl.Query_Mode     AS QueryMode,
                   fl.Source_Name    AS SourceName,
                   fl.Value_Column   AS ValueColumn,
                   fl.Display_Column AS DisplayColumn
            FROM   dbo.Ui_Field_Lookup fl
            JOIN   dbo.Ui_Field        fi ON fi.Field_Id = fl.Field_Id
            JOIN   dbo.Ui_Form         fm ON fm.Form_Id  = fi.Form_Id
            JOIN   dbo.Sys_Table       t  ON t.Table_Id  = fm.Table_Id
            WHERE  fl.Field_Id = @FieldId
              AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
            """;

        using var configConn = _configDb.CreateConnection();
        var cfg = await configConn.QueryFirstOrDefaultAsync<LookupCfgRow>(
            new CommandDefinition(cfgSql, new { FieldId = fieldId, TenantId = tenantId },
                cancellationToken: ct));

        if (cfg is null || string.IsNullOrWhiteSpace(cfg.SourceName))
            throw new InvalidOperationException(
                $"LookupInsert FieldId={fieldId}: không tìm thấy cấu hình hoặc thiếu Source_Name.");

        // Chỉ insert vào bảng thực — TVF / custom_sql không hỗ trợ
        if (!string.Equals(cfg.QueryMode ?? "table", "table", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"LookupInsert FieldId={fieldId}: chỉ hỗ trợ Query_Mode='table' (hiện: {cfg.QueryMode}).");

        if (!SafeIdentifierRegex().IsMatch(cfg.SourceName))
            throw new InvalidOperationException(
                $"LookupInsert FieldId={fieldId}: Source_Name '{cfg.SourceName}' chứa ký tự không hợp lệ.");

        var valueCol = (cfg.ValueColumn ?? "").Trim();
        if (!SafeIdentifierRegex().IsMatch(valueCol))
            throw new InvalidOperationException(
                $"LookupInsert FieldId={fieldId}: Value_Column '{valueCol}' không hợp lệ.");

        // ── Build INSERT parameterized — chỉ nhận cột có tên identifier hợp lệ ──
        var cols = new List<string>();
        var dp   = new DynamicParameters();
        foreach (var (key, val) in values)
        {
            // Bỏ qua cột khóa (ValueColumn thường là identity) + tên không an toàn
            if (!SafeIdentifierRegex().IsMatch(key)) continue;
            if (key.Equals(valueCol, StringComparison.OrdinalIgnoreCase)) continue;
            cols.Add(key);
            dp.Add(key, UnwrapParamValue(val));
        }

        if (cols.Count == 0)
            throw new InvalidOperationException(
                $"LookupInsert FieldId={fieldId}: không có cột hợp lệ để insert.");

        var colList   = string.Join(", ", cols);
        var paramList = string.Join(", ", cols.Select(c => "@" + c));
        var insertSql =
            $"INSERT INTO {cfg.SourceName} ({colList}) " +
            $"OUTPUT INSERTED.{valueCol} AS NewValue " +
            $"VALUES ({paramList})";

        using var dataConn = _dataDb.CreateConnection();

        // ── Check trùng các cột Is_Unique của form gắn bảng đích (chống trùng mã) ──
        const string uniqueColsSql = """
            SELECT sc.Column_Code
            FROM   dbo.Ui_Field  uf
            JOIN   dbo.Sys_Column sc ON sc.Column_Id = uf.Column_Id
            JOIN   dbo.Ui_Form   fm ON fm.Form_Id   = uf.Form_Id
            JOIN   dbo.Sys_Table  t ON t.Table_Id    = fm.Table_Id
            WHERE  uf.Is_Unique = 1
              AND  t.Table_Code = @SourceName
              AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
            """;
        var uniqueCols = (await configConn.QueryAsync<string>(
            new CommandDefinition(uniqueColsSql, new { cfg.SourceName, TenantId = tenantId },
                cancellationToken: ct))).ToList();

        foreach (var ucol in uniqueCols)
        {
            if (!SafeIdentifierRegex().IsMatch(ucol)) continue;
            var hit  = values.FirstOrDefault(kv => kv.Key.Equals(ucol, StringComparison.OrdinalIgnoreCase));
            var uval = UnwrapParamValue(hit.Value);
            if (uval is null || string.IsNullOrWhiteSpace(uval.ToString())) continue;

            var dupCount = await dataConn.ExecuteScalarAsync<int>(new CommandDefinition(
                $"SELECT COUNT(*) FROM {cfg.SourceName} WHERE [{ucol}] = @Val",
                new { Val = uval }, cancellationToken: ct));
            if (dupCount > 0)
                throw new ICare247.Domain.Exceptions.DuplicateValueException(cfg.SourceName, ucol);
        }

        var newValue = await dataConn.ExecuteScalarAsync<object>(
            new CommandDefinition(insertSql, dp, cancellationToken: ct));

        // Display lấy từ chính giá trị vừa nhập (cột Display, theo alias key)
        var displayKey = ExtractAliasKey(cfg.DisplayColumn ?? "");
        var display = values.TryGetValue(displayKey, out var dv) ? UnwrapParamValue(dv) : null;

        return new Dictionary<string, object?>
        {
            ["value"]   = newValue,
            ["display"] = display
        };
    }

    // ── QueryTreeAsync ────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<IDictionary<string, object>>> QueryTreeAsync(
        int fieldId,
        int tenantId,
        Dictionary<string, object?> contextValues,
        CancellationToken ct = default)
    {
        using var configConn = _configDb.CreateConnection();
        var cfg = await configConn.QueryFirstOrDefaultAsync<LookupCfgRow>(
            new CommandDefinition(
                // Dùng lại cfgSql constant — định nghĩa inline vì method khác scope
                """
                SELECT fl.Query_Mode       AS QueryMode,
                       fl.Source_Name      AS SourceName,
                       fl.Value_Column     AS ValueColumn,
                       fl.Display_Column   AS DisplayColumn,
                       fl.Filter_Sql       AS FilterSql,
                       fl.Order_By        AS OrderBy,
                       fl.Popup_Columns_Json AS PopupColumnsJson,
                       fl.Code_Field      AS CodeField,
                       fl.Parent_Column   AS ParentColumn
                FROM   dbo.Ui_Field_Lookup fl
                JOIN   dbo.Ui_Field        fi ON fi.Field_Id = fl.Field_Id
                JOIN   dbo.Ui_Form         fm ON fm.Form_Id  = fi.Form_Id
                JOIN   dbo.Sys_Table       t  ON t.Table_Id  = fm.Table_Id
                WHERE  fl.Field_Id = @FieldId
                  AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
                """,
                new { FieldId = fieldId, TenantId = tenantId },
                cancellationToken: ct));

        if (cfg is null || string.IsNullOrWhiteSpace(cfg.SourceName))
            return [];

        // ParentColumn bắt buộc phải có với TreeLookupBox
        if (string.IsNullOrWhiteSpace(cfg.ParentColumn))
            throw new InvalidOperationException(
                $"TreeLookup FieldId={fieldId}: Parent_Column chưa được cấu hình.");

        if (!SafeIdentifierRegex().IsMatch(cfg.ParentColumn))
            throw new InvalidOperationException(
                $"TreeLookup FieldId={fieldId}: Parent_Column '{cfg.ParentColumn}' chứa ký tự không hợp lệ.");

        // Build SQL giống QueryAsync nhưng inject thêm ParentColumn vào SELECT
        var cfgWithParent = cfg; // đã có ParentColumn
        var querySql = BuildSafeSqlForTree(cfgWithParent, out var error);
        if (querySql is null)
            throw new InvalidOperationException(
                $"TreeLookup FieldId={fieldId}: cấu hình không hợp lệ — {error}");

        var dp = new DynamicParameters();
        dp.Add("TenantId", tenantId);
        foreach (var (key, val) in contextValues)
        {
            if (SafeIdentifierRegex().IsMatch(key) && !key.Equals("TenantId", StringComparison.OrdinalIgnoreCase))
                dp.Add(key, UnwrapParamValue(val));
        }

        using var dataConn = _dataDb.CreateConnection();
        var rows = await dataConn.QueryAsync(
            new CommandDefinition(querySql, dp, cancellationToken: ct));

        // Thêm key chuẩn __parentId vào mỗi row để client không cần biết tên cột gốc
        var parentCol = cfg.ParentColumn;
        return rows
            .Select(r =>
            {
                var dict = (IDictionary<string, object>)r;
                var parentVal = dict.TryGetValue(parentCol, out var pv) ? pv : null;
                dict["__parentId"] = parentVal!;
                return dict;
            })
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Tương tự BuildSafeSql nhưng bổ sung ParentColumn vào danh sách SELECT.
    /// </summary>
    private static string? BuildSafeSqlForTree(LookupCfgRow cfg, out string? error)
    {
        error = null;

        if (!SafeIdentifierRegex().IsMatch(cfg.SourceName ?? ""))
        {
            error = $"SourceName '{cfg.SourceName}' chứa ký tự không hợp lệ.";
            return null;
        }
        if (!IsValidColumnExpr(cfg.ValueColumn, strict: true, out var colErr))
        { error = $"ValueColumn: {colErr}"; return null; }
        if (!IsValidColumnExpr(cfg.DisplayColumn, strict: false, out colErr))
        { error = $"DisplayColumn: {colErr}"; return null; }
        if (!string.IsNullOrWhiteSpace(cfg.FilterSql) && ContainsDangerousKeyword(cfg.FilterSql))
        { error = "FilterSql chứa keyword DDL/DML không được phép."; return null; }
        if (!string.IsNullOrWhiteSpace(cfg.OrderBy) && ContainsDangerousKeyword(cfg.OrderBy))
        { error = "OrderBy chứa keyword DDL/DML không được phép."; return null; }

        // Build SELECT — thêm ParentColumn vào danh sách cột
        var cols = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void AddCol(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var c = name.Trim();
            if (SafeIdentifierRegex().IsMatch(c) && seen.Add(c.ToUpperInvariant())) cols.Add(c);
        }
        AddCol(cfg.ValueColumn);
        if (!string.IsNullOrWhiteSpace(cfg.DisplayColumn))
        {
            var d = cfg.DisplayColumn.Trim();
            var key = ExtractAliasKey(d).ToUpperInvariant();
            if (seen.Add(key)) cols.Add(d);
        }
        AddCol(cfg.ParentColumn);   // cột quan trọng nhất của tree
        AddCol(cfg.CodeField);

        var selectCols = string.Join(", ", cols);
        var sql = $"SELECT {selectCols} FROM {cfg.SourceName}";
        if (!string.IsNullOrWhiteSpace(cfg.FilterSql)) sql += $" WHERE {cfg.FilterSql}";
        if (!string.IsNullOrWhiteSpace(cfg.OrderBy))   sql += $" ORDER BY {cfg.OrderBy}";
        return sql;
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
        /// <summary>Cột chứa Parent Id — dùng khi EditorType = TreeLookupBox (Migration 021).</summary>
        public string? ParentColumn     { get; init; }
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
