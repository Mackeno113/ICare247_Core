// File    : FieldDetailRowVm.cs
// Module  : Data
// Layer   : Core
// Purpose : Một dòng SỬA ĐƯỢC trong popup "Chi tiết cấu hình" (FieldDetailDialog) — dùng chung
//           cho Cột/Actions/Bộ lọc của màn Quản Lý View. Value nhập ở đây chỉ ghi ngược vào record
//           gốc khi bấm Lưu (qua Commit), record gốc là CÙNG instance đang bind trên lưới inline
//           nên ghi xong phản ánh ngay ra lưới (không cần refresh).

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>Loại control hiển thị cho 1 dòng trong popup chi tiết.</summary>
public enum FieldEditorKind
{
    /// <summary>Ô nhập chữ tự do.</summary>
    Text,
    /// <summary>Ô nhập số nguyên DevExpress SpinEdit (dùng NumberValue).</summary>
    Number,
    /// <summary>Checkbox (dùng BoolValue).</summary>
    Checkbox,
    /// <summary>Combo chọn 1 trong <see cref="Options"/> (dùng TextValue).</summary>
    Combo,
    /// <summary>Chỉ hiển thị, không cho sửa ở popup — field có luồng sửa riêng (vd 🌐 dịch key,
    /// nút ↑↓ đổi thứ tự, combo lookup phức tạp lấy từ nguồn ngoài).</summary>
    ReadOnly
}

/// <summary>Một field trong popup chi tiết — nhãn, hướng dẫn, loại control, giá trị đang sửa.</summary>
public sealed class FieldDetailRowVm : BindableBase
{
    public required string Label { get; init; }
    public string Guide { get; init; } = "";
    public FieldEditorKind Kind { get; init; } = FieldEditorKind.Text;

    /// <summary>Tên nhóm nghiệp vụ hiển thị popup chi tiết (vd "Nguồn dữ liệu", "Sắp xếp & lọc") —
    /// popup nhóm field theo giá trị này (thứ tự nhóm = thứ tự xuất hiện đầu tiên trong Rows).</summary>
    public string Section { get; init; } = "";

    /// <summary>Định danh kỹ thuật ổn định (KHÔNG dịch, không hiển thị) để hàm tính gợi ý bước tiếp
    /// theo (<see cref="FieldDetailDialogViewModel"/> HintCalculator) tra giá trị field theo tên,
    /// độc lập với <see cref="Label"/> (có thể đổi câu chữ mà không vỡ logic gợi ý).</summary>
    public string FieldKey { get; init; } = "";

    /// <summary>Danh sách lựa chọn khi Kind = Combo.</summary>
    public IReadOnlyList<string>? Options { get; init; }

    private string? _textValue;
    /// <summary>Giá trị đang sửa khi Kind = Text | Number | Combo.</summary>
    public string? TextValue { get => _textValue; set => SetProperty(ref _textValue, value); }

    private bool _boolValue;
    /// <summary>Giá trị đang sửa khi Kind = Checkbox.</summary>
    public bool BoolValue { get => _boolValue; set => SetProperty(ref _boolValue, value); }

    private int? _numberValue;
    /// <summary>Giá trị đang sửa khi Kind = Number — bind trực tiếp vào DevExpress SpinEdit.</summary>
    public int? NumberValue { get => _numberValue; set => SetProperty(ref _numberValue, value); }

    /// <summary>Biên nhỏ nhất/lớn nhất cho SpinEdit khi Kind = Number.</summary>
    public int MinNumber { get; init; } = 0;
    public int MaxNumber { get; init; } = 100000;

    private bool _isEnabled = true;
    /// <summary>Trạng thái editor theo ngữ cảnh của các field khác; giá trị cũ vẫn được giữ khi tắt.</summary>
    public bool IsEnabled { get => _isEnabled; private set => SetProperty(ref _isEnabled, value); }

    /// <summary>Điều kiện bật editor, được dialog tính lại mỗi khi một field phụ thuộc thay đổi.</summary>
    public Func<IReadOnlyList<FieldDetailRowVm>, bool>? EnabledWhen { get; init; }

    /// <summary>Giá trị hiển thị khi Kind = ReadOnly (đã format sẵn, vd "(trống)"/"Có"/"Không").</summary>
    public string DisplayValue { get; init; } = "";

    public bool IsText => Kind == FieldEditorKind.Text;
    public bool IsNumber => Kind == FieldEditorKind.Number;
    public bool IsCheckbox => Kind == FieldEditorKind.Checkbox;
    public bool IsCombo => Kind == FieldEditorKind.Combo;
    public bool IsReadOnly => Kind == FieldEditorKind.ReadOnly;
    public bool ShowTopLabel => Kind != FieldEditorKind.Checkbox;

    /// <summary>Tính lại trạng thái editor theo toàn bộ field đang có trong popup.</summary>
    public void RefreshEnabled(IReadOnlyList<FieldDetailRowVm> rows)
        => IsEnabled = EnabledWhen?.Invoke(rows) ?? true;

    /// <summary>Ghi TextValue/BoolValue hiện tại ngược vào record gốc — gọi khi bấm Lưu.</summary>
    public Action<FieldDetailRowVm>? Commit { get; init; }
}
