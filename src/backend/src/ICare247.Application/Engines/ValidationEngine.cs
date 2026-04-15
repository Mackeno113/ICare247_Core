// File    : ValidationEngine.cs
// Module  : Engines
// Layer   : Application
// Purpose : Concrete implementation của IValidationEngine — validate field/form theo rule list.

using ICare247.Application.Interfaces;
using ICare247.Domain.Engine;
using ICare247.Domain.Engine.Models;
using ICare247.Domain.Entities.Rule;
using ICare247.Domain.ValueObjects;

namespace ICare247.Application.Engines;

/// <summary>
/// IValidationEngine implementation.
/// Flow: Check Is_Required → Load rules → sort theo dependency → evaluate từng rule (IAstEngine) → collect kết quả fail.
/// <para>
/// Thông báo lỗi resolve qua <see cref="ResourceResolver"/> theo fallback hierarchy:
/// form+field specific → global template → hardcoded.
/// Xem spec: docs/spec/10_RESOURCE_KEY_CONVENTION.md
/// </para>
/// </summary>
public sealed class ValidationEngine : IValidationEngine
{
    private readonly IAstEngine            _astEngine;
    private readonly IRuleRepository       _ruleRepo;
    private readonly IDependencyRepository _dependencyRepo;
    private readonly IFieldRepository      _fieldRepo;

    public ValidationEngine(
        IAstEngine            astEngine,
        IRuleRepository       ruleRepo,
        IDependencyRepository dependencyRepo,
        IFieldRepository      fieldRepo)
    {
        _astEngine      = astEngine;
        _ruleRepo       = ruleRepo;
        _dependencyRepo = dependencyRepo;
        _fieldRepo      = fieldRepo;
    }

