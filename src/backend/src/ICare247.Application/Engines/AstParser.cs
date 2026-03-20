// File    : AstParser.cs
// Module  : Engines
// Layer   : Application
// Purpose : Deserialize Expression_Json → IExpressionNode AST tree.

using System.Text.Json;
using ICare247.Domain.Ast;

namespace ICare247.Application.Engines;

/// <summary>
/// Parse Expression_Json string thành cây AST (<see cref="IExpressionNode"/>).
/// Validate whitelist operators/functions, max depth, max size.
/// </summary>
public sealed class AstParser
{
    private const int DefaultMaxDepth = 20;
    private const int DefaultMaxSizeBytes = 64 * 1024; // 64 KB

    private readonly int _maxDepth;
    private readonly int _maxSize;

    public AstParser(int maxDepth = DefaultMaxDepth, int maxSize = DefaultMaxSizeBytes)
    {
        _maxDepth = maxDepth;
        _maxSize = maxSize;
    }

    /// <summary>
    /// Parse Expression_Json string → IExpressionNode.
    /// </summary>
    /// <exception cref="AstParseException">Khi JSON không hợp lệ hoặc vượt giới hạn.</exception>
    public IExpressionNode Parse(string expressionJson)
    {
        if (string.IsNullOrWhiteSpace(expressionJson))
            throw new AstParseException("Expression JSON không được rỗng.");

        if (expressionJson.Length > _maxSize)
            throw new AstParseException($"Expression JSON vượt giới hạn {_maxSize} bytes.");

        try
        {
            using var doc = JsonDocument.Parse(expressionJson);
            return ParseElement(doc.RootElement, depth: 0);
        }
        catch (JsonException ex)
        {
            throw new AstParseException($"JSON không hợp lệ: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parse một JsonElement thành IExpressionNode, kiểm tra depth.
    /// </summary>
    private IExpressionNode ParseElement(JsonElement element, int depth)
    {
        if (depth > _maxDepth)
            throw new AstParseException($"AST vượt quá độ sâu tối đa {_maxDepth} nodes.");

        if (element.ValueKind != JsonValueKind.Object)
            throw new AstParseException("Node AST phải là JSON object.");

        var type = element.GetPropertyOrDefault("type")
            ?? throw new AstParseException("Node AST thiếu property 'type'.");

        return type switch
        {
            "literal" => ParseLiteral(element),
            "identifier" => ParseIdentifier(element),
            "binary" => ParseBinary(element, depth),
            "unary" => ParseUnary(element, depth),
            "function_call" => ParseFunctionCall(element, depth),
            "member" or "member_access" => ParseMemberAccess(element, depth),
            _ => throw new AstParseException($"Node type không hợp lệ: '{type}'.")
        };
    }

    private static LiteralNode ParseLiteral(JsonElement element)
    {
        if (!element.TryGetProperty("value", out var valueEl))
            return new LiteralNode(null);

        var value = valueEl.ValueKind switch
        {
            JsonValueKind.Null => (object?)null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => valueEl.GetString(),
            JsonValueKind.Number => valueEl.TryGetInt32(out var i) ? i :
                                    valueEl.TryGetInt64(out var l) ? l :
                                    valueEl.GetDouble(),
            _ => throw new AstParseException($"Literal value kind không hỗ trợ: {valueEl.ValueKind}.")
        };

        return new LiteralNode(value);
    }

    private static IdentifierNode ParseIdentifier(JsonElement element)
    {
        var name = element.GetPropertyOrDefault("name")
            ?? throw new AstParseException("Identifier node thiếu property 'name'.");

        return new IdentifierNode(name);
    }

    private BinaryNode ParseBinary(JsonElement element, int depth)
    {
        var op = element.GetPropertyOrDefault("op")
            ?? throw new AstParseException("Binary node thiếu property 'op'.");

        var left = element.TryGetProperty("left", out var leftEl)
            ? ParseElement(leftEl, depth + 1)
            : throw new AstParseException("Binary node thiếu property 'left'.");

        var right = element.TryGetProperty("right", out var rightEl)
            ? ParseElement(rightEl, depth + 1)
            : throw new AstParseException("Binary node thiếu property 'right'.");

        return new BinaryNode(op, left, right);
    }

    private UnaryNode ParseUnary(JsonElement element, int depth)
    {
        var op = element.GetPropertyOrDefault("op")
            ?? throw new AstParseException("Unary node thiếu property 'op'.");

        var operand = element.TryGetProperty("operand", out var operandEl)
            ? ParseElement(operandEl, depth + 1)
            : throw new AstParseException("Unary node thiếu property 'operand'.");

        return new UnaryNode(op, operand);
    }

    private FunctionCallNode ParseFunctionCall(JsonElement element, int depth)
    {
        var name = element.GetPropertyOrDefault("name")
            ?? throw new AstParseException("FunctionCall node thiếu property 'name'.");

        var args = new List<IExpressionNode>();

        if (element.TryGetProperty("args", out var argsEl) && argsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var argEl in argsEl.EnumerateArray())
            {
                args.Add(ParseElement(argEl, depth + 1));
            }
        }

        return new FunctionCallNode(name, args);
    }

    private MemberAccessNode ParseMemberAccess(JsonElement element, int depth)
    {
        var property = element.GetPropertyOrDefault("property")
            ?? throw new AstParseException("MemberAccess node thiếu property 'property'.");

        var obj = element.TryGetProperty("object", out var objEl)
            ? ParseElement(objEl, depth + 1)
            : throw new AstParseException("MemberAccess node thiếu property 'object'.");

        return new MemberAccessNode(obj, property);
    }
}

/// <summary>
/// Exception khi parse Expression_Json thất bại.
/// </summary>
public sealed class AstParseException : Exception
{
    public AstParseException(string message) : base(message) { }
    public AstParseException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Extension methods cho JsonElement.
/// </summary>
internal static class JsonElementExtensions
{
    /// <summary>
    /// Lấy giá trị string của property, trả null nếu không tồn tại.
    /// </summary>
    public static string? GetPropertyOrDefault(this JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;
    }
}
