// File    : AstEngineTests.cs
// Module  : Engines
// Layer   : Tests
// Purpose : Unit tests cho AstEngine — orchestration + compiled cache.

using ICare247.Application.Engines;
using ICare247.Domain.ValueObjects;

namespace ICare247.Application.Tests.Engines;

public sealed class AstEngineTests
{
    private readonly AstEngine _engine;

    public AstEngineTests()
    {
        var registry = new FunctionRegistry();
        BuiltinFunctions.RegisterAll(registry);
        var parser = new AstParser();
        var compiler = new AstCompiler(registry);
        _engine = new AstEngine(parser, compiler);
    }

    [Fact]
    public void Evaluate_SimpleLiteral_ReturnsValue()
    {
        var result = _engine.Evaluate("""{"type":"literal","value":42}""", EvaluationContext.Empty);
        Assert.Equal(42, Convert.ToInt32(result));
    }

    [Fact]
    public void Evaluate_WithContext_ResolvesIdentifier()
    {
        var ctx = new EvaluationContext(new Dictionary<string, object?> { ["X"] = 10 });
        var json = """
        {
          "type":"binary","op":"*",
          "left":{"type":"identifier","name":"X"},
          "right":{"type":"literal","value":2}
        }
        """;
        var result = _engine.Evaluate(json, ctx);
        Assert.Equal(20.0, result);
    }

    [Fact]
    public void Evaluate_CachesCompiledDelegate()
    {
        var json = """{"type":"literal","value":99}""";
        var ctx = EvaluationContext.Empty;

        // Gọi 2 lần — lần 2 phải dùng cache
        var r1 = _engine.Evaluate(json, ctx);
        var r2 = _engine.Evaluate(json, ctx);

        Assert.Equal(r1, r2);
        Assert.Equal(99, Convert.ToInt32(r1));
    }

    [Fact]
    public void Evaluate_DifferentContexts_SameExpression()
    {
        var json = """{"type":"identifier","name":"Score"}""";

        var r1 = _engine.Evaluate(json, new EvaluationContext(
            new Dictionary<string, object?> { ["Score"] = 100 }));
        var r2 = _engine.Evaluate(json, new EvaluationContext(
            new Dictionary<string, object?> { ["Score"] = 200 }));

        Assert.Equal(100, Convert.ToInt32(r1));
        Assert.Equal(200, Convert.ToInt32(r2));
    }

    [Fact]
    public void Parse_ReturnsAstNode()
    {
        var node = _engine.Parse("""{"type":"literal","value":1}""");
        Assert.NotNull(node);
    }

    [Fact]
    public void Compile_ReturnsDelegate()
    {
        var node = _engine.Parse("""{"type":"literal","value":1}""");
        var func = _engine.Compile(node);
        Assert.NotNull(func);
        Assert.Equal(1, Convert.ToInt32(func(EvaluationContext.Empty)));
    }

    [Fact]
    public void Evaluate_ComplexExpression_EndToEnd()
    {
        // round(A * B / 100, 2) — tính phần trăm
        var json = """
        {
          "type":"function_call","name":"round",
          "args":[
            {
              "type":"binary","op":"/",
              "left":{
                "type":"binary","op":"*",
                "left":{"type":"identifier","name":"A"},
                "right":{"type":"identifier","name":"B"}
              },
              "right":{"type":"literal","value":100}
            },
            {"type":"literal","value":2}
          ]
        }
        """;

        var ctx = new EvaluationContext(new Dictionary<string, object?>
        {
            ["A"] = 250,
            ["B"] = 33
        });

        var result = _engine.Evaluate(json, ctx);
        Assert.Equal(82.5, result);
    }

    [Fact]
    public void Evaluate_NullPropagation_EndToEnd()
    {
        // A + B khi A = null → null
        var json = """
        {
          "type":"binary","op":"+",
          "left":{"type":"identifier","name":"A"},
          "right":{"type":"literal","value":10}
        }
        """;

        var result = _engine.Evaluate(json, EvaluationContext.Empty);
        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_InvalidJson_ThrowsAstParseException()
    {
        Assert.Throws<AstParseException>(
            () => _engine.Evaluate("bad json", EvaluationContext.Empty));
    }
}
