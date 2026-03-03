// File    : LiteralNode.cs
// Module  : Ast
// Layer   : Domain
// Purpose : Node đại diện cho giá trị literal trong expression (số, string, bool, null).

namespace ICare247.Domain.Ast;

/// <summary>
/// Giá trị literal được nhúng trực tiếp trong expression JSON.
/// Ví dụ: <c>{"type":"literal","value":42}</c> → <c>LiteralNode(42)</c>.
/// <para>Null propagation: Value có thể là null — compiler xử lý theo quy tắc null-safe.</para>
/// </summary>
/// <param name="Value">
/// Giá trị đã deserialize từ JSON: <c>int</c>, <c>double</c>, <c>string</c>,
/// <c>bool</c> hoặc <c>null</c>.
/// </param>
public sealed record LiteralNode(object? Value) : IExpressionNode
{
    /// <inheritdoc/>
    public NodeKind Kind => NodeKind.Literal;
}
