// File    : AutoGenerateFieldsDialogViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho dialog Auto-generate Fields — đọc cấu trúc cột Target DB,
//           cho user chọn cột cần tạo field, trả kết quả về FormEditorViewModel.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// Dialog cho phép user chọn các cột từ Target DB để auto-generate Ui_Field.
/// Nhận: schemaName, tableName, sections (danh sách section hiện có).
/// Trả về: OK + selectedColumns (ColumnSchemaDto[]) + targetSectionCode (string).
/// </summary>
public sealed class AutoGenerateFieldsDialogViewModel : ViewModelBase, IDialogAware
{
    private readonly ISchemaInspectorService _schemaInspector;
    private readonly IAppConfigService _appConfig;

    // ── IDialogAware ─────────────────────────────────────────
    public string Title => $"Tạo Fields tự động — {_tableName}";
    public DialogCloseListener RequestClose { get; set; }

    // ── Input từ caller ───────────────────────────────────────
    private string _schemaName = "dbo";
    private string _tableName  = "";

    // ── Dữ liệu cột ──────────────────────────────────────────
    /// <summary>Tất cả cột từ INFORMATION_SCHEMA — bao gồm cả ShouldSkip.</summary>
    public ObservableCollection<AutoGenerateColumnItem> AllColumns { get; } = [];

    /// <summary>Chỉ các cột không phải PK/Identity — hiển thị trong danh sách.</summary>
    public ObservableCollection<AutoGenerateColumnItem> VisibleColumns { get; } = [];

    // ── Section đích ──────────────────────────────────────────
    /// <summary>Danh sách section hiện có để user chọn đích.</summary>
    public ObservableCollection<SectionOptionItem> AvailableSections { get; } = [];

    private SectionOptionItem? _selectedSection;
    public SectionOptionItem? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (SetProperty(ref _selectedSection, value))
                GenerateCommand.RaiseCanExecuteChanged();
        }
    }

    // ── UI state ──────────────────────────────────────────────
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    private string _loadError = "";
    public string LoadError
    {
        get => _loadError;
        private set
        {
            if (SetProperty(ref _loadError, value))
                RaisePropertyChanged(nameof(HasLoadError));
        }
    }

    public bool HasLoadError => !string.IsNullOrEmpty(_loadError);

    // ── Thống kê ──────────────────────────────────────────────
    private int _selectedCount;
    /// <summary>Số cột đang được check để tạo field.</summary>
    public int SelectedCount
    {
        get => _selectedCount;
        private set
        {
            if (SetProperty(ref _selectedCount, value))
            {
                GenerateCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(SkippedCount));
            }
        }
    }

    /// <summary>Số cột bị ẩn (PK/Identity).</summary>
    public int SkippedCount => AllColumns.Count - VisibleColumns.Count;

    // ── Commands ──────────────────────────────────────────────
    public DelegateCommand SelectAllCommand { get; }
    public DelegateCommand DeselectAllCommand { get; }
    public DelegateCommand GenerateCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public AutoGenerateFieldsDialogViewModel(
        ISchemaInspectorService schemaInspector,
        IAppConfigService appConfig)
    {
        _schemaInspector = schemaInspector;
        _appConfig       = appConfig;

        SelectAllCommand   = new DelegateCommand(ExecuteSelectAll);
        DeselectAllCommand = new DelegateCommand(ExecuteDeselectAll);
        GenerateCommand    = new DelegateCommand(ExecuteGenerate, CanGenerate);
        CancelCommand      = new DelegateCommand(ExecuteCancel);
    }

    // ── IDialogAware ─────────────────────────────────────────

    public bool CanCloseDialog() => true;

    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        _schemaName = parameters.GetValue<string>("schemaName") ?? "dbo";
        _tableName  = parameters.GetValue<string>("tableName") ?? "";

        // NOTE: Nhận danh sách section để user chọn đích
        var sections = parameters.GetValue<IReadOnlyList<SectionOptionItem>>("sections")
                       ?? [];
        AvailableSections.Clear();
        foreach (var s in sections)
            AvailableSections.Add(s);

        // Chọn section đầu tiên mặc định (nếu có)
        SelectedSection = AvailableSections.FirstOrDefault();

        RaisePropertyChanged(nameof(Title));

        // Load columns từ Target DB
        _ = LoadColumnsAsync();
    }

    // ── Load cột từ Target DB ────────────────────────────────

    private async Task LoadColumnsAsync()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
        {
            LoadError = "Chưa chọn bảng (Table) cho form.";
            return;
        }

        // ── Kiểm tra Target DB đã cấu hình chưa ──────────────
        if (!_appConfig.IsTargetConfigured
         || string.IsNullOrWhiteSpace(_appConfig.TargetConnectionString))
        {
            LoadError = "Chưa cấu hình Target Database.\nVào Settings → Target Database để nhập connection string.";
            return;
        }

        IsLoading = true;
        LoadError = "";
        AllColumns.Clear();
        VisibleColumns.Clear();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var columns = await _schemaInspector.GetColumnsAsync(
                _appConfig.TargetConnectionString,
                _schemaName,
                _tableName,
                cts.Token);

            if (columns.Count == 0)
            {
                LoadError = $"Không tìm thấy bảng '{_schemaName}.{_tableName}' trong Target DB,\nhoặc bảng không có cột nào.";
                return;
            }

            // ── Tạo items, subscribe IsSelected changed ───────
            foreach (var col in columns)
            {
                var item = new AutoGenerateColumnItem(col);
                item.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(AutoGenerateColumnItem.IsSelected))
                        RefreshSelectedCount();
                };
                AllColumns.Add(item);

                // Chỉ hiển thị cột không phải PK/Identity
                if (!col.ShouldSkip)
                    VisibleColumns.Add(item);
            }

            RefreshSelectedCount();
        }
        catch (OperationCanceledException)
        {
            LoadError = "Timeout: không thể kết nối tới Target DB sau 15 giây.";
        }
        catch (Exception ex)
        {
            LoadError = $"Lỗi khi đọc schema: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Command handlers ─────────────────────────────────────

    private void ExecuteSelectAll()
    {
        foreach (var item in VisibleColumns)
            item.IsSelected = true;
    }

    private void ExecuteDeselectAll()
    {
        foreach (var item in VisibleColumns)
            item.IsSelected = false;
    }

    private bool CanGenerate()
        => SelectedCount > 0 && SelectedSection is not null && !IsLoading;

    private void ExecuteGenerate()
    {
        // ── Lấy danh sách cột đã chọn (không phải PK/Identity) ──
        var selectedColumns = VisibleColumns
            .Where(i => i.IsSelected)
            .Select(i => i.Column)
            .ToList();

        if (selectedColumns.Count == 0) return;

        var result = new DialogResult(ButtonResult.OK);
        result.Parameters.Add("selectedColumns", (IReadOnlyList<ColumnSchemaDto>)selectedColumns);
        result.Parameters.Add("targetSectionCode", SelectedSection?.Code ?? "");
        RequestClose.Invoke(result);
    }

    private void ExecuteCancel()
        => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));

    // ── Helpers ──────────────────────────────────────────────

    private void RefreshSelectedCount()
        => SelectedCount = VisibleColumns.Count(i => i.IsSelected);
}

/// <summary>
/// Item đơn giản đại diện cho một section trong danh sách chọn đích.
/// </summary>
public sealed record SectionOptionItem(string Code, string DisplayName)
{
    public string Label => $"{DisplayName}  [{Code}]";
}
