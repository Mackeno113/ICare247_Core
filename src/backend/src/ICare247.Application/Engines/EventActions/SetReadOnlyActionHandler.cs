// File    : SetReadOnlyActionHandler.cs
// Module  : Engines/EventActions
// Layer   : Application
// Purpose : SET_READONLY — toggle chỉ-đọc theo conditionExpression (AUDIT-5).

using ICare247.Domain.Engine;

namespace ICare247.Application.Engines.EventActions;

/// <summary>SET_READONLY: delta { "readOnly": bool } theo kết quả điều kiện.</summary>
public sealed class SetReadOnlyActionHandler(IAstEngine astEngine) : ConditionToggleActionHandler(astEngine)
{
    public override string ActionCode => "SET_READONLY";
    protected override string DataKey => "readOnly";
}
