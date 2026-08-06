// File    : SetVisibleActionHandler.cs
// Module  : Engines/EventActions
// Layer   : Application
// Purpose : SET_VISIBLE — show/hide field theo conditionExpression (AUDIT-5).

using ICare247.Domain.Engine;

namespace ICare247.Application.Engines.EventActions;

/// <summary>SET_VISIBLE: delta { "visible": bool } theo kết quả điều kiện.</summary>
public sealed class SetVisibleActionHandler(IAstEngine astEngine) : ConditionToggleActionHandler(astEngine)
{
    public override string ActionCode => "SET_VISIBLE";
    protected override string DataKey => "visible";
}
