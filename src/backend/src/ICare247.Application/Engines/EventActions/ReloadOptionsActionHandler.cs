// File    : ReloadOptionsActionHandler.cs
// Module  : Engines/EventActions
// Layer   : Application
// Purpose : RELOAD_OPTIONS — delta yêu cầu client reload dropdown; resolve placeholder trong apiEndpoint (AUDIT-5).

using ICare247.Domain.Engine.Models;

namespace ICare247.Application.Engines.EventActions;

/// <summary>
/// RELOAD_OPTIONS. Param JSON: { "targetField", "apiEndpoint", "dependsOn"? }.
/// Server chỉ gửi delta — client tự gọi endpoint (đã resolve {Field} → giá trị) để fetch options.
/// </summary>
public sealed class ReloadOptionsActionHandler : IEventActionHandler
{
    public string ActionCode => "RELOAD_OPTIONS";

    public Task<IReadOnlyList<UiDelta>> ExecuteAsync(EventActionContext context, CancellationToken ct)
    {
        var param = EventActionParam.Parse(context.Action.ActionParamJson);

        var targetField = EventActionParam.GetString(param, "targetField");
        var apiEndpoint = EventActionParam.GetString(param, "apiEndpoint");
        if (targetField is null || apiEndpoint is null)
            return Task.FromResult<IReadOnlyList<UiDelta>>([]);

        var resolvedEndpoint = EventActionParam.ResolvePlaceholders(apiEndpoint, context.State);
        var dependsOn = EventActionParam.GetStringArray(param, "dependsOn");

        var data = new Dictionary<string, object?>
        {
            ["apiEndpoint"] = resolvedEndpoint,
            ["dependsOn"] = dependsOn
        };

        IReadOnlyList<UiDelta> deltas = [new UiDelta(targetField, "RELOAD_OPTIONS", data)];
        return Task.FromResult(deltas);
    }
}
