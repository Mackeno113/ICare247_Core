// File    : PaletteItem.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : Item trong Palette panel (operator, function, field) của Expression Builder.

namespace ConfigStudio.WPF.UI.Modules.Grammar.Models;

/// <summary>
/// Loại item trong Palette.
/// </summary>
public enum PaletteItemType
{
    Operator,
    Function,
    Field
}

/// <summary>
/// Một item trong Palette panel — có thể là operator, function, hoặc field.
/// Click vào sẽ insert node tương ứng vào AST tree.
/// </summary>
public sealed class PaletteItem
{
    public PaletteItemType ItemType { get; set; }

    /// <summary>
    /// Tên hiển thị — vd: ">=", "len(str)", "SoLuong".
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Symbol/code dùng để tạo node — vd: ">=", "len", "SoLuong".
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>
    /// Mô tả tooltip — vd: "Greater than or equal", "Hàm đếm ký tự".
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// .NET return type hoặc field type — vd: "Boolean", "Int32".
    /// </summary>
    public string NetType { get; set; } = "";

    /// <summary>
    /// Số tham số tối thiểu (cho Function).
    /// </summary>
    public int ParamCountMin { get; set; }

    /// <summary>
    /// Số tham số tối đa (cho Function).
    /// </summary>
    public int ParamCountMax { get; set; }
}
