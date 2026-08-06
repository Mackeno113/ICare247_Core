// File    : ShowMessageActionHandler.cs
// Module  : Engines/EventActions
// Layer   : Application
// Purpose : SHOW_MESSAGE — thông báo inline tại field (không block submit); conditionExpression tùy chọn (AUDIT-5).

using ICare247.Domain.Engine;
using ICare247.Domain.Engine.Models;

namespace ICare247.Application.Engines.EventActions;

/// <summary>
/// SHOW_MESSAGE. Param JSON: { "targetField", "messageKey", "severity"?, "conditionExpression"? }.
/// conditionExpression có thì evaluate trước; false → không tạo delta.
/// </summary>
public sealed class ShowMessageActionHandler(IAstEngine astEngine) : IEventActionHandler
{
    public string ActionCode => "SHOW_MESSAGE";

    public Task<IReadOnlyList<UiDelta>> ExecuteAsync(EventActionContext context, CancellationToken ct)
    {
        var param = EventActionParam.Parse(context.Action.ActionParamJson);

        var targetField = EventActionParam.GetString(param, "targetField");
        var messageKey  = EventActionParam.GetString(param, "messageKey");
        var severity    = EventActionParam.GetString(param, "severity") ?? "Info";

        if (targetField is null || messageKey is null)
            return Task.FromResult<IReadOnlyList<UiDelta>>([]);

        // conditionExpression optional — nếu có thì evaluate; false → không tạo delta.
        var condExpr = EventActionParam.GetElement(param, "conditionExpression");
        if (condExpr is not null)
        {
            var result = astEngine.Evaluate(condExpr.Value.GetRawText(), context.State);
            if (!(BuiltinFunctions.ToBool(result) ?? false))
                return Task.FromResult<IReadOnlyList<UiDelta>>([]);
        }

        IReadOnlyList<UiDelta> deltas =
        [
            new UiDelta(targetField, "SHOW_MESSAGE",
                new Dictionary<string, object?>
                {
                    ["messageKey"] = messageKey,
                    ["severity"]   = severity
                })
        ];
        return Task.FromResult(deltas);
    }
}
