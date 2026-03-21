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
/// Khi chưa cấu hình → fallback mock data.
/// </summary>
public sealed class FieldConfigViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;
    private readonly IFieldDataService? _fieldService;
    private readonly II18nDataService? _i18nService;
    private readonly IRuleDataService? _ruleService;
    private readonly IEventDataService? _eventService;
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
        "LookupBox", "TextArea", "CheckBox", "ToggleSwitch"
    ];

    private string _selectedEditorType = "TextBox";
    public string SelectedEditorType
    {
        get => _selectedEditorType;
        set
        {
            if (SetProperty(ref _selectedEditorType, value))
            {
                LoadControlPropSchema();
                IsDirty = true;
            }
        }
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
                _ = ResolveI18nPreviewAsync(value, v => LabelPreview = v);
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
        set
        {
            if (SetProperty(ref _isRequired, value))
            {
                // NOTE: Toggle tự động tạo/xóa Required rule trong LinkedRules (chưa save DB)
                ToggleRequiredRule(value);
                IsDirty = true;
            }
        }
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

    public FieldConfigViewModel(
        IRegionManager regionManager,
        IFieldDataService? fieldService = null,
        II18nDataService? i18nService = null,
        IRuleDataService? ruleService = null,
        IEventDataService? eventService = null,
        IAppConfigService? appConfig = null)
    {
        _regionManager = regionManager;
        _fieldService = fieldService;
        _i18nService = i18nService;
        _ruleService = ruleService;
        _eventService = eventService;
        _appConfig = appConfig;

        SaveFieldCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), () => IsDirty)
            .ObservesProperty(() => IsDirty);
        CancelCommand = new DelegateCommand(ExecuteCancel);
        BrowseColumnCommand = new DelegateCommand(ExecuteBrowseColumn);
        ManageI18nCommand = new DelegateCommand(ExecuteManageI18n);
        AddRuleCommand = new DelegateCommand(ExecuteAddRule);
        OpenRuleCommand = new DelegateCommand<RuleSummaryDto>(ExecuteOpenRule);
        DeleteRuleCommand = new DelegateCommand<RuleSummaryDto>(ExecuteDeleteRule);
        AddEventCommand = new DelegateCommand(ExecuteAddEvent);
        OpenEventCommand = new DelegateCommand<EventSummaryDto>(ExecuteOpenEvent);
    }

    // ── INavigationAware ─────────────────────────────────────

    public async void OnNavigatedTo(NavigationContext navigationContext)
    {
        _mode = navigationContext.Parameters.GetValue<string>("mode") ?? "edit";
        FormId = navigationContext.Parameters.GetValue<int>("formId");
        SectionId = navigationContext.Parameters.GetValue<int>("sectionId");
        FieldId = navigationContext.Parameters.GetValue<int>("fieldId");

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
            LoadMockData();
        }
    }

    /// <summary>
    /// Load field detail, columns, linked rules/events từ DB.
    /// </summary>
    private async Task LoadFromDatabaseAsync()
    {
        IsLoading = true;
        try
        {
            var ct = _cts.Token;
            var tenantId = _appConfig!.TenantId;

            // 1. Load columns cho ComboBox chọn column
            var tableId = await _fieldService!.GetTableIdByFormAsync(FormId, tenantId, ct);
            if (tableId > 0)
            {
                var columns = await _fieldService.GetColumnsByTableAsync(tableId, ct);
                AvailableColumns.Clear();
                foreach (var c in columns)
                {
                    AvailableColumns.Add(new ColumnInfoDto
                    {
                        ColumnId = c.ColumnId,
                        ColumnCode = c.ColumnCode,
                        DataType = c.DataType,
                        NetType = c.NetType,
                        IsNullable = c.IsNullable
                    });
                }
            }

            if (_mode == "new")
            {
                SectionName = "";
                SelectedEditorType = "TextBox";
                IsLoading = false;
                IsDirty = false;
                return;
            }

            // 2. Load field detail
            if (FieldId > 0)
            {
                var field = await _fieldService.GetFieldDetailAsync(FieldId, tenantId, ct);
                if (field is not null)
                {
                    FormId = field.FormId;
                    SectionId = field.SectionId ?? 0;
                    SectionName = field.SectionCode;
                    SelectedColumn = AvailableColumns.FirstOrDefault(c => c.ColumnId == field.ColumnId);
                    ColumnCode = field.ColumnCode;
                    SelectedEditorType = field.EditorType;
                    OrderNo = field.OrderNo;
                    LabelKey = field.LabelKey;
                    PlaceholderKey = field.PlaceholderKey ?? "";
                    TooltipKey = field.TooltipKey ?? "";
                    IsVisible = field.IsVisible;
                    IsReadOnly = field.IsReadOnly;
                    ControlPropsJson = field.ControlPropsJson ?? "{}";
                }
            }

            // 3. Load linked rules
            if (FieldId > 0 && _ruleService is not null)
            {
                var rules = await _ruleService.GetRulesByFieldAsync(FieldId, ct);
                LinkedRules.Clear();
                foreach (var r in rules)
                {
                    LinkedRules.Add(new RuleSummaryDto
                    {
                        RuleId = r.RuleId,
                        OrderNo = r.OrderNo,
                        RuleTypeCode = r.RuleTypeCode,
                        ExpressionPreview = r.ExpressionJson ?? "",
                        ErrorKey = r.ErrorKey,
                        IsActive = r.IsActive
                    });
                }
                // Cập nhật IsRequired dựa trên linked rules
                _isRequired = LinkedRules.Any(r => r.RuleTypeCode == "Required");
                RaisePropertyChanged(nameof(IsRequired));
            }

            // 4. Load linked events
            if (FieldId > 0 && _eventService is not null)
            {
                var events = await _eventService.GetEventsByFieldAsync(FieldId, ct);
                LinkedEvents.Clear();
                foreach (var e in events)
                {
                    LinkedEvents.Add(new EventSummaryDto
                    {
                        EventId = e.EventId,
                        OrderNo = e.OrderNo,
                        TriggerCode = e.TriggerCode,
                        ConditionPreview = e.ConditionExpr ?? "",
                        ActionsCount = e.ActionsCount,
                        IsActive = e.IsActive
                    });
                }
            }

            IsLoading = false;
            IsDirty = false;
        }
        catch (OperationCanceledException) { /* Navigation away */ }
        catch
        {
            // Fallback mock khi lỗi DB
            LoadMockData();
        }
    }

    // ── Load mock data ───────────────────────────────────────

    /// <summary>
    /// Load mock data cho demo khi chưa kết nối DB.
    /// </summary>
    private void LoadMockData()
    {
        IsLoading = true;

        // ── 1. Load danh sách columns ────────────────────────
        AvailableColumns.Clear();
        AvailableColumns.Add(new ColumnInfoDto { ColumnId = 1, ColumnCode = "MaDonHang", DataType = "nvarchar(50)", NetType = "String", IsNullable = false });
        AvailableColumns.Add(new ColumnInfoDto { ColumnId = 2, ColumnCode = "NgayDatHang", DataType = "datetime", NetType = "DateTime", IsNullable = false });
        AvailableColumns.Add(new ColumnInfoDto { ColumnId = 3, ColumnCode = "TrangThai", DataType = "nvarchar(20)", NetType = "String", IsNullable = false });
        AvailableColumns.Add(new ColumnInfoDto { ColumnId = 4, ColumnCode = "NhaCungCap", DataType = "int", NetType = "Int32", IsNullable = true });
        AvailableColumns.Add(new ColumnInfoDto { ColumnId = 5, ColumnCode = "SoLuong", DataType = "int", NetType = "Int32", IsNullable = false });
        AvailableColumns.Add(new ColumnInfoDto { ColumnId = 6, ColumnCode = "DonGia", DataType = "decimal(18,2)", NetType = "Decimal", IsNullable = false });
        AvailableColumns.Add(new ColumnInfoDto { ColumnId = 7, ColumnCode = "ThanhTien", DataType = "decimal(18,2)", NetType = "Decimal", IsNullable = false });
        AvailableColumns.Add(new ColumnInfoDto { ColumnId = 8, ColumnCode = "LyDoTuChoi", DataType = "nvarchar(500)", NetType = "String", IsNullable = true });

        if (_mode == "new")
        {
            // ── Tạo field mới ────────────────────────────────
            SectionName = "Chi Tiết";
            SelectedEditorType = "TextBox";
            IsLoading = false;
            IsDirty = false;
            return;
        }

        // ── 2. Load thông tin field hiện tại (mock: SoLuong) ─
        FieldId = FieldId > 0 ? FieldId : 5;
        FormId = FormId > 0 ? FormId : 1;
        SectionId = SectionId > 0 ? SectionId : 2;
        SectionName = "Chi Tiết";

        SelectedColumn = AvailableColumns.FirstOrDefault(c => c.ColumnCode == "SoLuong");
        SelectedEditorType = "NumericBox";
        OrderNo = 1;

        LabelKey = "lbl.soluong";
        PlaceholderKey = "ph.soluong";
        TooltipKey = "tip.soluong";

        IsVisible = true;
        IsReadOnly = false;
        IsRequired = true;

        // ── 3. Load linked rules (mock) ──────────────────────
        LinkedRules.Clear();
        LinkedRules.Add(new RuleSummaryDto
        {
            RuleId = 1, OrderNo = 1, RuleTypeCode = "Required",
            ExpressionPreview = "(built-in)", ErrorKey = "err.fld.req", IsActive = true
        });
        LinkedRules.Add(new RuleSummaryDto
        {
            RuleId = 2, OrderNo = 2, RuleTypeCode = "Numeric",
            ExpressionPreview = "SoLuong >= 1 && SoLuong <= 9999",
            ErrorKey = "err.sl.range", IsActive = true
        });

        // ── 4. Load linked events (mock) ─────────────────────
        LinkedEvents.Clear();
        LinkedEvents.Add(new EventSummaryDto
        {
            EventId = 18, TriggerCode = "OnChange",
            ConditionPreview = "TrangThai == \"TuChoi\"",
            ActionsCount = 3, IsActive = true
        });

        IsLoading = false;
        IsDirty = false;
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
            setter(ResolveMockI18n(key));
        }
    }

    /// <summary>
    /// Mock resolve i18n key thành text hiển thị.
    /// </summary>
    private static string ResolveMockI18n(string key) => key switch
    {
        "lbl.soluong" => "Số Lượng",
        "ph.soluong" => "Nhập số lượng",
        "tip.soluong" => "Số lượng đặt hàng",
        "lbl.madohang" => "Mã Đơn Hàng",
        "lbl.ngaydathang" => "Ngày Đặt Hàng",
        "lbl.trangthai" => "Trạng Thái",
        "lbl.nhacungcap" => "Nhà Cung Cấp",
        "lbl.dongia" => "Đơn Giá",
        "lbl.thanhtien" => "Thành Tiền",
        "lbl.lydotuchoi" => "Lý Do Từ Chối",
        _ => key
    };

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
                p => p.Value);

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
            new() { PropName = "format",  PropType = "Enum",   DefaultValue = "dd/MM/yyyy", Label = "Định dạng ngày", AllowedValues = ["dd/MM/yyyy", "MM/yyyy", "yyyy"] },
            new() { PropName = "minDate", PropType = "String", DefaultValue = "",            Label = "Ngày tối thiểu" },
            new() { PropName = "maxDate", PropType = "String", DefaultValue = "",            Label = "Ngày tối đa" },
        ],
        _ => []
    };

    // ── Required rule toggle ─────────────────────────────────

    /// <summary>
    /// Tự động tạo/xóa Required rule khi toggle IsRequired (chưa save DB).
    /// </summary>
    private void ToggleRequiredRule(bool isRequired)
    {
        var existing = LinkedRules.FirstOrDefault(r => r.RuleTypeCode == "Required");

        if (isRequired && existing is null)
        {
            LinkedRules.Insert(0, new RuleSummaryDto
            {
                RuleId = 0, OrderNo = 0, RuleTypeCode = "Required",
                ExpressionPreview = "(built-in)", ErrorKey = "err.fld.req", IsActive = true
            });
            // NOTE: Reindex OrderNo sau khi insert
            ReindexRuleOrders();
        }
        else if (!isRequired && existing is not null)
        {
            LinkedRules.Remove(existing);
            ReindexRuleOrders();
        }
    }

    private void ReindexRuleOrders()
    {
        for (int i = 0; i < LinkedRules.Count; i++)
            LinkedRules[i].OrderNo = i + 1;
    }

    // ── Command handlers ─────────────────────────────────────

    private async Task ExecuteSaveAsync()
    {
        if (_fieldService is not null && _appConfig is { IsConfigured: true })
        {
            var field = new FieldConfigRecord
            {
                FieldId = FieldId,
                FormId = FormId,
                SectionId = SectionId > 0 ? SectionId : null,
                ColumnId = SelectedColumn?.ColumnId ?? 0,
                ColumnCode = ColumnCode,
                SectionCode = SectionName,
                EditorType = SelectedEditorType,
                LabelKey = LabelKey,
                PlaceholderKey = PlaceholderKey,
                TooltipKey = TooltipKey,
                IsVisible = IsVisible,
                IsReadOnly = IsReadOnly,
                OrderNo = OrderNo,
                ControlPropsJson = ControlPropsJson
            };
            await _fieldService.SaveFieldAsync(field, _appConfig.TenantId, _cts.Token);
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
            { "fieldId", FieldId },
            { "formId", FormId },
            { "mode", "new" }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.ValidationRuleEditor, p);
    }

    private void ExecuteOpenRule(RuleSummaryDto? rule)
    {
        if (rule is null) return;
        var p = new NavigationParameters
        {
            { "ruleId", rule.RuleId },
            { "fieldId", FieldId }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.ValidationRuleEditor, p);
    }

    private void ExecuteDeleteRule(RuleSummaryDto? rule)
    {
        if (rule is null) return;
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
