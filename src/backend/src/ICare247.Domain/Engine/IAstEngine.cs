// File    : IAstEngine.cs
// Module  : Engine
// Layer   : Domain
// Purpose : Interface của AST engine: parse Expression_Json → compile → evaluate.

using ICare247.Domain.Ast;
using ICare247.Domain.ValueObjects;

namespace ICare247.Domain.Engine;

/// <summary>
/// Engine xử lý expression dạng AST.
/// <para>
/// Flow: <c>Parse(json)</c> → <c>Compile(node)</c> → delegate được cache →
/// gọi delegate với <see cref="EvaluationContext"/>.
/// </para>
/// <para>
/// Không dùng eval, Roslyn, hay dynamic SQL — chỉ AST-based delegate compilation.
/// </para>
/// </summary>
public interface IAstEngine
{
    /// <summary>
    /// Parse Expression_Json (string) thành AST node tree.
    /// </summary>
    /// <param name="expressionJson">
    /// JSON string lưu trong <c>Sys_Rule.Expression_Json</c>.
    /// </param>
    /// <returns>Root node của AST.</returns>
    /// <exception cref="InvalidOperationException">
    /// Throw khi JSON không hợp lệ hoặc chứa function/operator không trong whitelist.
    /// </exception>
    IExpressionNode Parse(string expressionJson);

    /// <summary>
    /// Compile AST thành delegate có thể gọi trực tiếp.
    /// Kết quả được cache theo hash của expressionJson để tránh compile lại.
    /// </summary>
    /// <param name="node">Root node từ <see cref="Parse"/>.</param>
    /// <returns>
    /// Delegate nhận <see cref="EvaluationContext"/> và trả về kết quả.
    /// Delegate luôn null-safe — không throw khi operand là null.
    /// </returns>
    Func<EvaluationContext, object?> Compile(IExpressionNode node);

    /// <summary>
    /// Shortcut: parse + compile (nếu chưa cache) + execute trong một lần gọi.
    /// Dùng cho validate hoặc evaluate đơn giản.
    /// </summary>
    /// <param name="expressionJson">JSON string của expression.</param>
    /// <param name="context">Giá trị các field tại thời điểm evaluate.</param>
    /// <returns>Kết quả expression; <c>null</c> nếu expression null-propagate.</returns>
    object? Evaluate(string expressionJson, EvaluationContext context);
}
