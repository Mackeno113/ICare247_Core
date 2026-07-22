// File    : LookupTemplateManagerViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel CRUD danh sách và editor Ui_Lookup_Template trên cùng màn hình.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.Services;
using ConfigStudio.WPF.UI.Core.ViewModels;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>Điều phối list/editor và CRUD mẫu lookup.</summary>
public sealed class LookupTemplateManagerViewModel : ViewModelBase, INavigationAware
{
    private readonly ILookupTemplateDataService _dataService;
    private readonly IAppConfigService _appConfig;
    private readonly IDialogService _dialogService;
    private readonly INavigationHistoryService? _history;
    private readonly IAppLogger? _logger;
    private bool _isApplyingSelection;
    private LookupTemplateRecord? _selectedTemplate;
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

    public LookupTemplateManagerViewModel(
        ILookupTemplateDataService dataService,
        IAppConfigService appConfig,
        IDialogService dialogService,
        INavigationHistoryService? history = null,
        IAppLogger? logger = null)
    {
        _dataService = dataService;
        _appConfig = appConfig;
        _dialogService = dialogService;
        _history = history;
        _logger = logger;

        RefreshCommand = new DelegateCommand(async () => await LoadAsync(), () => !IsBusy);
        NewCommand = new DelegateCommand(ResetEditor, () => !IsBusy);
        SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
        DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), CanDelete);
        ResetEditor();
    }

    public ObservableCollection<LookupTemplateRecord> Templates { get; } = [];
    public IReadOnlyList<string> QueryModeOptions { get; } = ["table", "tvf", "custom_sql"];

    public LookupTemplateRecord? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (!SetProperty(ref _selectedTemplate, value) || _isApplyingSelection || value is null) return;
            ApplyRecord(value);
        }
    }

    public int? EditTemplateId
    {
        get => _editTemplateId;
        private set
        {
            if (!SetProperty(ref _editTemplateId, value)) return;
            RaisePropertyChanged(nameof(IsNew));
            RaisePropertyChanged(nameof(IsTemplateCodeEditable));
            RaisePropertyChanged(nameof(EditorTitle));
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
            RefreshCommand.RaiseCanExecuteChanged();
            NewCommand.RaiseCanExecuteChanged();
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
    public string EditorTitle => IsNew ? LookupTemplateUiText.CreateTitle : LookupTemplateUiText.EditTitle;
    public string SourceHelpText => EditQueryMode switch
    {
        "tvf" => LookupTemplateUiText.SourceHelpTvf,
        "custom_sql" => LookupTemplateUiText.SourceHelpSql,
        _ => LookupTemplateUiText.SourceHelpTable,
    };

    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand NewCommand { get; }
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand DeleteCommand { get; }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        _history?.RegisterNavigation(
            new NavigationCrumb
            {
                ViewName = ViewNames.LookupTemplateManager,
                Title = LookupTemplateUiText.ScreenTitle,
                Icon = "⌕",
            },
            isHierarchical: false);
        _ = LoadAsync();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    private bool CanSave()
        => !IsBusy
           && !string.IsNullOrWhiteSpace(EditTemplateCode)
           && !string.IsNullOrWhiteSpace(EditTen)
           && !string.IsNullOrWhiteSpace(EditSourceName)
           && !string.IsNullOrWhiteSpace(EditValueColumn)
           && !string.IsNullOrWhiteSpace(EditDisplayColumn);

    private bool CanDelete() => !IsBusy && EditTemplateId.HasValue && !EditIsSystem;

    private async Task LoadAsync(int? selectId = null)
    {
        IsBusy = true;
        ClearStatus();
        try
        {
            if (!_appConfig.IsConfigured)
                await _appConfig.LoadAsync();
            var records = await _dataService.GetTemplatesAsync();
            Templates.Clear();
            foreach (var record in records) Templates.Add(record);

            var targetId = selectId ?? EditTemplateId;
            var selected = targetId.HasValue
                ? Templates.FirstOrDefault(x => x.TemplateId == targetId.Value)
                : Templates.FirstOrDefault();
            if (selected is not null)
            {
                _isApplyingSelection = true;
                SelectedTemplate = selected;
                _isApplyingSelection = false;
                ApplyRecord(selected);
            }
            else
            {
                ResetEditor();
            }
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "LookupTemplateManager.Load");
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
        ClearStatus();
    }

    private void ResetEditor()
    {
        _isApplyingSelection = true;
        SelectedTemplate = null;
        _isApplyingSelection = false;
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
        ClearStatus();
    }

    private async Task SaveAsync()
    {
        if (!CanSave()) return;
        var wasNew = IsNew;
        IsBusy = true;
        ClearStatus();
        try
        {
            var id = await _dataService.SaveTemplateAsync(BuildRequest());
            await LoadAsync(id);
            StatusMessage = wasNew ? $"Đã tạo mẫu lookup Id={id}." : $"Đã cập nhật mẫu lookup Id={id}.";
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "LookupTemplateManager.Save");
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
            ResetEditor();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "LookupTemplateManager.Delete");
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
