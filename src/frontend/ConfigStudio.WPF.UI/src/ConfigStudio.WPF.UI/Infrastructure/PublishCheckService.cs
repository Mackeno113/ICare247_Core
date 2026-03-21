// File    : PublishCheckService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Kiểm tra tính hợp lệ của form trước publish — Dapper trực tiếp DB.

using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// Thực hiện 11 publish checks qua Dapper. Mỗi check trả về CheckResult (Passed/IsWarning/Detail).
/// Nếu DB chưa cấu hình → trả Warning thay vì crash.
/// </summary>
public sealed class PublishCheckService : IPublishCheckService
{
    private readonly IAppConfigService _config;

    public PublishCheckService(IAppConfigService config)
    {
        _config = config;
    }

    // ── Helpers ──────────────────────────────────────────────

    /// <summary>Tạo kết nối SQL Server từ connection string hiện tại.</summary>
    private SqlConnection CreateConn() => new(_config.ConnectionString);

    /// <summary>Trả về Warning khi DB chưa được cấu hình.</summary>
    private static CheckResult NotConfigured() =>
        new(Passed: false, IsWarning: true, Detail: "Chưa kết nối DB — không thể kiểm tra.");

    /// <summary>
    /// Lấy tất cả Expression_Json từ Val_Rule + Evt_Definition của form.
    /// Dùng chung cho nhiều checks.
    /// </summary>
    private async Task<IReadOnlyList<(string Source, string? ExprJson)>> GetAllExpressionsAsync(
        int formId, int tenantId, SqlConnection conn, CancellationToken ct)
    {
        // ── Expressions từ Val_Rule (Field_Id trực tiếp sau Migration 003) ─
        const string sqlRules = """
            SELECT CONCAT('Rule#', vr.Rule_Id) AS Source,
                   vr.Expression_Json           AS ExprJson
            FROM   dbo.Val_Rule vr
            JOIN   dbo.Ui_Field uf ON uf.Field_Id = vr.Field_Id
            WHERE  uf.Form_Id = @FormId
              AND  uf.Tenant_Id = @TenantId
              AND  vr.Is_Active = 1
              AND  vr.Expression_Json IS NOT NULL
            """;

        // ── Expressions từ Evt_Definition ────────────────────
        const string sqlEvents = """
            SELECT CONCAT('Event#', ed.Event_Id) AS Source,
                   ed.Condition_Expr              AS ExprJson
            FROM   dbo.Evt_Definition ed
            WHERE  ed.Form_Id = @FormId
              AND  ed.Is_Active = 1
              AND  ed.Condition_Expr IS NOT NULL
            """;

        var results = new List<(string, string?)>();
        var p = new { FormId = formId, TenantId = tenantId };

        var ruleExprs = await conn.QueryAsync<(string Source, string? ExprJson)>(
            new CommandDefinition(sqlRules, p, cancellationToken: ct));
        results.AddRange(ruleExprs);

        var eventExprs = await conn.QueryAsync<(string Source, string? ExprJson)>(
            new CommandDefinition(sqlEvents, new { FormId = formId }, cancellationToken: ct));
        results.AddRange(eventExprs);

        return results;
    }

    // ── CHECK 1: Label_Key hợp lệ ────────────────────────────

    /// <inheritdoc />
    public async Task<CheckResult> CheckLabelKeysAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return NotConfigured();

        const string sql = """
            SELECT ColumnCode
            FROM   dbo.Ui_Field
            WHERE  Form_Id = @FormId
              AND  Tenant_Id = @TenantId
              AND  (Label_Key IS NULL OR LTRIM(RTRIM(Label_Key)) = '')
            """;

        await using var conn = CreateConn();
        var missing = (await conn.QueryAsync<string>(
            new CommandDefinition(sql, new { FormId = formId, TenantId = tenantId }, cancellationToken: ct)))
            .AsList();

        if (missing.Count == 0)
            return new CheckResult(Passed: true);

