// File    : FormEditDialogViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho FormEditDialog — tạo mới / chỉnh sửa metadata form (4 tabs: Info, Sections, Events, Permissions).

using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// ViewModel cho FormEditDialog (IDialogAware).
/// Hỗ trợ 2 mode: Tạo mới (formId=0) và Chỉnh sửa (formId>0).
/// Tab 1: Thông tin cơ bản — Tab 2: Sections &amp; Fields — Tab 3: Events — Tab 4: Permissions.
/// </summary>
public sealed class FormEditDialogViewModel : ViewModelBase, IDialogAware
{
    private readonly IRegionManager _regionManager;
    private static readonly Regex FormCodeRegex = new(@"^[A-Z0-9_]+$", RegexOptions.Compiled);
    private CancellationTokenSource? _codeCheckCts;
    private string _originalFormCode = "";

    // ── IDialogAware ─────────────────────────────────────────
    public string Title => IsCreateMode ? "Tạo Form Mới" : $"Chỉnh sửa · {_originalFormCode}";
    public DialogCloseListener RequestClose { get; set; } = default!;

    public bool CanCloseDialog() => true;
    public void OnDialogClosed()
    {
        _codeCheckCts?.Cancel();
        _codeCheckCts?.Dispose();
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        var formId = parameters.GetValue<int>("formId");
        IsCreateMode = formId == 0;

        LoadTableOptions();

        if (IsCreateMode)
            InitCreateMode();
        else
        {
            _originalFormCode = parameters.GetValue<string>("formCode") ?? "";
            _ = LoadEditModeAsync(_originalFormCode);
        }
    }

    // ── Mode ──────────────────────────────────────────────────
    private bool _isCreateMode = true;
    public bool IsCreateMode
    {
        get => _isCreateMode;
        private set
        {
            if (SetProperty(ref _isCreateMode, value))
                RaisePropertyChanged(nameof(Title));
        }
    }

    // ── Tab 1 — Thông tin cơ bản ─────────────────────────────

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
                _ = CheckFormCodeUniqueAsync();
                SaveCommand.RaiseCanExecuteChanged();
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
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

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
                SaveCommand.RaiseCanExecuteChanged();
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

    private bool _isActive = true;
    public bool IsActive
    {
        get => _isActive;
        set { if (SetProperty(ref _isActive, value)) IsDirty = true; }
    }

    private int _version = 1;
    /// <summary>Version — readonly, chỉ hiển thị (system tự tăng khi lưu).</summary>
    public int Version
    {
        get => _version;
        private set => SetProperty(ref _version, value);
    }

    private string _checksum = "";
    /// <summary>Checksum — readonly, system tự tính.</summary>
    public string Checksum
    {
        get => _checksum;
        private set => SetProperty(ref _checksum, value);
    }

    // ── Lookups (Tab 1) ───────────────────────────────────────
    public ObservableCollection<TableLookupRecord> TableOptions { get; } = [];
    public List<string> PlatformOptions    { get; } = ["web", "mobile", "wpf"];
    public List<string> LayoutEngineOptions{ get; } = ["Grid", "Flex", "Custom"];

    // ── FormCode validation (Tab 1) ───────────────────────────
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

    private bool _isCheckingCode;
    public bool IsCheckingCode
    {
        get => _isCheckingCode;
        private set => SetProperty(ref _isCheckingCode, value);
    }

    // ── Validation summary (toàn dialog) ─────────────────────
    private string _validationSummary = "";
    public string ValidationSummary
    {
        get => _validationSummary;
        private set
        {
            if (SetProperty(ref _validationSummary, value))
                RaisePropertyChanged(nameof(HasValidationError));
        }
    }

    public bool HasValidationError => !string.IsNullOrEmpty(_validationSummary);

    // ── Dirty tracking ────────────────────────────────────────
    private bool _isDirty;
    public bool IsDirty
    {
        get => _isDirty;
        private set => SetProperty(ref _isDirty, value);
    }

    // ── Tab 2 — Sections & Fields ─────────────────────────────
    public ObservableCollection<FormTreeNode> Sections { get; } = [];
    // ── Tab 3 — Events ────────────────────────────────────────
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

    // ── Tab 4 — Permissions ───────────────────────────────────
    public ObservableCollection<FormPermissionRow> Permissions { get; } = [];

