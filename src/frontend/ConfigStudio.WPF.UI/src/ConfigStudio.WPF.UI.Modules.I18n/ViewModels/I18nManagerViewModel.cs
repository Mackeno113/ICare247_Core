// File    : I18nManagerViewModel.cs
// Module  : I18n
// Layer   : Presentation
// Purpose : ViewModel cho màn hình i18n Manager (Screen 10) — quản lý resource key/language matrix.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.I18n.Models;
using Prism.Commands;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.I18n.ViewModels;

/// <summary>
/// ViewModel cho màn hình i18n Manager (Screen 10).
/// DataGrid key/language matrix, filter theo module, search, hiện missing translations.
/// Khi DB đã cấu hình → load dữ liệu thật qua II18nDataService.
/// Khi chưa cấu hình → fallback mock data.
/// </summary>
public sealed class I18nManagerViewModel : ViewModelBase, INavigationAware
{
    private readonly II18nDataService? _i18nService;
    private readonly IAppConfigService? _appConfig;
    private CancellationTokenSource _cts = new();

    // ── Data ──────────────────────────────────────────────────
    public ObservableCollection<I18nEntryDto> Entries { get; } = [];
    public ICollectionView EntriesView { get; }

    private I18nEntryDto? _selectedEntry;
    public I18nEntryDto? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
                DeleteEntryCommand.RaiseCanExecuteChanged();
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
                EntriesView.Refresh();
                RaisePropertyChanged(nameof(FilteredCount));
            }
        }
    }

    public List<string> ModuleOptions { get; } = ["Tất cả", "Form", "Field", "Rule", "Event", "System"];

    private string _moduleFilter = "Tất cả";
    public string ModuleFilter
    {
        get => _moduleFilter;
        set
        {
            if (SetProperty(ref _moduleFilter, value))
            {
                EntriesView.Refresh();
                RaisePropertyChanged(nameof(FilteredCount));
            }
        }
    }

    private bool _showMissingOnly;
    public bool ShowMissingOnly
    {
        get => _showMissingOnly;
        set
        {
            if (SetProperty(ref _showMissingOnly, value))
            {
                EntriesView.Refresh();
                RaisePropertyChanged(nameof(FilteredCount));
            }
        }
    }

    // ── Statistics ─────────────────────────────────────────────
    public int TotalEntries => Entries.Count;
    public int FilteredCount => EntriesView.Cast<object>().Count();
    public int MissingCount => Entries.Count(e => e.HasMissing);

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand AddEntryCommand { get; }
    public DelegateCommand DeleteEntryCommand { get; }
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand ExportCommand { get; }
    public DelegateCommand ImportCommand { get; }

    public I18nManagerViewModel(II18nDataService? i18nService = null, IAppConfigService? appConfig = null)
    {
        _i18nService = i18nService;
        _appConfig = appConfig;

        EntriesView = CollectionViewSource.GetDefaultView(Entries);
        EntriesView.Filter = ApplyFilter;

        AddEntryCommand = new DelegateCommand(ExecuteAddEntry);
        DeleteEntryCommand = new DelegateCommand(ExecuteDeleteEntry, () => SelectedEntry is not null);
        RefreshCommand = new DelegateCommand(async () => await LoadDataAsync());
        ExportCommand = new DelegateCommand(ExecuteExport);
        ImportCommand = new DelegateCommand(ExecuteImport);
    }

    // ── INavigationAware ─────────────────────────────────────

    public async void OnNavigatedTo(NavigationContext navigationContext) => await LoadDataAsync();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
    }

    // ── Load data (DB hoặc mock) ─────────────────────────────

    private async Task LoadDataAsync()
    {
        if (_i18nService is not null && _appConfig is { IsConfigured: true })
        {
            await LoadFromDatabaseAsync();
        }
        else
        {
            LoadMockData();
        }
    }

    /// <summary>
    /// Đọc Sys_Resource (pivoted) từ DB, map sang UI DTO.
    /// Module được suy từ prefix của ResourceKey (lbl.→Field, err.→Rule, sec.→Form, evt.→Event).
    /// </summary>
    private async Task LoadFromDatabaseAsync()
    {
        try
        {
            var ct = _cts.Token;
            var records = await _i18nService!.GetResourcesAsync(ct);

            Entries.Clear();
            var id = 1;
            foreach (var r in records)
            {
                Entries.Add(new I18nEntryDto
                {
                    ResourceId = id++,
                    ResourceKey = r.ResourceKey,
                    Module = InferModule(r.ResourceKey),
                    ViVn = r.ViVn ?? "",
                    EnUs = r.EnUs ?? "",
                    JaJp = r.JaJp ?? ""
                });
            }

            RaisePropertyChanged(nameof(TotalEntries));
            RaisePropertyChanged(nameof(FilteredCount));
            RaisePropertyChanged(nameof(MissingCount));
        }
        catch (OperationCanceledException) { /* Navigation away */ }
        catch
        {
            // Fallback mock khi lỗi DB
            LoadMockData();
        }
    }

    /// <summary>
    /// Suy module từ prefix của resource key.
    /// </summary>
    private static string InferModule(string key) => key.Split('.')[0] switch
    {
        "lbl" or "ph" => "Field",
        "err" => "Rule",
        "sec" => "Form",
        "evt" => "Event",
        _ => "System"
    };

    // ── Load mock data ───────────────────────────────────────

    private void LoadMockData()
    {
        Entries.Clear();

        // ── Label keys ───────────────────────────────────────
        Entries.Add(new I18nEntryDto { ResourceId = 1, ResourceKey = "lbl.madonhang", Module = "Field", ViVn = "Mã Đơn Hàng", EnUs = "Order Code", JaJp = "注文コード" });
        Entries.Add(new I18nEntryDto { ResourceId = 2, ResourceKey = "lbl.ngaydathang", Module = "Field", ViVn = "Ngày Đặt Hàng", EnUs = "Order Date", JaJp = "注文日" });
        Entries.Add(new I18nEntryDto { ResourceId = 3, ResourceKey = "lbl.trangthai", Module = "Field", ViVn = "Trạng Thái", EnUs = "Status", JaJp = "ステータス" });
        Entries.Add(new I18nEntryDto { ResourceId = 4, ResourceKey = "lbl.nhacungcap", Module = "Field", ViVn = "Nhà Cung Cấp", EnUs = "Supplier", JaJp = "サプライヤー" });
        Entries.Add(new I18nEntryDto { ResourceId = 5, ResourceKey = "lbl.soluong", Module = "Field", ViVn = "Số Lượng", EnUs = "Quantity", JaJp = "数量" });
        Entries.Add(new I18nEntryDto { ResourceId = 6, ResourceKey = "lbl.dongia", Module = "Field", ViVn = "Đơn Giá", EnUs = "Unit Price", JaJp = "単価" });
        Entries.Add(new I18nEntryDto { ResourceId = 7, ResourceKey = "lbl.thanhtien", Module = "Field", ViVn = "Thành Tiền", EnUs = "Total", JaJp = "合計" });
        Entries.Add(new I18nEntryDto { ResourceId = 8, ResourceKey = "lbl.lydotuchoi", Module = "Field", ViVn = "Lý Do Từ Chối", EnUs = "Rejection Reason", JaJp = "" });

        // ── Placeholder keys ─────────────────────────────────
        Entries.Add(new I18nEntryDto { ResourceId = 9, ResourceKey = "ph.soluong", Module = "Field", ViVn = "Nhập số lượng", EnUs = "Enter quantity", JaJp = "数量を入力" });
        Entries.Add(new I18nEntryDto { ResourceId = 10, ResourceKey = "ph.search", Module = "System", ViVn = "Tìm kiếm...", EnUs = "Search...", JaJp = "検索..." });

        // ── Error keys ───────────────────────────────────────
        Entries.Add(new I18nEntryDto { ResourceId = 11, ResourceKey = "err.fld.req", Module = "Rule", ViVn = "Trường bắt buộc", EnUs = "Field is required", JaJp = "必須項目です" });
        Entries.Add(new I18nEntryDto { ResourceId = 12, ResourceKey = "err.sl.range", Module = "Rule", ViVn = "Số lượng phải từ 1-9999", EnUs = "Quantity must be 1-9999", JaJp = "" });
        Entries.Add(new I18nEntryDto { ResourceId = 13, ResourceKey = "err.sl.exceed", Module = "Rule", ViVn = "Số lượng vượt giới hạn", EnUs = "Quantity exceeds limit", JaJp = "数量が上限を超えています" });

        // ── Section/Form keys ────────────────────────────────
        Entries.Add(new I18nEntryDto { ResourceId = 14, ResourceKey = "sec.general", Module = "Form", ViVn = "Thông Tin Chung", EnUs = "General Info", JaJp = "一般情報" });
        Entries.Add(new I18nEntryDto { ResourceId = 15, ResourceKey = "sec.detail", Module = "Form", ViVn = "Chi Tiết", EnUs = "Details", JaJp = "詳細" });
        Entries.Add(new I18nEntryDto { ResourceId = 16, ResourceKey = "sec.note", Module = "Form", ViVn = "Ghi Chú", EnUs = "Notes", JaJp = "" });

        // ── Event messages ───────────────────────────────────
        Entries.Add(new I18nEntryDto { ResourceId = 17, ResourceKey = "evt.recalc.ok", Module = "Event", ViVn = "Đã tính lại thành tiền", EnUs = "Total recalculated", JaJp = "合計が再計算されました" });

        RaisePropertyChanged(nameof(TotalEntries));
        RaisePropertyChanged(nameof(FilteredCount));
        RaisePropertyChanged(nameof(MissingCount));
    }

    // ── Filter ───────────────────────────────────────────────

    private bool ApplyFilter(object obj)
    {
        if (obj is not I18nEntryDto entry) return false;

        if (ShowMissingOnly && !entry.HasMissing) return false;

        if (ModuleFilter != "Tất cả" && entry.Module != ModuleFilter) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim();
            return entry.ResourceKey.Contains(q, StringComparison.OrdinalIgnoreCase)
                || entry.ViVn.Contains(q, StringComparison.OrdinalIgnoreCase)
                || entry.EnUs.Contains(q, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    // ── Command handlers ─────────────────────────────────────

    private void ExecuteAddEntry()
    {
        var newId = Entries.Count > 0 ? Entries.Max(e => e.ResourceId) + 1 : 1;
        var entry = new I18nEntryDto
        {
            ResourceId = newId,
            ResourceKey = $"new.key.{newId}",
            Module = "Form",
            ViVn = "", EnUs = "", JaJp = ""
        };
        Entries.Add(entry);
        SelectedEntry = entry;
        RaisePropertyChanged(nameof(TotalEntries));
        RaisePropertyChanged(nameof(MissingCount));
    }

    private void ExecuteDeleteEntry()
    {
        if (SelectedEntry is null) return;
        Entries.Remove(SelectedEntry);
        SelectedEntry = null;
        RaisePropertyChanged(nameof(TotalEntries));
        RaisePropertyChanged(nameof(FilteredCount));
        RaisePropertyChanged(nameof(MissingCount));
    }

    private void ExecuteExport()
    {
        // TODO(phase2): Export sang CSV/JSON
    }

    private void ExecuteImport()
    {
        // TODO(phase2): Import từ CSV/JSON
    }
}
