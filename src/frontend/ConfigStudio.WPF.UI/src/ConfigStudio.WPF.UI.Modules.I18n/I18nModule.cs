// File    : I18nModule.cs
// Module  : I18n
// Layer   : Presentation
// Purpose : Dang ky view i18n Manager.

using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Modules.I18n.ViewModels;
using ConfigStudio.WPF.UI.Modules.I18n.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace ConfigStudio.WPF.UI.Modules.I18n;

public sealed class I18nModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<I18nManagerView, I18nManagerViewModel>(ViewNames.I18nManager);
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}
