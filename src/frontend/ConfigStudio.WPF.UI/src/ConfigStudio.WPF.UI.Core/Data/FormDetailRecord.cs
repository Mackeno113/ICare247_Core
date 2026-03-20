// File    : FormDetailRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO chi tiết form (header) cho FormDetailView.

namespace ConfigStudio.WPF.UI.Core.Data;

public sealed class FormDetailRecord
{
    public int FormId { get; init; }
    public string FormCode { get; init; } = "";
    public string TableName { get; init; } = "";
    public int TableId { get; init; }
    public string Platform { get; init; } = "web";
    public string LayoutEngine { get; init; } = "Grid";
    public int Version { get; init; }
    public string? Checksum { get; init; }
    public bool IsActive { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? Description { get; init; }
}
