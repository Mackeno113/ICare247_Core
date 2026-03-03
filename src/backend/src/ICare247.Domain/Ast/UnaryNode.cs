// File    : UnaryNode.cs
// Module  : Ast
// Layer   : Domain
// Purpose : Node phép toán một vế — phủ định logic hoặc đảo dấu số.

namespace ICare247.Domain.Ast;

/// <summary>
/// Phép toán một vế trong expression.
/// Ví dụ: <c>{"type":"unary","op":"!","operand":{...}}</c>.
/// <para>
/// Operators hỗ trợ: '!' (phủ định logic), '-' (đảo dấu số).
/// NULL-SAFE: Operand là null → trả null.
/// </para>
/// </summary>
/// <param name="Operator">Ký hiệu toán tử một vế: '!' hoặc '-'.</param>
/// <param name="Operand">Node con được áp dụng phép toán.</param>
public sealed record UnaryNode(
    string Operator,
    IExpressionNode Operand) : IExpressionNode
{
    /// <inheritdoc/>
    public NodeKind Kind => NodeKind.Unary;
}
