// File    : AstValidationResult.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : Kết quả validate AST tree — dùng chung cho Rules, Events, Expression Builder.

namespace ConfigStudio.WPF.UI.Modules.Grammar.Models;

/// <summary>
/// Kết quả validate AST tree.
/// Chứa trạng thái hợp lệ, return type, depth, danh sách lỗi, và referenced fields.
/// </summary>
public sealed class AstValidationResult
{
    public bool IsValid { get; set; }

    /// <summary>
    /// Return type thực tế của expression — vd: "Boolean", "Int32".
    /// </summary>
    public string? ActualReturnType { get; set; }

    /// <summary>
    /// Độ sâu thực tế của AST tree.
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Danh sách lỗi validation — rỗng nếu <see cref="IsValid"/> = true.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Danh sách field được tham chiếu trong expression — dùng cho Sys_Dependency.
    /// </summary>
    public List<string> ReferencedFields { get; set; } = [];
}
