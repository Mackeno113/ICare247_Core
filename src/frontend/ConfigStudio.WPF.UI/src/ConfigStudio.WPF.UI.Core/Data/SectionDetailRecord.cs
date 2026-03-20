// File    : SectionDetailRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO section cho FormDetailView.

namespace ConfigStudio.WPF.UI.Core.Data;

public sealed class SectionDetailRecord
{
    public int SectionId { get; init; }
    public string SectionCode { get; init; } = "";
    public string? TitleKey { get; init; }
    public int OrderNo { get; init; }
    public string? LayoutJson { get; init; }
    public int FieldCount { get; init; }
}
