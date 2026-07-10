// File    : FieldConfigViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho màn hình cấu hình chi tiết 1 field (Screen 04).

using System.Collections.ObjectModel;
using System.Text.Json;
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
/// ViewModel cho màn hình FieldConfig (Screen 04).
/// Quản lý cấu hình chi tiết 1 field: thông tin cơ bản, control props, rules, events.
/// Mở từ FormEditor khi click [⚙] trên field.
/// Khi DB đã cấu hình → load dữ liệu thật qua IFieldDataService + II18nDataService.
/// Khi chưa cấu hình → hiển thị danh sách rỗng + thông báo lỗi.
/// </summary>
public sealed class FieldConfigViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;
    private readonly IFieldDataService? _fieldService;
    private readonly IFormDataService? _formService;
    private readonly II18nDataService? _i18nService;
    private readonly IRuleDataService? _ruleService;
    private readonly IEventDataService? _eventService;
    private readonly ISysLookupDataService? _lookupService;
    private readonly IAppConfigService? _appConfig;
    private readonly IDialogService? _dialogService;
    private readonly IFormDetailDataService? _formDetailService;
    private readonly INavigationHistoryService? _history;
    private readonly IAppLogger? _logger;
    private readonly IUserNotifier? _notifier;
    private CancellationTokenSource _cts = new();

    /// <summary>
    /// True khi NẠP cấu hình FK/ComboBox từ Ui_Field_Lookup THẤT BẠI (vd cột DB thiếu → SqlException).
    /// Khi cờ này bật, các Fk* prop đang ở giá trị mặc định RỖNG (không phải data thật) → CHẶN Lưu
    /// để tránh ghi đè Ui_Field_Lookup bằng rỗng làm MẤT cấu hình. Reset về false mỗi lần load field.
    /// </summary>
    private bool _fkConfigLoadFailed;

    // ── Navigation params ────────────────────────────────────
    private int _fieldId;
    public int FieldId { get => _fieldId; set => SetProperty(ref _fieldId, value); }

    private int _formId;
    public int FormId { get => _formId; set => SetProperty(ref _formId, value); }

    /// <summary>FormId đã load vào FieldNavigatorGroups — tránh reload khi chỉ đổi field trong cùng form.</summary>
    private int _navigatorLoadedFormId = -1;

    private int _sectionId;
    public int SectionId { get => _sectionId; set => SetProperty(ref _sectionId, value); }

    // ── Section picker ───────────────────────────────────────
    public ObservableCollection<SectionOptionItem> AvailableSections { get; } = [];

    private SectionOptionItem? _selectedSection;
    public SectionOptionItem? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (SetProperty(ref _selectedSection, value) && value is not null)
            {
                SectionId   = value.Id;
                SectionName = value.DisplayName;
                IsDirty     = true;
            }
        }
    }

    // ── Basic tab ────────────────────────────────────────────
    private string _columnCode = "";
    public string ColumnCode { get => _columnCode; set => SetProperty(ref _columnCode, value); }

    private string _fieldCode = "";
    /// <summary>
    /// Field_Code lưu trực tiếp — bắt buộc với virtual field (không có Sys_Column).
    /// FieldCode hiệu lực = FieldCode nếu có, ngược lại dùng ColumnCode.
    /// </summary>
    public string FieldCode
    {
        get => _fieldCode;
        set
        {
            if (SetProperty(ref _fieldCode, value))
            {
                IsDirty = true;
                AutoDeriveI18nKeys();   // virtual field: FieldCode là cột hiệu lực để sinh key
            }
        }
    }

    private string _sectionName = "";
    public string SectionName { get => _sectionName; set => SetProperty(ref _sectionName, value); }

    private string _tableCode = "";
    public string TableCode { get => _tableCode; set => SetProperty(ref _tableCode, value); }

    public ObservableCollection<ColumnInfoDto> AvailableColumns { get; } = [];

    /// <summary>True khi có cột trong danh sách → hiện combo pick.</summary>
    public bool HasAvailableColumns => AvailableColumns.Count > 0;
    /// <summary>True khi list rỗng → hiện hint vàng.</summary>
    public bool IsColumnListEmpty   => AvailableColumns.Count == 0 && !IsVirtual;
    /// <summary>Hiện TextEdit gõ tay khi list rỗng hoặc IsVirtual (virtual field không cần cột).</summary>
    public bool CanTypeColumnCode   => AvailableColumns.Count == 0 || IsVirtual;

    private ColumnInfoDto? _selectedColumn;
    public ColumnInfoDto? SelectedColumn
    {
        get => _selectedColumn;
        set
        {
            if (SetProperty(ref _selectedColumn, value) && value is not null)
            {
                ColumnCode = value.ColumnCode;
                NetType = value.NetType;
                RaisePropertyChanged(nameof(DataTypeDisplay));
                RaisePropertyChanged(nameof(HasDataType));
                RaisePropertyChanged(nameof(CanConfigMasking));
                IsDirty = true;
                AutoDeriveI18nKeys();   // chọn cột → tự sinh key i18n + nạp bản dịch hiện có
            }
        }
    }

    private string _netType = "";
    public string NetType { get => _netType; set => SetProperty(ref _netType, value); }

    // ── Làm mờ trong log (thuộc tính CẤP CỘT — Sys_Column, dùng chung mọi form/view) ──
    private bool _isLogMasked;
    /// <summary>Bật làm mờ giá trị cột này trong log (import/audit). Ghi vào Sys_Column.Is_Log_Masked.</summary>
    public bool IsLogMasked { get => _isLogMasked; set { if (SetProperty(ref _isLogMasked, value)) IsDirty = true; } }

    private string _logMaskMode = "Full";
    /// <summary>Kiểu làm mờ: Full (***) · Partial (giữ 4 cuối) · Hash (sha256). Sys_Column.Log_Mask_Mode.</summary>
    public string LogMaskMode { get => _logMaskMode; set { if (SetProperty(ref _logMaskMode, value)) IsDirty = true; } }

    /// <summary>Các kiểu làm mờ hợp lệ.</summary>
    public List<string> LogMaskModeOptions { get; } = ["Full", "Partial", "Hash"];

    /// <summary>Chỉ cấu hình làm mờ cho field map cột thật (không ảo, có Column_Id).</summary>
    public bool CanConfigMasking => !IsVirtual && (_selectedColumn?.ColumnId ?? 0) > 0;

    // ── Form context (từ navigation params) ──────────────────
    private string _formCode = "";
    public string FormCode { get => _formCode; set => SetProperty(ref _formCode, value); }

    private string _formName = "";
    public string FormName { get => _formName; set => SetProperty(ref _formName, value); }

    /// <summary>DataType kèm MaxLength của column đang chọn — vd: "nvarchar(20)", "int".</summary>
    public string DataTypeDisplay => _selectedColumn?.DataTypeDisplay ?? "";

    /// <summary>True khi đã chọn column và có DataType để hiển thị badge.</summary>
    public bool HasDataType => !string.IsNullOrEmpty(DataTypeDisplay);

    // ── Field Navigator (Left Panel) ─────────────────────────
    public ObservableCollection<FieldNavGroup> FieldNavigatorGroups { get; } = [];

    // ── Bulk multi-select (song song FormEditor) ─────────────
    /// <summary>Các field đang được tick trong navigator (IsMultiChecked).</summary>
    public ObservableCollection<FieldNavItem> BulkSelectedFields { get; } = [];

    /// <summary>Danh sách section đích cho context-menu "Chuyển field đã chọn sang…".
    /// Rebuild qua <see cref="RefreshMoveTargets"/> mỗi khi mở menu.</summary>
    public ObservableCollection<FieldMoveTargetItem> MoveTargets { get; } = [];

    /// <summary>True khi có ≥1 field được tick → cho phép chuyển sang section khác.</summary>
    public bool CanMoveBulk => BulkSelectedFields.Count >= 1;

    /// <summary>Header MenuItem gốc — kèm số field đã tick; khi chưa tick thì báo hướng dẫn.</summary>
    public string BulkMoveHeader => BulkSelectedFields.Count > 0
        ? $"Chuyển {BulkSelectedFields.Count} field đã chọn sang…"
        : "Chưa tick field nào để chuyển";

    public List<string> AvailableEditorTypes { get; } =
    [
        "TextBox", "NumericBox", "ComboBox", "DatePicker",
        "RadioGroup", "LookupComboBox",
        "LookupBox", "TreeLookupBox", "TextArea", "CheckBox", "ToggleSwitch",
        "AttachmentBox"
    ];

    private string _selectedEditorType = "TextBox";
    public string SelectedEditorType
    {
        get => _selectedEditorType;
        set
        {
            // Nếu đang rời khỏi LookupBox và có config → yêu cầu xác nhận
            if (!_isLoading
                && _selectedEditorType == "LookupBox"
                && value != "LookupBox"
                && HasFkLookupConfig)
            {
                var result = System.Windows.MessageBox.Show(
                    "Bạn đang đổi kiểu field từ LookupBox sang loại khác.\n" +
                    "Toàn bộ cấu hình FK Lookup sẽ bị xóa khi lưu.\n\n" +
                    "Tiếp tục đổi kiểu?",
                    "Xác nhận đổi kiểu field",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.No) return;

                // User xác nhận → xóa config cũ
                ClearFkLookupConfig();
            }

            if (SetProperty(ref _selectedEditorType, value))
            {
                LoadControlPropSchema();
                RaisePropertyChanged(nameof(IsLookupEditor));
                RaisePropertyChanged(nameof(IsRadioGroupEditor));
                RaisePropertyChanged(nameof(IsLookupOrComboBoxEditor));
                RaisePropertyChanged(nameof(IsFkLookupEditor));
                RaisePropertyChanged(nameof(IsTreeLookupEditor));
                RaisePropertyChanged(nameof(IsComboBoxEditor));
                RaisePropertyChanged(nameof(IsDynamicDataEditor));
                RaisePropertyChanged(nameof(EditorTypeGuide));
                RaisePropertyChanged(nameof(HasEditorTypeGuide));
                if (IsLookupEditor && !_isLoading)
                    _ = LoadLookupCodesAsync();
                IsDirty = true;
            }
        }
    }

    // ── Sys_Lookup editor (RadioGroup / LookupComboBox) ──────

    /// <summary>True khi EditorType dùng Sys_Lookup (danh mục tĩnh ngắn).</summary>
    public bool IsLookupEditor =>
        SelectedEditorType is "RadioGroup" or "LookupComboBox";

    /// <summary>True chỉ khi EditorType = RadioGroup — hiện section Lookup Code đơn giản.</summary>
    public bool IsRadioGroupEditor => SelectedEditorType == "RadioGroup";

    /// <summary>
    /// True khi EditorType cần ComboBoxPropsPanel:
    /// LookupComboBox (static + search props) hoặc ComboBox (dynamic + search props).
    /// </summary>
    public bool IsLookupOrComboBoxEditor =>
        SelectedEditorType is "LookupComboBox" or "ComboBox";

    // ── FK Lookup editor (LookupBox / TreeLookupBox) ─────────

    /// <summary>True khi EditorType là LookupBox hoặc TreeLookupBox.</summary>
    public bool IsFkLookupEditor => SelectedEditorType is "LookupBox" or "TreeLookupBox";

    /// <summary>True khi EditorType là TreeLookupBox — hiện thêm input ParentColumn.</summary>
    public bool IsTreeLookupEditor => SelectedEditorType == "TreeLookupBox";

    /// <summary>True khi EditorType là ComboBox (dynamic data từ Bảng/TVF/SQL, dùng DxComboBox).</summary>
    public bool IsComboBoxEditor => SelectedEditorType == "ComboBox";

    /// <summary>
    /// True khi EditorType cần cấu hình nguồn dữ liệu động từ Ui_Field_Lookup
    /// (LookupBox, TreeLookupBox hoặc ComboBox).
    /// </summary>
    public bool IsDynamicDataEditor => IsFkLookupEditor || IsComboBoxEditor;

    /// <summary>Hướng dẫn inline cho EditorType đang chọn — cập nhật khi SelectedEditorType thay đổi.</summary>
    public ControlTypeGuide EditorTypeGuide => BuildGuide(SelectedEditorType);

    /// <summary>True khi đã chọn EditorType và có guide để hiển thị card hướng dẫn.</summary>
    public bool HasEditorTypeGuide => !string.IsNullOrEmpty(SelectedEditorType);

    private static ControlTypeGuide BuildGuide(string editorType) => editorType switch
    {
        "TextBox" => new(
            Icon:       "📝",
            Title:      "TextBox — Văn bản ngắn",
            WhenToUse:  "Tên người, mã số, email, địa chỉ, URL...",
            ColumnType: "nvarchar, varchar, char",
            Props:
            [
                new("maxLength",   "Độ dài ký tự tối đa (mặc định 255)"),
                new("isMultiline", "Cho phép nhiều dòng (true/false)"),
                new("rows",        "Số dòng hiển thị khi isMultiline = true"),
            ]),

        "TextArea" => new(
            Icon:       "📄",
            Title:      "TextArea — Văn bản dài",
            WhenToUse:  "Ghi chú, mô tả, nội dung dài...",
            ColumnType: "nvarchar(max), text",
            Props:
            [
                new("maxLength",   "Độ dài ký tự tối đa"),
                new("rows",        "Số dòng hiển thị (khuyến nghị ≥ 3)"),
            ]),

        "NumericBox" => new(
            Icon:       "🔢",
            Title:      "NumericBox — Số",
            WhenToUse:  "Số lượng, đơn giá, tuổi, phần trăm...",
            ColumnType: "int, decimal, float, double",
            Props:
            [
                new("minValue",  "Giá trị tối thiểu (mặc định 0)"),
                new("maxValue",  "Giá trị tối đa (mặc định 999999)"),
                new("decimals",  "Số chữ số thập phân (0 = số nguyên)"),
                new("spinStep",  "Bước nhảy khi bấm mũi tên (mặc định 1)"),
                new("allowNull", "Cho phép để trống (true/false)"),
            ]),

        "DatePicker" => new(
            Icon:       "📅",
            Title:      "DatePicker — Ngày / Ngày giờ",
            WhenToUse:  "Ngày sinh, ngày đặt hàng, ngày hết hạn...",
            ColumnType: "datetime, date",
            Props:
            [
                new("format",  "Định dạng: dd/MM/yyyy · dd/MM/yyyy HH:mm · MM/yyyy · yyyy"),
                new("minDate", "Ngày tối thiểu được chọn (bỏ trống = không giới hạn)"),
                new("maxDate", "Ngày tối đa được chọn (bỏ trống = không giới hạn)"),
            ]),

        "CheckBox" => new(
            Icon:       "☑️",
            Title:      "CheckBox — Có / Không",
            WhenToUse:  "Trạng thái bật/tắt, đồng ý điều khoản...",
            ColumnType: "bit (0/1)",
            Props:      [new("(không cần cấu hình thêm)", "Mapping trực tiếp vào cột bit")]),

        "ToggleSwitch" => new(
            Icon:       "🔘",
            Title:      "ToggleSwitch — Công tắc",
            WhenToUse:  "Active/Inactive, bật/tắt tính năng...",
            ColumnType: "bit (0/1)",
            Props:      [new("(không cần cấu hình thêm)", "Mapping trực tiếp vào cột bit")]),

        "ComboBox" => new(
            Icon:       "🔽",
            Title:      "ComboBox — Dropdown từ API",
            WhenToUse:  "Danh sách động lấy từ API endpoint...",
            ColumnType: "bất kỳ",
            Props:
            [
                new("dataSource",   "URL API endpoint trả danh sách (VD: /api/phongban)"),
                new("valueField",   "Field dùng làm giá trị lưu (mặc định: id)"),
                new("displayField", "Field hiển thị trong dropdown (mặc định: name)"),
                new("allowNull",    "Cho phép chọn trống (mặc định: true)"),
            ]),

        "RadioGroup" => new(
            Icon:       "🔘",
            Title:      "RadioGroup — Danh mục tĩnh (nút chọn)",
            WhenToUse:  "Giới tính, trạng thái đơn giản (≤ 5 options)...",
            ColumnType: "nvarchar",
            Props:
            [
                new("Lookup Code", "Mã danh mục trong Sys_Lookup (VD: GENDER, TRANGTHAI_PO)"),
                new("(cấu hình tại tab Control Props)", "→ mục Cấu hình Lookup"),
            ]),

        "LookupComboBox" => new(
            Icon:       "📋",
            Title:      "LookupComboBox — Danh mục tĩnh (dropdown)",
            WhenToUse:  "Danh mục nhiều options từ Sys_Lookup (> 5 options)...",
            ColumnType: "nvarchar",
            Props:
            [
                new("Lookup Code", "Mã danh mục trong Sys_Lookup (VD: DM_LOAI_HD)"),
                new("(cấu hình tại tab Control Props)", "→ mục Cấu hình Lookup"),
            ]),

        "LookupBox" => new(
            Icon:       "🔍",
            Title:      "LookupBox — FK tham chiếu bảng nghiệp vụ",
            WhenToUse:  "Phòng ban, nhà cung cấp, khách hàng (cột FK int)...",
            ColumnType: "int (FK)",
            Props:
            [
                new("Query Mode",     "table = bảng trực tiếp · function = TVF · sql = full SQL"),
                new("Source Table",   "[table] Tên bảng nguồn (VD: DM_PhongBan)"),
                new("Value Field",    "[table] Cột lưu vào DB (VD: PhongBan_Id)"),
                new("Display Field",  "[table] Cột hiển thị trong ô (VD: Ten_PhongBan)"),
                new("Filter SQL",     "[table] Điều kiện WHERE bổ sung (parameterized)"),
                new("Popup Columns",  "Danh sách cột hiển thị trong bảng popup chọn"),
                new("Reload OnChange","FieldCode trigger reload datasource khi thay đổi"),
                new("(cấu hình tại tab Control Props)", "→ mục FK Lookup"),
            ]),

        "TreeLookupBox" => new(
            Icon:       "🌳",
            Title:      "TreeLookupBox — Danh sách dạng cây (cha/con)",
            WhenToUse:  "Phòng ban theo cấp, danh mục có phân cấp cha/con...",
            ColumnType: "int (FK)",
            Props:
            [
                new("Source Table",    "Tên bảng nguồn có cột Parent_Id (VD: DM_PhongBan)"),
                new("Value Field",     "Cột lưu vào DB (VD: PhongBan_Id)"),
                new("Display Field",   "Cột hiển thị trong ô và trên cây (VD: Ten_PhongBan)"),
                new("Parent Column",   "Tên cột chứa Parent Id — bắt buộc (VD: Parent_Id)"),
                new("Filter SQL",      "Điều kiện lọc, hỗ trợ @TenantId, @FieldRef (VD: ChiNhanh_Id = @ChiNhanhId)"),
                new("Reload OnChange", "FieldCode trigger reload cây khi thay đổi"),
                new("(cấu hình tại tab Control Props)", "→ mục FK Lookup + ParentColumn"),
            ]),

        "AttachmentBox" => new(
            Icon:       "📎",
            Title:      "AttachmentBox — Đính kèm tệp (upload)",
            WhenToUse:  "Ảnh/PDF/Office. TỰ CHỌN chế độ theo cờ IsVirtual:\n" +
                        "• IsVirtual=BẬT  → ĐA TỆP: liên kết ở bảng phụ TT_TepDinhKem (không cần cột). Hồ sơ, ảnh sản phẩm.\n" +
                        "• IsVirtual=TẮT (map cột int) → 1 TỆP: lưu Id tệp vào cột (kiểu Logo_Id). Logo, avatar.",
            ColumnType: "int (khi 1-tệp) · — (khi đa tệp/IsVirtual)",
            Props:
            [
                new("loai",         "Phân loại tệp tuỳ chọn (VD: HopDong, Anh). Bỏ trống = không phân loại."),
                new("ownerTable",   "[đa tệp] Bảng chủ. Bỏ trống = tự suy từ form. (1-tệp không cần)"),
                new("ownerIdField", "[đa tệp] Khóa record trong context để lấy Owner_Id (mặc định: Id)."),
                new("(bảo mật)",    "Server tự kiểm allowlist + magic-byte + chặn mã thực thi; ảnh được nén + tạo thumbnail."),
            ]),

        _ => new(
            Icon:       "ℹ️",
            Title:      editorType,
            WhenToUse:  "",
            ColumnType: "",
            Props:      [])
    };

    /// <summary>True khi đang có cấu hình FK Lookup (dùng để confirm trước khi đổi type).</summary>
    private bool HasFkLookupConfig =>
        !string.IsNullOrWhiteSpace(_fkTableName)
        || !string.IsNullOrWhiteSpace(_fkValueField)
        || !string.IsNullOrWhiteSpace(_fkFunctionName)
        || !string.IsNullOrWhiteSpace(_fkSelectSql)
        || FkPopupColumns.Count > 0;

    /// <summary>Xóa toàn bộ FK Lookup config khi user xác nhận đổi EditorType.</summary>
    private void ClearFkLookupConfig()
    {
        _isRebuildingProps = true;
        _queryMode       = "table";
        _fkTableName     = "";
        _fkValueField    = "";
        _fkDisplayField  = "";
        _fkFilterSql     = "";
        _fkOrderBy       = "";
        _fkSearchEnabled = true;
        _fkFunctionName  = "";
        _fkSelectSql     = "";
        FkPopupColumns.Clear();
        FkFilterParams.Clear();
        FkFunctionParams.Clear();
        ReloadOnChangeFields.Clear();
        DataSourceConditions.Clear();
        // LookupBox new props (Migration 014)
        _editBoxMode          = "TextOnly";
        _codeField            = "";
        _dropDownWidth        = 600;
        _dropDownHeight       = 400;
        _reloadTriggerField   = "";
        // TreeLookupBox (Migration 021 + 069)
        _parentColumn         = "";
        _treeSelectableLevel  = "all";
        // Thêm mới entity (Migration 022)
        _allowAddNew          = false;
        _addFormCode          = "";
        _isRebuildingProps = false;
        // Raise tất cả property liên quan
        RaisePropertyChanged(nameof(QueryMode));
        RaisePropertyChanged(nameof(IsTableMode));
        RaisePropertyChanged(nameof(IsFunctionMode));
        RaisePropertyChanged(nameof(IsSqlMode));
        RaisePropertyChanged(nameof(FkTableName));
        RaisePropertyChanged(nameof(FkValueField));
        RaisePropertyChanged(nameof(FkDisplayField));
    }

    // ── Query Mode ─────────────────────────────────────────────────

    private string _queryMode = "table";
    /// <summary>
    /// Chế độ truy vấn dữ liệu lookup:
    /// "table" = Bảng/View + WHERE; "function" = TVF; "sql" = Full SQL.
    /// </summary>
    public string QueryMode
    {
        get => _queryMode;
        set
        {
            if (SetProperty(ref _queryMode, value))
            {
                RaisePropertyChanged(nameof(IsTableMode));
                RaisePropertyChanged(nameof(IsFunctionMode));
                RaisePropertyChanged(nameof(IsSqlMode));
                if (!_isRebuildingProps) RebuildControlPropsJson();
            }
        }
    }

    public bool IsTableMode    => _queryMode == "table";
    public bool IsFunctionMode => _queryMode == "function";
    public bool IsSqlMode      => _queryMode == "sql";

    // ── Function Mode ──────────────────────────────────────────────

    private string _fkFunctionName = "";
    /// <summary>Tên TVF (Table-Valued Function). VD: "fn_GetPhongBanHieuLuc".</summary>
    public string FkFunctionName
    {
        get => _fkFunctionName;
        set { if (SetProperty(ref _fkFunctionName, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    /// <summary>Danh sách tham số của TVF — thứ tự quan trọng (khớp với định nghĩa hàm).</summary>
    public ObservableCollection<FunctionParam> FkFunctionParams { get; } = [];

    /// <summary>Nguồn tham số TVF.</summary>
    public List<string> FunctionParamSourceTypes { get; } = ["field", "system"];

    /// <summary>Tham số hệ thống có sẵn.</summary>
    public List<string> SystemKeyOptions { get; } = ["@TenantId", "@Today", "@CurrentUser"];

    // ── SQL Mode ───────────────────────────────────────────────────

    private string _fkSelectSql = "";
    /// <summary>
    /// Full SELECT SQL (queryMode = "sql"). Phải có alias khớp ValueField + DisplayField.
    /// VD: "SELECT p.Id, p.Ten FROM DM_PhongBan p JOIN ... WHERE ..."
    /// </summary>
    public string FkSelectSql
    {
        get => _fkSelectSql;
        set { if (SetProperty(ref _fkSelectSql, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    private string _fkTableName = "";
    /// <summary>Tên bảng DB nguồn. VD: "DM_PhongBan".</summary>
    public string FkTableName
    {
        get => _fkTableName;
        set { if (SetProperty(ref _fkTableName, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    private string _fkValueField = "";
    /// <summary>Cột lưu vào DB (FK — int). VD: "PhongBan_Id".</summary>
    public string FkValueField
    {
        get => _fkValueField;
        set { if (SetProperty(ref _fkValueField, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    private string _fkDisplayField = "";
    /// <summary>Cột hiển thị chính trong ô input. VD: "Ten_PhongBan".</summary>
    public string FkDisplayField
    {
        get => _fkDisplayField;
        set { if (SetProperty(ref _fkDisplayField, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    private string _fkFilterSql = "";
    /// <summary>
    /// Điều kiện lọc bổ sung (parameterized). VD: "Is_Active = 1 AND Loai = @LoaiField".
    /// KHÔNG lọc cột Tenant_Id — cột đã bỏ (ADR-035).
    /// Các tham số hệ thống (@TenantId, @CurrentUser) được inject tự động lúc runtime.
    /// </summary>
    public string FkFilterSql
    {
        get => _fkFilterSql;
        set
        {
            if (SetProperty(ref _fkFilterSql, value) && !_isRebuildingProps)
            {
                RebuildControlPropsJson();
                RecomputeCascadeWarnings();
            }
        }
    }

    private string _fkOrderBy = "";
    /// <summary>Sắp xếp kết quả. VD: "Ten_PhongBan ASC".</summary>
    public string FkOrderBy
    {
        get => _fkOrderBy;
        set { if (SetProperty(ref _fkOrderBy, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    private bool _fkSearchEnabled = true;
    /// <summary>Cho phép search trong popup (incremental search).</summary>
    public bool FkSearchEnabled
    {
        get => _fkSearchEnabled;
        set { if (SetProperty(ref _fkSearchEnabled, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    /// <summary>Danh sách cột hiển thị trong popup dropdown của LookupBox.</summary>
    public ObservableCollection<FkColumnConfig> FkPopupColumns { get; } = [];

    /// <summary>
    /// Danh sách tham số động trong filterSql — mỗi item ánh xạ @Param → FieldCode trong form.
    /// Runtime engine resolve giá trị field rồi truyền vào SQL; khi field thay đổi → reload lookup.
    /// </summary>
    public ObservableCollection<FkFilterParam> FkFilterParams { get; } = [];

    /// <summary>Kiểu dữ liệu hợp lệ cho tham số filter.</summary>
    public List<string> FkParamTypes { get; } = ["String", "DateTime", "Int", "Decimal"];

    /// <summary>
    /// Danh sách FieldCode kích hoạt reload lookup khi giá trị thay đổi.
    /// VD: ["CapToChuc", "LoaiNhanVien"] → bất kỳ field nào thay đổi thì reload.
    /// </summary>
    public ObservableCollection<string> ReloadOnChangeFields { get; } = [];

    /// <summary>Input tạm để thêm FieldCode vào ReloadOnChangeFields.</summary>
    private string _reloadOnChangeInput = "";
    public string ReloadOnChangeInput
    {
        get => _reloadOnChangeInput;
        set => SetProperty(ref _reloadOnChangeInput, value);
    }

    /// <summary>
    /// Danh sách điều kiện đổi bảng nguồn dữ liệu.
    /// Khi field trong form thoả điều kiện → runtime đổi sang tableName khác.
    /// </summary>
    public ObservableCollection<DataSourceCondition> DataSourceConditions { get; } = [];

    /// <summary>Các phép so sánh hợp lệ trong DataSourceCondition.WhenOp.</summary>
    public List<string> WhenOpOptions { get; } =
        ["eq", "neq", "gt", "gte", "lt", "lte", "contains", "startsWith"];

    // ── LookupBox new props (Migration 014) ───────────────────────────

    /// <summary>
    /// Chế độ hiển thị EditBox khi đã chọn FK record (chỉ LookupBox).
    /// "TextOnly" | "CodeAndName" | "Custom". Mặc định: "TextOnly".
    /// </summary>
    private string _editBoxMode = "TextOnly";
    public string EditBoxMode
    {
        get => _editBoxMode;
        set { if (SetProperty(ref _editBoxMode, value)) { RaisePropertyChanged(nameof(IsCodeAndNameMode)); IsDirty = true; } }
    }

    /// <summary>True khi EditBoxMode = "CodeAndName" — hiện thêm input CodeField.</summary>
    public bool IsCodeAndNameMode => _editBoxMode == "CodeAndName";

    /// <summary>Tên cột mã code trong data source — dùng khi EditBoxMode = "CodeAndName".</summary>
    private string _codeField = "";
    public string CodeField
    {
        get => _codeField;
        set { if (SetProperty(ref _codeField, value)) IsDirty = true; }
    }

    /// <summary>
    /// Import: bỏ Filter_Sql (lọc cha cascade) → tra Mã con trên toàn bảng (Ui_Field_Lookup.Import_Global_Code).
    /// Chỉ bật cho FK có Mã con DUY NHẤT toàn cục (vd chi nhánh); trùng Mã → engine từ chối cả file khi import.
    /// </summary>
    private bool _importGlobalCode;
    public bool ImportGlobalCode
    {
        get => _importGlobalCode;
        set { if (SetProperty(ref _importGlobalCode, value)) IsDirty = true; }
    }

    /// <summary>Chiều rộng popup grid (px). Mặc định: 600.</summary>
    private int _dropDownWidth = 600;
    public int DropDownWidth
    {
        get => _dropDownWidth;
        set { if (SetProperty(ref _dropDownWidth, value)) IsDirty = true; }
    }

    /// <summary>Chiều cao popup grid (px). Mặc định: 400.</summary>
    private int _dropDownHeight = 400;
    public int DropDownHeight
    {
        get => _dropDownHeight;
        set { if (SetProperty(ref _dropDownHeight, value)) IsDirty = true; }
    }

    /// <summary>
    /// FieldCode của field khác trong form — khi thay đổi, LookupBox tự clear + reload.
    /// Lưu vào Ui_Field_Lookup.Reload_Trigger_Field (single field, đơn giản nhất).
    /// </summary>
    private string _reloadTriggerField = "";
    public string ReloadTriggerField
    {
        get => _reloadTriggerField;
        set { if (SetProperty(ref _reloadTriggerField, value)) { IsDirty = true; RecomputeCascadeWarnings(); } }
    }

    // ── Cảnh báo cascade (P2/P3) — chống cấu hình sai field ảo + cascade ──────

    /// <summary>Regex tách tham số @Name trong Filter SQL.</summary>
    private static readonly System.Text.RegularExpressions.Regex SqlParamRegex =
        new(@"@(\w+)", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>Token hệ thống server tự bơm — KHÔNG cần field trong form (spec 19 + WPF).</summary>
    private static readonly HashSet<string> CascadeSystemTokens = new(StringComparer.OrdinalIgnoreCase)
        { "TenantId", "Today", "CurrentUser", "NguoiDungID", "CongTyID_Active", "LangCode" };

    /// <summary>
    /// Cảnh báo cấu hình cascade sai (KHÔNG chặn Lưu — hiện banner + đưa vào Diễn giải):
    /// (P2) @param không khớp Field Code field nào → danh sách con rỗng;
    /// (P3) @param là field cha nhưng chưa đặt "Tự reload" → đổi cha không nạp lại con.
    /// </summary>
    public ObservableCollection<string> CascadeWarnings { get; } = [];

    /// <summary>True khi có cảnh báo cascade → hiện banner cảnh báo ở panel LookupBox.</summary>
    public bool HasCascadeWarnings => CascadeWarnings.Count > 0;

    /// <summary>Tập FieldCode hiệu lực của các field KHÁC trong form (lấy từ navigator).</summary>
    private List<string> GetSiblingFieldCodes() =>
        FieldNavigatorGroups
            .SelectMany(g => g.Fields)
            .Where(f => f.FieldId != FieldId)
            .Select(f => string.IsNullOrWhiteSpace(f.FieldCode) ? f.ColumnCode : f.FieldCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Tính lại cảnh báo cascade từ Filter SQL + field trong form.
    /// Chỉ còn P2 (@param không khớp field cha nào → danh sách con rỗng). Cảnh báo P3 cũ
    /// ("chưa đặt Tự reload") đã BỎ: renderer chế độ Bảng/View nay tự reload theo mọi @param
    /// trong Filter SQL nên không cần khai reload thủ công.
    /// Gọi khi đổi Filter SQL / sau khi nạp navigator.
    /// </summary>
    private void RecomputeCascadeWarnings()
    {
        CascadeWarnings.Clear();

        // Chỉ soát cascade cho lookup động chế độ Bảng/View có Filter SQL.
        if (IsFkLookupEditor && _queryMode == "table" && !string.IsNullOrWhiteSpace(FkFilterSql))
        {
            var siblings = GetSiblingFieldCodes();
            // Navigator chưa nạp field nào → không đủ dữ liệu để soát (tránh cảnh báo sai).
            if (siblings.Count > 0)
            {
                var prms = SqlParamRegex.Matches(FkFilterSql)
                    .Select(m => m.Groups[1].Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var p in prms)
                {
                    if (CascadeSystemTokens.Contains(p)) continue;   // token hệ thống

                    var isField = siblings.Any(c => string.Equals(c, p, StringComparison.OrdinalIgnoreCase));
                    if (!isField)
                    {
                        // P2 — @param không khớp field cha nào → con rỗng.
                        CascadeWarnings.Add(
                            $"⚠ @{p}: không field nào trong form có Field Code = \"{p}\" → danh sách con sẽ RỖNG. " +
                            $"Sửa: đặt Field Code field cha = \"{p}\", hoặc sửa lại tên @param.");
                    }
                    // isField = true → runtime tự reload theo @param, không cảnh báo.
                }
            }
        }

        RaisePropertyChanged(nameof(HasCascadeWarnings));
    }

    /// <summary>Các chế độ EditBox hợp lệ cho LookupBox.</summary>
    public List<string> EditBoxModeOptions { get; } = ["TextOnly", "CodeAndName", "Custom"];

    // ── Thêm mới entity từ LookupBox (Migration 022) ──────────────────

    private bool _allowAddNew;
    /// <summary>Bật nút "➕ Thêm mới" trên LookupBox runtime → lưu Ui_Field_Lookup.Allow_Add_New.</summary>
    public bool AllowAddNew
    {
        get => _allowAddNew;
        set
        {
            if (SetProperty(ref _allowAddNew, value))
            {
                IsDirty = true;
                // Bật thêm mới → nạp danh sách Form_Code cho combobox chọn form (nếu chưa có)
                if (value && AvailableFormCodes.Count == 0) _ = LoadFormCodesAsync();
            }
        }
    }

    private string _addFormCode = "";
    /// <summary>Form_Code của Ui_Form render dialog thêm mới → lưu Ui_Field_Lookup.Add_Form_Code.</summary>
    public string AddFormCode
    {
        get => _addFormCode;
        set { if (SetProperty(ref _addFormCode, value)) IsDirty = true; }
    }

    /// <summary>Danh sách Form_Code có sẵn của tenant — nguồn cho combobox chọn form dialog thêm mới.</summary>
    public ObservableCollection<string> AvailableFormCodes { get; } = [];

    /// <summary>
    /// Nạp danh sách Form_Code thật từ <c>Ui_Form</c> của tenant qua <see cref="IFormDataService"/>.
    /// Dùng cho combobox "Form Code dialog thêm mới". Sau sự kiện: combobox có dữ liệu để chọn.
    /// </summary>
    private async Task LoadFormCodesAsync()
    {
        if (_formService is null || _appConfig is not { IsConfigured: true }) return;
        try
        {
            var forms = await _formService.GetAllFormsAsync(_appConfig.TenantId, false, _cts.Token);
            AvailableFormCodes.Clear();
            foreach (var code in forms.Select(f => f.FormCode)
                                      .Where(c => !string.IsNullOrWhiteSpace(c))
                                      .Distinct()
                                      .OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
                AvailableFormCodes.Add(code);
        }
        catch (Exception ex)
        {
            // Log lỗi — thường do bảng Ui_Form chưa cấu hình hoặc connection lỗi
            _logger?.Capture(ex, "FieldConfig.LoadFormCodes");
        }
    }

    // ── TreeLookupBox props (Migration 021) ───────────────────────────

    /// <summary>
    /// Tên cột Parent Id trong bảng nguồn — bắt buộc với TreeLookupBox.
    /// VD: "Parent_Id". Lưu vào Ui_Field_Lookup.Parent_Column.
    /// </summary>
    private string _parentColumn = "";
    public string ParentColumn
    {
        get => _parentColumn;
        set { if (SetProperty(ref _parentColumn, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    // ── TreeLookupBox: cấp node được chọn (Migration 069) ─────────────────
    private string _treeSelectableLevel = "all";
    /// <summary>Giới hạn node được chọn: "all" | "leaf" | "branch". Lưu Ui_Field_Lookup.Tree_Selectable_Level.</summary>
    public string TreeSelectableLevel
    {
        get => _treeSelectableLevel;
        set { if (SetProperty(ref _treeSelectableLevel, value)) IsDirty = true; }
    }
    /// <summary>Các mức chọn hợp lệ cho TreeLookupBox.</summary>
    public List<string> TreeSelectableLevelOptions { get; } = ["all", "leaf", "branch"];

    // ── ComboBox / LookupComboBox display props ───────────────────────

    /// <summary>
    /// Chế độ tìm kiếm trong dropdown DxComboBox.
    /// "None" | "AutoFilter" | "AutoSearch". Mặc định: "AutoFilter".
    /// </summary>
    private string _cbSearchMode = "AutoFilter";
    public string CbSearchMode
    {
        get => _cbSearchMode;
        set
        {
            if (SetProperty(ref _cbSearchMode, value))
            {
                RaisePropertyChanged(nameof(ShowSearchFilterCondition));
                if (!_isRebuildingProps) RebuildControlPropsJson();
            }
        }
    }

    /// <summary>
    /// Điều kiện so khớp khi tìm kiếm.
    /// "Contains" | "StartsWith" | "Equals". Mặc định: "Contains".
    /// </summary>
    private string _cbSearchFilterCondition = "Contains";
    public string CbSearchFilterCondition
    {
        get => _cbSearchFilterCondition;
        set { if (SetProperty(ref _cbSearchFilterCondition, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    /// <summary>True khi SearchMode != "None" — hiện dropdown SearchFilterCondition.</summary>
    public bool ShowSearchFilterCondition => _cbSearchMode != "None";

    /// <summary>Cho phép người dùng nhập text tự do (AllowUserInput). Mặc định: false.</summary>
    private bool _cbAllowUserInput;
    public bool CbAllowUserInput
    {
        get => _cbAllowUserInput;
        set { if (SetProperty(ref _cbAllowUserInput, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    /// <summary>I18n key cho placeholder khi chưa chọn. Null = dùng fallback "-- Chọn --".</summary>
    private string _cbNullTextKey = "";
    public string CbNullTextKey
    {
        get => _cbNullTextKey;
        set { if (SetProperty(ref _cbNullTextKey, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    /// <summary>
    /// Chiều rộng dropdown panel.
    /// "ContentOrEditorWidth" | "ContentWidth" | "EditorWidth". Mặc định: "ContentOrEditorWidth".
    /// </summary>
    private string _cbDropDownWidthMode = "ContentOrEditorWidth";
    public string CbDropDownWidthMode
    {
        get => _cbDropDownWidthMode;
        set { if (SetProperty(ref _cbDropDownWidthMode, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    /// <summary>Nút xóa: "Hidden" | "Auto". Mặc định: "Auto".</summary>
    private string _cbClearButton = "Auto";
    public string CbClearButton
    {
        get => _cbClearButton;
        set { if (SetProperty(ref _cbClearButton, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    /// <summary>Tên field để group items trong dropdown — chỉ ComboBox (dynamic). Null = không group.</summary>
    private string _cbGroupFieldName = "";
    public string CbGroupFieldName
    {
        get => _cbGroupFieldName;
        set { if (SetProperty(ref _cbGroupFieldName, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    /// <summary>Tên field bool để disable item trong dropdown — chỉ ComboBox (dynamic). Null = không disable.</summary>
    private string _cbDisabledFieldName = "";
    public string CbDisabledFieldName
    {
        get => _cbDisabledFieldName;
        set { if (SetProperty(ref _cbDisabledFieldName, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
    }

    /// <summary>Các chế độ SearchMode hợp lệ.</summary>
    public List<string> SearchModeOptions { get; } = ["None", "AutoFilter", "AutoSearch"];
    /// <summary>Các điều kiện so khớp khi search hợp lệ.</summary>
    public List<string> SearchFilterConditionOptions { get; } = ["Contains", "StartsWith", "Equals"];
    /// <summary>Các chế độ chiều rộng dropdown hợp lệ.</summary>
    public List<string> DropDownWidthModeOptions { get; } = ["ContentOrEditorWidth", "ContentWidth", "EditorWidth"];
    /// <summary>Các chế độ nút xóa hợp lệ.</summary>
    public List<string> ClearButtonModeOptions { get; } = ["Hidden", "Auto"];

    /// <summary>
    /// Diễn giải cấu hình hiện tại bằng tiếng Việt — sinh tự động từ JSON.
    /// Dùng để kiểm tra trước khi lưu.
    /// </summary>
    private string _configExplanation = "";
    public string ConfigExplanation
    {
        get => _configExplanation;
        private set
        {
            if (SetProperty(ref _configExplanation, value))
            {
                RaisePropertyChanged(nameof(HasConfigExplanation));
                // Sinh diễn giải mới → tự mở rộng để user thấy ngay.
                if (!string.IsNullOrEmpty(value)) _isExplanationExpanded = true;
                RaisePropertyChanged(nameof(IsExplanationExpanded));
                RaisePropertyChanged(nameof(ShowConfigExplanation));
                RaisePropertyChanged(nameof(ExplanationToggleLabel));
            }
        }
    }

    /// <summary>True khi đã có nội dung diễn giải → hiện panel kết quả.</summary>
    public bool HasConfigExplanation => !string.IsNullOrEmpty(_configExplanation);

    private bool _isExplanationExpanded = true;
    /// <summary>Thu gọn/mở rộng khối "Diễn giải cấu hình". Bấm nút Diễn giải sẽ tự mở lại.</summary>
    public bool IsExplanationExpanded
    {
        get => _isExplanationExpanded;
        set
        {
            if (SetProperty(ref _isExplanationExpanded, value))
            {
                RaisePropertyChanged(nameof(ShowConfigExplanation));
                RaisePropertyChanged(nameof(ExplanationToggleLabel));
            }
        }
    }

    /// <summary>True khi có diễn giải VÀ đang mở → hiện khối nội dung (thu gọn = ẩn nội dung).</summary>
    public bool ShowConfigExplanation => HasConfigExplanation && _isExplanationExpanded;

    /// <summary>Nhãn nút thu gọn/mở rộng khối diễn giải.</summary>
    public string ExplanationToggleLabel => _isExplanationExpanded ? "Thu gọn ▲" : "Mở rộng ▼";

    public DelegateCommand AddFkColumnCommand { get; private set; } = null!;
    public DelegateCommand<FkColumnConfig> RemoveFkColumnCommand      { get; private set; } = null!;
    public DelegateCommand<FkColumnConfig> MoveFkColumnUpCommand      { get; private set; } = null!;
    public DelegateCommand<FkColumnConfig> MoveFkColumnDownCommand    { get; private set; } = null!;
    public DelegateCommand AddFkFilterParamCommand { get; private set; } = null!;
    public DelegateCommand<FkFilterParam> RemoveFkFilterParamCommand { get; private set; } = null!;
    public DelegateCommand<string> SetQueryModeCommand { get; private set; } = null!;
    public DelegateCommand AddFunctionParamCommand { get; private set; } = null!;
    public DelegateCommand<FunctionParam> RemoveFunctionParamCommand { get; private set; } = null!;
    public DelegateCommand AddReloadFieldCommand { get; private set; } = null!;
    public DelegateCommand<string> RemoveReloadFieldCommand { get; private set; } = null!;
    public DelegateCommand AddDataSourceConditionCommand { get; private set; } = null!;
    public DelegateCommand<DataSourceCondition> RemoveDataSourceConditionCommand { get; private set; } = null!;
    public DelegateCommand ExplainConfigCommand { get; private set; } = null!;
    public DelegateCommand ToggleExplanationCommand { get; private set; } = null!;
    public DelegateCommand CopyJsonCommand      { get; private set; } = null!;

    private string _lookupCode = "";
    /// <summary>Lookup code trong Sys_Lookup. VD: 'GENDER', 'MARITAL_STATUS'.</summary>
    public string LookupCode
    {
        get => _lookupCode;
        set
        {
            if (SetProperty(ref _lookupCode, value))
            {
                _ = LoadLookupPreviewAsync(value);
                // Sync lookupCode vào ControlPropsJson để persist khi Lưu Field
                if (!_isRebuildingProps)
                    RebuildControlPropsJson();
            }
        }
    }

    /// <summary>Danh sách lookup codes có sẵn trong DB (dùng cho dropdown gợi ý).</summary>
    public ObservableCollection<string> AvailableLookupCodes { get; } = [];

    /// <summary>Preview các items của lookup code đang chọn.</summary>
    public ObservableCollection<LookupItemDto> LookupPreviewItems { get; } = [];

    private async Task LoadLookupCodesAsync()
    {
        if (_lookupService is null) return;
        try
        {
            var codes = await _lookupService.GetAllCodesAsync(_cts.Token);
            AvailableLookupCodes.Clear();
            foreach (var c in codes) AvailableLookupCodes.Add(c);
        }
        catch (Exception ex)
        {
            // Log lỗi — thường do bảng Sys_Lookup chưa được tạo (migration 004 chưa chạy)
            _logger?.Capture(ex, "FieldConfig.LoadLookupCodes");
        }
    }

    private async Task LoadLookupPreviewAsync(string code)
    {
        LookupPreviewItems.Clear();
        if (_lookupService is null || string.IsNullOrWhiteSpace(code)) return;
        var items = await _lookupService.GetByCodeAsync(code, "vi", _cts.Token);
        foreach (var i in items)
            LookupPreviewItems.Add(new LookupItemDto { ItemCode = i.ItemCode, Label = i.Label });
    }

    private int _orderNo = 1;
    public int OrderNo
    {
        get => _orderNo;
        set { if (SetProperty(ref _orderNo, value)) IsDirty = true; }
    }

    // ── Display ──────────────────────────────────────────────
    private string _labelKey = "";
    public string LabelKey
    {
        get => _labelKey;
        set
        {
            if (SetProperty(ref _labelKey, value))
            {
                _ = ResolveI18nPreviewAsync(value, v => LabelPreview = v, () => LabelPreview);
                IsDirty = true;
            }
        }
    }

    private string _placeholderKey = "";
    public string PlaceholderKey
    {
        get => _placeholderKey;
        set
        {
            if (SetProperty(ref _placeholderKey, value))
            {
                _ = ResolveI18nPreviewAsync(value, v => PlaceholderPreview = v, () => PlaceholderPreview);
                IsDirty = true;
            }
        }
    }

    private string _tooltipKey = "";
    public string TooltipKey
    {
        get => _tooltipKey;
        set
        {
            if (SetProperty(ref _tooltipKey, value))
            {
                _ = ResolveI18nPreviewAsync(value, v => TooltipPreview = v, () => TooltipPreview);
                IsDirty = true;
            }
        }
    }

    // Khi true: đang gán giá trị từ DB (resolve), KHÔNG đánh dấu dirty.
    // User gõ tay → cờ false → IsDirty = true (để bật nút Lưu).
    private bool _suppressValueDirty;

    private string _labelPreview = "";
    /// <summary>Giá trị nhãn (vi) — user nhập THẲNG; key tự sinh ngầm. Lưu vào Sys_Resource khi Lưu field.</summary>
    public string LabelPreview
    {
        get => _labelPreview;
        set
        {
            if (SetProperty(ref _labelPreview, value) && !_suppressValueDirty)
            {
                IsDirty = true;
                ApplyLabelDefaultToEmptyDisplays();   // blur ô Nhãn → điền Gợi ý/Mô tả nếu đang trống
            }
        }
    }

    /// <summary>
    /// Khi user nhập xong Nhãn rồi rời ô (binding LostFocus): nếu Gợi ý nhập / Mô tả đang TRỐNG thì
    /// lấy mặc định = text Nhãn. Ô nào user đã tự nhập riêng (không trống) thì tôn trọng, không đè.
    /// Chỉ chạy do user thao tác (gọi từ setter khi suppress=false) → không kích hoạt lúc load/resolve.
    /// Sự kiện theo sau: ô được điền sẽ dirty → ghi Sys_Resource khi Lưu field.
    /// </summary>
    private void ApplyLabelDefaultToEmptyDisplays()
    {
        var label = (_labelPreview ?? "").Trim();
        if (label.Length == 0) return;

        if (string.IsNullOrWhiteSpace(PlaceholderPreview))
            PlaceholderPreview = label;
        if (string.IsNullOrWhiteSpace(TooltipPreview))
            TooltipPreview = label;
    }

    private string _placeholderPreview = "";
    /// <summary>Giá trị placeholder (vi) — user nhập thẳng; key tự sinh ngầm.</summary>
    public string PlaceholderPreview
    {
        get => _placeholderPreview;
        set { if (SetProperty(ref _placeholderPreview, value) && !_suppressValueDirty) IsDirty = true; }
    }

    private string _tooltipPreview = "";
    /// <summary>Giá trị tooltip (vi) — user nhập thẳng; key tự sinh ngầm.</summary>
    public string TooltipPreview
    {
        get => _tooltipPreview;
        set { if (SetProperty(ref _tooltipPreview, value) && !_suppressValueDirty) IsDirty = true; }
    }

    // ── Behavior ─────────────────────────────────────────────
    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set { if (SetProperty(ref _isVisible, value)) IsDirty = true; }
    }

    private bool _isReadOnly;
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set { if (SetProperty(ref _isReadOnly, value)) IsDirty = true; }
    }

    private bool _isRequired;
    public bool IsRequired
    {
        get => _isRequired;
        set
        {
            if (SetProperty(ref _isRequired, value))
            {
                // Khi bật Required và chưa có error key → auto-suggest ngay
                if (value && string.IsNullOrEmpty(_requiredErrorKey))
                    _ = AutoSuggestRequiredErrorKeyAsync();
                RaisePropertyChanged(nameof(IsRequiredExpanded));
                IsDirty = true;
            }
        }
    }

    /// <summary>True khi IsRequired = true — hiện section error key bên dưới.</summary>
    public bool IsRequiredExpanded => _isRequired;

    private string _requiredErrorKey = "";
    public string RequiredErrorKey
    {
        get => _requiredErrorKey;
        set
        {
            if (SetProperty(ref _requiredErrorKey, value))
            {
                _ = ResolveI18nPreviewAsync(value, v => RequiredErrorKeyPreview = v, () => RequiredErrorKeyPreview);
                IsDirty = true;
            }
        }
    }

    private string _requiredErrorKeyPreview = "";
    /// <summary>Giá trị thông báo lỗi bắt buộc (vi) — user nhập thẳng; key tự sinh ngầm.</summary>
    public string RequiredErrorKeyPreview
    {
        get => _requiredErrorKeyPreview;
        set
        {
            if (SetProperty(ref _requiredErrorKeyPreview, value))
            {
                RaisePropertyChanged(nameof(HasRequiredErrorKeyPreview));
                if (!_suppressValueDirty) IsDirty = true;
            }
        }
    }

    public bool HasRequiredErrorKeyPreview => !string.IsNullOrEmpty(_requiredErrorKeyPreview);

    private bool _lockOnEdit;
    /// <summary>
    /// ADR-017: Khóa field khi form mở ở chế độ Edit (record đã tồn tại).
    /// Cho phép nhập lúc tạo, không cho sửa khi update — pattern key/code/audit field.
    /// EffectiveReadOnly = IsReadOnly OR (LockOnEdit AND FormMode=Edit).
    /// </summary>
    public bool LockOnEdit
    {
        get => _lockOnEdit;
        set { if (SetProperty(ref _lockOnEdit, value)) IsDirty = true; }
    }

    private bool _showInList;
    /// <summary>
    /// Hiển thị cột này trong lưới danh sách Master Data (Ui_Field.Show_In_List).
    /// Mặc định false — chỉ hiện trong form chi tiết.
    /// Sự kiện theo sau: cột sẽ được lấy bởi MasterDataRepository.GetListAsync khi render lưới.
    /// </summary>
    public bool ShowInList
    {
        get => _showInList;
        set { if (SetProperty(ref _showInList, value)) IsDirty = true; }
    }

    private bool _isVirtual;
    /// <summary>
    /// Field UI-only, không map tới cột DB. Save layer bỏ qua field này khi ghi DB.
    /// Dùng cho helper field cascading (ví dụ: TinhThanh lọc XaPhuong).
    /// </summary>
    public bool IsVirtual
    {
        get => _isVirtual;
        set
        {
            if (SetProperty(ref _isVirtual, value))
            {
                IsDirty = true;
                RaisePropertyChanged(nameof(IsColumnListEmpty));
                RaisePropertyChanged(nameof(CanTypeColumnCode));
                RaisePropertyChanged(nameof(CanConfigMasking));
                AutoDeriveI18nKeys();   // đổi virtual → cột hiệu lực đổi (ColumnCode↔FieldCode)
            }
        }
    }

    private bool _isUnique;
    /// <summary>Field phải duy nhất — backend chống trùng (Ui_Field.Is_Unique). Dùng cho mã định danh.</summary>
    public bool IsUnique
    {
        get => _isUnique;
        set
        {
            if (SetProperty(ref _isUnique, value))
            {
                IsDirty = true;
                RaisePropertyChanged(nameof(IsUniqueExpanded));
                RaisePropertyChanged(nameof(UniqueErrorKey));
                if (value) _ = ResolveI18nPreviewAsync(UniqueErrorKey, v => UniqueErrorKeyPreview = v);
            }
        }
    }

    /// <summary>True khi IsUnique = true — hiện section thông báo trùng bên dưới.</summary>
    public bool IsUniqueExpanded => _isUnique;

    /// <summary>
    /// Key thông báo trùng (đa ngôn ngữ) — tự sinh theo pattern {tableCode}.val.{columnCode}.unique.
    /// Khớp key backend emit khi phát hiện trùng. Readonly (deterministic).
    /// </summary>
    public string UniqueErrorKey
    {
        get
        {
            var t = TableCode.ToLowerInvariant();
            var c = ColumnCode.ToLowerInvariant();
            return string.IsNullOrEmpty(t) || string.IsNullOrEmpty(c) ? "" : $"{t}.val.{c}.unique";
        }
    }

    private string _uniqueErrorKeyPreview = "";
    /// <summary>Preview bản dịch (vi) của UniqueErrorKey.</summary>
    public string UniqueErrorKeyPreview
    {
        get => _uniqueErrorKeyPreview;
        set
        {
            if (SetProperty(ref _uniqueErrorKeyPreview, value))
            {
                if (!_suppressValueDirty) IsDirty = true;
                RaisePropertyChanged(nameof(HasUniqueErrorKeyPreview));
            }
        }
    }

    public bool HasUniqueErrorKeyPreview => !string.IsNullOrWhiteSpace(_uniqueErrorKeyPreview);

    // ── Layout ───────────────────────────────────────────────

    private byte _colSpan = 1;
    /// <summary>Độ rộng field trong grid: 1 = 1/3, 2 = 2/3, 3 = full width.</summary>
    public byte ColSpan
    {
        get => _colSpan;
        set { if (SetProperty(ref _colSpan, value)) IsDirty = true; }
    }

    // ── Control Props tab ────────────────────────────────────
    public ObservableCollection<ControlPropValue> ControlProps { get; } = [];

    private string _controlPropsJson = "{}";
    public string ControlPropsJson { get => _controlPropsJson; set => SetProperty(ref _controlPropsJson, value); }

    // ── Rules tab ────────────────────────────────────────────
    public ObservableCollection<RuleSummaryDto> LinkedRules { get; } = [];


    // ── Events tab ───────────────────────────────────────────
    public ObservableCollection<EventSummaryDto> LinkedEvents { get; } = [];

    // ── State ────────────────────────────────────────────────
    private bool _isRebuildingProps;
    private bool _isDirty;
    public bool IsDirty { get => _isDirty; set => SetProperty(ref _isDirty, value); }

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    /// <summary>Lỗi load dữ liệu — hiển thị banner cảnh báo trên UI.</summary>
    private string _loadError = "";
    public string LoadError { get => _loadError; set => SetProperty(ref _loadError, value); }

    public bool HasLoadError => !string.IsNullOrEmpty(LoadError);

    private string _mode = "edit";

    // STT đề xuất cho field mới (chèn sau dòng đang chọn), truyền qua nav param "orderNo".
    private int _pendingNewOrderNo;

    // Cột chọn sẵn khi tạo field từ item "cột chưa tạo field", truyền qua nav param "columnCode".
    private string _pendingNewColumnCode = "";

    // ── Commands ─────────────────────────────────────────────
    public DelegateCommand SaveFieldCommand { get; }
    public DelegateCommand CancelCommand { get; }
    public DelegateCommand BrowseColumnCommand { get; }
    public DelegateCommand ManageI18nCommand { get; }
    public DelegateCommand AddRuleCommand { get; }
    public DelegateCommand<RuleSummaryDto> OpenRuleCommand { get; }
    public DelegateCommand<RuleSummaryDto> DeleteRuleCommand { get; }
    public DelegateCommand AddEventCommand { get; }
    public DelegateCommand<EventSummaryDto> OpenEventCommand { get; }
    public DelegateCommand<EventSummaryDto> DeleteEventCommand { get; }
    public DelegateCommand<string> OpenI18nKeyCommand { get; }
    public DelegateCommand<FieldNavItem> NavigateToFieldCommand { get; }
    public DelegateCommand RefreshNavigatorCommand { get; }
    public DelegateCommand<FieldNavItem> MoveFieldUpCommand { get; }
    public DelegateCommand<FieldNavItem> MoveFieldDownCommand { get; }
    public DelegateCommand<FieldNavItem?> ToggleBulkSelectionCommand { get; }
    public DelegateCommand<FieldMoveTargetItem?> MoveBulkToSectionCommand { get; }

    public FieldConfigViewModel(
        IRegionManager regionManager,
        IFieldDataService? fieldService = null,
        IFormDataService? formService = null,
        II18nDataService? i18nService = null,
        IRuleDataService? ruleService = null,
        IEventDataService? eventService = null,
        ISysLookupDataService? lookupService = null,
        IAppConfigService? appConfig = null,
        IDialogService? dialogService = null,
        IFormDetailDataService? formDetailService = null,
        INavigationHistoryService? history = null,
        IAppLogger? logger = null,
        IUserNotifier? notifier = null)
    {
        _regionManager     = regionManager;
        _fieldService      = fieldService;
        _formService       = formService;
        _i18nService       = i18nService;
        _ruleService       = ruleService;
        _eventService      = eventService;
        _lookupService     = lookupService;
        _appConfig         = appConfig;
        _dialogService     = dialogService;
        _formDetailService = formDetailService;
        _history           = history;
        _logger            = logger;
        _notifier          = notifier;

        SaveFieldCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), () => IsDirty)
            .ObservesProperty(() => IsDirty);
        CancelCommand = new DelegateCommand(ExecuteCancel);
        BrowseColumnCommand = new DelegateCommand(ExecuteBrowseColumn);
        ManageI18nCommand = new DelegateCommand(ExecuteManageI18n);
        AddRuleCommand  = new DelegateCommand(ExecuteAddRule);
        OpenRuleCommand = new DelegateCommand<RuleSummaryDto>(ExecuteOpenRule);
        DeleteRuleCommand    = new DelegateCommand<RuleSummaryDto>(async r => await ExecuteDeleteRuleAsync(r));
        AddEventCommand    = new DelegateCommand(ExecuteAddEvent);
        OpenEventCommand   = new DelegateCommand<EventSummaryDto>(ExecuteOpenEvent);
        DeleteEventCommand = new DelegateCommand<EventSummaryDto>(ExecuteDeleteEvent);
        OpenI18nKeyCommand = new DelegateCommand<string>(ExecuteOpenI18nKey);
        NavigateToFieldCommand         = new DelegateCommand<FieldNavItem>(ExecuteNavigateToField);
        RefreshNavigatorCommand        = new DelegateCommand(async () => await LoadFieldNavigatorAsync(_cts.Token));
        MoveFieldUpCommand   = new DelegateCommand<FieldNavItem>(async item => await ExecuteMoveFieldAsync(item, -1));
        MoveFieldDownCommand = new DelegateCommand<FieldNavItem>(async item => await ExecuteMoveFieldAsync(item, +1));
        ToggleBulkSelectionCommand = new DelegateCommand<FieldNavItem?>(ExecuteToggleBulkSelection);
        MoveBulkToSectionCommand   = new DelegateCommand<FieldMoveTargetItem?>(
                                         async t => await ExecuteMoveBulkToSectionAsync(t),
                                         _ => CanMoveBulk);
        BulkSelectedFields.CollectionChanged += (_, _) =>
        {
            RaisePropertyChanged(nameof(CanMoveBulk));
            RaisePropertyChanged(nameof(BulkMoveHeader));
            MoveBulkToSectionCommand.RaiseCanExecuteChanged();
        };

        // FK Lookup commands
        AddFkColumnCommand         = new DelegateCommand(ExecuteAddFkColumn);
        RemoveFkColumnCommand      = new DelegateCommand<FkColumnConfig>(ExecuteRemoveFkColumn);
        MoveFkColumnUpCommand      = new DelegateCommand<FkColumnConfig>(ExecuteMoveFkColumnUp);
        MoveFkColumnDownCommand    = new DelegateCommand<FkColumnConfig>(ExecuteMoveFkColumnDown);
        AddFkFilterParamCommand         = new DelegateCommand(ExecuteAddFkFilterParam);
        RemoveFkFilterParamCommand      = new DelegateCommand<FkFilterParam>(ExecuteRemoveFkFilterParam);
        SetQueryModeCommand             = new DelegateCommand<string>(mode => QueryMode = mode);
        AddFunctionParamCommand         = new DelegateCommand(ExecuteAddFunctionParam);
        RemoveFunctionParamCommand      = new DelegateCommand<FunctionParam>(ExecuteRemoveFunctionParam);
        AddReloadFieldCommand           = new DelegateCommand(ExecuteAddReloadField);
        RemoveReloadFieldCommand        = new DelegateCommand<string>(ExecuteRemoveReloadField);
        AddDataSourceConditionCommand   = new DelegateCommand(ExecuteAddDataSourceCondition);
        RemoveDataSourceConditionCommand= new DelegateCommand<DataSourceCondition>(ExecuteRemoveDataSourceCondition);
        ExplainConfigCommand            = new DelegateCommand(ExecuteExplainConfig);
        ToggleExplanationCommand        = new DelegateCommand(() => IsExplanationExpanded = !IsExplanationExpanded);
        CopyJsonCommand                 = new DelegateCommand(() => System.Windows.Clipboard.SetText(ControlPropsJson));
    }

    // ── INavigationAware ─────────────────────────────────────

    public async void OnNavigatedTo(NavigationContext navigationContext)
    {
        _mode     = navigationContext.Parameters.GetValue<string>("mode")      ?? "edit";
        FormId    = navigationContext.Parameters.GetValue<int>("formId");
        SectionId = navigationContext.Parameters.GetValue<int>("sectionId");
        FieldId   = navigationContext.Parameters.GetValue<int>("fieldId");
        // STT đề xuất cho field mới (chèn sau dòng đang chọn) — chỉ dùng ở mode "new".
        _pendingNewOrderNo = navigationContext.Parameters.GetValue<int>("orderNo");
        // Cột chọn sẵn khi tạo field từ item "cột chưa tạo field" — chỉ dùng ở mode "new".
        _pendingNewColumnCode = navigationContext.Parameters.GetValue<string>("columnCode") ?? "";
        UpdateNavigatorSelection(FieldId);
        TableCode = navigationContext.Parameters.GetValue<string>("tableCode") ?? "";
        FormCode  = navigationContext.Parameters.GetValue<string>("formCode")  ?? "";
        FormName  = navigationContext.Parameters.GetValue<string>("formName")  ?? "";

        // Mở từ danh sách form (fieldId=0, không phải tạo mới) → sẽ tự chọn field đầu sau khi load.
        // Bỏ qua breadcrumb tạm "Field #0"; lần điều hướng lại tới field thật mới ghi crumb.
        var autoPickFirstField = FieldId <= 0 && _mode != "new";

        if (!autoPickFirstField)
        {
            // Dang ky breadcrumb — hierarchical (child cua FormEditor).
            var fieldCode = navigationContext.Parameters.GetValue<string>("fieldCode") ?? "";
            _history?.RegisterNavigation(
                new NavigationCrumb
                {
                    ViewName = ViewNames.FieldConfig,
                    Title = string.IsNullOrEmpty(fieldCode) ? $"Field #{FieldId}" : $"Field: {fieldCode}",
                    Icon = "⚙",
                    Parameters = navigationContext.Parameters,
                },
                isHierarchical: true);
        }

        await LoadDataAsync();

        // Tự chọn field đầu tiên khi mở từ danh sách form → điều hướng lại tới field đó
        // (TableCode đã được suy trong LoadDataAsync nên params truyền đi đầy đủ).
        if (autoPickFirstField)
        {
            var firstField = FieldNavigatorGroups.SelectMany(g => g.Fields).FirstOrDefault();
            if (firstField is not null)
                ExecuteNavigateToField(firstField);
        }
    }

    // Tái sử dụng VM instance khi navigate giữa các field → giữ FieldNavigatorGroups, tránh reload list.
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        // KHÔNG hủy _cts ở đây. Khi user bấm field khác trong Left Navigator, view điều hướng
        // tới CHÍNH NÓ (cùng instance VM) nên cặp OnNavigatedFrom/OnNavigatedTo chạy đua nhau;
        // nếu hủy ở đây sẽ cancel luôn lần load vừa khởi động trong OnNavigatedTo → panel phải
        // giữ nguyên dữ liệu field cũ (load mới ném OperationCanceledException rồi return).
        // Việc hủy lần load TRƯỚC được xử lý ở đầu LoadDataAsync (last-navigation-wins).
    }

    // ── Load data (DB hoặc mock) ─────────────────────────────

    private async Task LoadDataAsync()
    {
        // Hủy lần load TRƯỚC (nếu user bấm nhanh sang field khác) rồi mở token mới cho lần này.
        // Đặt ở đây — KHÔNG đặt ở OnNavigatedFrom — để tránh race khi điều hướng tới cùng view:
        // mỗi lần load tự hủy lần load cũ, đảm bảo "click cuối cùng thắng".
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();

        if (_fieldService is not null && _appConfig is { IsConfigured: true })
        {
            await LoadFromDatabaseAsync();
        }
        else
        {
            // Chưa cấu hình DB → trả về trạng thái rỗng, hiện thông báo
            AvailableColumns.Clear();
            LinkedRules.Clear();
            LinkedEvents.Clear();
            LoadError = "Chưa cấu hình kết nối DB. Vào Settings để nhập Connection String.";
            RaisePropertyChanged(nameof(HasLoadError));
            IsLoading = false;
            IsDirty   = false;
        }
    }

    /// <summary>
    /// Xóa sạch mọi giá trị field-cụ-thể về mặc định trước khi mở form ở chế độ "thêm mới".
    /// Cần thiết vì VM tái sử dụng cùng instance giữa các lần điều hướng (IsNavigationTarget=true):
    /// không reset thì panel sẽ dính dữ liệu của field vừa xem (Field Code, nhãn/i18n, cờ Virtual,
    /// Required/Unique, cấu hình FK Lookup...). Gán thẳng backing field + raise thủ công để tránh
    /// kích hoạt side-effect (resolve i18n, dirty, RebuildControlPropsJson) của các setter.
    /// Sự kiện theo sau: caller set OrderNo (STT chèn) rồi IsDirty=false.
    /// </summary>
    private void ResetFieldStateForNew()
    {
        _suppressValueDirty = true;

        // Cột / mã / tên section
        _selectedColumn = null;
        _columnCode     = "";
        _fieldCode      = "";
        _netType        = "";
        _sectionName    = "";

        // Hiển thị (label/placeholder/tooltip) + preview i18n
        _labelKey = ""; _placeholderKey = ""; _tooltipKey = "";
        _labelPreview = ""; _placeholderPreview = ""; _tooltipPreview = "";

        // Hành vi
        _isVisible = true; _isReadOnly = false; _isRequired = false;
        _requiredErrorKey = ""; _requiredErrorKeyPreview = "";
        _lockOnEdit = false; _showInList = false; _isVirtual = false;
        _isUnique = false; _uniqueErrorKeyPreview = "";

        // Layout + control props
        _colSpan = 1;
        _controlPropsJson = "{}";
        ControlProps.Clear();
        _configExplanation = "";
        _isExplanationExpanded = true;

        // Sys_Lookup (RadioGroup / LookupComboBox)
        _lookupCode = "";
        LookupPreviewItems.Clear();

        // ComboBox display props
        _cbSearchMode = "AutoFilter"; _cbSearchFilterCondition = "Contains";
        _cbAllowUserInput = false; _cbNullTextKey = "";
        _cbDropDownWidthMode = "ContentOrEditorWidth"; _cbClearButton = "Auto";
        _cbGroupFieldName = ""; _cbDisabledFieldName = "";

        // Rules / Events liên kết — field mới chưa có
        LinkedRules.Clear();
        LinkedEvents.Clear();

        // FK Lookup (LookupBox/TreeLookupBox/ComboBox dynamic) — tái dùng hàm clear sẵn có
        ClearFkLookupConfig();
        _reloadOnChangeInput = "";
        CascadeWarnings.Clear();

        // Raise toàn bộ property để UI cập nhật về trạng thái rỗng
        RaisePropertyChanged(nameof(SelectedColumn));
        RaisePropertyChanged(nameof(DataTypeDisplay));
        RaisePropertyChanged(nameof(HasDataType));
        RaisePropertyChanged(nameof(ColumnCode));
        RaisePropertyChanged(nameof(FieldCode));
        RaisePropertyChanged(nameof(NetType));
        RaisePropertyChanged(nameof(SectionName));
        RaisePropertyChanged(nameof(IsColumnListEmpty));
        RaisePropertyChanged(nameof(CanTypeColumnCode));
        RaisePropertyChanged(nameof(LabelKey));
        RaisePropertyChanged(nameof(PlaceholderKey));
        RaisePropertyChanged(nameof(TooltipKey));
        RaisePropertyChanged(nameof(LabelPreview));
        RaisePropertyChanged(nameof(PlaceholderPreview));
        RaisePropertyChanged(nameof(TooltipPreview));
        RaisePropertyChanged(nameof(IsVisible));
        RaisePropertyChanged(nameof(IsReadOnly));
        RaisePropertyChanged(nameof(IsRequired));
        RaisePropertyChanged(nameof(IsRequiredExpanded));
        RaisePropertyChanged(nameof(RequiredErrorKey));
        RaisePropertyChanged(nameof(RequiredErrorKeyPreview));
        RaisePropertyChanged(nameof(HasRequiredErrorKeyPreview));
        RaisePropertyChanged(nameof(LockOnEdit));
        RaisePropertyChanged(nameof(ShowInList));
        RaisePropertyChanged(nameof(IsVirtual));
        RaisePropertyChanged(nameof(IsUnique));
        RaisePropertyChanged(nameof(IsUniqueExpanded));
        RaisePropertyChanged(nameof(UniqueErrorKey));
        RaisePropertyChanged(nameof(UniqueErrorKeyPreview));
        RaisePropertyChanged(nameof(HasUniqueErrorKeyPreview));
        RaisePropertyChanged(nameof(ColSpan));
        RaisePropertyChanged(nameof(ControlPropsJson));
        RaisePropertyChanged(nameof(ConfigExplanation));
        RaisePropertyChanged(nameof(HasConfigExplanation));
        RaisePropertyChanged(nameof(ShowConfigExplanation));
        RaisePropertyChanged(nameof(ExplanationToggleLabel));
        RaisePropertyChanged(nameof(LookupCode));
        RaisePropertyChanged(nameof(HasCascadeWarnings));
        RaisePropertyChanged(nameof(ReloadOnChangeInput));
        RaisePropertyChanged(nameof(CbSearchMode));
        RaisePropertyChanged(nameof(CbSearchFilterCondition));
        RaisePropertyChanged(nameof(ShowSearchFilterCondition));
        RaisePropertyChanged(nameof(CbAllowUserInput));
        RaisePropertyChanged(nameof(CbNullTextKey));
        RaisePropertyChanged(nameof(CbDropDownWidthMode));
        RaisePropertyChanged(nameof(CbClearButton));
        RaisePropertyChanged(nameof(CbGroupFieldName));
        RaisePropertyChanged(nameof(CbDisabledFieldName));

        // Editor type về mặc định "TextBox" — ép _selectedEditorType = "" để SetProperty luôn detect
        // change → LoadControlPropSchema() rebuild ControlProps sạch (đang trong load, _isLoading=true
        // nên không bật hộp thoại xác nhận đổi kiểu).
        _selectedEditorType = "";
        SelectedEditorType  = "TextBox";

        _suppressValueDirty = false;
    }

    /// <summary>
    /// Load field detail, columns, linked rules/events từ DB.
    /// Tách riêng từng bước — lỗi ở bước phụ (rules/events) không làm mất dữ liệu chính.
    /// </summary>
    /// <summary>
    /// Xử lý khi NẠP cấu hình FK/ComboBox từ Ui_Field_Lookup thất bại: reset cờ rebuild,
    /// bật cờ chặn-save, ghi log (file), báo user (banner shell) + hiện banner trên màn field.
    /// Sự kiện theo sau: nút Lưu bị khóa (ExecuteSaveAsync chặn) để không ghi đè rỗng làm mất data.
    /// Nguyên nhân hay gặp: DB thiếu cột mới (migration chưa chạy) → SqlException "Invalid column name".
    /// </summary>
    private void HandleFkConfigLoadError(Exception ex)
    {
        _isRebuildingProps  = false;
        _fkConfigLoadFailed = true;

        _logger?.Capture(ex, $"FieldConfig.LoadLookupConfig field #{FieldId}");
        _notifier?.NotifyError(
            "Không nạp được cấu hình nguồn dữ liệu (LookupBox/ComboBox). Nút Lưu đã bị KHÓA để tránh " +
            "ghi đè làm mất cấu hình. Kiểm tra migration DB (db/068, db/069) rồi khởi động lại ConfigStudio.",
            ex);

        LoadError = "Lỗi nạp cấu hình FK Lookup: " + ex.Message +
            "  →  Đã KHÓA nút Lưu để bảo vệ dữ liệu. Chạy migration DB còn thiếu (db/068, db/069) " +
            "rồi mở lại field. KHÔNG bấm Lưu khi panel đang trống.";
        RaisePropertyChanged(nameof(HasLoadError));
    }

    private async Task LoadFromDatabaseAsync()
    {
        IsLoading  = true;
        LoadError  = "";
        _fkConfigLoadFailed = false;   // reset cờ chặn-save mỗi lần load field mới

        var ct       = _cts.Token;
        var tenantId = _appConfig!.TenantId;

        try
        {
            // ── 0. Sections (cho ComboBox chọn section) ───────────────────
            System.Diagnostics.Debug.WriteLine($"[FieldConfig] LoadSections: FormId={FormId}, TenantId={tenantId}, IsConfigured={_appConfig?.IsConfigured}, ServiceNull={_formDetailService is null}");
            if (_formDetailService is not null)
            {
                var sections = await _formDetailService.GetSectionsByFormAsync(FormId, tenantId, ct);
                System.Diagnostics.Debug.WriteLine($"[FieldConfig] Sections count = {sections.Count}");
                AvailableSections.Clear();
                foreach (var s in sections)
                    AvailableSections.Add(new SectionOptionItem(s.SectionCode, s.TitleKey ?? s.SectionCode)
                        { Id = s.SectionId });

                // Restore selection sau khi load
                _selectedSection = AvailableSections.FirstOrDefault(s => s.Id == SectionId);
                RaisePropertyChanged(nameof(SelectedSection));
            }

            // ── 1. Columns (cần cho ComboBox chọn column) ─────────────────
            var tableId = await _fieldService!.GetTableIdByFormAsync(FormId, tenantId, ct);

            // Mở từ danh sách form không truyền tableCode → suy từ Sys_Table theo tableId
            // để các key i18n (label/placeholder/tooltip) sinh đúng tiền tố khi sửa cột/FieldCode.
            if (string.IsNullOrEmpty(TableCode) && tableId > 0 && _formService is not null)
            {
                var tables = await _formService.GetTablesByTenantAsync(tenantId, ct);
                TableCode = tables.FirstOrDefault(t => t.TableId == tableId)?.TableCode ?? "";
            }

            if (tableId > 0)
            {
                var columns = await _fieldService.GetColumnsByTableAsync(tableId, ct);
                AvailableColumns.Clear();
                foreach (var c in columns)
                {
                    AvailableColumns.Add(new ColumnInfoDto
                    {
                        ColumnId   = c.ColumnId,
                        ColumnCode = c.ColumnCode,
                        DataType   = c.DataType,
                        NetType    = c.NetType,
                        MaxLength  = c.MaxLength,
                        IsNullable = c.IsNullable
                    });
                }
                // Cập nhật visibility binding sau khi load xong
                RaisePropertyChanged(nameof(HasAvailableColumns));
                RaisePropertyChanged(nameof(IsColumnListEmpty));
                RaisePropertyChanged(nameof(CanTypeColumnCode));
            }

            if (_mode == "new")
            {
                // VM được tái sử dụng cùng instance (IsNavigationTarget=true) → phải xóa sạch giá trị
                // của field vừa xem, nếu không panel sẽ dính dữ liệu cũ (Field Code, i18n, Virtual, FK...).
                ResetFieldStateForNew();
                // STT = STT dòng đang chọn + 1 (đã tính ở FormEditor); 0 = nối cuối → giữ mặc định.
                if (_pendingNewOrderNo > 0) OrderNo = _pendingNewOrderNo;
                // Set IsLoading=false TRƯỚC khi chọn cột để AutoDeriveI18nKeys chạy (bị chặn khi _isLoading).
                IsLoading = false;
                // Tạo field từ cột chưa cấu hình → chọn sẵn cột đó (tự sinh key i18n theo cột).
                if (!string.IsNullOrEmpty(_pendingNewColumnCode))
                {
                    var col = AvailableColumns.FirstOrDefault(c =>
                        string.Equals(c.ColumnCode, _pendingNewColumnCode, StringComparison.OrdinalIgnoreCase));
                    if (col is not null) SelectedColumn = col;
                    else                 ColumnCode     = _pendingNewColumnCode;
                    _pendingNewColumnCode = "";
                }
                IsDirty   = false;
                return;
            }

            // ── 2. Field detail (dữ liệu chính — không được sai) ──────────
            if (FieldId > 0)
            {
                var field = await _fieldService.GetFieldDetailAsync(FieldId, tenantId, ct);
                if (field is not null)
                {
                    FormId             = field.FormId;
                    SectionId          = field.SectionId ?? 0;
                    SectionName        = field.SectionCode;
                    // Khớp theo ColumnId trước; fallback theo ColumnCode khi Ui_Field.Column_Id đã
                    // "lệch" so với Sys_Column hiện hành (cột bị tạo lại / Table_Id đổi) — nhờ vậy
                    // ComboBox vẫn hiện đúng cột, và lần Lưu kế tiếp ghi lại Column_Id chuẩn.
                    SelectedColumn     = AvailableColumns.FirstOrDefault(c => c.ColumnId == field.ColumnId)
                                      ?? AvailableColumns.FirstOrDefault(c =>
                                             string.Equals(c.ColumnCode, field.ColumnCode, StringComparison.OrdinalIgnoreCase));
                    ColumnCode         = field.ColumnCode;
                    FieldCode          = field.FieldCode ?? "";

                    // Làm mờ log (Sys_Column) — đọc phòng thủ theo Column_Id; cột chưa migrate → (false, Full).
                    var (isMasked, maskMode) = await _fieldService.GetColumnMaskingAsync(field.ColumnId, ct);
                    _isLogMasked = isMasked;
                    _logMaskMode = string.IsNullOrWhiteSpace(maskMode) ? "Full" : maskMode!;
                    RaisePropertyChanged(nameof(IsLogMasked));
                    RaisePropertyChanged(nameof(LogMaskMode));
                    RaisePropertyChanged(nameof(CanConfigMasking));

                    // Import global-code (Ui_Field_Lookup) — đọc phòng thủ theo Field_Id; cột chưa migrate → false.
                    _importGlobalCode = await _fieldService.GetFkImportGlobalAsync(field.FieldId, ct);
                    RaisePropertyChanged(nameof(ImportGlobalCode));
                    // NOTE: Set _controlPropsJson (backing field) trước khi SelectedEditorType thay đổi
                    // để LoadControlPropSchema() có thể restore giá trị từ DB
                    _controlPropsJson      = field.ControlPropsJson ?? "{}";
                    // NOTE: Reset _selectedEditorType về "" để SetProperty luôn detect change,
                    // tránh trường hợp field đang là "TextBox" (default) → không trigger LoadControlPropSchema
                    _selectedEditorType    = "";
                    SelectedEditorType     = field.EditorType;
                    OrderNo                = field.OrderNo;
                    ColSpan                = field.ColSpan;
                    LabelKey               = field.LabelKey;
                    PlaceholderKey         = field.PlaceholderKey ?? "";
                    TooltipKey             = field.TooltipKey     ?? "";
                    IsVisible              = field.IsVisible;
                    IsReadOnly             = field.IsReadOnly;
                    IsRequired             = field.IsRequired;
                    _requiredErrorKey      = field.RequiredErrorKey ?? "";
                    RaisePropertyChanged(nameof(RequiredErrorKey));
                    RaisePropertyChanged(nameof(IsRequiredExpanded));
                    if (!string.IsNullOrEmpty(_requiredErrorKey))
                        _ = ResolveI18nPreviewAsync(_requiredErrorKey, v => RequiredErrorKeyPreview = v);
                    LockOnEdit             = field.LockOnEdit;
                    ShowInList             = field.ShowInList;
                    IsVirtual              = field.IsVirtual;
                    IsUnique               = field.IsUnique;

                    // ── Restore Sys_Lookup (RadioGroup / LookupComboBox) ──
                    // Lookup_Source = "static" → đọc Lookup_Code trực tiếp từ DB (không parse JSON)
                    if (IsLookupEditor)
                    {
                        await LoadLookupCodesAsync();
                        LookupCode = field.LookupCode ?? "";
                    }

                    // ── Restore FK Lookup (LookupBox) — đọc từ Ui_Field_Lookup ──
                    // Lookup_Source = "dynamic" → load FieldLookupConfig từ bảng riêng
                    if (IsFkLookupEditor && field.LookupSource == "dynamic" && _fieldService is not null)
                    {
                        try
                        {
                            var cfg = await _fieldService.GetFieldLookupConfigAsync(FieldId, ct);
                            if (cfg is null) goto skipFkRestore;

                            _isRebuildingProps = true;
                            QueryMode       = cfg.QueryMode;
                            FkValueField    = cfg.ValueColumn;
                            FkDisplayField  = cfg.DisplayColumn;
                            FkOrderBy       = cfg.OrderBy ?? "";
                            FkSearchEnabled = cfg.SearchEnabled;

                            // Phân tách source theo query mode
                            switch (cfg.QueryMode)
                            {
                                case "table":
                                    FkTableName = cfg.SourceName;
                                    FkFilterSql = cfg.FilterSql ?? "";
                                    break;
                                case "tvf":
                                    FkFunctionName = cfg.SourceName;
                                    break;
                                case "custom_sql":
                                    FkSelectSql = cfg.SourceName;
                                    break;
                            }
                            // Restore danh sách cột popup từ PopupColumnsJson
                            FkPopupColumns.Clear();
                            if (!string.IsNullOrWhiteSpace(cfg.PopupColumnsJson))
                            {
                                try
                                {
                                    var cols = JsonSerializer.Deserialize<List<JsonElement>>(cfg.PopupColumnsJson);
                                    if (cols is not null)
                                        foreach (var col in cols)
                                        {
                                            var colCfg = new FkColumnConfig
                                            {
                                                FieldName  = col.TryGetProperty("fieldName",  out var fn) ? fn.GetString() ?? "" : "",
                                                // Ưu tiên captionKey (i18n key mới); fallback caption (data cũ plain text)
                                                CaptionKey = col.TryGetProperty("captionKey", out var ck) ? ck.GetString() ?? ""
                                                           : col.TryGetProperty("caption",    out var cp) ? cp.GetString() ?? "" : "",
                                                Width      = col.TryGetProperty("width",      out var w)  ? w.GetInt32() : 150
                                            };
                                            WireFkColumnHandlers(colCfg);
                                            FkPopupColumns.Add(colCfg);
                                        }
                                }
                                catch { /* bỏ qua nếu JSON không hợp lệ */ }
                            }
                            // LookupBox new props (Migration 014)
                            _editBoxMode        = cfg.EditBoxMode;
                            _codeField          = cfg.CodeField ?? "";
                            _dropDownWidth      = cfg.DropDownWidth;
                            _dropDownHeight     = cfg.DropDownHeight;
                            _reloadTriggerField = cfg.ReloadTriggerField ?? "";
                            // Multi-Trigger (Migration 068): CSV → danh sách tag ReloadOnChangeFields.
                            ReloadOnChangeFields.Clear();
                            if (!string.IsNullOrWhiteSpace(cfg.ReloadTriggerFields))
                                foreach (var f in cfg.ReloadTriggerFields.Split(',',
                                             StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                                    ReloadOnChangeFields.Add(f);
                            _parentColumn       = cfg.ParentColumn ?? "";
                            _treeSelectableLevel = string.IsNullOrWhiteSpace(cfg.TreeSelectableLevel) ? "all" : cfg.TreeSelectableLevel;
                            _allowAddNew        = cfg.AllowAddNew;
                            _addFormCode        = cfg.AddFormCode ?? "";

                            _isRebuildingProps = false;
                            // Raise LookupBox new props sau khi _isRebuildingProps = false
                            RaisePropertyChanged(nameof(EditBoxMode));
                            RaisePropertyChanged(nameof(ParentColumn));
                            RaisePropertyChanged(nameof(TreeSelectableLevel));
                            RaisePropertyChanged(nameof(IsCodeAndNameMode));
                            RaisePropertyChanged(nameof(CodeField));
                            RaisePropertyChanged(nameof(DropDownWidth));
                            RaisePropertyChanged(nameof(DropDownHeight));
                            RaisePropertyChanged(nameof(ReloadTriggerField));
                            RaisePropertyChanged(nameof(AllowAddNew));
                            RaisePropertyChanged(nameof(AddFormCode));
                            // Field đã bật thêm mới → nạp danh sách Form_Code cho combobox chọn form
                            if (_allowAddNew) await LoadFormCodesAsync();
                            skipFkRestore:;
                        }
                        catch (Exception fkEx) { HandleFkConfigLoadError(fkEx); }
                    }

                    // ── Restore ComboBox dynamic source — đọc từ Ui_Field_Lookup ──
                    // ComboBox với Lookup_Source = "dynamic" dùng cùng bảng Ui_Field_Lookup
                    if (IsComboBoxEditor && field.LookupSource == "dynamic" && _fieldService is not null)
                    {
                        try
                        {
                            var cfg = await _fieldService.GetFieldLookupConfigAsync(FieldId, ct);
                            if (cfg is not null)
                            {
                                _isRebuildingProps = true;
                                QueryMode       = cfg.QueryMode;
                                FkValueField    = cfg.ValueColumn;
                                FkDisplayField  = cfg.DisplayColumn;
                                FkOrderBy       = cfg.OrderBy ?? "";
                                FkSearchEnabled = cfg.SearchEnabled;
                                switch (cfg.QueryMode)
                                {
                                    case "table":   FkTableName    = cfg.SourceName; FkFilterSql = cfg.FilterSql ?? ""; break;
                                    case "tvf":     FkFunctionName = cfg.SourceName; break;
                                    case "custom_sql": FkSelectSql = cfg.SourceName; break;
                                }
                                FkPopupColumns.Clear();
                                if (!string.IsNullOrWhiteSpace(cfg.PopupColumnsJson))
                                {
                                    try
                                    {
                                        var cols = JsonSerializer.Deserialize<List<JsonElement>>(cfg.PopupColumnsJson);
                                        if (cols is not null)
                                            foreach (var col in cols)
                                            {
                                                var colCfg = new FkColumnConfig
                                                {
                                                    FieldName  = col.TryGetProperty("fieldName",  out var fn) ? fn.GetString() ?? "" : "",
                                                    // Ưu tiên captionKey (i18n key mới); fallback caption (data cũ plain text)
                                                    CaptionKey = col.TryGetProperty("captionKey", out var ck) ? ck.GetString() ?? ""
                                                               : col.TryGetProperty("caption",    out var cp) ? cp.GetString() ?? "" : "",
                                                    Width      = col.TryGetProperty("width",      out var w)  ? w.GetInt32() : 150
                                                };
                                                colCfg.PropertyChanged += (_, _) => RebuildControlPropsJson();
                                                FkPopupColumns.Add(colCfg);
                                            }
                                    }
                                    catch { /* bỏ qua */ }
                                }
                                _isRebuildingProps = false;
                            }
                        }
                        catch (Exception cbEx) { HandleFkConfigLoadError(cbEx); }
                    }

                    // ── Restore ComboBox / LookupComboBox display props từ ControlPropsJson ──
                    if (IsComboBoxEditor || SelectedEditorType == "LookupComboBox")
                    {
                        // Parse trực tiếp từ JSON dict — WPF không reference backend Domain
                        var raw = ParseControlPropsJson(field.ControlPropsJson ?? "{}");
                        _isRebuildingProps = true;
                        _cbSearchMode          = GetStr(raw, "searchMode",           "AutoFilter");
                        _cbSearchFilterCondition = GetStr(raw, "searchFilterCondition", "Contains");
                        _cbAllowUserInput      = GetBool(raw, "allowUserInput",      false);
                        _cbNullTextKey         = GetStr(raw, "nullTextKey",          "");
                        _cbDropDownWidthMode   = GetStr(raw, "dropDownWidthMode",    "ContentOrEditorWidth");
                        _cbClearButton         = GetStr(raw, "clearButton",          "Auto");
                        _cbGroupFieldName      = GetStr(raw, "groupFieldName",       "");
                        _cbDisabledFieldName   = GetStr(raw, "disabledFieldName",    "");
                        _isRebuildingProps     = false;
                        RaisePropertyChanged(nameof(CbSearchMode));
                        RaisePropertyChanged(nameof(CbSearchFilterCondition));
                        RaisePropertyChanged(nameof(ShowSearchFilterCondition));
                        RaisePropertyChanged(nameof(CbAllowUserInput));
                        RaisePropertyChanged(nameof(CbNullTextKey));
                        RaisePropertyChanged(nameof(CbDropDownWidthMode));
                        RaisePropertyChanged(nameof(CbClearButton));
                        RaisePropertyChanged(nameof(CbGroupFieldName));
                        RaisePropertyChanged(nameof(CbDisabledFieldName));
                    }
                }
            }
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            // Lỗi load dữ liệu chính → báo lỗi rõ ràng, KHÔNG fallback mock
            _logger?.Capture(ex, $"FieldConfig.Load field #{FieldId}");
            LoadError = $"Lỗi tải thông tin field #{FieldId}: {ex.Message}";
            RaisePropertyChanged(nameof(HasLoadError));
            IsLoading = false;
            return;
        }

        // ── 3. Linked rules (phụ — lỗi chỉ cần warning, không crash) ──────
        if (FieldId > 0 && _ruleService is not null)
        {
            try
            {
                var rules = await _ruleService.GetRulesByFieldAsync(FieldId, _cts.Token);
                LinkedRules.Clear();
                foreach (var r in rules)
                {
                    LinkedRules.Add(new RuleSummaryDto
                    {
                        RuleId            = r.RuleId,
                        OrderNo           = r.OrderNo,
                        RuleTypeCode      = r.RuleTypeCode,
                        ExpressionPreview = r.ExpressionJson ?? "",
                        ErrorKey          = r.ErrorKey,
                        IsActive          = r.IsActive
                    });
                }
                // IsRequired là cột DB (Ui_Field.Is_Required) — đã load từ GetFieldDetailAsync
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                // Rules load thất bại (VD: chưa chạy migration 003) → warning nhỏ
                _logger?.Capture(ex, $"FieldConfig.LoadRules field #{FieldId}");
                LoadError = string.IsNullOrEmpty(LoadError)
                    ? $"Không tải được validation rules: {ex.Message}"
                    : LoadError;
                RaisePropertyChanged(nameof(HasLoadError));
            }
        }

        // ── 4. Linked events (phụ — lỗi chỉ cần warning) ─────────────────
        if (FieldId > 0 && _eventService is not null)
        {
            try
            {
                var events = await _eventService.GetEventsByFieldAsync(FieldId, _cts.Token);
                LinkedEvents.Clear();
                foreach (var e in events)
                {
                    LinkedEvents.Add(new EventSummaryDto
                    {
                        EventId          = e.EventId,
                        OrderNo          = e.OrderNo,
                        TriggerCode      = e.TriggerCode,
                        ConditionPreview = e.ConditionExpr ?? "",
                        ActionsCount     = e.ActionsCount,
                        IsActive         = e.IsActive
                    });
                }
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex) { _logger?.Capture(ex, $"FieldConfig.LoadEvents field #{FieldId}"); }
        }

        // ── 5. Field Navigator — chỉ load khi đổi form, refresh thủ công qua nút ────
        if (_navigatorLoadedFormId != FormId)
            await LoadFieldNavigatorAsync(_cts.Token);

        // Refresh JSON preview — RebuildControlPropsJson đã gọi khi set SelectedEditorType,
        // nhưng FK/ComboBox config được load sau (async) → cần gọi lại để JSON phản ánh đúng
        // FkPopupColumns + ComboBox props đã restore từ DB.
        RebuildControlPropsJson();

        // Chuẩn hóa 3 key hiển thị về canonical (.label/.placeholder/.tooltip) — fix field cũ lưu
        // key legacy / thiếu placeholder-tooltip; resolve LẠI giá trị vi từ key canonical (xem hàm).
        NormalizeFieldKeysToCanonical();

        IsLoading = false;
        IsDirty   = false;
    }

    // ── i18n preview resolver ────────────────────────────────

    /// <summary>
    /// Resolve i18n key → giá trị (vi) nạp vào ô NHẬP. Fallback rỗng (không phải key)
    /// để user gõ thẳng bản dịch. Gán qua cờ suppress để không đánh dấu dirty oan.
    /// </summary>
    private async Task ResolveI18nPreviewAsync(string key, Action<string> setter, Func<string>? current = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return; // không có key → giữ nguyên text user đang gõ
        }

        // Chốt ngữ cảnh NGAY (đồng bộ, trước await — resolve chạy fire-and-forget nên _isLoading
        // có thể đã đảo lúc await xong): chỉ giữ text đang có khi user TỰ GÕ rồi mới sinh key
        // (AutoDeriveI18nKeys chạy lúc !_isLoading). Khi đang LOAD field, text trong ô là của
        // field TRƯỚC (chưa reset) → KHÔNG giữ, để resolve ghi đè (rỗng ⇒ xóa trắng đúng).
        var preserveTypedText = !_isLoading;

        if (_i18nService is not null && _appConfig is { IsConfigured: true })
        {
            var value = await _i18nService.ResolveKeyAsync(key, "vi", _cts.Token);
            // Chưa có bản dịch trong DB nhưng user đã gõ text (interactive) → giữ text, đừng xóa trắng.
            if (string.IsNullOrEmpty(value) && preserveTypedText && current is not null && !string.IsNullOrWhiteSpace(current()))
                return;
            SetResolvedValue(setter, value ?? "");
        }
    }

    /// <summary>Gán giá trị resolve vào ô nhập mà KHÔNG kích hoạt dirty (chỉ user gõ mới dirty).</summary>
    private void SetResolvedValue(Action<string> setter, string value)
    {
        _suppressValueDirty = true;
        try { setter(value); }
        finally { _suppressValueDirty = false; }
    }

    /// <summary>
    /// Tự suy 3 key hiển thị (label/placeholder/tooltip) + required theo cú pháp chuẩn từ
    /// TableCode + cột hiệu lực. Thay cho nút "Tạo key" — sinh ngầm khi chọn cột / đổi FieldCode.
    /// KHÔNG chạy lúc đang load (giữ nguyên key gốc từ DB, kể cả key legacy không theo pattern).
    /// Sự kiện theo sau: setter mỗi *Key tự resolve giá trị VI vào ô nhập tương ứng.
    /// </summary>
    private void AutoDeriveI18nKeys()
    {
        if (_isLoading) return;

        var label = BuildFieldKey("label");
        if (string.IsNullOrEmpty(label)) return;

        LabelKey       = label;
        PlaceholderKey = BuildFieldKey("placeholder");
        TooltipKey     = BuildFieldKey("tooltip");
        if (IsRequired)
        {
            var effectiveCode = (IsVirtual ? FieldCode : ColumnCode).ToLowerInvariant();
            RequiredErrorKey  = $"{TableCode.ToLowerInvariant()}.val.{effectiveCode}.required";
        }
    }

    /// <summary>
    /// Build key hiển thị field theo chuẩn spec 10 §1b: <c>{tableCode}.field.{code}.{qualifier}</c>
    /// (qualifier = label / placeholder / tooltip). Trả rỗng nếu thiếu TableCode hoặc cột hiệu lực.
    /// </summary>
    private string BuildFieldKey(string qualifier)
    {
        var tableCode     = TableCode.ToLowerInvariant();
        var effectiveCode = (IsVirtual ? FieldCode : ColumnCode).ToLowerInvariant();
        return string.IsNullOrEmpty(tableCode) || string.IsNullOrEmpty(effectiveCode)
            ? ""
            : $"{tableCode}.field.{effectiveCode}.{qualifier}";
    }

    /// <summary>
    /// Chuẩn hóa 3 key hiển thị về đúng cú pháp spec 10 cho field ĐANG SỬA — kể cả field cũ lưu key
    /// legacy (thiếu <c>.label</c>) hoặc placeholder/tooltip rỗng/NULL. Gán backing field để hiển thị
    /// key canonical, đồng thời RESOLVE LẠI giá trị vi TỪ KEY CANONICAL — vì canonical mới là nguồn sự
    /// thật khi Lưu (RegisterI18nKeysAsync ghi theo canonical). Nếu chỉ giữ giá trị resolve từ key
    /// legacy/NULL thì ô sẽ dính giá trị field TRƯỚC (key rỗng ⇒ resolve bị skip, không xóa ô).
    /// Resolve chạy lúc _isLoading=true ⇒ canonical rỗng sẽ xóa trắng ô (không giữ giá trị cũ).
    /// Sự kiện theo sau: key canonical hiển thị ngay + được ghi khi Lưu field (key cũ thành orphan, vô hại).
    /// </summary>
    private void NormalizeFieldKeysToCanonical()
    {
        var label = BuildFieldKey("label");
        if (string.IsNullOrEmpty(label)) return; // thiếu table/code → giữ nguyên

        if (_labelKey != label)
        {
            _labelKey = label;
            RaisePropertyChanged(nameof(LabelKey));
            _ = ResolveI18nPreviewAsync(label, v => LabelPreview = v, () => LabelPreview);
        }
        var placeholder = BuildFieldKey("placeholder");
        if (_placeholderKey != placeholder)
        {
            _placeholderKey = placeholder;
            RaisePropertyChanged(nameof(PlaceholderKey));
            _ = ResolveI18nPreviewAsync(placeholder, v => PlaceholderPreview = v, () => PlaceholderPreview);
        }
        var tooltip = BuildFieldKey("tooltip");
        if (_tooltipKey != tooltip)
        {
            _tooltipKey = tooltip;
            RaisePropertyChanged(nameof(TooltipKey));
            _ = ResolveI18nPreviewAsync(tooltip, v => TooltipPreview = v, () => TooltipPreview);
        }
    }

    // ── Required error key generator ─────────────────────────

    /// <summary>
    /// Auto-suggest Required_Error_Key theo pattern {tableCode}.val.{columnCode}.required.
    /// Dùng khi user bật toggle Is_Required và key chưa được nhập.
    /// </summary>
    private async Task AutoSuggestRequiredErrorKeyAsync()
    {
        var columnCode = ColumnCode.ToLowerInvariant();
        var tableCode  = TableCode.ToLowerInvariant();
        if (string.IsNullOrEmpty(columnCode) || string.IsNullOrEmpty(tableCode)) return;

        var key = $"{tableCode}.val.{columnCode}.required";
        _requiredErrorKey = key;
        RaisePropertyChanged(nameof(RequiredErrorKey));
        _ = ResolveI18nPreviewAsync(key, v => RequiredErrorKeyPreview = v);
    }

    // ── Field Navigator loader ────────────────────────────────

    /// <summary>
    /// Load danh sách field của form (grouped by section) cho Left Panel Navigator.
    /// Lỗi bị bỏ qua — navigator là tính năng phụ trợ, không ảnh hưởng main flow.
    /// </summary>
    private async Task LoadFieldNavigatorAsync(CancellationToken ct)
    {
        if (_formDetailService is null || _appConfig is not { IsConfigured: true }) return;

        try
        {
            var tenantId     = _appConfig.TenantId;
            var sectionsTask = _formDetailService.GetSectionsByFormAsync(FormId, tenantId, ct);
            var fieldsTask   = _formDetailService.GetFieldsByFormAsync(FormId, tenantId, ct);
            await Task.WhenAll(sectionsTask, fieldsTask);

            var sections = sectionsTask.Result;
            var fields   = fieldsTask.Result;

            // Group fields by SectionCode
            var fieldsBySec = fields
                .GroupBy(f => f.SectionCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderBy(f => f.OrderNo).ToList(),
                              StringComparer.OrdinalIgnoreCase);

            ExecuteClearBulkSelection();   // item cũ sắp bị thay thế → tránh giữ reference stale
            FieldNavigatorGroups.Clear();
            // Pass 1 (đồng bộ): dựng group/item với mã → list hiện ngay. Ghi TitleKey/LabelKey để
            // pass 2 resolve tên (vi) và cập nhật qua INotifyPropertyChanged (không chặn hiển thị).
            var sectionTitleKeys = new List<(FieldNavGroup Group, string? TitleKey)>();
            foreach (var sec in sections.OrderBy(s => s.OrderNo))
            {
                var group = new FieldNavGroup { SectionId = sec.SectionId, SectionCode = sec.SectionCode };
                if (fieldsBySec.TryGetValue(sec.SectionCode, out var secFields))
                {
                    foreach (var f in secFields)
                        group.Fields.Add(new FieldNavItem
                        {
                            FieldId        = f.FieldId,
                            SortOrder      = f.OrderNo,
                            ColumnCode     = f.ColumnCode,
                            FieldCode      = f.FieldCode,
                            EditorType     = f.EditorType,
                            IsVirtual      = f.IsVirtual,
                            LabelKey       = f.LabelKey,
                            IsCurrentField = f.FieldId == FieldId,
                            // Đã cấu hình = cờ Ui_Field.Is_Configured (bật khi user bấm Lưu Field).
                            Status         = f.IsConfigured
                                             ? FieldNavStatus.Configured
                                             : FieldNavStatus.Incomplete
                        });
                }
                if (group.Fields.Count > 0)
                {
                    FieldNavigatorGroups.Add(group);
                    sectionTitleKeys.Add((group, sec.TitleKey));
                }
            }

            // ── Cột chưa tạo field: có trong Sys_Column nhưng chưa có Ui_Field ──
            // Gộp vào 1 nhóm riêng ở cuối để user biết cột nào còn "chỉ mới tạo cột".
            await AppendUnconfiguredColumnsAsync(fields, tenantId, ct);

            // Pass 2: resolve tên section + field ra tiếng Việt (fallback mã khi chưa có bản dịch).
            await ResolveNavigatorNamesAsync(sectionTitleKeys, ct);

            _navigatorLoadedFormId = FormId;
            RecomputeCascadeWarnings();   // đã có field list → soát cascade (P2/P3)
        }
        catch (OperationCanceledException) { /* bỏ qua */ }
        catch (Exception ex) { _logger?.Capture(ex, "FieldConfig.LoadFieldNavigator"); }
    }

    /// <summary>
    /// Thêm nhóm "Cột chưa tạo field": các cột trong <c>Sys_Column</c> (bỏ cột khóa chính) chưa được
    /// map vào bất kỳ <c>Ui_Field</c> nào của form. Giúp user thấy cột nào "chỉ mới tạo cột" cần cấu hình.
    /// Sự kiện theo sau: click item → mở tạo field mới với cột đó đã chọn sẵn.
    /// </summary>
    private async Task AppendUnconfiguredColumnsAsync(
        IReadOnlyList<FieldDetailRecord> fields, int tenantId, CancellationToken ct)
    {
        if (_fieldService is null) return;

        var tableId = await _fieldService.GetTableIdByFormAsync(FormId, tenantId, ct);
        if (tableId <= 0) return;

        var columns = await _fieldService.GetColumnsByTableAsync(tableId, ct);
        if (columns.Count == 0) return;

        // Cột đã được map vào field (non-virtual) → loại khỏi danh sách "chưa tạo field".
        var mappedCodes = fields
            .Where(f => !f.IsVirtual && !string.IsNullOrWhiteSpace(f.ColumnCode))
            .Select(f => f.ColumnCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Cột hệ thống English (khóa chính + khối audit chuẩn) — không phải field nghiệp vụ → ẩn.
        var systemCodes = new HashSet<string>(
            Core.Services.AuditColumnTemplate.RequiredColumns, StringComparer.OrdinalIgnoreCase)
            { "Id" };

        var group = new FieldNavGroup { SectionId = 0, SectionCode = "CHƯA TẠO FIELD" };
        foreach (var c in columns)
        {
            // Bỏ cột khóa chính + cột hệ thống + cột đã map — chỉ hiện cột nghiệp vụ chưa tạo field.
            if (c.IsPk || systemCodes.Contains(c.ColumnCode) || mappedCodes.Contains(c.ColumnCode))
                continue;

            group.Fields.Add(new FieldNavItem
            {
                FieldId    = 0,
                ColumnCode = c.ColumnCode,
                EditorType = c.DataType,
                Status     = FieldNavStatus.ColumnOnly
            });
        }

        if (group.Fields.Count > 0)
            FieldNavigatorGroups.Add(group);
    }

    // Pass 2 của navigator: resolve tên section (TitleKey) + tên field (LabelKey) ra tiếng Việt.
    // Chạy tuần tự (giống FormEditor) sau khi list đã hiện mã → tên "điền dần" qua INotifyPropertyChanged.
    // Cột chưa tạo field (LabelKey rỗng) giữ nguyên mã.
    private async Task ResolveNavigatorNamesAsync(
        IReadOnlyList<(FieldNavGroup Group, string? TitleKey)> sectionTitleKeys, CancellationToken ct)
    {
        if (_i18nService is null || _appConfig is not { IsConfigured: true }) return;

        foreach (var (group, titleKey) in sectionTitleKeys)
            group.SectionName = await ResolveViAsync(titleKey, ct);

        foreach (var group in FieldNavigatorGroups)
            foreach (var item in group.Fields)
                if (!string.IsNullOrEmpty(item.LabelKey))
                    item.DisplayName = await ResolveViAsync(item.LabelKey, ct);
    }

    /// <summary>Resolve 1 resource key sang tiếng Việt; rỗng/không có bản dịch → chuỗi rỗng (để fallback mã).</summary>
    private async Task<string> ResolveViAsync(string? key, CancellationToken ct)
    {
        if (_i18nService is null || string.IsNullOrEmpty(key) || _appConfig is not { IsConfigured: true })
            return "";
        try { return await _i18nService.ResolveKeyAsync(key, "vi", ct) ?? ""; }
        catch (OperationCanceledException) { return ""; }
        catch (Exception ex) { _logger?.Capture(ex, $"FieldConfig.ResolveVi {key}"); return ""; }
    }

    // ── Field Navigator command ───────────────────────────────

    private void ExecuteNavigateToField(FieldNavItem? item)
    {
        if (item is null) return;

        // Cột chưa tạo field → mở chế độ "new" với cột đã chọn sẵn, nối cuối danh sách.
        if (item.Status == FieldNavStatus.ColumnOnly)
        {
            var firstSectionId = FieldNavigatorGroups.FirstOrDefault(g => g.SectionId > 0)?.SectionId ?? 0;
            var appendOrder = FieldNavigatorGroups
                .SelectMany(g => g.Fields).Where(f => f.FieldId > 0)
                .Select(f => f.SortOrder).DefaultIfEmpty(0).Max() + 1;

            var pNew = new NavigationParameters
            {
                { "fieldId",    0 },
                { "formId",     FormId },
                { "sectionId",  firstSectionId },
                { "orderNo",    appendOrder },
                { "columnCode", item.ColumnCode },
                { "tableCode",  TableCode },
                { "formCode",   FormCode },
                { "formName",   FormName },
                { "mode",       "new" }
            };
            _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FieldConfig, pNew);
            return;
        }

        if (item.FieldId == FieldId) return;

        // Tìm section chứa field này để truyền đúng sectionId → dropdown Section không bị mất khi navigate
        var sectionId = FieldNavigatorGroups
            .FirstOrDefault(g => g.Fields.Any(f => f.FieldId == item.FieldId))
            ?.SectionId ?? 0;

        var p = new NavigationParameters
        {
            { "fieldId",   item.FieldId },
            { "formId",    FormId },
            { "sectionId", sectionId },
            { "tableCode", TableCode },
            { "formCode",  FormCode },
            { "formName",  FormName },
            { "mode",      "edit" }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FieldConfig, p);
    }

    // ── Field Navigator selection sync ───────────────────────

    /// <summary>Cập nhật IsCurrentField cho tất cả item — không reload list.</summary>
    private void UpdateNavigatorSelection(int currentFieldId)
    {
        foreach (var group in FieldNavigatorGroups)
            foreach (var item in group.Fields)
                item.IsCurrentField = item.FieldId == currentFieldId;
    }

    // ── Field Navigator move up/down ─────────────────────────

    /// <summary>
    /// Di chuyển <paramref name="item"/> lên (<paramref name="direction"/>=-1)
    /// hoặc xuống (+1) trong group chứa nó, rồi persist Order_No (1, 3, 5...).
    /// </summary>
    private async Task ExecuteMoveFieldAsync(FieldNavItem? item, int direction)
    {
        if (item is null) return;

        var group = FieldNavigatorGroups.FirstOrDefault(g => g.Fields.Contains(item));
        if (group is null) return;

        var idx    = group.Fields.IndexOf(item);
        var newIdx = idx + direction;
        if (newIdx < 0 || newIdx >= group.Fields.Count) return;

        group.Fields.Move(idx, newIdx);

        // Gán lại Order_No: bắt đầu 1, bước +2 → 1, 3, 5, 7...
        var orderItems = new List<(int FieldId, int OrderNo)>();
        for (var i = 0; i < group.Fields.Count; i++)
        {
            group.Fields[i].SortOrder = 1 + i * 2;
            orderItems.Add((group.Fields[i].FieldId, group.Fields[i].SortOrder));
        }

        if (_fieldService is not null)
        {
            try { await _fieldService.UpdateFieldOrderAsync(orderItems, _cts.Token); }
            catch (Exception ex) { _logger?.Capture(ex, "FieldConfig.PersistFieldOrder"); }
        }
    }

    // ── Bulk multi-select + chuyển sang Section khác (context-menu) ──
    // Song song FormEditor: tick checkbox trên field → gom vào BulkSelectedFields; right-click
    // navigator → menu "Chuyển N field đã chọn sang…". Nhóm "CHƯA TẠO FIELD" (FieldId=0) không
    // được tick (không thể chuyển field chưa tồn tại).

    /// <summary>Toggle 1 field vào/khỏi bulk selection theo trạng thái IsMultiChecked của nó.</summary>
    private void ExecuteToggleBulkSelection(FieldNavItem? item)
    {
        if (item is null || item.IsColumnOnly || item.FieldId <= 0) return;

        if (item.IsMultiChecked)
        {
            if (!BulkSelectedFields.Contains(item))
                BulkSelectedFields.Add(item);
        }
        else
        {
            BulkSelectedFields.Remove(item);
        }
    }

    /// <summary>Bỏ tick toàn bộ + xóa khỏi BulkSelectedFields.</summary>
    private void ExecuteClearBulkSelection()
    {
        foreach (var f in BulkSelectedFields.ToList())
            f.IsMultiChecked = false;
        BulkSelectedFields.Clear();
    }

    /// <summary>Rebuild <see cref="MoveTargets"/> từ các section đang có (gọi khi mở context-menu).
    /// Header ưu tiên tên đã resolve từ navigator group; section rỗng chưa có group → fallback mã.</summary>
    public void RefreshMoveTargets()
    {
        MoveTargets.Clear();
        foreach (var s in AvailableSections.Where(s => s.Id > 0))
        {
            var resolved = FieldNavigatorGroups.FirstOrDefault(g => g.SectionId == s.Id)?.SectionName;
            var header   = !string.IsNullOrWhiteSpace(resolved) ? resolved : s.Code;
            MoveTargets.Add(new FieldMoveTargetItem(header, s.Id, s.Code, MoveBulkToSectionCommand));
        }
    }

    // Chuyển toàn bộ field đã tick sang section đích. Persist DB TRƯỚC (MoveFieldToSectionAsync),
    // chỉ khi thành công mới đổi vị trí trong navigator → tránh lệch state khi DB lỗi. Sau đó
    // reindex Order_No (1,3,5...) cho các group bị ảnh hưởng và persist qua UpdateFieldOrderAsync.
    /// <summary>Chuyển các field trong <see cref="BulkSelectedFields"/> sang section của <paramref name="target"/>.</summary>
    private async Task ExecuteMoveBulkToSectionAsync(FieldMoveTargetItem? target)
    {
        if (target is null || target.SectionId <= 0) return;
        if (BulkSelectedFields.Count == 0 || _fieldService is null) return;

        var fields = BulkSelectedFields.ToList();

        // Group đích có thể chưa tồn tại (navigator chỉ hiện group có field) → khởi tạo, chèn
        // trước nhóm "CHƯA TẠO FIELD" (SectionId=0) nếu có.
        var targetGroup = FieldNavigatorGroups.FirstOrDefault(g => g.SectionId == target.SectionId);
        if (targetGroup is null)
        {
            targetGroup = new FieldNavGroup
            {
                SectionId   = target.SectionId,
                SectionCode = target.SectionCode,
                SectionName = target.Header   // Header = tên đã resolve (hoặc mã fallback)
            };
            var colOnlyIdx = -1;
            for (var i = 0; i < FieldNavigatorGroups.Count; i++)
                if (FieldNavigatorGroups[i].SectionId == 0) { colOnlyIdx = i; break; }
            if (colOnlyIdx >= 0) FieldNavigatorGroups.Insert(colOnlyIdx, targetGroup);
            else                 FieldNavigatorGroups.Add(targetGroup);
        }

        var affectedGroups = new HashSet<FieldNavGroup>();
        foreach (var field in fields)
        {
            if (field.FieldId <= 0) continue; // cột chưa tạo field → bỏ qua

            var src = FieldNavigatorGroups.FirstOrDefault(g => g.Fields.Contains(field));
            if (src is null || ReferenceEquals(src, targetGroup)) continue; // đã ở section đích

            try
            {
                await _fieldService.MoveFieldToSectionAsync(field.FieldId, target.SectionId, _cts.Token);
            }
            catch (Exception ex)
            {
                _logger?.Capture(ex, $"FieldConfig.BulkMove field #{field.FieldId} → section #{target.SectionId}");
                continue; // DB lỗi → giữ nguyên field ở section nguồn
            }

            src.Fields.Remove(field);
            targetGroup.Fields.Add(field);
            affectedGroups.Add(src);
        }

        if (affectedGroups.Count == 0) { ExecuteClearBulkSelection(); return; }
        affectedGroups.Add(targetGroup);

        // Reindex Order_No (1,3,5...) cho mọi group bị ảnh hưởng + persist 1 lần.
        var orderItems = new List<(int FieldId, int OrderNo)>();
        foreach (var g in affectedGroups)
            for (var i = 0; i < g.Fields.Count; i++)
            {
                if (g.Fields[i].FieldId <= 0) continue;
                g.Fields[i].SortOrder = 1 + i * 2;
                orderItems.Add((g.Fields[i].FieldId, g.Fields[i].SortOrder));
            }

        if (orderItems.Count > 0)
        {
            try { await _fieldService.UpdateFieldOrderAsync(orderItems, _cts.Token); }
            catch (Exception ex) { _logger?.Capture(ex, "FieldConfig.BulkMovePersistOrder"); }
        }

        // Xóa group nguồn nếu rỗng (navigator chỉ hiện group có field).
        foreach (var g in affectedGroups.Where(g => g.SectionId != target.SectionId && g.Fields.Count == 0).ToList())
            FieldNavigatorGroups.Remove(g);

        ExecuteClearBulkSelection();
    }

    // ── Control prop schema loader ───────────────────────────

    /// <summary>
    /// Load schema control props dựa trên <see cref="SelectedEditorType"/>.
    /// Giữ lại giá trị cũ nếu PropName trùng, reset về Default nếu mới.
    /// </summary>
    private void LoadControlPropSchema()
    {
        // NOTE: Lưu giá trị cũ để khôi phục nếu PropName trùng.
        // Nếu ControlProps rỗng (lần đầu load từ DB) → parse từ _controlPropsJson để restore giá trị đã lưu.
        var oldValues = ControlProps.Count > 0
            ? ControlProps.ToDictionary(p => p.Definition.PropName, p => p.Value)
            : ParseControlPropsJson(_controlPropsJson);

        ControlProps.Clear();

        var definitions = GetPropDefinitions(SelectedEditorType);

        foreach (var def in definitions)
        {
            object? resolvedValue = def.DefaultValue;
            if (oldValues.TryGetValue(def.PropName, out var saved))
                resolvedValue = ConvertJsonPropValue(saved, def.PropType) ?? def.DefaultValue;

            var propValue = new ControlPropValue
            {
                Definition = def,
                Value      = resolvedValue
            };
            // NOTE: Dùng flag _isRebuildingProps để tránh gọi RebuildControlPropsJson đệ quy
            // khi PropertyChanged fire trong lúc đang build collection
            propValue.PropertyChanged += (_, _) =>
            {
                if (!_isRebuildingProps)
                    RebuildControlPropsJson();
            };
            ControlProps.Add(propValue);
        }

        RebuildControlPropsJson();
    }

    /// <summary>
    /// Rebuild JSON từ danh sách <see cref="ControlProps"/> hiện tại.
    /// </summary>
    /// <summary>
    /// Ép giá trị prop về đúng kiểu trước khi serialize JSON.
    /// DevExpress TextEdit trả EditValue dạng string ("2") nên prop kiểu Number
    /// phải parse về số — nếu không Blazor renderer (int/double) sẽ deserialize lỗi.
    /// </summary>
    private static object? CoercePropValue(ControlPropValue p)
    {
        if (p.Definition.PropType != "Number") return p.Value;

        return p.Value switch
        {
            null                                                    => null,
            string s when string.IsNullOrWhiteSpace(s)              => null,
            // Số nguyên thì giữ long, có phần thập phân thì giữ double
            string s when long.TryParse(s, out var l)               => l,
            string s when double.TryParse(s,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var d)                                          => d,
            string s when double.TryParse(s, out var d)             => d,
            _                                                       => p.Value
        };
    }

    private void RebuildControlPropsJson()
    {
        _isRebuildingProps = true;
        try
        {
            var dict = ControlProps.ToDictionary(
                p => p.Definition.PropName,
                p => CoercePropValue(p));

            // Sys_Lookup: đưa lookupCode vào JSON
            if (IsLookupEditor && !string.IsNullOrWhiteSpace(_lookupCode))
                dict["lookupCode"] = _lookupCode;

            // ComboBox / LookupComboBox: merge search + display props vào JSON
            if (IsComboBoxEditor || SelectedEditorType == "LookupComboBox")
            {
                dict["searchMode"]           = _cbSearchMode;
                dict["searchFilterCondition"]= _cbSearchFilterCondition;
                dict["allowUserInput"]       = (object)_cbAllowUserInput;
                dict["dropDownWidthMode"]    = _cbDropDownWidthMode;
                dict["clearButton"]          = _cbClearButton;
                if (!string.IsNullOrWhiteSpace(_cbNullTextKey))
                    dict["nullTextKey"] = _cbNullTextKey;
                if (!string.IsNullOrWhiteSpace(_cbGroupFieldName))
                    dict["groupFieldName"] = _cbGroupFieldName;
                if (!string.IsNullOrWhiteSpace(_cbDisabledFieldName))
                    dict["disabledFieldName"] = _cbDisabledFieldName;
            }

            // FK Lookup: serialize toàn bộ config LookupBox theo queryMode
            if (IsFkLookupEditor)
            {
                dict["queryMode"]    = _queryMode;
                dict["valueField"]   = _fkValueField;
                dict["displayField"] = _fkDisplayField;
                dict["searchEnabled"]= _fkSearchEnabled;
                dict["orderBy"]      = _fkOrderBy;

                switch (_queryMode)
                {
                    case "table":
                        dict["tableName"] = (object?)_fkTableName;
                        dict["filterSql"] = _fkFilterSql;
                        if (FkFilterParams.Count > 0)
                            dict["filterParams"] = FkFilterParams.Select(p => new
                                { param = p.Param, fieldRef = p.FieldRef, type = p.Type }).ToList();
                        break;

                    case "function":
                        dict["functionName"] = (object?)_fkFunctionName;
                        if (FkFunctionParams.Count > 0)
                            dict["functionParams"] = FkFunctionParams.Select(p => new
                            {
                                name       = p.Name,
                                sourceType = p.SourceType,
                                fieldRef   = p.SourceType == "field"  ? p.FieldRef  : (string?)null,
                                systemKey  = p.SourceType == "system" ? p.SystemKey : (string?)null,
                                type       = p.Type
                            }).ToList();
                        break;

                    case "sql":
                        dict["selectSql"] = (object?)_fkSelectSql;
                        if (FkFilterParams.Count > 0)
                            dict["filterParams"] = FkFilterParams.Select(p => new
                                { param = p.Param, fieldRef = p.FieldRef, type = p.Type }).ToList();
                        break;
                }

                // Cột popup, reloadOnChange, dataSourceConditions — dùng cho cả 3 mode
                // Lưu captionKey (i18n key) — backend resolve → text theo langCode khi trả Blazor
                dict["columns"] = FkPopupColumns.Select(c => new
                    { fieldName = c.FieldName, captionKey = c.CaptionKey, width = c.Width }).ToList();

                // Multi-Trigger giờ lưu ở cột Ui_Field_Lookup.Reload_Trigger_Fields (Migration 068),
                // KHÔNG serialize vào Control_Props_Json nữa (runtime đọc từ cột).

                if (DataSourceConditions.Count > 0)
                    dict["dataSourceConditions"] = DataSourceConditions.Select(c => new
                    {
                        when = new { field = c.WhenField, op = c.WhenOp, value = c.WhenValue },
                        tableName    = c.TableName,
                        displayField = c.DisplayField,
                        filterSql    = c.FilterSql
                    }).ToList();
            }

            ControlPropsJson = JsonSerializer.Serialize(dict, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            IsDirty = true;
        }
        finally
        {
            _isRebuildingProps = false;
        }
    }

    /// <summary>
    /// Parse JSON string thành dictionary prop values (dùng khi restore từ DB).
    /// </summary>
    private static Dictionary<string, object?> ParseControlPropsJson(string json)
    {
        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (raw is null) return [];
            return raw.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
        }
        catch { return []; }
    }

    /// <summary>Đọc string từ prop dict, fallback về <paramref name="def"/> nếu không tìm thấy.</summary>
    private static string GetStr(Dictionary<string, object?> d, string key, string def)
    {
        if (!d.TryGetValue(key, out var v) || v is not JsonElement je) return def;
        return je.ValueKind == JsonValueKind.String ? (je.GetString() ?? def) : def;
    }

    /// <summary>Đọc bool từ prop dict, fallback về <paramref name="def"/> nếu không tìm thấy.</summary>
    private static bool GetBool(Dictionary<string, object?> d, string key, bool def)
    {
        if (!d.TryGetValue(key, out var v) || v is not JsonElement je) return def;
        return je.ValueKind is JsonValueKind.True  ? true
             : je.ValueKind is JsonValueKind.False ? false
             : def;
    }

    /// <summary>
    /// Chuyển JsonElement thành đúng kiểu dựa trên PropType của definition.
    /// </summary>
    private static object? ConvertJsonPropValue(object? raw, string propType)
    {
        if (raw is not JsonElement je) return raw;
        return propType switch
        {
            "Number"  => je.ValueKind == JsonValueKind.Number  ? je.GetDouble()  : (object?)null,
            "Boolean" => je.ValueKind is JsonValueKind.True
                                      or JsonValueKind.False   ? je.GetBoolean() : (object?)null,
            _         => je.ValueKind == JsonValueKind.String  ? je.GetString()  : je.ToString()
        };
    }

    /// <summary>
    /// Trả về danh sách <see cref="ControlPropDefinition"/> mock theo editor type.
    /// Sau này sẽ load từ <c>Ui_Control_Map.Default_Props_Json</c>.
    /// </summary>
    private static List<ControlPropDefinition> GetPropDefinitions(string editorType) => editorType switch
    {
        "NumericBox" =>
        [
            new() { PropName = "minValue",  PropType = "Number",  DefaultValue = 0,      Label = "Giá trị tối thiểu" },
            new() { PropName = "maxValue",  PropType = "Number",  DefaultValue = 999999, Label = "Giá trị tối đa" },
            new() { PropName = "decimals",  PropType = "Number",  DefaultValue = 0,      Label = "Số chữ số thập phân" },
            new() { PropName = "spinStep",  PropType = "Number",  DefaultValue = 1,      Label = "Bước nhảy" },
            new() { PropName = "allowNull", PropType = "Boolean", DefaultValue = false,   Label = "Cho phép rỗng" },
        ],
        "TextBox" =>
        [
            new() { PropName = "maxLength",       PropType = "Number",  DefaultValue = 255,          Label = "Độ dài tối đa" },
            new() { PropName = "isPassword",      PropType = "Boolean", DefaultValue = false,         Label = "Ẩn ký tự (password)" },
            new() { PropName = "autoComplete",    PropType = "Enum",    DefaultValue = "off",         Label = "AutoComplete",
                    AllowedValues = ["off", "on", "new-password"] },
            new() { PropName = "bindValueMode",   PropType = "Enum",    DefaultValue = "OnLostFocus", Label = "Khi nào cập nhật giá trị",
                    AllowedValues = ["OnLostFocus", "OnInput"] },
            new() { PropName = "inputDelay",      PropType = "Number",  DefaultValue = 300,           Label = "Delay (ms) khi OnInput" },
            new() { PropName = "clearButtonMode", PropType = "Enum",    DefaultValue = "Auto",        Label = "Nút xóa",
                    AllowedValues = ["Auto", "Never"] },
        ],
        // TextArea = DxMemo — control riêng, user tự chọn
        "TextArea" =>
        [
            new() { PropName = "maxLength",     PropType = "Number",  DefaultValue = 4000,          Label = "Độ dài tối đa" },
            new() { PropName = "rows",          PropType = "Number",  DefaultValue = 4,             Label = "Số dòng hiển thị" },
            new() { PropName = "bindValueMode", PropType = "Enum",    DefaultValue = "OnLostFocus", Label = "Khi nào cập nhật giá trị",
                    AllowedValues = ["OnLostFocus", "OnInput"] },
            new() { PropName = "inputDelay",    PropType = "Number",  DefaultValue = 300,           Label = "Delay (ms) khi OnInput" },
        ],
        // ComboBox dùng dedicated ComboBoxPropsPanel — không qua generic ControlProps
        "ComboBox" => [],
        "DatePicker" =>
        [
            new() { PropName = "format",  PropType = "Enum",   DefaultValue = "dd/MM/yyyy", Label = "Định dạng ngày", AllowedValues = ["dd/MM/yyyy", "dd/MM/yyyy HH:mm", "MM/yyyy", "yyyy"] },
            new() { PropName = "minDate", PropType = "String", DefaultValue = "",            Label = "Ngày tối thiểu" },
            new() { PropName = "maxDate", PropType = "String", DefaultValue = "",            Label = "Ngày tối đa" },
        ],
        // CheckBox = DxCheckBox với CheckType.CheckBox
        "CheckBox" =>
        [
            new() { PropName = "allowIndeterminate", PropType = "Boolean", DefaultValue = false,   Label = "3 trạng thái (bool?)" },
            new() { PropName = "labelPosition",      PropType = "Enum",    DefaultValue = "Right",  Label = "Vị trí label",
                    AllowedValues = ["Right", "Left"] },
            new() { PropName = "labelWrapMode",      PropType = "Enum",    DefaultValue = "WordWrap", Label = "Xuống dòng label",
                    AllowedValues = ["WordWrap", "Ellipsis", "NoWrap"] },
        ],
        // ToggleSwitch = DxCheckBox với CheckType.Switch (không hỗ trợ indeterminate)
        "ToggleSwitch" =>
        [
            new() { PropName = "labelPosition", PropType = "Enum", DefaultValue = "Right", Label = "Vị trí label",
                    AllowedValues = ["Right", "Left"] },
        ],
        // LookupBox dùng panel riêng (FkTableName, FkValueField...) — không qua generic ControlProps
        "LookupBox" => [],
        _ => []
    };

    // ── FK Lookup command handlers ───────────────────────────

    /// <summary>Thêm 1 cột mới vào danh sách popup columns của LookupBox.</summary>
    private void ExecuteAddFkColumn()
    {
        var col = new FkColumnConfig { FieldName = "", CaptionKey = "", Width = 150 };
        WireFkColumnHandlers(col);
        FkPopupColumns.Add(col);
        RebuildControlPropsJson();
    }

    /// <summary>
    /// Đăng ký PropertyChanged handlers cho 1 FkColumnConfig:
    /// - Khi FieldName thay đổi → tự sinh CaptionKey nếu key đang rỗng hoặc là auto-gen cũ.
    /// - Mọi thay đổi → rebuild ControlPropsJson + IsDirty.
    /// </summary>
    private void WireFkColumnHandlers(FkColumnConfig col)
    {
        col.PropertyChanged += (sender, e) =>
        {
            if (sender is not FkColumnConfig c) return;

            // Auto-gen captionKey khi FieldName thay đổi
            if (e.PropertyName == nameof(FkColumnConfig.FieldName))
            {
                var generated = GenerateCaptionKey(FkTableName, c.FieldName);
                // Chỉ ghi đè nếu key đang rỗng hoặc user chưa nhập tay
                // (kiểm tra theo pattern: key cũ = auto-gen của fieldName cũ → cho phép overwrite)
                if (string.IsNullOrWhiteSpace(c.CaptionKey)
                    || c.CaptionKey.StartsWith(GetTablePrefix() + ".col.", StringComparison.OrdinalIgnoreCase))
                {
                    // Gán trực tiếp không qua setter để tránh vòng lặp (setter gọi PropertyChanged lại)
                    c.CaptionKey = generated;
                    return; // CaptionKey.set sẽ kích hoạt PropertyChanged → RebuildControlPropsJson được gọi
                }
            }

            RebuildControlPropsJson();
        };
    }

    /// <summary>Sinh i18n key theo pattern: {table_lower}.col.{column_snake_case}.</summary>
    private string GenerateCaptionKey(string? tableName, string? fieldName)
    {
        var table = string.IsNullOrWhiteSpace(tableName) ? "lookup" : tableName.ToLowerInvariant();
        var col   = ToSnakeCase(fieldName ?? "");
        return $"{table}.col.{col}";
    }

    /// <summary>Prefix table hiện tại (dùng để nhận biết auto-gen key).</summary>
    private string GetTablePrefix()
        => string.IsNullOrWhiteSpace(FkTableName) ? "lookup" : FkTableName.ToLowerInvariant();

    /// <summary>Chuyển PascalCase / camelCase sang snake_case. VD: MaPhongBan → ma_phong_ban.</summary>
    private static string ToSnakeCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        // Chèn dấu _ trước chữ hoa đứng sau chữ thường/số: MaPhongBan → Ma_Phong_Ban
        var snake = System.Text.RegularExpressions.Regex.Replace(s, @"(?<=[a-z0-9])([A-Z])", "_$1");
        return snake.ToLowerInvariant();
    }

    /// <summary>Xóa 1 cột khỏi danh sách popup columns của LookupBox.</summary>
    private void ExecuteRemoveFkColumn(FkColumnConfig col)
    {
        FkPopupColumns.Remove(col);
        RebuildControlPropsJson();
    }

    /// <summary>Di chuyển cột popup lên 1 vị trí (giảm index).</summary>
    private void ExecuteMoveFkColumnUp(FkColumnConfig col)
    {
        var idx = FkPopupColumns.IndexOf(col);
        if (idx <= 0) return;
        FkPopupColumns.Move(idx, idx - 1);
        RebuildControlPropsJson();
    }

    /// <summary>Di chuyển cột popup xuống 1 vị trí (tăng index).</summary>
    private void ExecuteMoveFkColumnDown(FkColumnConfig col)
    {
        var idx = FkPopupColumns.IndexOf(col);
        if (idx < 0 || idx >= FkPopupColumns.Count - 1) return;
        FkPopupColumns.Move(idx, idx + 1);
        RebuildControlPropsJson();
    }

    /// <summary>Thêm 1 tham số động mới vào filterParams của LookupBox.</summary>
    private void ExecuteAddFkFilterParam()
    {
        var param = new FkFilterParam { Param = "", FieldRef = "", Type = "String" };
        param.PropertyChanged += (_, _) => RebuildControlPropsJson();
        FkFilterParams.Add(param);
        RebuildControlPropsJson();
    }

    /// <summary>Xóa 1 tham số khỏi filterParams của LookupBox.</summary>
    private void ExecuteRemoveFkFilterParam(FkFilterParam param)
    {
        FkFilterParams.Remove(param);
        RebuildControlPropsJson();
    }

    /// <summary>Thêm 1 tham số mới vào danh sách FunctionParams của TVF.</summary>
    private void ExecuteAddFunctionParam()
    {
        var p = new FunctionParam { Name = "", SourceType = "field", Type = "String" };
        p.PropertyChanged += (_, _) => RebuildControlPropsJson();
        FkFunctionParams.Add(p);
        RebuildControlPropsJson();
    }

    /// <summary>Xóa 1 tham số khỏi FunctionParams.</summary>
    private void ExecuteRemoveFunctionParam(FunctionParam p)
    {
        FkFunctionParams.Remove(p);
        RebuildControlPropsJson();
    }

    /// <summary>Thêm FieldCode vào danh sách reloadOnChange.</summary>
    private void ExecuteAddReloadField()
    {
        var code = ReloadOnChangeInput.Trim();
        if (string.IsNullOrEmpty(code) || ReloadOnChangeFields.Contains(code)) return;
        ReloadOnChangeFields.Add(code);
        ReloadOnChangeInput = "";
        RebuildControlPropsJson();
        RecomputeCascadeWarnings();
    }

    /// <summary>Xóa 1 FieldCode khỏi danh sách reloadOnChange.</summary>
    private void ExecuteRemoveReloadField(string fieldCode)
    {
        ReloadOnChangeFields.Remove(fieldCode);
        RebuildControlPropsJson();
        RecomputeCascadeWarnings();
    }

    /// <summary>Thêm 1 điều kiện đổi bảng nguồn mới (rỗng) vào DataSourceConditions.</summary>
    private void ExecuteAddDataSourceCondition()
    {
        var cond = new DataSourceCondition { WhenOp = "eq" };
        cond.PropertyChanged += (_, _) => RebuildControlPropsJson();
        DataSourceConditions.Add(cond);
        RebuildControlPropsJson();
    }

    /// <summary>Xóa 1 điều kiện khỏi DataSourceConditions.</summary>
    private void ExecuteRemoveDataSourceCondition(DataSourceCondition cond)
    {
        DataSourceConditions.Remove(cond);
        RebuildControlPropsJson();
    }

    /// <summary>
    /// Sinh diễn giải tiếng Việt từ cấu hình LookupBox hiện tại.
    /// Giúp người dùng kiểm tra xem cấu hình có đúng ý định không.
    /// </summary>
    private void ExecuteExplainConfig()
    {
        if (!IsFkLookupEditor) { ConfigExplanation = ""; return; }

        RecomputeCascadeWarnings();   // đảm bảo cảnh báo cascade khớp cấu hình mới nhất

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("📋 DIỄN GIẢI CẤU HÌNH LOOKUP");
        sb.AppendLine(new string('─', 50));
        sb.AppendLine();

        // ── Thông tin chung ──
        if (IsVirtual)
            sb.AppendLine("🔮  Field ảo: KHÔNG lưu DB (chỉ để lọc/tham chiếu cascade).");
        sb.AppendLine($"⚙  Chế độ truy vấn: {_queryMode switch { "table" => "Bảng / View", "function" => "Table-Valued Function (TVF)", "sql" => "SQL tùy chỉnh", _ => _queryMode }}");
        if (!string.IsNullOrWhiteSpace(FkValueField))
            sb.AppendLine($"    Lưu vào DB: cột \"{FkValueField}\" (FK int)");
        if (!string.IsNullOrWhiteSpace(FkDisplayField))
            sb.AppendLine($"    Hiển thị trong ô: cột \"{FkDisplayField}\"");
        sb.AppendLine();

        switch (_queryMode)
        {
            case "table":
                sb.AppendLine($"🗄  Bảng / View nguồn: \"{FkTableName}\"");
                if (!string.IsNullOrWhiteSpace(FkFilterSql))
                {
                    sb.AppendLine("🔍  Điều kiện lọc (WHERE):");
                    sb.AppendLine($"    {FkFilterSql}");
                }
                if (FkFilterParams.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("⚡  Tham số từ field trong form:");
                    foreach (var p in FkFilterParams)
                        sb.AppendLine($"    @{p.Param} ← field \"{p.FieldRef}\" (kiểu {p.Type})");
                }
                sb.AppendLine();
                sb.AppendLine("    → SQL runtime:");
                sb.AppendLine($"    SELECT {FkValueField}, {FkDisplayField}");
                sb.AppendLine($"    FROM   {FkTableName}");
                sb.AppendLine($"    WHERE  {(string.IsNullOrWhiteSpace(FkFilterSql) ? "(không có filter)" : FkFilterSql)}");
                break;

            case "function":
                sb.AppendLine($"⚡  TVF: \"{FkFunctionName}\"");
                if (FkFunctionParams.Count > 0)
                {
                    sb.AppendLine("    Tham số (theo thứ tự):");
                    for (int i = 0; i < FkFunctionParams.Count; i++)
                    {
                        var p = FkFunctionParams[i];
                        var src = p.SourceType == "field"
                            ? $"field \"{p.FieldRef}\" trong form"
                            : $"hệ thống {p.SystemKey}";
                        sb.AppendLine($"    [{i + 1}] @{p.Name} ({p.Type}) ← {src}");
                    }
                    var paramList = string.Join(", ", FkFunctionParams.Select(p => $"@{p.Name}"));
                    sb.AppendLine();
                    sb.AppendLine("    → SQL runtime:");
                    sb.AppendLine($"    SELECT {FkValueField}, {FkDisplayField}");
                    sb.AppendLine($"    FROM   {FkFunctionName}({paramList})");
                }
                break;

            case "sql":
                sb.AppendLine("📝  Full SQL tùy chỉnh:");
                sb.AppendLine($"    {FkSelectSql}");
                if (FkFilterParams.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("⚡  Tham số từ field trong form:");
                    foreach (var p in FkFilterParams)
                        sb.AppendLine($"    @{p.Param} ← field \"{p.FieldRef}\" (kiểu {p.Type})");
                }
                break;
        }

        // ── Tham số hệ thống ──
        sb.AppendLine();
        sb.AppendLine("🔧  Tham số hệ thống tự inject:");
        sb.AppendLine("    @TenantId = Tenant hiện tại  |  @Today = Ngày hôm nay  |  @CurrentUser = User đăng nhập");

        // ── Reload on change ──
        if (ReloadOnChangeFields.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("🔄  Tự động reload khi field thay đổi:");
            foreach (var f in ReloadOnChangeFields)
                sb.AppendLine($"    • Field \"{f}\" thay đổi giá trị → reload lại danh sách");
            sb.AppendLine("    ⚠  Giá trị đang chọn sẽ bị xoá nếu không còn trong danh sách mới.");
        }

        // ── DataSource conditions ──
        if (DataSourceConditions.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("🔀  Đổi bảng nguồn theo điều kiện:");
            foreach (var c in DataSourceConditions)
            {
                sb.AppendLine($"    • Nếu field \"{c.WhenField}\" {c.WhenOpLabel} \"{c.WhenValue}\":");
                sb.AppendLine($"      → Lấy từ bảng \"{c.TableName}\", hiển thị cột \"{c.DisplayField}\"");
                if (!string.IsNullOrWhiteSpace(c.FilterSql))
                    sb.AppendLine($"      → Filter: {c.FilterSql}");
            }
            sb.AppendLine($"    • Các trường hợp còn lại → dùng bảng mặc định \"{FkTableName}\"");
        }

        // ── Cột popup ──
        if (FkPopupColumns.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("📊  Cột hiển thị trong popup chọn:");
            foreach (var col in FkPopupColumns)
                sb.AppendLine($"    • [{col.CaptionKey}] (cột DB: {col.FieldName}, rộng: {col.Width}px)");
        }

        // ── Search ──
        sb.AppendLine();
        sb.AppendLine(FkSearchEnabled
            ? "🔎  Cho phép tìm kiếm trong danh sách."
            : "⛔  Không cho phép tìm kiếm.");

        // ── Cảnh báo cascade (P2/P3) — @param sai hoặc thiếu reload ──
        if (HasCascadeWarnings)
        {
            sb.AppendLine();
            sb.AppendLine("🛑  CẢNH BÁO CASCADE — sửa trước khi lưu:");
            foreach (var w in CascadeWarnings)
                sb.AppendLine($"    {w}");
        }

        ConfigExplanation = sb.ToString();
    }

    /// <summary>Ánh xạ EditorType → SQL DataType mặc định cho EnsureColumnExistsAsync.</summary>
    private static string MapEditorTypeToSqlType(string editorType) => editorType switch
    {
        "NumericBox" or "SpinEdit"              => "int",
        "DecimalBox"                            => "decimal",
        "DatePicker" or "DateEdit"              => "date",
        "DateTimePicker" or "DateTimeEdit"      => "datetime2",
        "CheckBox" or "ToggleSwitch"            => "bit",
        "TextArea" or "Memo"                    => "nvarchar",
        _                                       => "nvarchar",   // TextBox, ComboBox, LookupBox,...
    };

    /// <summary>Ánh xạ EditorType → .NET type tương ứng.</summary>
    private static string MapEditorTypeToNetType(string editorType) => editorType switch
    {
        "NumericBox" or "SpinEdit"              => "int",
        "DecimalBox"                            => "decimal",
        "DatePicker" or "DateEdit"              => "DateTime",
        "DateTimePicker" or "DateTimeEdit"      => "DateTime",
        "CheckBox" or "ToggleSwitch"            => "bool",
        _                                       => "string",
    };

    private void ReindexRuleOrders()
    {
        for (int i = 0; i < LinkedRules.Count; i++)
            LinkedRules[i].OrderNo = i + 1;
    }

    private void ReindexEventOrders()
    {
        for (int i = 0; i < LinkedEvents.Count; i++)
            LinkedEvents[i].OrderNo = i + 1;
    }

    // ── Command handlers ─────────────────────────────────────

    private async Task ExecuteSaveAsync()
    {
        if (_fieldService is not null && _appConfig is { IsConfigured: true })
        {
            // GUARD: nạp cấu hình FK/ComboBox thất bại → các Fk* prop đang RỖNG (mặc định),
            // KHÔNG phải data thật. Nếu để Lưu sẽ ghi đè Ui_Field_Lookup bằng rỗng → mất cấu hình.
            // Chặn Lưu, yêu cầu khắc phục (chạy migration DB) rồi mở lại field.
            if (_fkConfigLoadFailed && (IsFkLookupEditor || IsComboBoxEditor))
            {
                SaveError = "Chưa Lưu được: cấu hình nguồn dữ liệu (FK/ComboBox) nạp lỗi nên panel đang trống. " +
                            "Lưu lúc này sẽ XÓA cấu hình cũ. Hãy chạy migration DB còn thiếu (db/068, db/069), " +
                            "khởi động lại ConfigStudio rồi mở lại field.";
                _notifier?.Notify(
                    "Đã chặn Lưu để bảo vệ cấu hình FK Lookup (panel nạp lỗi, đang trống).",
                    NotificationSeverity.Warning);
                return;
            }

            // Virtual field: bắt buộc phải có FieldCode để tham chiếu trong rules/events
            if (IsVirtual && string.IsNullOrWhiteSpace(FieldCode))
            {
                SaveError = "Field ảo bắt buộc phải có Field Code (dùng để tham chiếu trong rules/events).";
                return;
            }

            // Non-virtual field: cần Column_Id hợp lệ
            if (!IsVirtual && (SelectedColumn is null || SelectedColumn.ColumnId <= 0))
            {
                if (string.IsNullOrWhiteSpace(ColumnCode))
                {
                    SaveError = "Chưa chọn cột DB. Nhập tên cột hoặc bật 'Field ảo' nếu không cần lưu DB.";
                    return;
                }

                // User nhập tên cột thủ công → auto-create vào Sys_Column
                var tableId = await _fieldService!.GetTableIdByFormAsync(FormId, _appConfig.TenantId, _cts.Token);
                if (tableId <= 0)
                {
                    SaveError = "Form chưa liên kết Table. Vui lòng chọn Table trong Thông tin Form.";
                    return;
                }

                var colDto = new ColumnSchemaDto
                {
                    ColumnName = ColumnCode,
                    DataType   = MapEditorTypeToSqlType(SelectedEditorType),
                    NetType    = MapEditorTypeToNetType(SelectedEditorType),
                    IsNullable = true,
                };
                var createdId = await _fieldService.EnsureColumnExistsAsync(tableId, colDto, _cts.Token);
                if (createdId <= 0)
                {
                    SaveError = $"Không thể tạo cột '{ColumnCode}' trong Sys_Column.";
                    return;
                }

                // Tạo SelectedColumn in-memory để record lấy ColumnId đúng
                var newCol = new ColumnInfoDto
                {
                    ColumnId   = createdId,
                    ColumnCode = ColumnCode,
                    DataType   = colDto.DataType,
                    NetType    = colDto.NetType,
                    IsNullable = true
                };
                AvailableColumns.Add(newCol);
                _selectedColumn = newCol; // set backing field trực tiếp, tránh raise thêm event
                RaisePropertyChanged(nameof(SelectedColumn));
                RaisePropertyChanged(nameof(HasAvailableColumns));
                RaisePropertyChanged(nameof(IsColumnListEmpty));
                RaisePropertyChanged(nameof(CanTypeColumnCode));
            }

            // Xác định LookupSource theo EditorType
            var lookupSource = IsLookupEditor    ? "static"
                             : IsFkLookupEditor  ? "dynamic"
                             : IsComboBoxEditor  ? "dynamic"
                             : (string?)null;

            var field = new FieldConfigRecord
            {
                FieldId          = FieldId,
                FormId           = FormId,
                SectionId        = SectionId > 0 ? SectionId : null,
                // Field ảo → ép Column_Id NULL (0) dù trước đó có chọn cột, tránh ghi cột rác.
                ColumnId         = IsVirtual ? 0 : (SelectedColumn?.ColumnId ?? 0),
                ColumnCode       = ColumnCode,
                FieldCode        = IsVirtual && !string.IsNullOrWhiteSpace(FieldCode) ? FieldCode : null,
                SectionCode      = SectionName,
                EditorType       = SelectedEditorType,
                LabelKey         = LabelKey,
                PlaceholderKey   = PlaceholderKey,
                TooltipKey       = TooltipKey,
                IsVisible          = IsVisible,
                IsReadOnly         = IsReadOnly,
                IsRequired         = IsRequired,
                RequiredErrorKey   = IsRequired ? (string.IsNullOrWhiteSpace(RequiredErrorKey) ? null : RequiredErrorKey) : null,
                LockOnEdit         = LockOnEdit,
                ShowInList         = ShowInList,
                IsVirtual          = IsVirtual,
                IsUnique           = IsUnique,
                OrderNo          = OrderNo,
                ColSpan          = ColSpan,
                LookupSource     = lookupSource,
                LookupCode       = IsLookupEditor ? LookupCode : null,
                // LookupBox: ControlPropsJson = null (toàn bộ config lưu trong Ui_Field_Lookup)
                // ComboBox/LookupComboBox: ControlPropsJson lưu search+display props
                ControlPropsJson = IsFkLookupEditor ? null : ControlPropsJson
            };

            // Build lookup config cho dynamic field (LookupBox hoặc ComboBox)
            FieldLookupConfigRecord? lookupConfig = null;
            if (IsFkLookupEditor || IsComboBoxEditor)
            {
                // Xác định SourceName theo query mode
                var sourceName = _queryMode switch
                {
                    "tvf"        => FkFunctionName,
                    "custom_sql" => FkSelectSql,
                    _            => FkTableName          // "table" (default)
                };

                // Serialize popup columns
                // Lưu captionKey (i18n key) vào DB — backend resolve khi trả Blazor
                var popupColumnsJson = FkPopupColumns.Count > 0
                    ? JsonSerializer.Serialize(FkPopupColumns.Select(c => new
                        { fieldName = c.FieldName, captionKey = c.CaptionKey, width = c.Width }))
                    : null;

                lookupConfig = new FieldLookupConfigRecord
                {
                    FieldId          = FieldId,
                    QueryMode        = _queryMode,
                    SourceName       = sourceName,
                    ValueColumn      = FkValueField,
                    DisplayColumn    = FkDisplayField,
                    FilterSql        = string.IsNullOrWhiteSpace(FkFilterSql) ? null : FkFilterSql,
                    OrderBy          = string.IsNullOrWhiteSpace(FkOrderBy)   ? null : FkOrderBy,
                    SearchEnabled    = FkSearchEnabled,
                    PopupColumnsJson = popupColumnsJson,
                    // LookupBox-specific props — ComboBox giữ default
                    EditBoxMode         = IsFkLookupEditor ? _editBoxMode        : "TextOnly",
                    CodeField           = IsFkLookupEditor && !string.IsNullOrWhiteSpace(_codeField)
                                          ? _codeField : null,
                    DropDownWidth       = IsFkLookupEditor ? _dropDownWidth      : 600,
                    DropDownHeight      = IsFkLookupEditor ? _dropDownHeight     : 400,
                    ReloadTriggerField  = !string.IsNullOrWhiteSpace(_reloadTriggerField)
                                          ? _reloadTriggerField : null,
                    // Multi-Trigger (Migration 068): danh sách field cha → CSV. Runtime hợp với @param Filter SQL.
                    ReloadTriggerFields = ReloadOnChangeFields.Count > 0
                                          ? string.Join(",", ReloadOnChangeFields) : null,
                    // TreeLookupBox (Migration 021 + 069)
                    ParentColumn        = IsTreeLookupEditor && !string.IsNullOrWhiteSpace(_parentColumn)
                                          ? _parentColumn : null,
                    TreeSelectableLevel = IsTreeLookupEditor && _treeSelectableLevel != "all"
                                          ? _treeSelectableLevel : null,
                    // Thêm mới entity (Migration 022) — chỉ lưu khi bật + có form code
                    AllowAddNew         = IsFkLookupEditor && _allowAddNew
                                          && !string.IsNullOrWhiteSpace(_addFormCode),
                    AddFormCode         = IsFkLookupEditor && _allowAddNew
                                          && !string.IsNullOrWhiteSpace(_addFormCode)
                                          ? _addFormCode.Trim() : null,
                };
            }

            try
            {
                SaveError = null;
                // mode "new" → chèn sau field đang chọn: đẩy STT các field phía sau +1 khi lưu.
                var savedId = await _fieldService.SaveFieldAsync(
                    field, _appConfig.TenantId, lookupConfig,
                    shiftOnInsert: _mode == "new", ct: _cts.Token);

                // Làm mờ log = thuộc tính cấp cột (Sys_Column) → ghi riêng theo Column_Id (chỉ field map cột thật).
                if (CanConfigMasking)
                    await _fieldService.SaveColumnMaskingAsync(
                        _selectedColumn!.ColumnId, _isLogMasked, _logMaskMode, _cts.Token);

                // Import global-code (Ui_Field_Lookup) → ghi riêng theo Field_Id sau khi row lookup đã tồn tại.
                if (IsFkLookupEditor && savedId > 0)
                    await _fieldService.SaveFkImportGlobalAsync(savedId, _importGlobalCode, _cts.Token);

                // Đăng ký i18n keys vào Sys_Resource nếu chưa tồn tại
                await RegisterI18nKeysAsync(_cts.Token);

                // User bấm "Lưu Field" → đánh dấu ĐÃ cấu hình (badge navigator). Chỉ ở đây, không
                // gọi từ auto-generate/bulk nên field sinh tự động vẫn ở trạng thái "chưa cấu hình".
                if (savedId > 0)
                    await _fieldService.MarkFieldConfiguredAsync(savedId, _cts.Token);

                IsDirty = false;

                // INSERT mode "new": cập nhật FieldId rồi navigate thẳng về FormEditor
                // để user thấy field mới trong tree ngay (không cần bấm Hủy thủ công).
                if (_mode == "new" && savedId > 0)
                {
                    FieldId = savedId;
                    _mode   = "edit";
                    var backParams = new NavigationParameters
                    {
                        { "formId",          FormId  },
                        { "selectedFieldId", savedId },
                    };
                    _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormEditor, backParams);
                }
            }
            catch (Exception ex)
            {
                _logger?.Capture(ex, $"FieldConfig.Save field #{FieldId} ({FieldCode})");
                SaveError = $"Lưu thất bại: {ex.Message}";
            }
        }
    }

    private string? _saveError;
    /// <summary>Thông báo lỗi khi lưu thất bại — hiển thị banner đỏ trên UI.</summary>
    public string? SaveError
    {
        get => _saveError;
        set { if (SetProperty(ref _saveError, value)) RaisePropertyChanged(nameof(HasSaveError)); }
    }

    /// <summary>True khi có lỗi lưu → hiển thị banner lỗi.</summary>
    public bool HasSaveError => !string.IsNullOrEmpty(_saveError);

    /// <summary>
    /// Sau khi lưu field: ghi bản dịch (vi) cho các key hiển thị.
    /// Label/Placeholder/Tooltip/Required: user nhập thẳng → upsert (ghi đè); rỗng → init default nếu thiếu.
    /// captionKey popup + unique: chỉ init default khi chưa có (không ghi đè bản dịch đã sửa).
    /// </summary>
    private async Task RegisterI18nKeysAsync(CancellationToken ct)
    {
        if (_i18nService is null || _appConfig is not { IsConfigured: true }) return;

        // Label/Placeholder/Tooltip/Required: user nhập THẲNG giá trị vi ở ô tương ứng.
        // Có giá trị → upsert (ghi đè) bản dịch; rỗng → chỉ init default nếu chưa có.
        if (!string.IsNullOrWhiteSpace(LabelKey))
            await UpsertOrInitViAsync(LabelKey, LabelPreview, ColumnCode, ct);

        // Placeholder/Tooltip thường CÙNG text với label: user bỏ trống → mặc định lấy text của label
        // (tạo luôn bản dịch); user nhập KHÁC → tôn trọng giá trị user. Sau khi ghi, phản ánh lại ô nhập.
        var labelText = (LabelPreview ?? "").Trim();

        if (!string.IsNullOrWhiteSpace(PlaceholderKey))
        {
            var text = string.IsNullOrWhiteSpace(PlaceholderPreview) ? labelText : PlaceholderPreview.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                await _i18nService.SaveResourceAsync(PlaceholderKey, "vi", text, ct);
                if (string.IsNullOrWhiteSpace(PlaceholderPreview))
                    SetResolvedValue(v => PlaceholderPreview = v, text);
            }
        }

        if (!string.IsNullOrWhiteSpace(TooltipKey))
        {
            var text = string.IsNullOrWhiteSpace(TooltipPreview) ? labelText : TooltipPreview.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                await _i18nService.SaveResourceAsync(TooltipKey, "vi", text, ct);
                if (string.IsNullOrWhiteSpace(TooltipPreview))
                    SetResolvedValue(v => TooltipPreview = v, text);
            }
        }

        if (IsRequired && !string.IsNullOrWhiteSpace(RequiredErrorKey))
            await UpsertOrInitViAsync(RequiredErrorKey, RequiredErrorKeyPreview,
                                      $"Trường {ColumnCode} là bắt buộc", ct);

        // Đăng ký captionKey của từng cột popup LookupBox (chỉ init default, không có ô nhập riêng).
        foreach (var col in FkPopupColumns)
            if (!string.IsNullOrWhiteSpace(col.CaptionKey))
                await _i18nService.InitResourceIfMissingAsync(col.CaptionKey, "vi", col.FieldName, ct);

        // ── Unique: auto-tạo key chống trùng (vi + en) khi bật cờ Duy nhất ──
        // Key khớp backend emit: {tableCode}.val.{columnCode}.unique (xem SaveMasterDataCommandHandler).
        if (IsUnique)
        {
            var tableCode  = TableCode.ToLowerInvariant();
            var columnCode = ColumnCode.ToLowerInvariant();
            if (!string.IsNullOrEmpty(tableCode) && !string.IsNullOrEmpty(columnCode))
            {
                var uniqueKey = $"{tableCode}.val.{columnCode}.unique";
                var label     = string.IsNullOrWhiteSpace(LabelPreview) ? ColumnCode : LabelPreview;
                // vi: user gõ thẳng → upsert (ghi đè); bỏ trống → init mặc định "{label} đã tồn tại".
                await UpsertOrInitViAsync(uniqueKey, UniqueErrorKeyPreview, $"{label} đã tồn tại", ct);
                // en: chỉ init mặc định nếu chưa có (nhập bản dịch khác qua nút Dịch).
                await _i18nService.InitResourceIfMissingAsync(uniqueKey, "en", $"{label} already exists", ct);
            }
        }
    }

    /// <summary>
    /// Ghi bản dịch (vi) cho 1 key: có giá trị user nhập → upsert (ghi đè); rỗng → init default nếu chưa có.
    /// </summary>
    private async Task UpsertOrInitViAsync(string key, string? value, string fallbackDefault, CancellationToken ct)
    {
        if (_i18nService is null) return;
        if (!string.IsNullOrWhiteSpace(value))
            await _i18nService.SaveResourceAsync(key, "vi", value.Trim(), ct);
        else
            await _i18nService.InitResourceIfMissingAsync(key, "vi", fallbackDefault, ct);
    }

    private void ExecuteCancel()
    {
        var p = new NavigationParameters
        {
            { "formId",          FormId  },
            { "selectedFieldId", FieldId },
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormEditor, p);
    }

    private void ExecuteBrowseColumn()
    {
        if (_dialogService is null) return;

        var p = new DialogParameters();
        p.Add("columns", AvailableColumns.AsEnumerable());

        _dialogService.ShowDialog(ViewNames.ColumnPickerDialog, p, result =>
        {
            if (result.Result == ButtonResult.OK
                && result.Parameters.TryGetValue("selectedColumn", out ColumnInfoDto? col)
                && col is not null)
                SelectedColumn = col;
        });
    }

    private void ExecuteManageI18n()
    {
        // Nếu đã có Label_Key → mở popup dịch nhanh; chưa có → mở full I18n Manager.
        if (!string.IsNullOrWhiteSpace(LabelKey))
        {
            ExecuteOpenI18nKey("label");
            return;
        }
        var p = new NavigationParameters
        {
            { "tableCode", TableCode }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.I18nManager, p);
    }

    /// <summary>
    /// Mở popup I18nEditorDialog cho 1 trong các key của field:
    /// "label" / "placeholder" / "tooltip" / "requiredError".
    /// Popup tự lưu Sys_Resource; callback cập nhật preview tương ứng.
    /// </summary>
    private void ExecuteOpenI18nKey(string? which)
    {
        if (_dialogService is null) return;

        string key, label, seed;
        Action<string> refresh;
        switch (which)
        {
            case "label":
                key = LabelKey; label = "Nhãn field"; seed = LabelPreview;
                refresh = v => LabelPreview = v; break;
            case "placeholder":
                key = PlaceholderKey; label = "Placeholder"; seed = PlaceholderPreview;
                refresh = v => PlaceholderPreview = v; break;
            case "tooltip":
                key = TooltipKey; label = "Tooltip"; seed = TooltipPreview;
                refresh = v => TooltipPreview = v; break;
            case "requiredError":
                key = RequiredErrorKey; label = "Thông báo bắt buộc"; seed = RequiredErrorKeyPreview;
                refresh = v => RequiredErrorKeyPreview = v; break;
            case "unique":
                key = UniqueErrorKey; label = "Thông báo trùng"; seed = UniqueErrorKeyPreview;
                refresh = v => UniqueErrorKeyPreview = v; break;
            default:
                return;
        }

        if (string.IsNullOrWhiteSpace(key)) return;

        var p = new DialogParameters
        {
            { "key",          key },
            { "contextLabel", label },
            { "seedValue",    seed ?? "" }
        };
        _dialogService.ShowDialog(ViewNames.I18nEditorDialog, p, result =>
        {
            if (result.Result != ButtonResult.OK) return;
            refresh(result.Parameters.GetValue<string>("primaryValue") ?? "");
        });
    }

    private void ExecuteAddRule()
    {
        var p = new NavigationParameters
        {
            { "fieldId",     FieldId     },
            { "formId",      FormId      },
            { "fieldCode",   ColumnCode  },
            { "tableCode",   TableCode   },
            { "sectionName", SectionName },
            { "mode",        "new"       }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.ValidationRuleEditor, p);
    }

    private void ExecuteOpenRule(RuleSummaryDto? rule)
    {
        if (rule is null) return;
        var p = new NavigationParameters
        {
            { "ruleId",      rule.RuleId },
            { "fieldId",     FieldId     },
            { "formId",      FormId      },
            { "fieldCode",   ColumnCode  },
            { "tableCode",   TableCode   },
            { "sectionName", SectionName },
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.ValidationRuleEditor, p);
    }

    private async Task ExecuteDeleteRuleAsync(RuleSummaryDto? rule)
    {
        if (rule is null) return;

        // Xác nhận trước khi xóa
        var confirm = System.Windows.MessageBox.Show(
            $"Xóa rule [{rule.RuleTypeCode}] — {rule.ErrorKey}?\nThao tác này không thể hoàn tác.",
            "Xác nhận xóa rule",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        // Xóa DB nếu rule đã được lưu (RuleId > 0)
        if (rule.RuleId > 0 && _ruleService is not null)
            await _ruleService.DeleteRuleAsync(rule.RuleId, _cts.Token);

        LinkedRules.Remove(rule);
        ReindexRuleOrders();
        IsDirty = true;
    }

    private void ExecuteAddEvent()
    {
        var p = new NavigationParameters
        {
            { "fieldId",     FieldId     },
            { "formId",      FormId      },
            { "fieldCode",   ColumnCode  },
            { "tableCode",   TableCode   },
            { "sectionName", SectionName },
            { "mode",        "new"       }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.EventEditor, p);
    }

    private async void ExecuteDeleteEvent(EventSummaryDto? evt)
    {
        if (evt is null) return;

        // Xác nhận trước khi xóa — default No, không thể hoàn tác
        var confirm = System.Windows.MessageBox.Show(
            $"Xóa event [{evt.TriggerCode}] → '{evt.FieldTarget}'?\nThao tác này không thể hoàn tác.",
            "Xác nhận xóa event",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        // Xóa DB nếu event đã được lưu (EventId > 0)
        if (evt.EventId > 0 && _eventService is not null)
            await _eventService.DeleteEventAsync(evt.EventId, _cts.Token);

        LinkedEvents.Remove(evt);
        ReindexEventOrders();
        IsDirty = true;
    }

    private void ExecuteOpenEvent(EventSummaryDto? evt)
    {
        if (evt is null) return;
        var p = new NavigationParameters
        {
            { "eventId",     evt.EventId },
            { "fieldId",     FieldId     },
            { "formId",      FormId      },
            { "fieldCode",   ColumnCode  },
            { "tableCode",   TableCode   },
            { "sectionName", SectionName }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.EventEditor, p);
    }
}
