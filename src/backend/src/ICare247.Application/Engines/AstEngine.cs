// File    : AstEngine.cs
// Module  : Engines
// Layer   : Application
// Purpose : Concrete implementation của IAstEngine — parse + compile + execute + cache.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using ICare247.Domain.Ast;
using ICare247.Domain.Engine;
using ICare247.Domain.ValueObjects;

namespace ICare247.Application.Engines;

/// <summary>
/// IAstEngine implementation.
/// Pipeline: Parse (JSON → AST) → Compile (AST → Func) → Execute (Func + Context → Result).
/// Compiled delegates được cache in-memory bằng hash(expressionJson).
/// </summary>
public sealed class AstEngine : IAstEngine
{
    private readonly AstParser _parser;
    private readonly AstCompiler _compiler;

    // Cache compiled delegates — ConcurrentDictionary vì thread-safe, không cần Redis
    // (compiled Func tồn tại suốt app lifetime, không serialize được)
    private readonly ConcurrentDictionary<string, Func<EvaluationContext, object?>> _compiledCache = new();

    public AstEngine(AstParser parser, AstCompiler compiler)
    {
        _parser = parser;
        _compiler = compiler;
    }

    /// <inheritdoc />
    public IExpressionNode Parse(string expressionJson)
        => _parser.Parse(expressionJson);

    /// <inheritdoc />
    public Func<EvaluationContext, object?> Compile(IExpressionNode node)
        => _compiler.Compile(node);

    /// <inheritdoc />
    public object? Evaluate(string expressionJson, EvaluationContext context)
    {
        var func = GetOrCompile(expressionJson);
        return func(context);
    }

    /// <summary>
    /// Lấy compiled delegate từ cache, hoặc parse + compile nếu chưa có.
    /// </summary>
    private Func<EvaluationContext, object?> GetOrCompile(string expressionJson)
    {
        var hash = ComputeHash(expressionJson);
        return _compiledCache.GetOrAdd(hash, _ =>
        {
            var node = _parser.Parse(expressionJson);
            return _compiler.Compile(node);
        });
    }

    /// <summary>
    /// SHA256 hash của expression JSON, dùng làm cache key.
    /// </summary>
    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
