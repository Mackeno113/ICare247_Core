// File    : FkLookupFieldOption.cs
// Module  : Data
// Layer   : Core
// Purpose : Một lựa chọn cho dropdown "FK lookup" ở tab Cột (màn Quản lý View) —
//           1 field FK (LookupBox) của form sửa, để cấu hình auto-JOIN hiện TÊN cha.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Lựa chọn field khóa ngoại của form sửa cho ô "FK lookup" (cột lưới hiện TÊN cha thay vì id).
/// <see cref="FieldId"/> = giá trị lưu vào <c>Props_Json.fkLookup.fieldId</c>; <see cref="Label"/> = nhãn hiển thị.
/// </summary>
public sealed class FkLookupFieldOption
{
    /// <summary>Field_Id (Ui_Field) của LookupBox FK — lưu vào fkLookup.fieldId.</summary>
    public int FieldId { get; init; }

    /// <summary>Cột FK gốc trên bảng (Sys_Column.Column_Code), vd <c>NganHang_Id</c>.</summary>
    public string? BaseColumn { get; init; }

    /// <summary>Bảng cha (Ui_Field_Lookup.Source_Name), vd <c>DM_NganHang</c>.</summary>
    public string? SourceName { get; init; }

    /// <summary>Cột tên hiển thị (Display_Column), vd <c>Ten</c>.</summary>
    public string? DisplayColumn { get; init; }

    /// <summary>Nhãn thân thiện cho dropdown: <c>NganHang_Id → DM_NganHang (Ten)</c>.</summary>
    public string Label =>
        $"{BaseColumn} → {SourceName}" + (string.IsNullOrWhiteSpace(DisplayColumn) ? "" : $" ({DisplayColumn})");
}
