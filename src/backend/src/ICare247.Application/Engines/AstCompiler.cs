// File    : AstCompiler.cs
// Module  : Engines
// Layer   : Application
// Purpose : Compile IExpressionNode → Func<EvaluationContext, object?>.

using ICare247.Domain.Ast;
using ICare247.Domain.ValueObjects;

namespace ICare247.Application.Engines;

/// <summary>
/// Compile cây AST thành delegate thực thi.
/// Null-safe: mọi operation với null operand → null (không throw).
/// Division by 0 → null.
/// </summary>
public sealed class AstCompiler
{
    private readonly FunctionRegistry _functions;

    public AstCompiler(FunctionRegistry functions)
    {
        _functions = functions;
    }

    /// <summary>
    /// Compile IExpressionNode → Func&lt;EvaluationContext, object?&gt;.
    /// </summary>
    public Func<EvaluationContext, object?> Compile(IExpressionNode node)
    {
        return node.Kind switch
        {
            NodeKind.Literal => CompileLiteral((LiteralNode)node),
            NodeKind.Identifier => CompileIdentifier((IdentifierNode)node),
            NodeKind.Binary => CompileBinary((BinaryNode)node),
            NodeKind.Unary => CompileUnary((UnaryNode)node),
            NodeKind.FunctionCall => CompileFunctionCall((FunctionCallNode)node),
            NodeKind.MemberAccess => CompileMemberAccess((MemberAccessNode)node),
            _ => throw new AstEvalException($"NodeKind không hỗ trợ: {node.Kind}.")
        };
    }

    private static Func<EvaluationContext, object?> CompileLiteral(LiteralNode node)
    {
        var value = node.Value;
        return _ => value;
    }

    private static Func<EvaluationContext, object?> CompileIdentifier(IdentifierNode node)
    {
        var name = node.Name;
        return ctx => ctx.GetValue(name);
    }

    private Func<EvaluationContext, object?> CompileBinary(BinaryNode node)
    {
        var left = Compile(node.Left);
        var right = Compile(node.Right);
        var op = node.Operator;

        return ctx =>
        {
            var lval = left(ctx);
            var rval = right(ctx);

            return op switch
            {
                // ── Arithmetic ────────────────────────────────
                "+" => EvalAdd(lval, rval),
                "-" => EvalArithmetic(lval, rval, (a, b) => a - b),
                "*" => EvalArithmetic(lval, rval, (a, b) => a * b),
                "/" => EvalDivide(lval, rval),
                "%" => EvalModulo(lval, rval),

                // ── Comparison ────────────────────────────────
                "==" => Equals(lval, rval),
                "!=" => !Equals(lval, rval),
                ">" => EvalCompare(lval, rval) > 0,
                ">=" => EvalCompare(lval, rval) >= 0,
                "<" => EvalCompare(lval, rval) < 0,
                "<=" => EvalCompare(lval, rval) <= 0,

                // ── Logic ─────────────────────────────────────
                "&&" => EvalAnd(lval, rval),
                "||" => EvalOr(lval, rval),

                _ => throw new AstEvalException($"Operator không hỗ trợ: '{op}'.")
            };
        };
    }

    private Func<EvaluationContext, object?> CompileUnary(UnaryNode node)
    {
        var operand = Compile(node.Operand);
        var op = node.Operator;

        return ctx =>
        {
            var val = operand(ctx);

            return op switch
            {
                "!" => val is null ? null : !(BuiltinFunctions.ToBool(val) ?? false),
                "-" => val is null ? null : NegateNumber(val),
                _ => throw new AstEvalException($"Unary operator không hỗ trợ: '{op}'.")
            };
        };
    }

    private Func<EvaluationContext, object?> CompileFunctionCall(FunctionCallNode node)
    {
        var funcName = node.FunctionName;
        var entry = _functions.Get(funcName);

        // Validate arg count tại compile time
        _functions.ValidateArgCount(funcName, node.Args.Count);

        // Compile tất cả arguments
        var compiledArgs = node.Args.Select(Compile).ToArray();

        return ctx =>
        {
            // Evaluate arguments
            var args = new object?[compiledArgs.Length];
            for (int i = 0; i < compiledArgs.Length; i++)
                args[i] = compiledArgs[i](ctx);

            return entry.Func(args, ctx);
        };
    }

