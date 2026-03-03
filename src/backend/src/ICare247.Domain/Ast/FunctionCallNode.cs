// File    : FunctionCallNode.cs
// Module  : Ast
// Layer   : Domain
// Purpose : Node gọi hàm từ whitelist (len, iif, toDate, coalesce, ...).

namespace ICare247.Domain.Ast;

/// <summary>
/// Gọi một hàm có trong whitelist <c>Gram_Function</c>.
/// Ví dụ: <c>{"type":"function_call","name":"iif","args":[cond, trueVal, falseVal]}</c>.
/// <para>
/// AstParser validate tên hàm và số lượng argument trước khi tạo node.
/// NULL-SAFE: Từng argument có thể null — hàm xử lý theo đặc tả riêng.
/// </para>
/// </summary>
/// <param name="FunctionName">
/// Tên hàm (lowercase) — phải có trong <c>Gram_Function</c>.
/// Ví dụ: 'len', 'trim', 'iif', 'toDate', 'today', 'coalesce'.
/// </param>
/// <param name="Args">
/// Danh sách arguments theo thứ tự — không thay đổi sau khi parse.
/// </param>
public sealed record FunctionCallNode(
    string FunctionName,
    IReadOnlyList<IExpressionNode> Args) : IExpressionNode
{
    /// <inheritdoc/>
    public NodeKind Kind => NodeKind.FunctionCall;
}
