// File    : IExpressionNode.cs
// Module  : Ast
// Layer   : Domain
// Purpose : Interface gốc của mọi node trong AST expression tree.

namespace ICare247.Domain.Ast;

/// <summary>
/// Interface gốc của tất cả AST node.
/// AstCompiler nhận <c>IExpressionNode</c> và compile thành
/// <c>Func&lt;EvaluationContext, object?&gt;</c> — không dùng reflection khi execute.
/// </summary>
public interface IExpressionNode
{
    /// <summary>Loại node — dùng để dispatch trong AstCompiler.</summary>
    NodeKind Kind { get; }
}
