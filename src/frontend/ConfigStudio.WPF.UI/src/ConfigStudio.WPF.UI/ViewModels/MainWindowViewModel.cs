// File    : MainWindowViewModel.cs
// Module  : Shell
// Layer   : Presentation
// Purpose : Adapter view model cho convention auto-wire cua Prism.

using ConfigStudio.WPF.UI.Services;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.ViewModels;

public sealed class MainWindowViewModel : ShellViewModel
{
    public MainWindowViewModel()
        : base(null, null)
    { }

    public MainWindowViewModel(IRegionManager regionManager, IThemeService themeService)
        : base(regionManager, themeService)
    { }
}
