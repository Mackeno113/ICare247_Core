// File    : AstParserTests.cs
// Module  : Engines
// Layer   : Tests
// Purpose : Unit tests cho AstParser — parse Expression_Json → IExpressionNode.

using ICare247.Application.Engines;
using ICare247.Domain.Ast;

namespace ICare247.Application.Tests.Engines;

public sealed class AstParserTests
{
    private readonly AstParser _parser = new();

    // ── Literal ─────────────────────────────────────────────────

    [Fact]
    public void Parse_LiteralNumber_ReturnsLiteralNode()
    {
        var json = """{"type":"literal","value":42}""";
        var node = _parser.Parse(json);

        var lit = Assert.IsType<LiteralNode>(node);
        Assert.Equal(42, Convert.ToInt32(lit.Value));
    }

    [Fact]
    public void Parse_LiteralString_ReturnsLiteralNode()
    {
        var json = """{"type":"literal","value":"hello"}""";
        var node = _parser.Parse(json);

        var lit = Assert.IsType<LiteralNode>(node);
        Assert.Equal("hello", lit.Value);
    }

    [Fact]
    public void Parse_LiteralBoolTrue_ReturnsLiteralNode()
    {
        var json = """{"type":"literal","value":true}""";
        var node = _parser.Parse(json);

        var lit = Assert.IsType<LiteralNode>(node);
        Assert.Equal(true, lit.Value);
    }

    [Fact]
    public void Parse_LiteralNull_ReturnsLiteralNode()
    {
        var json = """{"type":"literal","value":null}""";
        var node = _parser.Parse(json);

        var lit = Assert.IsType<LiteralNode>(node);
        Assert.Null(lit.Value);
    }

    [Fact]
    public void Parse_LiteralWithoutValue_ReturnsNullLiteral()
    {
        var json = """{"type":"literal"}""";
        var node = _parser.Parse(json);

        var lit = Assert.IsType<LiteralNode>(node);
        Assert.Null(lit.Value);
    }

    // ── Identifier ──────────────────────────────────────────────

    [Fact]
    public void Parse_Identifier_ReturnsIdentifierNode()
    {
        var json = """{"type":"identifier","name":"FieldA"}""";
        var node = _parser.Parse(json);

        var id = Assert.IsType<IdentifierNode>(node);
        Assert.Equal("FieldA", id.Name);
    }

    // ── Binary ──────────────────────────────────────────────────

    [Fact]
    public void Parse_BinaryAdd_ReturnsBinaryNode()
    {
        var json = """
        {
          "type": "binary",
          "op": "+",
          "left": {"type":"identifier","name":"A"},
          "right": {"type":"literal","value":10}
        }
        """;
        var node = _parser.Parse(json);

        var bin = Assert.IsType<BinaryNode>(node);
        Assert.Equal("+", bin.Operator);
        Assert.IsType<IdentifierNode>(bin.Left);
        Assert.IsType<LiteralNode>(bin.Right);
    }

    // ── Unary ───────────────────────────────────────────────────

    [Fact]
    public void Parse_UnaryNot_ReturnsUnaryNode()
    {
        var json = """
        {
          "type": "unary",
          "op": "!",
          "operand": {"type":"literal","value":true}
        }
        """;
        var node = _parser.Parse(json);

        var un = Assert.IsType<UnaryNode>(node);
        Assert.Equal("!", un.Operator);
        Assert.IsType<LiteralNode>(un.Operand);
    }

    // ── FunctionCall ────────────────────────────────────────────

    [Fact]
    public void Parse_FunctionCall_ReturnsFunctionCallNode()
    {
        var json = """
        {
          "type": "function_call",
          "name": "len",
          "args": [{"type":"literal","value":"hello"}]
        }
        """;
        var node = _parser.Parse(json);

        var fn = Assert.IsType<FunctionCallNode>(node);
        Assert.Equal("len", fn.FunctionName);
        Assert.Single(fn.Args);
    }

    // ── MemberAccess ────────────────────────────────────────────

    [Fact]
    public void Parse_MemberAccess_ReturnsMemberAccessNode()
    {
        var json = """
        {
          "type": "member_access",
          "object": {"type":"identifier","name":"BirthDate"},
          "property": "Year"
        }
        """;
        var node = _parser.Parse(json);

        var ma = Assert.IsType<MemberAccessNode>(node);
        Assert.Equal("Year", ma.Property);
        Assert.IsType<IdentifierNode>(ma.Object);
    }

    // ── Validation / Error cases ────────────────────────────────

    [Fact]
    public void Parse_EmptyString_ThrowsAstParseException()
    {
        Assert.Throws<AstParseException>(() => _parser.Parse(""));
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsAstParseException()
    {
        Assert.Throws<AstParseException>(() => _parser.Parse("not json"));
    }

    [Fact]
    public void Parse_MissingType_ThrowsAstParseException()
    {
        Assert.Throws<AstParseException>(() => _parser.Parse("""{"value":1}"""));
    }

    [Fact]
    public void Parse_UnknownType_ThrowsAstParseException()
    {
        Assert.Throws<AstParseException>(() => _parser.Parse("""{"type":"unknown"}"""));
    }

    [Fact]
    public void Parse_ExceedsMaxSize_ThrowsAstParseException()
    {
        // Max size = 64KB, tạo JSON vượt quá
        var parser = new AstParser(maxSize: 100);
        var bigJson = "{\"type\":\"literal\",\"value\":\"" + new string('x', 200) + "\"}";
        Assert.Throws<AstParseException>(() => parser.Parse(bigJson));
    }

    [Fact]
    public void Parse_ExceedsMaxDepth_ThrowsAstParseException()
    {
        // Tạo nested binary depth > 2
        var parser = new AstParser(maxDepth: 2);
        var json = """
        {
          "type": "binary", "op": "+",
          "left": {
            "type": "binary", "op": "+",
            "left": {
              "type": "binary", "op": "+",
              "left": {"type":"literal","value":1},
              "right": {"type":"literal","value":2}
            },
            "right": {"type":"literal","value":3}
          },
          "right": {"type":"literal","value":4}
        }
        """;
        Assert.Throws<AstParseException>(() => parser.Parse(json));
    }

    // ── member (alias) ──────────────────────────────────────────

    [Fact]
    public void Parse_MemberAlias_ReturnsMemberAccessNode()
    {
        var json = """
        {
          "type": "member",
          "object": {"type":"identifier","name":"X"},
          "property": "length"
        }
        """;
        var node = _parser.Parse(json);
        Assert.IsType<MemberAccessNode>(node);
    }
}
