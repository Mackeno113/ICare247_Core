// File    : AstNaturalLanguage.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : Chuyển đổi AstNodeViewModel → human readable string cho preview.

using ConfigStudio.WPF.UI.Modules.Grammar.Models;

namespace ConfigStudio.WPF.UI.Modules.Grammar.Services;

/// <summary>
/// Chuyển đổi <see cref="AstNodeViewModel"/> tree thành chuỗi natural language.
/// Dùng cho preview trong Expression Builder và Rule/Event editors.
/// </summary>
public sealed class AstNaturalLanguage
{
    /// <summary>
    /// Chuyển AST tree thành text dễ đọc. Trả về "(trống)" nếu root là null.
    /// </summary>
    public string ToText(AstNodeViewModel? root)
    {
        if (root is null) return "(trống)";
        return NodeToText(root.Node);
    }

    private static string NodeToText(AstNode node) => node.Type switch
    {
        AstNodeType.Literal => FormatLiteral(node),
        AstNodeType.Identifier => node.Name,
        AstNodeType.Binary => FormatBinary(node),
        AstNodeType.Unary => FormatUnary(node),
        AstNodeType.Function => FormatFunction(node),
        AstNodeType.CustomHandler => $"[Custom: {node.HandlerCode}]",
        _ => "?"
    };

    private static string FormatLiteral(AstNode node)
    {
        if (node.Value is null) return "null";
        if (node.Value is string s) return $"\"{s}\"";
        if (node.Value is bool b) return b ? "true" : "false";
        return node.Value.ToString() ?? "null";
    }

    private static string FormatBinary(AstNode node)
    {
        var left = node.Left is not null ? NodeToText(node.Left) : "?";
        var right = node.Right is not null ? NodeToText(node.Right) : "?";

        // NOTE: Dùng tiếng Việt cho logical operators
        return node.Operator switch
        {
            "&&" => $"({left} VÀ {right})",
            "||" => $"({left} HOẶC {right})",
            _ => $"{left} {node.Operator} {right}"
        };
    }

    private static string FormatUnary(AstNode node)
    {
        var operand = node.Operand is not null ? NodeToText(node.Operand) : "?";
        return node.Operator switch
        {
            "!" => $"KHÔNG({operand})",
            _ => $"{node.Operator}{operand}"
        };
    }

    private static string FormatFunction(AstNode node)
    {
        var args = string.Join(", ", node.Arguments.Select(NodeToText));

        // NOTE: Một số function có natural language riêng
        return node.FunctionName.ToLowerInvariant() switch
        {
            "iif" when node.Arguments.Count == 3 =>
                $"NẾU {NodeToText(node.Arguments[0])} THÌ {NodeToText(node.Arguments[1])} KHÔNG THÌ {NodeToText(node.Arguments[2])}",
            "regex" when node.Arguments.Count == 2 =>
                $"{NodeToText(node.Arguments[0])} khớp pattern {NodeToText(node.Arguments[1])}",
            "isnull" when node.Arguments.Count == 1 =>
                $"{NodeToText(node.Arguments[0])} là null",
            _ => $"{node.FunctionName}({args})"
        };
    }
}
