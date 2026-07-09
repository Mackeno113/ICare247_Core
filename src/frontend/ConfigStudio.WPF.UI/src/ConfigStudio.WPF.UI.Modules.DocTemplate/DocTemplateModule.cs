// File    : DocTemplateModule.cs
// Module  : DocTemplate
// Layer   : Presentation
// Purpose : Đăng ký view soạn mẫu tài liệu (RichEdit) cho navigation. Spec 28 §8.2 (GĐ3).

using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Modules.DocTemplate.ViewModels;
using ConfigStudio.WPF.UI.Modules.DocTemplate.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace ConfigStudio.WPF.UI.Modules.DocTemplate;

/// <summary>Module soạn template Word/PDF (GĐ3). Slice (a): màn soạn fragment + panel biến.</summary>
public sealed class DocTemplateModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<DocTemplateEditorView, DocTemplateEditorViewModel>(
            ViewNames.DocTemplateEditor);
    }

    public void OnInitialized(IContainerProvider containerProvider) { }
}
