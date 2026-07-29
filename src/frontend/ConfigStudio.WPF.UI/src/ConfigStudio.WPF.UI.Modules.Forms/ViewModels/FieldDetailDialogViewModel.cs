// File    : FieldDetailDialogViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel popup "Chi tiết cấu hình" — sửa toàn bộ field của 1 dòng (Cột/Action/Filter
//           trong màn Quản Lý View) kèm hướng dẫn cấu hình từng field. Bấm Lưu → ghi ngược vào
//           record gốc (cùng instance đang bind trên lưới inline) qua FieldDetailRowVm.Commit.
//           Có thêm banner "Gợi ý bước tiếp theo" tính lại MỖI LẦN 1 field đổi giá trị, dựa vào
//           HintCalculator do caller truyền vào (logic phụ thuộc field khác nhau theo Cột/Action/Filter).

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.ViewModels;
using Prism.Commands;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// Popup Prism (IDialogAware). Tham số vào (DialogParameters):
///   - "title"          (string)                                         — tiêu đề popup.
///   - "rows"           (IReadOnlyList&lt;FieldDetailRowVm&gt;)           — danh sách field hiển thị + sửa.
///   - "hintCalculator" (Func&lt;IReadOnlyList&lt;FieldDetailRowVm&gt;, string&gt;) — tùy chọn, tính
///     câu gợi ý bước tiếp theo từ giá trị hiện tại của toàn bộ row; gọi lại mỗi khi 1 row đổi giá trị.
/// Trả về ButtonResult.OK khi Áp dụng (đã Commit từng row vào record gốc); Cancel khi Hủy.
/// </summary>
public sealed class FieldDetailDialogViewModel : ViewModelBase, IDialogAware
{
    private Func<IReadOnlyList<FieldDetailRowVm>, string>? _hintCalculator;

    public FieldDetailDialogViewModel()
    {
        SaveCommand = new DelegateCommand(ExecuteSave);
        CancelCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));
    }

    // ── IDialogAware ─────────────────────────────────────────
    public DialogCloseListener RequestClose { get; set; }

    private string _title = "Chi tiết cấu hình";
    public string Title { get => _title; private set => SetProperty(ref _title, value); }

    public bool CanCloseDialog() => true;

    /// <summary>Gỡ đăng ký PropertyChanged khỏi từng row khi popup đóng — tránh rò rỉ tham chiếu.</summary>
    public void OnDialogClosed()
    {
        foreach (var row in Rows) row.PropertyChanged -= OnRowPropertyChanged;
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        Title = parameters.GetValue<string>("title") ?? "Chi tiết cấu hình";
        _hintCalculator = parameters.GetValue<Func<IReadOnlyList<FieldDetailRowVm>, string>>("hintCalculator");

        Rows.Clear();
        var rows = parameters.GetValue<IReadOnlyList<FieldDetailRowVm>>("rows");
        if (rows is not null)
            foreach (var r in rows) { Rows.Add(r); r.PropertyChanged += OnRowPropertyChanged; }

        // Nhóm theo Section (spec form rule §1 — hierarchy bắt buộc); PropertyGroupDescription
        // giữ đúng thứ tự nhóm xuất hiện đầu tiên trong Rows, không sắp lại theo alphabet.
        RowsView = CollectionViewSource.GetDefaultView(Rows);
        RowsView.GroupDescriptions.Clear();
        RowsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(FieldDetailRowVm.Section)));

        RecomputeDynamicState();
    }

    /// <summary>Danh sách field của dòng đang sửa — đổ 1 lần khi mở popup.</summary>
    public ObservableCollection<FieldDetailRowVm> Rows { get; } = [];

    private ICollectionView? _rowsView;
    /// <summary>View nhóm <see cref="Rows"/> theo Section — dùng cho XAML thay vì bind thẳng Rows.</summary>
    public ICollectionView? RowsView { get => _rowsView; private set => SetProperty(ref _rowsView, value); }

    private string _nextStepHint = "";
    /// <summary>Câu gợi ý bước cấu hình tiếp theo, tính lại mỗi khi 1 field đổi giá trị.</summary>
    public string NextStepHint { get => _nextStepHint; private set => SetProperty(ref _nextStepHint, value); }

    public bool HasNextStepHint => !string.IsNullOrWhiteSpace(NextStepHint);

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }

    /// <summary>Field bất kỳ đổi giá trị → tính lại trạng thái phụ thuộc và gợi ý.</summary>
    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FieldDetailRowVm.TextValue)
            or nameof(FieldDetailRowVm.BoolValue)
            or nameof(FieldDetailRowVm.NumberValue))
            RecomputeDynamicState();
    }

    private void RecomputeDynamicState()
    {
        foreach (var row in Rows) row.RefreshEnabled(Rows);
        RecomputeHint();
    }

    private void RecomputeHint()
    {
        if (_hintCalculator is null) { NextStepHint = ""; return; }
        try { NextStepHint = _hintCalculator(Rows) ?? ""; }
        catch { NextStepHint = ""; }   // gợi ý là phụ trợ — lỗi tính toán không được chặn popup
        RaisePropertyChanged(nameof(HasNextStepHint));
    }

    /// <summary>Áp dụng từng row (bỏ qua Kind = ReadOnly, không có Commit) vào record gốc rồi đóng popup.</summary>
    private void ExecuteSave()
    {
        foreach (var row in Rows)
            row.Commit?.Invoke(row);
        RequestClose.Invoke(new DialogResult(ButtonResult.OK));
    }
}
