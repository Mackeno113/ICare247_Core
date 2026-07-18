// File    : FkLookupConfigVm.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : REFACTOR-B4.1 — VM con vùng cấu hình FK Lookup / ComboBox (2 panel
//           LookupBoxPropsPanel + ComboBoxPropsPanel đổi DataContext về đây; binding BÊN TRONG
//           panel giữ nguyên đường dẫn). Bước strangler: hiện tại ỦY QUYỀN 1-1 về root
//           FieldConfigViewModel (state + logic vẫn ở root, notify bridge re-raise cùng tên) —
//           các bước B4.x sau dời dần state/logic vào đây mà KHÔNG đụng XAML nữa.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using ConfigStudio.WPF.UI.Modules.Forms.Services;
using Prism.Commands;
using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>VM con vùng FK Lookup/ComboBox — root expose qua property <c>FkLookup</c>.</summary>
public sealed class FkLookupConfigVm : BindableBase
{
    private readonly FieldConfigViewModel _root;
    private readonly ISysLookupDataService? _lookupService;
    private readonly IFormDataService? _formService;
    private readonly IAppConfigService? _appConfig;
    private readonly IAppLogger? _logger;
    private readonly Func<CancellationToken> _token;

    public FkLookupConfigVm(
        FieldConfigViewModel root,
        ISysLookupDataService? lookupService,
        IFormDataService? formService,
        IAppConfigService? appConfig,
        IAppLogger? logger,
        Func<CancellationToken> token)
    {
        _root = root;
        _lookupService = lookupService;
        _formService = formService;
        _appConfig = appConfig;
        _logger = logger;
        _token = token;
        // Bridge notify: prop ủy quyền TRÙNG TÊN với root → re-raise nguyên PropertyName.
        // Tên không thuộc VM này (binding không tồn tại) chỉ tốn 1 lần so tên — vô hại.
        _root.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);

