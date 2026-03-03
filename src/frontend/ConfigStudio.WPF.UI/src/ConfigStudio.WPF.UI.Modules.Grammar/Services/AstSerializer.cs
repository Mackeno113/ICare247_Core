// File    : AstSerializer.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : Chuyển đổi AstNodeViewModel tree → JSON string theo Grammar V1 spec.

using System.Text.Json;
using System.Text.Json.Nodes;
using ConfigStudio.WPF.UI.Modules.Grammar.Models;

namespace ConfigStudio.WPF.UI.Modules.Grammar.Services;

/// <summary>
/// Serialize <see cref="AstNodeViewModel"/> tree thành JSON string.
/// Output đúng format Grammar V1 JSON: type, value, operator, left, right, name, arguments...
/// </summary>
public sealed class AstSerializer
{
    /// <summary>
    /// Serialize root node thành JSON string. Trả về "{}" nếu root là null.
    /// </summary>
    public string Serialize(AstNodeViewModel? root)
    {
        if (root is null) return "{}";

        var jsonNode = SerializeNode(root.Node);
        return jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static JsonObject SerializeNode(AstNode node)
    {
        var obj = new JsonObject { ["type"] = node.Type.ToString() };

        switch (node.Type)
        {
            case AstNodeType.Literal:
                obj["value"] = JsonValue.Create(node.Value);
                obj["netType"] = node.NetType;
                break;

            case AstNodeType.Identifier:
                obj["name"] = node.Name;
                break;

            case AstNodeType.Binary:
                obj["operator"] = node.Operator;
                obj["left"] = node.Left is not null ? SerializeNode(node.Left) : null;
                obj["right"] = node.Right is not null ? SerializeNode(node.Right) : null;
                break;

            case AstNodeType.Unary:
                obj["operator"] = node.Operator;
                obj["operand"] = node.Operand is not null ? SerializeNode(node.Operand) : null;
                break;

            case AstNodeType.Function:
                obj["name"] = node.FunctionName;
                var args = new JsonArray();
                foreach (var arg in node.Arguments)
                    args.Add(SerializeNode(arg));
                obj["arguments"] = args;
                break;

            case AstNodeType.CustomHandler:
                obj["handlerCode"] = node.HandlerCode;
                break;
        }

        return obj;
    }
}
