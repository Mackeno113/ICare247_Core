// File    : ColumnInfoDto.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Thông tin column từ bảng Sys_Column, dùng cho FieldConfig chọn column gắn vào field.

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// DTO chứa thông tin column từ <c>Sys_Column</c>.
/// Hiển thị trong ComboBox chọn column khi cấu hình field.
/// </summary>
public sealed class ColumnInfoDto
{
    public int ColumnId { get; set; }
    public string ColumnCode { get; set; } = "";
    public string DataType { get; set; } = "";
    public string NetType { get; set; } = "";
    public int? MaxLength { get; set; }
    public bool IsNullable { get; set; }

    /// <summary>
    /// Hiển thị DataType kèm MaxLength nếu có — ví dụ: "nvarchar(20)", "int".
    /// </summary>
    public string DataTypeDisplay => MaxLength.HasValue ? $"{DataType}({MaxLength})" : DataType;

    /// <summary>
    /// Chuỗi hiển thị dạng "SoLuong (Int32, NOT NULL)" cho ComboBox.
    /// </summary>
    public string DisplayName => $"{ColumnCode} ({NetType}{(IsNullable ? ", NULL" : ", NOT NULL")})";
}