    private FormTreeNode? _selectedSection;
    public FormTreeNode? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (SetProperty(ref _selectedSection, value))
            {
                RaisePropertyChanged(nameof(SelectedSectionFields));
                RaisePropertyChanged(nameof(HasSelectedSection));
                AddFieldCommand.RaiseCanExecuteChanged();
                RemoveSectionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasSelectedSection => SelectedSection is not null;

    /// <summary>Fields của section đang chọn — binding cho DataGrid bên phải.</summary>
    public IEnumerable<FormTreeNode> SelectedSectionFields
        => SelectedSection?.Children ?? Enumerable.Empty<FormTreeNode>();

    private FormTreeNode? _selectedField;
    public FormTreeNode? SelectedField
    {
        get => _selectedField;
        set
        {
            if (SetProperty(ref _selectedField, value))
            {
                RemoveFieldCommand.RaiseCanExecuteChanged();
                MoveFieldUpCommand.RaiseCanExecuteChanged();
                MoveFieldDownCommand.RaiseCanExecuteChanged();
                OpenFieldConfigCommand.RaiseCanExecuteChanged();
            }
        }
    }

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand SaveCommand        { get; }
    public DelegateCommand CancelCommand      { get; }

    // Tab 2 commands
    public DelegateCommand AddSectionCommand      { get; }
    public DelegateCommand RemoveSectionCommand   { get; }
    public DelegateCommand AddFieldCommand        { get; }
    public DelegateCommand RemoveFieldCommand     { get; }
    public DelegateCommand MoveFieldUpCommand     { get; }
    public DelegateCommand MoveFieldDownCommand   { get; }
    public DelegateCommand OpenFieldConfigCommand { get; }

    // Tab 3 commands
    public DelegateCommand AddEventCommand    { get; }
    public DelegateCommand RemoveEventCommand { get; }
    public DelegateCommand EditEventCommand   { get; }

    // Tab 4 — dùng để đánh dấu IsDirty khi checkbox thay đổi
    public DelegateCommand DirtyCommand { get; }

    public FormEditDialogViewModel(IRegionManager regionManager)
    {
        _regionManager = regionManager;

        SaveCommand   = new DelegateCommand(ExecuteSave,   CanSave);
        CancelCommand = new DelegateCommand(ExecuteCancel);

        AddSectionCommand    = new DelegateCommand(ExecuteAddSection);
        RemoveSectionCommand = new DelegateCommand(ExecuteRemoveSection, () => HasSelectedSection);
        AddFieldCommand      = new DelegateCommand(ExecuteAddField,      () => HasSelectedSection);
        RemoveFieldCommand   = new DelegateCommand(ExecuteRemoveField,   () => SelectedField is not null);
        MoveFieldUpCommand   = new DelegateCommand(ExecuteMoveFieldUp,   () => CanMoveField(-1));
        MoveFieldDownCommand = new DelegateCommand(ExecuteMoveFieldDown, () => CanMoveField(1));
        OpenFieldConfigCommand = new DelegateCommand(ExecuteOpenFieldConfig, () => SelectedField is not null);

        AddEventCommand    = new DelegateCommand(ExecuteAddEvent);
        RemoveEventCommand = new DelegateCommand(ExecuteRemoveEvent, () => SelectedEvent is not null);
        EditEventCommand   = new DelegateCommand(ExecuteEditEvent,   () => SelectedEvent is not null);

        DirtyCommand = new DelegateCommand(() => IsDirty = true);
    }

    // ── Init / Load ──────────────────────────────────────────

    private void InitCreateMode()
    {
        FormCode     = "";
        FormName     = "";
        Platform     = "web";
        SelectedTable = null;
        LayoutEngine = "Grid";
        Description  = "";
        IsActive     = true;
        Version      = 1;
        Checksum     = "";
        Sections.Clear();
        Events.Clear();
        LoadDefaultPermissions();
        IsDirty      = false;
        ValidationSummary = "";
        FormCodeError     = "";
    }

    private async Task LoadEditModeAsync(string formCode)
    {
        // TODO(phase2): gọi API GetFormByCode(formCode) để load dữ liệu thật
        await Task.Delay(30);
        LoadEditMockData(formCode);
    }

    /// <summary>Mock data cho edit mode.</summary>
    private void LoadEditMockData(string formCode)
    {
        FormCode     = formCode;
        FormName     = "Đơn Đặt Hàng";
        Platform     = "web";
        SelectedTable = TableOptions.FirstOrDefault(t => t.TableCode == "PurchaseOrder")
                        ?? TableOptions.FirstOrDefault();
        LayoutEngine = "Grid";
        Description  = "Form nhập đơn đặt hàng cho web platform.";
        IsActive     = true;
        Version      = 3;
        Checksum     = "a1b2c3d4e5f6";

        // ── Sections + Fields mock ───────────────────────────
        Sections.Clear();

        var sec1 = new FormTreeNode { Id = 1, NodeType = FormNodeType.Section, Code = "GENERAL_INFO", DisplayName = "Thông Tin Chung", SortOrder = 1 };
        sec1.Children.Add(new FormTreeNode { Id = 1, NodeType = FormNodeType.Field, Code = "FullName",    DisplayName = "Họ Tên",       EditorType = "TextBox",   SortOrder = 1 });
        sec1.Children.Add(new FormTreeNode { Id = 2, NodeType = FormNodeType.Field, Code = "DateOfBirth", DisplayName = "Ngày Sinh",    EditorType = "DatePicker",SortOrder = 2 });
        sec1.Children.Add(new FormTreeNode { Id = 3, NodeType = FormNodeType.Field, Code = "Gender",      DisplayName = "Giới Tính",    EditorType = "ComboBox",  SortOrder = 3 });
        Sections.Add(sec1);

        var sec2 = new FormTreeNode { Id = 2, NodeType = FormNodeType.Section, Code = "CONTACT_INFO", DisplayName = "Liên Hệ", SortOrder = 2 };
        sec2.Children.Add(new FormTreeNode { Id = 4, NodeType = FormNodeType.Field, Code = "Phone", DisplayName = "Điện Thoại", EditorType = "TextBox", SortOrder = 1 });
        sec2.Children.Add(new FormTreeNode { Id = 5, NodeType = FormNodeType.Field, Code = "Email", DisplayName = "Email",      EditorType = "TextBox", SortOrder = 2 });
        Sections.Add(sec2);

        SelectedSection = Sections.FirstOrDefault();

        // ── Events mock ──────────────────────────────────────
        Events.Clear();
        Events.Add(new EventSummaryDto { EventId = 1, OrderNo = 1, TriggerCode = "OnLoad",   FieldTarget = "",          ConditionPreview = "",                             ActionsCount = 2, IsActive = true  });
        Events.Add(new EventSummaryDto { EventId = 2, OrderNo = 2, TriggerCode = "OnChange", FieldTarget = "Gender",    ConditionPreview = "Gender == 'Female'",           ActionsCount = 1, IsActive = true  });
        Events.Add(new EventSummaryDto { EventId = 3, OrderNo = 3, TriggerCode = "OnChange", FieldTarget = "DateOfBirth", ConditionPreview = "today() - DateOfBirth > 60", ActionsCount = 3, IsActive = false });
        Events.Add(new EventSummaryDto { EventId = 4, OrderNo = 4, TriggerCode = "OnSubmit", FieldTarget = "",          ConditionPreview = "",                             ActionsCount = 1, IsActive = true  });

        // ── Permissions mock ─────────────────────────────────
        LoadDefaultPermissions();

        IsDirty = false;
    }

    /// <summary>Load danh sách roles mặc định với quyền form.</summary>
    private void LoadDefaultPermissions()
    {
        Permissions.Clear();
        // TODO(phase2): gọi API GetRoleLookup() để lấy danh sách roles thực từ Sys_Role
        Permissions.Add(new FormPermissionRow { RoleId = 1, RoleName = "Admin",    RoleDescription = "Quản trị hệ thống",           CanRead = true,  CanWrite = true,  CanSubmit = true  });
        Permissions.Add(new FormPermissionRow { RoleId = 2, RoleName = "Manager",  RoleDescription = "Quản lý nghiệp vụ",            CanRead = true,  CanWrite = true,  CanSubmit = true  });
        Permissions.Add(new FormPermissionRow { RoleId = 3, RoleName = "Staff",    RoleDescription = "Nhân viên nhập liệu",          CanRead = true,  CanWrite = true,  CanSubmit = false });
        Permissions.Add(new FormPermissionRow { RoleId = 4, RoleName = "Viewer",   RoleDescription = "Chỉ xem báo cáo",             CanRead = true,  CanWrite = false, CanSubmit = false });
        Permissions.Add(new FormPermissionRow { RoleId = 5, RoleName = "Auditor",  RoleDescription = "Kiểm toán — readonly",        CanRead = true,  CanWrite = false, CanSubmit = false });
        Permissions.Add(new FormPermissionRow { RoleId = 6, RoleName = "External", RoleDescription = "Đối tác / khách hàng ngoài",  CanRead = false, CanWrite = false, CanSubmit = false });
    }

    /// <summary>Load danh sách table từ DB (fallback sang mock).</summary>
    private void LoadTableOptions()
    {
        TableOptions.Clear();
        // TODO(phase2): gọi API GetSysTableLookup()
        TableOptions.Add(new TableLookupRecord { TableId = 1, TableCode = "PurchaseOrder", TableName = "Đơn Đặt Hàng",  SchemaName = "dbo" });
        TableOptions.Add(new TableLookupRecord { TableId = 2, TableCode = "HrLeave",       TableName = "Nghỉ Phép",      SchemaName = "dbo" });
        TableOptions.Add(new TableLookupRecord { TableId = 3, TableCode = "Inventory",     TableName = "Nhập Kho",       SchemaName = "dbo" });
        TableOptions.Add(new TableLookupRecord { TableId = 4, TableCode = "Inspection",    TableName = "Kiểm Tra",       SchemaName = "dbo" });
        TableOptions.Add(new TableLookupRecord { TableId = 5, TableCode = "Report",        TableName = "Báo Cáo",        SchemaName = "dbo" });
    }

    // ── FormCode validation ───────────────────────────────────

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
    /// Debounce 400ms kiểm tra trùng mã form (trong edit mode bỏ qua khi code không đổi).
    /// </summary>
    private async Task CheckFormCodeUniqueAsync()
    {
        if (HasFormCodeError) return;
        if (!IsCreateMode && FormCode == _originalFormCode) return;

        _codeCheckCts?.Cancel();
        _codeCheckCts = new CancellationTokenSource();
        var token = _codeCheckCts.Token;

        IsCheckingCode = true;
        try
        {
            await Task.Delay(400, token);
            // TODO(phase2): gọi API ExistsFormCode(FormCode)
            // Giả lập: "OLD_FORM" đã tồn tại
            var isDuplicate = FormCode is "OLD_FORM" or "PO_ORDER";
            if (isDuplicate)
                FormCodeError = $"Mã form \"{FormCode}\" đã tồn tại trong hệ thống.";
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsCheckingCode = false;
            SaveCommand.RaiseCanExecuteChanged();
        }
    }

    // ── Save / Cancel ────────────────────────────────────────

    private bool CanSave()
        => !IsCheckingCode
        && !HasFormCodeError
        && !string.IsNullOrWhiteSpace(FormCode)
        && !string.IsNullOrWhiteSpace(FormName)
        && SelectedTable is not null;

    private void ExecuteSave()
    {
        ValidationSummary = "";

        // ── Validate toàn bộ Tab 1 trước khi lưu ────────────
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(FormCode))
            errors.Add("• Form Code không được để trống.");
        else if (!FormCodeRegex.IsMatch(FormCode))
            errors.Add("• Form Code chỉ được chứa A-Z, 0-9 và dấu _.");
        else if (HasFormCodeError)
            errors.Add($"• {FormCodeError}");

        if (string.IsNullOrWhiteSpace(FormName))
            errors.Add("• Tên Form không được để trống.");

        if (SelectedTable is null)
            errors.Add("• Phải chọn Business Table.");

        if (errors.Count > 0)
        {
            ValidationSummary = string.Join("\n", errors);
            return;
        }

        // ── Đóng dialog với kết quả OK + dữ liệu form ────────
        var result = new DialogParameters
        {
            { "formCode",     FormCode },
            { "formName",     FormName },
            { "platform",     Platform },
            { "tableId",      SelectedTable!.TableId },
            { "tableName",    SelectedTable.TableName },
            { "layoutEngine", LayoutEngine },
            { "description",  Description },
            { "isActive",     IsActive },
            { "isCreate",     IsCreateMode }
        };

        RequestClose.Invoke(result, ButtonResult.OK);
    }

