// File    : AstCompilerTests.cs
// Module  : Engines
// Layer   : Tests
// Purpose : Unit tests cho AstCompiler — compile IExpressionNode → Func, evaluate.

using ICare247.Application.Engines;
using ICare247.Domain.Ast;
using ICare247.Domain.ValueObjects;

namespace ICare247.Application.Tests.Engines;

public sealed class AstCompilerTests
{
    private readonly AstCompiler _compiler;
    private readonly AstParser _parser = new();

    public AstCompilerTests()
    {
        var registry = new FunctionRegistry();
        BuiltinFunctions.RegisterAll(registry);
        _compiler = new AstCompiler(registry);
    }

    /// <summary>Helper: parse JSON → compile → evaluate với context.</summary>
    private object? Eval(string json, IDictionary<string, object?>? values = null)
    {
        var node = _parser.Parse(json);
        var func = _compiler.Compile(node);
        var ctx = values is not null ? new EvaluationContext(values) : EvaluationContext.Empty;
        return func(ctx);
    }

    // ── Literal ─────────────────────────────────────────────────

    [Fact]
    public void Literal_Number_ReturnsValue()
    {
        var result = Eval("""{"type":"literal","value":42}""");
        Assert.Equal(42, Convert.ToInt32(result));
    }

    [Fact]
    public void Literal_String_ReturnsValue()
    {
        var result = Eval("""{"type":"literal","value":"hello"}""");
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Literal_Null_ReturnsNull()
    {
        var result = Eval("""{"type":"literal","value":null}""");
        Assert.Null(result);
    }

    // ── Identifier ──────────────────────────────────────────────

    [Fact]
    public void Identifier_ExistingField_ReturnsValue()
    {
        var values = new Dictionary<string, object?> { ["Age"] = 25 };
        var result = Eval("""{"type":"identifier","name":"Age"}""", values);
        Assert.Equal(25, result);
    }

    [Fact]
    public void Identifier_MissingField_ReturnsNull()
    {
        var result = Eval("""{"type":"identifier","name":"Missing"}""");
        Assert.Null(result);
    }

    [Fact]
    public void Identifier_CaseInsensitive()
    {
        var values = new Dictionary<string, object?> { ["age"] = 30 };
        var result = Eval("""{"type":"identifier","name":"AGE"}""", values);
        Assert.Equal(30, result);
    }

    // ── Arithmetic ──────────────────────────────────────────────

    [Theory]
    [InlineData("+", 10, 3, 13.0)]
    [InlineData("-", 10, 3, 7.0)]
    [InlineData("*", 10, 3, 30.0)]
    [InlineData("/", 10, 4, 2.5)]
    [InlineData("%", 10, 3, 1.0)]
    public void Binary_Arithmetic_ReturnsCorrectResult(string op, int left, int right, double expected)
    {
        var json = $$"""
        {
          "type":"binary","op":"{{op}}",
          "left":{"type":"literal","value":{{left}}},
          "right":{"type":"literal","value":{{right}}}
        }
        """;
        var result = Eval(json);
        Assert.Equal(expected, Convert.ToDouble(result));
    }

    [Fact]
    public void Binary_DivisionByZero_ReturnsNull()
    {
        var json = """
        {
          "type":"binary","op":"/",
          "left":{"type":"literal","value":10},
          "right":{"type":"literal","value":0}
        }
        """;
        Assert.Null(Eval(json));
    }

    [Fact]
    public void Binary_ModuloByZero_ReturnsNull()
    {
        var json = """
        {
          "type":"binary","op":"%",
          "left":{"type":"literal","value":10},
          "right":{"type":"literal","value":0}
        }
        """;
        Assert.Null(Eval(json));
    }

    [Fact]
    public void Binary_ArithmeticWithNull_ReturnsNull()
    {
        var json = """
        {
          "type":"binary","op":"+",
          "left":{"type":"literal","value":10},
          "right":{"type":"literal","value":null}
        }
        """;
        // Khi cả 2 không phải string, null operand → null
        Assert.Null(Eval(json));
    }

    // ── String concat ───────────────────────────────────────────

    [Fact]
    public void Binary_Add_StringConcat()
    {
        var json = """
        {
          "type":"binary","op":"+",
          "left":{"type":"literal","value":"Hello "},
          "right":{"type":"literal","value":"World"}
        }
        """;
        Assert.Equal("Hello World", Eval(json));
    }

    [Fact]
    public void Binary_Add_StringAndNumber_Concat()
    {
        var json = """
        {
          "type":"binary","op":"+",
          "left":{"type":"literal","value":"Age: "},
          "right":{"type":"literal","value":25}
        }
        """;
        Assert.Equal("Age: 25", Eval(json));
    }

    // ── Comparison ──────────────────────────────────────────────

    [Theory]
    [InlineData("==", 5, 5, true)]
    [InlineData("==", 5, 3, false)]
    [InlineData("!=", 5, 3, true)]
    [InlineData(">", 5, 3, true)]
    [InlineData(">=", 5, 5, true)]
    [InlineData("<", 3, 5, true)]
    [InlineData("<=", 5, 5, true)]
    public void Binary_Comparison_ReturnsCorrectResult(string op, int left, int right, bool expected)
    {
        var json = $$"""
        {
          "type":"binary","op":"{{op}}",
          "left":{"type":"literal","value":{{left}}},
          "right":{"type":"literal","value":{{right}}}
        }
        """;
        Assert.Equal(expected, Eval(json));
    }

    [Fact]
    public void Binary_Equality_NullEqualsNull()
    {
        var json = """
        {
          "type":"binary","op":"==",
          "left":{"type":"literal","value":null},
          "right":{"type":"literal","value":null}
        }
        """;
        Assert.Equal(true, Eval(json));
    }

    [Fact]
    public void Binary_Equality_NullNotEqualsValue()
    {
        var json = """
        {
          "type":"binary","op":"==",
          "left":{"type":"literal","value":null},
          "right":{"type":"literal","value":1}
        }
        """;
        Assert.Equal(false, Eval(json));
    }

    // ── Logic ───────────────────────────────────────────────────

    [Fact]
    public void Binary_And_TrueAndTrue_ReturnsTrue()
    {
        var json = """
        {
          "type":"binary","op":"&&",
          "left":{"type":"literal","value":true},
          "right":{"type":"literal","value":true}
        }
        """;
        Assert.Equal(true, Eval(json));
    }

    [Fact]
    public void Binary_Or_FalseOrTrue_ReturnsTrue()
    {
        var json = """
        {
          "type":"binary","op":"||",
          "left":{"type":"literal","value":false},
          "right":{"type":"literal","value":true}
        }
        """;
        Assert.Equal(true, Eval(json));
    }

    // ── Unary ───────────────────────────────────────────────────

    [Fact]
    public void Unary_Not_True_ReturnsFalse()
    {
        var json = """
        {
          "type":"unary","op":"!",
          "operand":{"type":"literal","value":true}
        }
        """;
        Assert.Equal(false, Eval(json));
    }

    [Fact]
    public void Unary_Negate_Number()
    {
        var json = """
        {
          "type":"unary","op":"-",
          "operand":{"type":"literal","value":5}
        }
        """;
        Assert.Equal(-5.0, Eval(json));
    }

    [Fact]
    public void Unary_Negate_Null_ReturnsNull()
    {
        var json = """
        {
          "type":"unary","op":"-",
          "operand":{"type":"literal","value":null}
        }
        """;
        Assert.Null(Eval(json));
    }

    // ── FunctionCall ────────────────────────────────────────────

    [Fact]
    public void Function_Len_ReturnsStringLength()
    {
        var json = """
        {
          "type":"function_call","name":"len",
          "args":[{"type":"literal","value":"hello"}]
        }
        """;
        Assert.Equal(5, Eval(json));
    }

    [Fact]
    public void Function_Iif_TrueCondition_ReturnsThenValue()
    {
        var json = """
        {
          "type":"function_call","name":"iif",
          "args":[
            {"type":"literal","value":true},
            {"type":"literal","value":"yes"},
            {"type":"literal","value":"no"}
          ]
        }
        """;
        Assert.Equal("yes", Eval(json));
    }

    [Fact]
    public void Function_Coalesce_ReturnsFirstNonNull()
    {
        var json = """
        {
          "type":"function_call","name":"coalesce",
          "args":[
            {"type":"literal","value":null},
            {"type":"literal","value":null},
            {"type":"literal","value":42}
          ]
        }
        """;
        Assert.Equal(42, Convert.ToInt32(Eval(json)));
    }

    [Fact]
    public void Function_Round_ReturnsRoundedValue()
    {
        var json = """
        {
          "type":"function_call","name":"round",
          "args":[
            {"type":"literal","value":3.456},
            {"type":"literal","value":2}
          ]
        }
        """;
        Assert.Equal(3.46, Eval(json));
    }

    // ── MemberAccess ────────────────────────────────────────────

    [Fact]
    public void MemberAccess_DateTime_Year()
    {
        var values = new Dictionary<string, object?> { ["BirthDate"] = new DateTime(1990, 5, 15) };
        var json = """
        {
          "type":"member_access",
          "object":{"type":"identifier","name":"BirthDate"},
          "property":"Year"
        }
        """;
        Assert.Equal(1990, Eval(json, values));
    }

    [Fact]
    public void MemberAccess_String_Length()
    {
        var values = new Dictionary<string, object?> { ["Name"] = "Alice" };
        var json = """
        {
          "type":"member_access",
          "object":{"type":"identifier","name":"Name"},
          "property":"length"
        }
        """;
        Assert.Equal(5, Eval(json, values));
    }

    [Fact]
    public void MemberAccess_NullObject_ReturnsNull()
    {
        var json = """
        {
          "type":"member_access",
          "object":{"type":"identifier","name":"Missing"},
          "property":"Year"
        }
        """;
        Assert.Null(Eval(json));
    }

    // ── Complex expression ──────────────────────────────────────

    [Fact]
    public void Complex_NestedExpression_EvaluatesCorrectly()
    {
        // iif(Age >= 18, "Adult", "Minor")
        var json = """
        {
          "type":"function_call","name":"iif",
          "args":[
            {
              "type":"binary","op":">=",
              "left":{"type":"identifier","name":"Age"},
              "right":{"type":"literal","value":18}
            },
            {"type":"literal","value":"Adult"},
            {"type":"literal","value":"Minor"}
          ]
        }
        """;

        var adult = Eval(json, new Dictionary<string, object?> { ["Age"] = 25 });
        Assert.Equal("Adult", adult);

        var minor = Eval(json, new Dictionary<string, object?> { ["Age"] = 15 });
        Assert.Equal("Minor", minor);
    }

    // ── Error cases ─────────────────────────────────────────────

    [Fact]
    public void Function_UnknownName_ThrowsAstEvalException()
    {
        var json = """
        {
          "type":"function_call","name":"unknownFunc",
          "args":[]
        }
        """;
        var node = _parser.Parse(json);
        Assert.Throws<AstEvalException>(() => _compiler.Compile(node));
    }

    [Fact]
    public void Function_WrongArgCount_ThrowsAstEvalException()
    {
        // len cần 1 arg, truyền 0
        var json = """
        {
          "type":"function_call","name":"len",
          "args":[]
        }
        """;
        var node = _parser.Parse(json);
        Assert.Throws<AstEvalException>(() => _compiler.Compile(node));
    }

    [Fact]
    public void Binary_UnsupportedOperator_ThrowsAstEvalException()
    {
        var json = """
        {
          "type":"binary","op":"^",
          "left":{"type":"literal","value":1},
          "right":{"type":"literal","value":2}
        }
        """;
        var node = _parser.Parse(json);
        var func = _compiler.Compile(node);
        Assert.Throws<AstEvalException>(() => func(EvaluationContext.Empty));
    }
}
