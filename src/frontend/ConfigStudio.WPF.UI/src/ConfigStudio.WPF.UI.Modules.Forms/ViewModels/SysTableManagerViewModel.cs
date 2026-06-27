// File    : SysTableManagerViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho màn hình quản trị nhập liệu Sys_Table.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.Services;
using ConfigStudio.WPF.UI.Core.ViewModels;
using Prism.Commands;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// Quản lý danh sách và nhập liệu bảng <c>Sys_Table</c> theo tenant hiện tại.
/// </summary>
public sealed class SysTableManagerViewModel : ViewModelBase, INavigationAware, IRegionMemberLifetime
{
    private readonly IFormDataService? _formDataService;
    private readonly IAppConfigService? _appConfig;
    private readonly ISchemaInspectorService? _schemaInspector;
    private readonly ISchemaMaintenanceService? _schemaMaintenance;
    private readonly IAppLogger? _logger;
    private bool _isProgrammaticEditorUpdate;

    public ObservableCollection<SysTableRecord> Tables { get; } = [];

    /// <summary>
    /// Danh sách "schema.tên" bảng + VIEW thật trong Target DB (đọc qua SchemaInspector) —
    /// để chọn nhanh thay vì gõ tay Table_Code (tránh sai tên). Rỗng nếu Target DB chưa cấu hình.
    /// </summary>
    public ObservableCollection<string> DbObjects { get; } = [];

