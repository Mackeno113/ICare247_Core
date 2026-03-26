// File    : FieldConfigRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO chi tiết field cho FieldConfigView (edit mode).

namespace ConfigStudio.WPF.UI.Core.Data;

public sealed class FieldConfigRecord
{
    public int FieldId { get; init; }
    public int FormId { get; init; }
    public int? SectionId { get; init; }
    public int ColumnId { get; init; }
    public string ColumnCode { get; init; } = "";
    public string SectionCode { get; init; } = "";
    public string EditorType { get; init; } = "TextBox";
    public string LabelKey { get; init; } = "";
    public string? PlaceholderKey { get; init; }
    public string? TooltipKey { get; init; }
    public bool IsVisible { get; init; } = true;
    public bool IsReadOnly { get; init; }
    /// <summary>Không cho phép để trống khi submit. Lưu vào Ui_Field.Is_Required.</summary>
    public bool IsRequired { get; init; }
    /// <summary>Field có được tương tác không. False = grayout + không submit. Lưu vào Ui_Field.Is_Enabled.</summary>
    public bool IsEnabled { get; init; } = true;
    public int OrderNo { get; init; }
    public string? ControlPropsJson { get; init; }
    /// <summary>Độ rộng grid: 1 = 1/3, 2 = 2/3, 3 = full width.</summary>
    public byte ColSpan { get; init; } = 1;

    /// <summary>null = thường | "static" = Sys_Lookup | "dynamic" = Ui_Field_Lookup</summary>
    public string? LookupSource { get; init; }

    /// <summary>Lookup code trong Sys_Lookup. Chỉ có giá trị khi LookupSource = "static".</summary>
    public string? LookupCode { get; init; }

    public int Version { get; init; }
    public string? Description { get; init; }
}
