// File    : SetValueActionHandler.cs
// Module  : Engines/EventActions
// Layer   : Application
// Purpose : SET_VALUE — evaluate valueExpression (AST) → set giá trị mới cho targetField (AUDIT-5).

using ICare247.Domain.Engine;
using ICare247.Domain.Engine.Models;

namespace ICare247.Application.Engines.EventActions;

/// <summary>
/// SET_VALUE. Param JSON: { "targetField": "Total", "valueExpression": {...AST JSON...} }.
/// </summary>
public sealed class SetValueActionHandler(IAstEngine astEngine) : IEventActionHandler
{
    public string ActionCode => "SET_VALUE";

    public Task<IReadOnlyList<UiDelta>> ExecuteAsync(EventActionContext context, CancellationToken ct)
    {
        var param = EventActionParam.Parse(context.Action.ActionParamJson);

        var targetField = EventActionParam.GetString(param, "targetField");
        if (targetField is null)
            return Task.FromResult<IReadOnlyList<UiDelta>>([]);

        var valueExpr = EventActionParam.GetElement(param, "valueExpression");
        if (valueExpr is null)
            return Task.FromResult<IReadOnlyList<UiDelta>>([]);

        var newValue = astEngine.Evaluate(valueExpr.Value.GetRawText(), context.State);

        IReadOnlyList<UiDelta> deltas =
        [
            new UiDelta(targetField, "SET_VALUE",
                new Dictionary<string, object?> { ["value"] = newValue })
        ];
        return Task.FromResult(deltas);
    }
}
