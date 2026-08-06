// File    : SetRequiredActionHandler.cs
// Module  : Engines/EventActions
// Layer   : Application
// Purpose : SET_REQUIRED — toggle bắt buộc theo conditionExpression (AUDIT-5).

using ICare247.Domain.Engine;

namespace ICare247.Application.Engines.EventActions;

/// <summary>SET_REQUIRED: delta { "required": bool } theo kết quả điều kiện.</summary>
public sealed class SetRequiredActionHandler(IAstEngine astEngine) : ConditionToggleActionHandler(astEngine)
{
    public override string ActionCode => "SET_REQUIRED";
    protected override string DataKey => "required";
}
