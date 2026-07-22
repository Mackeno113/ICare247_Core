// File    : DynamicLookupRepository.cs
// Module  : Lookup
// Layer   : Infrastructure
// Purpose : Dapper implementation của IDynamicLookupRepository.
//           Đọc cấu hình Ui_Field_Lookup rồi build + execute parameterized SQL an toàn.

using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using ICare247.Application.Constants;
using ICare247.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

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

    // Cột audit do SERVER bơm khi insert — client gửi lên thì bỏ qua (chống giả mạo CreatedBy).
    private static readonly HashSet<string> AuditColumns =
        new(StringComparer.OrdinalIgnoreCase) { "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt" };

    private readonly ICacheService _cache;
    private readonly ILookupCacheVersion _lookupVer;
    private readonly IContextParamResolver _ctxResolver;
    private readonly ILogger<DynamicLookupRepository> _logger;

    public DynamicLookupRepository(
        IDbConnectionFactory configDb, IDataDbConnectionFactory dataDb,
        ICacheService cache, ILookupCacheVersion lookupVer, IContextParamResolver ctxResolver,
        ILogger<DynamicLookupRepository> logger)
    {
        _configDb    = configDb;
        _dataDb      = dataDb;
        _cache       = cache;
        _lookupVer   = lookupVer;
        _ctxResolver = ctxResolver;
        _logger      = logger;
    }

    // ── Đọc cấu hình lookup (PICKER-P4, spec 31 §5) ──────────────────────────
    // Field chọn mẫu (Template_Code) → ĐỊNH NGHĨA TRUY VẤN lấy trọn từ Ui_Lookup_Template
    // (kể cả Filter_Sql NULL của mẫu — không rơi về Filter_Sql riêng của field); các cột
    // HIỂN THỊ phụ (Popup/Code/Parent) field được override mẫu. Template_Code trỏ mẫu không
    // tồn tại/tắt → SourceName NULL → caller trả rỗng/báo lỗi rõ (misconfig nhìn thấy được).
    private const string CfgSqlExtended = """
        SELECT CASE WHEN lt.Template_Id IS NOT NULL THEN lt.Query_Mode     ELSE fl.Query_Mode     END AS QueryMode,
               CASE WHEN lt.Template_Id IS NOT NULL THEN lt.Source_Name    ELSE fl.Source_Name    END AS SourceName,
               CASE WHEN lt.Template_Id IS NOT NULL THEN lt.Value_Column   ELSE fl.Value_Column   END AS ValueColumn,
               CASE WHEN lt.Template_Id IS NOT NULL THEN lt.Display_Column ELSE fl.Display_Column END AS DisplayColumn,
               CASE WHEN lt.Template_Id IS NOT NULL THEN lt.Filter_Sql     ELSE fl.Filter_Sql     END AS FilterSql,
               CASE WHEN lt.Template_Id IS NOT NULL THEN lt.Order_By       ELSE fl.Order_By       END AS OrderBy,
               COALESCE(fl.Popup_Columns_Json, lt.Popup_Columns_Json)                              AS PopupColumnsJson,
               COALESCE(fl.Code_Field,         lt.Code_Field)                                      AS CodeField,
               COALESCE(fl.Parent_Column,      lt.Parent_Column)                                   AS ParentColumn,
               fl.Param_Map                                                                        AS ParamMap
        FROM   dbo.Ui_Field_Lookup fl
        JOIN   dbo.Ui_Field        fi ON fi.Field_Id = fl.Field_Id
        JOIN   dbo.Ui_Form         fm ON fm.Form_Id  = fi.Form_Id
        JOIN   dbo.Sys_Table       t  ON t.Table_Id  = fm.Table_Id
        LEFT JOIN dbo.Ui_Lookup_Template lt
               ON lt.Template_Code = fl.Template_Code AND lt.Is_Active = 1
        WHERE  fl.Field_Id = @FieldId
        """;

    // Fallback khi tenant Config DB CHƯA chạy db/083 (thiếu cột/bảng mới) — hành vi y hệt trước P4.
    private const string CfgSqlLegacy = """
        SELECT fl.Query_Mode          AS QueryMode,
               fl.Source_Name         AS SourceName,
               fl.Value_Column        AS ValueColumn,
               fl.Display_Column      AS DisplayColumn,
               fl.Filter_Sql          AS FilterSql,
               fl.Order_By            AS OrderBy,
               fl.Popup_Columns_Json  AS PopupColumnsJson,
               fl.Code_Field          AS CodeField,
               fl.Parent_Column       AS ParentColumn
        FROM   dbo.Ui_Field_Lookup fl
        JOIN   dbo.Ui_Field        fi ON fi.Field_Id = fl.Field_Id
        JOIN   dbo.Ui_Form         fm ON fm.Form_Id  = fi.Form_Id
        JOIN   dbo.Sys_Table       t  ON t.Table_Id  = fm.Table_Id
        WHERE  fl.Field_Id = @FieldId
        """;

    /// <summary>Đọc cấu hình lookup (đã resolve template). Tenant chưa migrate db/083 → fallback legacy.</summary>
    private static async Task<LookupCfgRow?> LoadCfgAsync(
        IDbConnection configConn, int fieldId, int tenantId, CancellationToken ct)
    {
        try
        {
            return await configConn.QueryFirstOrDefaultAsync<LookupCfgRow>(
                new CommandDefinition(CfgSqlExtended, new { FieldId = fieldId, TenantId = tenantId },
                    cancellationToken: ct));
        }
        catch (SqlException)
        {
            // Cột Template_Code/Param_Map hoặc bảng Ui_Lookup_Template chưa có — đọc kiểu cũ.
            return await configConn.QueryFirstOrDefaultAsync<LookupCfgRow>(
                new CommandDefinition(CfgSqlLegacy, new { FieldId = fieldId, TenantId = tenantId },
                    cancellationToken: ct));
        }
    }

    /// <summary>
    /// Build tham số Dapper cho câu lookup từ 3 nguồn, theo thứ tự ưu tiên GIẢM DẦN:
    /// token <c>Sys_Context_Param</c> (server) → Param_Map của mẫu (admin) → giá trị form (client).
    /// Trả kèm map "tham số → giá trị đã bind" để hash cache key — token theo user (vd
    /// @NguoiDungID) PHẢI vào key, không thì user này đọc trúng cache của user khác.
    /// <para>
    /// Token bind TRƯỚC và khoá lại, không nguồn nào ghi đè. Trước đây client bind trước còn token
    /// chỉ điền chỗ trống ⇒ POST <c>{"NguoiDungID": &lt;id người khác&gt;}</c> thay được luôn ranh
    /// giới phân quyền của câu SQL (<c>fnt_CongTyTheoQuyen(@NguoiDungID)</c>) và đọc dữ liệu người
    /// khác. Tập khoá suy ra từ chính tên mà resolver trả về nên phủ mọi token registry — thêm
    /// token mới không phải sửa code ở đây.
    /// </para>
    /// <para>
    /// CHỈ bind đúng những @param câu SQL thực sự tham chiếu. Client gửi snapshot TOÀN form
    /// (~20-30 field) nên nếu bind hết thì <c>sp_executesql</c> khai báo hàng chục tham số cho
    /// câu chỉ dùng 1: mỗi form sinh một chuỗi khai báo khác nhau ⇒ nhân bản plan cache, và
    /// giá trị số/ngày đi qua JSON bị suy thành <c>nvarchar(4000)</c> ⇒ implicit convert làm
    /// mất index seek khi admin viết Filter_Sql so sánh với cột số/ngày. Tham số không xuất
    /// hiện trong SQL thì SQL cũng không đọc được — loại đi không mất gì.
    /// </para>
    /// <para>Sự kiện theo sau: caller dùng Dp để execute và Effective để tính cache key.</para>
    /// </summary>
    private async Task<(DynamicParameters Dp, Dictionary<string, object?> Effective)> BuildParamsAsync(
        LookupCfgRow cfg, Dictionary<string, object?> contextValues, string querySql,
        int fieldId, CancellationToken ct)
    {
        var dp = new DynamicParameters();
        var effective = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Tập @param câu SQL tham chiếu — cổng lọc duy nhất cho mọi nguồn giá trị bên dưới.
        var wanted = SqlParamRegex().Matches(querySql)
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Bind 1 tham số nếu SQL có dùng; ghi vào effective để cùng vào cache key.
        void Bind(string name, object? value)
        {
            if (!wanted.Contains(name)) return;
            dp.Add(name, value);
            effective[name] = value;
        }

        // ── Bước 1: đọc Param_Map (thuần CPU, chưa chạm DB) ──────────────────────
        // {"Canonical": "FieldCode" | "@TokenName" | hằng số}
        var mapAliases = new List<(string Canonical, string TokenName)>();   // canonical ← @Token
        var mapValues  = new List<(string Canonical, object? Value)>();      // canonical ← field / hằng số
        if (!string.IsNullOrWhiteSpace(cfg.ParamMap))
        {
            try
            {
                var map = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(cfg.ParamMap);
                foreach (var (canonical, src) in map ?? [])
                {
                    if (!SafeIdentifierRegex().IsMatch(canonical)) continue;
                    if (!wanted.Contains(canonical)) continue;   // SQL không dùng → khỏi resolve
                    if (src.ValueKind == JsonValueKind.String)
                    {
                        var s = src.GetString() ?? "";
                        if (s.StartsWith('@'))
                            mapAliases.Add((canonical, s.TrimStart('@')));
                        else
                        {
                            // Map từ field trên form — field chưa có giá trị → bind NULL (filter tự rỗng)
                            contextValues.TryGetValue(s, out var fv);
                            mapValues.Add((canonical, UnwrapParamValue(fv)));
                        }
                    }
                    else
                    {
                        // Hằng số (number/bool) — spec 31 §7 câu 2: cho phép
                        mapValues.Add((canonical, UnwrapParamValue(src)));
                    }
                }
            }
            catch (JsonException)
            {
                // Param_Map hỏng → bỏ qua; @param thiếu sẽ được chẩn đoán rõ bên dưới.
            }
        }

        // ── Bước 2: token server chốt TRƯỚC — không nguồn nào ghi đè được ────────
        // ResolveAsync chỉ trả về tên CÓ trong Sys_Context_Param, nên tập khoá tự suy ra từ
        // registry: admin thêm token mới là được bảo vệ ngay, không phải sửa code. Token trả
        // giá trị null (header rỗng…) VẪN vào khoá — nếu không thì client điền vào chỗ trống đó.
        var tokenNames = wanted.Concat(mapAliases.Select(a => a.TokenName))
                               .Distinct(StringComparer.OrdinalIgnoreCase);
        var serverValues = await _ctxResolver.ResolveAsync(tokenNames, ct);
        var locked = new HashSet<string>(serverValues.Keys, StringComparer.OrdinalIgnoreCase);

        foreach (var (name, v) in serverValues)
            Bind(name, v);

        // ── Bước 3: giá trị form do CLIENT gửi — cấm đè token ────────────────────
        // contextValues là body request, tên key do client tự đặt. Không chặn thì POST
        // {"NguoiDungID": <id user khác>} sẽ thay luôn ranh giới phân quyền của câu SQL
        // (vd fnt_CongTyTheoQuyen(@NguoiDungID)) ⇒ đọc được dữ liệu của người khác.
        foreach (var (key, val) in contextValues)
        {
            if (!SafeIdentifierRegex().IsMatch(key)) continue;
            if (locked.Contains(key))
            {
                if (wanted.Contains(key))
                    _logger.LogWarning(
                        "DynamicLookup FieldId={FieldId}: bỏ qua '{Key}' từ client — trùng token " +
                        "Sys_Context_Param nên dùng giá trị server. Đổi tên Field_Code nếu đây là field thật.",
                        fieldId, key);
                continue;
            }
            Bind(key, UnwrapParamValue(val));
        }

        // ── Bước 4: Param_Map (cấu hình admin) — cũng không đè được token ────────
        foreach (var (canonical, value) in mapValues)
        {
            if (locked.Contains(canonical))
            {
                _logger.LogWarning(
                    "DynamicLookup FieldId={FieldId}: Param_Map map '{Canonical}' sang field/hằng số " +
                    "nhưng đó là token Sys_Context_Param — giữ giá trị server.", fieldId, canonical);
                continue;
            }
            Bind(canonical, value);
        }
        foreach (var (canonical, tokenName) in mapAliases)
        {
            if (locked.Contains(canonical)) continue;   // canonical tự là token — bước 2 đã bind
            serverValues.TryGetValue(tokenName, out var v);
            Bind(canonical, v);
        }

        // ── Bước 5: @param nào vẫn chưa có giá trị → báo lỗi cấu hình rõ ràng ────
        var bound = new HashSet<string>(dp.ParameterNames, StringComparer.OrdinalIgnoreCase);
        var missing = wanted.Where(p => !bound.Contains(p)).ToList();
        if (missing.Count > 0)
        {
            var ctxKeys = contextValues.Count > 0
                ? string.Join(", ", contextValues.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                : "(rỗng)";
            throw new InvalidOperationException(
                $"DynamicLookup FieldId={fieldId}: SQL cần tham số CHƯA CÓ trên form: " +
                $"{string.Join(", ", missing.Select(p => "@" + p))}. " +
                $"Map tham số qua Param_Map (mẫu lookup), hoặc thêm field Field Code trùng tên, " +
                $"hoặc đăng ký token Sys_Context_Param. Field trên form hiện có: [{ctxKeys}].");
        }

        return (dp, effective);
    }

    /// <summary>
    /// Cache-aside cho dữ liệu lookup: hit → trả từ cache; miss → loader (chạy SQL) → ghi cache.
    /// Cache tắt (Cache:Enabled=false) → GetAsync luôn null, SetAsync no-op → luôn đọc DB.
    /// </summary>
    private async Task<IReadOnlyList<IDictionary<string, object>>> CachedAsync(
        string cacheKey,
        Func<Task<List<Dictionary<string, object>>>> loader,
        CancellationToken ct)
    {
        var hit = await _cache.GetAsync<List<Dictionary<string, object>>>(cacheKey, ct);
        if (hit is not null)
            return hit.Cast<IDictionary<string, object>>().ToList().AsReadOnly();

        var rows = await loader();
        await _cache.SetAsync(cacheKey, rows, ct: ct);
        return rows.Cast<IDictionary<string, object>>().ToList().AsReadOnly();
    }

    /// <summary>
    /// Hash các @param context có trong Filter SQL (bỏ @TenantId — đã vào key qua tenantId). Cascade cho
    /// kết quả khác nhau theo giá trị cha → phải vào key. Field không phải @param → không ảnh hưởng key.
    /// </summary>
    private static string HashContext(string? filterSql, Dictionary<string, object?> ctx)
    {
        if (string.IsNullOrWhiteSpace(filterSql)) return "0";
        var parts = SqlParamRegex().Matches(filterSql)
            .Select(m => m.Groups[1].Value)
            .Where(p => !p.Equals("TenantId", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .Select(p => $"{p}={(ctx.TryGetValue(p, out var v) ? v : null)}");
        var raw = string.Join("|", parts);
        if (raw.Length == 0) return "0";
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes, 0, 8);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IDictionary<string, object>>> QueryAsync(
        int fieldId,
        int tenantId,
        Dictionary<string, object?> contextValues,
        CancellationToken ct = default)
    {
        // ── Bước 1: Đọc cấu hình (đã resolve Ui_Lookup_Template — PICKER-P4) ──────
        // Cô lập tenant ở tầng connection (ADR-035) — Config DB đã thuộc đúng 1 tenant.
        using var configConn = _configDb.CreateConnection();
        var cfg = await LoadCfgAsync(configConn, fieldId, tenantId, ct);

        // Không có cấu hình, hoặc cấu hình chưa hoàn chỉnh (SourceName rỗng) → trả rỗng
        if (cfg is null || string.IsNullOrWhiteSpace(cfg.SourceName))
            return [];

        // ── Bước 2: Build SQL + bind ĐỦ tham số (form + Param_Map + token) TRƯỚC khi
        //    tính cache key — token theo user phải vào key, tránh dùng chung cache sai người.
        var querySql = BuildSafeSql(cfg, out var error);
        if (querySql is null)
            throw new InvalidOperationException(
                $"DynamicLookup FieldId={fieldId}: cấu hình không hợp lệ — {error}");

        var (dp, effectiveCtx) = await BuildParamsAsync(cfg, contextValues, querySql, fieldId, ct);

        // ── Bước 3: Cache-aside theo (version bảng nguồn) + hash tham số hiệu lực ──
        var cacheKey = CacheKeys.DynamicLookup(
            fieldId, tenantId, _lookupVer.Get(tenantId, cfg.SourceName!),
            HashContext(querySql, effectiveCtx), isTree: false);

        return await CachedAsync(cacheKey, async () =>
        {
            // ── Execute query trên Data DB → materialize (bỏ cột null cho JSON gọn) ──
            using var dataConn = _dataDb.CreateConnection();
            var rows = await dataConn.QueryAsync(
                new CommandDefinition(querySql, dp, cancellationToken: ct));
            return MaterializeRows(rows);
        }, ct);
    }

    /// <summary>Materialize DapperRow → Dictionary phẳng cho cache/JSON (bỏ cột value = null).</summary>
    private static List<Dictionary<string, object>> MaterializeRows(IEnumerable<dynamic> rows)
        => rows.Select(r =>
        {
            var d = new Dictionary<string, object>();
            foreach (var kv in (IDictionary<string, object>)r)
                if (kv.Value is not null) d[kv.Key] = kv.Value;
            return d;
        }).ToList();

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

        // ── Feature B (bộ control dùng chung): Query_Mode='self_parent' — chọn "cha" trong CHÍNH
        // bảng của field, tự loại chính bản ghi đang sửa + mọi hậu duệ (chống tạo vòng lặp cây).
        // Cần Parent_Column hợp lệ (đã dùng sẵn cho TreeLookupBox — Ui_Field_Lookup.Parent_Column).
        // @__SelfId do client gửi kèm contextValues khi đang sửa record có Id (rỗng khi Thêm mới →
        // CTE rỗng → NOT IN so với tập rỗng luôn TRUE → không loại gì, đúng ý nghĩa "chưa có gì để loại").
        string? selfParentCte = null;
        var whereClauses = new List<string>();
        if (!string.IsNullOrWhiteSpace(cfg.FilterSql))
            whereClauses.Add(cfg.FilterSql!);

        if (mode == "self_parent")
        {
            if (!SafeIdentifierRegex().IsMatch(cfg.ParentColumn ?? ""))
            {
                error = "Query_Mode='self_parent' cần Parent_Column hợp lệ (cột cha tự tham chiếu).";
                return null;
            }
            selfParentCte =
                $"; WITH __self_cte AS (" +
                $"SELECT {cfg.ValueColumn} AS Id FROM {cfg.SourceName} WHERE {cfg.ValueColumn} = @__SelfId " +
                $"UNION ALL " +
                $"SELECT t.{cfg.ValueColumn} FROM {cfg.SourceName} t JOIN __self_cte c ON t.{cfg.ParentColumn} = c.Id" +
                $") ";
            whereClauses.Add($"{cfg.ValueColumn} NOT IN (SELECT Id FROM __self_cte)");
        }

        // Build SELECT — gồm ValueColumn, DisplayColumn, CodeField (nếu có), và các cột từ PopupColumnsJson
        var selectCols = BuildSelectColumns(cfg);
        var sql = (selfParentCte ?? "") + $"SELECT {selectCols} FROM {cfg.SourceName}";

        if (whereClauses.Count > 0)
            sql += " WHERE " + string.Join(" AND ", whereClauses);

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
        long? userId = null,
        CancellationToken ct = default)
    {
        // ── Đọc config (đã resolve template — mẫu 'table' vẫn AddNew được) ────
        using var configConn = _configDb.CreateConnection();
        var cfg = await LoadCfgAsync(configConn, fieldId, tenantId, ct);

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
            // Cột audit do server bơm — KHÔNG cho client tự đặt (chống giả mạo CreatedBy).
            if (AuditColumns.Contains(key)) continue;
            cols.Add(key);
            dp.Add(key, UnwrapParamValue(val));
        }

        if (cols.Count == 0)
            throw new InvalidOperationException(
                $"LookupInsert FieldId={fieldId}: không có cột hợp lệ để insert.");

        using var dataConn = _dataDb.CreateConnection();

        // ── Bơm cột audit (ADR-022 §0.1): CreatedBy NOT NULL và KHÔNG có DEFAULT (db/061),
        //    nên phải set tường minh. Chỉ bơm cột bảng đích THỰC SỰ có — bảng cũ/opt-out không vỡ.
        var audit = await GetAuditColumnsAsync(dataConn, cfg.SourceName, ct);

        var insCols = cols.Select(c => $"[{c}]").ToList();
        var insVals = cols.Select(c => "@" + c).ToList();

        if (audit.Contains("CreatedBy"))
        {
            if (userId is null or 0)
                throw new InvalidOperationException(
                    $"LookupInsert FieldId={fieldId}: bảng '{cfg.SourceName}' yêu cầu CreatedBy nhưng " +
                    "không xác định được người thao tác (thiếu claim sub/NameIdentifier).");
            insCols.Add("[CreatedBy]"); insVals.Add("@__CreatedBy"); dp.Add("__CreatedBy", userId.Value);
        }
        if (audit.Contains("CreatedAt")) { insCols.Add("[CreatedAt]"); insVals.Add("SYSUTCDATETIME()"); }

        var insertSql =
            $"INSERT INTO {cfg.SourceName} ({string.Join(", ", insCols)}) " +
            $"OUTPUT INSERTED.{valueCol} AS NewValue " +
            $"VALUES ({string.Join(", ", insVals)})";

        // ── Check trùng các cột Is_Unique của form gắn bảng đích (chống trùng mã) ──
        const string uniqueColsSql = """
            SELECT sc.Column_Code
            FROM   dbo.Ui_Field  uf
            JOIN   dbo.Sys_Column sc ON sc.Column_Id = uf.Column_Id
            JOIN   dbo.Ui_Form   fm ON fm.Form_Id   = uf.Form_Id
            JOIN   dbo.Sys_Table  t ON t.Table_Id    = fm.Table_Id
            WHERE  uf.Is_Unique = 1
              AND  t.Table_Code = @SourceName
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
        var cfg = await LoadCfgAsync(configConn, fieldId, tenantId, ct);

        if (cfg is null || string.IsNullOrWhiteSpace(cfg.SourceName))
            return [];

        // ParentColumn bắt buộc phải có với TreeLookupBox
        if (string.IsNullOrWhiteSpace(cfg.ParentColumn))
            throw new InvalidOperationException(
                $"TreeLookup FieldId={fieldId}: Parent_Column chưa được cấu hình.");
        if (!SafeIdentifierRegex().IsMatch(cfg.ParentColumn))
            throw new InvalidOperationException(
                $"TreeLookup FieldId={fieldId}: Parent_Column '{cfg.ParentColumn}' chứa ký tự không hợp lệ.");

        var querySql = BuildSafeSqlForTree(cfg, out var error);
        if (querySql is null)
            throw new InvalidOperationException(
                $"TreeLookup FieldId={fieldId}: cấu hình không hợp lệ — {error}");

        // Bind đủ tham số TRƯỚC khi tính cache key (như QueryAsync — token theo user phải vào key)
        var (dp, effectiveCtx) = await BuildParamsAsync(cfg, contextValues, querySql, fieldId, ct);

        var cacheKey = CacheKeys.DynamicLookup(
            fieldId, tenantId, _lookupVer.Get(tenantId, cfg.SourceName!),
            HashContext(querySql, effectiveCtx), isTree: true);

        return await CachedAsync(cacheKey, async () =>
        {
            using var dataConn = _dataDb.CreateConnection();
            var rows = await dataConn.QueryAsync(
                new CommandDefinition(querySql, dp, cancellationToken: ct));

            // Thêm key __parentId (bỏ nếu null = node gốc; client coi thiếu key là gốc). Bỏ cột null cho JSON gọn.
            var parentCol = cfg.ParentColumn!;
            return rows.Select(r =>
            {
                var src = (IDictionary<string, object>)r;
                var d = new Dictionary<string, object>();
                foreach (var kv in src)
                    if (kv.Value is not null) d[kv.Key] = kv.Value;
                if (src.TryGetValue(parentCol, out var pv) && pv is not null)
                    d["__parentId"] = pv;
                return d;
            }).ToList();
        }, ct);
    }

    /// <summary>
    /// Tương tự BuildSafeSql nhưng bổ sung ParentColumn vào danh sách SELECT.
    /// Hỗ trợ Query_Mode='custom_sql' (vd mẫu TPL_CONG_TY nguồn TVF fnt_CongTyTheoQuyen):
    /// câu SQL dùng nguyên văn như nhánh flat, nhưng phải TỰ SELECT kèm cột cha — vì
    /// QueryTreeAsync đọc key ParentColumn trên từng dòng kết quả để gắn __parentId;
    /// thiếu cột này mọi node thành gốc (cây phẳng) mà KHÔNG có lỗi nào báo ra.
    /// <para>Sự kiện theo sau: QueryTreeAsync bind tham số (BuildParamsAsync) rồi thực thi câu SQL trả về.</para>
    /// </summary>
    private static string? BuildSafeSqlForTree(LookupCfgRow cfg, out string? error)
    {
        error = null;

        var mode = (cfg.QueryMode ?? "table").ToLower();

        if (mode == "custom_sql")
        {
            // SQL tùy chỉnh từ admin — chỉ block DDL/DML keyword (đồng nhất nhánh flat BuildSafeSql)
            if (ContainsDangerousKeyword(cfg.SourceName))
            {
                error = "custom_sql chứa keyword DDL/DML không được phép.";
                return null;
            }
            // Guard defense-in-depth: cột cha phải xuất hiện trong câu SQL (hoặc SELECT *) —
            // bắt misconfig ngay tại đây thay vì trả cây phẳng im lặng.
            if (!(cfg.SourceName ?? "").Contains(cfg.ParentColumn ?? "", StringComparison.OrdinalIgnoreCase)
                && !(cfg.SourceName ?? "").Contains('*'))
            {
                error = $"custom_sql phải SELECT kèm cột cha '{cfg.ParentColumn}' (Parent_Column) để dựng cây.";
                return null;
            }
            return cfg.SourceName;
        }

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
        /// <summary>JSON map tham số canonical của mẫu lookup ← Field_Code/@token/hằng số (Migration 083).</summary>
        public string? ParamMap         { get; init; }
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

    /// <summary>
    /// Cột audit (CreatedBy/CreatedAt/UpdatedBy/UpdatedAt) THỰC SỰ có trên bảng đích của Data DB.
    /// Bảng cũ / bảng opt-out (vd <c>HT_NguoiDung_LuoiLayout</c>) không có → engine bỏ qua, không vỡ.
    /// <para>
    /// <paramref name="sourceName"/> có thể là <c>Bang</c> hoặc <c>schema.Bang</c> — <c>SafeIdentifierRegex</c>
    /// cho phép dấu chấm. Không tách schema thì <c>TABLE_NAME</c> không khớp ⇒ trả rỗng ⇒ KHÔNG bơm CreatedBy
    /// ⇒ SQL lại báo "Cannot insert NULL into CreatedBy". Đã dính một lần, đừng bỏ nhánh này.
    /// </para>
    /// <para>Sự kiện theo sau: caller chỉ bơm những cột nằm trong tập trả về.</para>
    /// </summary>
    private static async Task<HashSet<string>> GetAuditColumnsAsync(
        IDbConnection data, string sourceName, CancellationToken ct)
    {
        var dot    = sourceName.LastIndexOf('.');
        var schema = dot > 0 ? sourceName[..dot] : "dbo";
        var table  = dot > 0 ? sourceName[(dot + 1)..] : sourceName;

        const string sql = """
            SELECT COLUMN_NAME
            FROM   INFORMATION_SCHEMA.COLUMNS
            WHERE  TABLE_SCHEMA = @Schema AND TABLE_NAME = @Table
              AND  COLUMN_NAME IN ('CreatedBy','CreatedAt','UpdatedBy','UpdatedAt')
            """;
        var rows = await data.QueryAsync<string>(
            new CommandDefinition(sql, new { Schema = schema, Table = table }, cancellationToken: ct));
        return new HashSet<string>(rows, StringComparer.OrdinalIgnoreCase);
    }
}
