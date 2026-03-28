// File    : I18nManagerViewModel.cs
// Module  : I18n
// Layer   : Presentation
// Purpose : ViewModel cho màn hình i18n Manager (Screen 10) — quản lý resource key/language matrix.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.I18n.Models;
using Microsoft.Win32;
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
        ExportCommand      = new DelegateCommand(async () => await ExecuteExportAsync());
        ImportCommand      = new DelegateCommand(async () => await ExecuteImportAsync());
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

    // ── Export ───────────────────────────────────────────────

    private async Task ExecuteExportAsync()
    {
        if (Entries.Count == 0)
        {
            MessageBox.Show("Không có dữ liệu để xuất.", "Export",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Title    = "Xuất I18n",
            Filter   = "CSV files (*.csv)|*.csv|JSON files (*.json)|*.json",
            FileName = $"i18n_export_{DateTime.Now:yyyyMMdd_HHmm}"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
            if (ext == ".json")
                await ExportJsonAsync(dlg.FileName);
            else
                await ExportCsvAsync(dlg.FileName);

            MessageBox.Show($"Đã xuất {Entries.Count} entries sang:\n{dlg.FileName}",
                "Export thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Xuất thất bại: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportCsvAsync(string path)
    {
        var snapshot = Entries.ToList();
        await Task.Run(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("Key,VI,EN,JA");
            foreach (var e in snapshot)
                sb.AppendLine($"{CsvEscape(e.ResourceKey)},{CsvEscape(e.ViVn)},{CsvEscape(e.EnUs)},{CsvEscape(e.JaJp)}");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        });
    }

    private async Task ExportJsonAsync(string path)
    {
        var snapshot = Entries
            .Select(e => new { Key = e.ResourceKey, VI = e.ViVn, EN = e.EnUs, JA = e.JaJp })
            .ToList();
        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json, Encoding.UTF8);
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    // ── Import ───────────────────────────────────────────────

    private async Task ExecuteImportAsync()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Nhập I18n",
            Filter = "CSV files (*.csv)|*.csv|JSON files (*.json)|*.json|Tất cả (*.csv;*.json)|*.csv;*.json"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var ext  = Path.GetExtension(dlg.FileName).ToLowerInvariant();
            var rows = ext == ".json"
                ? await ParseJsonAsync(dlg.FileName)
                : await ParseCsvAsync(dlg.FileName);

            if (rows.Count == 0)
            {
                MessageBox.Show("File không có dữ liệu hoặc sai định dạng.", "Import",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Sẽ nhập {rows.Count} entries.\nCác key đã tồn tại sẽ bị ghi đè.\nTiếp tục?",
                "Xác nhận Import", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            // Merge vào Entries
            var added = 0; var updated = 0;
            foreach (var row in rows)
            {
                var existing = Entries.FirstOrDefault(e =>
                    e.ResourceKey.Equals(row.Key, StringComparison.OrdinalIgnoreCase));

                if (existing is not null)
                {
                    existing.ViVn = row.VI;
                    existing.EnUs = row.EN;
                    existing.JaJp = row.JA;
                    updated++;
                }
                else
                {
                    Entries.Add(new I18nEntryDto
                    {
                        ResourceId  = Entries.Count > 0 ? Entries.Max(e => e.ResourceId) + 1 : 1,
                        ResourceKey = row.Key,
                        Module      = InferModule(row.Key),
                        TablePrefix = ExtractTablePrefix(row.Key),
                        ViVn = row.VI, EnUs = row.EN, JaJp = row.JA
                    });
                    added++;
                }
            }

            // Persist vào DB nếu đã cấu hình
            if (_i18nService is not null && _appConfig is { IsConfigured: true })
                await PersistImportedAsync(rows);

            RebuildTableOptions();
            RefreshFilter();
            RaisePropertyChanged(nameof(TotalEntries));
            RaisePropertyChanged(nameof(MissingCount));

            MessageBox.Show($"Import hoàn tất: {added} mới, {updated} cập nhật.",
                "Import thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import thất bại: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task PersistImportedAsync(List<ImportRow> rows)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        foreach (var row in rows)
        {
            await _i18nService!.SaveResourceAsync(row.Key, "vi", row.VI, cts.Token);
            await _i18nService!.SaveResourceAsync(row.Key, "en", row.EN, cts.Token);
            await _i18nService!.SaveResourceAsync(row.Key, "ja", row.JA, cts.Token);
        }
    }

    private static async Task<List<ImportRow>> ParseCsvAsync(string path)
    {
        var lines  = await File.ReadAllLinesAsync(path, Encoding.UTF8);
        var result = new List<ImportRow>();
        foreach (var line in lines.Skip(1)) // bỏ header
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line);
            if (cols.Length < 4) continue;
            if (!string.IsNullOrWhiteSpace(cols[0]))
                result.Add(new ImportRow(cols[0], cols[1], cols[2], cols[3]));
        }
        return result;
    }

    /// <summary>RFC 4180 CSV parser — hỗ trợ field có dấu phẩy, nháy kép, xuống dòng.</summary>
    private static string[] ParseCsvLine(string line)
    {
        var fields  = new List<string>();
        var current = new StringBuilder();
        var inQuote = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuote)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else inQuote = false;
                }
                else current.Append(c);
            }
            else
            {
                if (c == '"') inQuote = true;
                else if (c == ',') { fields.Add(current.ToString()); current.Clear(); }
                else current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return [.. fields];
    }

    private static async Task<List<ImportRow>> ParseJsonAsync(string path)
    {
        var content = await File.ReadAllTextAsync(path, Encoding.UTF8);
        var rows    = JsonSerializer.Deserialize<List<JsonImportRow>>(content,
                          new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return rows?
            .Where(r => !string.IsNullOrWhiteSpace(r.Key))
            .Select(r => new ImportRow(r.Key!, r.VI ?? "", r.EN ?? "", r.JA ?? ""))
            .ToList() ?? [];
    }

    // ── Import helpers ────────────────────────────────────────

    private sealed record ImportRow(string Key, string VI, string EN, string JA);

    private sealed class JsonImportRow
    {
        public string? Key { get; set; }
        public string? VI  { get; set; }
        public string? EN  { get; set; }
        public string? JA  { get; set; }
    }
}

/// <summary>Args truyền từ code-behind khi cell commit. FieldName là tên property của DTO trên grid.</summary>
public sealed record CellSaveArgs(string ResourceKey, string FieldName, string? Value);
