// File    : CloneFormDialogViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho dialog nhân bản form — nhập Form_Code mới, validate unique, xác nhận clone.

using System.Text.RegularExpressions;
using ConfigStudio.WPF.UI.Core.ViewModels;
using Prism.Commands;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// ViewModel cho CloneFormDialog.
/// Nhận tham số: SourceFormCode (form nguồn), ExistingCodes (danh sách code đã dùng để validate unique).
/// Trả về ButtonResult.OK + tham số NewFormCode nếu user xác nhận.
/// </summary>
public sealed class CloneFormDialogViewModel : ViewModelBase, IDialogAware
{
    // NOTE: Regex validate Form_Code: chỉ cho phép chữ hoa, số, dấu gạch dưới
    private static readonly Regex FormCodeRegex = new(@"^[A-Z0-9_]+$", RegexOptions.Compiled);

    // ── IDialogAware ─────────────────────────────────────────
    public string Title => "Nhân bản Form";
    public DialogCloseListener RequestClose { get; set; }

    // ── Source form info (nhận từ caller) ────────────────────
    private string _sourceFormCode = "";
    public string SourceFormCode
    {
        get => _sourceFormCode;
        private set => SetProperty(ref _sourceFormCode, value);
    }

    /// <summary>
    /// Danh sách Form_Code đã tồn tại — dùng để validate unique phía client.
    /// Caller truyền vào qua IDialogParameters.
    /// </summary>
    private HashSet<string> _existingCodes = [];

    // ── Input ─────────────────────────────────────────────────
    private string _newFormCode = "";
    public string NewFormCode
    {
        get => _newFormCode;
        set
        {
            if (SetProperty(ref _newFormCode, value))
            {
                // NOTE: Validate ngay khi user gõ để hiển thị lỗi realtime
                ValidateNewFormCode();
                CloneCommand.RaiseCanExecuteChanged();
            }
        }
    }

    // ── Validation ────────────────────────────────────────────
    private string _validationMessage = "";
    public string ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            if (SetProperty(ref _validationMessage, value))
                RaisePropertyChanged(nameof(HasValidationError));
        }
    }

    /// <summary>True khi có lỗi validation để hiển thị TextBlock error.</summary>
    public bool HasValidationError => !string.IsNullOrEmpty(_validationMessage);

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand CloneCommand  { get; }
    public DelegateCommand CancelCommand { get; }

    public CloneFormDialogViewModel()
    {
        CloneCommand  = new DelegateCommand(ExecuteClone, CanClone);
        CancelCommand = new DelegateCommand(ExecuteCancel);
    }

    // ── IDialogAware implementation ───────────────────────────

    public bool CanCloseDialog() => true;

    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        // NOTE: Nhận source form code để gợi ý tên mặc định cho clone
        SourceFormCode = parameters.GetValue<string>("sourceFormCode") ?? "";

        // NOTE: Nhận danh sách code hiện có từ FormManagerViewModel để validate unique
        var codes = parameters.GetValue<IEnumerable<string>>("existingCodes");
        _existingCodes = codes is not null
            ? new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase)
            : [];

        // NOTE: Gợi ý tên mặc định: SOURCE_COPY (user có thể chỉnh sửa)
        NewFormCode = string.IsNullOrWhiteSpace(SourceFormCode)
            ? ""
            : $"{SourceFormCode}_COPY";
    }

    // ── Validation ────────────────────────────────────────────

    private void ValidateNewFormCode()
    {
        var code = NewFormCode.Trim();

        if (string.IsNullOrWhiteSpace(code))
        {
            ValidationMessage = "Form_Code không được để trống.";
            return;
        }

        if (!FormCodeRegex.IsMatch(code))
        {
            ValidationMessage = "Form_Code chỉ được dùng chữ HOA, số và dấu gạch dưới (A-Z, 0-9, _).";
            return;
        }

        if (_existingCodes.Contains(code))
        {
            ValidationMessage = $"Form_Code \"{code}\" đã tồn tại. Vui lòng chọn tên khác.";
            return;
        }

        // NOTE: Không được trùng chính mình (dù đã check _existingCodes nhưng nên rõ ràng)
        if (code.Equals(SourceFormCode, StringComparison.OrdinalIgnoreCase))
        {
            ValidationMessage = "Form_Code mới phải khác với form nguồn.";
            return;
        }

        ValidationMessage = "";
    }

    private bool CanClone()
        => !string.IsNullOrWhiteSpace(NewFormCode) && !HasValidationError;

    // ── Handlers ──────────────────────────────────────────────

    private void ExecuteClone()
    {
        if (!CanClone()) return;

        // NOTE: Trả về OK + NewFormCode để FormManagerViewModel thực hiện clone
        var result = new DialogResult(ButtonResult.OK);
        result.Parameters.Add("newFormCode", NewFormCode.Trim().ToUpperInvariant());
        RequestClose.Invoke(result);
    }

    private void ExecuteCancel()
    {
        RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
    }
}
