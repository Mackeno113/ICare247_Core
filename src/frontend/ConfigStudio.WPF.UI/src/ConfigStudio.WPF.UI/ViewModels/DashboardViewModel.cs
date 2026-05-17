// File    : DashboardViewModel.cs
// Module  : Shell
// Layer   : Presentation
// Purpose : ViewModel placeholder cho Dashboard.

using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Services;
using ConfigStudio.WPF.UI.Core.ViewModels;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.ViewModels;

public sealed class DashboardViewModel : ViewModelBase, INavigationAware
{
    private readonly INavigationHistoryService? _history;

    public DashboardViewModel(INavigationHistoryService? history = null)
    {
        _history = history;
    }

    public string Title => "Dashboard";

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        _history?.RegisterNavigation(
            new NavigationCrumb { ViewName = ViewNames.Dashboard, Title = "Dashboard", Icon = "⌂" },
            isHierarchical: false);
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
