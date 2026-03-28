// File    : ColumnPickerDialogViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho ColumnPickerDialog — tìm kiếm và chọn column từ Sys_Column.
//           Nhận danh sách AvailableColumns qua DialogParameters, trả về column được chọn.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// Dialog chọn column từ danh sách <c>Sys_Column</c> của bảng hiện tại.
/// Hỗ trợ tìm kiếm realtime theo ColumnCode hoặc DataType.
/// Trả <see cref="ButtonResult.OK"/> + parameter <c>"selectedColumn"</c> khi user chọn.
/// </summary>
public sealed class ColumnPickerDialogViewModel : ViewModelBase, IDialogAware
{
    // ── IDialogAware ─────────────────────────────────────────
    public DialogCloseListener RequestClose { get; set; }
    public string Title => "Chọn Column";

    // ── Data ─────────────────────────────────────────────────

    /// <summary>Toàn bộ columns nhận từ caller.</summary>
    private List<ColumnInfoDto> _allColumns = [];

    /// <summary>Columns sau khi filter theo SearchText.</summary>
    public ObservableCollection<ColumnInfoDto> FilteredColumns { get; } = [];

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

    private ColumnInfoDto? _selectedColumn;
    public ColumnInfoDto? SelectedColumn
    {
        get => _selectedColumn;
        set
        {
            if (SetProperty(ref _selectedColumn, value))
                SelectCommand.RaiseCanExecuteChanged();
        }
    }

    // ── Commands ─────────────────────────────────────────────

    public DelegateCommand SelectCommand  { get; }
    public DelegateCommand CancelCommand  { get; }

    public ColumnPickerDialogViewModel()
    {
        SelectCommand = new DelegateCommand(ExecuteSelect, () => SelectedColumn is not null)
            .ObservesProperty(() => SelectedColumn);
        CancelCommand = new DelegateCommand(ExecuteCancel);
    }

    // ── IDialogAware implementation ───────────────────────────

    public bool CanCloseDialog() => true;
    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        // Nhận danh sách columns từ caller (FieldConfigViewModel)
        if (parameters.TryGetValue("columns", out IEnumerable<ColumnInfoDto>? cols) && cols is not null)
            _allColumns = cols.ToList();

        ApplyFilter();
    }

    // ── Helpers ──────────────────────────────────────────────

    private void ApplyFilter()
    {
        FilteredColumns.Clear();

        var term = SearchText.Trim();

        var filtered = string.IsNullOrEmpty(term)
            ? _allColumns
            : _allColumns.Where(c =>
                c.ColumnCode.Contains(term, StringComparison.OrdinalIgnoreCase)
                || c.DataType.Contains(term, StringComparison.OrdinalIgnoreCase));

        foreach (var c in filtered)
            FilteredColumns.Add(c);

        // Giữ selection nếu vẫn trong list, reset nếu bị lọc ra
        if (SelectedColumn is not null && !FilteredColumns.Contains(SelectedColumn))
            SelectedColumn = null;
    }

    private void ExecuteSelect()
    {
        if (SelectedColumn is null) return;
        var result = new DialogResult(ButtonResult.OK);
        result.Parameters.Add("selectedColumn", SelectedColumn);
        RequestClose.Invoke(result);
    }

    private void ExecuteCancel()
        => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
}
