// File    : FieldDetailDto.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : DTO hiển thị thông tin field trong tab Fields của FormDetailView.

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// DTO hiển thị tóm tắt một field trong tab Fields của Form Detail.
/// Map từ <c>Ui_Field</c> JOIN <c>Sys_Column</c> bảng DB.
/// </summary>
public sealed class FieldDetailDto
{
    public int    FieldId     { get; set; }
    public int    OrderNo     { get; set; }
    public string ColumnName  { get; set; } = "";
    public string SectionCode { get; set; } = "";
    public string EditorType  { get; set; } = "";
    public bool   IsVisible   { get; set; } = true;
    public bool   IsReadOnly  { get; set; }
    public int    RuleCount   { get; set; }
}
