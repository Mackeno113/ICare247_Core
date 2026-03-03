// File    : GrammarModule.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : Dang ky view Grammar Library.

using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Modules.Grammar.ViewModels;
using ConfigStudio.WPF.UI.Modules.Grammar.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace ConfigStudio.WPF.UI.Modules.Grammar;

public sealed class GrammarModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<GrammarLibraryView, GrammarLibraryViewModel>(ViewNames.GrammarLibrary);
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}
