// File    : ImpactPreviewService.cs
// Module  : Core
// Layer   : Shared
// Purpose : Implementation phân tích impact — scan rules/events tham chiếu field code.

using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Core.Services;

/// <summary>
/// ImpactPreviewService implementation.
/// Phân tích bằng cách scan Expression_Json/Condition_Expr tìm field references.
/// </summary>
public sealed class ImpactPreviewService : IImpactPreviewService
{
    private readonly IRuleDataService? _ruleService;
    private readonly IEventDataService? _eventService;

    public ImpactPreviewService(
        IRuleDataService? ruleService = null,
        IEventDataService? eventService = null)
    {
        _ruleService = ruleService;
        _eventService = eventService;
    }

    /// <inheritdoc />
    public async Task<ImpactAnalysis> AnalyzeFieldImpactAsync(
        string fieldCode, int formId, int tenantId,
        CancellationToken ct = default)
    {
        var affectedRules = new List<ImpactItem>();
        var affectedEvents = new List<ImpactItem>();
        var affectedFields = new List<ImpactItem>();

        // ── Scan rules tham chiếu field này ─────────────────────
        if (_ruleService is not null)
        {
            try
            {
                // Lấy tất cả rules của form → scan expression chứa fieldCode
                // NOTE: Dùng simple string search vì parse AST cần backend engine
                var rules = await _ruleService.GetRulesByFieldAsync(formId, ct);
                foreach (var rule in rules)
                {
                    var exprContains = ContainsFieldReference(rule.ExpressionJson, fieldCode);

                    if (exprContains)
                    {
                        affectedRules.Add(new ImpactItem(
                            rule.RuleId.ToString(),
                            $"Rule #{rule.RuleId} ({rule.RuleTypeCode})",
                            "rule",
                            "Expression tham chiếu field"));
                    }
                }
            }
            catch
            {
                // Không crash khi service fail
            }
        }

        // ── Scan events tham chiếu field này ────────────────────
        if (_eventService is not null)
        {
            try
            {
                var events = await _eventService.GetEventsByFieldAsync(formId, ct);
                foreach (var evt in events)
                {
                    var condContains = ContainsFieldReference(evt.ConditionExpr, fieldCode);

                    if (condContains)
                    {
                        affectedEvents.Add(new ImpactItem(
                            evt.EventId.ToString(),
                            $"Event #{evt.EventId} ({evt.TriggerCode})",
                            "event",
                            "Condition tham chiếu field"));
                    }
                }
            }
            catch
            {
                // Không crash khi service fail
            }
        }

        return new ImpactAnalysis
        {
            AffectedFields = affectedFields,
            AffectedRules = affectedRules,
            AffectedEvents = affectedEvents
        };
    }

    /// <inheritdoc />
    public Task<ImpactAnalysis> AnalyzeExpressionImpactAsync(
        string? expressionJson, int formId, int tenantId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(expressionJson))
            return Task.FromResult(ImpactAnalysis.Empty);

        // Extract field names from expression JSON
        // Tìm pattern: "name": "FieldCode" trong identifier nodes
        var referencedFields = ExtractFieldReferences(expressionJson);
        var affectedFields = referencedFields
            .Select(f => new ImpactItem(f, f, "field", "Tham chiếu trong expression"))
            .ToList();

        return Task.FromResult(new ImpactAnalysis
        {
            AffectedFields = affectedFields
        });
    }

    // ── Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Kiểm tra expression JSON có chứa tham chiếu đến fieldCode không.
    /// Simple string search — tìm "name":"fieldCode" pattern.
    /// </summary>
    private static bool ContainsFieldReference(string? json, string fieldCode)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;

        // Tìm pattern identifier node: "name": "fieldCode"
        return json.Contains($"\"{fieldCode}\"", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extract danh sách field codes từ expression JSON.
    /// Tìm pattern: "type":"identifier","name":"FieldCode"
    /// </summary>
    private static IReadOnlyList<string> ExtractFieldReferences(string json)
    {
        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Simple regex-like scan cho identifier nodes
        // Pattern: "type":"identifier" ... "name":"VALUE"
        var searchPos = 0;
        while (searchPos < json.Length)
        {
            var typeIdx = json.IndexOf("\"identifier\"", searchPos, StringComparison.OrdinalIgnoreCase);
            if (typeIdx < 0) break;

            var nameIdx = json.IndexOf("\"name\"", typeIdx, StringComparison.OrdinalIgnoreCase);
            if (nameIdx < 0) break;

            // Tìm giá trị sau "name": "..."
            var colonIdx = json.IndexOf(':', nameIdx + 6);
            if (colonIdx < 0) break;

            var quoteStart = json.IndexOf('"', colonIdx + 1);
            if (quoteStart < 0) break;

            var quoteEnd = json.IndexOf('"', quoteStart + 1);
            if (quoteEnd < 0) break;

            var fieldName = json[(quoteStart + 1)..quoteEnd];
            if (!string.IsNullOrWhiteSpace(fieldName))
                fields.Add(fieldName);

            searchPos = quoteEnd + 1;
        }

        return fields.ToList();
    }
}