    /// <inheritdoc />
    public async Task<ValidationResponse> ValidateFieldAsync(
        int formId,
        string fieldCode,
        object? value,
        EvaluationContext context,
        int tenantId,
        string langCode = "vi",
        IReadOnlyDictionary<string, string>? resourceMap = null,
        string formCode = "",
        CancellationToken ct = default)
    {
        var failures = new List<ValidationResult>();

        // ── 1. Check Is_Required trước khi evaluate rules ─────────────────
        var formFields = await _fieldRepo.GetByFormIdAsync(formId, tenantId, langCode, ct);
        var fieldMeta  = formFields.FirstOrDefault(
            f => f.FieldCode.Equals(fieldCode, StringComparison.OrdinalIgnoreCase));

        if (fieldMeta is not null && fieldMeta.IsRequired && IsEmpty(value))
        {
            // Resolve thông báo lỗi qua ResourceResolver — dùng field label nếu có
            var requiredMsg = ResourceResolver.ResolveRequired(
                resourceMap, formCode, fieldCode,
                fieldLabel: fieldMeta.Label,
                langCode);

            failures.Add(new ValidationResult(RuleId: 0, Severity: "error", Message: requiredMsg));

            // Required fail → trả ngay, không cần evaluate rules tiếp
            return new ValidationResponse(fieldCode, false, failures);
        }

        // ── 2. Load và evaluate Val_Rules ─────────────────────────────────
        var rules = await _ruleRepo.GetByFieldAsync(formId, fieldCode, tenantId, ct);
        if (rules.Count == 0 && failures.Count == 0)
            return ValidationResponse.Valid(fieldCode);

        // Cập nhật context với giá trị mới của field đang validate
        var evalContext = context.WithValue(fieldCode, value);
        var fieldLabel  = fieldMeta?.Label ?? fieldCode;

        failures.AddRange(EvaluateRules(rules, evalContext, resourceMap, fieldLabel));

        var isValid = !failures.Any(f => f.Severity.Equals("error", StringComparison.OrdinalIgnoreCase));
        return new ValidationResponse(fieldCode, isValid, failures);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, ValidationResponse>> ValidateFormAsync(
        int formId,
        EvaluationContext context,
        int tenantId,
        string langCode = "vi",
        IReadOnlyDictionary<string, string>? resourceMap = null,
        string formCode = "",
        CancellationToken ct = default)
    {
        // ── 1. Load TẤT CẢ fields để check Is_Required ────────────────────
        var allFields = await _fieldRepo.GetByFormIdAsync(formId, tenantId, langCode, ct);

        // ── 2. Load tất cả rules của form — 1 query thay vì N ─────────────
        var rulesByField = await _ruleRepo.GetByFormAsync(formId, tenantId, ct);

        // ── 3. Tập hợp tất cả fieldCode cần validate ──────────────────────
        // = field có Is_Required=true + field có Val_Rule
        var allFieldCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in allFields)
            allFieldCodes.Add(f.FieldCode);
        foreach (var code in rulesByField.Keys)
            allFieldCodes.Add(code);

        // ── 4. Topological sort dựa trên dependency graph ─────────────────
        var dependencies  = await _dependencyRepo.GetByFormAsync(formId, tenantId, ct);
        var sortedFields  = TopologicalSort(allFieldCodes, dependencies);

        // Build lookup nhanh: FieldCode → FieldMetadata
        var fieldMetaMap = allFields.ToDictionary(
            f => f.FieldCode, f => f, StringComparer.OrdinalIgnoreCase);

        var results = new Dictionary<string, ValidationResponse>(StringComparer.OrdinalIgnoreCase);

        foreach (var fieldCode in sortedFields)
        {
            var failures = new List<ValidationResult>();

            // Check Is_Required: field disabled (Is_Enabled=false) → bỏ qua required check
            if (fieldMetaMap.TryGetValue(fieldCode, out var meta)
                && meta.IsRequired
                && meta.IsEnabled
                && IsEmpty(context.GetValue(fieldCode)))
            {
                // Resolve thông báo lỗi qua ResourceResolver
                var requiredMsg = ResourceResolver.ResolveRequired(
                    resourceMap, formCode, fieldCode,
                    fieldLabel: meta.Label,
                    langCode);

                failures.Add(new ValidationResult(RuleId: 0, Severity: "error", Message: requiredMsg));
            }

            // Evaluate Val_Rules nếu có
            if (rulesByField.TryGetValue(fieldCode, out var rules))
            {
                var fieldLabel = fieldMetaMap.TryGetValue(fieldCode, out var fm) ? fm.Label : fieldCode;
                failures.AddRange(EvaluateRules(rules, context, resourceMap, fieldLabel));
            }

            if (failures.Count > 0)
            {
                var isValid = !failures.Any(f => f.Severity.Equals("error", StringComparison.OrdinalIgnoreCase));
                results[fieldCode] = new ValidationResponse(fieldCode, isValid, failures);
            }
        }

        return results;
    }

    // ── Required check helper ────────────────────────────────────

    /// <summary>
    /// Kiểm tra value rỗng: null, string rỗng/whitespace, collection rỗng.
    /// </summary>
    private static bool IsEmpty(object? value) => value switch
    {
        null => true,
        string s => string.IsNullOrWhiteSpace(s),
        System.Collections.ICollection col => col.Count == 0,
        _ => false
    };

    // ── Core evaluation logic ───────────────────────────────────

    /// <summary>
    /// Evaluate danh sách rules, trả về list kết quả fail.
    /// Rules pass → bỏ qua.
    /// </summary>
    private List<ValidationResult> EvaluateRules(
        IReadOnlyList<RuleMetadata> rules,
        EvaluationContext context,
        IReadOnlyDictionary<string, string>? resourceMap,
        string fieldLabel)
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
                // rule.ErrorMessage lưu Error_Key (VD: 'nhanvien.val.email.Regex')
                // ResourceResolver tra map để lấy text thực tế
                var message = ResourceResolver.ResolveRuleMessage(
                    resourceMap, rule.ErrorMessage, fieldLabel);

                failures.Add(new ValidationResult(rule.RuleId, rule.Severity, message));
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
        // Required rule — backward compat (ADR-011: deprecated, dùng Ui_Field.Is_Required thay thế)
        // Giữ lại để không crash các rule cũ trong DB chưa migrate
        if (rule.RuleType.Equals("Required", StringComparison.OrdinalIgnoreCase))
            return EvaluateRequired(rule.FieldCode, context);

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
        var adjList  = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in allFields)
        {
            inDegree[field] = 0;
            adjList[field]  = new List<string>();
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
