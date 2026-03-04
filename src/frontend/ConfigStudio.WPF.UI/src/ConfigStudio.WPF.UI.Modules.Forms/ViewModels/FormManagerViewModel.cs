// File    : FormManagerViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho màn hình Form Manager (Screen 02) — danh sách form, search/filter, CRUD.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// ViewModel cho màn hình Form Manager (Screen 02).
/// Hiển thị DataGrid danh sách form, search/filter, navigate đến Form Editor.
/// </summary>
public sealed class FormManagerViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;
    private readonly IFormDataService? _formDataService;
    private readonly IAppConfigService? _appConfig;
    private static readonly string ErrorLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ICare247", "ConfigStudio", "logs", "form-manager-errors.log");

    // ── Data ──────────────────────────────────────────────────
    public ObservableCollection<FormSummaryDto> Forms { get; } = [];

    /// <summary>CollectionView hỗ trợ filter/sort trên DataGrid.</summary>
    public ICollectionView FormsView { get; }

    private FormSummaryDto? _selectedForm;
    public FormSummaryDto? SelectedForm
    {
        get => _selectedForm;
        set
        {
            if (SetProperty(ref _selectedForm, value))
            {
                EditFormCommand.RaiseCanExecuteChanged();
                DeleteFormCommand.RaiseCanExecuteChanged();
                DuplicateFormCommand.RaiseCanExecuteChanged();
            }
        }
    }

    // ── Filter ────────────────────────────────────────────────
    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FormsView.Refresh();
                RaisePropertyChanged(nameof(FilteredCount));
            }
        }
    }

    private string _platformFilter = "Tất cả";
    public string PlatformFilter
    {
        get => _platformFilter;
        set
        {
            if (SetProperty(ref _platformFilter, value))
            {
                FormsView.Refresh();
                RaisePropertyChanged(nameof(FilteredCount));
            }
        }
    }

    public List<string> PlatformOptions { get; } = ["Tất cả", "web", "mobile"];

    private bool _showInactiveOnly;
    public bool ShowInactiveOnly
    {
        get => _showInactiveOnly;
        set
        {
            if (SetProperty(ref _showInactiveOnly, value))
            {
                FormsView.Refresh();
                RaisePropertyChanged(nameof(FilteredCount));
            }
        }
    }

    // ── Statistics ─────────────────────────────────────────────
    public int TotalForms => Forms.Count;
    public int FilteredCount => FormsView.Cast<object>().Count();

    // ── Loading state ──────────────────────────────────────────
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    private string _loadErrorMessage = "";
    /// <summary>
    /// Message lỗi khi load danh sách form thất bại.
    /// </summary>
    public string LoadErrorMessage
    {
        get => _loadErrorMessage;
        private set
        {
            if (SetProperty(ref _loadErrorMessage, value))
                RaisePropertyChanged(nameof(HasLoadError));
        }
    }

    /// <summary>True khi có lỗi tải dữ liệu để hiển thị banner.</summary>
    public bool HasLoadError => !string.IsNullOrWhiteSpace(_loadErrorMessage);

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand CreateFormCommand { get; }
    public DelegateCommand EditFormCommand { get; }
    public DelegateCommand DeleteFormCommand { get; }
    public DelegateCommand DuplicateFormCommand { get; }
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand<FormSummaryDto> OpenFormCommand { get; }

    public FormManagerViewModel(
        IRegionManager regionManager,
        IFormDataService? formDataService = null,
        IAppConfigService? appConfig = null)
    {
        _regionManager = regionManager;
        _formDataService = formDataService;
        _appConfig = appConfig;

        FormsView = CollectionViewSource.GetDefaultView(Forms);
        FormsView.Filter = ApplyFilter;

        CreateFormCommand = new DelegateCommand(ExecuteCreateForm);
        EditFormCommand = new DelegateCommand(ExecuteEditForm, () => SelectedForm is not null);
        DeleteFormCommand = new DelegateCommand(ExecuteDeleteForm, () => SelectedForm is not null);
        DuplicateFormCommand = new DelegateCommand(ExecuteDuplicateForm, () => SelectedForm is not null);
        RefreshCommand = new DelegateCommand(async () => await LoadDataAsync());
        OpenFormCommand = new DelegateCommand<FormSummaryDto>(ExecuteOpenForm);
    }

    // ── INavigationAware ─────────────────────────────────────

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // NOTE: fire-and-forget — WPF không hỗ trợ async navigation callback
        _ = LoadDataAsync();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    // ── Load data ────────────────────────────────────────────

    /// <summary>
    /// Load danh sách form từ DB (nếu đã cấu hình), fallback sang mock data.
    /// </summary>
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        LoadErrorMessage = "";

        try
        {
            await EnsureAppConfigLoadedAsync();

            // ── 1. Thử load từ DB ──────────────────────────────────
            if (_formDataService is not null && _appConfig is not null && _appConfig.IsConfigured)
            {
                var records = await _formDataService.GetAllFormsAsync(
                    _appConfig.TenantId,
                    includeInactive: true);

                Forms.Clear();
                foreach (var r in records)
                {
                    Forms.Add(new FormSummaryDto
                    {
                        FormId       = r.FormId,
                        FormCode     = r.FormCode,
                        FormName     = r.FormName,
                        Version      = r.Version,
                        Platform     = r.Platform,
                        SectionCount = r.SectionCount,
                        FieldCount   = r.FieldCount,
                        IsActive     = r.IsActive,
                        UpdatedAt    = r.UpdatedAt,
                        UpdatedBy    = r.UpdatedBy,
                    });
                }

                RaisePropertyChanged(nameof(TotalForms));
                RaisePropertyChanged(nameof(FilteredCount));
                return;
            }

            // ── 2. Fallback: mock data khi chưa có DB ──────────────
            LoadMockData();
        }
        catch (Exception ex)
        {
            Forms.Clear();
            RaisePropertyChanged(nameof(TotalForms));
            RaisePropertyChanged(nameof(FilteredCount));

            LoadErrorMessage = $"Không thể tải danh sách form: {ex.Message}";
            LogLoadError(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Mock data dùng khi chưa cấu hình DB (dev / demo).
    /// </summary>
    private void LoadMockData()
    {
        Forms.Clear();

        Forms.Add(new FormSummaryDto
        {
            FormId = 1, FormCode = "PO_ORDER", FormName = "Đơn Đặt Hàng",
            Version = 3, Platform = "web", SectionCount = 3, FieldCount = 8,
            IsActive = true, UpdatedAt = DateTime.Now.AddHours(-2), UpdatedBy = "admin"
        });
        Forms.Add(new FormSummaryDto
        {
            FormId = 2, FormCode = "HR_LEAVE", FormName = "Đơn Xin Nghỉ Phép",
            Version = 1, Platform = "web", SectionCount = 2, FieldCount = 6,
            IsActive = true, UpdatedAt = DateTime.Now.AddDays(-1), UpdatedBy = "hr_admin"
        });
        Forms.Add(new FormSummaryDto
        {
            FormId = 3, FormCode = "INV_RECEIPT", FormName = "Phiếu Nhập Kho",
            Version = 5, Platform = "web", SectionCount = 4, FieldCount = 12,
            IsActive = true, UpdatedAt = DateTime.Now.AddDays(-3), UpdatedBy = "admin"
        });
        Forms.Add(new FormSummaryDto
        {
            FormId = 4, FormCode = "MOBILE_CHECK", FormName = "Kiểm Tra Hiện Trường",
            Version = 2, Platform = "mobile", SectionCount = 5, FieldCount = 15,
            IsActive = true, UpdatedAt = DateTime.Now.AddDays(-5), UpdatedBy = "field_admin"
        });
        Forms.Add(new FormSummaryDto
        {
            FormId = 5, FormCode = "OLD_FORM", FormName = "Form Cũ (Inactive)",
            Version = 1, Platform = "web", SectionCount = 1, FieldCount = 3,
            IsActive = false, UpdatedAt = DateTime.Now.AddMonths(-2), UpdatedBy = "admin"
        });

        RaisePropertyChanged(nameof(TotalForms));
        RaisePropertyChanged(nameof(FilteredCount));
    }

    // ── Filter logic ─────────────────────────────────────────

    private bool ApplyFilter(object obj)
    {
        if (obj is not FormSummaryDto form) return false;

        // NOTE: Filter active/inactive
        if (ShowInactiveOnly && form.IsActive) return false;
        if (!ShowInactiveOnly && !form.IsActive) return false;

        // NOTE: Filter platform
        if (PlatformFilter != "Tất cả" && form.Platform != PlatformFilter) return false;

        // NOTE: Filter search text
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim();
            return form.FormCode.Contains(q, StringComparison.OrdinalIgnoreCase)
                || form.FormName.Contains(q, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    // ── Command handlers ─────────────────────────────────────

    private void ExecuteCreateForm()
    {
        var p = new NavigationParameters { { "formId", 0 } };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormEditor, p);
    }

    private void ExecuteEditForm()
    {
        if (SelectedForm is null) return;
        NavigateToEditor(SelectedForm.FormId);
    }

    private void ExecuteOpenForm(FormSummaryDto? form)
    {
        if (form is null) return;
        NavigateToEditor(form.FormId);
    }

    private void ExecuteDeleteForm()
    {
        // TODO(phase2): Confirm dialog trước khi soft-delete
        if (SelectedForm is null) return;
        SelectedForm.IsActive = false;
        FormsView.Refresh();
        RaisePropertyChanged(nameof(FilteredCount));
    }

    private void ExecuteDuplicateForm()
    {
        if (SelectedForm is null) return;
        var clone = new FormSummaryDto
        {
            FormId = Forms.Max(f => f.FormId) + 1,
            FormCode = SelectedForm.FormCode + "_COPY",
            FormName = SelectedForm.FormName + " (Bản sao)",
            Version = 1,
            Platform = SelectedForm.Platform,
            SectionCount = SelectedForm.SectionCount,
            FieldCount = SelectedForm.FieldCount,
            IsActive = true,
            UpdatedAt = DateTime.Now,
            UpdatedBy = "admin"
        };
        Forms.Add(clone);
        RaisePropertyChanged(nameof(TotalForms));
        RaisePropertyChanged(nameof(FilteredCount));
    }

    private void NavigateToEditor(int formId)
    {
        var p = new NavigationParameters { { "formId", formId } };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormEditor, p);
    }

    /// <summary>
    /// Ghi lỗi load dữ liệu ra file local để dễ truy vết sự cố production.
    /// </summary>
    private static void LogLoadError(Exception ex)
    {
        try
        {
            var dir = Path.GetDirectoryName(ErrorLogPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var log = $"""
                [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] LoadFormsError
                Message: {ex.Message}
                StackTrace:
                {ex.StackTrace}
                ----------------------------------------
                """;

            File.AppendAllText(ErrorLogPath, log + Environment.NewLine);
        }
        catch
        {
            // NOTE: Không để logging failure làm sập luồng UI.
        }
    }

    /// <summary>
    /// Đảm bảo đã load appsettings trước khi kiểm tra IsConfigured/TenantId.
    /// </summary>
    private async Task EnsureAppConfigLoadedAsync()
    {
        if (_appConfig is null || _appConfig.IsConfigured)
            return;

        await _appConfig.LoadAsync();
    }
}
