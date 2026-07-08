// File    : FieldLookupConfigDto.cs
// Module  : ICare247.UI.DynamicForms
// Purpose : Cấu hình động của field lookup — mirror domain FieldLookupConfig. Dùng bởi ComboBox/LookupBox renderer.

namespace ICare247.UI.DynamicForms.Models;

/// <summary>
/// Cấu hình động của field lookup — mirror của domain <c>FieldLookupConfig</c>.
/// Được dùng bởi ComboBoxRenderer và LookupBoxRenderer để biết cách hiển thị.
/// </summary>
public sealed class FieldLookupConfigDto
{
    /// <summary>"table" | "tvf" | "custom_sql"</summary>
    public string  QueryMode      { get; set; } = "table";
    public string  SourceName     { get; set; } = "";
    public string  ValueColumn    { get; set; } = "";
    public string  DisplayColumn  { get; set; } = "";
    public string? FilterSql      { get; set; }
    public string? OrderBy        { get; set; }
    public bool    SearchEnabled  { get; set; } = true;
    public string? PopupColumnsJson { get; set; }

    // ── LookupBox (DxDropDownBox) props ──────────────────────────────────
    /// <summary>"TextOnly" | "CodeAndName" | "Custom"</summary>
    public string  EditBoxMode    { get; set; } = "TextOnly";
    public string? CodeField      { get; set; }
    public int     DropDownWidth  { get; set; } = 600;
    public int     DropDownHeight { get; set; } = 400;
    /// <summary>FieldCode của field trigger cascading reload (đơn).</summary>
    public string? ReloadTriggerField { get; set; }
    /// <summary>Danh sách FieldCode cha kích hoạt reload (Multi-Trigger). Renderer hợp với @param Filter SQL.</summary>
    public List<string> ReloadTriggerFields { get; set; } = [];
    /// <summary>TreeLookupBox: node được chọn — "all" | "leaf" | "branch". Null = "all".</summary>
    public string? TreeSelectableLevel { get; set; }
    /// <summary>Số ký tự tối thiểu để kích hoạt filter. 0 = filter ngay từ ký tự đầu tiên.</summary>
    public int FilterMinLength { get; set; } = 0;
    /// <summary>Cho phép mở dialog "thêm mới" entity ngay trên LookupBox.</summary>
    public bool AllowAddNew { get; set; }
    /// <summary>Form_Code của Ui_Form render dialog nhập liệu khi thêm mới.</summary>
    public string? AddFormCode { get; set; }
}
