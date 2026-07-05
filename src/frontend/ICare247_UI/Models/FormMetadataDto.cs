// File    : FormMetadataDto.cs
// Module  : ICare247_UI
// Purpose : DTO nhận response từ GET /api/v1/config/forms/{code}.
//           Mirror của FormMetadata domain entity.

namespace ICare247_UI.Models;

/// <summary>Metadata toàn bộ một form — nhận từ backend API.</summary>
public sealed class FormMetadataDto
{
    public int    FormId   { get; set; }
    public int    TenantId { get; set; }
    public string FormCode { get; set; } = "";
    public string FormName { get; set; } = "";
    public int    Version  { get; set; }
    public string Platform { get; set; } = "web";
    /// <summary>Bề rộng tối đa form (px). null = mặc định 880.</summary>
    public int?   MaxWidth { get; set; }
    /// <summary>Số cột lưới nền (1..4). null = mặc định 4. =1 → mỗi field 1 dòng.</summary>
    public int?   Columns  { get; set; }
    public List<SectionMetadataDto> Sections { get; set; } = [];
    public List<FieldMetadataDto>   Fields   { get; set; } = [];
}

/// <summary>Metadata một section trong form.</summary>
public sealed class SectionMetadataDto
{
    public int    SectionId   { get; set; }
    public string SectionCode { get; set; } = "";
    public string SectionName { get; set; } = "";
    public int    SortOrder   { get; set; }
    public List<FieldMetadataDto> Fields { get; set; } = [];
}

/// <summary>Metadata một field — dùng để render control tương ứng.</summary>
public sealed class FieldMetadataDto
{
    public int     FieldId          { get; set; }
    public string  FieldCode        { get; set; } = "";
    /// <summary>
    /// Kiểu editor từ DB: 'TextBox', 'TextArea', 'NumberEdit', 'DateEdit',
    /// 'DateTimeEdit', 'CheckBox', 'ComboBox', 'LookupEdit',...
    /// FormRunner normalize về lowercase trước khi truyền vào FieldRenderer.
    /// </summary>
    public string  FieldType        { get; set; } = "TextBox";
    public string  Label            { get; set; } = "";
    /// <summary>Cấu hình UI dạng JSON — không phải default value.</summary>
    public string? ControlPropsJson { get; set; }
    public string? DefaultValueJson { get; set; }
    public bool    IsVisible        { get; set; } = true;
    public bool    IsReadOnly       { get; set; }
    /// <summary>Luôn false từ metadata; Val_Rule quyết định required runtime.</summary>
    public bool    IsRequired       { get; set; }
    /// <summary>true = khóa khi FormMode=Edit (ADR-017). EffectiveReadOnly = IsReadOnly OR (LockOnEdit AND IsEditMode).</summary>
    public bool    LockOnEdit       { get; set; }
    /// <summary>true = field UI-only, không map tới cột DB. Save layer bỏ qua field này.</summary>
    public bool    IsVirtual        { get; set; }
    public int     SortOrder        { get; set; }
    /// <summary>Độ rộng grid 4-col: 1 = 1/4, 2 = 2/4(half), 3 = 3/4, 4 = full.</summary>
    public byte    ColSpan          { get; set; } = 1;
    /// <summary>null | "static" (Sys_Lookup) | "dynamic" (Ui_Field_Lookup)</summary>
    public string? LookupSource     { get; set; }
    /// <summary>Mã lookup trong Sys_Lookup — chỉ có giá trị khi LookupSource = "static".</summary>
    public string? LookupCode       { get; set; }
    /// <summary>
    /// Cấu hình FK lookup động — chỉ có giá trị khi LookupSource = "dynamic".
    /// Được backend serialize từ <c>FieldLookupConfig</c> domain entity.
    /// </summary>
    public FieldLookupConfigDto? LookupConfig { get; set; }
}

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
    /// <summary>Số ký tự tối thiểu để kích hoạt filter. 0 = filter ngay từ ký tự đầu tiên.</summary>
    public int FilterMinLength { get; set; } = 0;
    /// <summary>Cho phép mở dialog "thêm mới" entity ngay trên LookupBox.</summary>
    public bool AllowAddNew { get; set; }
    /// <summary>Form_Code của Ui_Form render dialog nhập liệu khi thêm mới.</summary>
    public string? AddFormCode { get; set; }
}
