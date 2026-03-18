// File    : SectionDetailDto.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : DTO hiển thị thông tin section trong tab Sections của FormDetailView.

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// DTO hiển thị tóm tắt một section trong tab Sections của Form Detail.
/// Map từ <c>Ui_Section</c> bảng DB.
/// </summary>
public sealed class SectionDetailDto
{
    public int    SectionId   { get; set; }
    public int    OrderNo     { get; set; }
    public string SectionCode { get; set; } = "";
    public string TitleKey    { get; set; } = "";
    public string LayoutJson  { get; set; } = "";
    public int    FieldCount  { get; set; }
}