    private Func<EvaluationContext, object?> CompileMemberAccess(MemberAccessNode node)
    {
        var obj = Compile(node.Object);
        var property = node.Property;

        return ctx =>
        {
            var objVal = obj(ctx);
            if (objVal is null) return null;

            // Hỗ trợ DateTime properties
            if (objVal is DateTime dt)
            {
                return property.ToLowerInvariant() switch
                {
                    "year" => dt.Year,
                    "month" => dt.Month,
                    "day" => dt.Day,
                    "hour" => dt.Hour,
                    "minute" => dt.Minute,
                    "second" => dt.Second,
                    "dayofweek" => (int)dt.DayOfWeek,
                    _ => null
                };
            }

            // Hỗ trợ string properties
            if (objVal is string str)
            {
                return property.ToLowerInvariant() switch
                {
                    "length" => str.Length,
                    _ => null
                };
            }

            return null;
        };
    }

    // ── Arithmetic helpers (null-safe) ───────────────────────

    /// <summary>
    /// Cộng: hỗ trợ cả number + string concat.
    /// </summary>
    private static object? EvalAdd(object? left, object? right)
    {
        // String concat nếu 1 trong 2 là string
        if (left is string || right is string)
            return $"{left}{right}";

        var l = BuiltinFunctions.ToDouble(left);
        var r = BuiltinFunctions.ToDouble(right);
        if (l is null || r is null) return null;
        return l.Value + r.Value;
    }

    private static object? EvalArithmetic(object? left, object? right, Func<double, double, double> op)
    {
        var l = BuiltinFunctions.ToDouble(left);
        var r = BuiltinFunctions.ToDouble(right);
        if (l is null || r is null) return null;
        return op(l.Value, r.Value);
    }

    /// <summary>
    /// Chia: division by 0 → null (không throw).
    /// </summary>
    private static object? EvalDivide(object? left, object? right)
    {
        var l = BuiltinFunctions.ToDouble(left);
        var r = BuiltinFunctions.ToDouble(right);
        if (l is null || r is null) return null;
        if (r.Value == 0.0) return null; // Spec: division by 0 → null
        return l.Value / r.Value;
    }

    private static object? EvalModulo(object? left, object? right)
    {
        var l = BuiltinFunctions.ToDouble(left);
        var r = BuiltinFunctions.ToDouble(right);
        if (l is null || r is null) return null;
        if (r.Value == 0.0) return null;
        return l.Value % r.Value;
    }

    // ── Comparison helpers (null-safe) ───────────────────────

    /// <summary>
    /// So sánh 2 giá trị. Trả 0 nếu bằng, &lt;0 nếu left nhỏ hơn, &gt;0 nếu left lớn hơn.
    /// Null-safe: null == null → 0, null vs non-null → null (treat as false in boolean context).
    /// </summary>
    private static int EvalCompare(object? left, object? right)
    {
        if (left is null && right is null) return 0;
        if (left is null || right is null) return 0; // Null-safe: trả 0 (false cho >, <, v.v.)

        // Cả 2 là number
        var l = BuiltinFunctions.ToDouble(left);
        var r = BuiltinFunctions.ToDouble(right);
        if (l is not null && r is not null)
            return l.Value.CompareTo(r.Value);

        // Cả 2 là DateTime
        var ld = BuiltinFunctions.ToDateTime(left);
        var rd = BuiltinFunctions.ToDateTime(right);
        if (ld is not null && rd is not null)
            return ld.Value.CompareTo(rd.Value);

        // Fallback: so sánh string
        return string.Compare(
            Convert.ToString(left),
            Convert.ToString(right),
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Equality check (== operator). Null-safe.
    /// </summary>
    private static new bool Equals(object? left, object? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;

        // Number comparison
        var l = BuiltinFunctions.ToDouble(left);
        var r = BuiltinFunctions.ToDouble(right);
        if (l is not null && r is not null)
            return Math.Abs(l.Value - r.Value) < double.Epsilon;

        // Bool comparison
        if (left is bool lb && right is bool rb)
            return lb == rb;

        // DateTime comparison
        var ld = BuiltinFunctions.ToDateTime(left);
        var rd = BuiltinFunctions.ToDateTime(right);
        if (ld is not null && rd is not null)
            return ld.Value == rd.Value;

        // Fallback: string comparison
        return string.Equals(
            Convert.ToString(left),
            Convert.ToString(right),
            StringComparison.OrdinalIgnoreCase);
    }

    // ── Logic helpers ────────────────────────────────────────

    private static object? EvalAnd(object? left, object? right)
    {
        var l = BuiltinFunctions.ToBool(left);
        var r = BuiltinFunctions.ToBool(right);
        if (l is null || r is null) return null;
        return l.Value && r.Value;
    }

    private static object? EvalOr(object? left, object? right)
    {
        var l = BuiltinFunctions.ToBool(left);
        var r = BuiltinFunctions.ToBool(right);
        if (l is null || r is null) return null;
        return l.Value || r.Value;
    }

    private static object? NegateNumber(object val)
    {
        var d = BuiltinFunctions.ToDouble(val);
        return d.HasValue ? -d.Value : null;
    }
}
