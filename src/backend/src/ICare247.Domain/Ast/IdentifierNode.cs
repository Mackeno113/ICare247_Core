// File    : IdentifierNode.cs
// Module  : Ast
// Layer   : Domain
// Purpose : Node tham chiếu đến một field trong EvaluationContext theo tên.

namespace ICare247.Domain.Ast;

/// <summary>
/// Tham chiếu đến giá trị của field trong <c>EvaluationContext</c>.
/// Ví dụ: <c>{"type":"identifier","name":"DateOfBirth"}</c>.
/// <para>
/// NULL-SAFE: Nếu <c>Name</c> không tồn tại trong context → trả <c>null</c>, không throw.
/// Lý do: field có thể chưa có giá trị khi form mới load.
/// </para>
/// </summary>
/// <param name="Name">Tên field (Ui_Field.Field_Code) — lookup OrdinalIgnoreCase.</param>
public sealed record IdentifierNode(string Name) : IExpressionNode
{
    /// <inheritdoc/>
    public NodeKind Kind => NodeKind.Identifier;
}
