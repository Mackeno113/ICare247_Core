// File    : FieldNavGroup.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Model nhóm section cho Left Panel Field Navigator trong FieldConfigView.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Nhóm section trong Left Panel Field Navigator.
/// Mỗi group tương ứng 1 section, chứa danh sách field thuộc section đó.
/// </summary>
public sealed class FieldNavGroup : INotifyPropertyChanged
{
    public int    SectionId   { get; set; }
    public string SectionCode { get; set; } = "";

    private string _sectionName = "";
    /// <summary>Tên section đã resolve i18n (vi) — rỗng thì fallback về <see cref="SectionCode"/>.
    /// Gán sau khi build (async) → dùng INotifyPropertyChanged để header cập nhật.</summary>
    public string SectionName
    {
        get => _sectionName;
        set
        {
            if (_sectionName != value)
            {
                _sectionName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    /// <summary>Nhãn hiển thị trên header nhóm: tên đã dịch, fallback mã section.</summary>
    public string DisplayTitle => string.IsNullOrWhiteSpace(SectionName) ? SectionCode : SectionName;

    public ObservableCollection<FieldNavItem> Fields { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Một đích chuyển field trong context-menu "Chuyển field đã chọn sang…" của Field Navigator
/// (FieldConfigView). Song song với <see cref="MoveTargetItem"/> của FormEditor, nhưng khóa theo
/// <see cref="SectionId"/>/<see cref="SectionCode"/> thay vì <c>FormTreeNode</c> — panel navigator
/// không dùng cây node. Mang sẵn <see cref="MoveCommand"/> để MenuItem bind thẳng, tránh lỗi
/// RelativeSource qua Popup submenu.
/// </summary>
public sealed class FieldMoveTargetItem
{
    /// <summary>Nhãn hiển thị trên MenuItem (label section — giống dropdown "Section").</summary>
    public string Header { get; }

    /// <summary>Section_Id đích (Ui_Section.Section_Id).</summary>
    public int SectionId { get; }

    /// <summary>Section_Code đích — dùng khi phải khởi tạo group mới trong navigator.</summary>
    public string SectionCode { get; }

    /// <summary>Lệnh chuyển bulk — cùng 1 instance của VM; CommandParameter = chính item này.</summary>
    public ICommand MoveCommand { get; }

    public FieldMoveTargetItem(string header, int sectionId, string sectionCode, ICommand moveCommand)
    {
        Header      = header;
        SectionId   = sectionId;
        SectionCode = sectionCode;
        MoveCommand = moveCommand;
    }
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
/// Nguồn sự thật = cờ <c>Ui_Field.Is_Configured</c> (db/067), KHÔNG suy từ nhãn i18n.
/// </summary>
public enum FieldNavStatus
{
    /// <summary>Đã có Ui_Field và user đã bấm "Lưu Field" (Is_Configured = 1).</summary>
    Configured,
    /// <summary>Đã có Ui_Field nhưng chưa bấm "Lưu Field" lần nào (Is_Configured = 0) — VD field sinh tự động.</summary>
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

    /// <summary>Label_Key i18n của field (Ui_Field.Label_Key) — nguồn để resolve <see cref="DisplayName"/>.</summary>
    public string? LabelKey { get; set; }

    private FieldNavStatus _status = FieldNavStatus.Configured;
    /// <summary>
    /// Trạng thái cấu hình — điều khiển badge trong navigator.
    /// Đổi lúc runtime (VD: user bấm "Lưu Field" → Configured) nên phải notify cả các
    /// property dẫn xuất; nếu không badge giữ nguyên giá trị lúc nạp navigator.
    /// </summary>
    public FieldNavStatus Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusKey));
            OnPropertyChanged(nameof(StatusLabel));
            OnPropertyChanged(nameof(StatusTooltip));
            OnPropertyChanged(nameof(IsColumnOnly));
        }
    }

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
        FieldNavStatus.Incomplete => "Field đã tạo nhưng chưa Lưu cấu hình lần nào. Bấm để mở và bấm 'Lưu Field'.",
        _                          => "Field đã được cấu hình và lưu."
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

    // Bulk multi-select: tick riêng cho mỗi field trong navigator, độc lập với IsCurrentField
    // (single-select của navigator). Y hệt FormTreeNode.IsMultiChecked ở FormEditor.
    private bool _isMultiChecked;
    /// <summary>True khi field được tick để bulk-move sang section khác.</summary>
    public bool IsMultiChecked
    {
        get => _isMultiChecked;
        set { if (_isMultiChecked != value) { _isMultiChecked = value; OnPropertyChanged(); } }
    }

    /// <summary>Mã hiệu lực: FieldCode cho virtual field, ColumnCode cho field thường.</summary>
    public string EffectiveCode => !string.IsNullOrEmpty(FieldCode) ? FieldCode : ColumnCode;

    private string _displayName = "";
    /// <summary>Tên field đã resolve i18n (vi). Gán sau khi build (async) → dùng INotifyPropertyChanged.</summary>
    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (_displayName != value)
            {
                _displayName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Title));
            }
        }
    }

    /// <summary>Dòng chính hiển thị: tên đã dịch, rỗng thì fallback về mã (EffectiveCode).</summary>
    public string Title => string.IsNullOrWhiteSpace(DisplayName) ? EffectiveCode : DisplayName;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
