// File    : FormSummaryDto.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : DTO hiển thị tóm tắt form trong DataGrid của Form Manager.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// DTO hiển thị 1 form trong danh sách Form Manager.
/// Map từ <c>Ui_Form</c> bảng DB.
/// </summary>
public class FormSummaryDto : BindableBase
{
    public int FormId { get; set; }
    public string FormCode { get; set; } = "";
    public string FormName { get; set; } = "";
    public int Version { get; set; }
    public string Platform { get; set; } = "web";
    public string TableName { get; set; } = "";
    public int SectionCount { get; set; }
    public int FieldCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string UpdatedBy { get; set; } = "";

    /// <summary>Trạng thái hiển thị (Active / Inactive).</summary>
    public string StatusText => IsActive ? "Active" : "Inactive";
}
