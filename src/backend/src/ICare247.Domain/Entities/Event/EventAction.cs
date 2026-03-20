// File    : EventAction.cs
// Module  : Event
// Layer   : Domain
// Purpose : Metadata của một action trong event — maps từ bảng Evt_Action.

namespace ICare247.Domain.Entities.Event;

/// <summary>
/// Một action trong event handler — maps từ bảng <c>Evt_Action</c>.
/// Mỗi <see cref="EventDefinition"/> có thể chứa nhiều actions, execute theo <see cref="OrderNo"/>.
/// </summary>
public sealed class EventAction
{
    /// <summary>Khóa chính — Evt_Action.Action_Id.</summary>
    public int ActionId { get; init; }

    /// <summary>Event chứa action này.</summary>
    public int EventId { get; init; }

    /// <summary>
    /// Loại action: 'SET_VALUE' | 'SET_VISIBLE' | 'SET_REQUIRED' |
    /// 'SET_READONLY' | 'RELOAD_OPTIONS' | 'TRIGGER_VALIDATION'.
    /// </summary>
    public string ActionCode { get; init; } = string.Empty;

    /// <summary>
    /// JSON params tùy theo loại action.
    /// Ví dụ SET_VALUE: <c>{"targetField":"Total","valueExpression":{...}}</c>.
    /// </summary>
    public string? ActionParamJson { get; init; }

    /// <summary>Thứ tự execute trong event.</summary>
    public int OrderNo { get; init; }
}
