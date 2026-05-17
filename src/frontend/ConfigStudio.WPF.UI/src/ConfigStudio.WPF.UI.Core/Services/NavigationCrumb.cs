// File    : NavigationCrumb.cs
// Module  : Core
// Layer   : Shared
// Purpose : POCO mo ta 1 muc tren breadcrumb bar.

using Prism.Navigation;

namespace ConfigStudio.WPF.UI.Core.Services;

public sealed class NavigationCrumb
{
    public string ViewName { get; init; } = "";
    public string Title    { get; init; } = "";
    public string Icon     { get; init; } = "";
    public INavigationParameters? Parameters { get; init; }
}
