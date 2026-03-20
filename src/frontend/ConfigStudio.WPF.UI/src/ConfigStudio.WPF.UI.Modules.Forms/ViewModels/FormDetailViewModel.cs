// File    : FormDetailViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho màn hình Form Detail (Screen 02 Detail) — xem readonly metadata form.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// ViewModel cho màn hình Form Detail — hiển thị readonly toàn bộ metadata của form
/// bao gồm header, sections, fields, events, rules, audit log.
/// Khi DB đã cấu hình → load dữ liệu thật qua IFormDetailDataService.
/// Khi chưa cấu hình → fallback mock data.
/// </summary>
public sealed class FormDetailViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;
    private readonly IFormDetailDataService? _detailService;
    private readonly IAppConfigService? _appConfig;
    private CancellationTokenSource _cts = new();

    // ── Header metadata ───────────────────────────────────────
    private int _formId;
    public int FormId
    {
        get => _formId;
        private set => SetProperty(ref _formId, value);
    }

    private string _formCode = "";
    public string FormCode
    {
        get => _formCode;
        private set => SetProperty(ref _formCode, value);
    }

    private string _formName = "";
    public string FormName
    {
        get => _formName;
        private set => SetProperty(ref _formName, value);
    }

    private string _tableName = "";
    public string TableName
    {
        get => _tableName;
        private set => SetProperty(ref _tableName, value);
    }

    private string _platform = "";
    public string Platform
    {
        get => _platform;
        private set => SetProperty(ref _platform, value);
    }

    private string _layoutEngine = "";
    public string LayoutEngine
    {
        get => _layoutEngine;
        private set => SetProperty(ref _layoutEngine, value);
    }

    private string _description = "";
    public string Description
    {
        get => _description;
        private set => SetProperty(ref _description, value);
    }

    private int _version;
    public int Version
    {
        get => _version;
        private set => SetProperty(ref _version, value);
    }

    private string _checksum = "";
    public string Checksum
    {
        get => _checksum;
        private set => SetProperty(ref _checksum, value);
    }

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        private set
        {
            if (SetProperty(ref _isActive, value))
            {
                RaisePropertyChanged(nameof(StatusText));
                DeactivateCommand.RaiseCanExecuteChanged();
                RestoreCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private DateTime _updatedAt;
    public DateTime UpdatedAt
    {
        get => _updatedAt;
        private set => SetProperty(ref _updatedAt, value);
    }

    private string _updatedBy = "";
    public string UpdatedBy
    {
        get => _updatedBy;
        private set => SetProperty(ref _updatedBy, value);
    }

    /// <summary>Badge text: "Active" / "Inactive".</summary>
    public string StatusText => IsActive ? "Active" : "Inactive";

    // ── Loading state ─────────────────────────────────────────
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    // ── Tab collections ───────────────────────────────────────
    public ObservableCollection<SectionDetailDto>  Sections  { get; } = [];
    public ObservableCollection<FieldDetailDto>    Fields    { get; } = [];
    public ObservableCollection<EventSummaryDto>   Events    { get; } = [];
    public ObservableCollection<RuleSummaryDto>    Rules     { get; } = [];
    public ObservableCollection<AuditLogEntryDto>  AuditLogs { get; } = [];

    // ── Tab header counts (computed) ──────────────────────────
    public int SectionCount  => Sections.Count;
    public int FieldCount    => Fields.Count;
    public int EventCount    => Events.Count;
    public int RuleCount     => Rules.Count;
    public int AuditCount    => AuditLogs.Count;

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand BackCommand        { get; }
    public DelegateCommand EditCommand        { get; }
    public DelegateCommand PreviewCommand     { get; }
    public DelegateCommand DeactivateCommand  { get; }
    public DelegateCommand RestoreCommand     { get; }

    public FormDetailViewModel(
        IRegionManager regionManager,
        IFormDetailDataService? detailService = null,
        IAppConfigService? appConfig = null)
    {
        _regionManager = regionManager;
        _detailService = detailService;
        _appConfig = appConfig;

        BackCommand       = new DelegateCommand(ExecuteBack);
        EditCommand       = new DelegateCommand(ExecuteEdit);
        PreviewCommand    = new DelegateCommand(ExecutePreview);
        DeactivateCommand = new DelegateCommand(ExecuteDeactivateAsync, () => IsActive);
        RestoreCommand    = new DelegateCommand(ExecuteRestoreAsync,    () => !IsActive);
    }

    // ── INavigationAware ─────────────────────────────────────

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // NOTE: nhận formId + formCode từ FormManagerView qua ViewDetailCommand
        FormId = navigationContext.Parameters.GetValue<int>("formId");
        var code = navigationContext.Parameters.GetValue<string>("formCode");
        if (!string.IsNullOrWhiteSpace(code))
            _ = LoadDataAsync(code);
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => false;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
    }

    // ── Load data ────────────────────────────────────────────

    private async Task LoadDataAsync(string formCode)
    {
        IsLoading = true;
        try
        {
            if (_detailService is not null && _appConfig is { IsConfigured: true })
            {
                await LoadFromDatabaseAsync(formCode);
            }
            else
            {
                LoadMockData(formCode);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Load toàn bộ metadata form từ DB qua IFormDetailDataService.
    /// </summary>
    private async Task LoadFromDatabaseAsync(string formCode)
    {
        try
        {
            var ct = _cts.Token;
            var tenantId = _appConfig!.TenantId;

            // Load header
            var detail = await _detailService!.GetFormDetailAsync(FormId, tenantId, ct);
            if (detail is null)
            {
                // Fallback nếu không tìm thấy form
                LoadMockData(formCode);
                return;
            }

            FormCode     = detail.FormCode;
            FormName     = detail.FormCode; // DB không có FormName riêng
            TableName    = detail.TableName;
            Platform     = detail.Platform;
            LayoutEngine = detail.LayoutEngine;
            Description  = detail.Description ?? "";
            Version      = detail.Version;
            Checksum     = detail.Checksum ?? "";
            IsActive     = detail.IsActive;
            UpdatedAt    = detail.UpdatedAt;
            UpdatedBy    = ""; // DB query hiện không SELECT Updated_By

            // Load sections
            var sections = await _detailService.GetSectionsByFormAsync(FormId, tenantId, ct);
            Sections.Clear();
            foreach (var s in sections)
            {
                Sections.Add(new SectionDetailDto
                {
                    SectionId   = s.SectionId,
                    OrderNo     = s.OrderNo,
                    SectionCode = s.SectionCode,
                    TitleKey    = s.TitleKey ?? "",
                    LayoutJson  = s.LayoutJson ?? "",
                    FieldCount  = s.FieldCount
                });
            }
            RaisePropertyChanged(nameof(SectionCount));

            // Load fields
            var fields = await _detailService.GetFieldsByFormAsync(FormId, tenantId, ct);
            Fields.Clear();
            foreach (var f in fields)
            {
                Fields.Add(new FieldDetailDto
                {
                    FieldId     = f.FieldId,
                    OrderNo     = f.OrderNo,
                    ColumnName  = f.ColumnCode,
                    SectionCode = f.SectionCode,
                    EditorType  = f.EditorType,
                    IsVisible   = f.IsVisible,
                    IsReadOnly  = f.IsReadOnly,
                    RuleCount   = f.RuleCount
                });
            }
            RaisePropertyChanged(nameof(FieldCount));

            // Load events summary
            var events = await _detailService.GetEventsSummaryByFormAsync(FormId, tenantId, ct);
            Events.Clear();
            foreach (var e in events)
            {
                Events.Add(new EventSummaryDto
                {
                    EventId          = e.EventId,
                    OrderNo          = e.OrderNo,
                    TriggerCode      = e.TriggerCode,
                    FieldTarget      = e.FieldTarget,
                    ConditionPreview = e.ConditionPreview ?? "",
                    ActionsCount     = e.ActionsCount,
                    IsActive         = e.IsActive
                });
            }
            RaisePropertyChanged(nameof(EventCount));

            // Load rules summary
            var rules = await _detailService.GetRulesSummaryByFormAsync(FormId, tenantId, ct);
            Rules.Clear();
            foreach (var r in rules)
            {
                Rules.Add(new RuleSummaryDto
                {
                    RuleId            = r.RuleId,
                    OrderNo           = r.OrderNo,
                    RuleTypeCode      = r.RuleTypeCode,
                    ExpressionPreview = r.ExpressionPreview ?? "",
                    ErrorKey          = r.ErrorKey,
                    IsActive          = r.IsActive
                });
            }
            RaisePropertyChanged(nameof(RuleCount));

            // Load audit log
            var audit = await _detailService.GetAuditLogAsync("Form", FormId, ct);
            AuditLogs.Clear();
            foreach (var a in audit)
            {
                AuditLogs.Add(new AuditLogEntryDto
                {
                    LogId         = (int)a.AuditId,
                    ActionType    = a.Action,
                    ChangedAt     = a.ChangedAt,
                    ChangedBy     = a.ChangedBy,
                    CorrelationId = a.CorrelationId ?? "",
                    ChangeSummary = a.ChangeSummary ?? ""
                });
            }
            RaisePropertyChanged(nameof(AuditCount));
        }
        catch (OperationCanceledException) { /* Navigation away */ }
        catch
        {
            // Fallback mock khi lỗi DB
            LoadMockData(formCode);
        }
    }

    /// <summary>
    /// Mock data để demo màn hình khi chưa kết nối DB.
    /// </summary>
    private void LoadMockData(string formCode)
    {
        // ── Header ──────────────────────────────────────────
        FormCode     = formCode;
        // NOTE: Gán FormId mock nếu chưa có (navigate từ FormManager sẽ truyền formId thật)
        if (FormId == 0)
        {
            FormId = formCode switch
            {
                "PO_ORDER"     => 1,
                "HR_LEAVE"     => 2,
                "INV_RECEIPT"  => 3,
                "MOBILE_CHECK" => 4,
                "WPF_REPORT"   => 5,
                _              => 99
            };
        }
        FormName     = formCode switch
        {
            "PO_ORDER"      => "Đơn Đặt Hàng",
            "HR_LEAVE"      => "Đơn Xin Nghỉ Phép",
            "INV_RECEIPT"   => "Phiếu Nhập Kho",
            "MOBILE_CHECK"  => "Kiểm Tra Hiện Trường",
            "WPF_REPORT"    => "Báo Cáo Desktop",
            _               => formCode
        };
        TableName    = "PurchaseOrder";
        Platform     = "web";
        LayoutEngine = "Grid";
        Description  = $"Form cấu hình cho {FormName} — tự động tạo bởi mock data.";
        Version      = 3;
        Checksum     = "a1b2c3d4e5f6a1b2";
        IsActive     = true;
        UpdatedAt    = DateTime.Now.AddHours(-2);
        UpdatedBy    = "admin";

        // ── Sections ─────────────────────────────────────────
        Sections.Clear();
        Sections.Add(new SectionDetailDto { SectionId = 1, OrderNo = 1, SectionCode = "GENERAL_INFO",   TitleKey = "section.general.title",   FieldCount = 4 });
        Sections.Add(new SectionDetailDto { SectionId = 2, OrderNo = 2, SectionCode = "CONTACT_INFO",   TitleKey = "section.contact.title",   FieldCount = 3 });
        Sections.Add(new SectionDetailDto { SectionId = 3, OrderNo = 3, SectionCode = "MEDICAL_HISTORY",TitleKey = "section.medical.title",   FieldCount = 5 });
        RaisePropertyChanged(nameof(SectionCount));

        // ── Fields ────────────────────────────────────────────
        Fields.Clear();
        Fields.Add(new FieldDetailDto { FieldId = 1, OrderNo = 1, ColumnName = "FullName",    SectionCode = "GENERAL_INFO",    EditorType = "TextBox",   IsVisible = true,  IsReadOnly = false, RuleCount = 2 });
        Fields.Add(new FieldDetailDto { FieldId = 2, OrderNo = 2, ColumnName = "DateOfBirth", SectionCode = "GENERAL_INFO",    EditorType = "DateEdit",  IsVisible = true,  IsReadOnly = false, RuleCount = 1 });
        Fields.Add(new FieldDetailDto { FieldId = 3, OrderNo = 3, ColumnName = "Gender",      SectionCode = "GENERAL_INFO",    EditorType = "ComboBox",  IsVisible = true,  IsReadOnly = false, RuleCount = 0 });
        Fields.Add(new FieldDetailDto { FieldId = 4, OrderNo = 4, ColumnName = "Phone",       SectionCode = "CONTACT_INFO",    EditorType = "TextBox",   IsVisible = true,  IsReadOnly = false, RuleCount = 1 });
        Fields.Add(new FieldDetailDto { FieldId = 5, OrderNo = 5, ColumnName = "Email",       SectionCode = "CONTACT_INFO",    EditorType = "TextBox",   IsVisible = true,  IsReadOnly = false, RuleCount = 1 });
        Fields.Add(new FieldDetailDto { FieldId = 6, OrderNo = 6, ColumnName = "Province",    SectionCode = "CONTACT_INFO",    EditorType = "ComboBox",  IsVisible = true,  IsReadOnly = false, RuleCount = 0 });
        Fields.Add(new FieldDetailDto { FieldId = 7, OrderNo = 7, ColumnName = "Notes",       SectionCode = "MEDICAL_HISTORY", EditorType = "MemoEdit",  IsVisible = true,  IsReadOnly = false, RuleCount = 0 });
        Fields.Add(new FieldDetailDto { FieldId = 8, OrderNo = 8, ColumnName = "CreatedAt",   SectionCode = "GENERAL_INFO",    EditorType = "DateEdit",  IsVisible = false, IsReadOnly = true,  RuleCount = 0 });
        RaisePropertyChanged(nameof(FieldCount));

        // ── Events ────────────────────────────────────────────
        Events.Clear();
        Events.Add(new EventSummaryDto { EventId = 1, OrderNo = 1, TriggerCode = "OnLoad",    FieldTarget = "",         ConditionPreview = "",                  ActionsCount = 1, IsActive = true });
        Events.Add(new EventSummaryDto { EventId = 2, OrderNo = 2, TriggerCode = "OnChange",  FieldTarget = "Province", ConditionPreview = "",                  ActionsCount = 2, IsActive = true });
        Events.Add(new EventSummaryDto { EventId = 3, OrderNo = 3, TriggerCode = "OnSubmit",  FieldTarget = "",         ConditionPreview = "Gender == 'female'", ActionsCount = 3, IsActive = true });
        Events.Add(new EventSummaryDto { EventId = 4, OrderNo = 4, TriggerCode = "OnChange",  FieldTarget = "Phone",    ConditionPreview = "",                  ActionsCount = 1, IsActive = false });
        RaisePropertyChanged(nameof(EventCount));

        // ── Rules ─────────────────────────────────────────────
        Rules.Clear();
        Rules.Add(new RuleSummaryDto { RuleId = 1, OrderNo = 1, RuleTypeCode = "Required",   ExpressionPreview = "",                       ErrorKey = "error.required",    IsActive = true });
        Rules.Add(new RuleSummaryDto { RuleId = 2, OrderNo = 2, RuleTypeCode = "MaxLength",  ExpressionPreview = "len(FullName) <= 200",    ErrorKey = "error.maxlen",      IsActive = true });
        Rules.Add(new RuleSummaryDto { RuleId = 3, OrderNo = 3, RuleTypeCode = "DateRange",  ExpressionPreview = "DateOfBirth <= today()",  ErrorKey = "error.futuredate",  IsActive = true });
        Rules.Add(new RuleSummaryDto { RuleId = 4, OrderNo = 4, RuleTypeCode = "Regex",      ExpressionPreview = "regex(Phone, '^0[0-9]')", ErrorKey = "error.phoneformat", IsActive = true });
        Rules.Add(new RuleSummaryDto { RuleId = 5, OrderNo = 5, RuleTypeCode = "Required",   ExpressionPreview = "",                       ErrorKey = "error.required",    IsActive = true });
        RaisePropertyChanged(nameof(RuleCount));

        // ── Audit Log ─────────────────────────────────────────
        AuditLogs.Clear();
        AuditLogs.Add(new AuditLogEntryDto { LogId = 3, ActionType = "UPDATE", ChangedAt = DateTime.Now.AddHours(-2),   ChangedBy = "admin",     CorrelationId = "abc-123-def", ChangeSummary = "Cập nhật Description" });
        AuditLogs.Add(new AuditLogEntryDto { LogId = 2, ActionType = "UPDATE", ChangedAt = DateTime.Now.AddDays(-5),   ChangedBy = "dev@icare",  CorrelationId = "xyz-456-ghi", ChangeSummary = "Thêm section MEDICAL_HISTORY" });
        AuditLogs.Add(new AuditLogEntryDto { LogId = 1, ActionType = "INSERT", ChangedAt = DateTime.Now.AddDays(-14),  ChangedBy = "dev@icare",  CorrelationId = "mnp-789-jkl", ChangeSummary = "Tạo form lần đầu" });
        RaisePropertyChanged(nameof(AuditCount));
    }

    // ── Command handlers ─────────────────────────────────────

    private void ExecuteBack()
        => _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormManager);

    private void ExecuteEdit()
    {
        // NOTE: truyền formId + formCode để FormEditorView nhận đúng — formId>0 = edit mode
        var p = new NavigationParameters
        {
            { "formId", FormId },
            { "formCode", FormCode }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormEditor, p);
    }

    private void ExecutePreview()
    {
        // TODO(phase2): mở preview dialog render form từ metadata
    }

    private async void ExecuteDeactivateAsync()
    {
        if (_detailService is not null && _appConfig is { IsConfigured: true })
        {
            await _detailService.DeactivateFormAsync(FormId, _appConfig.TenantId, _cts.Token);
        }
        IsActive = false;
    }

    private async void ExecuteRestoreAsync()
    {
        if (_detailService is not null && _appConfig is { IsConfigured: true })
        {
            await _detailService.RestoreFormAsync(FormId, _appConfig.TenantId, _cts.Token);
        }
        IsActive = true;
    }
}
