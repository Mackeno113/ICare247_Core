// File    : FormEditorViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho màn hình Form Editor (Screen 03) — quản lý toàn bộ form: metadata, sections, fields, events, permissions.

using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Interfaces;
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
    private readonly IRegionManager  _regionManager;
    private readonly IFormDataService? _formDataService;
    private readonly IAppConfigService? _appConfig;
    private CancellationTokenSource? _formCodeValidationCts;
    private static readonly Regex FormCodeRegex = new(@"^[A-Z0-9_]+$", RegexOptions.Compiled);
    private string _originalFormCode = "";

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
                IsDirty = true;
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
        set { if (SetProperty(ref _selectedTable, value)) IsDirty = true; }
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

        IsDirty = true;
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
            if (SetProperty(ref _isNewForm, value))
                RaisePropertyChanged(nameof(IsEditorVisible));
        }
    }

    /// <summary>True khi đang ở chế độ edit form có sẵn (không phải tạo mới).</summary>
    public bool IsEditorVisible => !_isNewForm;

    // Input fields cho "tạo form mới"
    private string _newFormCode = "";
    public string NewFormCode
    {
        get => _newFormCode;
        set
        {
            if (SetProperty(ref _newFormCode, value))
            {
                RaisePropertyChanged(nameof(CanCreateNewForm));
                _ = ValidateNewFormCodeRealtimeAsync();
            }
        }
    }

    private string _newFormName = "";
    public string NewFormName
    {
        get => _newFormName;
        set
        {
            if (SetProperty(ref _newFormName, value))
                RaisePropertyChanged(nameof(CanCreateNewForm));
        }
    }

    private string _newFormPlatform = "web";
    public string NewFormPlatform
    {
        get => _newFormPlatform;
        set => SetProperty(ref _newFormPlatform, value);
    }

    private int? _newTableId;
    public int? NewTableId
    {
        get => _newTableId;
        set
        {
            if (SetProperty(ref _newTableId, value))
                RaisePropertyChanged(nameof(CanCreateNewForm));
        }
    }

    /// <summary>
    /// Cho phép tạo mới khi không loading, không trùng mã và đủ input bắt buộc.
    /// </summary>
    public bool CanCreateNewForm =>
        IsNotLoading
        && !IsCheckingFormCode
        && !IsFormCodeDuplicate
        && !string.IsNullOrWhiteSpace(NewFormCode)
        && !string.IsNullOrWhiteSpace(NewFormName)
        && NewTableId.HasValue
        && NewTableId.Value > 0;

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

    // Permissions — dùng để đánh dấu IsDirty khi checkbox thay đổi
    public DelegateCommand DirtyCommand { get; }

    public FormEditorViewModel(
        IRegionManager regionManager,
        IFormDataService? formDataService = null,
        IAppConfigService? appConfig = null)
    {
        _regionManager   = regionManager;
        _formDataService = formDataService;
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

        // Permissions
        DirtyCommand = new DelegateCommand(() => IsDirty = true);
    }

    // ── INavigationAware ─────────────────────────────────────

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        FormId = navigationContext.Parameters.GetValue<int>("formId");

        // ── Phân nhánh: tạo mới hay edit ────────────────────
        if (FormId == 0)
        {
            // Chế độ tạo form mới — reset input fields
            IsNewForm        = true;
            NewFormCode      = "";
            NewFormName      = "";
            NewFormPlatform  = "web";
            NewTableId       = null;
            CreateErrorMessage = "";
            IsCheckingFormCode = false;
            IsFormCodeDuplicate = false;
            FormCodeValidationMessage = "";
            _ = LoadTableLookupSafeAsync();
        }
        else
        {
            IsNewForm = false;
            _originalFormCode = navigationContext.Parameters.GetValue<string>("formCode") ?? "";
            LoadMockData();

            // NOTE: activeTab param cho phép mở đúng tab khi navigate từ EditFormCommand (tab 0 = Thông tin Form)
            if (navigationContext.Parameters.ContainsKey("activeTab"))
                ActiveTabIndex = navigationContext.Parameters.GetValue<int>("activeTab");
            else
                ActiveTabIndex = 1; // Mặc định tab "Thuộc tính"
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
    }

    // ── Load mock data ───────────────────────────────────────

    /// <summary>
    /// Load mock data cho demo. Sau này sẽ thay bằng API call tới backend.
    /// Mock form: "Đơn Đặt Hàng" với 3 sections, 8 fields + events + permissions.
    /// </summary>
    private void LoadMockData()
    {
        IsLoading = true;

        // ── Form info ────────────────────────────────────────
        FormCode = "PO_ORDER";
        FormName = "Đơn Đặt Hàng";
        Version = 3;
        Platform = "web";
        LayoutEngine = "Grid";
        Description = "Form nhập đơn đặt hàng cho web platform.";
        IsFormActive = true;
        Checksum = "a1b2c3d4e5f6";
        _originalFormCode = "PO_ORDER";

        // ── Table lookup ─────────────────────────────────────
        LoadTableOptions();
        SelectedTable = TableLookupItems.FirstOrDefault(t => t.TableCode == "PurchaseOrder")
                        ?? TableLookupItems.FirstOrDefault();

        // ── Section 1: Thông Tin Chung ──────────────────────
        var section1 = new FormTreeNode
        {
            Id = 1, NodeType = FormNodeType.Section,
            Code = "SEC_GENERAL", DisplayName = "Thông Tin Chung",
            SortOrder = 1
        };
        section1.Children.Add(new FormTreeNode
        {
            Id = 1, NodeType = FormNodeType.Field,
            Code = "MaDonHang", DisplayName = "Mã Đơn Hàng",
            FieldType = "text", EditorType = "TextBox",
            IsRequired = true, SortOrder = 1
        });
        section1.Children.Add(new FormTreeNode
        {
            Id = 2, NodeType = FormNodeType.Field,
            Code = "NgayDatHang", DisplayName = "Ngày Đặt Hàng",
            FieldType = "date", EditorType = "DatePicker",
            IsRequired = true, SortOrder = 2
        });
        section1.Children.Add(new FormTreeNode
        {
            Id = 3, NodeType = FormNodeType.Field,
            Code = "TrangThai", DisplayName = "Trạng Thái",
            FieldType = "text", EditorType = "ComboBox",
            IsRequired = true, SortOrder = 3
        });

        // ── Section 2: Chi Tiết ─────────────────────────────
        var section2 = new FormTreeNode
        {
            Id = 2, NodeType = FormNodeType.Section,
            Code = "SEC_DETAIL", DisplayName = "Chi Tiết",
            SortOrder = 2
        };
        section2.Children.Add(new FormTreeNode
        {
            Id = 4, NodeType = FormNodeType.Field,
            Code = "NhaCungCap", DisplayName = "Nhà Cung Cấp",
            FieldType = "number", EditorType = "LookupBox",
            IsRequired = false, SortOrder = 1
        });
        section2.Children.Add(new FormTreeNode
        {
            Id = 5, NodeType = FormNodeType.Field,
            Code = "SoLuong", DisplayName = "Số Lượng",
            FieldType = "number", EditorType = "NumericBox",
            IsRequired = true, SortOrder = 2
        });
        section2.Children.Add(new FormTreeNode
        {
            Id = 6, NodeType = FormNodeType.Field,
            Code = "DonGia", DisplayName = "Đơn Giá",
            FieldType = "number", EditorType = "NumericBox",
            IsRequired = true, SortOrder = 3
        });
        section2.Children.Add(new FormTreeNode
        {
            Id = 7, NodeType = FormNodeType.Field,
            Code = "ThanhTien", DisplayName = "Thành Tiền",
            FieldType = "number", EditorType = "NumericBox",
            IsRequired = false, SortOrder = 4
        });

        // ── Section 3: Ghi Chú ──────────────────────────────
        var section3 = new FormTreeNode
        {
            Id = 3, NodeType = FormNodeType.Section,
            Code = "SEC_NOTE", DisplayName = "Ghi Chú",
            SortOrder = 3
        };
        section3.Children.Add(new FormTreeNode
        {
            Id = 8, NodeType = FormNodeType.Field,
            Code = "LyDoTuChoi", DisplayName = "Lý Do Từ Chối",
            FieldType = "text", EditorType = "TextArea",
            IsRequired = false, SortOrder = 1
        });

        Sections.Clear();
        Sections.Add(section1);
        Sections.Add(section2);
        Sections.Add(section3);

        // ── Events mock ──────────────────────────────────────
        Events.Clear();
        Events.Add(new EventSummaryDto { EventId = 1, OrderNo = 1, TriggerCode = "OnLoad",   FieldTarget = "",            ConditionPreview = "",                             ActionsCount = 2, IsActive = true  });
        Events.Add(new EventSummaryDto { EventId = 2, OrderNo = 2, TriggerCode = "OnChange", FieldTarget = "TrangThai",   ConditionPreview = "TrangThai == 'Approved'",      ActionsCount = 1, IsActive = true  });
        Events.Add(new EventSummaryDto { EventId = 3, OrderNo = 3, TriggerCode = "OnChange", FieldTarget = "SoLuong",     ConditionPreview = "SoLuong * DonGia",             ActionsCount = 3, IsActive = false });
        Events.Add(new EventSummaryDto { EventId = 4, OrderNo = 4, TriggerCode = "OnSubmit", FieldTarget = "",            ConditionPreview = "",                             ActionsCount = 1, IsActive = true  });

        // ── Permissions mock ─────────────────────────────────
        LoadDefaultPermissions();

        RaisePropertyChanged(nameof(TotalSections));
        RaisePropertyChanged(nameof(TotalFields));

        IsLoading = false;
        IsDirty = false;
        FormCodeError = "";
    }

    /// <summary>Load danh sách table từ DB (fallback sang mock).</summary>
    private void LoadTableOptions()
    {
        TableLookupItems.Clear();
        // TODO(phase2): gọi API GetSysTableLookup()
        TableLookupItems.Add(new TableLookupRecord { TableId = 1, TableCode = "PurchaseOrder", TableName = "Đơn Đặt Hàng",  SchemaName = "dbo" });
        TableLookupItems.Add(new TableLookupRecord { TableId = 2, TableCode = "HrLeave",       TableName = "Nghỉ Phép",      SchemaName = "dbo" });
        TableLookupItems.Add(new TableLookupRecord { TableId = 3, TableCode = "Inventory",     TableName = "Nhập Kho",       SchemaName = "dbo" });
        TableLookupItems.Add(new TableLookupRecord { TableId = 4, TableCode = "Inspection",    TableName = "Kiểm Tra",       SchemaName = "dbo" });
        TableLookupItems.Add(new TableLookupRecord { TableId = 5, TableCode = "Report",        TableName = "Báo Cáo",        SchemaName = "dbo" });
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
        IsDirty = true;
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
        IsDirty = true;
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
        IsDirty = true;
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
        if (IsNewForm) return; // Tạo mới dùng NewFormCode, không dùng FormCode

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
    /// Kiểm tra trùng FormCode theo tenant — dùng cho edit mode.
    /// Bỏ qua nếu code không đổi so với ban đầu.
    /// </summary>
    private async Task ValidateFormCodeRealtimeAsync()
    {
        if (IsNewForm) return; // Tạo mới dùng ValidateNewFormCodeRealtimeAsync
        if (HasFormCodeError) return;
        if (FormCode == _originalFormCode) return;

        _formCodeValidationCts?.Cancel();
        _formCodeValidationCts?.Dispose();
        _formCodeValidationCts = null;

        var cts = new CancellationTokenSource();
        _formCodeValidationCts = cts;

        IsCheckingFormCode = true;
        try
        {
            await Task.Delay(400, cts.Token);

            await EnsureAppConfigLoadedAsync();

            if (_formDataService is null || (_appConfig?.IsConfigured ?? false) is false)
            {
                // NOTE: Không có DB — mock check
                var isDuplicate = FormCode is "OLD_FORM" or "PO_ORDER";
                if (isDuplicate && FormCode != _originalFormCode)
                    FormCodeError = $"Mã form \"{FormCode}\" đã tồn tại trong hệ thống.";
            }
            else
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
                IsCheckingFormCode = false;
        }
    }

    /// <summary>
    /// Kiểm tra trùng NewFormCode — dùng cho create mode.
    /// </summary>
    private async Task ValidateNewFormCodeRealtimeAsync()
    {
        _formCodeValidationCts?.Cancel();
        _formCodeValidationCts?.Dispose();
        _formCodeValidationCts = null;

        var normalizedCode = NewFormCode.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            IsCheckingFormCode = false;
            IsFormCodeDuplicate = false;
            FormCodeValidationMessage = "";
            return;
        }

        await EnsureAppConfigLoadedAsync();

        if (_formDataService is null || (_appConfig?.IsConfigured ?? false) is false)
        {
            IsCheckingFormCode = false;
            IsFormCodeDuplicate = false;
            FormCodeValidationMessage = "Chưa cấu hình DB nên chưa thể kiểm tra trùng mã realtime.";
            return;
        }

        var cts = new CancellationTokenSource();
        _formCodeValidationCts = cts;

        try
        {
            IsCheckingFormCode = true;
            FormCodeValidationMessage = "Đang kiểm tra trùng mã form...";

            await Task.Delay(350, cts.Token);

            var exists = await _formDataService.ExistsFormCodeAsync(
                normalizedCode,
                _appConfig!.TenantId,
                cts.Token);

            if (cts.IsCancellationRequested)
                return;

            IsFormCodeDuplicate = exists;
            FormCodeValidationMessage = exists
                ? $"Mã form '{normalizedCode}' đã tồn tại trong tenant {_appConfig.TenantId}."
                : $"Mã form '{normalizedCode}' có thể sử dụng.";
        }
        catch (OperationCanceledException)
        {
            // NOTE: Người dùng gõ tiếp nên request cũ bị hủy — bỏ qua.
        }
        catch (Exception ex)
        {
            if (cts.IsCancellationRequested)
                return;

            IsFormCodeDuplicate = false;
            FormCodeValidationMessage = $"Không thể kiểm tra trùng mã: {ex.Message}";
        }
        finally
        {
            if (ReferenceEquals(_formCodeValidationCts, cts))
                IsCheckingFormCode = false;
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
        var normalizedCode = NewFormCode.Trim();
        var normalizedName = NewFormName.Trim();

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
        if (!NewTableId.HasValue || NewTableId.Value <= 0)
        {
            CreateErrorMessage = "Vui lòng chọn Table_Id hợp lệ.";
            return;
        }
        if (IsCheckingFormCode)
        {
            CreateErrorMessage = "Đang kiểm tra trùng mã form, vui lòng chờ trong giây lát.";
            return;
        }
        if (IsFormCodeDuplicate)
        {
            CreateErrorMessage = $"Mã form '{normalizedCode}' đã tồn tại trong tenant hiện tại.";
            return;
        }

        CreateErrorMessage = "";
        IsLoading = true;

        try
        {
            int newFormId;

            if (_formDataService is not null && (_appConfig?.IsConfigured ?? false))
            {
                // ── 2a. Có DB → insert thật ──────────────────
                var exists = await _formDataService.ExistsFormCodeAsync(
                    normalizedCode,
                    _appConfig.TenantId);
                if (exists)
                {
                    IsFormCodeDuplicate = true;
                    FormCodeValidationMessage = $"Mã form '{normalizedCode}' đã tồn tại trong tenant {_appConfig.TenantId}.";
                    CreateErrorMessage = "Không thể tạo mới vì mã form bị trùng.";
                    return;
                }

                newFormId = await _formDataService.CreateFormAsync(
                    normalizedCode,
                    normalizedName,
                    NewFormPlatform,
                    _appConfig.TenantId,
                    NewTableId.Value);
            }
            else
            {
                // ── 2b. Không có DB → mock với id tạm ────────
                newFormId = -1;
            }

            // ── 3. Navigate sang editor với formId vừa tạo ──
            var p = new NavigationParameters { { "formId", newFormId } };
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
