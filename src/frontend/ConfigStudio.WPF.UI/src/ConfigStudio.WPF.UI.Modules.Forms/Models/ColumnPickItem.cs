// File    : ColumnPickItem.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Dòng chọn trong ColumnPickerDialog — bọc ColumnInfoDto + cờ chọn/đã dùng (multi-select).

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Một dòng trong popup chọn cột. Bọc <see cref="ColumnInfoDto"/> và bổ sung trạng thái
/// <see cref="IsSelected"/> (tick chọn khi multi-select) + <see cref="IsAlreadyUsed"/>
/// (cột đã có trong danh sách → khóa, không cho chọn lại).
/// </summary>
public sealed class ColumnPickItem : BindableBase
{
    /// <summary>Khởi tạo dòng chọn từ DTO cột + cờ đã dùng.</summary>
    /// <param name="column">Thông tin cột từ Sys_Column.</param>
    /// <param name="isAlreadyUsed">true nếu cột đã có trong danh sách hiện tại.</param>
    public ColumnPickItem(ColumnInfoDto column, bool isAlreadyUsed)
    {
        Column = column;
        IsAlreadyUsed = isAlreadyUsed;
    }

    /// <summary>Cột gốc trả về cho caller khi chọn.</summary>
    public ColumnInfoDto Column { get; }

    /// <summary>Cột đã tồn tại trong danh sách → hiển thị mờ + khóa tick.</summary>
    public bool IsAlreadyUsed { get; }

    private bool _isSelected;
    /// <summary>Đang được tick chọn (chế độ multi-select).</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set { if (!IsAlreadyUsed) SetProperty(ref _isSelected, value); }
    }
}
