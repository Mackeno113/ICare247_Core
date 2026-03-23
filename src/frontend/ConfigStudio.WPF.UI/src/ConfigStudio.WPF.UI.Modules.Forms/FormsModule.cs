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
        containerRegistry.RegisterForNavigation<FormManagerView,    FormManagerViewModel>    (ViewNames.FormManager);
        containerRegistry.RegisterForNavigation<FormDetailView,     FormDetailViewModel>     (ViewNames.FormDetail);
        containerRegistry.RegisterForNavigation<FormEditorView,     FormEditorViewModel>     (ViewNames.FormEditor);
        containerRegistry.RegisterForNavigation<SysTableManagerView,  SysTableManagerViewModel>  (ViewNames.SysTableManager);
        containerRegistry.RegisterForNavigation<SysLookupManagerView, SysLookupManagerViewModel> (ViewNames.SysLookupManager);
        containerRegistry.RegisterForNavigation<FieldConfigView,    FieldConfigViewModel>    (ViewNames.FieldConfig);
        containerRegistry.RegisterForNavigation<PublishChecklistView,PublishChecklistViewModel>(ViewNames.PublishChecklist);

        // NOTE: Đăng ký dialog xác nhận vô hiệu hóa form (soft-delete)
        containerRegistry.RegisterDialog<DeactivateFormDialog, DeactivateFormDialogViewModel>(ViewNames.DeactivateFormDialog);

        // NOTE: Đăng ký dialog nhân bản form (clone với Form_Code mới)
        containerRegistry.RegisterDialog<CloneFormDialog, CloneFormDialogViewModel>(ViewNames.CloneFormDialog);

        // NOTE: Đăng ký dialog auto-generate fields từ Target DB schema
        containerRegistry.RegisterDialog<AutoGenerateFieldsDialog, AutoGenerateFieldsDialogViewModel>(ViewNames.AutoGenerateFieldsDialog);

        // NOTE: Đăng ký dialog đồng bộ schema — hiện diff giữa Target DB và form hiện tại
        containerRegistry.RegisterDialog<SyncSchemaDialog, SyncSchemaDialogViewModel>(ViewNames.SyncSchemaDialog);
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}
