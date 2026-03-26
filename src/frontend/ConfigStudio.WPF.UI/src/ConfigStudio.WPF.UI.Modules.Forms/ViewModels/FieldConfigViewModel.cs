// File    : FieldConfigViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho màn hình cấu hình chi tiết 1 field (Screen 04).

using System.Collections.ObjectModel;
using System.Text.Json;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
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
    private readonly II18nDataService? _i18nService;
    private readonly IRuleDataService? _ruleService;
    private readonly IEventDataService? _eventService;
    private readonly ISysLookupDataService? _lookupService;
    private readonly IAppConfigService? _appConfig;
    private CancellationTokenSource _cts = new();

    // ── Navigation params ────────────────────────────────────
    private int _fieldId;
    public int FieldId { get => _fieldId; set => SetProperty(ref _fieldId, value); }

    private int _formId;
    public int FormId { get => _formId; set => SetProperty(ref _formId, value); }

    private int _sectionId;
    public int SectionId { get => _sectionId; set => SetProperty(ref _sectionId, value); }

    // ── Basic tab ────────────────────────────────────────────
    private string _columnCode = "";
    public string ColumnCode { get => _columnCode; set => SetProperty(ref _columnCode, value); }

    private string _sectionName = "";
    public string SectionName { get => _sectionName; set => SetProperty(ref _sectionName, value); }

    private string _tableCode = "";
    public string TableCode { get => _tableCode; set => SetProperty(ref _tableCode, value); }

    public ObservableCollection<ColumnInfoDto> AvailableColumns { get; } = [];

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
                IsDirty = true;
            }
        }
    }

    private string _netType = "";
    public string NetType { get => _netType; set => SetProperty(ref _netType, value); }

    public List<string> AvailableEditorTypes { get; } =
    [
        "TextBox", "NumericBox", "ComboBox", "DatePicker",
        "RadioGroup", "LookupComboBox",
        "LookupBox", "TextArea", "CheckBox", "ToggleSwitch"
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
                RaisePropertyChanged(nameof(IsFkLookupEditor));
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

    // ── FK Lookup editor (LookupBox) ─────────────────────────

    /// <summary>True khi EditorType là LookupBox (FK tham chiếu bảng nghiệp vụ).</summary>
    public bool IsFkLookupEditor => SelectedEditorType == "LookupBox";

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
    /// Điều kiện lọc bổ sung (parameterized). VD: "Is_Active = 1 AND Tenant_Id = @TenantId".
    /// Các tham số hệ thống (@TenantId, @CurrentUser) được inject tự động lúc runtime.
    /// </summary>
    public string FkFilterSql
    {
        get => _fkFilterSql;
        set { if (SetProperty(ref _fkFilterSql, value) && !_isRebuildingProps) RebuildControlPropsJson(); }
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
                RaisePropertyChanged(nameof(HasConfigExplanation));
        }
    }

    /// <summary>True khi đã có nội dung diễn giải → hiện panel kết quả.</summary>
    public bool HasConfigExplanation => !string.IsNullOrEmpty(_configExplanation);

    public DelegateCommand AddFkColumnCommand { get; private set; } = null!;
    public DelegateCommand<FkColumnConfig> RemoveFkColumnCommand { get; private set; } = null!;
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
            System.Diagnostics.Debug.WriteLine($"[FieldConfig] LoadLookupCodes lỗi: {ex.Message}");
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
            var old = _labelKey;
            if (SetProperty(ref _labelKey, value))
            {
                _ = ResolveI18nPreviewAsync(value, v => LabelPreview = v);
                // Auto-sync Placeholder/Tooltip nếu chúng đang bám theo LabelKey cũ
                SyncKeyIfLinked(ref _placeholderKey, nameof(PlaceholderKey), old, value,
                                v => PlaceholderPreview = v);
                SyncKeyIfLinked(ref _tooltipKey, nameof(TooltipKey), old, value,
                                v => TooltipPreview = v);
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
                _ = ResolveI18nPreviewAsync(value, v => PlaceholderPreview = v);
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
                _ = ResolveI18nPreviewAsync(value, v => TooltipPreview = v);
                IsDirty = true;
            }
        }
    }

    /// <summary>
    /// Nếu field phụ (placeholder/tooltip) đang bằng giá trị cũ của LabelKey
    /// hoặc chưa được nhập → tự đồng bộ theo LabelKey mới.
    /// </summary>
    private void SyncKeyIfLinked(ref string field, string propertyName,
                                  string oldLabel, string newLabel,
                                  Action<string> updatePreview)
    {
        if (field == oldLabel || string.IsNullOrEmpty(field))
        {
            field = newLabel;
            RaisePropertyChanged(propertyName);
            _ = ResolveI18nPreviewAsync(newLabel, updatePreview);
        }
    }

    private string _labelPreview = "";
    public string LabelPreview { get => _labelPreview; set => SetProperty(ref _labelPreview, value); }

    private string _placeholderPreview = "";
    public string PlaceholderPreview { get => _placeholderPreview; set => SetProperty(ref _placeholderPreview, value); }

    private string _tooltipPreview = "";
    public string TooltipPreview { get => _tooltipPreview; set => SetProperty(ref _tooltipPreview, value); }

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
        set { if (SetProperty(ref _isRequired, value)) IsDirty = true; }
    }

    private bool _isEnabled = true;
    /// <summary>
    /// Field có được tương tác không. False = grayout hoàn toàn + không submit.
    /// Khác IsReadOnly: readonly vẫn hiện giá trị và submit; disabled thì không.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set { if (SetProperty(ref _isEnabled, value)) IsDirty = true; }
    }

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

    public FieldConfigViewModel(
        IRegionManager regionManager,
        IFieldDataService? fieldService = null,
        II18nDataService? i18nService = null,
        IRuleDataService? ruleService = null,
        IEventDataService? eventService = null,
        ISysLookupDataService? lookupService = null,
        IAppConfigService? appConfig = null)
    {
        _regionManager  = regionManager;
        _fieldService   = fieldService;
        _i18nService    = i18nService;
        _ruleService    = ruleService;
        _eventService   = eventService;
        _lookupService  = lookupService;
        _appConfig      = appConfig;

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

        // FK Lookup commands
        AddFkColumnCommand         = new DelegateCommand(ExecuteAddFkColumn);
        RemoveFkColumnCommand      = new DelegateCommand<FkColumnConfig>(ExecuteRemoveFkColumn);
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
    }

    // ── INavigationAware ─────────────────────────────────────

    public async void OnNavigatedTo(NavigationContext navigationContext)
    {
        _mode     = navigationContext.Parameters.GetValue<string>("mode")      ?? "edit";
        FormId    = navigationContext.Parameters.GetValue<int>("formId");
        SectionId = navigationContext.Parameters.GetValue<int>("sectionId");
        FieldId   = navigationContext.Parameters.GetValue<int>("fieldId");
        TableCode = navigationContext.Parameters.GetValue<string>("tableCode") ?? "";

        await LoadDataAsync();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => false;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
    }

    // ── Load data (DB hoặc mock) ─────────────────────────────

    private async Task LoadDataAsync()
    {
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
    /// Load field detail, columns, linked rules/events từ DB.
    /// Tách riêng từng bước — lỗi ở bước phụ (rules/events) không làm mất dữ liệu chính.
    /// </summary>
    private async Task LoadFromDatabaseAsync()
    {
        IsLoading  = true;
        LoadError  = "";

        var ct       = _cts.Token;
        var tenantId = _appConfig!.TenantId;

        try
        {
            // ── 1. Columns (cần cho ComboBox chọn column) ─────────────────
            var tableId = await _fieldService!.GetTableIdByFormAsync(FormId, tenantId, ct);
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
                        IsNullable = c.IsNullable
                    });
                }
            }

            if (_mode == "new")
            {
                SectionName = "";
                SelectedEditorType = "TextBox";
                IsLoading = false;
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
                    SelectedColumn     = AvailableColumns.FirstOrDefault(c => c.ColumnId == field.ColumnId);
                    ColumnCode         = field.ColumnCode;
                    SelectedEditorType = field.EditorType;
                    OrderNo            = field.OrderNo;
                    ColSpan            = field.ColSpan;
                    LabelKey           = field.LabelKey;
                    // Mặc định bằng LabelKey nếu chưa cấu hình riêng
                    PlaceholderKey     = string.IsNullOrEmpty(field.PlaceholderKey) ? field.LabelKey : field.PlaceholderKey;
                    TooltipKey         = string.IsNullOrEmpty(field.TooltipKey)     ? field.LabelKey : field.TooltipKey;
                    IsVisible          = field.IsVisible;
                    IsReadOnly         = field.IsReadOnly;
                    IsRequired         = field.IsRequired;
                    IsEnabled          = field.IsEnabled;
                    ControlPropsJson   = field.ControlPropsJson ?? "{}";

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
                            QueryMode      = cfg.QueryMode;
                            FkValueField   = cfg.ValueColumn;
                            FkDisplayField = cfg.DisplayColumn;
                            FkOrderBy      = cfg.OrderBy ?? "";
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
                                                FieldName = col.TryGetProperty("fieldName", out var fn) ? fn.GetString() ?? "" : "",
                                                Caption   = col.TryGetProperty("caption",   out var cp) ? cp.GetString() ?? "" : "",
                                                Width     = col.TryGetProperty("width",     out var w)  ? w.GetInt32() : 150
                                            };
                                            colCfg.PropertyChanged += (_, _) => RebuildControlPropsJson();
                                            FkPopupColumns.Add(colCfg);
                                        }
                                }
                                catch { /* bỏ qua nếu JSON không hợp lệ */ }
                            }

                            _isRebuildingProps = false;
                            skipFkRestore:;
                        }
                        catch { _isRebuildingProps = false; /* bỏ qua lỗi load FK config */ }
                    }
                }
            }
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            // Lỗi load dữ liệu chính → báo lỗi rõ ràng, KHÔNG fallback mock
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
            catch { /* Events load thất bại — bỏ qua, không hiện lỗi */ }
        }

        IsLoading = false;
        IsDirty   = false;
    }

    // ── i18n preview resolver ────────────────────────────────

    /// <summary>
    /// Resolve i18n key thành text preview. Dùng DB nếu có, fallback mock.
    /// </summary>
    private async Task ResolveI18nPreviewAsync(string key, Action<string> setter)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            setter("");
            return;
        }

        if (_i18nService is not null && _appConfig is { IsConfigured: true })
        {
            var value = await _i18nService.ResolveKeyAsync(key, "vi", _cts.Token);
            setter(value ?? key);
        }
        else
        {
            // Chưa cấu hình DB → hiển thị key nguyên để user biết cần resolve
            setter(key);
        }
    }

    // ── Control prop schema loader ───────────────────────────

    /// <summary>
    /// Load schema control props dựa trên <see cref="SelectedEditorType"/>.
    /// Giữ lại giá trị cũ nếu PropName trùng, reset về Default nếu mới.
    /// </summary>
    private void LoadControlPropSchema()
    {
        // NOTE: Lưu giá trị cũ để khôi phục nếu PropName trùng
        var oldValues = ControlProps.ToDictionary(
            p => p.Definition.PropName,
            p => p.Value);

        ControlProps.Clear();

        var definitions = GetPropDefinitions(SelectedEditorType);

        foreach (var def in definitions)
        {
            var propValue = new ControlPropValue
            {
                Definition = def,
                Value = oldValues.TryGetValue(def.PropName, out var old) ? old : def.DefaultValue
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
    private void RebuildControlPropsJson()
    {
        _isRebuildingProps = true;
        try
        {
            var dict = ControlProps.ToDictionary(
                p => p.Definition.PropName,
                p => (object?)p.Value);

            // Sys_Lookup: đưa lookupCode vào JSON
            if (IsLookupEditor && !string.IsNullOrWhiteSpace(_lookupCode))
                dict["lookupCode"] = _lookupCode;

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
                dict["columns"] = FkPopupColumns.Select(c => new
                    { fieldName = c.FieldName, caption = c.Caption, width = c.Width }).ToList();

                if (ReloadOnChangeFields.Count > 0)
                    dict["reloadOnChange"] = ReloadOnChangeFields.ToList();

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
            new() { PropName = "maxLength",   PropType = "Number",  DefaultValue = 255,   Label = "Độ dài tối đa" },
            new() { PropName = "isMultiline", PropType = "Boolean", DefaultValue = false,  Label = "Nhiều dòng" },
            new() { PropName = "rows",        PropType = "Number",  DefaultValue = 1,      Label = "Số dòng (khi multiline)" },
        ],
        "ComboBox" =>
        [
            new() { PropName = "dataSource",   PropType = "String",  DefaultValue = "",     Label = "API endpoint datasource" },
            new() { PropName = "valueField",   PropType = "String",  DefaultValue = "id",   Label = "Field giá trị" },
            new() { PropName = "displayField", PropType = "String",  DefaultValue = "name", Label = "Field hiển thị" },
            new() { PropName = "allowNull",    PropType = "Boolean", DefaultValue = true,   Label = "Cho phép rỗng" },
        ],
        "DatePicker" =>
        [
            new() { PropName = "format",  PropType = "Enum",   DefaultValue = "dd/MM/yyyy", Label = "Định dạng ngày", AllowedValues = ["dd/MM/yyyy", "dd/MM/yyyy HH:mm", "MM/yyyy", "yyyy"] },
            new() { PropName = "minDate", PropType = "String", DefaultValue = "",            Label = "Ngày tối thiểu" },
            new() { PropName = "maxDate", PropType = "String", DefaultValue = "",            Label = "Ngày tối đa" },
        ],
        // LookupBox dùng panel riêng (FkTableName, FkValueField...) — không qua generic ControlProps
        "LookupBox" => [],
        _ => []
    };

    // ── FK Lookup command handlers ───────────────────────────

    /// <summary>Thêm 1 cột mới vào danh sách popup columns của LookupBox.</summary>
    private void ExecuteAddFkColumn()
    {
        var col = new FkColumnConfig { FieldName = "", Caption = "", Width = 150 };
        // Đăng ký rebuild JSON khi user sửa tên/caption/width inline
        col.PropertyChanged += (_, _) => RebuildControlPropsJson();
        FkPopupColumns.Add(col);
        RebuildControlPropsJson();
    }

    /// <summary>Xóa 1 cột khỏi danh sách popup columns của LookupBox.</summary>
    private void ExecuteRemoveFkColumn(FkColumnConfig col)
    {
        FkPopupColumns.Remove(col);
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
    }

    /// <summary>Xóa 1 FieldCode khỏi danh sách reloadOnChange.</summary>
    private void ExecuteRemoveReloadField(string fieldCode)
    {
        ReloadOnChangeFields.Remove(fieldCode);
        RebuildControlPropsJson();
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

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("📋 DIỄN GIẢI CẤU HÌNH LOOKUP");
        sb.AppendLine(new string('─', 50));
        sb.AppendLine();

        // ── Thông tin chung ──
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
                sb.AppendLine($"    • \"{col.Caption}\" (cột DB: {col.FieldName}, rộng: {col.Width}px)");
        }

        // ── Search ──
        sb.AppendLine();
        sb.AppendLine(FkSearchEnabled
            ? "🔎  Cho phép tìm kiếm trong danh sách."
            : "⛔  Không cho phép tìm kiếm.");

        ConfigExplanation = sb.ToString();
    }

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
            // Xác định LookupSource theo EditorType
            var lookupSource = IsLookupEditor   ? "static"
                             : IsFkLookupEditor ? "dynamic"
                             : (string?)null;

            var field = new FieldConfigRecord
            {
                FieldId          = FieldId,
                FormId           = FormId,
                SectionId        = SectionId > 0 ? SectionId : null,
                ColumnId         = SelectedColumn?.ColumnId ?? 0,
                ColumnCode       = ColumnCode,
                SectionCode      = SectionName,
                EditorType       = SelectedEditorType,
                LabelKey         = LabelKey,
                PlaceholderKey   = PlaceholderKey,
                TooltipKey       = TooltipKey,
                IsVisible        = IsVisible,
                IsReadOnly       = IsReadOnly,
                IsRequired       = IsRequired,
                IsEnabled        = IsEnabled,
                OrderNo          = OrderNo,
                ColSpan          = ColSpan,
                LookupSource     = lookupSource,
                LookupCode       = IsLookupEditor ? LookupCode : null,
                // LookupBox: ControlPropsJson không dùng (config lưu trong Ui_Field_Lookup)
                ControlPropsJson = IsFkLookupEditor ? null : ControlPropsJson
            };

            // Build lookup config cho dynamic field
            FieldLookupConfigRecord? lookupConfig = null;
            if (IsFkLookupEditor)
            {
                // Xác định SourceName theo query mode
                var sourceName = _queryMode switch
                {
                    "tvf"        => FkFunctionName,
                    "custom_sql" => FkSelectSql,
                    _            => FkTableName          // "table" (default)
                };

                // Serialize popup columns
                var popupColumnsJson = FkPopupColumns.Count > 0
                    ? JsonSerializer.Serialize(FkPopupColumns.Select(c => new
                        { fieldName = c.FieldName, caption = c.Caption, width = c.Width }))
                    : null;

                lookupConfig = new FieldLookupConfigRecord
                {
                    FieldId        = FieldId,
                    QueryMode      = _queryMode,
                    SourceName     = sourceName,
                    ValueColumn    = FkValueField,
                    DisplayColumn  = FkDisplayField,
                    FilterSql      = string.IsNullOrWhiteSpace(FkFilterSql) ? null : FkFilterSql,
                    OrderBy        = string.IsNullOrWhiteSpace(FkOrderBy)   ? null : FkOrderBy,
                    SearchEnabled  = FkSearchEnabled,
                    PopupColumnsJson = popupColumnsJson
                };
            }

            await _fieldService.SaveFieldAsync(field, _appConfig.TenantId, lookupConfig, _cts.Token);
        }
        IsDirty = false;
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
        // TODO(phase2): Mở popup chọn column từ Sys_Column
    }

    private void ExecuteManageI18n()
    {
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.I18nManager);
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
            { "fieldId", FieldId },
            { "formId", FormId },
            { "mode", "new" }
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
            { "eventId", evt.EventId },
            { "fieldId", FieldId }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.EventEditor, p);
    }
}
