// File    : EventDefinition.cs
// Module  : Event
// Layer   : Domain
// Purpose : Metadata của một event handler — trigger condition + danh sách actions.

namespace ICare247.Domain.Entities.Event;

/// <summary>
/// Metadata của một event handler — maps từ bảng <c>Evt_Definition</c>.
/// EventEngine load event definitions theo (FormId, TriggerCode) rồi evaluate condition → execute actions.
/// </summary>
public sealed class EventDefinition
{
    /// <summary>Khóa chính — Evt_Definition.Event_Id.</summary>
    public int EventId { get; init; }

    /// <summary>Form chứa event này.</summary>
    public int FormId { get; init; }

    /// <summary>
    /// Field phát sinh event (Ui_Field.Field_Id).
    /// NULL nếu event áp dụng cho toàn form (FORM_LOAD, FORM_SUBMIT).
    /// </summary>
    public int? FieldId { get; init; }

    /// <summary>
    /// Field code tương ứng — resolve từ JOIN Ui_Field.
    /// NULL nếu event level form.
    /// </summary>
    public string? FieldCode { get; init; }

    /// <summary>
    /// Trigger code: 'OnChange' | 'OnBlur' | 'OnLoad' | 'OnSubmit' | 'OnSectionToggle'.
    /// </summary>
    public string TriggerCode { get; init; } = string.Empty;

    /// <summary>
    /// AST expression JSON — điều kiện để execute actions.
    /// NULL = luôn execute (không có điều kiện).
    /// </summary>
    public string? ConditionExpr { get; init; }

    /// <summary>Thứ tự execute khi nhiều events cùng trigger.</summary>
    public int OrderNo { get; init; }

    /// <summary>Danh sách actions — load kèm từ Evt_Action.</summary>
    public IReadOnlyList<EventAction> Actions { get; init; } = [];
}
