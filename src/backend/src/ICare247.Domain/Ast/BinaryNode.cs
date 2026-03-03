// File    : BinaryNode.cs
// Module  : Ast
// Layer   : Domain
// Purpose : Node phép toán hai vế — số học, so sánh hoặc logic.

namespace ICare247.Domain.Ast;

/// <summary>
/// Phép toán hai vế trong expression.
/// Ví dụ: <c>{"type":"binary","op":">","left":{...},"right":{...}}</c>.
/// <para>
/// Operator phải có trong whitelist <c>Gram_Operator</c> — AstParser validate khi parse.
/// NULL-SAFE: Nếu Left hoặc Right evaluate ra null → kết quả là null, không throw.
/// </para>
/// </summary>
/// <param name="Operator">
/// Ký hiệu toán tử: '+', '-', '*', '/', '==', '!=', '&gt;', '&gt;=', '&lt;', '&lt;=', '&amp;&amp;', '||'.
/// </param>
/// <param name="Left">Vế trái của phép toán.</param>
/// <param name="Right">Vế phải của phép toán.</param>
public sealed record BinaryNode(
    string Operator,
    IExpressionNode Left,
    IExpressionNode Right) : IExpressionNode
{
    /// <inheritdoc/>
    public NodeKind Kind => NodeKind.Binary;
}
