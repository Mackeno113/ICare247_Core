// File    : FormsModule.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Dang ky view Forms cho navigation.

using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Modules.Forms.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace ConfigStudio.WPF.UI.Modules.Forms;

public sealed class FormsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<FormManagerView, FormManagerViewModel>(ViewNames.FormManager);
        containerRegistry.RegisterForNavigation<FormEditorView, FormEditorViewModel>(ViewNames.FormEditor);
        containerRegistry.RegisterForNavigation<SysTableManagerView, SysTableManagerViewModel>(ViewNames.SysTableManager);
        containerRegistry.RegisterForNavigation<FieldConfigView, FieldConfigViewModel>(ViewNames.FieldConfig);
        containerRegistry.RegisterForNavigation<PublishChecklistView, PublishChecklistViewModel>(ViewNames.PublishChecklist);
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}