        return new CheckResult(
            Passed: false,
            Detail: $"{missing.Count} field thiếu Label_Key: {string.Join(", ", missing.Take(5))}{(missing.Count > 5 ? "..." : "")}");
    }

    // ── CHECK 2: Expression_Json parse được ──────────────────

    /// <inheritdoc />
    public async Task<CheckResult> CheckExpressionsParseAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return NotConfigured();

        await using var conn = CreateConn();
        var expressions = await GetAllExpressionsAsync(formId, tenantId, conn, ct);

        var invalid = new List<string>();
        foreach (var (source, json) in expressions)
        {
            if (string.IsNullOrWhiteSpace(json)) continue;
            try { JsonDocument.Parse(json); }
            catch { invalid.Add(source); }
        }

        if (invalid.Count == 0)
            return new CheckResult(Passed: true);

        return new CheckResult(
            Passed: false,
            Detail: $"{invalid.Count} expression JSON không hợp lệ: {string.Join(", ", invalid.Take(5))}");
    }

    // ── CHECK 3: Functions whitelist ─────────────────────────

    /// <inheritdoc />
    public async Task<CheckResult> CheckFunctionWhitelistAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return NotConfigured();

        await using var conn = CreateConn();

        // Load whitelist từ Gram_Function
        var whitelist = (await conn.QueryAsync<string>(
            new CommandDefinition(
                "SELECT LOWER(Function_Code) FROM dbo.Gram_Function WHERE Is_Active = 1",
                cancellationToken: ct)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (whitelist.Count == 0)
            return new CheckResult(Passed: true, IsWarning: true, Detail: "Gram_Function chưa có data — bỏ qua check.");

        var expressions = await GetAllExpressionsAsync(formId, tenantId, conn, ct);
        var violations = new List<string>();

        foreach (var (source, json) in expressions)
        {
            if (string.IsNullOrWhiteSpace(json)) continue;
            var funcs = ExtractFunctionNames(json);
            foreach (var fn in funcs.Where(f => !whitelist.Contains(f)))
                violations.Add($"{source}: '{fn}'");
        }

        if (violations.Count == 0)
            return new CheckResult(Passed: true);

        return new CheckResult(
            Passed: false,
            Detail: $"Function ngoài whitelist: {string.Join(", ", violations.Take(5))}");
    }

    // ── CHECK 4: Operators whitelist ─────────────────────────

    /// <inheritdoc />
    public async Task<CheckResult> CheckOperatorWhitelistAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return NotConfigured();

        await using var conn = CreateConn();

        var whitelist = (await conn.QueryAsync<string>(
            new CommandDefinition(
                "SELECT Operator_Symbol FROM dbo.Gram_Operator WHERE Is_Active = 1",
                cancellationToken: ct)))
            .ToHashSet(StringComparer.Ordinal);

        if (whitelist.Count == 0)
            return new CheckResult(Passed: true, IsWarning: true, Detail: "Gram_Operator chưa có data — bỏ qua check.");

        var expressions = await GetAllExpressionsAsync(formId, tenantId, conn, ct);
        var violations = new List<string>();

        foreach (var (source, json) in expressions)
        {
            if (string.IsNullOrWhiteSpace(json)) continue;
            var ops = ExtractOperators(json);
            foreach (var op in ops.Where(o => !whitelist.Contains(o)))
                violations.Add($"{source}: '{op}'");
        }

        if (violations.Count == 0)
            return new CheckResult(Passed: true);

        return new CheckResult(
            Passed: false,
            Detail: $"Operator ngoài whitelist: {string.Join(", ", violations.Take(5))}");
    }

    // ── CHECK 5: Rule return type = Boolean ──────────────────

    /// <inheritdoc />
    public async Task<CheckResult> CheckRuleReturnTypeAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return NotConfigured();

        // NOTE: Kiểm tra đơn giản — rule expression phải chứa binary/unary node (boolean-forming)
        const string sql = """
            SELECT CONCAT('Rule#', vr.Rule_Id) AS Source,
                   vr.Expression_Json           AS ExprJson,
                   vr.Rule_Type_Code            AS RuleType
            FROM   dbo.Val_Rule vr
            JOIN   dbo.Ui_Field uf ON uf.Field_Id = vr.Field_Id
            WHERE  uf.Form_Id = @FormId
              AND  uf.Tenant_Id = @TenantId
              AND  vr.Rule_Type_Code = 'CUSTOM'
              AND  vr.Is_Active = 1
              AND  vr.Expression_Json IS NOT NULL
            """;

        await using var conn = CreateConn();
        var rules = await conn.QueryAsync<(string Source, string? ExprJson, string RuleType)>(
            new CommandDefinition(sql, new { FormId = formId, TenantId = tenantId }, cancellationToken: ct));

        var issues = new List<string>();
        foreach (var (source, json, _) in rules)
        {
            if (string.IsNullOrWhiteSpace(json)) continue;
            // Kiểm tra đơn giản: expression CUSTOM phải có ít nhất 1 binary/unary/function node
            if (!json.Contains("\"binary\"", StringComparison.OrdinalIgnoreCase)
             && !json.Contains("\"unary\"", StringComparison.OrdinalIgnoreCase)
             && !json.Contains("\"function\"", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(source);
            }
        }

        if (issues.Count == 0)
            return new CheckResult(Passed: true);

        return new CheckResult(
            Passed: false,
            IsWarning: true, // Warning vì không thể verify type chính xác không có full AST engine
            Detail: $"{issues.Count} CUSTOM rule có thể không trả Boolean: {string.Join(", ", issues.Take(3))}");
    }

    // ── CHECK 6: Calculate return type ───────────────────────

    /// <inheritdoc />
    public Task<CheckResult> CheckCalculateReturnTypeAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        // NOTE: Cần type inference đầy đủ — bỏ qua, trả Warning để không block publish
        return Task.FromResult(new CheckResult(
            Passed: true,
            IsWarning: true,
            Detail: "Chưa hỗ trợ tự động — kiểm tra thủ công tính tương thích kiểu dữ liệu."));
    }

    // ── CHECK 7: Circular dependency ─────────────────────────

    /// <inheritdoc />
    public async Task<CheckResult> CheckCircularDependencyAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return NotConfigured();

        // Kiểm tra qua Sys_Dependency (nếu có) hoặc tự build từ expressions
        const string sql = """
            SELECT Source_Field_Code AS SourceId,
                   Target_Field_Code AS TargetId
            FROM   dbo.Sys_Dependency
            WHERE  Form_Id = @FormId
              AND  Tenant_Id = @TenantId
            """;

        await using var conn = CreateConn();
        IReadOnlyList<(string SourceId, string TargetId)> edges;
        try
        {
            edges = (await conn.QueryAsync<(string SourceId, string TargetId)>(
                new CommandDefinition(sql, new { FormId = formId, TenantId = tenantId }, cancellationToken: ct)))
                .AsList();
        }
        catch
        {
            // Sys_Dependency có thể chưa có — skip
            return new CheckResult(Passed: true, IsWarning: true, Detail: "Bảng Sys_Dependency chưa được build.");
        }

        if (edges.Count == 0)
            return new CheckResult(Passed: true);

        // DFS phát hiện cycle
        var adj = new Dictionary<string, List<string>>();
        foreach (var (src, tgt) in edges)
        {
            if (!adj.ContainsKey(src)) adj[src] = [];
            adj[src].Add(tgt);
        }

        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();
        var circularNodes = new HashSet<string>();

        foreach (var node in adj.Keys)
            DfsDetect(node, adj, visited, inStack, circularNodes);

        if (circularNodes.Count == 0)
            return new CheckResult(Passed: true);

        return new CheckResult(
            Passed: false,
            Detail: $"Circular dependency: {string.Join(", ", circularNodes.Take(5))}");
    }

    private static void DfsDetect(
        string nodeId,
        Dictionary<string, List<string>> adj,
        HashSet<string> visited,
        HashSet<string> inStack,
        HashSet<string> circular)
    {
        if (inStack.Contains(nodeId)) { circular.Add(nodeId); return; }
        if (visited.Contains(nodeId)) return;

        visited.Add(nodeId);
        inStack.Add(nodeId);

        if (adj.TryGetValue(nodeId, out var neighbors))
            foreach (var n in neighbors)
            {
                DfsDetect(n, adj, visited, inStack, circular);
                if (circular.Contains(n)) circular.Add(nodeId);
            }

        inStack.Remove(nodeId);
    }

    // ── CHECK 8: AST depth ≤ 20 ──────────────────────────────

    /// <inheritdoc />
    public async Task<CheckResult> CheckAstDepthAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return NotConfigured();

        await using var conn = CreateConn();
        var expressions = await GetAllExpressionsAsync(formId, tenantId, conn, ct);

        var violations = new List<string>();
        foreach (var (source, json) in expressions)
        {
            if (string.IsNullOrWhiteSpace(json)) continue;
            var depth = MeasureJsonDepth(json);
            if (depth > 20)
                violations.Add($"{source} (depth={depth})");
        }

        if (violations.Count == 0)
            return new CheckResult(Passed: true);

        return new CheckResult(
            Passed: false,
            Detail: $"AST depth > 20: {string.Join(", ", violations.Take(3))}");
    }

    // ── CHECK 9: Error_Key i18n ───────────────────────────────

    /// <inheritdoc />
    public async Task<CheckResult> CheckI18nCompletenessAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return NotConfigured();

        // Lấy tất cả Error_Key từ rules của form
        const string sqlKeys = """
            SELECT DISTINCT vr.Error_Key
            FROM   dbo.Val_Rule vr
            JOIN   dbo.Ui_Field uf ON uf.Field_Id = vr.Field_Id
            WHERE  uf.Form_Id = @FormId
              AND  uf.Tenant_Id = @TenantId
              AND  vr.Is_Active = 1
              AND  vr.Error_Key IS NOT NULL
              AND  vr.Error_Key <> ''
            """;

        // Lấy danh sách ngôn ngữ active
        const string sqlLangs = "SELECT Lang_Code FROM dbo.Sys_Language WHERE Is_Active = 1";

        // Lấy tất cả resources đã có
        const string sqlResources = """
            SELECT Resource_Key, Lang_Code
            FROM   dbo.Sys_Resource
            WHERE  Resource_Key IN @Keys
            """;

        await using var conn = CreateConn();

        var errorKeys = (await conn.QueryAsync<string>(
            new CommandDefinition(sqlKeys, new { FormId = formId, TenantId = tenantId }, cancellationToken: ct)))
            .AsList();

        if (errorKeys.Count == 0)
            return new CheckResult(Passed: true);

        var languages = (await conn.QueryAsync<string>(
            new CommandDefinition(sqlLangs, cancellationToken: ct)))
            .AsList();

        if (languages.Count == 0)
            return new CheckResult(Passed: true, IsWarning: true, Detail: "Chưa có ngôn ngữ nào active trong Sys_Language.");

        var resources = (await conn.QueryAsync<(string Key, string Lang)>(
            new CommandDefinition(sqlResources, new { Keys = errorKeys }, cancellationToken: ct)))
            .GroupBy(r => r.Key)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Lang).ToHashSet());

        var missing = new List<string>();
        foreach (var key in errorKeys)
        {
            if (!resources.TryGetValue(key, out var translatedLangs))
            {
                missing.Add($"'{key}' (thiếu tất cả ngôn ngữ)");
                continue;
            }

            var missingLangs = languages.Where(l => !translatedLangs.Contains(l)).ToList();
            if (missingLangs.Count > 0)
                missing.Add($"'{key}' thiếu [{string.Join(",", missingLangs)}]");
        }

        if (missing.Count == 0)
            return new CheckResult(Passed: true);

        return new CheckResult(
            Passed: false,
            Detail: $"{missing.Count} Error_Key thiếu bản dịch: {string.Join("; ", missing.Take(3))}{(missing.Count > 3 ? "..." : "")}");
    }

    // ── CHECK 10: CallAPI URL format ─────────────────────────

    /// <inheritdoc />
    public async Task<CheckResult> CheckCallApiUrlsAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return NotConfigured();

        const string sql = """
            SELECT CONCAT('Event#', ea.Event_Id, '/Action#', ea.Action_Id) AS Source,
                   ea.Action_Param_Json AS ParamJson
            FROM   dbo.Evt_Action ea
            JOIN   dbo.Evt_Definition ed ON ed.Event_Id = ea.Event_Id
            WHERE  ed.Form_Id = @FormId
              AND  ea.Action_Code = 'CALL_API'
              AND  ea.Action_Param_Json IS NOT NULL
            """;

        await using var conn = CreateConn();
        var actions = (await conn.QueryAsync<(string Source, string? ParamJson)>(
            new CommandDefinition(sql, new { FormId = formId }, cancellationToken: ct)))
            .AsList();

        if (actions.Count == 0)
            return new CheckResult(Passed: true);

        var invalid = new List<string>();
        foreach (var (source, json) in actions)
        {
            if (string.IsNullOrWhiteSpace(json)) { invalid.Add($"{source}: JSON null"); continue; }

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("url", out var urlEl))
                {
                    invalid.Add($"{source}: thiếu field 'url'");
                    continue;
                }

                var url = urlEl.GetString();
                if (string.IsNullOrWhiteSpace(url)
                 || (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                  && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                  && !url.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)))
                {
                    invalid.Add($"{source}: URL không hợp lệ '{url}'");
                }
            }
            catch
            {
                invalid.Add($"{source}: JSON parse lỗi");
            }
        }

        if (invalid.Count == 0)
            return new CheckResult(Passed: true);

        return new CheckResult(
            Passed: false,
            Detail: $"CallAPI URL không hợp lệ: {string.Join("; ", invalid.Take(3))}");
    }

    // ── CHECK 11: Sys_Dependency đầy đủ ─────────────────────

    /// <inheritdoc />
    public async Task<CheckResult> CheckDependencyGraphAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return NotConfigured();

        // Đếm cross-field references trong expressions
        await using var conn = CreateConn();
        var expressions = await GetAllExpressionsAsync(formId, tenantId, conn, ct);

        int totalRefs = 0;
        foreach (var (_, json) in expressions)
        {
            if (!string.IsNullOrWhiteSpace(json))
                totalRefs += CountIdentifierReferences(json);
        }

        if (totalRefs == 0)
            return new CheckResult(Passed: true); // Không có cross-field refs → không cần graph

        // Kiểm tra Sys_Dependency có entries
        int depCount = 0;
        try
        {
            const string sqlCount = """
                SELECT COUNT(*) FROM dbo.Sys_Dependency
                WHERE Form_Id = @FormId AND Tenant_Id = @TenantId
                """;
            depCount = await conn.QuerySingleAsync<int>(
                new CommandDefinition(sqlCount, new { FormId = formId, TenantId = tenantId }, cancellationToken: ct));
        }
        catch
        {
            return new CheckResult(Passed: true, IsWarning: true, Detail: "Bảng Sys_Dependency chưa tồn tại.");
        }

        if (depCount > 0)
            return new CheckResult(Passed: true);

        return new CheckResult(
            Passed: false,
            IsWarning: true,
            Detail: $"Tìm thấy {totalRefs} field reference trong expressions nhưng Sys_Dependency chưa được build. Chạy Regenerate trong DependencyViewer.");
    }

    // ── Helpers phân tích JSON expression ────────────────────

    /// <summary>
    /// Extract tên hàm từ expression JSON. Tìm pattern "type":"function","name":"FnCode".
    /// </summary>
    private static IReadOnlyList<string> ExtractFunctionNames(string json)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pos = 0;
        while (pos < json.Length)
        {
            var idx = json.IndexOf("\"function\"", pos, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;

            var nameIdx = json.IndexOf("\"name\"", idx, StringComparison.OrdinalIgnoreCase);
            if (nameIdx < 0) break;

            var colon = json.IndexOf(':', nameIdx + 6);
            if (colon < 0) break;

            var q1 = json.IndexOf('"', colon + 1);
            if (q1 < 0) break;

            var q2 = json.IndexOf('"', q1 + 1);
            if (q2 < 0) break;

            var name = json[(q1 + 1)..q2];
            if (!string.IsNullOrWhiteSpace(name))
                result.Add(name);

            pos = q2 + 1;
        }
        return result.ToList();
    }

    /// <summary>
    /// Extract operators từ expression JSON. Tìm pattern "operator":"OP".
    /// </summary>
    private static IReadOnlyList<string> ExtractOperators(string json)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        var pos = 0;
        while (pos < json.Length)
        {
            var idx = json.IndexOf("\"operator\"", pos, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;

            var colon = json.IndexOf(':', idx + 10);
            if (colon < 0) break;

            var q1 = json.IndexOf('"', colon + 1);
            if (q1 < 0) break;

            var q2 = json.IndexOf('"', q1 + 1);
            if (q2 < 0) break;

            var op = json[(q1 + 1)..q2];
            if (!string.IsNullOrWhiteSpace(op))
                result.Add(op);

            pos = q2 + 1;
        }
        return result.ToList();
    }

    /// <summary>Đếm độ sâu JSON bằng cách track nesting của { và [.</summary>
    private static int MeasureJsonDepth(string json)
    {
        int max = 0, current = 0;
        foreach (var ch in json)
        {
            if (ch == '{' || ch == '[') { current++; if (current > max) max = current; }
            else if (ch == '}' || ch == ']') current--;
        }
        return max;
    }

    /// <summary>Đếm số identifier nodes (field references) trong JSON.</summary>
    private static int CountIdentifierReferences(string json)
    {
        int count = 0, pos = 0;
        while (pos < json.Length)
        {
            var idx = json.IndexOf("\"identifier\"", pos, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;
            count++;
            pos = idx + 12;
        }
        return count;
    }
}
