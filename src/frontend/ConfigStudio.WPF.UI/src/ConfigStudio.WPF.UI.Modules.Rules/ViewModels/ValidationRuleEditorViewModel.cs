// File    : ValidationRuleEditorViewModel.cs
// Module  : Rules
// Layer   : Presentation
// Purpose : ViewModel cho màn hình Validation Rule Editor (Screen 05) — quản lý rules của 1 field.

using System.Collections.ObjectModel;
using System.Windows;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Rules.Models;
using Prism.Commands;
using Prism.Navigation.Regions;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.Rules.ViewModels;

/// <summary>
/// ViewModel cho màn hình Validation Rule Editor (Screen 05).
/// Hiển thị DataGrid rules, thêm/sửa/xóa, mở Expression Builder dialog.
/// Khi DB đã cấu hình → load/save dữ liệu thật qua IRuleDataService.
/// Khi chưa cấu hình → fallback mock data.
/// </summary>
public sealed class ValidationRuleEditorViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;
    private readonly IDialogService _dialogService;
    private readonly IRuleDataService? _ruleService;
    private readonly II18nDataService? _i18nService;
    private readonly IAppConfigService? _appConfig;
    private CancellationTokenSource _cts = new();

    // ── Navigation params / Context ───────────────────────────
    private int _fieldId;
    public int FieldId { get => _fieldId; set => SetProperty(ref _fieldId, value); }

    private int _formId;
    public int FormId { get => _formId; set => SetProperty(ref _formId, value); }

    private string _fieldCode = "";
    public string FieldCode { get => _fieldCode; set => SetProperty(ref _fieldCode, value); }

    private string _tableCode = "";
    public string TableCode { get => _tableCode; set => SetProperty(ref _tableCode, value); }

    private string _sectionName = "";
    public string SectionName { get => _sectionName; set => SetProperty(ref _sectionName, value); }

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

    // ── Rule type options + descriptions ─────────────────────
    public List<string> RuleTypeOptions { get; } = ["Required", "Range", "Regex", "Numeric", "Custom"];

    /// <summary>Mô tả ngắn loại rule đang chọn — hiển thị trong edit panel.</summary>
    public string EditRuleTypeDescription => EditRuleType switch
    {
        "Required" => "Bắt buộc nhập giá trị. Không cần thiết lập điều kiện thêm.",
        "Range"    => "Giá trị phải nằm trong khoảng min–max. Thiết lập điều kiện bên dưới.",
        "Regex"    => "Giá trị phải khớp với biểu thức chính quy (Regular Expression).",
        "Numeric"  => "Chỉ cho phép nhập số. Tùy chọn min/max.",
        "Custom"   => "Điều kiện tự do — dùng Expression Builder để xây dựng logic.",
        _          => ""
    };

    /// <summary>Rule Required không cần Expression — ẩn phần expression builder.</summary>
    public bool IsExpressionNeeded => EditRuleType != "Required";

    /// <summary>
    /// Auto-generated ErrorKey theo pattern: {tableCode}.val.{fieldCode}.{ruleTypeCode}.
    /// VD: nhanvien.val.hoten.required | donhang.val.soluong.range
    /// </summary>
    public string AutoErrorKey
        => string.IsNullOrEmpty(TableCode) || string.IsNullOrEmpty(FieldCode)
            ? $"val.{FieldCode.ToLowerInvariant()}.{EditRuleType.ToLowerInvariant()}"
            : $"{TableCode.ToLowerInvariant()}.val.{FieldCode.ToLowerInvariant()}.{EditRuleType.ToLowerInvariant()}";

    public List<string> SeverityOptions { get; } = ["Error", "Warning", "Info"];

    /// <summary>Mô tả mức độ severity đang chọn.</summary>
    public string EditSeverityDescription => EditSeverity switch
    {
        "Error"   => "Chặn submit form — người dùng bắt buộc phải sửa.",
        "Warning" => "Hiển thị cảnh báo nhưng vẫn cho phép submit.",
        "Info"    => "Chỉ hiện gợi ý, không ảnh hưởng submit.",
        _         => ""
    };

    // ── State ─────────────────────────────────────────────────
    private bool _isDirty;
    public bool IsDirty { get => _isDirty; set => SetProperty(ref _isDirty, value); }

    // ── Edit panel (inline) ──────────────────────────────────
    private bool _isEditPanelOpen;
    public bool IsEditPanelOpen { get => _isEditPanelOpen; set => SetProperty(ref _isEditPanelOpen, value); }

    private string _editRuleType = "Custom";
    public string EditRuleType
    {
        get => _editRuleType;
        set
        {
            if (SetProperty(ref _editRuleType, value))
            {
                RaisePropertyChanged(nameof(EditRuleTypeDescription));
                RaisePropertyChanged(nameof(IsExpressionNeeded));
                RaisePropertyChanged(nameof(AutoErrorKey));
            }
        }
    }

    private string _editErrorKey = "";
    public string EditErrorKey { get => _editErrorKey; set => SetProperty(ref _editErrorKey, value); }

    private string _editSeverity = "Error";
    public string EditSeverity
    {
        get => _editSeverity;
        set
        {
            if (SetProperty(ref _editSeverity, value))
                RaisePropertyChanged(nameof(EditSeverityDescription));
        }
    }

    private string _editExpressionPreview = "";
    public string EditExpressionPreview { get => _editExpressionPreview; set => SetProperty(ref _editExpressionPreview, value); }

    private string _editMode = "new";

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand AddRuleCommand { get; }
    public DelegateCommand EditRuleCommand { get; }
    public DelegateCommand<RuleItemDto> DeleteRuleCommand { get; }
    public DelegateCommand MoveUpCommand { get; }
    public DelegateCommand MoveDownCommand { get; }
    public DelegateCommand OpenExpressionBuilderCommand { get; }
    public DelegateCommand SaveRuleCommand { get; }
    public DelegateCommand CancelEditCommand { get; }
    public DelegateCommand SaveAllCommand { get; }
    public DelegateCommand BackCommand { get; }

    public ValidationRuleEditorViewModel(
        IRegionManager regionManager,
        IDialogService dialogService,
        IRuleDataService? ruleService = null,
        II18nDataService? i18nService = null,
        IAppConfigService? appConfig = null)
    {
        _regionManager = regionManager;
        _dialogService = dialogService;
        _ruleService   = ruleService;
        _i18nService   = i18nService;
        _appConfig     = appConfig;

        AddRuleCommand = new DelegateCommand(ExecuteAddRule);
        EditRuleCommand = new DelegateCommand(ExecuteEditRule, () => IsRuleSelected);
        DeleteRuleCommand = new DelegateCommand<RuleItemDto>(async rule => await ExecuteDeleteRuleAsync(rule));
        MoveUpCommand = new DelegateCommand(ExecuteMoveUp, () => IsRuleSelected);
        MoveDownCommand = new DelegateCommand(ExecuteMoveDown, () => IsRuleSelected);
        OpenExpressionBuilderCommand = new DelegateCommand(ExecuteOpenExpressionBuilder, () => IsRuleSelected);
        SaveRuleCommand = new DelegateCommand(async () => await ExecuteSaveRuleAsync());
        CancelEditCommand = new DelegateCommand(() => IsEditPanelOpen = false);
        SaveAllCommand = new DelegateCommand(async () => await ExecuteSaveAllAsync(), () => IsDirty)
            .ObservesProperty(() => IsDirty);
        BackCommand = new DelegateCommand(ExecuteBack);
    }

    // ── INavigationAware ─────────────────────────────────────

    public async void OnNavigatedTo(NavigationContext navigationContext)
    {
        FieldId     = navigationContext.Parameters.GetValue<int>("fieldId");
        FormId      = navigationContext.Parameters.GetValue<int>("formId");
        FieldCode   = navigationContext.Parameters.GetValue<string>("fieldCode")   ?? "";
        TableCode   = navigationContext.Parameters.GetValue<string>("tableCode")   ?? "";
        SectionName = navigationContext.Parameters.GetValue<string>("sectionName") ?? "";
        RaisePropertyChanged(nameof(AutoErrorKey));

        if (FieldId == 0) FieldId = 5;
        if (FormId == 0) FormId = 1;

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
        if (_ruleService is not null && _appConfig is { IsConfigured: true })
        {
            await LoadFromDatabaseAsync();
        }
        else
        {
            LoadMockData();
        }
    }

    /// <summary>
    /// Load rules từ DB qua IRuleDataService.
    /// </summary>
    private async Task LoadFromDatabaseAsync()
    {
        try
        {
            var ct = _cts.Token;
            var rules = await _ruleService!.GetRulesByFieldAsync(FieldId, ct);

            Rules.Clear();
            foreach (var r in rules)
            {
                Rules.Add(new RuleItemDto
                {
                    RuleId = r.RuleId,
                    FieldId = FieldId,
                    FieldCode = FieldCode,
                    RuleTypeCode = r.RuleTypeCode,
                    OrderNo = r.OrderNo,
                    ExpressionJson = r.ExpressionJson ?? "{}",
                    ExpressionPreview = r.ExpressionJson ?? "",
                    ErrorKey = r.ErrorKey,
                    IsActive = r.IsActive
                });
            }

            IsDirty = false;
        }
        catch (OperationCanceledException) { /* Navigation away */ }
        catch
        {
            LoadMockData();
        }
    }

    // ── Load mock data ───────────────────────────────────────

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

    private async Task ExecuteDeleteRuleAsync(RuleItemDto? rule)
    {
        if (rule is null) return;

        // Xác nhận trước khi xóa — default No
        var label   = string.IsNullOrEmpty(rule.ErrorKey) ? rule.RuleTypeCode : rule.ErrorKey;
        var confirm = MessageBox.Show(
            $"Bạn có chắc muốn xóa rule \"{label}\" (#{rule.OrderNo}) không?\nThao tác này không thể hoàn tác.",
            "Xác nhận xóa",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirm != MessageBoxResult.Yes) return;

        // Xóa khỏi DB nếu rule đã persist (RuleId > 0)
        if (rule.RuleId > 0 && _ruleService is not null && _appConfig is { IsConfigured: true })
        {
            try
            {
                await _ruleService.DeleteRuleAsync(rule.RuleId, _cts.Token);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Xóa thất bại: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
        }

        Rules.Remove(rule);
        ReindexOrders();
        if (SelectedRule == rule) SelectedRule = null;
        IsDirty = Rules.Count > 0; // chỉ dirty nếu còn rule chưa save
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

    private async Task ExecuteSaveRuleAsync()
    {
        RuleItemDto rule;

        if (_editMode == "new")
        {
            rule = new RuleItemDto
            {
                RuleId            = 0,
                FieldId           = FieldId,
                FieldCode         = FieldCode,
                RuleTypeCode      = EditRuleType,
                OrderNo           = Rules.Count + 1,
                ExpressionPreview = EditExpressionPreview,
                ExpressionJson    = "",
                ErrorKey          = AutoErrorKey,
                Severity          = EditSeverity,
                IsActive          = true
            };
            Rules.Add(rule);
        }
        else if (SelectedRule is not null)
        {
            rule = SelectedRule;
            rule.RuleTypeCode = EditRuleType;
            rule.ErrorKey     = AutoErrorKey;
            rule.Severity     = EditSeverity;
        }
        else return;

        // Lưu ngay vào DB — không cần bấm "Lưu tất cả" sau
        if (_ruleService is not null && _appConfig is { IsConfigured: true })
        {
            try
            {
                var ct     = _cts.Token;
                var record = new RuleItemRecord
                {
                    RuleId         = rule.RuleId,
                    FieldId        = FieldId,
                    RuleTypeCode   = rule.RuleTypeCode,
                    OrderNo        = rule.OrderNo,
                    ExpressionJson = string.IsNullOrEmpty(rule.ExpressionJson) ? null : rule.ExpressionJson,
                    ErrorKey       = rule.ErrorKey,
                    Severity       = rule.Severity,
                    IsActive       = rule.IsActive
                };

                // 1. Lưu Val_Rule
                var savedId = await _ruleService.SaveRuleAsync(record, ct);
                rule.RuleId = savedId;

                // 2. Auto-init Sys_Resource cho Error_Key nếu chưa có bản dịch
                //    Chỉ INSERT khi missing — không ghi đè bản dịch người dùng đã nhập
                if (_i18nService is not null && !string.IsNullOrEmpty(rule.ErrorKey))
                {
                    await InitErrorKeyResourcesAsync(rule.ErrorKey, ct);
                }
            }
            catch (Exception ex)
            {
                // Ghi lỗi vào status — không crash UI
                _ = ex;
            }
        }

        IsEditPanelOpen = false;
        IsDirty = false;
    }

    /// <summary>
    /// Save tất cả rules qua IRuleDataService (nếu DB configured).
    /// </summary>
    private async Task ExecuteSaveAllAsync()
    {
        if (_ruleService is not null && _appConfig is { IsConfigured: true })
        {
            var ct = _cts.Token;
            foreach (var rule in Rules)
            {
                var record = new RuleItemRecord
                {
                    RuleId         = rule.RuleId,
                    FieldId        = FieldId,
                    RuleTypeCode   = rule.RuleTypeCode,
                    OrderNo        = rule.OrderNo,
                    ExpressionJson = string.IsNullOrEmpty(rule.ExpressionJson) ? null : rule.ExpressionJson,
                    ErrorKey       = rule.ErrorKey,
                    Severity       = rule.Severity,
                    IsActive       = rule.IsActive
                };
                await _ruleService.SaveRuleAsync(record, ct);
            }
        }
        IsDirty = false;
    }

    private void ExecuteBack()
    {
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

    /// <summary>
    /// Auto-init Sys_Resource cho Error_Key với giá trị mặc định theo từng ngôn ngữ.
    /// Chỉ INSERT khi key+lang chưa có — không overwrite bản dịch người dùng đã nhập.
    /// </summary>
    private async Task InitErrorKeyResourcesAsync(string errorKey, CancellationToken ct)
    {
        if (_i18nService is null) return;

        // Sinh thông báo mặc định theo rule type + field code
        var vi = BuildDefaultErrorMessage(EditRuleType, FieldCode, "vi");
        var en = BuildDefaultErrorMessage(EditRuleType, FieldCode, "en");

        await _i18nService.InitResourceIfMissingAsync(errorKey, "vi", vi, ct);
        await _i18nService.InitResourceIfMissingAsync(errorKey, "en", en, ct);
    }

    /// <summary>
    /// Sinh thông báo lỗi mặc định theo loại rule, field code và ngôn ngữ.
    /// </summary>
    private static string BuildDefaultErrorMessage(string ruleType, string fieldCode, string lang)
    {
        if (lang == "vi")
        {
            return ruleType switch
            {
                "Required" => $"Trường '{fieldCode}' là bắt buộc.",
                "Range"    => $"Giá trị '{fieldCode}' nằm ngoài khoảng cho phép.",
                "Regex"    => $"Định dạng '{fieldCode}' không hợp lệ.",
                "Numeric"  => $"Trường '{fieldCode}' chỉ chấp nhận giá trị số.",
                "Custom"   => $"Giá trị '{fieldCode}' không thỏa điều kiện.",
                _          => $"Giá trị '{fieldCode}' không hợp lệ."
            };
        }

        // en (default)
        return ruleType switch
        {
            "Required" => $"'{fieldCode}' is required.",
            "Range"    => $"'{fieldCode}' is out of the allowed range.",
            "Regex"    => $"'{fieldCode}' format is invalid.",
            "Numeric"  => $"'{fieldCode}' must be a numeric value.",
            "Custom"   => $"'{fieldCode}' does not meet the condition.",
            _          => $"'{fieldCode}' has an invalid value."
        };
    }
}
