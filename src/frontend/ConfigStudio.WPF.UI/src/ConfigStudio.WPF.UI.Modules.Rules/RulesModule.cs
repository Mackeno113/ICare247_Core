// File    : RulesModule.cs
// Module  : Rules
// Layer   : Presentation
// Purpose : Dang ky view Validation Rule Editor.

using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Modules.Rules.ViewModels;
using ConfigStudio.WPF.UI.Modules.Rules.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace ConfigStudio.WPF.UI.Modules.Rules;

public sealed class RulesModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<ValidationRuleEditorView, ValidationRuleEditorViewModel>(ViewNames.ValidationRuleEditor);
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}
