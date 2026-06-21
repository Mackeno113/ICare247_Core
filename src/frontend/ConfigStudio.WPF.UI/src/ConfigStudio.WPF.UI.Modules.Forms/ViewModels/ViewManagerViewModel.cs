// File    : ViewManagerViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel màn "Quản lý View" — cấu hình lưới/cây (Ui_View + cột + action).

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.Services;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// Quản lý cấu hình hiển thị danh sách (Grid/TreeList) qua cụm bảng Ui_View.
/// Master-detail: lưới danh sách View bên trái + editor header/cột/action bên phải.
/// </summary>
public sealed class ViewManagerViewModel : ViewModelBase, INavigationAware, IRegionMemberLifetime
{
    private readonly IViewDataService? _viewData;
    private readonly IFormDataService? _formData;
    private readonly IFieldDataService? _fieldData;
    private readonly ISchemaInspectorService? _schemaInspector;
    private readonly IDialogService? _dialogService;
    private readonly IAppConfigService? _appConfig;
    private readonly INavigationHistoryService? _history;
    private readonly IAppLogger? _logger;
    private bool _isProgrammaticSelect;

    /// <summary>Tạm ngưng rekey i18n khi đang nạp dữ liệu / reset editor (set key theo lô).</summary>
    private bool _suppressRekey;

    /// <summary>Khóa (SourceType|TableId|SourceObject) đã nạp danh sách cột — tránh nạp lại thừa.</summary>
    private string _columnsLoadedKey = "";

    // ── Dropdown sources (literal — không i18n) ────────────────
    public IReadOnlyList<string> ViewTypeOptions { get; } = ["Grid", "TreeList", "Cards"];
    public IReadOnlyList<string> SourceTypeOptions { get; } = ["Table", "View", "Sp", "Api"];
    public IReadOnlyList<string> SelectionModeOptions { get; } = ["none", "single", "multiple"];
    public IReadOnlyList<string> RenderModeOptions { get; } =
        ["Text", "Html", "Image", "Link", "Badge", "Boolean", "Template"];
    public IReadOnlyList<string> ColumnKindOptions { get; } = ["Data", "Selection", "Command", "TreeSpin"];
    public IReadOnlyList<string> TextAlignOptions { get; } = ["left", "center", "right"];
    public IReadOnlyList<string> ActionTypeOptions { get; } =
        ["BuiltIn", "Export", "Print", "Navigate", "Event", "Api"];
    public IReadOnlyList<string> ActionScopeOptions { get; } = ["Toolbar", "Row", "Both"];
    public IReadOnlyList<string> ExportEngineOptions { get; } = ["Grid", "Server"];

    // ── Panel lọc (lưới nâng cao) ──────────────────────────────
    public IReadOnlyList<string> FilterControlTypeOptions { get; } =
        ["Text", "Number", "Date", "Combo", "MultiSelect", "Checkbox", "Radio"];
    public IReadOnlyList<string> FilterParamTypeOptions { get; } =
        ["string", "int", "decimal", "date", "bool"];
    public IReadOnlyList<string> FilterOperatorOptions { get; } = ["=", "LIKE", ">=", "<=", "IN"];
    public IReadOnlyList<string> FilterPanelPositionOptions { get; } = ["left", "top"];

    public ObservableCollection<TableLookupRecord> Tables { get; } = [];
    public ObservableCollection<FormRecord> Forms { get; } = [];

    /// <summary>
    /// Danh sách cột của nguồn — nạp lười khi mở column picker.
    /// Table → đọc Sys_Column (Config DB); View/SP → đọc trực tiếp Target DB (INFORMATION_SCHEMA / describe_first_result_set).
    /// </summary>
    public ObservableCollection<ColumnInfoDto> AvailableColumns { get; } = [];

    // ── Master list ────────────────────────────────────────────
    public ObservableCollection<ViewRecord> Views { get; } = [];
    public ICollectionView ViewsView { get; }

    public ObservableCollection<ViewColumnRecord> EditColumns { get; } = [];
    public ObservableCollection<ViewActionRecord> EditActions { get; } = [];
    public ObservableCollection<ViewFilterRecord> EditFilters { get; } = [];

    private ViewRecord? _selectedView;
    public ViewRecord? SelectedView
    {
        get => _selectedView;
        set
        {
            if (SetProperty(ref _selectedView, value) && !_isProgrammaticSelect)
                _ = LoadDetailSafeAsync(value);
        }
    }

