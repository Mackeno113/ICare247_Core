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
/// DataGrid key/language matrix, filter theo table prefix + module, inline edit + auto-save to DB.
/// Khi DB đã cấu hình → load dữ liệu thật qua II18nDataService.
/// Khi chưa cấu hình → hiển thị danh sách rỗng.
/// </summary>
public sealed class I18nManagerViewModel : ViewModelBase, INavigationAware
{
    private readonly II18nDataService? _i18nService;
    private readonly IAppConfigService? _appConfig;
    private CancellationTokenSource _cts = new();

    // ── Data ──────────────────────────────────────────────────
    public ObservableCollection<I18nEntryDto> Entries { get; } = [];

    private string _saveError = "";
    public string SaveError { get => _saveError; private set => SetProperty(ref _saveError, value); }
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
                RefreshFilter();
        }
    }

    // Danh sách table prefix (nhanvien, donhang...) lấy động từ keys đã load
    public ObservableCollection<string> TableOptions { get; } = ["Tất cả"];

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

    public List<string> ModuleOptions { get; } = ["Tất cả", "Field", "Form", "Rule", "Event", "System"];

    private string _moduleFilter = "Tất cả";
    public string ModuleFilter
    {
        get => _moduleFilter;
        set
        {
            if (SetProperty(ref _moduleFilter, value))
                RefreshFilter();
        }
    }

    private bool _showMissingOnly;
    public bool ShowMissingOnly
    {
        get => _showMissingOnly;
        set
        {
            if (SetProperty(ref _showMissingOnly, value))
                RefreshFilter();
        }
    }

    // ── Statistics ─────────────────────────────────────────────
    public int TotalEntries => Entries.Count;
    public int FilteredCount => EntriesView.Cast<object>().Count();
    public int MissingCount => Entries.Count(e => e.HasMissing);

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand GoBackCommand { get; }
    public DelegateCommand AddEntryCommand { get; }
    public DelegateCommand DeleteEntryCommand { get; }
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand ExportCommand { get; }
    public DelegateCommand ImportCommand { get; }

    /// <summary>Gọi từ code-behind khi cell vi-VN/en-US/ja-JP commit — save ngay vào DB.</summary>
    public DelegateCommand<CellSaveArgs> SaveCellCommand { get; }

    private IRegionNavigationJournal? _journal;

    public I18nManagerViewModel(II18nDataService? i18nService = null, IAppConfigService? appConfig = null)
    {
        _i18nService = i18nService;
        _appConfig   = appConfig;

        EntriesView        = CollectionViewSource.GetDefaultView(Entries);
        EntriesView.Filter = ApplyFilter;

        GoBackCommand      = new DelegateCommand(ExecuteGoBack, () => _journal?.CanGoBack == true)
                                 .ObservesProperty(() => CanGoBack);
        AddEntryCommand    = new DelegateCommand(ExecuteAddEntry);
        DeleteEntryCommand = new DelegateCommand(ExecuteDeleteEntry, () => SelectedEntry is not null);
        RefreshCommand     = new DelegateCommand(async () => await LoadDataAsync());
        ExportCommand      = new DelegateCommand(ExecuteExport);
        ImportCommand      = new DelegateCommand(ExecuteImport);
        SaveCellCommand    = new DelegateCommand<CellSaveArgs>(async args => await ExecuteSaveCellAsync(args));
    }

    // Trigger RaiseCanExecuteChanged cho GoBackCommand
    public bool CanGoBack => _journal?.CanGoBack == true;

    private void ExecuteGoBack()
    {
        if (_journal?.CanGoBack == true)
            _journal.GoBack();
    }

    // ── INavigationAware ─────────────────────────────────────

    public async void OnNavigatedTo(NavigationContext navigationContext)
    {
        _journal = navigationContext.NavigationService.Journal;
        RaisePropertyChanged(nameof(CanGoBack));

        // Nếu navigate từ FieldConfig/FormEditor kèm tableCode → auto-filter về table đó
        var tableCode = navigationContext.Parameters.GetValue<string>("tableCode") ?? "";
        if (!string.IsNullOrWhiteSpace(tableCode))
            _pendingTableFilter = tableCode.ToLowerInvariant();

        await LoadDataAsync();
    }

    private string _pendingTableFilter = "";

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
    }

    // ── Load data ────────────────────────────────────────────

    private async Task LoadDataAsync()
    {
        if (_i18nService is not null && _appConfig is { IsConfigured: true })
            await LoadFromDatabaseAsync();
        else
        {
            // Chưa cấu hình DB → danh sách rỗng
            Entries.Clear();
            RebuildTableOptions();
            RefreshFilter();
        }
    }

    /// <summary>
    /// Đọc Sys_Resource từ DB, map sang UI DTO.
    /// Module suy từ segment thứ 2 của ResourceKey (tablecode.field.x → Field, tablecode.section.x → Form).
    /// </summary>
    private async Task LoadFromDatabaseAsync()
    {
        try
        {
            var ct      = _cts.Token;
            var records = await _i18nService!.GetResourcesAsync(ct);

            Entries.Clear();
            var id = 1;
            foreach (var r in records)
            {
                Entries.Add(new I18nEntryDto
                {
                    ResourceId  = id++,
                    ResourceKey = r.ResourceKey,
                    Module      = InferModule(r.ResourceKey),
                    TablePrefix = ExtractTablePrefix(r.ResourceKey),
                    ViVn        = r.ViVn ?? "",
                    EnUs        = r.EnUs ?? "",
                    JaJp        = r.JaJp ?? ""
                });
            }

            RebuildTableOptions();
            ApplyPendingFilter();
            RefreshFilter();
        }
        catch (OperationCanceledException) { /* Navigation away */ }
        catch
        {
            Entries.Clear();
            RebuildTableOptions();
            RefreshFilter();
        }
    }

    // ── Table option helpers ─────────────────────────────────

    /// <summary>Lấy prefix đầu tiên của key: "nhanvien.field.x" → "nhanvien".</summary>
    private static string ExtractTablePrefix(string key)
    {
        var dot = key.IndexOf('.');
        return dot > 0 ? key[..dot] : key;
    }

    /// <summary>
    /// Suy module từ segment thứ 2 của key (tablecode.{segment}.fieldcode).
    /// "nhanvien.field.x" → "Field", "nhanvien.section.x" → "Form".
    /// </summary>
    private static string InferModule(string key)
    {
        var parts = key.Split('.');
        if (parts.Length < 2) return "System";
        return parts[1].ToLowerInvariant() switch
        {
            "field"   => "Field",
            "section" => "Form",
            "rule"    => "Rule",
            "event"   => "Event",
            // Legacy prefix format
            "lbl" or "ph" => "Field",
            "err"         => "Rule",
            "sec"         => "Form",
            "evt"         => "Event",
            _             => "System"
        };
    }

    private void RebuildTableOptions()
    {
        var prefixes = Entries
            .Select(e => e.TablePrefix)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p)
            .ToList();

        TableOptions.Clear();
        TableOptions.Add("Tất cả");
        foreach (var p in prefixes)
            TableOptions.Add(p);
    }

    private void ApplyPendingFilter()
    {
        if (string.IsNullOrEmpty(_pendingTableFilter)) return;
        // Chọn đúng prefix nếu có trong list
        var match = TableOptions.FirstOrDefault(o =>
            o.Equals(_pendingTableFilter, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            TableFilter = match;
        _pendingTableFilter = "";
    }

    private void RefreshFilter()
    {
        EntriesView.Refresh();
        RaisePropertyChanged(nameof(FilteredCount));
    }

    // ── Inline edit → save DB ────────────────────────────────

    /// <summary>
    /// Gọi từ code-behind sau khi user commit cell.
    /// Map FieldName (tên property DTO) → language code, rồi save vào Sys_Resource.
    /// </summary>
    private async Task ExecuteSaveCellAsync(CellSaveArgs args)
    {
        if (_i18nService is null || _appConfig is not { IsConfigured: true }) return;
        if (string.IsNullOrWhiteSpace(args.ResourceKey)) return;

        // Map tên property của I18nEntryDto → language code lưu trong DB
        var lang = args.FieldName switch
        {
            "ViVn" => "vi",
            "EnUs" => "en",
            "JaJp" => "ja",
            _      => null
        };
        if (lang is null) return;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _i18nService.SaveResourceAsync(args.ResourceKey, lang, args.Value ?? "", cts.Token);
        }
        catch (Exception ex)
        {
            SaveError = $"Lưu thất bại: {ex.Message}";
        }
    }

    // ── Filter ───────────────────────────────────────────────

    private bool ApplyFilter(object obj)
    {
        if (obj is not I18nEntryDto entry) return false;

        if (ShowMissingOnly && !entry.HasMissing) return false;

        if (TableFilter != "Tất cả"
            && !entry.TablePrefix.Equals(TableFilter, StringComparison.OrdinalIgnoreCase))
            return false;

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
        var prefix = TableFilter != "Tất cả" ? TableFilter : "new";
        var newId  = Entries.Count > 0 ? Entries.Max(e => e.ResourceId) + 1 : 1;
        var entry  = new I18nEntryDto
        {
            ResourceId  = newId,
            ResourceKey = $"{prefix}.field.key{newId}",
            Module      = "Field",
            TablePrefix = prefix,
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
        RefreshFilter();
        RaisePropertyChanged(nameof(MissingCount));
    }

    private void ExecuteExport() { /* TODO(phase2): Export sang CSV/JSON */ }
    private void ExecuteImport() { /* TODO(phase2): Import từ CSV/JSON   */ }
}

/// <summary>Args truyền từ code-behind khi cell commit. FieldName là tên property của DTO trên grid.</summary>
public sealed record CellSaveArgs(string ResourceKey, string FieldName, string? Value);
