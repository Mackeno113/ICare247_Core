// File    : FormMetadataDto.cs
// Module  : RuntimeCheck
// Purpose : DTO nhận response từ GET /api/v1/config/forms/{code}.
//           Mirror của FormMetadata domain entity.

namespace ICare247.Blazor.RuntimeCheck.Models;

/// <summary>Metadata toàn bộ một form — nhận từ backend API.</summary>
public sealed class FormMetadataDto
{
    public int    FormId   { get; set; }
    public int    TenantId { get; set; }
    public string FormCode { get; set; } = "";
    public string FormName { get; set; } = "";
    public int    Version  { get; set; }
    public string Platform { get; set; } = "web";
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
    /// <summary>false = grayout, không tương tác, KHÔNG submit (ADR-010).</summary>
    public bool    IsEnabled        { get; set; } = true;
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
    /// <summary>FieldCode của field trigger cascading reload.</summary>
    public string? ReloadTriggerField { get; set; }
}
