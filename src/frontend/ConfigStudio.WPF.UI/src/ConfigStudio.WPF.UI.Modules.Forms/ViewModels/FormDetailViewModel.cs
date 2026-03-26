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
/// Khi chưa cấu hình → hiển thị thông báo lỗi, không load dữ liệu.
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

    // ── Loading / error state ─────────────────────────────────
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    private string _errorMessage = "";
    /// <summary>Thông báo lỗi khi load DB thất bại. Rỗng = không có lỗi.</summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
                RaisePropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(_errorMessage);

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
        // Nhận cả hai từ caller — FormId là guard chính, FormCode dùng cho display/mock/log
        FormId   = navigationContext.Parameters.GetValue<int>("formId");
        FormCode = navigationContext.Parameters.GetValue<string>("formCode") ?? "";

        // Guard bằng FormId (khóa kỹ thuật) — không phụ thuộc vào formCode có được truyền hay không
        if (FormId > 0)
            _ = LoadDataAsync();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => false;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
    }

    // ── Load data ────────────────────────────────────────────

    private async Task LoadDataAsync()
    {
        IsLoading    = true;
        ErrorMessage = "";
        try
        {
            if (_detailService is null || _appConfig is not { IsConfigured: true })
            {
                ErrorMessage = "Chưa cấu hình kết nối DB. Vào Settings để nhập Connection String.";
                return;
            }
            await LoadFromDatabaseAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Load toàn bộ metadata form từ DB qua IFormDetailDataService.
    /// Dùng FormId (khóa kỹ thuật) cho tất cả query — FormCode chỉ dùng cho display/log.
    /// </summary>
    private async Task LoadFromDatabaseAsync()
    {
        try
        {
            var ct = _cts.Token;
            var tenantId = _appConfig!.TenantId;

            // Load header theo FormId — nhất quán với tất cả query bên dưới
            var detail = await _detailService!.GetFormDetailAsync(FormId, tenantId, ct);
            if (detail is null)
            {
                ErrorMessage = $"Không tìm thấy form với Id={FormId} trong DB.";
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
        catch (OperationCanceledException) { /* Navigation away — bỏ qua */ }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi tải dữ liệu form: {ex.Message}";
        }
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
