// File    : ColumnPickerDialogViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho ColumnPickerDialog — tìm kiếm và chọn column từ Sys_Column.
//           Hỗ trợ 2 chế độ: single-select (FieldConfig) và multi-select (ViewManager),
//           kèm khóa các cột đã có trong danh sách.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// Dialog chọn column từ danh sách <c>Sys_Column</c> của bảng hiện tại.
/// Tham số vào (DialogParameters):
///   - "columns"     (IEnumerable&lt;ColumnInfoDto&gt;, bắt buộc) — danh sách cột nguồn.
///   - "multiSelect" (bool, tùy chọn) — bật chọn nhiều (mặc định false = single).
///   - "usedColumns" (IEnumerable&lt;string&gt;, tùy chọn) — ColumnCode đã có trong danh sách → khóa.
/// Trả về (<see cref="ButtonResult.OK"/>):
///   - single: parameter "selectedColumn"  = <see cref="ColumnInfoDto"/>.
///   - multi : parameter "selectedColumns" = List&lt;<see cref="ColumnInfoDto"/>&gt;.
/// </summary>
public sealed class ColumnPickerDialogViewModel : ViewModelBase, IDialogAware
{
    // ── IDialogAware ─────────────────────────────────────────
    public DialogCloseListener RequestClose { get; set; }
    public string Title => "Chọn Column";

    // ── Data ─────────────────────────────────────────────────

    /// <summary>Toàn bộ dòng chọn nhận từ caller (đã bọc cờ đã-dùng).</summary>
    private List<ColumnPickItem> _allColumns = [];

    /// <summary>Dòng chọn sau khi filter theo SearchText.</summary>
    public ObservableCollection<ColumnPickItem> FilteredColumns { get; } = [];

    private bool _multiSelect;
    /// <summary>true = cho chọn nhiều cột (hiện checkbox); false = single-select.</summary>
    public bool MultiSelect
    {
        get => _multiSelect;
        private set
        {
            if (SetProperty(ref _multiSelect, value))
                RaisePropertyChanged(nameof(IsSingleSelect));
        }
    }

    /// <summary>Nghịch đảo <see cref="MultiSelect"/> — dùng cho binding visibility.</summary>
    public bool IsSingleSelect => !MultiSelect;

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    private ColumnPickItem? _selectedItem;
    /// <summary>Dòng đang highlight (chế độ single-select).</summary>
    public ColumnPickItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
                SelectCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>Số cột đang tick chọn (chế độ multi-select).</summary>
    public int SelectedCount => _allColumns.Count(c => c.IsSelected);

    /// <summary>Nhãn nút xác nhận: "Chọn" hoặc "Chọn (N)" khi multi-select.</summary>
    public string SelectButtonText => MultiSelect ? $"Chọn ({SelectedCount})" : "Chọn";

    // ── Commands ─────────────────────────────────────────────

    public DelegateCommand SelectCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public ColumnPickerDialogViewModel()
    {
        SelectCommand = new DelegateCommand(ExecuteSelect, CanSelect);
        CancelCommand = new DelegateCommand(ExecuteCancel);
    }

    // ── IDialogAware implementation ───────────────────────────

    public bool CanCloseDialog() => true;
    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        MultiSelect = parameters.TryGetValue("multiSelect", out bool multi) && multi;

        var used = parameters.TryGetValue("usedColumns", out IEnumerable<string>? u) && u is not null
            ? new HashSet<string>(u, StringComparer.OrdinalIgnoreCase)
            : [];

        if (parameters.TryGetValue("columns", out IEnumerable<ColumnInfoDto>? cols) && cols is not null)
            _allColumns = cols
                .Select(c => CreateItem(c, used.Contains(c.ColumnCode)))
                .ToList();

        RaisePropertyChanged(nameof(SelectButtonText));
        ApplyFilter();
    }

    // ── Helpers ──────────────────────────────────────────────

    /// <summary>Tạo dòng chọn và lắng nghe tick để cập nhật đếm/nút.</summary>
    private ColumnPickItem CreateItem(ColumnInfoDto column, bool isUsed)
    {
        var item = new ColumnPickItem(column, isUsed);
        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(ColumnPickItem.IsSelected)) return;
            RaisePropertyChanged(nameof(SelectedCount));
            RaisePropertyChanged(nameof(SelectButtonText));
            SelectCommand.RaiseCanExecuteChanged();
        };
        return item;
    }

    private void ApplyFilter()
    {
        FilteredColumns.Clear();

        var term = SearchText.Trim();
        var filtered = string.IsNullOrEmpty(term)
            ? _allColumns
            : _allColumns.Where(c =>
                c.Column.ColumnCode.Contains(term, StringComparison.OrdinalIgnoreCase)
                || c.Column.DataType.Contains(term, StringComparison.OrdinalIgnoreCase));

        foreach (var c in filtered)
            FilteredColumns.Add(c);

        // Giữ highlight nếu vẫn trong list, reset nếu bị lọc ra
        if (SelectedItem is not null && !FilteredColumns.Contains(SelectedItem))
            SelectedItem = null;
    }

    /// <summary>Cho phép xác nhận: multi cần ≥1 tick; single cần đã chọn dòng khả dụng.</summary>
    private bool CanSelect()
        => MultiSelect
            ? SelectedCount > 0
            : SelectedItem is { IsAlreadyUsed: false };

    private void ExecuteSelect()
    {
        var result = new DialogResult(ButtonResult.OK);

        if (MultiSelect)
        {
            var picked = _allColumns
                .Where(c => c.IsSelected && !c.IsAlreadyUsed)
                .Select(c => c.Column)
                .ToList();
            if (picked.Count == 0) return;
            result.Parameters.Add("selectedColumns", picked);
        }
        else
        {
            if (SelectedItem is null or { IsAlreadyUsed: true }) return;
            result.Parameters.Add("selectedColumn", SelectedItem.Column);
        }

        RequestClose.Invoke(result);
    }

    private void ExecuteCancel()
        => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
}
