// File    : TabDetailRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO tab (Ui_Tab) cho FormEditor — đọc danh sách tab của form.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Bản ghi tab đọc từ <c>Ui_Tab</c>.
/// <see cref="SectionCount"/> = số section đã gán vào tab này (Ui_Section.Tab_Id).
/// </summary>
public sealed class TabDetailRecord
{
    public int     TabId       { get; init; }
    public string  TabCode     { get; init; } = "";
    public string? TitleKey    { get; init; }
    public string? IconKey     { get; init; }
    public int     OrderNo     { get; init; }
    public bool    IsDefault   { get; init; }
    public int     SectionCount { get; init; }
}