    private string? _selectedDbObject;
    /// <summary>Bảng/view user chọn từ combobox. Sự kiện theo sau: tự điền Schema_Name + Table_Code.</summary>
    public string? SelectedDbObject
    {
        get => _selectedDbObject;
        set
        {
            if (!SetProperty(ref _selectedDbObject, value)) return;
            if (string.IsNullOrWhiteSpace(value)) return;
            // Chỉ auto-fill khi chọn đúng 1 mục trong danh sách (tránh điền theo text gõ dở khi lọc).
            if (!DbObjects.Contains(value)) return;

            // value dạng "dbo.TC_CapCongTy" → tách Schema_Name + Table_Code; Table_Name mặc định = tên bảng.
            var dot = value.IndexOf('.');
            var schema = dot > 0 ? value[..dot] : "dbo";
            var name = dot > 0 ? value[(dot + 1)..] : value;
            EditSchemaName = schema;
            EditTableCode = name;
            if (string.IsNullOrWhiteSpace(EditTableName))
                EditTableName = name;
        }
    }

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
                RaisePropertyChanged(nameof(ShowCreateFormButton));
                SaveCommand.RaiseCanExecuteChanged();
                CreateFormFromTableCommand.RaiseCanExecuteChanged();
                _ = RefreshSelectedTableHasFormAsync();
            }
        }
    }

    private bool _selectedTableHasForm;
    /// <summary>True khi bảng đang chọn ĐÃ có Ui_Form (theo Table_Id hoặc Form_Code=Table_Code) → ẩn nút tạo form.</summary>
    public bool SelectedTableHasForm
    {
        get => _selectedTableHasForm;
        private set
        {
            if (SetProperty(ref _selectedTableHasForm, value))
            {
                RaisePropertyChanged(nameof(ShowCreateFormButton));
                CreateFormFromTableCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Hiện nút "Tạo Form từ bảng này": có region, đã chọn bảng đã lưu, và bảng CHƯA có form.</summary>
    public bool ShowCreateFormButton =>
        _regionManager is not null && SelectedTable is { TableId: > 0 } && !SelectedTableHasForm;

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
            {
                SaveCommand.RaiseCanExecuteChanged();
                GenerateHookStoreCommand?.RaiseCanExecuteChanged();
                CheckAuditColumnsCommand?.RaiseCanExecuteChanged();
            }
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

    public string SaveButtonText => IsEditMode ? "Cập nhật" : "Lưu";

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
                GenerateHookStoreCommand?.RaiseCanExecuteChanged();
                CheckAuditColumnsCommand?.RaiseCanExecuteChanged();
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
    /// <summary>Sinh file .sql skeleton hook store (spc_/sp_AfterSave_) cho bảng đang chọn (SVHOOK-5).</summary>
    public DelegateCommand GenerateHookStoreCommand { get; }
    /// <summary>Đối chiếu cột auto chuẩn (§0.1) của bảng đang chọn với Target DB → sinh .sql ALTER nếu thiếu.</summary>
    public DelegateCommand CheckAuditColumnsCommand { get; }
    /// <summary>Mở màn hình Tạo Form mới với Business Table = bảng đang chọn (điền sẵn Form Code + Tên Form).</summary>
    public DelegateCommand CreateFormFromTableCommand { get; }

    private readonly INavigationHistoryService? _history;
    private readonly IRegionManager? _regionManager;

    public SysTableManagerViewModel(
        IFormDataService? formDataService = null,
        IAppConfigService? appConfig = null,
        INavigationHistoryService? history = null,
        ISchemaInspectorService? schemaInspector = null,
        ISchemaMaintenanceService? schemaMaintenance = null,
        IAppLogger? logger = null,
        IRegionManager? regionManager = null)
    {
        _formDataService = formDataService;
        _appConfig = appConfig;
        _history = history;
        _schemaInspector = schemaInspector;
        _schemaMaintenance = schemaMaintenance;
        _logger = logger;
        _regionManager = regionManager;

        TablesView = CollectionViewSource.GetDefaultView(Tables);
        TablesView.Filter = ApplyFilter;

        RefreshCommand = new DelegateCommand(async () => await LoadDataSafeAsync());
        NewCommand = new DelegateCommand(ExecuteNew, () => IsNotBusy);
        SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), CanSave);
        GenerateHookStoreCommand = new DelegateCommand(async () => await ExecuteGenerateHookStoreAsync(), CanGenerateHookStore);
        CheckAuditColumnsCommand = new DelegateCommand(async () => await ExecuteCheckAuditColumnsAsync(), CanCheckAuditColumns);
        CreateFormFromTableCommand = new DelegateCommand(ExecuteCreateFormFromTable, CanCreateFormFromTable);

        ResetEditorForNew();
    }

    // Giu state search/filter khi user roi va quay lai man hinh.
    public bool KeepAlive => true;

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        _history?.RegisterNavigation(
            new NavigationCrumb { ViewName = ViewNames.SysTableManager, Title = "Sys Table", Icon = "⌗" },
            isHierarchical: false);
        _ = LoadDataSafeAsync();
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

    /// <summary>Chỉ tạo form khi đã chọn 1 bảng ĐÃ LƯU (có Table_Id), có region, và bảng CHƯA có form.</summary>
    private bool CanCreateFormFromTable() =>
        _regionManager is not null && SelectedTable is { TableId: > 0 } && !SelectedTableHasForm;

    /// <summary>
    /// Kiểm tra bảng đang chọn đã có Ui_Form chưa (Table_Id hoặc Form_Code=Table_Code) → cập nhật cờ ẩn nút.
    /// Ẩn nút trong lúc kiểm tra để không bao giờ mời tạo trùng. Sự kiện theo sau: nút ẩn/hiện theo kết quả.
    /// </summary>
    private async Task RefreshSelectedTableHasFormAsync()
    {
        // Mặc định ẩn nút trong lúc chờ kết quả (tránh nhấp tạo trùng trước khi check xong).
        SelectedTableHasForm = true;

        if (_formDataService is null || SelectedTable is not { TableId: > 0 } table)
        {
            SelectedTableHasForm = false;
            return;
        }

        try
        {
            var exists = await _formDataService.FormExistsForTableAsync(table.TableId, table.TableCode, TenantId);
            // Bỏ qua nếu user đã đổi sang bảng khác trong lúc chờ (tránh ghi đè cờ sai).
            if (SelectedTable?.TableId == table.TableId)
                SelectedTableHasForm = exists;
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "SysTableManager.CheckFormExists");
            // Lỗi check → vẫn cho phép tạo (không chặn nhầm người dùng).
            if (SelectedTable?.TableId == table.TableId)
                SelectedTableHasForm = false;
        }
    }

    /// <summary>
    /// Mở màn hình Tạo Form mới (FormEditor, formId=0) với Business Table = bảng đang chọn.
    /// Sự kiện theo sau: FormEditor nạp danh sách bảng rồi chọn sẵn theo businessTableId,
    /// kéo theo tự điền Form Code (= Table_Code) và Tên Form (= Table_Name).
    /// </summary>
    private void ExecuteCreateFormFromTable()
    {
        if (_regionManager is null || SelectedTable is not { TableId: > 0 } table) return;

        var p = new NavigationParameters
        {
            { "formId", 0 },
            { "businessTableId", table.TableId },
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormEditor, p);
    }

    /// <summary>
    /// Wrapper an toàn cho fire-and-forget — bắt mọi exception để tránh crash ứng dụng.
    /// </summary>
    private async Task LoadDataSafeAsync(int? selectedTableId = null)
    {
        try
        {
            await LoadDataAsync(selectedTableId);
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "SysTableManager.Load");
            LoadErrorMessage = $"Không thể tải dữ liệu Sys_Table: {ex.Message}";
        }
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

            if (DbObjects.Count == 0)
                await LoadDbObjectsAsync();

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
            _logger?.Capture(ex, "SysTableManager.LoadDataSafe");
            LoadErrorMessage = $"Không thể tải dữ liệu Sys_Table: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Nạp danh sách bảng + VIEW thật từ Target DB (gợi ý chọn nhanh). Lỗi/chưa cấu hình → bỏ qua.</summary>
    private async Task LoadDbObjectsAsync()
    {
        if (_schemaInspector is null || _appConfig is null
            || !_appConfig.IsTargetConfigured
            || string.IsNullOrWhiteSpace(_appConfig.TargetConnectionString))
            return;

        try
        {
            var names = await _schemaInspector.GetTableNamesAsync(_appConfig.TargetConnectionString);
            DbObjects.Clear();
            foreach (var n in names)
                DbObjects.Add(n);
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "SysTableManager.LoadDbObjects");
            // Không chặn màn — combobox rỗng, user vẫn gõ tay được.
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
            || (table.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false);
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
            _logger?.Capture(ex, "SysTableManager.Save");
            SetSaveError($"Lỗi lưu Sys_Table: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Tạo hook store skeleton trực tiếp trên Target DB (SVHOOK-5, ADR-029) ─────

    private bool CanGenerateHookStore()
        => IsNotBusy
        && !string.IsNullOrWhiteSpace(EditTableCode)
        && _schemaInspector is not null
        && _schemaMaintenance is not null
        && _appConfig?.IsTargetConfigured == true;

    /// <summary>
    /// Tạo 2 store skeleton (spc_Grid_/sp_AfterSave_Grid_) cho bảng đang chọn TRỰC TIẾP trên
    /// Target DB. Hỏi xác nhận (báo store nào sẽ tạo / đã có), rồi chạy batch idempotent
    /// (IF OBJECT_ID IS NULL EXEC('CREATE PROCEDURE…')) — chỉ tạo khi chưa có, KHÔNG đè proc đã sửa.
    /// </summary>
    private async Task ExecuteGenerateHookStoreAsync()
    {
        SaveStatusMessage = "";
        IsSaveStatusError = false;

        var tableCode = EditTableCode.Trim();
        var schema = string.IsNullOrWhiteSpace(EditSchemaName) ? "dbo" : EditSchemaName.Trim();

        if (!IsSafeIdentifier(tableCode))
        {
            SetSaveError("Table_Code không hợp lệ để tạo store (chỉ chữ/số/_, không khoảng trắng).");
            return;
        }

        await EnsureAppConfigLoadedAsync();
        if (_schemaInspector is null || _schemaMaintenance is null || _appConfig is null
            || !_appConfig.IsTargetConfigured
            || string.IsNullOrWhiteSpace(_appConfig.TargetConnectionString))
        {
            SetSaveError("Chưa cấu hình Target DB. Vào Settings để cấu hình trước khi tạo store.");
            return;
        }

        IsBusy = true;
        try
        {
            var conn = _appConfig.TargetConnectionString;
            var validateName = HookStoreTemplate.ValidateProcName(tableCode);
            var afterSaveName = HookStoreTemplate.AfterSaveProcName(tableCode);

            // Báo trước store nào đã có (skeleton chỉ tạo cái chưa có).
            var validateExists = await _schemaInspector.ProcedureExistsAsync(conn, schema, validateName);
            var afterSaveExists = await _schemaInspector.ProcedureExistsAsync(conn, schema, afterSaveName);

            var toCreate = new List<string>();
            if (!validateExists) toCreate.Add(validateName);
            if (!afterSaveExists) toCreate.Add(afterSaveName);

            if (toCreate.Count == 0)
            {
                SaveStatusMessage = $"Cả 2 store đã có sẵn trên Target DB ({validateName}, {afterSaveName}) — không tạo lại (không đè).";
                IsSaveStatusError = false;
                return;
            }

            var existsNote = (validateExists || afterSaveExists)
                ? $"\n\nĐã có sẵn (giữ nguyên): {string.Join(", ", new[] { validateExists ? validateName : null, afterSaveExists ? afterSaveName : null }.Where(n => n is not null))}."
                : "";

            var confirm = System.Windows.MessageBox.Show(
                $"Tạo {toCreate.Count} store skeleton trên Target DB cho bảng [{schema}].[{tableCode}]:\n\n  • {string.Join("\n  • ", toCreate)}\n\n"
                + "Skeleton pass-through (chỉ tạo nếu chưa có, không đè proc đã sửa tay)." + existsNote,
                "Tạo hook store", System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question, System.Windows.MessageBoxResult.No);
            if (confirm != System.Windows.MessageBoxResult.Yes)
            {
                SaveStatusMessage = "Đã huỷ — không thay đổi Target DB.";
                IsSaveStatusError = false;
                return;
            }

            var batches = HookStoreTemplate.BuildProcBatches(schema, tableCode);
            await _schemaMaintenance.ExecuteStatementsAsync(conn, batches);

            SaveStatusMessage = $"Đã tạo {toCreate.Count} store skeleton trên [{schema}]: {string.Join(", ", toCreate)}. Viết logic bằng ALTER PROCEDURE trực tiếp trên Target DB (skeleton không lưu file db/procs).";
            IsSaveStatusError = false;
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "SysTableManager.GenerateHookStore");
            SetSaveError($"Lỗi tạo store: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool IsSafeIdentifier(string s)
        => !string.IsNullOrWhiteSpace(s)
        && System.Text.RegularExpressions.Regex.IsMatch(s, "^[A-Za-z_][A-Za-z0-9_]*$");

    // ── Kiểm tra & bổ sung cột auto chuẩn (§0.1 spec 11) ─────────────────────────

    private bool CanCheckAuditColumns()
        => IsNotBusy
        && !string.IsNullOrWhiteSpace(EditTableCode)
        && _schemaInspector is not null
        && _schemaMaintenance is not null
        && _appConfig?.IsTargetConfigured == true;

    /// <summary>
    /// Đọc cột thật của bảng từ Target DB, đối chiếu khối cột auto chuẩn. Đủ → báo OK;
    /// thiếu → hỏi xác nhận (liệt kê cột) rồi thực thi ALTER TRỰC TIẾP lên Target DB
    /// (idempotent — mỗi cột bọc IF COL_LENGTH IS NULL, chạy trong 1 transaction).
    /// </summary>
    private async Task ExecuteCheckAuditColumnsAsync()
    {
        SaveStatusMessage = "";
        IsSaveStatusError = false;

        var tableCode = EditTableCode.Trim();
        var schema = string.IsNullOrWhiteSpace(EditSchemaName) ? "dbo" : EditSchemaName.Trim();

        if (!IsSafeIdentifier(tableCode))
        {
            SetSaveError("Table_Code không hợp lệ để ALTER (chỉ chữ/số/_, không khoảng trắng).");
            return;
        }

        await EnsureAppConfigLoadedAsync();
        if (_schemaInspector is null || _schemaMaintenance is null || _appConfig is null
            || !_appConfig.IsTargetConfigured
            || string.IsNullOrWhiteSpace(_appConfig.TargetConnectionString))
        {
            SetSaveError("Chưa cấu hình Target DB. Vào Settings để cấu hình trước khi kiểm tra cột.");
            return;
        }

        IsBusy = true;
        try
        {
            var columns = await _schemaInspector.GetColumnsAsync(
                _appConfig.TargetConnectionString, schema, tableCode);

            if (columns.Count == 0)
            {
                SetSaveError($"Không đọc được cột của [{schema}].[{tableCode}] — bảng không tồn tại trong Target DB hoặc Table_Code là mã logic (không phải bảng vật lý).");
                return;
            }

            var missing = AuditColumnTemplate.FindMissing(columns.Select(c => c.ColumnName));
            if (missing.Count == 0)
            {
                SaveStatusMessage = $"[{schema}].[{tableCode}] đã đủ khối cột auto chuẩn ({string.Join(", ", AuditColumnTemplate.RequiredColumns)}). Không cần bổ sung.";
                IsSaveStatusError = false;
                return;
            }

            // Xác nhận trước khi đổi schema DB thật.
            var confirm = System.Windows.MessageBox.Show(
                $"Bảng [{schema}].[{tableCode}] thiếu {missing.Count} cột auto:\n\n  • {string.Join("\n  • ", missing)}\n\n"
                + "Thêm các cột này trực tiếp vào Target DB? (idempotent — chỉ thêm cột chưa có)",
                "Bổ sung cột chuẩn", System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question, System.Windows.MessageBoxResult.No);
            if (confirm != System.Windows.MessageBoxResult.Yes)
            {
                SaveStatusMessage = "Đã huỷ — không thay đổi Target DB.";
                IsSaveStatusError = false;
                return;
            }

            var statements = AuditColumnTemplate.BuildAlterStatements(schema, tableCode, missing);
            var executed = await _schemaMaintenance.ExecuteStatementsAsync(
                _appConfig.TargetConnectionString, statements);

            SaveStatusMessage = $"Đã thêm {executed} cột vào [{schema}].[{tableCode}]: {string.Join(", ", missing)}.";
            IsSaveStatusError = false;
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "SysTableManager.CheckAuditColumns");
            SetSaveError($"Lỗi bổ sung cột chuẩn: {ex.Message}");
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

        // Đồng bộ combo "Chọn bảng/view" để hiển thị đúng bảng đang sửa (dạng "schema.code").
        // Set THẲNG backing field — không qua setter — để KHÔNG kích hoạt lại auto-fill Schema/Code.
        _selectedDbObject = $"{EditSchemaName}.{table.TableCode}";
        RaisePropertyChanged(nameof(SelectedDbObject));
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
        _selectedDbObject = null;                       // không kích hoạt auto-fill khi reset
        RaisePropertyChanged(nameof(SelectedDbObject));
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
