// File    : ConditionToggleActionHandler.cs
// Module  : Engines/EventActions
// Layer   : Application
// Purpose : Base cho 4 action bật/tắt theo điều kiện (SET_VISIBLE/REQUIRED/READONLY/ENABLED) — cùng
//           pattern: evaluate conditionExpression → delta { <DataKey> = bool } (AUDIT-5, gỡ trùng lặp).

using ICare247.Domain.Engine;
using ICare247.Domain.Engine.Models;

namespace ICare247.Application.Engines.EventActions;

/// <summary>
/// Khung xử lý action toggle theo điều kiện: parse targetField + conditionExpression, evaluate AST,
/// sinh 1 delta gắn kết quả bool vào <see cref="DataKey"/>. Lớp con chỉ khai <see cref="IEventActionHandler.ActionCode"/>
/// + <see cref="DataKey"/>.
/// </summary>
public abstract class ConditionToggleActionHandler : IEventActionHandler
{
    private readonly IAstEngine _astEngine;

    protected ConditionToggleActionHandler(IAstEngine astEngine) => _astEngine = astEngine;

    /// <inheritdoc />
    public abstract string ActionCode { get; }

    /// <summary>Khóa dữ liệu trong delta chứa kết quả điều kiện (VD "visible", "required").</summary>
    protected abstract string DataKey { get; }

    /// <inheritdoc />
    public Task<IReadOnlyList<UiDelta>> ExecuteAsync(EventActionContext context, CancellationToken ct)
    {
        var param = EventActionParam.Parse(context.Action.ActionParamJson);

        var targetField = EventActionParam.GetString(param, "targetField");
        if (targetField is null)
            return Task.FromResult<IReadOnlyList<UiDelta>>([]);

        var condExpr = EventActionParam.GetElement(param, "conditionExpression");
        if (condExpr is null)
            return Task.FromResult<IReadOnlyList<UiDelta>>([]);

        var result = _astEngine.Evaluate(condExpr.Value.GetRawText(), context.State);
        var boolResult = BuiltinFunctions.ToBool(result) ?? false;

        IReadOnlyList<UiDelta> deltas =
        [
            new UiDelta(targetField, ActionCode,
                new Dictionary<string, object?> { [DataKey] = boolResult })
        ];
        return Task.FromResult(deltas);
    }
}
