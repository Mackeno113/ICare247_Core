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
using Prism.Dialogs;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// ViewModel cho màn hình Form Manager (Screen 02).
/// Hiển thị DataGrid danh sách form, search/filter, navigate đến Form Detail và Form Editor.
/// </summary>
public sealed class FormManagerViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;
    private readonly IDialogService _dialogService;
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
                DeactivateFormCommand.RaiseCanExecuteChanged();
                RestoreFormCommand.RaiseCanExecuteChanged();
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
                RefreshFilter();
        }
    }

    private string _platformFilter = "Tất cả";
    public string PlatformFilter
    {
        get => _platformFilter;
        set
        {
            if (SetProperty(ref _platformFilter, value))
                RefreshFilter();
        }
    }

    private string _tableFilter = "Tất cả";
    public string TableFilter
    {
        get => _tableFilter;
        set
        {
            if (SetProperty(ref _tableFilter, value))
                RefreshFilter();
        }
    }

    /// <summary>
    /// Khi false (mặc định): chỉ hiển thị form đang active.
    /// Khi true: hiển thị tất cả kể cả form đã ẩn.
    /// </summary>
    private bool _showInactive;
    public bool ShowInactive
    {
        get => _showInactive;
        set
        {
            if (SetProperty(ref _showInactive, value))
                RefreshFilter();
        }
    }

    public List<string> PlatformOptions { get; } = ["Tất cả", "web", "mobile", "wpf"];

    // NOTE: TableOptions load từ Sys_Table — hiện dùng mock, phase 2 sẽ call API
    private List<string> _tableOptions = ["Tất cả"];
    public List<string> TableOptions
    {
        get => _tableOptions;
        private set => SetProperty(ref _tableOptions, value);
    }

    // ── Statistics ─────────────────────────────────────────────
    public int TotalForms => Forms.Count;
    public int ActiveCount => Forms.Count(f => f.IsActive);
    public int InactiveCount => Forms.Count(f => !f.IsActive);
    public int FilteredCount => FormsView.Cast<object>().Count();

    // ── Loading state ──────────────────────────────────────────
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    private string _loadErrorMessage = "";
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
    public DelegateCommand<FormSummaryDto> ViewDetailCommand { get; }
    public DelegateCommand<FormSummaryDto> PreviewFormCommand { get; }
    public DelegateCommand<FormSummaryDto> DeactivateFormCommand { get; }
    public DelegateCommand<FormSummaryDto> RestoreFormCommand { get; }

    public FormManagerViewModel(
        IRegionManager regionManager,
        IDialogService dialogService,
        IFormDataService? formDataService = null,
        IAppConfigService? appConfig = null)
    {
        _regionManager   = regionManager;
        _dialogService   = dialogService;
        _formDataService = formDataService;
        _appConfig       = appConfig;

        FormsView = CollectionViewSource.GetDefaultView(Forms);
        FormsView.Filter = ApplyFilter;

        CreateFormCommand    = new DelegateCommand(ExecuteCreateForm);
        EditFormCommand      = new DelegateCommand(ExecuteEditForm, () => SelectedForm is not null);
        DeleteFormCommand    = new DelegateCommand(ExecuteDeleteSelected, () => SelectedForm is not null);
        DuplicateFormCommand = new DelegateCommand(ExecuteDuplicateForm, () => SelectedForm is not null);
        RefreshCommand       = new DelegateCommand(async () => await LoadDataSafeAsync());
        OpenFormCommand      = new DelegateCommand<FormSummaryDto>(ExecuteOpenForm);
        ViewDetailCommand    = new DelegateCommand<FormSummaryDto>(ExecuteViewDetail);
        PreviewFormCommand   = new DelegateCommand<FormSummaryDto>(ExecutePreviewForm);
        DeactivateFormCommand = new DelegateCommand<FormSummaryDto>(ExecuteDeactivate);
        RestoreFormCommand   = new DelegateCommand<FormSummaryDto>(ExecuteRestore);
    }

    // ── INavigationAware ─────────────────────────────────────

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // NOTE: fire-and-forget — WPF không hỗ trợ async navigation callback
        _ = LoadDataSafeAsync();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    // ── Load data ────────────────────────────────────────────

    /// <summary>
    /// Wrapper an toàn cho fire-and-forget — bắt mọi exception để tránh crash ứng dụng.
    /// </summary>
    private async Task LoadDataSafeAsync()
    {
        try
        {
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            LoadErrorMessage = $"Không thể tải danh sách form: {ex.Message}";
            LogLoadError(ex);
        }
    }

    /// <summary>
    /// Load danh sách form từ DB. Nếu chưa cấu hình → hiện thông báo, list rỗng.
    /// </summary>
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        LoadErrorMessage = "";

        try
        {
            await EnsureAppConfigLoadedAsync();

            if (_formDataService is null || _appConfig is null || !_appConfig.IsConfigured)
            {
                Forms.Clear();
                RaiseStatistics();
                LoadErrorMessage = "Chưa cấu hình kết nối DB. Vào Settings để nhập Connection String.";
                return;
            }

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
                    TableName    = r.TableName,
                    SectionCount = r.SectionCount,
                    FieldCount   = r.FieldCount,
                    EventCount   = r.EventCount,
                    IsActive     = r.IsActive,
                    UpdatedAt    = r.UpdatedAt,
                    UpdatedBy    = r.UpdatedBy,
                });
            }

            RebuildTableOptions();
            RaiseStatistics();
        }
        catch (Exception ex)
        {
            Forms.Clear();
            RaiseStatistics();
            LoadErrorMessage = $"Không thể tải danh sách form: {ex.Message}";
            LogLoadError(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Xây dựng lại danh sách TableOptions từ dữ liệu hiện tại trong Forms.
    /// </summary>
    private void RebuildTableOptions()
    {
        var tables = Forms
            .Select(f => f.TableName)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        TableOptions = ["Tất cả", .. tables];
    }

    // ── Filter logic ─────────────────────────────────────────

    private bool ApplyFilter(object obj)
    {
        if (obj is not FormSummaryDto form) return false;

        // NOTE: Khi chưa bật ShowInactive → chỉ hiện form active
        if (!ShowInactive && !form.IsActive) return false;

        // NOTE: Filter platform
        if (PlatformFilter != "Tất cả" && form.Platform != PlatformFilter) return false;

        // NOTE: Filter table
        if (TableFilter != "Tất cả" && form.TableName != TableFilter) return false;

        // NOTE: Filter search text — tìm theo FormCode hoặc FormName
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim();
            return form.FormCode.Contains(q, StringComparison.OrdinalIgnoreCase)
                || form.FormName.Contains(q, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    private void RefreshFilter()
    {
        FormsView.Refresh();
        RaisePropertyChanged(nameof(FilteredCount));
    }

    // ── Command handlers ─────────────────────────────────────

    private void ExecuteCreateForm()
    {
        // NOTE: Navigate sang FormEditor với formId=0 → chế độ tạo form mới
        NavigateToEditor(0);
    }

    private void ExecuteEditForm()
    {
        if (SelectedForm is null) return;
        // NOTE: Navigate sang FormEditor, activeTab=0 → mở tab "Thông tin Form" (metadata Ui_Form)
        var p = new NavigationParameters
        {
            { "formId",   SelectedForm.FormId },
            { "formCode", SelectedForm.FormCode },
            { "activeTab", 0 }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormEditor, p);
    }

    private void ExecuteOpenForm(FormSummaryDto? form)
    {
        if (form is null) return;
        NavigateToEditor(form.FormId);
    }

    /// <summary>
    /// Navigate sang FormDetailView (readonly) khi click vào FormCode.
    /// </summary>
    private void ExecuteViewDetail(FormSummaryDto? form)
    {
        if (form is null) return;
        var p = new NavigationParameters
        {
            { "formId", form.FormId },
            { "formCode", form.FormCode }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormDetail, p);
    }

    /// <summary>
    /// Mở preview form — render metadata thành form thực (TODO: phase 2).
    /// </summary>
    private void ExecutePreviewForm(FormSummaryDto? form)
    {
        if (form is null) return;

        var p = new DialogParameters
        {
            { "formId",   form.FormId   },
            { "formName", form.FormName },
            { "formCode", form.FormCode }
        };
        _dialogService.ShowDialog(ViewNames.FormPreviewDialog, p, _ => { });
    }

    /// <summary>
    /// Vô hiệu hóa form (soft-delete: Is_Active = false).
    /// Hiển thị DeactivateFormDialog để user xác nhận trước khi thực hiện.
    /// </summary>
    private void ExecuteDeactivate(FormSummaryDto? form)
    {
        if (form is null || !form.IsActive) return;

        // NOTE: Truyền thông tin form + impact counts vào dialog để user hiểu rõ ảnh hưởng
        var p = new DialogParameters
        {
            { "formCode",     form.FormCode },
            { "formName",     form.FormName },
            { "sectionCount", form.SectionCount },
            { "fieldCount",   form.FieldCount },
            { "eventCount",   form.EventCount }
        };

        _dialogService.ShowDialog(ViewNames.DeactivateFormDialog, p, result =>
        {
            if (result.Result != ButtonResult.OK) return;

            // NOTE: User đã xác nhận → thực hiện soft-delete
            form.IsActive = false;
            RefreshFilter();
            RaiseStatistics();
        });
    }

    /// <summary>
    /// Khôi phục form đã bị vô hiệu hóa.
    /// </summary>
    private void ExecuteRestore(FormSummaryDto? form)
    {
        if (form is null || form.IsActive) return;
        form.IsActive = true;
        RefreshFilter();
        RaiseStatistics();
    }

    /// <summary>
    /// Xóa (deactivate) form đang được chọn trong DataGrid.
    /// </summary>
    private void ExecuteDeleteSelected()
    {
        if (SelectedForm is null) return;
        ExecuteDeactivate(SelectedForm);
    }

    private void ExecuteDuplicateForm()
    {
        if (SelectedForm is null) return;

        // NOTE: Truyền source code + danh sách code hiện có để CloneFormDialog validate unique
        var existingCodes = Forms.Select(f => f.FormCode);
        var p = new DialogParameters
        {
            { "sourceFormCode", SelectedForm.FormCode },
            { "existingCodes",  existingCodes }
        };

        _dialogService.ShowDialog(ViewNames.CloneFormDialog, p, async result =>
        {
            if (result.Result != ButtonResult.OK) return;

            var newCode = result.Parameters.GetValue<string>("newFormCode");
            if (string.IsNullOrWhiteSpace(newCode)) return;

            var newName = SelectedForm.FormName + " (Bản sao)";
            var sourceId = SelectedForm.FormId;

            // Gọi DB nếu đã cấu hình — deep clone Ui_Form + Ui_Section + Ui_Field
            if (_formDataService is not null && _appConfig is { IsConfigured: true })
            {
                try
                {
                    var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
                    var newFormId = await _formDataService.CloneFormAsync(
                        sourceId, newCode, newName, _appConfig.TenantId, ct);

                    // Reload danh sách form để lấy record đầy đủ từ DB
                    await LoadDataSafeAsync();
                    return;
                }
                catch (Exception ex)
                {
                    LoadErrorMessage = $"Clone form thất bại: {ex.Message}";
                    LogLoadError(ex);
                    // Fallthrough → thêm tạm thời vào UI
                }
            }

            // Fallback khi chưa cấu hình DB
            var clone = new FormSummaryDto
            {
                FormId       = Forms.DefaultIfEmpty().Max(f => f?.FormId ?? 0) + 1,
                FormCode     = newCode,
                FormName     = newName,
                Version      = 1,
                Platform     = SelectedForm.Platform,
                TableName    = SelectedForm.TableName,
                SectionCount = SelectedForm.SectionCount,
                FieldCount   = SelectedForm.FieldCount,
                IsActive     = true,
                UpdatedAt    = DateTime.Now,
                UpdatedBy    = "admin"
            };
            Forms.Add(clone);
            RebuildTableOptions();
            RaiseStatistics();
        });
    }

    private void NavigateToEditor(int formId)
    {
        var p = new NavigationParameters { { "formId", formId } };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormEditor, p);
    }

    // ── Helpers ──────────────────────────────────────────────

    private void RaiseStatistics()
    {
        RaisePropertyChanged(nameof(TotalForms));
        RaisePropertyChanged(nameof(ActiveCount));
        RaisePropertyChanged(nameof(InactiveCount));
        RaisePropertyChanged(nameof(FilteredCount));
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
