// File    : LookupItemDto.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : DTO hiển thị preview lookup items trong FieldConfigView.

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Hiển thị một mục lookup trong panel xem trước (FieldConfig → Control Props).
/// </summary>
public sealed class LookupItemDto
{
    public string ItemCode { get; set; } = "";
    public string Label    { get; set; } = "";
}
