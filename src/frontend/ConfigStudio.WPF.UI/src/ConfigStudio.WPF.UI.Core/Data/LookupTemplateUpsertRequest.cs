// File    : LookupTemplateUpsertRequest.cs
// Module  : Data
// Layer   : Core
// Purpose : Payload tạo mới hoặc cập nhật một mẫu lookup dùng lại.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>Yêu cầu ghi một dòng <c>Ui_Lookup_Template</c>.</summary>
public sealed class LookupTemplateUpsertRequest
{
    public int? TemplateId { get; set; }
    public string TemplateCode { get; set; } = "";
    public string Ten { get; set; } = "";
    public string? MoTa { get; set; }
    public string QueryMode { get; set; } = "table";
    public string SourceName { get; set; } = "";
    public string ValueColumn { get; set; } = "";
    public string DisplayColumn { get; set; } = "";
    public string? CodeField { get; set; }
    public string? FilterSql { get; set; }
    public string? OrderBy { get; set; }
    public string? PopupColumnsJson { get; set; }
    public string? ParentColumn { get; set; }
    public string? CanonicalParams { get; set; }
    public bool IsActive { get; set; } = true;
}
