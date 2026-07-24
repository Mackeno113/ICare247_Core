// File    : LookupTemplateEditDialogViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel popup Sửa/Tạo 1 mẫu lookup (tách khỏi LookupTemplateManagerViewModel — màn
//           danh sách chỉ còn xem, mọi thao tác nhập liệu chuyển vào đây). Nhận LookupTemplateRecord
//           (hoặc null = tạo mới) qua Prism IDialogAware; lưu xong đóng popup và trả Template_Id cho
//           lưới danh sách chọn lại dòng vừa lưu.

using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.Services;
using ConfigStudio.WPF.UI.Core.ViewModels;
using Prism.Commands;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>Editor 1 mẫu lookup, hiển thị dạng popup Prism.</summary>
public sealed class LookupTemplateEditDialogViewModel : ViewModelBase, IDialogAware
{
    private readonly ILookupTemplateDataService _dataService;
    private readonly IAppConfigService _appConfig;
    private readonly IDialogService _dialogService;
    private readonly IAppLogger? _logger;

    private int? _editTemplateId;
    private string _editTemplateCode = "";
    private string _editTen = "";
    private string _editMoTa = "";
    private string _editQueryMode = "table";
    private string _editSourceName = "";
    private string _editValueColumn = "";
    private string _editDisplayColumn = "";
    private string _editCodeField = "";
    private string _editFilterSql = "";
    private string _editOrderBy = "";
    private string _editPopupColumnsJson = "";
    private string _editParentColumn = "";
    private string _editCanonicalParams = "";
    private bool _editIsActive = true;
    private bool _editIsSystem;
    private bool _editIsCustomized;
    private DateTime? _editSyncedAt;
    private int? _editSourceVer;
    private bool _isBusy;
    private string _statusMessage = "";
    private bool _isStatusError;
    private string _title = LookupTemplateUiText.CreateTitle;

