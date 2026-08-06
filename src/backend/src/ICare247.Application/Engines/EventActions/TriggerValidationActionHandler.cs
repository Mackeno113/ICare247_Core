// File    : TriggerValidationActionHandler.cs
// Module  : Engines/EventActions
// Layer   : Application
// Purpose : TRIGGER_VALIDATION — gọi ValidationEngine cho danh sách field, gói kết quả thành delta (AUDIT-5).

using ICare247.Application.Interfaces;
using ICare247.Domain.Engine;
using ICare247.Domain.Engine.Models;

namespace ICare247.Application.Engines.EventActions;

/// <summary>
/// TRIGGER_VALIDATION. Param JSON: { "targetFields": ["DateOfBirth", "Age"] }.
/// Resolve resource map (ADR-014) để message ra i18n thay vì raw Error_Key.
/// </summary>
public sealed class TriggerValidationActionHandler(
    IValidationEngine validationEngine,
    IConfigCache config) : IEventActionHandler
{
    public string ActionCode => "TRIGGER_VALIDATION";

    public async Task<IReadOnlyList<UiDelta>> ExecuteAsync(EventActionContext context, CancellationToken ct)
    {
        var param = EventActionParam.Parse(context.Action.ActionParamJson);

        var targetFields = EventActionParam.GetStringArray(param, "targetFields");
        if (targetFields is null || targetFields.Count == 0)
            return [];

        var formEvent = context.FormEvent;
        var deltas = new List<UiDelta>();

        var resourceMap = string.IsNullOrEmpty(formEvent.FormCode)
            ? null
            : await config.GetResourceMapAsync(formEvent.FormCode, formEvent.LangCode, formEvent.TenantId, ct);

        foreach (var fieldCode in targetFields)
        {
            var value = context.State.GetValue(fieldCode);
            var response = await validationEngine.ValidateFieldAsync(
                formEvent.FormId, fieldCode, value, context.State,
                formEvent.TenantId, langCode: formEvent.LangCode,
                resourceMap: resourceMap, formCode: formEvent.FormCode, ct: ct);

            var errors = response.Results
                .Select(r => new Dictionary<string, object?>
                {
                    ["ruleId"] = r.RuleId,
                    ["severity"] = r.Severity,
                    ["message"] = r.Message
                })
                .ToList();

            deltas.Add(new UiDelta(fieldCode, "TRIGGER_VALIDATION",
                new Dictionary<string, object?>
                {
                    ["isValid"] = response.IsValid,
                    ["errors"] = errors
                }));
        }

        return deltas;
    }
}
