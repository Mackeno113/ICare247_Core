// File    : FieldNavGroup.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Model nhóm section cho Left Panel Field Navigator trong FieldConfigView.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Nhóm section trong Left Panel Field Navigator.
/// Mỗi group tương ứng 1 section, chứa danh sách field thuộc section đó.
/// </summary>
public sealed class FieldNavGroup
{
    public int    SectionId   { get; set; }
    public string SectionCode { get; set; } = "";
    public ObservableCollection<FieldNavItem> Fields { get; } = [];
}

/// <summary>
/// Param cho ReorderFieldCommand — tránh tuple value-type (Prism DelegateCommand yêu cầu reference type).
/// </summary>
public sealed class FieldReorderArgs
{
    public FieldNavItem    Dragged     { get; init; } = null!;
    public FieldNavGroup   TargetGroup { get; init; } = null!;
    public int             InsertIndex { get; init; }
}

/// <summary>
/// Trạng thái cấu hình của 1 item trong Field Navigator — để nhận biết mức độ hoàn thiện.
/// </summary>
public enum FieldNavStatus
{
    /// <summary>Đã có Ui_Field + nhãn i18n (vi) → cấu hình đầy đủ.</summary>
    Configured,
    /// <summary>Đã có Ui_Field nhưng thiếu nhãn i18n (vi) → chưa cấu hình xong.</summary>
    Incomplete,
    /// <summary>Chỉ có cột trong Sys_Column, chưa tạo Ui_Field.</summary>
    ColumnOnly
}

/// <summary>
/// Item field trong Left Panel Field Navigator.
/// </summary>
public sealed class FieldNavItem : INotifyPropertyChanged
{
    public int FieldId { get; set; }
    public int SortOrder { get; set; }
    public string ColumnCode { get; set; } = "";
    public string? FieldCode { get; set; }
    public string EditorType { get; set; } = "";
    public bool IsVirtual { get; set; }

    /// <summary>Trạng thái cấu hình — điều khiển badge trong navigator.</summary>
    public FieldNavStatus Status { get; set; } = FieldNavStatus.Configured;

    /// <summary>Khóa trạng thái (dùng cho DataTrigger tô màu badge trong XAML).</summary>
    public string StatusKey => Status switch
    {
        FieldNavStatus.ColumnOnly => "column",
        FieldNavStatus.Incomplete => "incomplete",
        _                          => "configured"
    };

    /// <summary>Nhãn badge ngắn hiển thị cạnh mã field.</summary>
    public string StatusLabel => Status switch
    {
        FieldNavStatus.ColumnOnly => "chưa tạo field",
        FieldNavStatus.Incomplete => "chưa cấu hình",
        _                          => "đã cấu hình"
    };

    /// <summary>Tooltip giải thích trạng thái.</summary>
    public string StatusTooltip => Status switch
    {
        FieldNavStatus.ColumnOnly => "Cột đã có trong bảng nhưng chưa tạo field. Bấm để tạo field từ cột này.",
        FieldNavStatus.Incomplete => "Field đã tạo nhưng chưa có nhãn hiển thị (i18n). Bấm để bổ sung cấu hình.",
        _                          => "Field đã cấu hình đầy đủ (có nhãn hiển thị)."
    };

    /// <summary>True khi chỉ là cột chưa tạo field — dùng ẩn nút ↑↓ (chưa có Order_No).</summary>
    public bool IsColumnOnly => Status == FieldNavStatus.ColumnOnly;

    private bool _isCurrentField;
    /// <summary>True khi đây là field đang được chỉnh sửa hiện tại.</summary>
    public bool IsCurrentField
    {
        get => _isCurrentField;
        set { if (_isCurrentField != value) { _isCurrentField = value; OnPropertyChanged(); } }
    }

    /// <summary>Mã hiệu lực: FieldCode cho virtual field, ColumnCode cho field thường.</summary>
    public string EffectiveCode => !string.IsNullOrEmpty(FieldCode) ? FieldCode : ColumnCode;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
