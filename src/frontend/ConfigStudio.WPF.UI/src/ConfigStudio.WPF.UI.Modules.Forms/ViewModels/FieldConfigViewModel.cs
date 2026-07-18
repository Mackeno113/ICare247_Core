// File    : FieldConfigViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho màn hình cấu hình chi tiết 1 field (Screen 04).

using System.Collections.ObjectModel;
using System.Text.Json;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Helpers;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.Services;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using ConfigStudio.WPF.UI.Modules.Forms.Services;
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

    // ── Field Navigator (Left Panel) — VM con (REFACTOR-B2) ──────────────────
    /// <summary>VM con vùng Field Navigator (cây field + bulk move). Khởi tạo trong ctor.</summary>
    public FieldNavigatorVm Navigator { get; }


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
                    _ = FkLookup.LoadLookupCodesAsync();
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
            ],
            Steps:
            [
                "Tab Cơ bản: kiểm tra field map đúng cột nvarchar và đặt nhãn i18n.",
                "Tab Control Props: đặt maxLength ≤ độ dài cột DB (nvarchar(100) → 100).",
                "Cần nhiều dòng (ghi chú ngắn)? Bật isMultiline và đặt rows.",
                "Bấm Lưu Field — trỏ chuột vào từng ô nhập để xem hướng dẫn riêng.",
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
            ],
            Steps:
            [
                "Dùng cho cột nvarchar(max)/text chứa nội dung dài (mô tả, ghi chú).",
                "Tab Control Props: đặt rows ≥ 3 để vùng nhập đủ cao.",
                "Nếu cột DB có giới hạn độ dài → đặt maxLength khớp.",
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
            ],
            Steps:
            [
                "Xác định khoảng hợp lệ theo nghiệp vụ → đặt minValue / maxValue.",
                "Cột decimal(x,y)? Đặt decimals = y (VD decimal(18,2) → 2); số nguyên → 0.",
                "Ô tiền tệ: đặt spinStep 1000/10000 để bấm mũi tên nhanh.",
                "Cột DB NULLABLE và nghiệp vụ cho phép trống → bật allowNull.",
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
            ],
            Steps:
            [
                "Chọn format khớp cột DB: date → dd/MM/yyyy; datetime cần giờ → dd/MM/yyyy HH:mm.",
                "Kỳ báo cáo tháng/năm → format MM/yyyy hoặc yyyy.",
                "Giới hạn khoảng chọn bằng minDate / maxDate nếu nghiệp vụ yêu cầu.",
            ]),

        "CheckBox" => new(
            Icon:       "☑️",
            Title:      "CheckBox — Có / Không",
            WhenToUse:  "Trạng thái bật/tắt, đồng ý điều khoản...",
            ColumnType: "bit (0/1)",
            Props:      [new("(không cần cấu hình thêm)", "Mapping trực tiếp vào cột bit")],
            Steps:
            [
                "Chỉ cần map đúng cột bit ở tab Cơ bản + đặt nhãn i18n — không có props thêm.",
            ]),

        "ToggleSwitch" => new(
            Icon:       "🔘",
            Title:      "ToggleSwitch — Công tắc",
            WhenToUse:  "Active/Inactive, bật/tắt tính năng...",
            ColumnType: "bit (0/1)",
            Props:      [new("(không cần cấu hình thêm)", "Mapping trực tiếp vào cột bit")],
            Steps:
            [
                "Chỉ cần map đúng cột bit ở tab Cơ bản + đặt nhãn i18n — không có props thêm.",
                "Chọn ToggleSwitch thay CheckBox khi ý nghĩa là bật/tắt trạng thái (Active/Inactive).",
            ]),

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
            ],
            Steps:
            [
                "Tab Control Props → mục Nguồn dữ liệu: chọn chế độ Bảng/View (phổ biến) hoặc TVF / SQL.",
                "Khai Cột Value (giá trị lưu DB) và Cột Display (text hiển thị).",
                "Nhập tên bảng/View nguồn; thêm Filter SQL nếu cần lọc (hỗ trợ @TenantId, @FieldCode cascade).",
                "Mục Tìm kiếm: chọn AutoFilter + Contains cho danh mục dài.",
                "Mục Hiển thị: đặt placeholder key i18n, nút Clear cho field không bắt buộc.",
                "Bấm Diễn giải để kiểm tra cấu hình rồi Lưu Field.",
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
            ],
            Steps:
            [
                "Chỉ dùng cho danh mục TĨNH ≤ 5 lựa chọn (giới tính, trạng thái đơn giản).",
                "Tab Control Props: chọn Lookup Code trong Sys_Lookup (VD: GENDER).",
                "Kiểm tra mục 'Xem trước options' hiển thị đúng danh mục.",
                "Giá trị lưu DB là Item_Code (nvarchar) — không phải Id.",
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
            ],
            Steps:
            [
                "Dùng cho danh mục TĨNH > 5 lựa chọn từ Sys_Lookup — dạng dropdown.",
                "Tab Control Props: chọn Lookup Code, kiểm tra 'Xem trước options'.",
                "Mục Tìm kiếm: bật AutoFilter + Contains để user gõ lọc nhanh.",
                "Danh mục nghiệp vụ có bảng riêng (DM_...) → dùng LookupBox (FK) thay vì control này.",
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
            ],
            Steps:
            [
                "Tab Control Props → Nguồn dữ liệu FK: chọn chế độ Bảng/View (mặc định, khuyên dùng).",
                "Khai Cột Value = cột Id bảng nguồn (lưu vào FK) và Cột Display = cột tên hiển thị.",
                "Nhập tên bảng nguồn (VD: DM_PhongBan); cần lọc → Filter SQL (VD: Is_Active = 1).",
                "Lọc cascade theo field khác: dùng @FieldCode trong Filter SQL — tự reload khi field đó đổi.",
                "Popup grid: khai các cột hiển thị + Resource Key i18n; để trống nếu danh mục đơn giản.",
                "Muốn Import Excel theo Mã → khai Cột Mã (Code_Field).",
                "Bấm Diễn giải kiểm tra toàn bộ cấu hình rồi Lưu Field.",
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
            ],
            Steps:
            [
                "Bảng nguồn PHẢI có cột self-reference chứa Id cha (VD: Parent_Id) — kiểm tra trước.",
                "Tab Control Props → Nguồn dữ liệu FK: nhập bảng nguồn + Cột Value + Cột Display.",
                "Mục Cấu hình Tree (khung xanh): nhập Cột cha (Parent Column) — BẮT BUỘC.",
                "Chọn 'Cấp node được chọn': all / leaf (chỉ node lá) / branch (chỉ node có con).",
                "Cần lọc theo field khác (VD chi nhánh): Filter SQL với @FieldCode — cây tự reload.",
                "Bấm Diễn giải kiểm tra rồi Lưu Field.",
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
            ],
            Steps:
            [
                "Chọn chế độ theo cờ IsVirtual ở tab Cơ bản: BẬT = đa tệp (hồ sơ, ảnh SP); TẮT + map cột int = 1 tệp (logo, avatar).",
                "Tab Control Props: đặt 'loai' nếu muốn phân loại tệp (VD: HopDong).",
                "Chế độ đa tệp: ownerTable/ownerIdField thường ĐỂ TRỐNG — hệ thống tự suy từ form.",
                "Bảo mật upload server tự xử lý — không cần cấu hình thêm.",
            ]),

        _ => new(
            Icon:       "ℹ️",
            Title:      editorType,
            WhenToUse:  "",
            ColumnType: "",
            Props:      [])
    };

    /// <summary>True khi đang có cấu hình FK Lookup (dùng để confirm trước khi đổi type) —
    /// state ở VM con (B4.2 nhóm 3).</summary>
    private bool HasFkLookupConfig => FkLookup.HasFkSourceConfig;

    /// <summary>Xóa toàn bộ FK Lookup config khi user xác nhận đổi EditorType —
    /// state ở VM con (B4.2 nhóm 2+3), root chỉ giữ cờ đang-rebuild quanh reset.</summary>
    private void ClearFkLookupConfig()
    {
        _isRebuildingProps = true;
        FkLookup.ResetFkSourceState();
        // EditBox/Tree/AddNew (Migration 014/021/022/069) — state ở VM con (B4.2 nhóm 2)
        FkLookup.ResetLookupDbState();
        _isRebuildingProps = false;
        FkLookup.RaiseFkSourceProps();
    }

    // Query Mode + nguồn FK (QueryMode/Fk*/5 collection/ReloadOnChangeInput + option list)
    // — ĐÃ DỜI sang FkLookupConfigVm (B4.2 nhóm 3).

    // EditBox hiển thị (EditBoxMode/CodeField/ImportGlobalCode — Migration 014)
    // — ĐÃ DỜI sang FkLookupConfigVm (B4.2 nhóm 2).

    // Mẫu lookup dùng chung (Ui_Lookup_Template — db/083, PICKER-P4): SelectedLookupTemplate +
    // ParamRows + RebuildTemplateParamRows/BuildParamMapJson/LoadLookupTemplateStateAsync + CanonicalParamDef
    // — ĐÃ DỜI sang FkLookupConfigVm (B4.2 nhóm 4).

    // Cảnh báo cascade (CascadeWarnings/RecomputeCascadeWarnings + regex/token) — ĐÃ DỜI sang
    // FkLookupConfigVm (B4.2 nhóm 4). GetSiblingFieldCodes GIỮ ở root (phụ thuộc Navigator + FieldId),
    // VM con gọi qua _root.

    /// <summary>Tập FieldCode hiệu lực của các field KHÁC trong form (lấy từ navigator).
    /// Internal để VM con FkLookup soát cascade (B4.2 nhóm 4).</summary>
    internal List<string> GetSiblingFieldCodes() =>
        Navigator.Groups
            .SelectMany(g => g.Fields)
            .Where(f => f.FieldId != FieldId)
            .Select(f => string.IsNullOrWhiteSpace(f.FieldCode) ? f.ColumnCode : f.FieldCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    // Thêm mới entity (AllowAddNew/AddFormCode/AvailableFormCodes/LoadFormCodesAsync — Migration 022)
    // + TreeLookupBox (ParentColumn/TreeSelectableLevel — Migration 021/069)
    // + 2 option list EditBoxModeOptions/TreeSelectableLevelOptions
    // — ĐÃ DỜI sang FkLookupConfigVm (B4.2 nhóm 2).

    // ── ComboBox / LookupComboBox display props — ĐÃ DỜI sang FkLookupConfigVm (B4.2) ──

    /// <summary>Hook cho VM con FkLookup: prop lookup đổi → rebuild Control_Props_Json (guard cờ đang-rebuild).</summary>
    internal void NotifyLookupPropChanged()
    {
        if (!_isRebuildingProps) RebuildControlPropsJson();
    }

    // Diễn giải cấu hình (ConfigExplanation/IsExplanationExpanded/ExecuteExplainConfig + 2 command)
    // — ĐÃ DỜI sang FkLookupConfigVm (B4.2 nhóm 4).

    // 13 command FK Lookup — ĐÃ DỜI sang FkLookupConfigVm (B4.2 nhóm 3).
    public DelegateCommand CopyJsonCommand      { get; private set; } = null!;

    // Sys_Lookup tĩnh (LookupCode + AvailableLookupCodes + LookupPreviewItems + loader)
    // — ĐÃ DỜI sang FkLookupConfigVm (B4.2).

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
    /// Khi user nhập xong Nhãn rồi rời ô (binding LostFocus): Gợi ý nhập / Mô tả đang TRỐNG — hoặc còn giữ
    /// mặc định sinh từ mã cột — thì lấy text Nhãn. Ô nào user đã tự dịch riêng thì tôn trọng, không đè.
    /// Chỉ chạy do user thao tác (gọi từ setter khi suppress=false) → không kích hoạt lúc load/resolve.
    /// Sự kiện theo sau: ô được điền sẽ dirty → ghi Sys_Resource khi Lưu field.
    /// </summary>
    private void ApplyLabelDefaultToEmptyDisplays()
    {
        var label = (_labelPreview ?? "").Trim();
        if (label.Length == 0) return;

        var defaults = DisplayDefaultValues;
        if (I18nDefaults.IsUntranslated(PlaceholderPreview, defaults))
            PlaceholderPreview = label;
        if (I18nDefaults.IsUntranslated(TooltipPreview, defaults))
            TooltipPreview = label;
    }

    /// <summary>
    /// Giá trị coi như "chưa dịch" của field này: mã cột hiệu lực (ảo → FieldCode) ở dạng thô và tách hoa.
    /// Placeholder/Tooltip còn mang giá trị này ⇒ được phép ghi đè theo Nhãn.
    /// </summary>
    private IReadOnlyList<string> DisplayDefaultValues
        => I18nDefaults.BuildColumnMarkers(IsVirtual ? FieldCode : ColumnCode);

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

    // ── Mẫu thông báo validation mặc định (vi/en) ─────────────
    // Token runtime do backend thay: {0} = giá trị user nhập · {1} = nhãn field
    // (ResourceResolver.ApplyTokens + SaveMasterDataCommandHandler). Vì vậy mẫu KHÔNG nhúng sẵn
    // nhãn — đổi nhãn field là thông báo tự đúng theo, không cần sửa lại bản dịch.
    // Required: {0} luôn rỗng (giá trị bỏ trống) → chỉ dùng {1}.

    /// <summary>Mẫu mặc định (vi) cho thông báo lỗi khi field bắt buộc bị bỏ trống.</summary>
    private const string DefaultRequiredMessageVi = "{1} không được để trống!";

    /// <summary>Mẫu mặc định (vi) cho thông báo khi giá trị field duy nhất bị trùng.</summary>
    private const string DefaultUniqueMessageVi = "{1} {0} đã được sử dụng. Vui lòng nhập {1} khác!";

    /// <summary>Mẫu mặc định (en) cho thông báo khi giá trị field duy nhất bị trùng.</summary>
    private const string DefaultUniqueMessageEn = "{1} {0} is already in use. Please enter a different {1}!";

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
                // Key chưa có bản dịch → điền mẫu mặc định để user thấy ngay text sẽ được lưu.
                if (value)
                    _ = ResolveI18nPreviewAsync(
                        UniqueErrorKey,
                        v => UniqueErrorKeyPreview = string.IsNullOrWhiteSpace(v) ? DefaultUniqueMessageVi : v);
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

    // ── Rules + Events tab — VM con (REFACTOR-B3) ────────────
    /// <summary>VM con 2 tab Rules/Events (danh sách + mở editor + xóa). Khởi tạo trong ctor.</summary>
    public FieldRulesEventsVm RulesEvents { get; }

    // ── FK Lookup / ComboBox — VM con facade (REFACTOR-B4.1) ─
    /// <summary>VM con vùng FK Lookup/ComboBox — DataContext của 2 panel props. Hiện ủy quyền
    /// 1-1 về root (strangler); các bước B4.x dời dần state vào con mà không đụng XAML.</summary>
    public FkLookupConfigVm FkLookup { get; }

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
    public DelegateCommand<string> OpenI18nKeyCommand { get; }

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
        OpenI18nKeyCommand = new DelegateCommand<string>(ExecuteOpenI18nKey);

        // VM con Rules/Events (REFACTOR-B3): chụp ngữ cảnh root qua Func; markDirty → bật nút Lưu.
        FkLookup = new FkLookupConfigVm(this, lookupService, formService, fieldService, appConfig, logger, () => _cts.Token);

        RulesEvents = new FieldRulesEventsVm(
            ruleService, eventService, logger, regionManager,
            () => new FieldRulesEventsVm.Context(FieldId, FormId, ColumnCode, TableCode, SectionName),
            () => _cts.Token,
            () => IsDirty = true);

        // VM con Field Navigator (REFACTOR-B2): chụp ngữ cảnh root qua Func (state đổi theo navigation),
        // token theo _cts hiện hành; onLoaded → soát cascade khi list sẵn sàng.
        Navigator = new FieldNavigatorVm(
            formDetailService, fieldService, i18nService, appConfig, logger, regionManager,
            () => new FieldNavigatorVm.Context(
                FieldId, FormId, TableCode, FormCode, FormName, AvailableSections.ToList()),
            () => _cts.Token,
            FkLookup.RecomputeCascadeWarnings);

        // 13 command FK Lookup + 2 command Diễn giải — khởi tạo trong FkLookupConfigVm (B4.2 nhóm 3/4).
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
        Navigator.UpdateSelection(FieldId);
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
            var firstField = Navigator.Groups.SelectMany(g => g.Fields).FirstOrDefault();
            if (firstField is not null)
                Navigator.NavigateToField(firstField);
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
            RulesEvents.Clear();
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

        // Rules / Events liên kết — field mới chưa có
        RulesEvents.Clear();

        // FK Lookup (LookupBox/TreeLookupBox/ComboBox dynamic) — tái dùng hàm clear sẵn có (giữ
        // guard _isRebuildingProps private của root, không dời được xuống VM con — xem ClearFkLookupConfig).
        ClearFkLookupConfig();
        // Phần còn lại của VM con FkLookup (Sys_Lookup/Cb*, ReloadOnChangeInput, diễn giải + cascade)
        // — gộp 1 lệnh duy nhất (B5, thay 3 lệnh rời rạc trước đây).
        FkLookup.ResetForNewField();

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
        // ConfigExplanation/HasConfigExplanation/ShowConfigExplanation/ExplanationToggleLabel +
        // HasCascadeWarnings + ReloadOnChangeInput + LookupCode + Cb* — VM con tự raise (B4.2).

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
                    FkLookup.RestoreImportGlobalCode(await _fieldService.GetFkImportGlobalAsync(field.FieldId, ct));

                    // Mẫu lookup dùng chung (db/083, PICKER-P4) — đọc phòng thủ; chưa migrate → không dùng mẫu.
                    // State ở VM con (B4.2 nhóm 4).
                    await FkLookup.LoadLookupTemplateStateAsync(field.FieldId, ct);
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
                        await FkLookup.LoadLookupCodesAsync();
                        FkLookup.LookupCode = field.LookupCode ?? "";
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
                            FkLookup.QueryMode       = cfg.QueryMode;
                            FkLookup.FkValueField    = cfg.ValueColumn;
                            FkLookup.FkDisplayField  = cfg.DisplayColumn;
                            FkLookup.FkOrderBy       = cfg.OrderBy ?? "";
                            FkLookup.FkSearchEnabled = cfg.SearchEnabled;

                            // Phân tách source theo query mode
                            switch (cfg.QueryMode)
                            {
                                case "table":
                                    FkLookup.FkTableName = cfg.SourceName;
                                    FkLookup.FkFilterSql = cfg.FilterSql ?? "";
                                    break;
                                case "tvf":
                                    FkLookup.FkFunctionName = cfg.SourceName;
                                    break;
                                case "custom_sql":
                                    FkLookup.FkSelectSql = cfg.SourceName;
                                    break;
                            }
                            // Restore danh sách cột popup từ PopupColumnsJson
                            FkLookup.FkPopupColumns.Clear();
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
                                            FkLookup.WireFkColumnHandlers(colCfg);
                                            FkLookup.FkPopupColumns.Add(colCfg);
                                        }
                                }
                                catch { /* bỏ qua nếu JSON không hợp lệ */ }
                            }
                            // EditBox/Tree/AddNew (Migration 014/021/022/069) — state ở VM con
                            // (B4.2 nhóm 2): gán backing trong lúc cờ đang-rebuild bật, raise sau.
                            FkLookup.RestoreLookupDbConfig(cfg);
                            // Multi-Trigger (Migration 068): CSV → danh sách tag ReloadOnChangeFields.
                            FkLookup.ReloadOnChangeFields.Clear();
                            if (!string.IsNullOrWhiteSpace(cfg.ReloadTriggerFields))
                                foreach (var f in cfg.ReloadTriggerFields.Split(',',
                                             StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                                    FkLookup.ReloadOnChangeFields.Add(f);

                            _isRebuildingProps = false;
                            // Raise LookupBox new props sau khi _isRebuildingProps = false
                            FkLookup.RaiseLookupDbProps();
                            // Field đã bật thêm mới → nạp danh sách Form_Code cho combobox chọn form
                            if (FkLookup.AllowAddNew) await FkLookup.LoadFormCodesAsync();
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
                                FkLookup.QueryMode       = cfg.QueryMode;
                                FkLookup.FkValueField    = cfg.ValueColumn;
                                FkLookup.FkDisplayField  = cfg.DisplayColumn;
                                FkLookup.FkOrderBy       = cfg.OrderBy ?? "";
                                FkLookup.FkSearchEnabled = cfg.SearchEnabled;
                                switch (cfg.QueryMode)
                                {
                                    case "table":   FkLookup.FkTableName    = cfg.SourceName; FkLookup.FkFilterSql = cfg.FilterSql ?? ""; break;
                                    case "tvf":     FkLookup.FkFunctionName = cfg.SourceName; break;
                                    case "custom_sql": FkLookup.FkSelectSql = cfg.SourceName; break;
                                }
                                FkLookup.FkPopupColumns.Clear();
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
                                                FkLookup.WireRebuildOnChange(colCfg);
                                                FkLookup.FkPopupColumns.Add(colCfg);
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
                        // Parse trực tiếp từ JSON dict — WPF không reference backend Domain.
                        // State Cb* ở VM con (B4.2); giữ cờ đang-rebuild quanh restore như trước.
                        var raw = ControlPropsJsonService.ParseControlPropsJson(field.ControlPropsJson ?? "{}");
                        _isRebuildingProps = true;
                        FkLookup.RestoreComboPropsFromJson(raw);
                        _isRebuildingProps = false;
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

        // ── 3+4. Linked rules + events — VM con (REFACTOR-B3). Lỗi rules → warning nhỏ;
        //    OperationCanceled từ VM con ném tiếp → abort chuỗi load như trước.
        try
        {
            var rulesError = await RulesEvents.LoadRulesAsync(_cts.Token);
            if (rulesError is not null)
            {
                LoadError = string.IsNullOrEmpty(LoadError) ? rulesError : LoadError;
                RaisePropertyChanged(nameof(HasLoadError));
            }
            await RulesEvents.LoadEventsAsync(_cts.Token);
        }
        catch (OperationCanceledException) { return; }

        // ── 5. Field Navigator — chỉ load khi đổi form, refresh thủ công qua nút ────
        if (Navigator.LoadedFormId != FormId)
            await Navigator.LoadAsync(_cts.Token);

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
            RequiredErrorKey = FieldI18nKeyService.BuildValidationKey(
                TableCode, IsVirtual ? FieldCode : ColumnCode, "required");
    }

    /// <summary>
    /// Build key hiển thị field theo chuẩn spec 10 §1b — logic ở
    /// <see cref="FieldI18nKeyService.BuildFieldKey"/> (REFACTOR-B1), đây chỉ bơm ngữ cảnh VM.
    /// </summary>
    private string BuildFieldKey(string qualifier)
        => FieldI18nKeyService.BuildFieldKey(TableCode, IsVirtual ? FieldCode : ColumnCode, qualifier);

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
        var key = FieldI18nKeyService.BuildValidationKey(TableCode, ColumnCode, "required");
        if (string.IsNullOrEmpty(key)) return;

        _requiredErrorKey = key;
        RaisePropertyChanged(nameof(RequiredErrorKey));
        // Key chưa có bản dịch → điền mẫu mặc định để user thấy ngay text sẽ được lưu.
        _ = ResolveI18nPreviewAsync(
            key,
            v => RequiredErrorKeyPreview = string.IsNullOrWhiteSpace(v) ? DefaultRequiredMessageVi : v);
    }

    // ── Field Navigator — ủy quyền VM con (REFACTOR-B2) ──────────────────────

    /// <summary>Đồng bộ item navigator cho field vừa Lưu — logic ở FieldNavigatorVm.SyncItemAfterSave.</summary>
    private void SyncNavigatorItemAfterSave(int fieldId)
        => Navigator.SyncItemAfterSave(fieldId, LabelKey, LabelPreview);

    /// <summary>Code-behind (ContextMenuOpening) gọi trước khi mở menu — ủy quyền VM con.</summary>
    public void RefreshMoveTargets() => Navigator.RefreshMoveTargets();


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
            : ControlPropsJsonService.ParseControlPropsJson(_controlPropsJson);

        ControlProps.Clear();

        var definitions = ControlPropsJsonService.GetPropDefinitions(SelectedEditorType);

        foreach (var def in definitions)
        {
            object? resolvedValue = def.DefaultValue;
            if (oldValues.TryGetValue(def.PropName, out var saved))
                resolvedValue = ControlPropsJsonService.ConvertJsonPropValue(saved, def.PropType) ?? def.DefaultValue;

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

    /// <summary>True khi đang restore/rebuild — VM con FkLookup đọc để bỏ qua side-effect setter
    /// (B4.2 nhóm 3, tương đương guard `!_isRebuildingProps` cũ ở root).</summary>
    internal bool IsRebuildingProps => _isRebuildingProps;

    /// <summary>
    /// Rebuild JSON từ snapshot state hiện tại — logic lắp JSON nằm ở
    /// <see cref="ControlPropsJsonService.BuildJson"/> (REFACTOR-B1); VM chỉ chụp state + gán kết quả.
    /// Internal để VM con FkLookup gọi từ command handler (B4.2 nhóm 3).
    /// </summary>
    internal void RebuildControlPropsJson()
    {
        _isRebuildingProps = true;
        try
        {
            ControlPropsJson = ControlPropsJsonService.BuildJson(new ControlPropsJsonService.BuildInput
            {
                ControlProps = ControlProps.ToList(),

                IsLookupEditor = IsLookupEditor,
                LookupCode = FkLookup.LookupCode,

                IsComboLike = IsComboBoxEditor || SelectedEditorType == "LookupComboBox",
                CbSearchMode = FkLookup.CbSearchMode,
                CbSearchFilterCondition = FkLookup.CbSearchFilterCondition,
                CbAllowUserInput = FkLookup.CbAllowUserInput,
                CbDropDownWidthMode = FkLookup.CbDropDownWidthMode,
                CbClearButton = FkLookup.CbClearButton,
                CbNullTextKey = FkLookup.CbNullTextKey,
                CbGroupFieldName = FkLookup.CbGroupFieldName,
                CbDisabledFieldName = FkLookup.CbDisabledFieldName,

                IsFkLookupEditor = IsFkLookupEditor,
                QueryMode = FkLookup.QueryMode,
                FkValueField = FkLookup.FkValueField,
                FkDisplayField = FkLookup.FkDisplayField,
                FkSearchEnabled = FkLookup.FkSearchEnabled,
                FkOrderBy = FkLookup.FkOrderBy,
                FkTableName = FkLookup.FkTableName,
                FkFilterSql = FkLookup.FkFilterSql,
                FkFunctionName = FkLookup.FkFunctionName,
                FkSelectSql = FkLookup.FkSelectSql,
                FilterParams = FkLookup.FkFilterParams.ToList(),
                FunctionParams = FkLookup.FkFunctionParams.ToList(),
                PopupColumns = FkLookup.FkPopupColumns.ToList(),
                DataSourceConditions = FkLookup.DataSourceConditions.ToList(),
            });

            IsDirty = true;
        }
        finally
        {
            _isRebuildingProps = false;
        }
    }

    // ── FK Lookup command handlers — ĐÃ DỜI sang FkLookupConfigVm (B4.2 nhóm 3):
    //    13 command + WireFkColumnHandlers (handler đặt tên thay lambda +=) + helper caption-key.
    // ── Diễn giải cấu hình (ExecuteExplainConfig) — ĐÃ DỜI sang FkLookupConfigVm (B4.2 nhóm 4).

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
                LookupCode       = IsLookupEditor ? FkLookup.LookupCode : null,
                // LookupBox: ControlPropsJson = null (toàn bộ config lưu trong Ui_Field_Lookup)
                // ComboBox/LookupComboBox: ControlPropsJson lưu search+display props
                ControlPropsJson = IsFkLookupEditor ? null : ControlPropsJson
            };

            // Build lookup config cho dynamic field (LookupBox hoặc ComboBox)
            FieldLookupConfigRecord? lookupConfig = null;
            if (IsFkLookupEditor || IsComboBoxEditor)
            {
                // Xác định SourceName theo query mode (state ở VM con — B4.2 nhóm 3)
                var sourceName = FkLookup.QueryMode switch
                {
                    "tvf"        => FkLookup.FkFunctionName,
                    "custom_sql" => FkLookup.FkSelectSql,
                    _            => FkLookup.FkTableName          // "table" (default)
                };

                // Serialize popup columns
                // Lưu captionKey (i18n key) vào DB — backend resolve khi trả Blazor
                var popupColumnsJson = FkLookup.FkPopupColumns.Count > 0
                    ? JsonSerializer.Serialize(FkLookup.FkPopupColumns.Select(c => new
                        { fieldName = c.FieldName, captionKey = c.CaptionKey, width = c.Width }))
                    : null;

                lookupConfig = new FieldLookupConfigRecord
                {
                    FieldId          = FieldId,
                    QueryMode        = FkLookup.QueryMode,
                    SourceName       = sourceName,
                    ValueColumn      = FkLookup.FkValueField,
                    DisplayColumn    = FkLookup.FkDisplayField,
                    FilterSql        = string.IsNullOrWhiteSpace(FkLookup.FkFilterSql) ? null : FkLookup.FkFilterSql,
                    OrderBy          = string.IsNullOrWhiteSpace(FkLookup.FkOrderBy)   ? null : FkLookup.FkOrderBy,
                    SearchEnabled    = FkLookup.FkSearchEnabled,
                    PopupColumnsJson = popupColumnsJson,
                    // LookupBox-specific props (state ở VM con — B4.2 nhóm 2) — ComboBox giữ default
                    EditBoxMode         = IsFkLookupEditor ? FkLookup.EditBoxMode : "TextOnly",
                    CodeField           = IsFkLookupEditor && !string.IsNullOrWhiteSpace(FkLookup.CodeField)
                                          ? FkLookup.CodeField : null,
                    DropDownWidth       = IsFkLookupEditor ? FkLookup.DropDownWidth  : 600,
                    DropDownHeight      = IsFkLookupEditor ? FkLookup.DropDownHeight : 400,
                    ReloadTriggerField  = !string.IsNullOrWhiteSpace(FkLookup.ReloadTriggerField)
                                          ? FkLookup.ReloadTriggerField : null,
                    // Multi-Trigger (Migration 068): danh sách field cha → CSV. Runtime hợp với @param Filter SQL.
                    ReloadTriggerFields = FkLookup.ReloadOnChangeFields.Count > 0
                                          ? string.Join(",", FkLookup.ReloadOnChangeFields) : null,
                    // TreeLookupBox (Migration 021 + 069)
                    ParentColumn        = IsTreeLookupEditor && !string.IsNullOrWhiteSpace(FkLookup.ParentColumn)
                                          ? FkLookup.ParentColumn : null,
                    TreeSelectableLevel = IsTreeLookupEditor && FkLookup.TreeSelectableLevel != "all"
                                          ? FkLookup.TreeSelectableLevel : null,
                    // Thêm mới entity (Migration 022) — chỉ lưu khi bật + có form code
                    AllowAddNew         = IsFkLookupEditor && FkLookup.AllowAddNew
                                          && !string.IsNullOrWhiteSpace(FkLookup.AddFormCode),
                    AddFormCode         = IsFkLookupEditor && FkLookup.AllowAddNew
                                          && !string.IsNullOrWhiteSpace(FkLookup.AddFormCode)
                                          ? FkLookup.AddFormCode.Trim() : null,
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
                    await _fieldService.SaveFkImportGlobalAsync(savedId, FkLookup.ImportGlobalCode, _cts.Token);

                // Mẫu lookup dùng chung (db/083) → ghi Template_Code + Param_Map (phòng thủ — chưa migrate thì bỏ qua).
                // State ở VM con (B4.2 nhóm 4).
                if (IsFkLookupEditor && savedId > 0)
                    await _fieldService.SaveFieldLookupTemplateAsync(
                        savedId,
                        FkLookup.SelectedTemplateCodeOrNull,
                        FkLookup.BuildParamMapJson(),
                        _cts.Token);

                // Đăng ký i18n keys vào Sys_Resource nếu chưa tồn tại
                await RegisterI18nKeysAsync(_cts.Token);

                // User bấm "Lưu Field" → đánh dấu ĐÃ cấu hình (badge navigator). Chỉ ở đây, không
                // gọi từ auto-generate/bulk nên field sinh tự động vẫn ở trạng thái "chưa cấu hình".
                if (savedId > 0)
                {
                    await _fieldService.MarkFieldConfiguredAsync(savedId, _cts.Token);

                    // Navigator chỉ nạp lại khi ĐỔI form → phải đồng bộ item ngay tại chỗ, nếu không
                    // badge vẫn hiện "chưa cấu hình" và tên vẫn là mã cột dù DB đã cập nhật.
                    // Gọi SAU RegisterI18nKeysAsync để lấy đúng nhãn (vi) vừa ghi vào Sys_Resource.
                    SyncNavigatorItemAfterSave(savedId);
                }

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

        // Toàn bộ orchestration ghi key nằm ở FieldI18nKeyService (REFACTOR-B1) — VM chụp state
        // vào Request rồi áp Result lên ô nhập (SetResolvedValue — không kích hoạt dirty).
        var result = await FieldI18nKeyService.RegisterKeysAsync(_i18nService, new FieldI18nKeyService.RegisterKeysRequest
        {
            LabelKey = LabelKey,
            LabelValue = LabelPreview,
            PlaceholderKey = PlaceholderKey,
            PlaceholderValue = PlaceholderPreview,
            TooltipKey = TooltipKey,
            TooltipValue = TooltipPreview,
            ColumnCode = ColumnCode,
            TableCode = TableCode,
            IsRequired = IsRequired,
            RequiredErrorKey = RequiredErrorKey,
            RequiredErrorValue = RequiredErrorKeyPreview,
            DefaultRequiredMessageVi = DefaultRequiredMessageVi,
            IsUnique = IsUnique,
            UniqueErrorValue = UniqueErrorKeyPreview,
            DefaultUniqueMessageVi = DefaultUniqueMessageVi,
            DefaultUniqueMessageEn = DefaultUniqueMessageEn,
            PopupColumnKeys = FkLookup.FkPopupColumns.Select(c => (c.CaptionKey, c.FieldName)).ToList(),
            DisplayDefaults = DisplayDefaultValues,
        }, ct);

        if (result.PlaceholderApplied is { } ph) SetResolvedValue(v => PlaceholderPreview = v, ph);
        if (result.TooltipApplied is { } tt)     SetResolvedValue(v => TooltipPreview = v, tt);
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

        // Nhãn là nguồn của Gợi ý nhập / Mô tả: popup ghi luôn 2 key kia ở ngôn ngữ nào chúng chưa dịch.
        var isLabel = which == "label";
        if (isLabel)
        {
            p.Add("followKeys", (IReadOnlyList<string>)new[] { PlaceholderKey, TooltipKey });
            p.Add("defaultValues", DisplayDefaultValues);
        }

        _dialogService.ShowDialog(ViewNames.I18nEditorDialog, p, result =>
        {
            if (result.Result != ButtonResult.OK) return;
            refresh(result.Parameters.GetValue<string>("primaryValue") ?? "");

            // Popup vừa có thể ghi đè placeholder/tooltip (vi) → resolve lại để ô nhập khớp DB.
            if (!isLabel) return;
            _ = ResolveI18nPreviewAsync(PlaceholderKey, v => PlaceholderPreview = v);
            _ = ResolveI18nPreviewAsync(TooltipKey, v => TooltipPreview = v);
        });
    }

    // Rules/Events command handlers đã chuyển sang FieldRulesEventsVm (REFACTOR-B3).
}
