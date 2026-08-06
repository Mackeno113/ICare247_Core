// File    : ClearValueActionHandler.cs
// Module  : Engines/EventActions
// Layer   : Application
// Purpose : CLEAR_VALUE — đặt giá trị field về null (không cần điều kiện) (AUDIT-5).

using ICare247.Domain.Engine.Models;

namespace ICare247.Application.Engines.EventActions;

/// <summary>CLEAR_VALUE. Param JSON: { "targetField": "District" }.</summary>
public sealed class ClearValueActionHandler : IEventActionHandler
{
    public string ActionCode => "CLEAR_VALUE";

    public Task<IReadOnlyList<UiDelta>> ExecuteAsync(EventActionContext context, CancellationToken ct)
    {
        var param = EventActionParam.Parse(context.Action.ActionParamJson);

        var targetField = EventActionParam.GetString(param, "targetField");
        if (targetField is null)
            return Task.FromResult<IReadOnlyList<UiDelta>>([]);

        IReadOnlyList<UiDelta> deltas =
        [
            new UiDelta(targetField, "CLEAR_VALUE",
                new Dictionary<string, object?> { ["value"] = null })
        ];
        return Task.FromResult(deltas);
    }
}
