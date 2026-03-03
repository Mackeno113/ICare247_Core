// File    : AstDeserializer.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : Chuyển đổi JSON string → AstNodeViewModel tree theo Grammar V1 spec.

using System.Text.Json;
using ConfigStudio.WPF.UI.Modules.Grammar.Models;

namespace ConfigStudio.WPF.UI.Modules.Grammar.Services;

/// <summary>
/// Deserialize JSON string thành <see cref="AstNodeViewModel"/> tree.
/// Nếu json null/empty → trả về null (blank builder).
/// </summary>
public sealed class AstDeserializer
{
    /// <summary>
    /// Deserialize JSON string thành AstNodeViewModel tree.
    /// </summary>
    public AstNodeViewModel? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var node = ParseNode(doc.RootElement);
            if (node is null) return null;

            return BuildViewModel(node, parent: null, depth: 0);
        }
        catch (JsonException)
        {
            // NOTE: JSON không hợp lệ → trả null, không throw
            return null;
        }
    }

    private static AstNode? ParseNode(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        if (!element.TryGetProperty("type", out var typeProp)) return null;

        var typeStr = typeProp.GetString() ?? "";
        if (!Enum.TryParse<AstNodeType>(typeStr, out var nodeType)) return null;

        var node = new AstNode { Type = nodeType };

        switch (nodeType)
        {
            case AstNodeType.Literal:
                node.Value = ParseLiteralValue(element);
                node.NetType = element.TryGetProperty("netType", out var nt) ? nt.GetString() ?? "" : "";
                break;

            case AstNodeType.Identifier:
                node.Name = element.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "";
                break;

            case AstNodeType.Binary:
                node.Operator = element.TryGetProperty("operator", out var binOp) ? binOp.GetString() ?? "" : "";
                if (element.TryGetProperty("left", out var left))
                    node.Left = ParseNode(left);
                if (element.TryGetProperty("right", out var right))
                    node.Right = ParseNode(right);
                break;

            case AstNodeType.Unary:
                node.Operator = element.TryGetProperty("operator", out var unOp) ? unOp.GetString() ?? "" : "";
                if (element.TryGetProperty("operand", out var operand))
                    node.Operand = ParseNode(operand);
                break;

            case AstNodeType.Function:
                node.FunctionName = element.TryGetProperty("name", out var fn) ? fn.GetString() ?? "" : "";
                if (element.TryGetProperty("arguments", out var args) && args.ValueKind == JsonValueKind.Array)
                {
                    foreach (var arg in args.EnumerateArray())
                    {
                        var argNode = ParseNode(arg);
                        if (argNode is not null)
                            node.Arguments.Add(argNode);
                    }
                }
                break;

            case AstNodeType.CustomHandler:
                node.HandlerCode = element.TryGetProperty("handlerCode", out var hc) ? hc.GetString() ?? "" : "";
                break;
        }

        return node;
    }

    /// <summary>
    /// Parse giá trị literal từ JSON — hỗ trợ number, string, boolean.
    /// </summary>
    private static object? ParseLiteralValue(JsonElement element)
    {
        if (!element.TryGetProperty("value", out var val)) return null;

        return val.ValueKind switch
        {
            JsonValueKind.Number when val.TryGetInt32(out var i) => i,
            JsonValueKind.Number when val.TryGetDecimal(out var d) => d,
            JsonValueKind.String => val.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => val.ToString()
        };
    }

    /// <summary>
    /// Build AstNodeViewModel tree từ AstNode, gán parent/depth cho mỗi node.
    /// </summary>
    private static AstNodeViewModel BuildViewModel(AstNode node, AstNodeViewModel? parent, int depth)
    {
        var vm = new AstNodeViewModel
        {
            Node = node,
            Parent = parent,
            Depth = depth,
            IsExpanded = true
        };

        switch (node.Type)
        {
            case AstNodeType.Binary:
                if (node.Left is not null)
                    vm.Children.Add(BuildViewModel(node.Left, vm, depth + 1));
                if (node.Right is not null)
                    vm.Children.Add(BuildViewModel(node.Right, vm, depth + 1));
                break;

            case AstNodeType.Unary:
                if (node.Operand is not null)
                    vm.Children.Add(BuildViewModel(node.Operand, vm, depth + 1));
                break;

            case AstNodeType.Function:
                foreach (var arg in node.Arguments)
                    vm.Children.Add(BuildViewModel(arg, vm, depth + 1));
                break;
        }

        return vm;
    }
}
