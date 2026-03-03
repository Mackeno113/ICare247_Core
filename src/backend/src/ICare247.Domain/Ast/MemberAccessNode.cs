// File    : MemberAccessNode.cs
// Module  : Ast
// Layer   : Domain
// Purpose : Node truy cập property qua dot notation (object.Property).

namespace ICare247.Domain.Ast;

/// <summary>
/// Truy cập property của một object qua dot notation.
/// Ví dụ: <c>{"type":"member","object":{identifier: "Address"},"property":"City"}</c>
/// tương đương với <c>Address.City</c>.
/// <para>
/// NULL-SAFE: Nếu Object evaluate ra null → trả null, không throw NullReferenceException.
/// </para>
/// </summary>
/// <param name="Object">Node đại diện cho object cha.</param>
/// <param name="Property">Tên property cần truy cập (OrdinalIgnoreCase).</param>
public sealed record MemberAccessNode(
    IExpressionNode Object,
    string Property) : IExpressionNode
{
    /// <inheritdoc/>
    public NodeKind Kind => NodeKind.MemberAccess;
}
