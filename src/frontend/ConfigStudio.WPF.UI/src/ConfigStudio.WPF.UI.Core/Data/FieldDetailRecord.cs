// File    : FieldDetailRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO field summary cho FormDetailView.

namespace ConfigStudio.WPF.UI.Core.Data;

public sealed class FieldDetailRecord
{
    public int FieldId { get; init; }
    public int OrderNo { get; init; }
    public string ColumnCode { get; init; } = "";
    public string SectionCode { get; init; } = "";
    public string EditorType { get; init; } = "";
    public string LabelKey { get; init; } = "";
    public bool IsVisible { get; init; }
    public bool IsReadOnly { get; init; }
    /// <summary>Field ảo — không map cột DB (Is_Virtual). Column_Id = NULL.</summary>
    public bool IsVirtual { get; init; }
    public int RuleCount { get; init; }
}