    private ViewColumnRecord? _selectedColumn;
    public ViewColumnRecord? SelectedColumn
    {
        get => _selectedColumn;
        set
        {
            if (SetProperty(ref _selectedColumn, value))
            {
                RemoveColumnCommand.RaiseCanExecuteChanged();
                MoveColumnUpCommand.RaiseCanExecuteChanged();
                MoveColumnDownCommand.RaiseCanExecuteChanged();
                OpenColumnCaptionI18nCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private ViewActionRecord? _selectedAction;
    public ViewActionRecord? SelectedAction
    {
        get => _selectedAction;
        set
        {
            if (SetProperty(ref _selectedAction, value))
            {
                RemoveActionCommand.RaiseCanExecuteChanged();
                OpenActionLabelI18nCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private ViewFilterRecord? _selectedFilter;
    public ViewFilterRecord? SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
            {
                RemoveFilterCommand.RaiseCanExecuteChanged();
                MoveFilterUpCommand.RaiseCanExecuteChanged();
                MoveFilterDownCommand.RaiseCanExecuteChanged();
                OpenFilterLabelI18nCommand.RaiseCanExecuteChanged();
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
                ViewsView.Refresh();
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
                ViewsView.Refresh();
                RaisePropertyChanged(nameof(FilteredCount));
            }
        }
    }

    // ── Editor fields (header) ─────────────────────────────────
    private int? _editViewId;
    public int? EditViewId { get => _editViewId; private set => SetProperty(ref _editViewId, value); }

    private int _editVersion = 1;

    private string _editViewCodeSuffix = "";
    /// <summary>
    /// Phần hậu tố do người dùng nhập. View_Code thực = <see cref="EditViewType"/> + "_" + hậu tố này.
    /// </summary>
    public string EditViewCodeSuffix
    {
        get => _editViewCodeSuffix;
        set
        {
            var oldCode = EditViewCode;
            if (SetProperty(ref _editViewCodeSuffix, value))
            {
                RaisePropertyChanged(nameof(EditViewCode));
                RekeyForViewCodeChange(oldCode, EditViewCode);
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Tiền tố cố định "{View_Type}_" hiển thị (read-only) trước ô nhập hậu tố.</summary>
    public string ViewCodePrefix => $"{EditViewType}_";

    /// <summary>View_Code đầy đủ ghép từ View_Type + hậu tố; rỗng nếu chưa nhập hậu tố.</summary>
    public string EditViewCode =>
        string.IsNullOrWhiteSpace(EditViewCodeSuffix) ? "" : $"{EditViewType}_{EditViewCodeSuffix.Trim()}";

    private string _editViewType = "Grid";
    public string EditViewType
    {
        get => _editViewType;
        set
        {
            var oldCode = EditViewCode;
            if (SetProperty(ref _editViewType, value))
            {
                // Đổi View_Type vẫn giữ nguyên hậu tố user nhập — chỉ tiền tố thay đổi.
                RaisePropertyChanged(nameof(IsTreeList));
                RaisePropertyChanged(nameof(ViewCodePrefix));
                RaisePropertyChanged(nameof(EditViewCode));
                RekeyForViewCodeChange(oldCode, EditViewCode);
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Hiện card cấu hình cây khi View_Type = TreeList.</summary>
    public bool IsTreeList => string.Equals(_editViewType, "TreeList", StringComparison.OrdinalIgnoreCase);

    private TableLookupRecord? _editTable;
    public TableLookupRecord? EditTable
    {
        get => _editTable;
        set
        {
            if (SetProperty(ref _editTable, value))
            {
                SaveCommand.RaiseCanExecuteChanged();
                BrowseColumnCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private string _editSourceType = "Table";
    public string EditSourceType { get => _editSourceType; set => SetProperty(ref _editSourceType, value); }

    private string _editSourceObject = "";
    public string EditSourceObject { get => _editSourceObject; set => SetProperty(ref _editSourceObject, value); }

    private string _editTitleKey = "";
    public string EditTitleKey { get => _editTitleKey; set => SetProperty(ref _editTitleKey, value); }

    private FormRecord? _editEditForm;
    public FormRecord? EditEditForm { get => _editEditForm; set => SetProperty(ref _editEditForm, value); }

    private int _editPageSize = 20;
    public int EditPageSize { get => _editPageSize; set => SetProperty(ref _editPageSize, value); }

    private bool _editAllowPaging = true;
    public bool EditAllowPaging { get => _editAllowPaging; set => SetProperty(ref _editAllowPaging, value); }

    private bool _editVirtualScroll;
    public bool EditVirtualScroll { get => _editVirtualScroll; set => SetProperty(ref _editVirtualScroll, value); }

    private bool _editShowFilterRow = true;
    public bool EditShowFilterRow { get => _editShowFilterRow; set => SetProperty(ref _editShowFilterRow, value); }

    private bool _editShowGroupPanel;
    public bool EditShowGroupPanel { get => _editShowGroupPanel; set => SetProperty(ref _editShowGroupPanel, value); }

    private bool _editShowSearchBox = true;
    public bool EditShowSearchBox { get => _editShowSearchBox; set => SetProperty(ref _editShowSearchBox, value); }

    private bool _editShowColumnChooser;
    public bool EditShowColumnChooser { get => _editShowColumnChooser; set => SetProperty(ref _editShowColumnChooser, value); }

    private string _editSelectionMode = "none";
    public string EditSelectionMode { get => _editSelectionMode; set => SetProperty(ref _editSelectionMode, value); }

    private bool _editAllowAdd = true;
    public bool EditAllowAdd { get => _editAllowAdd; set => SetProperty(ref _editAllowAdd, value); }

    private bool _editAllowEdit = true;
    public bool EditAllowEdit { get => _editAllowEdit; set => SetProperty(ref _editAllowEdit, value); }

    private bool _editAllowDelete = true;
    public bool EditAllowDelete { get => _editAllowDelete; set => SetProperty(ref _editAllowDelete, value); }

    private bool _editAllowExport = true;
    public bool EditAllowExport { get => _editAllowExport; set => SetProperty(ref _editAllowExport, value); }

    private string _editExportFormats = "";
    public string EditExportFormats { get => _editExportFormats; set => SetProperty(ref _editExportFormats, value); }

    private string _editExportFileNameKey = "";
    public string EditExportFileNameKey { get => _editExportFileNameKey; set => SetProperty(ref _editExportFileNameKey, value); }

    private bool _editAllowPrint;
    public bool EditAllowPrint { get => _editAllowPrint; set => SetProperty(ref _editAllowPrint, value); }

    private string _editKeyField = "";
    public string EditKeyField { get => _editKeyField; set => SetProperty(ref _editKeyField, value); }

    private string _editParentField = "";
    public string EditParentField { get => _editParentField; set => SetProperty(ref _editParentField, value); }

    private int? _editExpandLevel;
    public int? EditExpandLevel { get => _editExpandLevel; set => SetProperty(ref _editExpandLevel, value); }

    // ── Panel lọc (lưới nâng cao) ──────────────────────────────
    private bool _editFilterPanelEnabled;
    public bool EditFilterPanelEnabled { get => _editFilterPanelEnabled; set => SetProperty(ref _editFilterPanelEnabled, value); }

    private string _editFilterPanelPosition = "left";
    public string EditFilterPanelPosition { get => _editFilterPanelPosition; set => SetProperty(ref _editFilterPanelPosition, value); }

    private bool _editFilterCollapsible = true;
    public bool EditFilterCollapsible { get => _editFilterCollapsible; set => SetProperty(ref _editFilterCollapsible, value); }

    private bool _editAutoSearchOnLoad;
    public bool EditAutoSearchOnLoad { get => _editAutoSearchOnLoad; set => SetProperty(ref _editAutoSearchOnLoad, value); }

    private string _editSearchLabelKey = "";
    public string EditSearchLabelKey { get => _editSearchLabelKey; set => SetProperty(ref _editSearchLabelKey, value); }

    private string _editResetLabelKey = "";
    public string EditResetLabelKey { get => _editResetLabelKey; set => SetProperty(ref _editResetLabelKey, value); }

    private bool _editIsActive = true;
    public bool EditIsActive { get => _editIsActive; set => SetProperty(ref _editIsActive, value); }

    private string _editDescription = "";
    public string EditDescription { get => _editDescription; set => SetProperty(ref _editDescription, value); }

    // ── State ──────────────────────────────────────────────────
    public int TenantId => _appConfig?.TenantId ?? 0;
    public bool IsEditMode => EditViewId.HasValue;
    public string SaveButtonText => "Lưu";
    public string EditorTitle => IsEditMode ? "Cập nhật View" : "Tạo mới View";

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
                DeactivateCommand.RaiseCanExecuteChanged();
            }
        }
    }
    public bool IsNotBusy => !IsBusy;

    private string _loadErrorMessage = "";
    public string LoadErrorMessage
    {
        get => _loadErrorMessage;
        private set { if (SetProperty(ref _loadErrorMessage, value)) RaisePropertyChanged(nameof(HasLoadError)); }
    }
    public bool HasLoadError => !string.IsNullOrWhiteSpace(_loadErrorMessage);

    private string _saveStatusMessage = "";
    public string SaveStatusMessage
    {
        get => _saveStatusMessage;
        private set { if (SetProperty(ref _saveStatusMessage, value)) RaisePropertyChanged(nameof(HasSaveStatus)); }
    }
    public bool HasSaveStatus => !string.IsNullOrWhiteSpace(_saveStatusMessage);

    private bool _isSaveStatusError;
    public bool IsSaveStatusError { get => _isSaveStatusError; private set => SetProperty(ref _isSaveStatusError, value); }

    public int TotalViews => Views.Count;
    public int FilteredCount => ViewsView.Cast<object>().Count();

    // ── Commands ───────────────────────────────────────────────
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand NewCommand { get; }
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand DeactivateCommand { get; }
    public DelegateCommand AddColumnCommand { get; }
    public DelegateCommand RemoveColumnCommand { get; }
    public DelegateCommand MoveColumnUpCommand { get; }
    public DelegateCommand MoveColumnDownCommand { get; }
    public DelegateCommand AddActionCommand { get; }
    public DelegateCommand RemoveActionCommand { get; }
    public DelegateCommand AddFilterCommand { get; }
    public DelegateCommand RemoveFilterCommand { get; }
    public DelegateCommand MoveFilterUpCommand { get; }
    public DelegateCommand MoveFilterDownCommand { get; }
    public DelegateCommand OpenFilterLabelI18nCommand { get; }
    public DelegateCommand<ViewFilterRecord> OpenFilterLabelI18nRowCommand { get; }
    public DelegateCommand BrowseColumnCommand { get; }
    public DelegateCommand OpenTitleI18nCommand { get; }
    public DelegateCommand OpenExportFileNameI18nCommand { get; }
    public DelegateCommand OpenColumnCaptionI18nCommand { get; }
    public DelegateCommand OpenActionLabelI18nCommand { get; }
    public DelegateCommand<ViewColumnRecord> OpenColumnCaptionI18nRowCommand { get; }
    public DelegateCommand<ViewActionRecord> OpenActionLabelI18nRowCommand { get; }

    /// <summary>
    /// Khởi tạo ViewModel + thiết lập CollectionView (filter) và command.
    /// </summary>
    /// <param name="viewData">Service truy vấn Ui_View.</param>
    /// <param name="formData">Service tra cứu Sys_Table + Ui_Form cho dropdown.</param>
    /// <param name="fieldData">Service tra cứu cột Sys_Column cho column picker (nguồn Table).</param>
    /// <param name="schemaInspector">Đọc cấu trúc cột trực tiếp từ Target DB (nguồn View/SP).</param>
    /// <param name="dialogService">Mở popup i18n + column picker.</param>
    /// <param name="appConfig">Cấu hình DB + tenant.</param>
    /// <param name="history">Lịch sử điều hướng (breadcrumb).</param>
    /// <param name="logger">Ghi log lỗi.</param>
    public ViewManagerViewModel(
        IViewDataService? viewData = null,
        IFormDataService? formData = null,
        IFieldDataService? fieldData = null,
        ISchemaInspectorService? schemaInspector = null,
        IDialogService? dialogService = null,
        IAppConfigService? appConfig = null,
        INavigationHistoryService? history = null,
        IAppLogger? logger = null)
    {
        _viewData = viewData;
        _formData = formData;
        _fieldData = fieldData;
        _schemaInspector = schemaInspector;
        _dialogService = dialogService;
        _appConfig = appConfig;
        _history = history;
        _logger = logger;

        ViewsView = CollectionViewSource.GetDefaultView(Views);
        ViewsView.Filter = ApplyFilter;

        RefreshCommand = new DelegateCommand(async () => await LoadDataSafeAsync());
        NewCommand = new DelegateCommand(ExecuteNew, () => IsNotBusy);
        SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), CanSave);
        DeactivateCommand = new DelegateCommand(async () => await ExecuteDeactivateAsync(), () => IsNotBusy && IsEditMode);
        AddColumnCommand = new DelegateCommand(ExecuteAddColumn);
        RemoveColumnCommand = new DelegateCommand(ExecuteRemoveColumn, () => SelectedColumn is not null);
        MoveColumnUpCommand = new DelegateCommand(() => MoveColumn(-1), () => SelectedColumn is not null);
        MoveColumnDownCommand = new DelegateCommand(() => MoveColumn(1), () => SelectedColumn is not null);
        AddActionCommand = new DelegateCommand(ExecuteAddAction);
        RemoveActionCommand = new DelegateCommand(ExecuteRemoveAction, () => SelectedAction is not null);
        AddFilterCommand = new DelegateCommand(ExecuteAddFilter);
        RemoveFilterCommand = new DelegateCommand(ExecuteRemoveFilter, () => SelectedFilter is not null);
        MoveFilterUpCommand = new DelegateCommand(() => MoveFilter(-1), () => SelectedFilter is not null);
        MoveFilterDownCommand = new DelegateCommand(() => MoveFilter(1), () => SelectedFilter is not null);
        OpenFilterLabelI18nCommand = new DelegateCommand(ExecuteOpenFilterLabelI18n, () => SelectedFilter is not null);
        OpenFilterLabelI18nRowCommand = new DelegateCommand<ViewFilterRecord>(OpenFilterLabelI18nForRow);
        BrowseColumnCommand = new DelegateCommand(async () => await ExecuteBrowseColumnAsync(), () => EditTable is not null);
        OpenTitleI18nCommand = new DelegateCommand(ExecuteOpenTitleI18n);
        OpenExportFileNameI18nCommand = new DelegateCommand(ExecuteOpenExportFileNameI18n);
        OpenColumnCaptionI18nCommand = new DelegateCommand(ExecuteOpenColumnCaptionI18n, () => SelectedColumn is not null);
        OpenActionLabelI18nCommand = new DelegateCommand(ExecuteOpenActionLabelI18n, () => SelectedAction is not null);
        OpenColumnCaptionI18nRowCommand = new DelegateCommand<ViewColumnRecord>(OpenColumnCaptionI18nForRow);
        OpenActionLabelI18nRowCommand = new DelegateCommand<ViewActionRecord>(OpenActionLabelI18nForRow);

        ResetEditor();
    }

    public bool KeepAlive => true;

    /// <summary>
    /// Khi điều hướng tới màn — đăng ký breadcrumb và nạp dữ liệu.
    /// </summary>
    /// <param name="navigationContext">Ngữ cảnh điều hướng Prism.</param>
    /// <remarks>Side-effect: thêm crumb "Views" + bắt đầu load list (fire-and-forget).</remarks>
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        _history?.RegisterNavigation(
            new NavigationCrumb { ViewName = ViewNames.ViewManager, Title = "Views", Icon = "▦" },
            isHierarchical: false);
        _ = LoadDataSafeAsync();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    /// <summary>Điều kiện cho phép lưu: không bận + có View_Code + đã chọn bảng nguồn.</summary>
    /// <returns>true nếu được phép lưu.</returns>
    private bool CanSave()
        => IsNotBusy && !string.IsNullOrWhiteSpace(EditViewCodeSuffix) && EditTable is not null;

    /// <summary>Wrapper an toàn nạp dữ liệu — bắt mọi exception tránh crash.</summary>
    /// <param name="selectViewId">View_Id cần chọn lại sau khi load (optional).</param>
    private async Task LoadDataSafeAsync(int? selectViewId = null)
    {
        try { await LoadDataAsync(selectViewId); }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "ViewManager.Load");
            LoadErrorMessage = $"Không thể tải danh sách View: {ex.Message}";
        }
    }

    /// <summary>
    /// Nạp dropdown (bảng + form), danh sách View và chọn lại item nếu cần.
    /// </summary>
    /// <param name="selectViewId">View_Id chọn lại sau khi load.</param>
    /// <remarks>Side-effect: refresh CollectionView; raise Total/Filtered; set busy on/off.</remarks>
    private async Task LoadDataAsync(int? selectViewId = null)
    {
        IsBusy = true;
        LoadErrorMessage = "";
        try
        {
            await EnsureAppConfigLoadedAsync();
            RaisePropertyChanged(nameof(TenantId));

            if (_viewData is null || _appConfig is null || !_appConfig.IsConfigured)
            {
                Views.Clear();
                RaisePropertyChanged(nameof(TotalViews));
                RaisePropertyChanged(nameof(FilteredCount));
                LoadErrorMessage = "Chưa cấu hình DB. Vui lòng vào Settings để cấu hình ConnectionString.";
                return;
            }

            await LoadLookupsAsync();

            var records = await _viewData.GetViewsAsync(_appConfig.TenantId, includeInactive: true);
            Views.Clear();
            foreach (var r in records) Views.Add(r);
            ViewsView.Refresh();
            RaisePropertyChanged(nameof(TotalViews));
            RaisePropertyChanged(nameof(FilteredCount));

            var targetId = selectViewId ?? EditViewId ?? SelectedView?.ViewId;
            if (targetId.HasValue)
            {
                var match = Views.FirstOrDefault(v => v.ViewId == targetId.Value);
                if (match is not null)
                {
                    _isProgrammaticSelect = true;
                    SelectedView = match;
                    _isProgrammaticSelect = false;
                    await LoadDetailSafeAsync(match);
                }
            }
        }
        finally { IsBusy = false; }
    }

    /// <summary>Nạp danh sách bảng + form cho dropdown editor (chỉ 1 lần / refresh).</summary>
    private async Task LoadLookupsAsync()
    {
        if (_formData is null || _appConfig is null) return;

        var tables = await _formData.GetTablesByTenantAsync(_appConfig.TenantId);
        Tables.Clear();
        foreach (var t in tables) Tables.Add(t);

        var forms = await _formData.GetAllFormsAsync(_appConfig.TenantId, includeInactive: false);
        Forms.Clear();
        foreach (var f in forms) Forms.Add(f);
    }

    /// <summary>Wrapper an toàn nạp chi tiết một View vào editor.</summary>
    /// <param name="view">View được chọn (null = bỏ qua).</param>
    private async Task LoadDetailSafeAsync(ViewRecord? view)
    {
        if (view is null || _viewData is null) return;
        try
        {
            IsBusy = true;
            var detail = await _viewData.GetViewDetailAsync(view.ViewId);
            if (detail is null)
            {
                LoadErrorMessage = $"Không tìm thấy chi tiết View_Id={view.ViewId}.";
                return;
            }
            ApplyDetail(detail);
            SaveStatusMessage = "";
            IsSaveStatusError = false;
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "ViewManager.LoadDetail");
            LoadErrorMessage = $"Không thể nạp chi tiết View: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    /// <summary>Đổ chi tiết một View (header + cột + action) vào editor.</summary>
    /// <param name="detail">Dữ liệu chi tiết.</param>
    /// <remarks>Side-effect: thay toàn bộ EditColumns/EditActions + raise IsEditMode/SaveButtonText/EditorTitle.</remarks>
    private void ApplyDetail(ViewDetailRecord detail)
    {
        _suppressRekey = true;
        try
        {
        var h = detail.Header;
        EditViewId = h.ViewId;
        _editVersion = h.Version;
        EditViewType = h.ViewType;
        EditViewCodeSuffix = StripViewTypePrefix(h.ViewCode, h.ViewType);
        EditTable = Tables.FirstOrDefault(t => t.TableId == h.TableId);
        EditSourceType = h.SourceType;
        EditSourceObject = h.SourceObject ?? "";
        EditTitleKey = h.TitleKey ?? "";
        EditEditForm = h.EditFormId.HasValue ? Forms.FirstOrDefault(f => f.FormId == h.EditFormId.Value) : null;
        EditPageSize = h.PageSize;
        EditAllowPaging = h.AllowPaging;
        EditVirtualScroll = h.VirtualScroll;
        EditShowFilterRow = h.ShowFilterRow;
        EditShowGroupPanel = h.ShowGroupPanel;
        EditShowSearchBox = h.ShowSearchBox;
        EditShowColumnChooser = h.ShowColumnChooser;
        EditSelectionMode = h.SelectionMode;
        EditAllowAdd = h.AllowAdd;
        EditAllowEdit = h.AllowEdit;
        EditAllowDelete = h.AllowDelete;
        EditAllowExport = h.AllowExport;
        EditExportFormats = h.ExportFormats ?? "";
        EditExportFileNameKey = h.ExportFileNameKey ?? "";
        EditAllowPrint = h.AllowPrint;
        EditKeyField = h.KeyField ?? "";
        EditParentField = h.ParentField ?? "";
        EditExpandLevel = h.ExpandLevel;
        EditFilterPanelEnabled = h.FilterPanelEnabled;
        EditFilterPanelPosition = string.IsNullOrWhiteSpace(h.FilterPanelPosition) ? "left" : h.FilterPanelPosition;
        EditFilterCollapsible = h.FilterCollapsible;
        EditAutoSearchOnLoad = h.AutoSearchOnLoad;
        EditSearchLabelKey = h.SearchLabelKey ?? "";
        EditResetLabelKey = h.ResetLabelKey ?? "";
        EditIsActive = h.IsActive;
        EditDescription = h.Description ?? "";

        EditColumns.Clear();
        foreach (var c in detail.Columns) EditColumns.Add(c);
        EditActions.Clear();
        foreach (var a in detail.Actions) EditActions.Add(a);
        EditFilters.Clear();
        foreach (var f in detail.Filters) EditFilters.Add(f);

        RaiseEditorState();
        }
        finally { _suppressRekey = false; }
    }

    /// <summary>Lọc danh sách View theo ShowInactive + SearchText.</summary>
    /// <param name="obj">Phần tử đang xét.</param>
    /// <returns>true nếu hiển thị.</returns>
    private bool ApplyFilter(object obj)
    {
        if (obj is not ViewRecord v) return false;
        if (!ShowInactive && !v.IsActive) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var q = SearchText.Trim();
        return v.ViewCode.Contains(q, StringComparison.OrdinalIgnoreCase)
            || v.ViewType.Contains(q, StringComparison.OrdinalIgnoreCase)
            || v.TableCode.Contains(q, StringComparison.OrdinalIgnoreCase)
            || (v.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    /// <summary>Đặt editor về trạng thái tạo mới (bỏ chọn View hiện tại).</summary>
    /// <remarks>Side-effect: clear SelectedView + reset toàn bộ field editor + clear status.</remarks>
    private void ExecuteNew()
    {
        var confirm = System.Windows.MessageBox.Show(
            "Xóa trắng nội dung đang nhập để bắt đầu một View mới?\nMọi thay đổi chưa lưu sẽ mất.",
            "Xác nhận tạo mới",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        _isProgrammaticSelect = true;
        SelectedView = null;
        _isProgrammaticSelect = false;
        ResetEditor();
        SaveStatusMessage = "";
        IsSaveStatusError = false;
    }

    /// <summary>Bỏ tiền tố "{View_Type}_" khỏi View_Code để khôi phục phần hậu tố user nhập.</summary>
    /// <param name="viewCode">View_Code đầy đủ.</param>
    /// <param name="viewType">View_Type tương ứng.</param>
    /// <returns>Hậu tố sau tiền tố, hoặc nguyên View_Code nếu không khớp tiền tố.</returns>
    private static string StripViewTypePrefix(string viewCode, string viewType)
    {
        var prefix = $"{viewType}_";
        return viewCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? viewCode[prefix.Length..]
            : viewCode;
    }

    /// <summary>Reset toàn bộ field editor về mặc định cho View mới.</summary>
    private void ResetEditor()
    {
        _suppressRekey = true;
        try
        {
        EditViewId = null;
        _editVersion = 1;
        EditViewType = "Grid";
        EditViewCodeSuffix = "";
        EditTable = null;
        EditSourceType = "Table";
        EditSourceObject = "";
        EditTitleKey = "";
        EditEditForm = null;
        EditPageSize = 20;
        EditAllowPaging = true;
        EditVirtualScroll = false;
        EditShowFilterRow = true;
        EditShowGroupPanel = false;
        EditShowSearchBox = true;
        EditShowColumnChooser = false;
        EditSelectionMode = "none";
        EditAllowAdd = true;
        EditAllowEdit = true;
        EditAllowDelete = true;
        EditAllowExport = true;
        EditExportFormats = "xlsx,csv";
        EditExportFileNameKey = "";
        EditAllowPrint = false;
        EditKeyField = "";
        EditParentField = "";
        EditExpandLevel = null;
        EditFilterPanelEnabled = false;
        EditFilterPanelPosition = "left";
        EditFilterCollapsible = true;
        EditAutoSearchOnLoad = false;
        EditSearchLabelKey = "";
        EditResetLabelKey = "";
        EditIsActive = true;
        EditDescription = "";
        EditColumns.Clear();
        EditActions.Clear();
        EditFilters.Clear();
        RaiseEditorState();
        }
        finally { _suppressRekey = false; }
    }

    /// <summary>Raise các property phụ thuộc trạng thái edit/new.</summary>
    private void RaiseEditorState()
    {
        RaisePropertyChanged(nameof(IsEditMode));
        RaisePropertyChanged(nameof(SaveButtonText));
        RaisePropertyChanged(nameof(EditorTitle));
        RaisePropertyChanged(nameof(IsTreeList));
        SaveCommand.RaiseCanExecuteChanged();
        DeactivateCommand.RaiseCanExecuteChanged();
    }

    /// <summary>Thêm một dòng cột mới (rỗng) vào lưới cấu hình cột.</summary>
    /// <remarks>Side-effect: chọn dòng vừa thêm để user nhập ngay.</remarks>
    private void ExecuteAddColumn()
    {
        var col = new ViewColumnRecord { OrderNo = EditColumns.Count, FieldName = "" };
        EditColumns.Add(col);
        SelectedColumn = col;
    }

    /// <summary>Xóa dòng cột đang chọn khỏi lưới.</summary>
    private void ExecuteRemoveColumn()
    {
        if (SelectedColumn is null) return;
        EditColumns.Remove(SelectedColumn);
        SelectedColumn = null;
    }

    /// <summary>Di chuyển cột đang chọn lên/xuống và cập nhật OrderNo.</summary>
    /// <param name="delta">-1 lên, +1 xuống.</param>
    private void MoveColumn(int delta)
    {
        if (SelectedColumn is null) return;
        var idx = EditColumns.IndexOf(SelectedColumn);
        var newIdx = idx + delta;
        if (newIdx < 0 || newIdx >= EditColumns.Count) return;
        EditColumns.Move(idx, newIdx);
        for (var i = 0; i < EditColumns.Count; i++) EditColumns[i].OrderNo = i;
    }

    /// <summary>Thêm một action mới (rỗng) vào lưới hành động.</summary>
    private void ExecuteAddAction()
    {
        var act = new ViewActionRecord { OrderNo = EditActions.Count, ActionCode = "" };
        EditActions.Add(act);
        SelectedAction = act;
    }

    /// <summary>Xóa action đang chọn khỏi lưới.</summary>
    private void ExecuteRemoveAction()
    {
        if (SelectedAction is null) return;
        EditActions.Remove(SelectedAction);
        SelectedAction = null;
    }

    // ── Panel lọc (Ui_View_Filter) ─────────────────────────────

    /// <summary>Thêm một dòng filter mới (rỗng) vào panel lọc.</summary>
    private void ExecuteAddFilter()
    {
        var f = new ViewFilterRecord { OrderNo = EditFilters.Count, FilterCode = "", ControlType = "Text" };
        EditFilters.Add(f);
        SelectedFilter = f;
    }

    /// <summary>Xóa dòng filter đang chọn khỏi panel.</summary>
    private void ExecuteRemoveFilter()
    {
        if (SelectedFilter is null) return;
        EditFilters.Remove(SelectedFilter);
        SelectedFilter = null;
    }

    /// <summary>Di chuyển filter đang chọn lên/xuống và cập nhật OrderNo.</summary>
    /// <param name="delta">-1 lên, +1 xuống.</param>
    private void MoveFilter(int delta)
    {
        if (SelectedFilter is null) return;
        var idx = EditFilters.IndexOf(SelectedFilter);
        var newIdx = idx + delta;
        if (newIdx < 0 || newIdx >= EditFilters.Count) return;
        EditFilters.Move(idx, newIdx);
        for (var i = 0; i < EditFilters.Count; i++) EditFilters[i].OrderNo = i;
    }

    /// <summary>Dịch Label_Key của filter đang chọn (nút toolbar).</summary>
    private void ExecuteOpenFilterLabelI18n() => OpenFilterLabelI18nForRow(SelectedFilter);

    /// <summary>Dịch Label_Key của một filter — tự sinh key từ Filter_Code nếu đang trống.</summary>
    /// <param name="flt">Dòng filter (từ nút 🌐 inline hoặc filter đang chọn).</param>
    private void OpenFilterLabelI18nForRow(ViewFilterRecord? flt)
    {
        if (flt is null) return;
        if (string.IsNullOrWhiteSpace(flt.LabelKey))
        {
            var code = flt.FilterCode?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(code)) { SetSaveError("Nhập Filter_Code trước khi tạo key nhãn."); return; }
            var key = BuildViewKey($"filter.{code}.label");
            if (key is null) { SetSaveError("Cần View_Code và bảng nguồn trước khi tạo key i18n."); return; }
            flt.LabelKey = key;
        }
        OpenI18nDialog(flt.LabelKey!, $"Nhãn filter {flt.FilterCode}");
    }

    /// <summary>
    /// Lưu View hiện tại (tạo mới hoặc cập nhật) kèm toàn bộ cột + action.
    /// </summary>
    /// <remarks>Side-effect: gọi service ghi DB (transaction); reload list + chọn lại View; set status.</remarks>
    private async Task ExecuteSaveAsync()
    {
        SaveStatusMessage = "";
        IsSaveStatusError = false;

        var code = EditViewCode.Trim();
        if (string.IsNullOrWhiteSpace(code)) { SetSaveError("View_Code không được để trống."); return; }
        if (EditTable is null) { SetSaveError("Phải chọn bảng nguồn."); return; }

        await EnsureAppConfigLoadedAsync();
        if (_viewData is null || _appConfig is null || !_appConfig.IsConfigured)
        {
            SetSaveError("Chưa cấu hình DB. Vui lòng vào Settings.");
            return;
        }

        var request = BuildRequest(code);

        IsBusy = true;
        try
        {
            var viewId = await _viewData.SaveViewAsync(request, _appConfig.TenantId);
            SaveStatusMessage = IsEditMode
                ? $"Đã cập nhật View_Id={viewId}."
                : $"Đã tạo mới View_Id={viewId}.";
            IsSaveStatusError = false;
            await LoadDataAsync(viewId);
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "ViewManager.Save");
            SetSaveError($"Lỗi lưu View: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    /// <summary>Dựng payload upsert từ trạng thái editor hiện tại.</summary>
    /// <param name="code">View_Code đã trim.</param>
    /// <returns><see cref="ViewUpsertRequest"/> đầy đủ header + cột + action.</returns>
    private ViewUpsertRequest BuildRequest(string code) => new()
    {
        ViewId = EditViewId,
        Version = _editVersion,
        ViewCode = code,
        ViewType = EditViewType,
        TableId = EditTable!.TableId,
        SourceType = EditSourceType,
        SourceObject = EditSourceObject,
        TitleKey = EditTitleKey,
        EditFormId = EditEditForm?.FormId,
        PageSize = EditPageSize,
        AllowPaging = EditAllowPaging,
        VirtualScroll = EditVirtualScroll,
        ShowFilterRow = EditShowFilterRow,
        ShowGroupPanel = EditShowGroupPanel,
        ShowSearchBox = EditShowSearchBox,
        ShowColumnChooser = EditShowColumnChooser,
        SelectionMode = EditSelectionMode,
        AllowAdd = EditAllowAdd,
        AllowEdit = EditAllowEdit,
        AllowDelete = EditAllowDelete,
        AllowExport = EditAllowExport,
        ExportFormats = EditExportFormats,
        ExportFileNameKey = EditExportFileNameKey,
        AllowPrint = EditAllowPrint,
        KeyField = EditKeyField,
        ParentField = EditParentField,
        ExpandLevel = EditExpandLevel,
        FilterPanelEnabled = EditFilterPanelEnabled,
        FilterPanelPosition = EditFilterPanelPosition,
        FilterCollapsible = EditFilterCollapsible,
        AutoSearchOnLoad = EditAutoSearchOnLoad,
        SearchLabelKey = EditSearchLabelKey,
        ResetLabelKey = EditResetLabelKey,
        IsActive = EditIsActive,
        Description = EditDescription,
        Columns = [.. EditColumns],
        Actions = [.. EditActions],
        Filters = [.. EditFilters],
    };

    /// <summary>Ẩn (soft-delete) View đang chỉnh sửa sau khi xác nhận hợp lệ.</summary>
    /// <remarks>Side-effect: set Is_Active=0 trên DB rồi reload list + về trạng thái tạo mới.</remarks>
    private async Task ExecuteDeactivateAsync()
    {
        if (!EditViewId.HasValue || _viewData is null || _appConfig is null) return;
        IsBusy = true;
        try
        {
            await _viewData.DeactivateViewAsync(EditViewId.Value, _appConfig.TenantId);
            SaveStatusMessage = $"Đã ẩn View_Id={EditViewId.Value}.";
            IsSaveStatusError = false;
            ExecuteNew();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "ViewManager.Deactivate");
            SetSaveError($"Lỗi ẩn View: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    /// <summary>Đặt thông báo lỗi lưu.</summary>
    /// <param name="message">Nội dung lỗi.</param>
    private void SetSaveError(string message)
    {
        SaveStatusMessage = message;
        IsSaveStatusError = true;
    }

    // ── VIEW-4d: i18n key + column picker ──────────────────────

    /// <summary>
    /// Dựng key i18n theo convention <c>{tableCode}.view.{viewCode}.{suffix}</c> (spec 10 §1d).
    /// </summary>
    /// <param name="suffix">Hậu tố key (vd "title", "col.ma.caption", "action.add.label").</param>
    /// <returns>Key đầy đủ, hoặc null nếu chưa đủ Table_Code + View_Code.</returns>
    private string? BuildViewKey(string suffix)
    {
        var table = EditTable?.TableCode?.Trim().ToLowerInvariant();
        var view = EditViewCode.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(view)) return null;
        return $"{table}.view.{view}.{suffix}";
    }

    /// <summary>
    /// Khi View_Code đổi, thay đoạn <c>.view.{cũ}.</c> → <c>.view.{mới}.</c> trong mọi key i18n
    /// đã sinh (Title/Export + Caption/Label/Tooltip/Confirm của cột &amp; action) — không để key trỏ View_Code cũ.
    /// </summary>
    /// <param name="oldCode">View_Code trước khi đổi.</param>
    /// <param name="newCode">View_Code sau khi đổi.</param>
    private void RekeyForViewCodeChange(string oldCode, string newCode)
    {
        if (_suppressRekey) return;
        var oldC = oldCode.Trim().ToLowerInvariant();
        var newC = newCode.Trim().ToLowerInvariant();
        if (oldC.Length == 0 || oldC == newC) return;

        var from = $".view.{oldC}.";
        var to = $".view.{newC}.";

        EditTitleKey = SwapSegment(EditTitleKey, from, to) ?? "";
        EditExportFileNameKey = SwapSegment(EditExportFileNameKey, from, to) ?? "";
        EditSearchLabelKey = SwapSegment(EditSearchLabelKey, from, to) ?? "";
        EditResetLabelKey = SwapSegment(EditResetLabelKey, from, to) ?? "";

        foreach (var f in EditFilters)
        {
            f.LabelKey = SwapSegment(f.LabelKey, from, to);
            f.PlaceholderKey = SwapSegment(f.PlaceholderKey, from, to);
            f.TooltipKey = SwapSegment(f.TooltipKey, from, to);
        }

        foreach (var c in EditColumns)
        {
            c.CaptionKey = SwapSegment(c.CaptionKey, from, to);
            c.ExportCaptionKey = SwapSegment(c.ExportCaptionKey, from, to);
            c.CellTemplateKey = SwapSegment(c.CellTemplateKey, from, to);
        }
        foreach (var a in EditActions)
        {
            a.LabelKey = SwapSegment(a.LabelKey, from, to);
            a.TooltipKey = SwapSegment(a.TooltipKey, from, to);
            a.ConfirmKey = SwapSegment(a.ConfirmKey, from, to);
        }
    }

    /// <summary>Thay đoạn <paramref name="from"/> bằng <paramref name="to"/> nếu key có chứa; giữ nguyên null/rỗng.</summary>
    private static string? SwapSegment(string? value, string from, string to)
        => string.IsNullOrEmpty(value) ? value
           : value!.Replace(from, to, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Mở popup <see cref="ViewNames.I18nEditorDialog"/> cho một resource key.
    /// Popup tự lưu Sys_Resource mọi ngôn ngữ; callback nhận bản dịch ngôn ngữ mặc định.
    /// </summary>
    /// <param name="key">Resource key cần dịch (đã đảm bảo không rỗng).</param>
    /// <param name="contextLabel">Nhãn ngữ cảnh hiển thị trên header popup.</param>
    /// <param name="onSaved">Callback cập nhật preview sau khi lưu (optional).</param>
    private void OpenI18nDialog(string key, string contextLabel, Action<string>? onSaved = null)
    {
        if (_dialogService is null || string.IsNullOrWhiteSpace(key)) return;

        var p = new DialogParameters
        {
            { "key", key },
            { "contextLabel", contextLabel }
        };
        _dialogService.ShowDialog(ViewNames.I18nEditorDialog, p, result =>
        {
            if (result.Result != ButtonResult.OK) return;
            onSaved?.Invoke(result.Parameters.GetValue<string>("primaryValue") ?? "");
        });
    }

    /// <summary>Dịch Title_Key của View — tự sinh key theo convention nếu đang trống (như màn Form).</summary>
    private void ExecuteOpenTitleI18n()
    {
        if (string.IsNullOrWhiteSpace(EditTitleKey))
        {
            var key = BuildViewKey("title");
            if (key is null) { SetSaveError("Cần View_Code và bảng nguồn trước khi tạo key i18n."); return; }
            EditTitleKey = key;
        }
        OpenI18nDialog(EditTitleKey, "Tiêu đề màn View");
    }

    /// <summary>Dịch Export_File_Name_Key — tự sinh key theo convention nếu đang trống (như màn Form).</summary>
    private void ExecuteOpenExportFileNameI18n()
    {
        if (string.IsNullOrWhiteSpace(EditExportFileNameKey))
        {
            var key = BuildViewKey("export.filename");
            if (key is null) { SetSaveError("Cần View_Code và bảng nguồn trước khi tạo key i18n."); return; }
            EditExportFileNameKey = key;
        }
        OpenI18nDialog(EditExportFileNameKey, "Tên file xuất");
    }

    /// <summary>Dịch Caption_Key của cột đang chọn (nút toolbar).</summary>
    private void ExecuteOpenColumnCaptionI18n() => OpenColumnCaptionI18nForRow(SelectedColumn);

    /// <summary>Dịch Label_Key của action đang chọn (nút toolbar).</summary>
    private void ExecuteOpenActionLabelI18n() => OpenActionLabelI18nForRow(SelectedAction);

    /// <summary>Dịch Caption_Key của một cột — tự sinh key từ Field_Name nếu đang trống (như màn Form).</summary>
    /// <param name="col">Dòng cột (từ nút 🌐 inline hoặc cột đang chọn).</param>
    private void OpenColumnCaptionI18nForRow(ViewColumnRecord? col)
    {
        if (col is null) return;
        if (string.IsNullOrWhiteSpace(col.CaptionKey))
        {
            var field = col.FieldName?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(field)) { SetSaveError("Nhập Field_Name của cột trước khi tạo key caption."); return; }
            var key = BuildViewKey($"col.{field}.caption");
            if (key is null) { SetSaveError("Cần View_Code và bảng nguồn trước khi tạo key i18n."); return; }
            col.CaptionKey = key;
        }
        OpenI18nDialog(col.CaptionKey!, $"Caption cột {col.FieldName}");
    }

    /// <summary>Dịch Label_Key của một action — tự sinh key từ Action_Code nếu đang trống (như màn Form).</summary>
    /// <param name="act">Dòng action (từ nút 🌐 inline hoặc action đang chọn).</param>
    private void OpenActionLabelI18nForRow(ViewActionRecord? act)
    {
        if (act is null) return;
        if (string.IsNullOrWhiteSpace(act.LabelKey))
        {
            var code = act.ActionCode?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(code)) { SetSaveError("Nhập Action_Code trước khi tạo key nhãn."); return; }
            var key = BuildViewKey($"action.{code}.label");
            if (key is null) { SetSaveError("Cần View_Code và bảng nguồn trước khi tạo key i18n."); return; }
            act.LabelKey = key;
        }
        OpenI18nDialog(act.LabelKey!, $"Nhãn action {act.ActionCode}");
    }

    /// <summary>
    /// Nạp danh sách cột của nguồn (1 lần / khóa nguồn). Rẽ nhánh theo Source_Type:
    /// <list type="bullet">
    /// <item>Table → đọc Sys_Column trong Config DB (cột đã sync), Column_Id thật.</item>
    /// <item>View/SP → đọc cấu trúc trực tiếp từ Target DB (View qua INFORMATION_SCHEMA,
    ///       SP qua describe_first_result_set); Column_Id = 0 ⇒ lưu NULL (unbound).</item>
    /// </list>
    /// Ném <see cref="InvalidOperationException"/> khi nguồn View/SP mà chưa cấu hình Target DB
    /// hoặc đối tượng không trả về cột nào.
    /// </summary>
    private async Task EnsureColumnsLoadedAsync()
    {
        if (EditTable is null) return;

        var key = $"{EditSourceType}|{EditTable.TableId}|{EditSourceObject}";
        if (_columnsLoadedKey == key && AvailableColumns.Count > 0) return;

        var isDbObject = string.Equals(EditSourceType, "View", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(EditSourceType, "Sp", StringComparison.OrdinalIgnoreCase);

        AvailableColumns.Clear();

        if (isDbObject)
        {
            // ── View/SP: cấu trúc KHÔNG nằm trong Sys_Column → đọc thẳng Target DB ──
            if (_schemaInspector is null || _appConfig is null) return;
            if (!_appConfig.IsTargetConfigured
             || string.IsNullOrWhiteSpace(_appConfig.TargetConnectionString))
                throw new InvalidOperationException(
                    "Chưa cấu hình Target DB. Vào Settings → Target Database để đọc cột của View/SP.");

            var (schema, name) = ResolveSourceObject();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var isSp = string.Equals(EditSourceType, "Sp", StringComparison.OrdinalIgnoreCase);
            var cols = isSp
                ? await _schemaInspector.GetProcedureColumnsAsync(
                    _appConfig.TargetConnectionString, schema, name, cts.Token)
                : await _schemaInspector.GetColumnsAsync(
                    _appConfig.TargetConnectionString, schema, name, cts.Token);

            if (cols.Count == 0)
                throw new InvalidOperationException(
                    $"Không đọc được cột của {EditSourceType} '{schema}.{name}' từ Target DB " +
                    "(không tồn tại hoặc không xác định được result-set).");

            foreach (var c in cols)
                AvailableColumns.Add(new ColumnInfoDto
                {
                    ColumnId = 0,               // View/SP không có Sys_Column → Column_Id = NULL khi lưu
                    ColumnCode = c.ColumnName,
                    DataType = c.DataType,
                    NetType = c.NetType,
                    MaxLength = c.MaxLength,
                    IsNullable = c.IsNullable
                });
        }
        else
        {
            // ── Table: cột lấy từ Sys_Column (Config DB) như cũ ──
            if (_fieldData is null) return;

            var cols = await _fieldData.GetColumnsByTableAsync(EditTable.TableId);
            foreach (var c in cols)
                AvailableColumns.Add(new ColumnInfoDto
                {
                    ColumnId = c.ColumnId,
                    ColumnCode = c.ColumnCode,
                    DataType = c.DataType,
                    NetType = c.NetType,
                    MaxLength = c.MaxLength,
                    IsNullable = c.IsNullable
                });
        }

        _columnsLoadedKey = key;
    }

    /// <summary>
    /// Xác định (schema, tên đối tượng) để đọc cột cho nguồn View/SP.
    /// Ưu tiên Source_Object (nếu nhập, hỗ trợ dạng "schema.object"); nếu trống dùng Table_Code.
    /// Schema mặc định lấy theo Sys_Table (fallback "dbo").
    /// </summary>
    /// <returns>Cặp (schema, tên đối tượng) đã bỏ ngoặc vuông.</returns>
    private (string Schema, string Name) ResolveSourceObject()
    {
        var raw = string.IsNullOrWhiteSpace(EditSourceObject)
            ? EditTable!.TableCode
            : EditSourceObject.Trim();
        var schema = string.IsNullOrWhiteSpace(EditTable!.SchemaName) ? "dbo" : EditTable.SchemaName.Trim();

        var dot = raw.IndexOf('.');
        if (dot > 0)
        {
            schema = raw[..dot].Trim('[', ']', ' ');
            raw = raw[(dot + 1)..];
        }
        return (schema, raw.Trim('[', ']', ' '));
    }

    /// <summary>
    /// Mở column picker chọn cột từ Sys_Column; gán vào cột đang chọn (hoặc tạo dòng mới).
    /// </summary>
    /// <remarks>Side-effect: nạp lười AvailableColumns; set FieldName + ColumnId của cột.</remarks>
    private async Task ExecuteBrowseColumnAsync()
    {
        if (_dialogService is null) return;
        if (EditTable is null) { SetSaveError("Chọn bảng nguồn trước khi chọn cột."); return; }

        try { await EnsureColumnsLoadedAsync(); }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "ViewManager.LoadColumns");
            SetSaveError($"Không thể nạp danh sách cột: {ex.Message}");
            return;
        }

        var used = EditColumns
            .Select(c => c.FieldName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        var p = new DialogParameters
        {
            { "columns", AvailableColumns.AsEnumerable() },
            { "multiSelect", true },
            { "usedColumns", used.AsEnumerable() }
        };

        _dialogService.ShowDialog(ViewNames.ColumnPickerDialog, p, result =>
        {
            if (result.Result != ButtonResult.OK) return;
            if (!result.Parameters.TryGetValue("selectedColumns", out List<ColumnInfoDto>? cols) || cols is null) return;

            ViewColumnRecord? last = null;
            foreach (var col in cols)
            {
                // An toàn: bỏ qua cột đã có (picker vốn đã khóa, phòng hờ trùng).
                if (EditColumns.Any(c => string.Equals(c.FieldName, col.ColumnCode, StringComparison.OrdinalIgnoreCase)))
                    continue;
                var row = new ViewColumnRecord
                {
                    OrderNo = EditColumns.Count,
                    FieldName = col.ColumnCode,
                    // View/SP không có Sys_Column (ColumnId=0) → lưu NULL tránh vi phạm FK_Ui_View_Column_Column
                    ColumnId = col.ColumnId > 0 ? col.ColumnId : (int?)null
                };
                EditColumns.Add(row);
                last = row;
            }
            if (last is not null) SelectedColumn = last;
        });
    }

    /// <summary>Đảm bảo appsettings đã được nạp trước khi gọi DB.</summary>
    private async Task EnsureAppConfigLoadedAsync()
    {
        if (_appConfig is null || _appConfig.IsConfigured) return;
        await _appConfig.LoadAsync();
    }
}
