// File    : SidebarEntry.cs
// Module  : Shell
// Layer   : Presentation
// Purpose : Model hiển thị cho mỗi item trong custom sidebar — header, divider, nav item.

namespace ConfigStudio.WPF.UI.Models;

public sealed class SidebarEntry
{
    public bool IsHeader  { get; init; }
    public bool IsDivider { get; init; }
    public string Title   { get; init; } = string.Empty;
    public string Icon    { get; init; } = string.Empty;

    /// <summary>NavigationItem gốc — null nếu là header hoặc divider.</summary>
    public NavigationItem? NavItem { get; init; }
}
