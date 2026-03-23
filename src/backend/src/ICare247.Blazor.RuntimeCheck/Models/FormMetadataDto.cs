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
    public int     SortOrder        { get; set; }
}
