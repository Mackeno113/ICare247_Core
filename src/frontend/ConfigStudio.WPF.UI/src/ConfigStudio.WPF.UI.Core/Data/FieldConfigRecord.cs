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
    public int OrderNo { get; init; }
    public string? ControlPropsJson { get; init; }
    public int Version { get; init; }
    public string? Description { get; init; }
}
