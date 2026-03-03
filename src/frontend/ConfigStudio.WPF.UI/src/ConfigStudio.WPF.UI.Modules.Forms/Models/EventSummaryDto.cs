// File    : EventSummaryDto.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : DTO tóm tắt thông tin event gắn vào field, hiển thị trong tab Events.

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// DTO hiển thị tóm tắt event trong DataGrid tab "Events" của FieldConfig.
/// </summary>
public sealed class EventSummaryDto
{
    public int EventId { get; set; }
    public string TriggerCode { get; set; } = "";
    public string ConditionPreview { get; set; } = "";
    public int ActionsCount { get; set; }
    public bool IsActive { get; set; } = true;
}