        // 13 command FK Lookup (B4.2 nhóm 3) — handler đặt tên, không lambda inline.
        SetQueryModeCommand              = new DelegateCommand<string>(mode => QueryMode = mode);
        AddFkColumnCommand               = new DelegateCommand(ExecuteAddFkColumn);
        RemoveFkColumnCommand            = new DelegateCommand<FkColumnConfig>(ExecuteRemoveFkColumn);
        MoveFkColumnUpCommand            = new DelegateCommand<FkColumnConfig>(ExecuteMoveFkColumnUp);
        MoveFkColumnDownCommand          = new DelegateCommand<FkColumnConfig>(ExecuteMoveFkColumnDown);
        AddFkFilterParamCommand          = new DelegateCommand(ExecuteAddFkFilterParam);
        RemoveFkFilterParamCommand       = new DelegateCommand<FkFilterParam>(ExecuteRemoveFkFilterParam);
        AddFunctionParamCommand          = new DelegateCommand(ExecuteAddFunctionParam);
        RemoveFunctionParamCommand       = new DelegateCommand<FunctionParam>(ExecuteRemoveFunctionParam);
        AddReloadFieldCommand            = new DelegateCommand(ExecuteAddReloadField);
        RemoveReloadFieldCommand         = new DelegateCommand<string>(ExecuteRemoveReloadField);
        AddDataSourceConditionCommand    = new DelegateCommand(ExecuteAddDataSourceCondition);
        RemoveDataSourceConditionCommand = new DelegateCommand<DataSourceCondition>(ExecuteRemoveDataSourceCondition);
    }

    // ── Cờ editor type (root suy từ SelectedEditorType) ──────────────────────
    public bool IsLookupEditor => _root.IsLookupEditor;
    public bool IsLookupOrComboBoxEditor => _root.IsLookupOrComboBoxEditor;
    public bool IsFkLookupEditor => _root.IsFkLookupEditor;
    public bool IsTreeLookupEditor => _root.IsTreeLookupEditor;
    public bool IsComboBoxEditor => _root.IsComboBoxEditor;

    // ── Chế độ truy vấn — STATE Ở ĐÂY (B4.2 nhóm 3) ──────────────────────────

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
                if (!_root.IsRebuildingProps) _root.RebuildControlPropsJson();
            }
        }
    }

    public bool IsTableMode    => _queryMode == "table";
    public bool IsFunctionMode => _queryMode == "function";
    public bool IsSqlMode      => _queryMode == "sql";

    public DelegateCommand<string> SetQueryModeCommand { get; }

    // ── Nguồn dữ liệu FK — STATE Ở ĐÂY (B4.2 nhóm 3) ─────────────────────────
    // Setter đổi giá trị → rebuild Control_Props_Json (bỏ qua khi root đang rebuild/restore).

    private string _fkTableName = "";
    /// <summary>Tên bảng DB nguồn. VD: "DM_PhongBan".</summary>
    public string FkTableName
    {
        get => _fkTableName;
        set { if (SetProperty(ref _fkTableName, value) && !_root.IsRebuildingProps) _root.RebuildControlPropsJson(); }
    }

    private string _fkFunctionName = "";
    /// <summary>Tên TVF (Table-Valued Function). VD: "fnt_CongTyTheoQuyen".</summary>
    public string FkFunctionName
    {
        get => _fkFunctionName;
        set { if (SetProperty(ref _fkFunctionName, value) && !_root.IsRebuildingProps) _root.RebuildControlPropsJson(); }
    }

    private string _fkSelectSql = "";
    /// <summary>
    /// Full SELECT SQL (queryMode = "sql"). Phải có alias khớp ValueField + DisplayField.
    /// VD: "SELECT p.Id, p.Ten FROM DM_PhongBan p JOIN ... WHERE ..."
    /// </summary>
    public string FkSelectSql
    {
        get => _fkSelectSql;
        set { if (SetProperty(ref _fkSelectSql, value) && !_root.IsRebuildingProps) _root.RebuildControlPropsJson(); }
    }

    private string _fkValueField = "";
    /// <summary>Cột lưu vào DB (FK — int). VD: "PhongBan_Id".</summary>
    public string FkValueField
    {
        get => _fkValueField;
        set { if (SetProperty(ref _fkValueField, value) && !_root.IsRebuildingProps) _root.RebuildControlPropsJson(); }
    }

    private string _fkDisplayField = "";
    /// <summary>Cột hiển thị chính trong ô input. VD: "Ten_PhongBan".</summary>
    public string FkDisplayField
    {
        get => _fkDisplayField;
        set { if (SetProperty(ref _fkDisplayField, value) && !_root.IsRebuildingProps) _root.RebuildControlPropsJson(); }
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
            if (SetProperty(ref _fkFilterSql, value) && !_root.IsRebuildingProps)
            {
                _root.RebuildControlPropsJson();
                _root.RecomputeCascadeWarnings();
            }
        }
    }

    private string _fkOrderBy = "";
    /// <summary>Sắp xếp kết quả. VD: "Ten_PhongBan ASC".</summary>
    public string FkOrderBy
    {
        get => _fkOrderBy;
        set { if (SetProperty(ref _fkOrderBy, value) && !_root.IsRebuildingProps) _root.RebuildControlPropsJson(); }
    }

    private bool _fkSearchEnabled = true;
    /// <summary>Cho phép search trong popup (incremental search).</summary>
    public bool FkSearchEnabled
    {
        get => _fkSearchEnabled;
        set { if (SetProperty(ref _fkSearchEnabled, value) && !_root.IsRebuildingProps) _root.RebuildControlPropsJson(); }
    }

    /// <summary>Danh sách cột hiển thị trong popup dropdown của LookupBox.</summary>
    public ObservableCollection<FkColumnConfig> FkPopupColumns { get; } = [];

    /// <summary>
    /// Danh sách tham số động trong filterSql — mỗi item ánh xạ @Param → FieldCode trong form.
    /// Runtime engine resolve giá trị field rồi truyền vào SQL; khi field thay đổi → reload lookup.
    /// </summary>
    public ObservableCollection<FkFilterParam> FkFilterParams { get; } = [];

    /// <summary>Danh sách tham số của TVF — thứ tự quan trọng (khớp với định nghĩa hàm).</summary>
    public ObservableCollection<FunctionParam> FkFunctionParams { get; } = [];

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

    // ── 13 command FK Lookup — Ở ĐÂY (B4.2 nhóm 3, khởi tạo trong ctor) ──────
    public DelegateCommand AddFkColumnCommand { get; }
    public DelegateCommand<FkColumnConfig> RemoveFkColumnCommand { get; }
    public DelegateCommand<FkColumnConfig> MoveFkColumnUpCommand { get; }
    public DelegateCommand<FkColumnConfig> MoveFkColumnDownCommand { get; }
    public DelegateCommand AddFkFilterParamCommand { get; }
    public DelegateCommand<FkFilterParam> RemoveFkFilterParamCommand { get; }
    public DelegateCommand AddFunctionParamCommand { get; }
    public DelegateCommand<FunctionParam> RemoveFunctionParamCommand { get; }
    public DelegateCommand AddReloadFieldCommand { get; }
    public DelegateCommand<string> RemoveReloadFieldCommand { get; }
    public DelegateCommand AddDataSourceConditionCommand { get; }
    public DelegateCommand<DataSourceCondition> RemoveDataSourceConditionCommand { get; }

    // ── Danh sách option tĩnh — SỞ HỮU tại đây từ B4.2 nhóm 3 ────────────────
    /// <summary>Nguồn tham số TVF.</summary>
    public List<string> FunctionParamSourceTypes { get; } = ["field", "system"];
    /// <summary>Tham số hệ thống có sẵn.</summary>
    public List<string> SystemKeyOptions { get; } = ["@TenantId", "@Today", "@CurrentUser"];
    /// <summary>Kiểu dữ liệu hợp lệ cho tham số filter.</summary>
    public List<string> FkParamTypes { get; } = ["String", "DateTime", "Int", "Decimal"];
    /// <summary>Các phép so sánh hợp lệ trong DataSourceCondition.WhenOp.</summary>
    public List<string> WhenOpOptions { get; } =
        ["eq", "neq", "gt", "gte", "lt", "lte", "contains", "startsWith"];
    // Option list nhóm 2 — SỞ HỮU tại đây từ B4.2 nhóm 2 (root đã xóa).
    public List<string> EditBoxModeOptions { get; } = ["TextOnly", "CodeAndName", "Custom"];
    public List<string> TreeSelectableLevelOptions { get; } = ["all", "leaf", "branch"];
    // 4 option list ComboBox — SỞ HỮU tại đây từ B4.2 (root đã xóa).
    public List<string> SearchModeOptions { get; } = ["None", "AutoFilter", "AutoSearch"];
    public List<string> SearchFilterConditionOptions { get; } = ["Contains", "StartsWith", "Equals"];
    public List<string> DropDownWidthModeOptions { get; } = ["ContentOrEditorWidth", "ContentWidth", "EditorWidth"];
    public List<string> ClearButtonModeOptions { get; } = ["Hidden", "Auto"];

    // ── EditBox hiển thị — STATE Ở ĐÂY (B4.2 nhóm 2, lưu Ui_Field_Lookup) ────

    /// <summary>
    /// Chế độ hiển thị EditBox khi đã chọn FK record (chỉ LookupBox).
    /// "TextOnly" | "CodeAndName" | "Custom". Mặc định: "TextOnly".
    /// </summary>
    private string _editBoxMode = "TextOnly";
    public string EditBoxMode
    {
        get => _editBoxMode;
        set
        {
            if (SetProperty(ref _editBoxMode, value))
            {
                RaisePropertyChanged(nameof(IsCodeAndNameMode));
                _root.IsDirty = true;
            }
        }
    }

    /// <summary>True khi EditBoxMode = "CodeAndName" — hiện thêm input CodeField.</summary>
    public bool IsCodeAndNameMode => _editBoxMode == "CodeAndName";

    /// <summary>Tên cột mã code trong data source — dùng khi EditBoxMode = "CodeAndName".</summary>
    private string _codeField = "";
    public string CodeField
    {
        get => _codeField;
        set { if (SetProperty(ref _codeField, value)) _root.IsDirty = true; }
    }

    /// <summary>
    /// Import: bỏ Filter_Sql (lọc cha cascade) → tra Mã con trên toàn bảng (Ui_Field_Lookup.Import_Global_Code).
    /// Chỉ bật cho FK có Mã con DUY NHẤT toàn cục (vd chi nhánh); trùng Mã → engine từ chối cả file khi import.
    /// </summary>
    private bool _importGlobalCode;
    public bool ImportGlobalCode
    {
        get => _importGlobalCode;
        set { if (SetProperty(ref _importGlobalCode, value)) _root.IsDirty = true; }
    }

    /// <summary>Chiều rộng popup grid (px). Mặc định: 600.</summary>
    private int _dropDownWidth = 600;
    public int DropDownWidth
    {
        get => _dropDownWidth;
        set { if (SetProperty(ref _dropDownWidth, value)) _root.IsDirty = true; }
    }

    /// <summary>Chiều cao popup grid (px). Mặc định: 400.</summary>
    private int _dropDownHeight = 400;
    public int DropDownHeight
    {
        get => _dropDownHeight;
        set { if (SetProperty(ref _dropDownHeight, value)) _root.IsDirty = true; }
    }

    /// <summary>
    /// FieldCode của field khác trong form — khi thay đổi, LookupBox tự clear + reload.
    /// Lưu vào Ui_Field_Lookup.Reload_Trigger_Field (single field, đơn giản nhất).
    /// </summary>
    private string _reloadTriggerField = "";
    public string ReloadTriggerField
    {
        get => _reloadTriggerField;
        set
        {
            if (SetProperty(ref _reloadTriggerField, value))
            {
                _root.IsDirty = true;
                _root.RecomputeCascadeWarnings();
            }
        }
    }

    // ── Mẫu lookup dùng chung (PICKER-P4) ────────────────────────────────────
    public ObservableCollection<LookupTemplateRecord> LookupTemplates => _root.LookupTemplates;
    public LookupTemplateRecord SelectedLookupTemplate
    {
        get => _root.SelectedLookupTemplate;
        set => _root.SelectedLookupTemplate = value;
    }
    public bool IsLookupTemplateSelected => _root.IsLookupTemplateSelected;
    public string? SelectedLookupTemplateMoTa => _root.SelectedLookupTemplateMoTa;
    public ObservableCollection<LookupTemplateParamRowVm> LookupTemplateParamRows => _root.LookupTemplateParamRows;
    public bool HasLookupTemplateParams => _root.HasLookupTemplateParams;
    public bool HasNoLookupTemplateParams => _root.HasNoLookupTemplateParams;

    // ── Cảnh báo cascade ─────────────────────────────────────────────────────
    public ObservableCollection<string> CascadeWarnings => _root.CascadeWarnings;
    public bool HasCascadeWarnings => _root.HasCascadeWarnings;

    // ── Thêm mới entity từ LookupBox — STATE Ở ĐÂY (B4.2 nhóm 2, Migration 022) ──

    private bool _allowAddNew;
    /// <summary>Bật nút "➕ Thêm mới" trên LookupBox runtime → lưu Ui_Field_Lookup.Allow_Add_New.</summary>
    public bool AllowAddNew
    {
        get => _allowAddNew;
        set
        {
            if (SetProperty(ref _allowAddNew, value))
            {
                _root.IsDirty = true;
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
        set { if (SetProperty(ref _addFormCode, value)) _root.IsDirty = true; }
    }

    /// <summary>Danh sách Form_Code có sẵn của tenant — nguồn cho combobox chọn form dialog thêm mới.</summary>
    public ObservableCollection<string> AvailableFormCodes { get; } = [];

    /// <summary>
    /// Nạp danh sách Form_Code thật từ <c>Ui_Form</c> của tenant qua <see cref="IFormDataService"/>.
    /// Dùng cho combobox "Form Code dialog thêm mới". Sau sự kiện: combobox có dữ liệu để chọn.
    /// </summary>
    internal async Task LoadFormCodesAsync()
    {
        if (_formService is null || _appConfig is not { IsConfigured: true }) return;
        try
        {
            var forms = await _formService.GetAllFormsAsync(_appConfig.TenantId, false, _token());
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

    // ── TreeLookupBox — STATE Ở ĐÂY (B4.2 nhóm 2, Migration 021 + 069) ───────

    /// <summary>
    /// Tên cột Parent Id trong bảng nguồn — bắt buộc với TreeLookupBox.
    /// VD: "Parent_Id". Lưu vào Ui_Field_Lookup.Parent_Column.
    /// </summary>
    private string _parentColumn = "";
    public string ParentColumn
    {
        get => _parentColumn;
        // Side-effect như root cũ: rebuild Control_Props_Json (hook tự guard cờ đang-rebuild).
        set { if (SetProperty(ref _parentColumn, value)) _root.NotifyLookupPropChanged(); }
    }

    private string _treeSelectableLevel = "all";
    /// <summary>Giới hạn node được chọn: "all" | "leaf" | "branch". Lưu Ui_Field_Lookup.Tree_Selectable_Level.</summary>
    public string TreeSelectableLevel
    {
        get => _treeSelectableLevel;
        set { if (SetProperty(ref _treeSelectableLevel, value)) _root.IsDirty = true; }
    }

    // ── Diễn giải cấu hình ───────────────────────────────────────────────────
    public string ConfigExplanation => _root.ConfigExplanation;
    public bool HasConfigExplanation => _root.HasConfigExplanation;
    public bool ShowConfigExplanation => _root.ShowConfigExplanation;
    public string ExplanationToggleLabel => _root.ExplanationToggleLabel;
    public DelegateCommand ExplainConfigCommand => _root.ExplainConfigCommand;
    public DelegateCommand ToggleExplanationCommand => _root.ToggleExplanationCommand;

    // ── ComboBox / LookupComboBox (Cb*) — STATE Ở ĐÂY (B4.2) ─────────────────
    // Setter đổi giá trị → root rebuild Control_Props_Json (hook nội bộ tự guard cờ đang-rebuild).

    /// <summary>Chế độ tìm kiếm trong dropdown DxComboBox: "None" | "AutoFilter" | "AutoSearch".</summary>
    private string _cbSearchMode = "AutoFilter";
    public string CbSearchMode
    {
        get => _cbSearchMode;
        set
        {
            if (SetProperty(ref _cbSearchMode, value))
            {
                RaisePropertyChanged(nameof(ShowSearchFilterCondition));
                _root.NotifyLookupPropChanged();
            }
        }
    }

    /// <summary>Điều kiện so khớp khi tìm kiếm: "Contains" | "StartsWith" | "Equals".</summary>
    private string _cbSearchFilterCondition = "Contains";
    public string CbSearchFilterCondition
    {
        get => _cbSearchFilterCondition;
        set { if (SetProperty(ref _cbSearchFilterCondition, value)) _root.NotifyLookupPropChanged(); }
    }

    /// <summary>True khi SearchMode != "None" — hiện dropdown SearchFilterCondition.</summary>
    public bool ShowSearchFilterCondition => _cbSearchMode != "None";

    /// <summary>Cho phép người dùng nhập text tự do (AllowUserInput). Mặc định: false.</summary>
    private bool _cbAllowUserInput;
    public bool CbAllowUserInput
    {
        get => _cbAllowUserInput;
        set { if (SetProperty(ref _cbAllowUserInput, value)) _root.NotifyLookupPropChanged(); }
    }

    /// <summary>I18n key cho placeholder khi chưa chọn. Rỗng = dùng fallback "-- Chọn --".</summary>
    private string _cbNullTextKey = "";
    public string CbNullTextKey
    {
        get => _cbNullTextKey;
        set { if (SetProperty(ref _cbNullTextKey, value)) _root.NotifyLookupPropChanged(); }
    }

    /// <summary>Chiều rộng dropdown: "ContentOrEditorWidth" | "ContentWidth" | "EditorWidth".</summary>
    private string _cbDropDownWidthMode = "ContentOrEditorWidth";
    public string CbDropDownWidthMode
    {
        get => _cbDropDownWidthMode;
        set { if (SetProperty(ref _cbDropDownWidthMode, value)) _root.NotifyLookupPropChanged(); }
    }

    /// <summary>Nút xóa: "Hidden" | "Auto". Mặc định: "Auto".</summary>
    private string _cbClearButton = "Auto";
    public string CbClearButton
    {
        get => _cbClearButton;
        set { if (SetProperty(ref _cbClearButton, value)) _root.NotifyLookupPropChanged(); }
    }

    /// <summary>Tên field để group items trong dropdown — chỉ ComboBox (dynamic). Rỗng = không group.</summary>
    private string _cbGroupFieldName = "";
    public string CbGroupFieldName
    {
        get => _cbGroupFieldName;
        set { if (SetProperty(ref _cbGroupFieldName, value)) _root.NotifyLookupPropChanged(); }
    }

    /// <summary>Tên field bool để disable item trong dropdown — chỉ ComboBox (dynamic).</summary>
    private string _cbDisabledFieldName = "";
    public string CbDisabledFieldName
    {
        get => _cbDisabledFieldName;
        set { if (SetProperty(ref _cbDisabledFieldName, value)) _root.NotifyLookupPropChanged(); }
    }

    // ── Sys_Lookup tĩnh (LookupComboBox) — STATE Ở ĐÂY (B4.2) ────────────────

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
                _root.NotifyLookupPropChanged();
            }
        }
    }

    /// <summary>Danh sách lookup codes có sẵn trong DB (dùng cho dropdown gợi ý).</summary>
    public ObservableCollection<string> AvailableLookupCodes { get; } = [];

    /// <summary>Preview các items của lookup code đang chọn.</summary>
    public ObservableCollection<LookupItemDto> LookupPreviewItems { get; } = [];

    /// <summary>Nạp danh sách lookup code từ Sys_Lookup (lỗi → log, không chặn màn).</summary>
    public async Task LoadLookupCodesAsync()
    {
        if (_lookupService is null) return;
        try
        {
            var codes = await _lookupService.GetAllCodesAsync(_token());
            AvailableLookupCodes.Clear();
            foreach (var c in codes) AvailableLookupCodes.Add(c);
        }
        catch (Exception ex)
        {
            // Log lỗi — thường do bảng Sys_Lookup chưa được tạo (migration 004 chưa chạy)
            _logger?.Capture(ex, "FieldConfig.LoadLookupCodes");
        }
    }

    /// <summary>Nạp preview item của 1 lookup code (tự chạy khi đổi LookupCode).</summary>
    private async Task LoadLookupPreviewAsync(string code)
    {
        LookupPreviewItems.Clear();
        if (_lookupService is null || string.IsNullOrWhiteSpace(code)) return;
        var items = await _lookupService.GetByCodeAsync(code, "vi", _token());
        foreach (var i in items)
            LookupPreviewItems.Add(new LookupItemDto { ItemCode = i.ItemCode, Label = i.Label });
    }

    /// <summary>
    /// Restore Cb* từ dict Control_Props_Json khi load field (gán backing trực tiếp — KHÔNG chạy
    /// side-effect rebuild; root giữ cờ đang-rebuild quanh lời gọi như trước). Raise để UI cập nhật.
    /// </summary>
    internal void RestoreComboPropsFromJson(Dictionary<string, object?> raw)
    {
        _cbSearchMode          = ControlPropsJsonService.GetStr(raw, "searchMode",           "AutoFilter");
        _cbSearchFilterCondition = ControlPropsJsonService.GetStr(raw, "searchFilterCondition", "Contains");
        _cbAllowUserInput      = ControlPropsJsonService.GetBool(raw, "allowUserInput",      false);
        _cbNullTextKey         = ControlPropsJsonService.GetStr(raw, "nullTextKey",          "");
        _cbDropDownWidthMode   = ControlPropsJsonService.GetStr(raw, "dropDownWidthMode",    "ContentOrEditorWidth");
        _cbClearButton         = ControlPropsJsonService.GetStr(raw, "clearButton",          "Auto");
        _cbGroupFieldName      = ControlPropsJsonService.GetStr(raw, "groupFieldName",       "");
        _cbDisabledFieldName   = ControlPropsJsonService.GetStr(raw, "disabledFieldName",    "");
        RaiseComboProps();
    }

    /// <summary>
    /// Restore nhóm prop lưu Ui_Field_Lookup (EditBox/Tree/AddNew) từ record DB khi load field —
    /// gán backing trực tiếp, KHÔNG side-effect; root gọi trong lúc cờ đang-rebuild bật,
    /// raise sau qua <see cref="RaiseLookupDbProps"/> (giữ đúng thứ tự cũ ở root).
    /// </summary>
    internal void RestoreLookupDbConfig(FieldLookupConfigRecord cfg)
    {
        _editBoxMode         = cfg.EditBoxMode;
        _codeField           = cfg.CodeField ?? "";
        _dropDownWidth       = cfg.DropDownWidth;
        _dropDownHeight      = cfg.DropDownHeight;
        _reloadTriggerField  = cfg.ReloadTriggerField ?? "";
        _parentColumn        = cfg.ParentColumn ?? "";
        _treeSelectableLevel = string.IsNullOrWhiteSpace(cfg.TreeSelectableLevel) ? "all" : cfg.TreeSelectableLevel;
        _allowAddNew         = cfg.AllowAddNew;
        _addFormCode         = cfg.AddFormCode ?? "";
    }

    /// <summary>Raise các prop nhóm Ui_Field_Lookup sau restore (root gọi khi đã tắt cờ đang-rebuild).</summary>
    internal void RaiseLookupDbProps()
    {
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
    }

    /// <summary>Restore cờ Import_Global_Code (đọc riêng, phòng thủ theo Field_Id) — gán backing + raise.</summary>
    internal void RestoreImportGlobalCode(bool value)
    {
        _importGlobalCode = value;
        RaisePropertyChanged(nameof(ImportGlobalCode));
    }

    /// <summary>
    /// Reset nhóm Ui_Field_Lookup về mặc định — root gọi từ ClearFkLookupConfig (đổi editor type /
    /// field mới). Giữ hành vi cũ: gán backing, KHÔNG raise (root chỉ raise nhóm QueryMode/Fk*);
    /// KHÔNG reset ImportGlobalCode (root cũ cũng không).
    /// </summary>
    internal void ResetLookupDbState()
    {
        _editBoxMode         = "TextOnly";
        _codeField           = "";
        _dropDownWidth       = 600;
        _dropDownHeight      = 400;
        _reloadTriggerField  = "";
        _parentColumn        = "";
        _treeSelectableLevel = "all";
        _allowAddNew         = false;
        _addFormCode         = "";
    }

    /// <summary>Reset Cb* + Sys_Lookup về mặc định (field mới) — gán backing trực tiếp + raise.</summary>
    internal void ResetComboAndLookupState()
    {
        _lookupCode = "";
        LookupPreviewItems.Clear();
        _cbSearchMode = "AutoFilter"; _cbSearchFilterCondition = "Contains";
        _cbAllowUserInput = false; _cbNullTextKey = "";
        _cbDropDownWidthMode = "ContentOrEditorWidth"; _cbClearButton = "Auto";
        _cbGroupFieldName = ""; _cbDisabledFieldName = "";
        RaisePropertyChanged(nameof(LookupCode));
        RaiseComboProps();
    }

    private void RaiseComboProps()
    {
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

    // ═════════════════════════ B4.2 nhóm 3 — nguồn FK ═══════════════════════

    /// <summary>True khi đang có cấu hình FK Lookup (root dùng để confirm trước khi đổi type).</summary>
    internal bool HasFkSourceConfig =>
        !string.IsNullOrWhiteSpace(_fkTableName)
        || !string.IsNullOrWhiteSpace(_fkValueField)
        || !string.IsNullOrWhiteSpace(_fkFunctionName)
        || !string.IsNullOrWhiteSpace(_fkSelectSql)
        || FkPopupColumns.Count > 0;

    /// <summary>
    /// Reset nguồn FK về mặc định (root gọi từ ClearFkLookupConfig trong lúc cờ đang-rebuild bật) —
    /// gán backing trực tiếp + clear 5 collection, KHÔNG raise; root raise sau qua
    /// <see cref="RaiseFkSourceProps"/> (giữ đúng tập prop root cũ raise).
    /// </summary>
    internal void ResetFkSourceState()
    {
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
    }

    /// <summary>Raise sau reset nguồn FK — đúng tập prop mà root cũ raise trong ClearFkLookupConfig.</summary>
    internal void RaiseFkSourceProps()
    {
        RaisePropertyChanged(nameof(QueryMode));
        RaisePropertyChanged(nameof(IsTableMode));
        RaisePropertyChanged(nameof(IsFunctionMode));
        RaisePropertyChanged(nameof(IsSqlMode));
        RaisePropertyChanged(nameof(FkTableName));
        RaisePropertyChanged(nameof(FkValueField));
        RaisePropertyChanged(nameof(FkDisplayField));
    }

    // ── Command handlers (từ root, B4.2 nhóm 3) ──────────────────────────────

    /// <summary>Thêm 1 cột mới vào danh sách popup columns của LookupBox.</summary>
    private void ExecuteAddFkColumn()
    {
        var col = new FkColumnConfig { FieldName = "", CaptionKey = "", Width = 150 };
        WireFkColumnHandlers(col);
        FkPopupColumns.Add(col);
        _root.RebuildControlPropsJson();
    }

    /// <summary>
    /// Đăng ký handler PropertyChanged cho 1 FkColumnConfig (handler đặt tên
    /// <see cref="OnFkColumnPropertyChanged"/>) — root gọi lại khi restore từ DB.
    /// </summary>
    internal void WireFkColumnHandlers(FkColumnConfig col)
        => col.PropertyChanged += OnFkColumnPropertyChanged;

    /// <summary>
    /// Handler cột popup: FieldName đổi → tự sinh CaptionKey nếu key đang rỗng hoặc là auto-gen cũ;
    /// mọi thay đổi → rebuild ControlPropsJson + IsDirty.
    /// </summary>
    private void OnFkColumnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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

        _root.RebuildControlPropsJson();
    }

    /// <summary>
    /// Handler chung cho item filter-param / function-param / data-source-condition / cột popup
    /// ComboBox: mọi thay đổi → rebuild ControlPropsJson (thay lambda inline cũ ở root).
    /// </summary>
    private void OnFkItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => _root.RebuildControlPropsJson();

    /// <summary>Gắn handler rebuild-only cho item (root dùng khi restore cột popup ComboBox).</summary>
    internal void WireRebuildOnChange(System.ComponentModel.INotifyPropertyChanged item)
        => item.PropertyChanged += OnFkItemPropertyChanged;

    /// <summary>Sinh i18n key theo pattern: {table_lower}.col.{column_snake_case}.</summary>
    private static string GenerateCaptionKey(string? tableName, string? fieldName)
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
        _root.RebuildControlPropsJson();
    }

    /// <summary>Di chuyển cột popup lên 1 vị trí (giảm index).</summary>
    private void ExecuteMoveFkColumnUp(FkColumnConfig col)
    {
        var idx = FkPopupColumns.IndexOf(col);
        if (idx <= 0) return;
        FkPopupColumns.Move(idx, idx - 1);
        _root.RebuildControlPropsJson();
    }

    /// <summary>Di chuyển cột popup xuống 1 vị trí (tăng index).</summary>
    private void ExecuteMoveFkColumnDown(FkColumnConfig col)
    {
        var idx = FkPopupColumns.IndexOf(col);
        if (idx < 0 || idx >= FkPopupColumns.Count - 1) return;
        FkPopupColumns.Move(idx, idx + 1);
        _root.RebuildControlPropsJson();
    }

    /// <summary>Thêm 1 tham số động mới vào filterParams của LookupBox.</summary>
    private void ExecuteAddFkFilterParam()
    {
        var param = new FkFilterParam { Param = "", FieldRef = "", Type = "String" };
        param.PropertyChanged += OnFkItemPropertyChanged;
        FkFilterParams.Add(param);
        _root.RebuildControlPropsJson();
    }

    /// <summary>Xóa 1 tham số khỏi filterParams của LookupBox.</summary>
    private void ExecuteRemoveFkFilterParam(FkFilterParam param)
    {
        FkFilterParams.Remove(param);
        _root.RebuildControlPropsJson();
    }

    /// <summary>Thêm 1 tham số mới vào danh sách FunctionParams của TVF.</summary>
    private void ExecuteAddFunctionParam()
    {
        var p = new FunctionParam { Name = "", SourceType = "field", Type = "String" };
        p.PropertyChanged += OnFkItemPropertyChanged;
        FkFunctionParams.Add(p);
        _root.RebuildControlPropsJson();
    }

    /// <summary>Xóa 1 tham số khỏi FunctionParams.</summary>
    private void ExecuteRemoveFunctionParam(FunctionParam p)
    {
        FkFunctionParams.Remove(p);
        _root.RebuildControlPropsJson();
    }

    /// <summary>Thêm FieldCode vào danh sách reloadOnChange.</summary>
    private void ExecuteAddReloadField()
    {
        var code = ReloadOnChangeInput.Trim();
        if (string.IsNullOrEmpty(code) || ReloadOnChangeFields.Contains(code)) return;
        ReloadOnChangeFields.Add(code);
        ReloadOnChangeInput = "";
        _root.RebuildControlPropsJson();
        _root.RecomputeCascadeWarnings();
    }

    /// <summary>Xóa 1 FieldCode khỏi danh sách reloadOnChange.</summary>
    private void ExecuteRemoveReloadField(string fieldCode)
    {
        ReloadOnChangeFields.Remove(fieldCode);
        _root.RebuildControlPropsJson();
        _root.RecomputeCascadeWarnings();
    }

    /// <summary>Thêm 1 điều kiện đổi bảng nguồn mới (rỗng) vào DataSourceConditions.</summary>
    private void ExecuteAddDataSourceCondition()
    {
        var cond = new DataSourceCondition { WhenOp = "eq" };
        cond.PropertyChanged += OnFkItemPropertyChanged;
        DataSourceConditions.Add(cond);
        _root.RebuildControlPropsJson();
    }

    /// <summary>Xóa 1 điều kiện khỏi DataSourceConditions.</summary>
    private void ExecuteRemoveDataSourceCondition(DataSourceCondition cond)
    {
        DataSourceConditions.Remove(cond);
        _root.RebuildControlPropsJson();
    }
}
