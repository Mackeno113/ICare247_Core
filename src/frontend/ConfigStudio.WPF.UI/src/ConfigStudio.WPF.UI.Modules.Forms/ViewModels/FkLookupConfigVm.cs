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
    private readonly IAppLogger? _logger;
    private readonly Func<CancellationToken> _token;

    public FkLookupConfigVm(
        FieldConfigViewModel root,
        ISysLookupDataService? lookupService,
        IAppLogger? logger,
        Func<CancellationToken> token)
    {
        _root = root;
        _lookupService = lookupService;
        _logger = logger;
        _token = token;
        // Bridge notify: prop ủy quyền TRÙNG TÊN với root → re-raise nguyên PropertyName.
        // Tên không thuộc VM này (binding không tồn tại) chỉ tốn 1 lần so tên — vô hại.
        _root.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
    }

    // ── Cờ editor type (root suy từ SelectedEditorType) ──────────────────────
    public bool IsLookupEditor => _root.IsLookupEditor;
    public bool IsLookupOrComboBoxEditor => _root.IsLookupOrComboBoxEditor;
    public bool IsFkLookupEditor => _root.IsFkLookupEditor;
    public bool IsTreeLookupEditor => _root.IsTreeLookupEditor;
    public bool IsComboBoxEditor => _root.IsComboBoxEditor;

    // ── Chế độ truy vấn ──────────────────────────────────────────────────────
    public bool IsTableMode => _root.IsTableMode;
    public bool IsFunctionMode => _root.IsFunctionMode;
    public bool IsSqlMode => _root.IsSqlMode;
    public DelegateCommand<string> SetQueryModeCommand => _root.SetQueryModeCommand;

    // ── Nguồn dữ liệu FK ─────────────────────────────────────────────────────
    public string FkTableName    { get => _root.FkTableName;    set => _root.FkTableName = value; }
    public string FkFunctionName { get => _root.FkFunctionName; set => _root.FkFunctionName = value; }
    public string FkSelectSql    { get => _root.FkSelectSql;    set => _root.FkSelectSql = value; }
    public string FkValueField   { get => _root.FkValueField;   set => _root.FkValueField = value; }
    public string FkDisplayField { get => _root.FkDisplayField; set => _root.FkDisplayField = value; }
    public string FkFilterSql    { get => _root.FkFilterSql;    set => _root.FkFilterSql = value; }
    public string FkOrderBy      { get => _root.FkOrderBy;      set => _root.FkOrderBy = value; }
    public bool FkSearchEnabled  { get => _root.FkSearchEnabled; set => _root.FkSearchEnabled = value; }

    public ObservableCollection<FkColumnConfig> FkPopupColumns => _root.FkPopupColumns;
    public ObservableCollection<FunctionParam> FkFunctionParams => _root.FkFunctionParams;

    public DelegateCommand AddFkColumnCommand => _root.AddFkColumnCommand;
    public DelegateCommand<FkColumnConfig> RemoveFkColumnCommand => _root.RemoveFkColumnCommand;
    public DelegateCommand<FkColumnConfig> MoveFkColumnUpCommand => _root.MoveFkColumnUpCommand;
    public DelegateCommand<FkColumnConfig> MoveFkColumnDownCommand => _root.MoveFkColumnDownCommand;
    public DelegateCommand AddFunctionParamCommand => _root.AddFunctionParamCommand;
    public DelegateCommand<FunctionParam> RemoveFunctionParamCommand => _root.RemoveFunctionParamCommand;

    // ── Danh sách option tĩnh ────────────────────────────────────────────────
    public List<string> FunctionParamSourceTypes => _root.FunctionParamSourceTypes;
    public List<string> SystemKeyOptions => _root.SystemKeyOptions;
    public List<string> FkParamTypes => _root.FkParamTypes;
    public List<string> WhenOpOptions => _root.WhenOpOptions;
    public List<string> EditBoxModeOptions => _root.EditBoxModeOptions;
    public List<string> TreeSelectableLevelOptions => _root.TreeSelectableLevelOptions;
    // 4 option list ComboBox — SỞ HỮU tại đây từ B4.2 (root đã xóa).
    public List<string> SearchModeOptions { get; } = ["None", "AutoFilter", "AutoSearch"];
    public List<string> SearchFilterConditionOptions { get; } = ["Contains", "StartsWith", "Equals"];
    public List<string> DropDownWidthModeOptions { get; } = ["ContentOrEditorWidth", "ContentWidth", "EditorWidth"];
    public List<string> ClearButtonModeOptions { get; } = ["Hidden", "Auto"];

    // ── EditBox hiển thị ─────────────────────────────────────────────────────
    public string EditBoxMode { get => _root.EditBoxMode; set => _root.EditBoxMode = value; }
    public bool IsCodeAndNameMode => _root.IsCodeAndNameMode;
    public string CodeField { get => _root.CodeField; set => _root.CodeField = value; }
    public bool ImportGlobalCode { get => _root.ImportGlobalCode; set => _root.ImportGlobalCode = value; }
    public int DropDownWidth { get => _root.DropDownWidth; set => _root.DropDownWidth = value; }
    public int DropDownHeight { get => _root.DropDownHeight; set => _root.DropDownHeight = value; }
    public string ReloadTriggerField { get => _root.ReloadTriggerField; set => _root.ReloadTriggerField = value; }

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

    // ── Thêm mới entity từ LookupBox ─────────────────────────────────────────
    public bool AllowAddNew { get => _root.AllowAddNew; set => _root.AllowAddNew = value; }
    public string AddFormCode { get => _root.AddFormCode; set => _root.AddFormCode = value; }
    public ObservableCollection<string> AvailableFormCodes => _root.AvailableFormCodes;

    // ── TreeLookupBox ────────────────────────────────────────────────────────
    public string ParentColumn { get => _root.ParentColumn; set => _root.ParentColumn = value; }
    public string TreeSelectableLevel { get => _root.TreeSelectableLevel; set => _root.TreeSelectableLevel = value; }

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
}
