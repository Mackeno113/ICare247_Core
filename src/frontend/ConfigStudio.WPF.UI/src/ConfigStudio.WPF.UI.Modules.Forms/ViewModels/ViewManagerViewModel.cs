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
using Prism.Commands;
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
    private readonly IAppConfigService? _appConfig;
    private readonly INavigationHistoryService? _history;
    private readonly IAppLogger? _logger;
    private bool _isProgrammaticSelect;

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

    public ObservableCollection<TableLookupRecord> Tables { get; } = [];
    public ObservableCollection<FormRecord> Forms { get; } = [];

    // ── Master list ────────────────────────────────────────────
    public ObservableCollection<ViewRecord> Views { get; } = [];
    public ICollectionView ViewsView { get; }

    public ObservableCollection<ViewColumnRecord> EditColumns { get; } = [];
    public ObservableCollection<ViewActionRecord> EditActions { get; } = [];

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
                RemoveActionCommand.RaiseCanExecuteChanged();
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

    private string _editViewCode = "";
    public string EditViewCode
    {
        get => _editViewCode;
        set { if (SetProperty(ref _editViewCode, value)) SaveCommand.RaiseCanExecuteChanged(); }
    }

    private string _editViewType = "Grid";
    public string EditViewType
    {
        get => _editViewType;
        set { if (SetProperty(ref _editViewType, value)) RaisePropertyChanged(nameof(IsTreeList)); }
    }

    /// <summary>Hiện card cấu hình cây khi View_Type = TreeList.</summary>
    public bool IsTreeList => string.Equals(_editViewType, "TreeList", StringComparison.OrdinalIgnoreCase);

    private TableLookupRecord? _editTable;
    public TableLookupRecord? EditTable
    {
        get => _editTable;
        set { if (SetProperty(ref _editTable, value)) SaveCommand.RaiseCanExecuteChanged(); }
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

    private bool _editIsActive = true;
    public bool EditIsActive { get => _editIsActive; set => SetProperty(ref _editIsActive, value); }

    private string _editDescription = "";
    public string EditDescription { get => _editDescription; set => SetProperty(ref _editDescription, value); }

    // ── State ──────────────────────────────────────────────────
    public int TenantId => _appConfig?.TenantId ?? 0;
    public bool IsEditMode => EditViewId.HasValue;
    public string SaveButtonText => IsEditMode ? "Cập nhật View" : "Tạo View";
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

    /// <summary>
    /// Khởi tạo ViewModel + thiết lập CollectionView (filter) và command.
    /// </summary>
    /// <param name="viewData">Service truy vấn Ui_View.</param>
    /// <param name="formData">Service tra cứu Sys_Table + Ui_Form cho dropdown.</param>
    /// <param name="appConfig">Cấu hình DB + tenant.</param>
    /// <param name="history">Lịch sử điều hướng (breadcrumb).</param>
    /// <param name="logger">Ghi log lỗi.</param>
    public ViewManagerViewModel(
        IViewDataService? viewData = null,
        IFormDataService? formData = null,
        IAppConfigService? appConfig = null,
        INavigationHistoryService? history = null,
        IAppLogger? logger = null)
    {
        _viewData = viewData;
        _formData = formData;
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
        => IsNotBusy && !string.IsNullOrWhiteSpace(EditViewCode) && EditTable is not null;

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
        var h = detail.Header;
        EditViewId = h.ViewId;
        _editVersion = h.Version;
        EditViewCode = h.ViewCode;
        EditViewType = h.ViewType;
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
        EditIsActive = h.IsActive;
        EditDescription = h.Description ?? "";

        EditColumns.Clear();
        foreach (var c in detail.Columns) EditColumns.Add(c);
        EditActions.Clear();
        foreach (var a in detail.Actions) EditActions.Add(a);

        RaiseEditorState();
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
        _isProgrammaticSelect = true;
        SelectedView = null;
        _isProgrammaticSelect = false;
        ResetEditor();
        SaveStatusMessage = "";
        IsSaveStatusError = false;
    }

    /// <summary>Reset toàn bộ field editor về mặc định cho View mới.</summary>
    private void ResetEditor()
    {
        EditViewId = null;
        _editVersion = 1;
        EditViewCode = "";
        EditViewType = "Grid";
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
        EditIsActive = true;
        EditDescription = "";
        EditColumns.Clear();
        EditActions.Clear();
        RaiseEditorState();
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
        IsActive = EditIsActive,
        Description = EditDescription,
        Columns = [.. EditColumns],
        Actions = [.. EditActions],
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

    /// <summary>Đảm bảo appsettings đã được nạp trước khi gọi DB.</summary>
    private async Task EnsureAppConfigLoadedAsync()
    {
        if (_appConfig is null || _appConfig.IsConfigured) return;
        await _appConfig.LoadAsync();
    }
}