    private void ExecuteCancel()
    {
        // Nếu có thay đổi chưa lưu → hỏi xác nhận trước khi đóng
        if (IsDirty)
        {
            var answer = System.Windows.MessageBox.Show(
                "Bạn có thay đổi chưa lưu. Bạn có chắc muốn thoát không?",
                "Xác nhận thoát",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (answer != System.Windows.MessageBoxResult.Yes)
                return;
        }

        RequestClose.Invoke(new DialogParameters(), ButtonResult.Cancel);
    }

    // ── Tab 2 — Sections command handlers ────────────────────

    private void ExecuteAddSection()
    {
        var orderNo = Sections.Count + 1;
        var section = new FormTreeNode
        {
            Id          = Sections.Count > 0 ? Sections.Max(s => s.Id) + 1 : 1,
            NodeType    = FormNodeType.Section,
            Code        = $"SECTION_{orderNo:D2}",
            DisplayName = $"Section {orderNo}",
            SortOrder   = orderNo,
            IsExpanded  = true
        };
        Sections.Add(section);
        SelectedSection = section;
        IsDirty = true;
    }

    private void ExecuteRemoveSection()
    {
        if (SelectedSection is null) return;
        Sections.Remove(SelectedSection);
        SelectedSection = Sections.FirstOrDefault();
        IsDirty = true;
    }

    // ── Tab 2 — Fields command handlers ──────────────────────

    private void ExecuteAddField()
    {
        if (SelectedSection is null) return;
        var orderNo = SelectedSection.Children.Count + 1;
        var field = new FormTreeNode
        {
            Id          = orderNo,
            NodeType    = FormNodeType.Field,
            Code        = $"FIELD_{orderNo:D2}",
            DisplayName = $"Field {orderNo}",
            EditorType  = "TextBox",
            SortOrder   = orderNo
        };
        SelectedSection.Children.Add(field);
        SelectedField = field;
        // NOTE: RaisePropertyChanged cần thiết vì Children thay đổi không tự notify
        RaisePropertyChanged(nameof(SelectedSectionFields));
        IsDirty = true;
    }

    private void ExecuteRemoveField()
    {
        if (SelectedField is null || SelectedSection is null) return;
        SelectedSection.Children.Remove(SelectedField);
        SelectedField = null;
        RaisePropertyChanged(nameof(SelectedSectionFields));
        IsDirty = true;
    }

    private void ExecuteMoveFieldUp()
    {
        if (SelectedField is null || SelectedSection is null) return;
        var idx = SelectedSection.Children.IndexOf(SelectedField);
        if (idx <= 0) return;
        SelectedSection.Children.Move(idx, idx - 1);
        RaisePropertyChanged(nameof(SelectedSectionFields));
        IsDirty = true;
    }

    private void ExecuteMoveFieldDown()
    {
        if (SelectedField is null || SelectedSection is null) return;
        var idx = SelectedSection.Children.IndexOf(SelectedField);
        if (idx < 0 || idx >= SelectedSection.Children.Count - 1) return;
        SelectedSection.Children.Move(idx, idx + 1);
        RaisePropertyChanged(nameof(SelectedSectionFields));
        IsDirty = true;
    }

    private bool CanMoveField(int direction)
    {
        if (SelectedField is null || SelectedSection is null) return false;
        var idx = SelectedSection.Children.IndexOf(SelectedField);
        return direction < 0 ? idx > 0 : idx < SelectedSection.Children.Count - 1;
    }

    /// <summary>
    /// Đóng dialog và navigate sang FieldConfig cho field được chọn.
    /// </summary>
    private void ExecuteOpenFieldConfig()
    {
        if (SelectedField is null) return;
        // NOTE: đóng dialog trước, sau đó navigate — không thể navigate khi đang trong dialog context
        var p = new DialogParameters { { "fieldId", SelectedField.Id } };
        RequestClose.Invoke(p, ButtonResult.None);

        var navP = new NavigationParameters { { "fieldId", SelectedField.Id } };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FieldConfig, navP);
    }

    // ── Tab 3 — Events command handlers ──────────────────────

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

    /// <summary>
    /// Đóng dialog và navigate sang EventEditor cho event được chọn.
    /// </summary>
    private void ExecuteEditEvent()
    {
        if (SelectedEvent is null) return;
        // NOTE: đóng dialog trước, navigate về EventEditor
        var p = new DialogParameters { { "eventId", SelectedEvent.EventId } };
        RequestClose.Invoke(p, ButtonResult.None);

        var navP = new NavigationParameters { { "eventId", SelectedEvent.EventId } };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.EventEditor, navP);
    }
}
