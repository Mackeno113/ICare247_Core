// File    : SetEnabledActionHandler.cs
// Module  : Engines/EventActions
// Layer   : Application
// Purpose : SET_ENABLED — bật/tắt enabled theo conditionExpression (false = grayout, ADR-012) (AUDIT-5).

using ICare247.Domain.Engine;

namespace ICare247.Application.Engines.EventActions;

/// <summary>SET_ENABLED: delta { "enabled": bool } theo kết quả điều kiện.</summary>
public sealed class SetEnabledActionHandler(IAstEngine astEngine) : ConditionToggleActionHandler(astEngine)
{
    public override string ActionCode => "SET_ENABLED";
    protected override string DataKey => "enabled";
}
