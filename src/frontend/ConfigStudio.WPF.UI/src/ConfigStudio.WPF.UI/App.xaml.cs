// File    : App.xaml.cs
// Module  : Bootstrap
// Layer   : Presentation
// Purpose : Khoi tao Prism application, shell va module catalog.

using System.Windows;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Modules.Events;
using ConfigStudio.WPF.UI.Modules.Forms;
using ConfigStudio.WPF.UI.Modules.Grammar;
using ConfigStudio.WPF.UI.Modules.I18n;
using ConfigStudio.WPF.UI.Modules.Rules;
using ConfigStudio.WPF.UI.Services;
using ConfigStudio.WPF.UI.ViewModels;
using ConfigStudio.WPF.UI.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Unity;

namespace ConfigStudio.WPF.UI;

public partial class App : PrismApplication
{
    protected override void ConfigureViewModelLocator()
    {
        base.ConfigureViewModelLocator();
        ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
    }

    protected override Window CreateShell()
        => Container.Resolve<MainWindow>();

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
        containerRegistry.RegisterForNavigation<DashboardView, DashboardViewModel>(ViewNames.Dashboard);
        containerRegistry.RegisterForNavigation<SettingsView, SettingsViewModel>(ViewNames.Settings);
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        moduleCatalog.AddModule<FormsModule>();
        moduleCatalog.AddModule<RulesModule>();
        moduleCatalog.AddModule<EventsModule>();
        moduleCatalog.AddModule<GrammarModule>();
        moduleCatalog.AddModule<I18nModule>();
    }
}
