// File    : EventItemRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO event từ Evt_Definition.

namespace ConfigStudio.WPF.UI.Core.Data;

public sealed class EventItemRecord
{
    public int EventId { get; init; }
    public int FormId { get; init; }
    public int? FieldId { get; init; }
    public string TriggerCode { get; init; } = "";
    public string? ConditionExpr { get; init; }
    public int OrderNo { get; init; }
    public bool IsActive { get; init; } = true;
    public int ActionsCount { get; init; }
}

public sealed class EventSummaryRecord
{
    public int EventId { get; init; }
    public int OrderNo { get; init; }
    public string TriggerCode { get; init; } = "";
    public string FieldTarget { get; init; } = "";
    public string? ConditionPreview { get; init; }
    public int ActionsCount { get; init; }
    public bool IsActive { get; init; }
}

public sealed class ActionItemRecord
{
    public int ActionId { get; init; }
    public int EventId { get; init; }
    public string ActionCode { get; init; } = "";
    public string? ParamJson { get; init; }
    public int OrderNo { get; init; }
}

public sealed class ActionTypeRecord
{
    public string ActionCode { get; init; } = "";
    public string? ParamSchema { get; init; }
}
