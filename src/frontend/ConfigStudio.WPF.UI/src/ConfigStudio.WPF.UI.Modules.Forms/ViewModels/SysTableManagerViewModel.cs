// File    : SysTableManagerViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho màn hình quản trị nhập liệu Sys_Table.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using Prism.Commands;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// Quản lý danh sách và nhập liệu bảng <c>Sys_Table</c> theo tenant hiện tại.
/// </summary>
public sealed class SysTableManagerViewModel : ViewModelBase, INavigationAware
{
    private readonly IFormDataService? _formDataService;
    private readonly IAppConfigService? _appConfig;
    private bool _isProgrammaticEditorUpdate;

    public ObservableCollection<SysTableRecord> Tables { get; } = [];

    /// <summary>CollectionView hỗ trợ search/filter trên DataGrid.</summary>
    public ICollectionView TablesView { get; }

    private SysTableRecord? _selectedTable;
    public SysTableRecord? SelectedTable
    {
        get => _selectedTable;
        set
        {
            if (SetProperty(ref _selectedTable, value))
            {
                if (!_isProgrammaticEditorUpdate)
                    ApplySelectedTable(value);

                RaisePropertyChanged(nameof(IsEditMode));
                RaisePropertyChanged(nameof(SaveButtonText));
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                TablesView.Refresh();
                RaisePropertyChanged(nameof(FilteredCount));
            }
        }
    }

    private bool _showInactive;
    public bool ShowInactive
    {
        get => _showInactive;
        set
        {
            if (SetProperty(ref _showInactive, value))
            {
                TablesView.Refresh();
                RaisePropertyChanged(nameof(FilteredCount));
            }
        }
    }

    private int? _editTableId;
    public int? EditTableId
    {
        get => _editTableId;
        private set => SetProperty(ref _editTableId, value);
    }

    private string _editTableCode = "";
    public string EditTableCode
    {
        get => _editTableCode;
        set
        {
            if (SetProperty(ref _editTableCode, value))
                SaveCommand.RaiseCanExecuteChanged();
        }
    }

    private string _editTableName = "";
    public string EditTableName
    {
        get => _editTableName;
        set
        {
            if (SetProperty(ref _editTableName, value))
                SaveCommand.RaiseCanExecuteChanged();
        }
    }

    private string _editSchemaName = "dbo";
    public string EditSchemaName
    {
        get => _editSchemaName;
        set
        {
            if (SetProperty(ref _editSchemaName, value))
                SaveCommand.RaiseCanExecuteChanged();
        }
    }

    private bool _editIsTenant = true;
    public bool EditIsTenant
    {
        get => _editIsTenant;
        set => SetProperty(ref _editIsTenant, value);
    }

    private bool _editIsActive = true;
    public bool EditIsActive
    {
        get => _editIsActive;
        set => SetProperty(ref _editIsActive, value);
    }

    private string _editDescription = "";
    public string EditDescription
    {
        get => _editDescription;
        set => SetProperty(ref _editDescription, value);
    }

    public int TenantId => _appConfig?.TenantId ?? 0;

    public bool IsEditMode => SelectedTable is not null && EditTableId.HasValue;

    public string SaveButtonText => IsEditMode ? "Cập nhật" : "Tạo mới";

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaisePropertyChanged(nameof(IsNotBusy));
                SaveCommand.RaiseCanExecuteChanged();
                NewCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsNotBusy => !IsBusy;

    private string _loadErrorMessage = "";
    public string LoadErrorMessage
    {
        get => _loadErrorMessage;
        private set
        {
            if (SetProperty(ref _loadErrorMessage, value))
                RaisePropertyChanged(nameof(HasLoadError));
        }
    }

    public bool HasLoadError => !string.IsNullOrWhiteSpace(_loadErrorMessage);

    private string _saveStatusMessage = "";
    public string SaveStatusMessage
    {
        get => _saveStatusMessage;
        private set
        {
            if (SetProperty(ref _saveStatusMessage, value))
                RaisePropertyChanged(nameof(HasSaveStatus));
        }
    }

    private bool _isSaveStatusError;
    public bool IsSaveStatusError
    {
        get => _isSaveStatusError;
        private set => SetProperty(ref _isSaveStatusError, value);
    }

    public bool HasSaveStatus => !string.IsNullOrWhiteSpace(_saveStatusMessage);

    public int TotalTables => Tables.Count;
    public int FilteredCount => TablesView.Cast<object>().Count();

    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand NewCommand { get; }
    public DelegateCommand SaveCommand { get; }

