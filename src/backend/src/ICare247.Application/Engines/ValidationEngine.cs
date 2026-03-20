// File    : ValidationEngine.cs
// Module  : Engines
// Layer   : Application
// Purpose : Concrete implementation của IValidationEngine — validate field/form theo rule list.

using ICare247.Application.Engines;
using ICare247.Application.Interfaces;
using ICare247.Domain.Engine;
using ICare247.Domain.Engine.Models;
using ICare247.Domain.Entities.Rule;
using ICare247.Domain.ValueObjects;

namespace ICare247.Application.Engines;

/// <summary>
/// IValidationEngine implementation.
/// Flow: Load rules → sort theo dependency → evaluate từng rule (IAstEngine) → collect kết quả fail.
/// </summary>
public sealed class ValidationEngine : IValidationEngine
{
    private readonly IAstEngine _astEngine;
    private readonly IRuleRepository _ruleRepo;
    private readonly IDependencyRepository _dependencyRepo;

    public ValidationEngine(
        IAstEngine astEngine,
        IRuleRepository ruleRepo,
        IDependencyRepository dependencyRepo)
    {
        _astEngine = astEngine;
        _ruleRepo = ruleRepo;
        _dependencyRepo = dependencyRepo;
    }

    /// <inheritdoc />
    public async Task<ValidationResponse> ValidateFieldAsync(
        int formId,
        string fieldCode,
        object? value,
        EvaluationContext context,
        int tenantId,
        CancellationToken ct = default)
    {
        // Load rules cho field này
        var rules = await _ruleRepo.GetByFieldAsync(formId, fieldCode, tenantId, ct);

        if (rules.Count == 0)
            return ValidationResponse.Valid(fieldCode);

        // Cập nhật context với giá trị mới của field đang validate
        var evalContext = context.WithValue(fieldCode, value);

        // Evaluate từng rule theo thứ tự SortOrder
        var failures = EvaluateRules(rules, evalContext);

        var isValid = !failures.Any(f => f.Severity.Equals("error", StringComparison.OrdinalIgnoreCase));
        return new ValidationResponse(fieldCode, isValid, failures);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, ValidationResponse>> ValidateFormAsync(
        int formId,
        EvaluationContext context,
        int tenantId,
        CancellationToken ct = default)
    {
        // Load tất cả rules của form — 1 query thay vì N
        var rulesByField = await _ruleRepo.GetByFormAsync(formId, tenantId, ct);

        if (rulesByField.Count == 0)
            return new Dictionary<string, ValidationResponse>();

        // Load dependencies để topological sort thứ tự validate fields
        var dependencies = await _dependencyRepo.GetByFormAsync(formId, tenantId, ct);
        var sortedFields = TopologicalSort(rulesByField.Keys, dependencies);

        var results = new Dictionary<string, ValidationResponse>(StringComparer.OrdinalIgnoreCase);

        foreach (var fieldCode in sortedFields)
        {
            if (!rulesByField.TryGetValue(fieldCode, out var rules))
                continue;

            var failures = EvaluateRules(rules, context);
            var isValid = !failures.Any(f => f.Severity.Equals("error", StringComparison.OrdinalIgnoreCase));
            results[fieldCode] = new ValidationResponse(fieldCode, isValid, failures);
        }

        return results;
    }

    // ── Core evaluation logic ───────────────────────────────────

    /// <summary>
    /// Evaluate danh sách rules, trả về list kết quả fail.
    /// Rules pass → bỏ qua (không trả về).
    /// </summary>
    private List<ValidationResult> EvaluateRules(
        IReadOnlyList<RuleMetadata> rules,
        EvaluationContext context)
    {
        var failures = new List<ValidationResult>();

        foreach (var rule in rules)
        {
            // Kiểm tra condition — nếu false thì skip rule
            if (!ShouldEvaluate(rule, context))
                continue;

            var passed = EvaluateRule(rule, context);

            if (!passed)
            {
                failures.Add(new ValidationResult(
                    rule.RuleId,
                    rule.Severity,
                    rule.ErrorMessage));
            }
        }

        return failures;
    }

    /// <summary>
    /// Kiểm tra Condition_Expr — nếu null thì luôn evaluate, nếu false thì skip.
    /// </summary>
    private bool ShouldEvaluate(RuleMetadata rule, EvaluationContext context)
    {
        if (string.IsNullOrWhiteSpace(rule.ConditionExpr))
            return true;

        try
        {
            var result = _astEngine.Evaluate(rule.ConditionExpr, context);
            return BuiltinFunctions.ToBool(result) ?? false;
        }
        catch
        {
            // Condition eval fail → skip rule (an toàn hơn là throw)
            return false;
        }
    }

    /// <summary>
    /// Evaluate một rule đơn lẻ. Trả true nếu pass, false nếu fail.
    /// </summary>
    private bool EvaluateRule(RuleMetadata rule, EvaluationContext context)
    {
        // Required rule: không cần Expression_Json
        if (rule.RuleType.Equals("Required", StringComparison.OrdinalIgnoreCase))
        {
            return EvaluateRequired(rule.FieldCode, context);
        }

        // Custom/Regex/Range: evaluate Expression_Json → expect truthy value
        if (string.IsNullOrWhiteSpace(rule.ExpressionJson))
            return true; // Không có expression → pass (tránh crash)

        try
        {
            var result = _astEngine.Evaluate(rule.ExpressionJson, context);
            return BuiltinFunctions.ToBool(result) ?? false;
        }
        catch
        {
            // Eval fail → treat as fail (conservative — hiện lỗi cho user)
            return false;
        }
    }

    /// <summary>
    /// Required check: value != null && không rỗng (string) && không whitespace only.
    /// </summary>
    private static bool EvaluateRequired(string fieldCode, EvaluationContext context)
    {
        var value = context.GetValue(fieldCode);

        if (value is null)
            return false;

        if (value is string str)
            return !string.IsNullOrWhiteSpace(str);

        return true;
    }

    // ── Topological sort ────────────────────────────────────────

    /// <summary>
    /// Topological sort danh sách field codes dựa trên dependency graph.
    /// Fields không có dependency → giữ nguyên thứ tự gốc.
    /// Cycle → bỏ qua cycle, trả về best-effort order.
    /// </summary>
    internal static IReadOnlyList<string> TopologicalSort(
        IEnumerable<string> fieldCodes,
        IReadOnlyList<FieldDependency> dependencies)
    {
        var allFields = new HashSet<string>(fieldCodes, StringComparer.OrdinalIgnoreCase);

        // Nếu không có dependency → trả nguyên thứ tự
        if (dependencies.Count == 0)
            return allFields.ToList();

        // Build adjacency list: source → targets (target phụ thuộc source → source phải đi trước)
        var inDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var adjList = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in allFields)
        {
            inDegree[field] = 0;
            adjList[field] = new List<string>();
        }

        foreach (var dep in dependencies)
        {
            // Source phải validate trước Target
            if (!allFields.Contains(dep.SourceFieldCode) || !allFields.Contains(dep.TargetFieldCode))
                continue;

            adjList[dep.SourceFieldCode].Add(dep.TargetFieldCode);
            inDegree[dep.TargetFieldCode]++;
        }

        // Kahn's algorithm
        var queue = new Queue<string>();
        foreach (var kv in inDegree)
        {
            if (kv.Value == 0)
                queue.Enqueue(kv.Key);
        }

        var sorted = new List<string>();
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(current);

            foreach (var neighbor in adjList[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }

        // Nếu có cycle → thêm remaining fields ở cuối (best-effort)
        foreach (var field in allFields)
        {
            if (!sorted.Contains(field))
                sorted.Add(field);
        }

        return sorted;
    }
}
