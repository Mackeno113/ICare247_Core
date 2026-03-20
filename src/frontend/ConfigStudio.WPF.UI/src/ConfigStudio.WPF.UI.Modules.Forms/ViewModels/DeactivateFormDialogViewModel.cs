// File    : DeactivateFormDialogViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho dialog xác nhận vô hiệu hóa form (soft-delete Is_Active=false).

using ConfigStudio.WPF.UI.Core.ViewModels;
using Prism.Commands;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// ViewModel cho DeactivateFormDialog.
/// Hiển thị thông tin impact trước khi user xác nhận vô hiệu hóa form.
/// Nhận tham số qua IDialogParameters: FormCode, FormName, FieldCount, SectionCount, EventCount.
/// Trả về ButtonResult.OK nếu user xác nhận, Cancel nếu hủy.
/// </summary>
public sealed class DeactivateFormDialogViewModel : ViewModelBase, IDialogAware
{
    // ── IDialogAware ─────────────────────────────────────────
    public string Title => "Vô hiệu hóa Form";
    public DialogCloseListener RequestClose { get; set; }

    // ── Form info (nhận từ caller) ────────────────────────────
    private string _formCode = "";
    public string FormCode
    {
        get => _formCode;
        private set => SetProperty(ref _formCode, value);
    }

    private string _formName = "";
    public string FormName
    {
        get => _formName;
        private set => SetProperty(ref _formName, value);
    }

    // ── Impact counts ─────────────────────────────────────────
    private int _sectionCount;
    public int SectionCount
    {
        get => _sectionCount;
        private set
        {
            if (SetProperty(ref _sectionCount, value))
                RaisePropertyChanged(nameof(ImpactSummary));
        }
    }

    private int _fieldCount;
    public int FieldCount
    {
        get => _fieldCount;
        private set
        {
            if (SetProperty(ref _fieldCount, value))
                RaisePropertyChanged(nameof(ImpactSummary));
        }
    }

    private int _eventCount;
    public int EventCount
    {
        get => _eventCount;
        private set
        {
            if (SetProperty(ref _eventCount, value))
                RaisePropertyChanged(nameof(ImpactSummary));
        }
    }

    /// <summary>
    /// Chuỗi tóm tắt impact hiển thị trong dialog (tự tính từ các count).
    /// </summary>
    public string ImpactSummary =>
        $"{SectionCount} section, {FieldCount} field, {EventCount} event sẽ bị ẩn khỏi runtime.";

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand ConfirmCommand { get; }
    public DelegateCommand CancelCommand  { get; }

    public DeactivateFormDialogViewModel()
    {
        ConfirmCommand = new DelegateCommand(ExecuteConfirm);
        CancelCommand  = new DelegateCommand(ExecuteCancel);
    }

    // ── IDialogAware implementation ───────────────────────────

    public bool CanCloseDialog() => true;

    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        // NOTE: Nhận thông tin form từ FormManagerViewModel qua IDialogParameters
        FormCode     = parameters.GetValue<string>("formCode")  ?? "";
        FormName     = parameters.GetValue<string>("formName")  ?? "";
        SectionCount = parameters.GetValue<int>("sectionCount");
        FieldCount   = parameters.GetValue<int>("fieldCount");
        EventCount   = parameters.GetValue<int>("eventCount");
    }

    // ── Handlers ──────────────────────────────────────────────

    private void ExecuteConfirm()
    {
        // NOTE: Trả về OK để FormManagerViewModel thực hiện soft-delete
        RequestClose.Invoke(new DialogResult(ButtonResult.OK));
    }

    private void ExecuteCancel()
    {
        RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
    }
}
