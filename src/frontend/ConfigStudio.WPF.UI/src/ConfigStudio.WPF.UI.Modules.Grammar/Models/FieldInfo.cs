// File    : FieldInfo.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : Thông tin field trong form context — truyền vào Expression Builder để chọn Identifier.

namespace ConfigStudio.WPF.UI.Modules.Grammar.Models;

/// <summary>
/// Thông tin field trong form context.
/// Dùng để hiển thị danh sách field trong Palette và validate Identifier node.
/// </summary>
public sealed class FieldInfo
{
    public string Code { get; set; } = "";
    public string NetType { get; set; } = "";

    /// <summary>
    /// Chuỗi hiển thị: "SoLuong (Int32)".
    /// </summary>
    public string DisplayName => $"{Code} ({NetType})";

    public FieldInfo() { }

    public FieldInfo(string code, string netType)
    {
        Code = code;
        NetType = netType;
    }
}
