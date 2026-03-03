// File    : AstNode.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : Model C# cho AST node — ánh xạ 1:1 với Grammar V1 JSON spec.

namespace ConfigStudio.WPF.UI.Modules.Grammar.Models;

/// <summary>
/// Loại node trong AST tree theo Grammar V1 spec.
/// </summary>
public enum AstNodeType
{
    Literal,
    Identifier,
    Binary,
    Unary,
    Function,
    CustomHandler
}

/// <summary>
/// Model dữ liệu cho 1 node AST.
/// Mỗi node type chỉ sử dụng một tập con property tương ứng.
/// </summary>
public sealed class AstNode
{
    public AstNodeType Type { get; set; }

    // ── Literal ──────────────────────────────────────────────
    public object? Value { get; set; }

    /// <summary>
    /// .NET type: String | Int32 | Decimal | Boolean | DateTime.
    /// Dùng cho Literal (xác định kiểu giá trị) và Identifier (từ Sys_Column.Net_Type).
    /// </summary>
    public string NetType { get; set; } = "";

    // ── Identifier ───────────────────────────────────────────
    /// <summary>
    /// Column_Code tham chiếu — vd: "SoLuong", "TrangThai".
    /// </summary>
    public string Name { get; set; } = "";

    // ── Binary ───────────────────────────────────────────────
    /// <summary>
    /// Operator symbol: ==, !=, >, >=, &lt;, &lt;=, &amp;&amp;, ||, +, -, *, /, %, ??.
    /// Dùng cho cả Binary và Unary (!).
    /// </summary>
    public string Operator { get; set; } = "";
    public AstNode? Left { get; set; }
    public AstNode? Right { get; set; }

    // ── Unary ────────────────────────────────────────────────
    public AstNode? Operand { get; set; }

    // ── Function ─────────────────────────────────────────────
    public string FunctionName { get; set; } = "";
    public List<AstNode> Arguments { get; set; } = [];

    // ── CustomHandler ────────────────────────────────────────
    public string HandlerCode { get; set; } = "";
}
