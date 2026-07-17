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
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>VM con vùng FK Lookup/ComboBox — root expose qua property <c>FkLookup</c>.</summary>
public sealed class FkLookupConfigVm : BindableBase
{
    private readonly FieldConfigViewModel _root;

    public FkLookupConfigVm(FieldConfigViewModel root)
    {
        _root = root;
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
    public List<string> SearchModeOptions => _root.SearchModeOptions;
    public List<string> SearchFilterConditionOptions => _root.SearchFilterConditionOptions;
    public List<string> DropDownWidthModeOptions => _root.DropDownWidthModeOptions;
    public List<string> ClearButtonModeOptions => _root.ClearButtonModeOptions;

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

    // ── ComboBox / LookupComboBox (Cb*) ──────────────────────────────────────
    public string CbSearchMode { get => _root.CbSearchMode; set => _root.CbSearchMode = value; }
    public string CbSearchFilterCondition { get => _root.CbSearchFilterCondition; set => _root.CbSearchFilterCondition = value; }
    public bool ShowSearchFilterCondition => _root.ShowSearchFilterCondition;
    public bool CbAllowUserInput { get => _root.CbAllowUserInput; set => _root.CbAllowUserInput = value; }
    public string CbNullTextKey { get => _root.CbNullTextKey; set => _root.CbNullTextKey = value; }
    public string CbDropDownWidthMode { get => _root.CbDropDownWidthMode; set => _root.CbDropDownWidthMode = value; }
    public string CbClearButton { get => _root.CbClearButton; set => _root.CbClearButton = value; }
    public string CbGroupFieldName { get => _root.CbGroupFieldName; set => _root.CbGroupFieldName = value; }
    public string CbDisabledFieldName { get => _root.CbDisabledFieldName; set => _root.CbDisabledFieldName = value; }

    // ── Sys_Lookup tĩnh (LookupComboBox) ─────────────────────────────────────
    public string LookupCode { get => _root.LookupCode; set => _root.LookupCode = value; }
    public ObservableCollection<string> AvailableLookupCodes => _root.AvailableLookupCodes;
    public ObservableCollection<LookupItemDto> LookupPreviewItems => _root.LookupPreviewItems;
}