    public LookupTemplateEditDialogViewModel(
        ILookupTemplateDataService dataService,
        IAppConfigService appConfig,
        IDialogService dialogService,
        IAppLogger? logger = null)
    {
        _dataService = dataService;
        _appConfig = appConfig;
        _dialogService = dialogService;
        _logger = logger;

        SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
        DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), CanDelete);
        CancelCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));
    }

    public IReadOnlyList<string> QueryModeOptions { get; } = ["table", "tvf", "custom_sql"];

    public int? EditTemplateId
    {
        get => _editTemplateId;
        private set
        {
            if (!SetProperty(ref _editTemplateId, value)) return;
            RaisePropertyChanged(nameof(IsNew));
            RaisePropertyChanged(nameof(IsTemplateCodeEditable));
            Title = IsNew ? LookupTemplateUiText.CreateTitle : LookupTemplateUiText.EditTitle;
            DeleteCommand.RaiseCanExecuteChanged();
        }
    }

    public string EditTemplateCode
    {
        get => _editTemplateCode;
        set { if (SetProperty(ref _editTemplateCode, value)) SaveCommand.RaiseCanExecuteChanged(); }
    }

    public string EditTen
    {
        get => _editTen;
        set { if (SetProperty(ref _editTen, value)) SaveCommand.RaiseCanExecuteChanged(); }
    }

    public string EditMoTa { get => _editMoTa; set => SetProperty(ref _editMoTa, value); }
    public string EditQueryMode
    {
        get => _editQueryMode;
        set
        {
            if (!SetProperty(ref _editQueryMode, value)) return;
            RaisePropertyChanged(nameof(SourceHelpText));
        }
    }

    public string EditSourceName
    {
        get => _editSourceName;
        set { if (SetProperty(ref _editSourceName, value)) SaveCommand.RaiseCanExecuteChanged(); }
    }

    public string EditValueColumn
    {
        get => _editValueColumn;
        set { if (SetProperty(ref _editValueColumn, value)) SaveCommand.RaiseCanExecuteChanged(); }
    }

    public string EditDisplayColumn
    {
        get => _editDisplayColumn;
        set { if (SetProperty(ref _editDisplayColumn, value)) SaveCommand.RaiseCanExecuteChanged(); }
    }

    public string EditCodeField { get => _editCodeField; set => SetProperty(ref _editCodeField, value); }
    public string EditFilterSql { get => _editFilterSql; set => SetProperty(ref _editFilterSql, value); }
    public string EditOrderBy { get => _editOrderBy; set => SetProperty(ref _editOrderBy, value); }
    public string EditPopupColumnsJson { get => _editPopupColumnsJson; set => SetProperty(ref _editPopupColumnsJson, value); }
    public string EditParentColumn { get => _editParentColumn; set => SetProperty(ref _editParentColumn, value); }
    public string EditCanonicalParams { get => _editCanonicalParams; set => SetProperty(ref _editCanonicalParams, value); }
    public bool EditIsActive { get => _editIsActive; set => SetProperty(ref _editIsActive, value); }
    public bool EditIsSystem
    {
        get => _editIsSystem;
        private set
        {
            if (!SetProperty(ref _editIsSystem, value)) return;
            DeleteCommand.RaiseCanExecuteChanged();
        }
    }
    public bool EditIsCustomized { get => _editIsCustomized; private set => SetProperty(ref _editIsCustomized, value); }
    public DateTime? EditSyncedAt { get => _editSyncedAt; private set => SetProperty(ref _editSyncedAt, value); }
    public int? EditSourceVer { get => _editSourceVer; private set => SetProperty(ref _editSourceVer, value); }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            SaveCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (!SetProperty(ref _statusMessage, value)) return;
            RaisePropertyChanged(nameof(HasStatus));
        }
    }

    public bool IsStatusError { get => _isStatusError; private set => SetProperty(ref _isStatusError, value); }
    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool IsNew => !EditTemplateId.HasValue;
    public bool IsTemplateCodeEditable => IsNew;
    public string SourceHelpText => EditQueryMode switch
    {
        "tvf" => LookupTemplateUiText.SourceHelpTvf,
        "custom_sql" => LookupTemplateUiText.SourceHelpSql,
        _ => LookupTemplateUiText.SourceHelpTable,
    };

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand CancelCommand { get; }

    // ── IDialogAware ─────────────────────────────────────────

    public string Title { get => _title; private set => SetProperty(ref _title, value); }
    public DialogCloseListener RequestClose { get; set; }
    public bool CanCloseDialog() => true;
    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters)
        => _ = LoadAsync(parameters.GetValue<LookupTemplateRecord>("record"));

    private async Task LoadAsync(LookupTemplateRecord? record)
    {
        IsBusy = true;
        ClearStatus();
        try
        {
            if (!_appConfig.IsConfigured)
                await _appConfig.LoadAsync();

            if (record is not null)
                ApplyRecord(record);
            else
                ResetEditor();
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "LookupTemplateEditDialog.Load");
            SetError($"Không thể tải mẫu lookup: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    private void ApplyRecord(LookupTemplateRecord record)
    {
        EditTemplateId = record.TemplateId;
        EditTemplateCode = record.TemplateCode;
        EditTen = record.Ten;
        EditMoTa = record.MoTa ?? "";
        EditQueryMode = record.QueryMode;
        EditSourceName = record.SourceName;
        EditValueColumn = record.ValueColumn;
        EditDisplayColumn = record.DisplayColumn;
        EditCodeField = record.CodeField ?? "";
        EditFilterSql = record.FilterSql ?? "";
        EditOrderBy = record.OrderBy ?? "";
        EditPopupColumnsJson = record.PopupColumnsJson ?? "";
        EditParentColumn = record.ParentColumn ?? "";
        EditCanonicalParams = record.CanonicalParams ?? "";
        EditIsActive = record.IsActive;
        EditIsSystem = record.IsSystem;
        EditIsCustomized = record.IsCustomized;
        EditSyncedAt = record.SyncedAt;
        EditSourceVer = record.SourceVer;
    }

    private void ResetEditor()
    {
        EditTemplateId = null;
        EditTemplateCode = "";
        EditTen = "";
        EditMoTa = "";
        EditQueryMode = "table";
        EditSourceName = "";
        EditValueColumn = "";
        EditDisplayColumn = "";
        EditCodeField = "";
        EditFilterSql = "";
        EditOrderBy = "";
        EditPopupColumnsJson = "";
        EditParentColumn = "";
        EditCanonicalParams = "";
        EditIsActive = true;
        EditIsSystem = false;
        EditIsCustomized = false;
        EditSyncedAt = null;
        EditSourceVer = null;
    }

    private bool CanSave()
        => !IsBusy
           && !string.IsNullOrWhiteSpace(EditTemplateCode)
           && !string.IsNullOrWhiteSpace(EditTen)
           && !string.IsNullOrWhiteSpace(EditSourceName)
           && !string.IsNullOrWhiteSpace(EditValueColumn)
           && !string.IsNullOrWhiteSpace(EditDisplayColumn);

    private bool CanDelete() => !IsBusy && EditTemplateId.HasValue && !EditIsSystem;

    private async Task SaveAsync()
    {
        if (!CanSave()) return;
        IsBusy = true;
        ClearStatus();
        try
        {
            var id = await _dataService.SaveTemplateAsync(BuildRequest());
            var result = new DialogResult(ButtonResult.OK);
            result.Parameters.Add("savedId", id);
            RequestClose.Invoke(result);
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "LookupTemplateEditDialog.Save");
            SetError($"Không thể lưu mẫu lookup: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    private LookupTemplateUpsertRequest BuildRequest() => new()
    {
        TemplateId = EditTemplateId,
        TemplateCode = EditTemplateCode,
        Ten = EditTen,
        MoTa = EditMoTa,
        QueryMode = EditQueryMode,
        SourceName = EditSourceName,
        ValueColumn = EditValueColumn,
        DisplayColumn = EditDisplayColumn,
        CodeField = EditCodeField,
        FilterSql = EditFilterSql,
        OrderBy = EditOrderBy,
        PopupColumnsJson = EditPopupColumnsJson,
        ParentColumn = EditParentColumn,
        CanonicalParams = EditCanonicalParams,
        IsActive = EditIsActive,
    };

    private async Task DeleteAsync()
    {
        if (!CanDelete() || !EditTemplateId.HasValue) return;

        try
        {
            var referenceCount = await _dataService.CountReferencesAsync(EditTemplateCode);
            if (referenceCount > 0)
            {
                SetError(
                    $"Không thể xóa mẫu '{EditTemplateCode}' vì đang được {referenceCount} field tham chiếu.");
                return;
            }

            var parameters = new DialogParameters
            {
                { "title", LookupTemplateUiText.ConfirmDeleteTitle },
                { "message", $"Xóa vĩnh viễn mẫu '{EditTemplateCode}'? Thao tác này không thể hoàn tác." },
                { "confirmText", LookupTemplateUiText.ConfirmDeleteButton },
            };
            var completion = new TaskCompletionSource<ButtonResult>();
            _dialogService.ShowDialog(
                ViewNames.ConfirmDialog,
                parameters,
                result => completion.TrySetResult(result.Result));
            if (await completion.Task != ButtonResult.OK) return;

            IsBusy = true;
            await _dataService.DeleteTemplateAsync(EditTemplateId.Value);
            var closeResult = new DialogResult(ButtonResult.OK);
            closeResult.Parameters.Add("savedId", (int?)null!);
            RequestClose.Invoke(closeResult);
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "LookupTemplateEditDialog.Delete");
            SetError($"Không thể xóa mẫu lookup: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    private void SetError(string message)
    {
        StatusMessage = message;
        IsStatusError = true;
    }

    private void ClearStatus()
    {
        StatusMessage = "";
        IsStatusError = false;
    }
}