    public SysTableManagerViewModel(
        IFormDataService? formDataService = null,
        IAppConfigService? appConfig = null)
    {
        _formDataService = formDataService;
        _appConfig = appConfig;

        TablesView = CollectionViewSource.GetDefaultView(Tables);
        TablesView.Filter = ApplyFilter;

        RefreshCommand = new DelegateCommand(async () => await LoadDataAsync());
        NewCommand = new DelegateCommand(ExecuteNew, () => IsNotBusy);
        SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), CanSave);

        ResetEditorForNew();
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        _ = LoadDataAsync();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    private bool CanSave()
    {
        return IsNotBusy
            && !string.IsNullOrWhiteSpace(EditTableCode)
            && !string.IsNullOrWhiteSpace(EditTableName)
            && !string.IsNullOrWhiteSpace(EditSchemaName);
    }

    private async Task LoadDataAsync(int? selectedTableId = null)
    {
        IsBusy = true;
        LoadErrorMessage = "";

        try
        {
            await EnsureAppConfigLoadedAsync();
            RaisePropertyChanged(nameof(TenantId));

            if (_formDataService is null || _appConfig is null || !_appConfig.IsConfigured)
            {
                Tables.Clear();
                RaisePropertyChanged(nameof(TotalTables));
                RaisePropertyChanged(nameof(FilteredCount));
                LoadErrorMessage = "Chưa cấu hình DB. Vui lòng vào Settings để cấu hình ConnectionString.";
                return;
            }

            var records = await _formDataService.GetSysTablesAsync(
                _appConfig.TenantId,
                includeInactive: true);

            Tables.Clear();
            foreach (var record in records)
                Tables.Add(record);

            TablesView.Refresh();
            RaisePropertyChanged(nameof(TotalTables));
            RaisePropertyChanged(nameof(FilteredCount));

            var targetId = selectedTableId
                ?? EditTableId
                ?? SelectedTable?.TableId;
            if (targetId.HasValue)
            {
                var selected = Tables.FirstOrDefault(t => t.TableId == targetId.Value);
                if (selected is not null)
                {
                    _isProgrammaticEditorUpdate = true;
                    SelectedTable = selected;
                    _isProgrammaticEditorUpdate = false;
                    ApplySelectedTable(selected);
                    return;
                }
            }

            if (Tables.Count == 0)
                ExecuteNew();
        }
        catch (Exception ex)
        {
            Tables.Clear();
            RaisePropertyChanged(nameof(TotalTables));
            RaisePropertyChanged(nameof(FilteredCount));
            LoadErrorMessage = $"Không thể tải dữ liệu Sys_Table: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool ApplyFilter(object obj)
    {
        if (obj is not SysTableRecord table)
            return false;

        if (!ShowInactive && !table.IsActive)
            return false;

        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var q = SearchText.Trim();
        return table.TableCode.Contains(q, StringComparison.OrdinalIgnoreCase)
            || table.TableName.Contains(q, StringComparison.OrdinalIgnoreCase)
            || table.SchemaName.Contains(q, StringComparison.OrdinalIgnoreCase)
            || table.Description.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    private void ExecuteNew()
    {
        _isProgrammaticEditorUpdate = true;
        SelectedTable = null;
        _isProgrammaticEditorUpdate = false;

        ResetEditorForNew();
        SaveStatusMessage = "";
        IsSaveStatusError = false;
    }

    private async Task ExecuteSaveAsync()
    {
        SaveStatusMessage = "";
        IsSaveStatusError = false;

        var normalizedCode = EditTableCode.Trim();
        var normalizedName = EditTableName.Trim();
        var normalizedSchema = EditSchemaName.Trim();
        var normalizedDescription = string.IsNullOrWhiteSpace(EditDescription)
            ? null
            : EditDescription.Trim();

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            SetSaveError("Table_Code không được để trống.");
            return;
        }
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            SetSaveError("Table_Name không được để trống.");
            return;
        }
        if (string.IsNullOrWhiteSpace(normalizedSchema))
        {
            SetSaveError("Schema_Name không được để trống.");
            return;
        }

        await EnsureAppConfigLoadedAsync();
        if (_formDataService is null || _appConfig is null || !_appConfig.IsConfigured)
        {
            SetSaveError("Chưa cấu hình DB. Vui lòng vào Settings để cấu hình ConnectionString.");
            return;
        }

        IsBusy = true;
        try
        {
            if (IsEditMode && EditTableId.HasValue)
            {
                await _formDataService.UpdateSysTableAsync(
                    EditTableId.Value,
                    normalizedCode,
                    normalizedName,
                    normalizedSchema,
                    EditIsTenant,
                    EditIsActive,
                    _appConfig.TenantId,
                    normalizedDescription);

                SaveStatusMessage = $"Đã cập nhật Sys_Table.Table_Id={EditTableId.Value}.";
                IsSaveStatusError = false;
                await LoadDataAsync(EditTableId.Value);
            }
            else
            {
                var newTableId = await _formDataService.CreateSysTableAsync(
                    normalizedCode,
                    normalizedName,
                    normalizedSchema,
                    EditIsTenant,
                    _appConfig.TenantId,
                    normalizedDescription);

                SaveStatusMessage = $"Đã tạo mới Sys_Table.Table_Id={newTableId}.";
                IsSaveStatusError = false;
                await LoadDataAsync(newTableId);
            }
        }
        catch (Exception ex)
        {
            SetSaveError($"Lỗi lưu Sys_Table: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplySelectedTable(SysTableRecord? table)
    {
        if (table is null)
        {
            ResetEditorForNew();
            return;
        }

        EditTableId = table.TableId;
        EditTableCode = table.TableCode;
        EditTableName = table.TableName;
        EditSchemaName = string.IsNullOrWhiteSpace(table.SchemaName) ? "dbo" : table.SchemaName;
        EditIsTenant = table.IsTenant;
        EditIsActive = table.IsActive;
        EditDescription = table.Description;
    }

    private void ResetEditorForNew()
    {
        EditTableId = null;
        EditTableCode = "";
        EditTableName = "";
        EditSchemaName = "dbo";
        EditIsTenant = true;
        EditIsActive = true;
        EditDescription = "";
    }

    private void SetSaveError(string message)
    {
        SaveStatusMessage = message;
        IsSaveStatusError = true;
    }

    private async Task EnsureAppConfigLoadedAsync()
    {
        if (_appConfig is null || _appConfig.IsConfigured)
            return;

        await _appConfig.LoadAsync();
    }
}
