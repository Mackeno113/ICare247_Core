// File    : FormEditorViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho màn hình Form Editor (Screen 03) — quản lý toàn bộ form: metadata, sections, fields, events, permissions.

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.Services;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// ViewModel cho màn hình Form Editor (Screen 03).
/// Gộp chức năng FormEditDialog vào đây — một nơi duy nhất quản lý:
/// - Metadata form (Tab Thông tin): FormCode, FormName, Platform, LayoutEngine, Description, IsActive...
/// - Sections &amp; Fields (TreeView bên trái + Property Panel)
/// - Events (Tab Events)
/// - Permissions (Tab Permissions)
/// Khi formId=0 → chế độ tạo form mới (IsNewForm=true).
/// </summary>
public sealed class FormEditorViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;
    private readonly IFormDataService? _formDataService;
    private readonly IFormDetailDataService? _detailService;
    private readonly IAppConfigService? _appConfig;
    private CancellationTokenSource? _formCodeValidationCts;
    private CancellationTokenSource _loadCts = new();
    private static readonly Regex FormCodeRegex = new(@"^[A-Z0-9_]+$", RegexOptions.Compiled);
    private string _originalFormCode = "";

    // ── P0 UX Services ────────────────────────────────────────
    private AutoSaveService? _autoSave;
    private readonly UndoRedoService<string> _undoRedo = new();
    private LintingService? _linting;

    // ── Auto-save status ──────────────────────────────────────
    private AutoSaveStatus _autoSaveStatus = AutoSaveStatus.Idle;
    /// <summary>Trạng thái auto-save hiện tại.</summary>
    public AutoSaveStatus AutoSaveStatus
    {
        get => _autoSaveStatus;
        private set => SetProperty(ref _autoSaveStatus, value);
    }

    private DateTime? _lastSavedAt;
    /// <summary>Thời gian auto-save gần nhất.</summary>
    public DateTime? LastSavedAt
    {
        get => _lastSavedAt;
        private set => SetProperty(ref _lastSavedAt, value);
    }

    private string? _autoSaveError;
    /// <summary>Lỗi auto-save gần nhất (null nếu thành công).</summary>
    public string? AutoSaveError
    {
        get => _autoSaveError;
        private set => SetProperty(ref _autoSaveError, value);
    }

    // ── Undo/Redo state ───────────────────────────────────────
    private bool _canUndo;
    public bool CanUndoAction { get => _canUndo; private set => SetProperty(ref _canUndo, value); }

    private bool _canRedo;
    public bool CanRedoAction { get => _canRedo; private set => SetProperty(ref _canRedo, value); }

    // ── Lint issues ───────────────────────────────────────────
    private IReadOnlyList<LintIssue> _lintIssues = [];
    /// <summary>Danh sách lint issues hiện tại.</summary>
    public IReadOnlyList<LintIssue> LintIssues
    {
        get => _lintIssues;
        private set
        {
            if (SetProperty(ref _lintIssues, value))
            {
                RaisePropertyChanged(nameof(LintErrorCount));
                RaisePropertyChanged(nameof(LintWarningCount));
                RaisePropertyChanged(nameof(HasLintErrors));
            }
        }
    }

    public int LintErrorCount => _lintIssues.Count(i => i.Severity == "error");
    public int LintWarningCount => _lintIssues.Count(i => i.Severity == "warning");
    public bool HasLintErrors => _lintIssues.Any(i => i.Severity == "error");

    // ── Form info ─────────────────────────────────────────────
    private int _formId;
    public int FormId { get => _formId; set => SetProperty(ref _formId, value); }

    private string _formCode = "";
    public string FormCode
    {
        get => _formCode;
        set
        {
            // NOTE: tự động uppercase khi nhập
            var upper = value?.ToUpperInvariant() ?? "";
            if (SetProperty(ref _formCode, upper))
            {
                IsDirty = true;
                ValidateFormCodeFormat();
                _ = ValidateFormCodeRealtimeAsync();
                RaisePropertyChanged(nameof(CanCreateNewForm));
            }
        }
    }

    private string _formName = "";
    public string FormName
    {
        get => _formName;
        set
        {
            if (SetProperty(ref _formName, value))
            {
                IsDirty = true;
                RaisePropertyChanged(nameof(CanCreateNewForm));
            }
        }
    }

    private int _version = 1;
    public int Version { get => _version; set => SetProperty(ref _version, value); }

    private string _platform = "web";
    public string Platform
    {
        get => _platform;
        set { if (SetProperty(ref _platform, value)) IsDirty = true; }
    }

    private TableLookupRecord? _selectedTable;
    public TableLookupRecord? SelectedTable
    {
        get => _selectedTable;
        set
        {
            if (SetProperty(ref _selectedTable, value))
            {
                IsDirty = true;
                RaisePropertyChanged(nameof(CanCreateNewForm));
            }
        }
    }

    private string _layoutEngine = "Grid";
    public string LayoutEngine
    {
        get => _layoutEngine;
        set { if (SetProperty(ref _layoutEngine, value)) IsDirty = true; }
    }

    private string _description = "";
    public string Description
    {
        get => _description;
        set { if (SetProperty(ref _description, value)) IsDirty = true; }
    }

    private bool _isFormActive = true;
    /// <summary>Is_Active của form — dùng tên IsFormActive để tránh trùng với FrameworkElement.IsActive.</summary>
    public bool IsFormActive
    {
        get => _isFormActive;
        set { if (SetProperty(ref _isFormActive, value)) IsDirty = true; }
    }

    private string _checksum = "";
    /// <summary>Checksum — readonly, system tự tính.</summary>
    public string Checksum
    {
        get => _checksum;
        private set => SetProperty(ref _checksum, value);
    }

    // ── Lookups ───────────────────────────────────────────────
    /// <summary>Danh sách bảng Sys_Table dùng cho lookup chọn Table_Id.</summary>
    public ObservableCollection<TableLookupRecord> TableLookupItems { get; } = [];
    public List<string> PlatformOptions { get; } = ["web", "mobile", "wpf"];
    public List<string> LayoutEngineOptions { get; } = ["Grid", "Flex", "Custom"];

    // ── FormCode validation ───────────────────────────────────
    private string _formCodeError = "";
    public string FormCodeError
    {
        get => _formCodeError;
        private set
        {
            if (SetProperty(ref _formCodeError, value))
                RaisePropertyChanged(nameof(HasFormCodeError));
        }
    }

    public bool HasFormCodeError => !string.IsNullOrEmpty(_formCodeError);

    private bool _isCheckingFormCode;
    /// <summary>True khi đang debounce hoặc đang query kiểm tra trùng mã form.</summary>
    public bool IsCheckingFormCode
    {
        get => _isCheckingFormCode;
        private set
        {
            if (SetProperty(ref _isCheckingFormCode, value))
                RaisePropertyChanged(nameof(CanCreateNewForm));
        }
    }

    private bool _isFormCodeDuplicate;
    /// <summary>True khi <see cref="FormCode"/> đã tồn tại trong tenant hiện tại.</summary>
    public bool IsFormCodeDuplicate
    {
        get => _isFormCodeDuplicate;
        private set
        {
            if (SetProperty(ref _isFormCodeDuplicate, value))
                RaisePropertyChanged(nameof(CanCreateNewForm));
        }
    }

    private string _formCodeValidationMessage = "";
    /// <summary>Thông điệp trạng thái kiểm tra mã form realtime hiển thị dưới ô nhập.</summary>
    public string FormCodeValidationMessage
    {
        get => _formCodeValidationMessage;
        private set
        {
            if (SetProperty(ref _formCodeValidationMessage, value))
                RaisePropertyChanged(nameof(HasFormCodeValidationMessage));
        }
    }

    public bool HasFormCodeValidationMessage => !string.IsNullOrWhiteSpace(_formCodeValidationMessage);

    // ── Tree structure ────────────────────────────────────────
    /// <summary>Danh sách sections (root nodes) của form.</summary>
    public ObservableCollection<FormTreeNode> Sections { get; } = [];

    private FormTreeNode? _selectedNode;
    /// <summary>Node đang được chọn trong TreeView.</summary>
    public FormTreeNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            var old = _selectedNode;
            if (SetProperty(ref _selectedNode, value))
            {
                // Hủy subscribe node cũ — tránh memory leak và IsDirty bị trigger sai
                if (old is not null)
                    old.PropertyChanged -= OnSelectedNodePropertyChanged;

                // Subscribe node mới để detect khi user sửa property trực tiếp trong panel
                if (_selectedNode is not null)
                    _selectedNode.PropertyChanged += OnSelectedNodePropertyChanged;

                RaisePropertyChanged(nameof(IsNodeSelected));
                RaisePropertyChanged(nameof(IsFieldSelected));
                RaisePropertyChanged(nameof(IsSectionSelected));
                DeleteNodeCommand.RaiseCanExecuteChanged();
                MoveUpCommand.RaiseCanExecuteChanged();
                MoveDownCommand.RaiseCanExecuteChanged();
                OpenFieldConfigCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Khi property của node đang chọn thay đổi → đánh dấu form có thay đổi chưa lưu.
    /// Bỏ qua IsExpanded / IsSelected / IsActive vì đây là trạng thái UI, không phải dữ liệu form.
    /// </summary>
    private void OnSelectedNodePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FormTreeNode.IsExpanded)
                            or nameof(FormTreeNode.IsSelected)
                            or nameof(FormTreeNode.IsActive))
            return;

        // Push undo state trước khi đánh dấu dirty
        PushUndoState($"Sửa {e.PropertyName}");
        IsDirty = true;

        // Notify auto-save + linting
        _autoSave?.NotifyDirty();
        _linting?.NotifyChanged();
    }

    public bool IsNodeSelected => SelectedNode is not null;
    public bool IsFieldSelected => SelectedNode?.NodeType == FormNodeType.Field;
    public bool IsSectionSelected => SelectedNode?.NodeType == FormNodeType.Section;

    // ── Events ────────────────────────────────────────────────
    public ObservableCollection<EventSummaryDto> Events { get; } = [];

    private EventSummaryDto? _selectedEvent;
    public EventSummaryDto? SelectedEvent
    {
        get => _selectedEvent;
        set
        {
            if (SetProperty(ref _selectedEvent, value))
            {
                RemoveEventCommand.RaiseCanExecuteChanged();
                EditEventCommand.RaiseCanExecuteChanged();
            }
        }
    }

    // ── Permissions ───────────────────────────────────────────
    public ObservableCollection<FormPermissionRow> Permissions { get; } = [];

    // ── New Form mode ─────────────────────────────────────────
    private bool _isNewForm;
    /// <summary>True khi formId=0 — hiện panel tạo form mới thay vì tree editor.</summary>
    public bool IsNewForm
    {
        get => _isNewForm;
        private set
        {
            SetProperty(ref _isNewForm, value);
        }
    }

    /// <summary>
    /// Cho phép tạo mới khi không loading, không trùng mã và đủ input bắt buộc.
    /// </summary>
    public bool CanCreateNewForm =>
        IsNotLoading
        && !IsCheckingFormCode
        && !HasFormCodeError
        && !string.IsNullOrWhiteSpace(FormCode)
        && !string.IsNullOrWhiteSpace(FormName)
        && SelectedTable is not null;

    private string _createErrorMessage = "";
    public string CreateErrorMessage
    {
        get => _createErrorMessage;
        set
        {
            if (SetProperty(ref _createErrorMessage, value))
                RaisePropertyChanged(nameof(HasCreateError));
        }
    }

    public bool HasCreateError => !string.IsNullOrEmpty(_createErrorMessage);

    // ── State ─────────────────────────────────────────────────
    private int _activeTabIndex = 1;
    /// <summary>Index tab đang chọn trong right panel: 0=Thông tin Form, 1=Thuộc tính, 2=Events, 3=Permissions.</summary>
    public int ActiveTabIndex { get => _activeTabIndex; set => SetProperty(ref _activeTabIndex, value); }

    private bool _isDirty;
    public bool IsDirty { get => _isDirty; set => SetProperty(ref _isDirty, value); }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                RaisePropertyChanged(nameof(IsNotLoading));
                RaisePropertyChanged(nameof(CanCreateNewForm));
            }
        }
    }

    public bool IsNotLoading => !_isLoading;

    private string _errorMessage = "";
    /// <summary>Thông báo lỗi khi load DB thất bại (edit mode). Rỗng = không có lỗi.</summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
                RaisePropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(_errorMessage);


    private string _searchText = "";
    /// <summary>Text tìm kiếm để filter tree.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    // ── Statistics ─────────────────────────────────────────────
    public int TotalSections => Sections.Count;
    public int TotalFields => Sections.Sum(s => s.Children.Count);

    // ── Commands ──────────────────────────────────────────────
    // Tree manipulation
    public DelegateCommand AddSectionCommand { get; }
    public DelegateCommand AddFieldCommand { get; }
    public DelegateCommand DeleteNodeCommand { get; }
    public DelegateCommand MoveUpCommand { get; }
    public DelegateCommand MoveDownCommand { get; }
    public DelegateCommand OpenFieldConfigCommand { get; }

    // Form actions
    public DelegateCommand SaveFormCommand { get; }
    public DelegateCommand PublishCommand { get; }
    public DelegateCommand ViewDependenciesCommand { get; }
    public DelegateCommand BackToListCommand { get; }
    public DelegateCommand ExpandAllCommand { get; }
    public DelegateCommand CollapseAllCommand { get; }
    public DelegateCommand CreateNewFormCommand { get; }

    // Events
    public DelegateCommand AddEventCommand { get; }
    public DelegateCommand RemoveEventCommand { get; }
    public DelegateCommand EditEventCommand { get; }

    // Undo/Redo
    public DelegateCommand UndoCommand { get; }
    public DelegateCommand RedoCommand { get; }

    // Permissions — dùng để đánh dấu IsDirty khi checkbox thay đổi
    public DelegateCommand DirtyCommand { get; }

    public FormEditorViewModel(
        IRegionManager regionManager,
        IFormDataService? formDataService = null,
        IFormDetailDataService? detailService = null,
        IAppConfigService? appConfig = null)
    {
        _regionManager   = regionManager;
        _formDataService = formDataService;
        _detailService   = detailService;
        _appConfig       = appConfig;

        // Tree manipulation
        AddSectionCommand = new DelegateCommand(ExecuteAddSection);
        AddFieldCommand = new DelegateCommand(ExecuteAddField);
        DeleteNodeCommand = new DelegateCommand(ExecuteDeleteNode, () => IsNodeSelected);
        MoveUpCommand = new DelegateCommand(ExecuteMoveUp, () => IsNodeSelected);
        MoveDownCommand = new DelegateCommand(ExecuteMoveDown, () => IsNodeSelected);
        OpenFieldConfigCommand = new DelegateCommand(ExecuteOpenFieldConfig, () => IsFieldSelected);

        // Form actions
        SaveFormCommand = new DelegateCommand(ExecuteSave, () => IsDirty)
            .ObservesProperty(() => IsDirty);
        PublishCommand = new DelegateCommand(ExecutePublish);
        ViewDependenciesCommand = new DelegateCommand(ExecuteViewDependencies);
        BackToListCommand = new DelegateCommand(ExecuteBackToList);
        ExpandAllCommand = new DelegateCommand(() => SetExpandAll(true));
        CollapseAllCommand = new DelegateCommand(() => SetExpandAll(false));
        CreateNewFormCommand = new DelegateCommand(async () => await ExecuteCreateNewFormAsync());

        // Events
        AddEventCommand = new DelegateCommand(ExecuteAddEvent);
        RemoveEventCommand = new DelegateCommand(ExecuteRemoveEvent, () => SelectedEvent is not null);
        EditEventCommand = new DelegateCommand(ExecuteEditEvent, () => SelectedEvent is not null);

        // Undo/Redo
        UndoCommand = new DelegateCommand(ExecuteUndo, () => CanUndoAction)
            .ObservesProperty(() => CanUndoAction);
        RedoCommand = new DelegateCommand(ExecuteRedo, () => CanRedoAction)
            .ObservesProperty(() => CanRedoAction);

        // Permissions
        DirtyCommand = new DelegateCommand(() => IsDirty = true);

        // ── Wire undo/redo state changed ─────────────────────
        _undoRedo.StateChanged += (_, _) =>
        {
            CanUndoAction = _undoRedo.CanUndo;
            CanRedoAction = _undoRedo.CanRedo;
        };
    }

    // ── INavigationAware ─────────────────────────────────────

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        InitP0Services();
        FormId = navigationContext.Parameters.GetValue<int>("formId");

        // ── Phân nhánh: tạo mới hay edit ────────────────────
        if (FormId == 0)
        {
            // Chế độ tạo form mới — dùng chung tab "Thông tin Form"
            IsNewForm          = true;
            _originalFormCode  = "";
            FormCode           = "";
            FormName           = "";
            Platform           = "web";
            LayoutEngine       = "Grid";
            Description        = "";
            IsFormActive       = true;
            SelectedTable      = null;
            Version            = 1;
            Checksum           = "";
            CreateErrorMessage = "";
            FormCodeError      = "";
            IsCheckingFormCode = false;
            IsFormCodeDuplicate = false;
            Sections.Clear();
            Events.Clear();
            LoadDefaultPermissions();
            ActiveTabIndex     = 0; // Mở tab "Thông tin Form"
            IsDirty            = false;
            _ = LoadTableLookupSafeAsync();
        }
        else
        {
            IsNewForm = false;
            _originalFormCode = navigationContext.Parameters.GetValue<string>("formCode") ?? "";

            // NOTE: activeTab param cho phép mở đúng tab khi navigate từ EditFormCommand (tab 0 = Thông tin Form)
            ActiveTabIndex = navigationContext.Parameters.ContainsKey("activeTab")
                ? navigationContext.Parameters.GetValue<int>("activeTab")
                : 1; // Mặc định tab "Thuộc tính"

            _ = LoadFromDatabaseAsync();
        }
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => false;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        // Hủy subscribe node đang chọn để tránh memory leak khi navigate ra ngoài
        if (_selectedNode is not null)
            _selectedNode.PropertyChanged -= OnSelectedNodePropertyChanged;

        _formCodeValidationCts?.Cancel();
        _formCodeValidationCts?.Dispose();
        _formCodeValidationCts = null;

        // Hủy load DB nếu đang chạy
        _loadCts.Cancel();
        _loadCts = new CancellationTokenSource();

        DisposeP0Services();
    }

    // ── Load data from DB (edit mode) ────────────────────────

    /// <summary>
    /// Load toàn bộ dữ liệu form từ DB theo FormId.
    /// Dùng IFormDetailDataService cho header/sections/fields/events,
    /// IFormDataService cho table lookup.
    /// </summary>
    private async Task LoadFromDatabaseAsync()
    {
        IsLoading    = true;
        ErrorMessage = "";
        try
        {
            if (_detailService is null || _appConfig is not { IsConfigured: true })
            {
                ErrorMessage = "Chưa cấu hình kết nối DB. Vào Settings để nhập Connection String.";
                return;
            }

            await EnsureAppConfigLoadedAsync();
            var ct        = _loadCts.Token;
            var tenantId  = _appConfig.TenantId;

            // ── 1. Header ────────────────────────────────────
            var detail = await _detailService.GetFormDetailAsync(FormId, tenantId, ct);
            if (detail is null)
            {
                ErrorMessage = $"Không tìm thấy form với Id={FormId} trong DB.";
                return;
            }

            FormCode     = detail.FormCode;
            FormName     = detail.FormCode; // DB không có FormName riêng — dùng FormCode
            Platform     = detail.Platform;
            LayoutEngine = detail.LayoutEngine;
            Description  = detail.Description ?? "";
            Version      = detail.Version;
            Checksum     = detail.Checksum ?? "";
            IsFormActive = detail.IsActive;
            _originalFormCode = detail.FormCode;

            // ── 2. Table lookup + chọn đúng table ────────────
            await LoadTableLookupSafeAsync();
            SelectedTable = TableLookupItems.FirstOrDefault(t => t.TableId == detail.TableId)
                            ?? TableLookupItems.FirstOrDefault();

            // ── 3. Sections + Fields → build tree ─────────────
            var sectionRecords = await _detailService.GetSectionsByFormAsync(FormId, tenantId, ct);
            var fieldRecords   = await _detailService.GetFieldsByFormAsync(FormId, tenantId, ct);

            // Index fields theo SectionCode để group nhanh
            var fieldsBySectionCode = fieldRecords
                .GroupBy(f => f.SectionCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderBy(f => f.OrderNo).ToList(),
                              StringComparer.OrdinalIgnoreCase);

            Sections.Clear();
            foreach (var s in sectionRecords.OrderBy(s => s.OrderNo))
            {
                var sectionNode = new FormTreeNode
                {
                    Id          = s.SectionId,
                    NodeType    = FormNodeType.Section,
                    Code        = s.SectionCode,
                    DisplayName = s.TitleKey ?? s.SectionCode, // TitleKey là i18n key — dùng làm label tạm
                    SortOrder   = s.OrderNo,
                    IsExpanded  = true
                };

                if (fieldsBySectionCode.TryGetValue(s.SectionCode, out var fields))
                {
                    foreach (var f in fields)
                    {
                        sectionNode.Children.Add(new FormTreeNode
                        {
                            Id          = f.FieldId,
                            NodeType    = FormNodeType.Field,
                            Code        = f.ColumnCode,
                            DisplayName = f.ColumnCode,    // DB chưa có Display_Name riêng
                            FieldType   = "text",          // DB không lưu FieldType ở đây
                            EditorType  = f.EditorType,
                            IsRequired  = false,           // load chi tiết khi mở FieldConfig
                            SortOrder   = f.OrderNo
                        });
                    }
                }

                Sections.Add(sectionNode);
            }

            // ── 4. Events ─────────────────────────────────────
            var eventRecords = await _detailService.GetEventsSummaryByFormAsync(FormId, tenantId, ct);
            Events.Clear();
            foreach (var e in eventRecords.OrderBy(e => e.OrderNo))
            {
                Events.Add(new EventSummaryDto
                {
                    EventId          = e.EventId,
                    OrderNo          = e.OrderNo,
                    TriggerCode      = e.TriggerCode,
                    FieldTarget      = e.FieldTarget,
                    ConditionPreview = e.ConditionPreview ?? "",
                    ActionsCount     = e.ActionsCount,
                    IsActive         = e.IsActive
                });
            }

            // ── 5. Permissions — TODO(phase2): load từ Sys_Role ──
            LoadDefaultPermissions();

            RaisePropertyChanged(nameof(TotalSections));
            RaisePropertyChanged(nameof(TotalFields));

            IsDirty       = false;
            FormCodeError = "";
        }
        catch (OperationCanceledException) { /* Navigate away — bỏ qua */ }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi tải dữ liệu form: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Load danh sách table từ DB rồi chọn theo TableId.
    /// Dùng khi navigate vào edit mode sau khi đã có detail.TableId.
    /// </summary>
    private async Task LoadTableLookupAndSelectByIdAsync(int tableId)
    {
        await LoadTableLookupSafeAsync();
        SelectedTable = TableLookupItems.FirstOrDefault(t => t.TableId == tableId)
                        ?? TableLookupItems.FirstOrDefault();
    }

    /// <summary>Load danh sách roles mặc định với quyền form.</summary>
    private void LoadDefaultPermissions()
    {
        Permissions.Clear();
        // TODO(phase2): gọi API GetRoleLookup() để lấy danh sách roles thực từ Sys_Role
        Permissions.Add(new FormPermissionRow { RoleId = 1, RoleName = "Admin",    RoleDescription = "Quản trị hệ thống",           CanRead = true,  CanWrite = true,  CanSubmit = true  });
        Permissions.Add(new FormPermissionRow { RoleId = 2, RoleName = "Manager",  RoleDescription = "Quản lý nghiệp vụ",           CanRead = true,  CanWrite = true,  CanSubmit = true  });
        Permissions.Add(new FormPermissionRow { RoleId = 3, RoleName = "Staff",    RoleDescription = "Nhân viên nhập liệu",         CanRead = true,  CanWrite = true,  CanSubmit = false });
        Permissions.Add(new FormPermissionRow { RoleId = 4, RoleName = "Viewer",   RoleDescription = "Chỉ xem báo cáo",             CanRead = true,  CanWrite = false, CanSubmit = false });
        Permissions.Add(new FormPermissionRow { RoleId = 5, RoleName = "Auditor",  RoleDescription = "Kiểm toán — readonly",        CanRead = true,  CanWrite = false, CanSubmit = false });
        Permissions.Add(new FormPermissionRow { RoleId = 6, RoleName = "External", RoleDescription = "Đối tác / khách hàng ngoài",  CanRead = false, CanWrite = false, CanSubmit = false });
    }

    // ── Filter / Search ──────────────────────────────────────

    /// <summary>
    /// Filter TreeView theo <see cref="SearchText"/>.
    /// Nếu rỗng → hiện tất cả. Nếu có text → ẩn field không match, ẩn section rỗng.
    /// </summary>
    private void ApplyFilter()
    {
        var query = SearchText.Trim();

        foreach (var section in Sections)
        {
            if (string.IsNullOrEmpty(query))
            {
                // NOTE: Reset visibility — hiện tất cả
                section.IsActive = true;
                section.IsExpanded = true;
                foreach (var field in section.Children)
                    field.IsActive = true;
            }
            else
            {
                // NOTE: Filter field theo code hoặc display name
                bool sectionMatch = section.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                                 || section.Code.Contains(query, StringComparison.OrdinalIgnoreCase);

                foreach (var field in section.Children)
                {
                    bool fieldMatch = field.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                                   || field.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                                   || field.EditorType.Contains(query, StringComparison.OrdinalIgnoreCase);
                    field.IsActive = sectionMatch || fieldMatch;
                }

                section.IsActive = sectionMatch || section.Children.Any(f => f.IsActive);
                if (section.IsActive) section.IsExpanded = true;
            }
        }
    }

    // ── Tree manipulation ────────────────────────────────────

    private void ExecuteAddSection()
    {
        var newId = Sections.Count > 0 ? Sections.Max(s => s.Id) + 1 : 1;
        var section = new FormTreeNode
        {
            Id = newId,
            NodeType = FormNodeType.Section,
            Code = $"SEC_NEW_{newId}",
            DisplayName = $"Section Mới {newId}",
            SortOrder = Sections.Count + 1,
            IsExpanded = true
        };
        Sections.Add(section);
        SelectedNode = section;
        PushUndoState("Thêm section");
        IsDirty = true;
        _autoSave?.NotifyDirty();
        _linting?.NotifyChanged();
        RaisePropertyChanged(nameof(TotalSections));
    }

    private void ExecuteAddField()
    {
        // NOTE: Thêm field vào section đang chọn, hoặc section cuối cùng
        var targetSection = SelectedNode?.NodeType == FormNodeType.Section
            ? SelectedNode
            : FindParentSection(SelectedNode);

        targetSection ??= Sections.LastOrDefault();

        if (targetSection is null)
        {
            // NOTE: Chưa có section nào → tạo section trước
            ExecuteAddSection();
            targetSection = Sections.Last();
        }

        var newId = Sections.SelectMany(s => s.Children).DefaultIfEmpty()
            .Max(f => f?.Id ?? 0) + 1;

        var field = new FormTreeNode
        {
            Id = newId,
            NodeType = FormNodeType.Field,
            Code = $"FIELD_NEW_{newId}",
            DisplayName = $"Field Mới {newId}",
            FieldType = "text",
            EditorType = "TextBox",
            SortOrder = targetSection.Children.Count + 1
        };

        targetSection.Children.Add(field);
        targetSection.IsExpanded = true;
        SelectedNode = field;
        PushUndoState("Thêm field");
        IsDirty = true;
        _autoSave?.NotifyDirty();
        _linting?.NotifyChanged();
        RaisePropertyChanged(nameof(TotalFields));
    }

    private void ExecuteDeleteNode()
    {
        if (SelectedNode is null) return;

        if (SelectedNode.NodeType == FormNodeType.Section)
        {
            // TODO(phase2): Confirm dialog trước khi xóa section (kèm tất cả fields)
            Sections.Remove(SelectedNode);
            RaisePropertyChanged(nameof(TotalSections));
            RaisePropertyChanged(nameof(TotalFields));
        }
        else
        {
            var parent = FindParentSection(SelectedNode);
            parent?.Children.Remove(SelectedNode);
            RaisePropertyChanged(nameof(TotalFields));
        }

        SelectedNode = null;
        PushUndoState("Xóa node");
        IsDirty = true;
        _autoSave?.NotifyDirty();
        _linting?.NotifyChanged();
    }

    private void ExecuteMoveUp()
    {
        if (SelectedNode is null) return;

        if (SelectedNode.NodeType == FormNodeType.Section)
        {
            MoveInCollection(Sections, SelectedNode, -1);
        }
        else
        {
            var parent = FindParentSection(SelectedNode);
            if (parent is not null)
                MoveInCollection(parent.Children, SelectedNode, -1);
        }

        ReindexSortOrders();
        IsDirty = true;
    }

    private void ExecuteMoveDown()
    {
        if (SelectedNode is null) return;

        if (SelectedNode.NodeType == FormNodeType.Section)
        {
            MoveInCollection(Sections, SelectedNode, +1);
        }
        else
        {
            var parent = FindParentSection(SelectedNode);
            if (parent is not null)
                MoveInCollection(parent.Children, SelectedNode, +1);
        }

        ReindexSortOrders();
        IsDirty = true;
    }

    private void ExecuteOpenFieldConfig()
    {
        if (SelectedNode is null || SelectedNode.NodeType != FormNodeType.Field) return;

        var parentSection = FindParentSection(SelectedNode);
        var p = new NavigationParameters
        {
            { "fieldId", SelectedNode.Id },
            { "formId", FormId },
            { "sectionId", parentSection?.Id ?? 0 },
            { "mode", "edit" }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FieldConfig, p);
    }

    // ── Events command handlers ──────────────────────────────

    private void ExecuteAddEvent()
    {
        var orderNo = Events.Count + 1;
        var ev = new EventSummaryDto
        {
            EventId          = Events.Count > 0 ? Events.Max(e => e.EventId) + 1 : 1,
            OrderNo          = orderNo,
            TriggerCode      = "OnChange",
            FieldTarget      = "",
            ConditionPreview = "",
            ActionsCount     = 0,
            IsActive         = true
        };
        Events.Add(ev);
        SelectedEvent = ev;
        IsDirty = true;
    }

    private void ExecuteRemoveEvent()
    {
        if (SelectedEvent is null) return;
        Events.Remove(SelectedEvent);
        SelectedEvent = Events.FirstOrDefault();
        IsDirty = true;
    }

    private void ExecuteEditEvent()
    {
        if (SelectedEvent is null) return;
        var p = new NavigationParameters { { "eventId", SelectedEvent.EventId } };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.EventEditor, p);
    }

    // ── FormCode validation ──────────────────────────────────

    /// <summary>Validate format mã form (regex: A-Z, 0-9, _).</summary>
    private void ValidateFormCodeFormat()
    {
        if (string.IsNullOrWhiteSpace(FormCode))
        {
            FormCodeError = "Form Code không được để trống.";
            return;
        }
        if (!FormCodeRegex.IsMatch(FormCode))
        {
            FormCodeError = "Chỉ nhập A-Z, 0-9 và dấu gạch dưới (_).";
            return;
        }
        FormCodeError = "";
    }

    /// <summary>
    /// Kiểm tra trùng FormCode theo tenant theo thời gian thực (debounce 350ms).
    /// Bỏ qua nếu code không đổi so với ban đầu (edit mode).
    /// </summary>
    private async Task ValidateFormCodeRealtimeAsync()
    {
        if (HasFormCodeError) return;
        if (!IsNewForm && FormCode == _originalFormCode) return;

        _formCodeValidationCts?.Cancel();
        _formCodeValidationCts?.Dispose();
        _formCodeValidationCts = null;

        var cts = new CancellationTokenSource();
        _formCodeValidationCts = cts;

        IsCheckingFormCode = true;
        RaisePropertyChanged(nameof(CanCreateNewForm));

        try
        {
            await Task.Delay(350, cts.Token);
            await EnsureAppConfigLoadedAsync();

            // Chỉ kiểm tra trùng khi DB đã cấu hình — không mock
            if (_formDataService is not null && (_appConfig?.IsConfigured ?? false))
            {
                var exists = await _formDataService.ExistsFormCodeAsync(FormCode, _appConfig!.TenantId, cts.Token);
                if (!cts.IsCancellationRequested && exists)
                    FormCodeError = $"Mã form \"{FormCode}\" đã tồn tại trong tenant {_appConfig.TenantId}.";
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (ReferenceEquals(_formCodeValidationCts, cts))
            {
                IsCheckingFormCode = false;
                RaisePropertyChanged(nameof(CanCreateNewForm));
            }
        }
    }

    // ── New Form creation ─────────────────────────────────────

    /// <summary>
    /// Validate input, gọi IFormDataService.CreateFormAsync, navigate sang editor với formId mới.
    /// Fallback: nếu không có DB → vẫn navigate với formId=-1 (mock mode).
    /// </summary>
    private async Task ExecuteCreateNewFormAsync()
    {
        // ── 1. Validate ──────────────────────────────────────
        var normalizedCode = FormCode.Trim();
        var normalizedName = FormName.Trim();

        await EnsureAppConfigLoadedAsync();

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            CreateErrorMessage = "Mã Form không được để trống.";
            return;
        }
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            CreateErrorMessage = "Tên Form không được để trống.";
            return;
        }
        if (SelectedTable is null)
        {
            CreateErrorMessage = "Vui lòng chọn Business Table.";
            return;
        }
        if (IsCheckingFormCode)
        {
            CreateErrorMessage = "Đang kiểm tra trùng mã form, vui lòng chờ trong giây lát.";
            return;
        }
        if (HasFormCodeError)
        {
            CreateErrorMessage = $"Mã form không hợp lệ: {FormCodeError}";
            return;
        }

        CreateErrorMessage = "";
        IsLoading = true;

        try
        {
            if (_formDataService is null || !(_appConfig?.IsConfigured ?? false))
            {
                CreateErrorMessage = "Chưa cấu hình kết nối DB. Vào Settings để nhập Connection String.";
                return;
            }

            // ── 2. Kiểm tra trùng lần cuối trước khi insert ──
            var exists = await _formDataService.ExistsFormCodeAsync(
                normalizedCode,
                _appConfig.TenantId);
            if (exists)
            {
                FormCodeError      = $"Mã form \"{normalizedCode}\" đã tồn tại trong tenant {_appConfig.TenantId}.";
                CreateErrorMessage = "Không thể tạo mới vì mã form bị trùng.";
                return;
            }

            // ── 3. Insert vào DB ──────────────────────────────
            var newFormId = await _formDataService.CreateFormAsync(
                normalizedCode,
                normalizedName,
                Platform,
                _appConfig.TenantId,
                SelectedTable.TableId);

            // ── 4. Navigate sang editor với formId vừa tạo ───
            var p = new NavigationParameters
            {
                { "formId",   newFormId },
                { "formCode", normalizedCode }
            };
            _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormEditor, p);
        }
        catch (Exception ex)
        {
            CreateErrorMessage = $"Lỗi tạo form: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Navigation commands ──────────────────────────────────

    private void ExecuteSave()
    {
        // TODO(phase2): Gọi API save form metadata + sections + fields + events + permissions
        IsDirty = false;
        _undoRedo.Clear();
    }

    private void ExecutePublish()
    {
        var p = new NavigationParameters { { "formId", FormId } };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.PublishChecklist, p);
    }

    private void ExecuteViewDependencies()
    {
        var p = new NavigationParameters { { "formId", FormId } };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.DependencyViewer, p);
    }

    private void ExecuteBackToList()
    {
        // TODO(phase2): Confirm nếu IsDirty trước khi navigate back
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormManager);
    }

    // ── P0: Undo/Redo ─────────────────────────────────────────

    private void ExecuteUndo()
    {
        var currentSnapshot = CreateSnapshot();
        var previous = _undoRedo.Undo(currentSnapshot);
        if (previous is not null)
            RestoreSnapshot(previous);
    }

    private void ExecuteRedo()
    {
        var currentSnapshot = CreateSnapshot();
        var next = _undoRedo.Redo(currentSnapshot);
        if (next is not null)
            RestoreSnapshot(next);
    }

    /// <summary>Push trạng thái hiện tại vào undo stack.</summary>
    private void PushUndoState(string description)
    {
        var snapshot = CreateSnapshot();
        _undoRedo.PushState(snapshot, description);
    }

    /// <summary>Tạo JSON snapshot của form state hiện tại.</summary>
    private string CreateSnapshot()
    {
        var state = new
        {
            FormCode,
            FormName,
            Platform,
            LayoutEngine,
            Description,
            IsFormActive,
            Sections = Sections.Select(s => new
            {
                s.Id, s.Code, s.DisplayName, s.SortOrder,
                Children = s.Children.Select(f => new
                {
                    f.Id, f.Code, f.DisplayName, f.FieldType,
                    f.EditorType, f.IsRequired, f.SortOrder
                }).ToList()
            }).ToList()
        };
        return JsonSerializer.Serialize(state);
    }

    /// <summary>Khôi phục form state từ JSON snapshot.</summary>
    private void RestoreSnapshot(string snapshotJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(snapshotJson);
            var root = doc.RootElement;

            // Restore metadata (bypass setters' IsDirty)
            _formCode = root.GetProperty("FormCode").GetString() ?? "";
            RaisePropertyChanged(nameof(FormCode));

            _formName = root.GetProperty("FormName").GetString() ?? "";
            RaisePropertyChanged(nameof(FormName));

            _platform = root.GetProperty("Platform").GetString() ?? "web";
            RaisePropertyChanged(nameof(Platform));

            _layoutEngine = root.GetProperty("LayoutEngine").GetString() ?? "Grid";
            RaisePropertyChanged(nameof(LayoutEngine));

            _description = root.GetProperty("Description").GetString() ?? "";
            RaisePropertyChanged(nameof(Description));

            _isFormActive = root.GetProperty("IsFormActive").GetBoolean();
            RaisePropertyChanged(nameof(IsFormActive));

            // Restore sections + fields
            Sections.Clear();
            foreach (var sectionEl in root.GetProperty("Sections").EnumerateArray())
            {
                var section = new FormTreeNode
                {
                    Id = sectionEl.GetProperty("Id").GetInt32(),
                    NodeType = FormNodeType.Section,
                    Code = sectionEl.GetProperty("Code").GetString() ?? "",
                    DisplayName = sectionEl.GetProperty("DisplayName").GetString() ?? "",
                    SortOrder = sectionEl.GetProperty("SortOrder").GetInt32(),
                    IsExpanded = true
                };

                foreach (var fieldEl in sectionEl.GetProperty("Children").EnumerateArray())
                {
                    section.Children.Add(new FormTreeNode
                    {
                        Id = fieldEl.GetProperty("Id").GetInt32(),
                        NodeType = FormNodeType.Field,
                        Code = fieldEl.GetProperty("Code").GetString() ?? "",
                        DisplayName = fieldEl.GetProperty("DisplayName").GetString() ?? "",
                        FieldType = fieldEl.GetProperty("FieldType").GetString() ?? "text",
                        EditorType = fieldEl.GetProperty("EditorType").GetString() ?? "TextBox",
                        IsRequired = fieldEl.GetProperty("IsRequired").GetBoolean(),
                        SortOrder = fieldEl.GetProperty("SortOrder").GetInt32()
                    });
                }

                Sections.Add(section);
            }

            RaisePropertyChanged(nameof(TotalSections));
            RaisePropertyChanged(nameof(TotalFields));
        }
        catch
        {
            // Snapshot bị lỗi → bỏ qua
        }
    }

    // ── P0: Init services ───────────────────────────────────────

    /// <summary>Khởi tạo Auto-save + Linting services khi navigate vào editor.</summary>
    private void InitP0Services()
    {
        // Auto-save: debounce 3 giây
        _autoSave = new AutoSaveService(async ct =>
        {
            // TODO(phase2): Thay bằng actual save qua data service
            await Task.Delay(100, ct); // Simulate save
        });
        _autoSave.StatusChanged += (_, _) =>
        {
            AutoSaveStatus = _autoSave.Status;
            LastSavedAt = _autoSave.LastSavedAt;
            AutoSaveError = _autoSave.ErrorMessage;
        };

        // Linting: debounce 500ms, validate form metadata + structure
        _linting = new LintingService(ct => Task.FromResult(RunFormLint()));
        _linting.IssuesChanged += (_, _) =>
        {
            LintIssues = _linting.Issues;
        };
    }

    /// <summary>Cleanup P0 services khi navigate ra.</summary>
    private void DisposeP0Services()
    {
        _autoSave?.Dispose();
        _autoSave = null;
        _linting?.Dispose();
        _linting = null;
    }

    /// <summary>
    /// Chạy lint rules cho form hiện tại.
    /// Trả về danh sách issues.
    /// </summary>
    private IReadOnlyList<LintIssue> RunFormLint()
    {
        var issues = new List<LintIssue>();

        // LINT001: FormCode trống
        if (string.IsNullOrWhiteSpace(FormCode))
            issues.Add(new LintIssue("LINT001", "error", "Form Code không được để trống.", "FormCode", "naming"));

        // LINT002: FormCode format
        if (!string.IsNullOrWhiteSpace(FormCode) && !FormCodeRegex.IsMatch(FormCode))
            issues.Add(new LintIssue("LINT002", "error", "Form Code chỉ chấp nhận A-Z, 0-9, _.", "FormCode", "naming"));

        // LINT003: Không có section
        if (Sections.Count == 0 && !IsNewForm)
            issues.Add(new LintIssue("LINT003", "warning", "Form chưa có section nào.", "Sections", "required"));

        // LINT004: Section trống (không có field)
        foreach (var section in Sections)
        {
            if (section.Children.Count == 0)
                issues.Add(new LintIssue("LINT004", "warning",
                    $"Section \"{section.Code}\" chưa có field nào.", section.Code, "required"));
        }

        // LINT005: Duplicate field code
        var fieldCodes = Sections
            .SelectMany(s => s.Children)
            .GroupBy(f => f.Code, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);
        foreach (var dup in fieldCodes)
            issues.Add(new LintIssue("LINT005", "error",
                $"Field Code \"{dup.Key}\" bị trùng ({dup.Count()} lần).", dup.Key, "naming"));

        // LINT006: Duplicate section code
        var sectionCodes = Sections
            .GroupBy(s => s.Code, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);
        foreach (var dup in sectionCodes)
            issues.Add(new LintIssue("LINT006", "error",
                $"Section Code \"{dup.Key}\" bị trùng ({dup.Count()} lần).", dup.Key, "naming"));

        // LINT007: Field code chứa ký tự không hợp lệ
        foreach (var field in Sections.SelectMany(s => s.Children))
        {
            if (!string.IsNullOrWhiteSpace(field.Code) && !FormCodeRegex.IsMatch(field.Code))
                issues.Add(new LintIssue("LINT007", "warning",
                    $"Field Code \"{field.Code}\" nên dùng A-Z, 0-9, _.", field.Code, "naming"));
        }

        // LINT008: FormName trống
        if (string.IsNullOrWhiteSpace(FormName) && !IsNewForm)
            issues.Add(new LintIssue("LINT008", "warning", "Tên form chưa được đặt.", "FormName", "required"));

        return issues;
    }

    // ── Helpers ──────────────────────────────────────────────

    private FormTreeNode? FindParentSection(FormTreeNode? fieldNode)
    {
        if (fieldNode is null) return null;
        return Sections.FirstOrDefault(s => s.Children.Contains(fieldNode));
    }

    private static void MoveInCollection(ObservableCollection<FormTreeNode> collection, FormTreeNode item, int direction)
    {
        int currentIndex = collection.IndexOf(item);
        int newIndex = currentIndex + direction;
        if (newIndex < 0 || newIndex >= collection.Count) return;
        collection.Move(currentIndex, newIndex);
    }

    private void ReindexSortOrders()
    {
        for (int i = 0; i < Sections.Count; i++)
        {
            Sections[i].SortOrder = i + 1;
            for (int j = 0; j < Sections[i].Children.Count; j++)
                Sections[i].Children[j].SortOrder = j + 1;
        }
    }

    private void SetExpandAll(bool expanded)
    {
        foreach (var section in Sections)
            section.IsExpanded = expanded;
    }

    private async Task EnsureAppConfigLoadedAsync()
    {
        if (_appConfig is null || _appConfig.IsConfigured)
            return;

        await _appConfig.LoadAsync();
    }

    private async Task LoadTableLookupSafeAsync()
    {
        try
        {
            await LoadTableLookupAsync();
        }
        catch (Exception ex)
        {
            CreateErrorMessage = $"Không thể tải danh sách Sys_Table: {ex.Message}";
        }
    }

    private async Task LoadTableLookupAsync()
    {
        TableLookupItems.Clear();

        try
        {
            await EnsureAppConfigLoadedAsync();
            if (_formDataService is null || _appConfig is null || !_appConfig.IsConfigured)
                return;

            var tables = await _formDataService.GetTablesByTenantAsync(_appConfig.TenantId);
            foreach (var table in tables)
                TableLookupItems.Add(table);
        }
        catch (Exception ex)
        {
            CreateErrorMessage = $"Không thể tải danh sách Sys_Table: {ex.Message}";
        }
    }
}
