// File    : ConfirmDialogViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel dialog xác nhận dùng chung cho thao tác quan trọng.

using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.ViewModels;
using Prism.Commands;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>Dialog Prism trả về OK khi người dùng xác nhận.</summary>
public sealed class ConfirmDialogViewModel : ViewModelBase, IDialogAware
{
    private string _title = LookupTemplateUiText.ConfirmDeleteTitle;
    private string _message = "";
    private string _confirmText = LookupTemplateUiText.ConfirmDeleteButton;

    public string Title { get => _title; private set => SetProperty(ref _title, value); }
    public string Message { get => _message; private set => SetProperty(ref _message, value); }
    public string ConfirmText { get => _confirmText; private set => SetProperty(ref _confirmText, value); }
    public string CancelText => LookupTemplateUiText.Cancel;
    public DialogCloseListener RequestClose { get; set; }
    public DelegateCommand ConfirmCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public ConfirmDialogViewModel()
    {
        ConfirmCommand = new DelegateCommand(
            () => RequestClose.Invoke(new DialogResult(ButtonResult.OK)));
        CancelCommand = new DelegateCommand(
            () => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));
    }

    public bool CanCloseDialog() => true;
    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        Title = parameters.GetValue<string>("title") ?? LookupTemplateUiText.ConfirmDeleteTitle;
        Message = parameters.GetValue<string>("message") ?? "";
        ConfirmText = parameters.GetValue<string>("confirmText") ?? LookupTemplateUiText.ConfirmDeleteButton;
    }
}
