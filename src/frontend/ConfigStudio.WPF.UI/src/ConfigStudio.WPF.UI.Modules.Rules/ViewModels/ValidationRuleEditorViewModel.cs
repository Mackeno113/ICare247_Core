// File    : ValidationRuleEditorViewModel.cs
// Module  : Rules
// Layer   : Presentation
// Purpose : ViewModel cho màn hình Validation Rule Editor (Screen 05) — quản lý rules của 1 field.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Rules.Models;
using Prism.Commands;
using Prism.Navigation.Regions;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.Rules.ViewModels;

/// <summary>
/// ViewModel cho màn hình Validation Rule Editor (Screen 05).
/// Hiển thị DataGrid rules, thêm/sửa/xóa, mở Expression Builder dialog.
/// </summary>
public sealed class ValidationRuleEditorViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;
    private readonly IDialogService _dialogService;

    // ── Navigation params ─────────────────────────────────────
    private int _fieldId;
    public int FieldId { get => _fieldId; set => SetProperty(ref _fieldId, value); }

    private int _formId;
    public int FormId { get => _formId; set => SetProperty(ref _formId, value); }

    private string _fieldCode = "";
    public string FieldCode { get => _fieldCode; set => SetProperty(ref _fieldCode, value); }

    // ── Data ──────────────────────────────────────────────────
    public ObservableCollection<RuleItemDto> Rules { get; } = [];

    private RuleItemDto? _selectedRule;
    public RuleItemDto? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (SetProperty(ref _selectedRule, value))
            {
                RaisePropertyChanged(nameof(IsRuleSelected));
                EditRuleCommand.RaiseCanExecuteChanged();
                DeleteRuleCommand.RaiseCanExecuteChanged();
                MoveUpCommand.RaiseCanExecuteChanged();
                MoveDownCommand.RaiseCanExecuteChanged();
                OpenExpressionBuilderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsRuleSelected => SelectedRule is not null;

    // ── Filter ────────────────────────────────────────────────
    public List<string> RuleTypeOptions { get; } = ["Tất cả", "Required", "Numeric", "Range", "Regex", "Custom"];

    private string _ruleTypeFilter = "Tất cả";
    public string RuleTypeFilter
    {
        get => _ruleTypeFilter;
        set => SetProperty(ref _ruleTypeFilter, value);
    }

    public List<string> SeverityOptions { get; } = ["Error", "Warning", "Info"];

    // ── State ─────────────────────────────────────────────────
    private bool _isDirty;
    public bool IsDirty { get => _isDirty; set => SetProperty(ref _isDirty, value); }

    // ── Edit panel (inline) ──────────────────────────────────
    private bool _isEditPanelOpen;
    public bool IsEditPanelOpen { get => _isEditPanelOpen; set => SetProperty(ref _isEditPanelOpen, value); }

    private string _editRuleType = "Custom";
    public string EditRuleType { get => _editRuleType; set => SetProperty(ref _editRuleType, value); }

    private string _editErrorKey = "";
    public string EditErrorKey { get => _editErrorKey; set => SetProperty(ref _editErrorKey, value); }

    private string _editSeverity = "Error";
    public string EditSeverity { get => _editSeverity; set => SetProperty(ref _editSeverity, value); }

    private string _editExpressionPreview = "";
    public string EditExpressionPreview { get => _editExpressionPreview; set => SetProperty(ref _editExpressionPreview, value); }

    private string _editMode = "new";

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand AddRuleCommand { get; }
    public DelegateCommand EditRuleCommand { get; }
    public DelegateCommand DeleteRuleCommand { get; }
    public DelegateCommand MoveUpCommand { get; }
    public DelegateCommand MoveDownCommand { get; }
    public DelegateCommand OpenExpressionBuilderCommand { get; }
    public DelegateCommand SaveRuleCommand { get; }
    public DelegateCommand CancelEditCommand { get; }
    public DelegateCommand SaveAllCommand { get; }
    public DelegateCommand BackCommand { get; }

    public ValidationRuleEditorViewModel(IRegionManager regionManager, IDialogService dialogService)
    {
        _regionManager = regionManager;
        _dialogService = dialogService;

        AddRuleCommand = new DelegateCommand(ExecuteAddRule);
        EditRuleCommand = new DelegateCommand(ExecuteEditRule, () => IsRuleSelected);
        DeleteRuleCommand = new DelegateCommand(ExecuteDeleteRule, () => IsRuleSelected);
        MoveUpCommand = new DelegateCommand(ExecuteMoveUp, () => IsRuleSelected);
        MoveDownCommand = new DelegateCommand(ExecuteMoveDown, () => IsRuleSelected);
        OpenExpressionBuilderCommand = new DelegateCommand(ExecuteOpenExpressionBuilder, () => IsRuleSelected);
        SaveRuleCommand = new DelegateCommand(ExecuteSaveRule);
        CancelEditCommand = new DelegateCommand(() => IsEditPanelOpen = false);
        SaveAllCommand = new DelegateCommand(ExecuteSaveAll, () => IsDirty)
            .ObservesProperty(() => IsDirty);
        BackCommand = new DelegateCommand(ExecuteBack);
    }

    // ── INavigationAware ─────────────────────────────────────

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        FieldId = navigationContext.Parameters.GetValue<int>("fieldId");
        FormId = navigationContext.Parameters.GetValue<int>("formId");

        if (FieldId == 0) FieldId = 5;
        if (FormId == 0) FormId = 1;

        LoadMockData();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => false;

    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    // ── Load mock data ───────────────────────────────────────

    /// <summary>
    /// Load mock data cho demo. Sau này sẽ gọi API load rules theo fieldId.
    /// </summary>
    private void LoadMockData()
    {
        FieldCode = "SoLuong";

        Rules.Clear();
        Rules.Add(new RuleItemDto
        {
            RuleId = 1, FieldId = FieldId, FieldCode = FieldCode,
            RuleTypeCode = "Required", OrderNo = 1,
            ExpressionPreview = "(built-in)", ExpressionJson = "{}",
            ErrorKey = "err.fld.req", Severity = "Error", IsActive = true
        });
        Rules.Add(new RuleItemDto
        {
            RuleId = 2, FieldId = FieldId, FieldCode = FieldCode,
            RuleTypeCode = "Range", OrderNo = 2,
            ExpressionPreview = "SoLuong >= 1 && SoLuong <= 9999",
            ExpressionJson = "{\"type\":\"Binary\",\"op\":\"&&\"}",
            ErrorKey = "err.sl.range", Severity = "Error", IsActive = true
        });
        Rules.Add(new RuleItemDto
        {
            RuleId = 3, FieldId = FieldId, FieldCode = FieldCode,
            RuleTypeCode = "Custom", OrderNo = 3,
            ExpressionPreview = "SoLuong <= DonGia * 100",
            ExpressionJson = "{\"type\":\"Binary\",\"op\":\"<=\"}",
            ErrorKey = "err.sl.exceed", Severity = "Warning", IsActive = true
        });

        IsDirty = false;
    }

    // ── Command handlers ─────────────────────────────────────

    private void ExecuteAddRule()
    {
        _editMode = "new";
        EditRuleType = "Custom";
        EditErrorKey = "";
        EditSeverity = "Error";
        EditExpressionPreview = "";
        IsEditPanelOpen = true;
    }

    private void ExecuteEditRule()
    {
        if (SelectedRule is null) return;
        _editMode = "edit";
        EditRuleType = SelectedRule.RuleTypeCode;
        EditErrorKey = SelectedRule.ErrorKey;
        EditSeverity = SelectedRule.Severity;
        EditExpressionPreview = SelectedRule.ExpressionPreview;
        IsEditPanelOpen = true;
    }

    private void ExecuteDeleteRule()
    {
        if (SelectedRule is null) return;
        Rules.Remove(SelectedRule);
        ReindexOrders();
        SelectedRule = null;
        IsDirty = true;
    }

    private void ExecuteMoveUp()
    {
        if (SelectedRule is null) return;
        int idx = Rules.IndexOf(SelectedRule);
        if (idx > 0)
        {
            Rules.Move(idx, idx - 1);
            ReindexOrders();
            IsDirty = true;
        }
    }

    private void ExecuteMoveDown()
    {
        if (SelectedRule is null) return;
        int idx = Rules.IndexOf(SelectedRule);
        if (idx < Rules.Count - 1)
        {
            Rules.Move(idx, idx + 1);
            ReindexOrders();
            IsDirty = true;
        }
    }

    private void ExecuteOpenExpressionBuilder()
    {
        if (SelectedRule is null) return;

        // NOTE: Mở ExpressionBuilder dialog, truyền expression JSON hiện tại
        var p = new DialogParameters
        {
            { "expressionJson", SelectedRule.ExpressionJson },
            { "fieldCode", FieldCode }
        };

        _dialogService.ShowDialog(ViewNames.ExpressionBuilderDialog, p, result =>
        {
            if (result.Result == ButtonResult.OK)
            {
                // NOTE: Nhận expression JSON mới từ dialog
                var newJson = result.Parameters.GetValue<string>("expressionJson");
                if (!string.IsNullOrEmpty(newJson))
                {
                    SelectedRule.ExpressionJson = newJson;
                    SelectedRule.ExpressionPreview = result.Parameters.GetValue<string>("naturalText") ?? newJson;
                    IsDirty = true;
                }
            }
        });
    }

    private void ExecuteSaveRule()
    {
        if (_editMode == "new")
        {
            var newId = Rules.Count > 0 ? Rules.Max(r => r.RuleId) + 1 : 1;
            Rules.Add(new RuleItemDto
            {
                RuleId = newId, FieldId = FieldId, FieldCode = FieldCode,
                RuleTypeCode = EditRuleType, OrderNo = Rules.Count + 1,
                ExpressionPreview = EditExpressionPreview,
                ExpressionJson = "{}",
                ErrorKey = EditErrorKey, Severity = EditSeverity,
                IsActive = true
            });
        }
        else if (SelectedRule is not null)
        {
            SelectedRule.RuleTypeCode = EditRuleType;
            SelectedRule.ErrorKey = EditErrorKey;
            SelectedRule.Severity = EditSeverity;
        }

        IsEditPanelOpen = false;
        IsDirty = true;
    }

    private void ExecuteSaveAll()
    {
        // TODO(phase2): Gọi API save tất cả rules
        IsDirty = false;
    }

    private void ExecuteBack()
    {
        // TODO(phase2): Confirm nếu IsDirty
        var p = new NavigationParameters
        {
            { "fieldId", FieldId },
            { "formId", FormId },
            { "mode", "edit" }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FieldConfig, p);
    }

    // ── Helpers ──────────────────────────────────────────────

    private void ReindexOrders()
    {
        for (int i = 0; i < Rules.Count; i++)
            Rules[i].OrderNo = i + 1;
    }
}
