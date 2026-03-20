// File    : BuiltinFunctionsTests.cs
// Module  : Engines
// Layer   : Tests
// Purpose : Unit tests cho BuiltinFunctions — type conversion helpers + registered functions.

using ICare247.Application.Engines;
using ICare247.Domain.ValueObjects;

namespace ICare247.Application.Tests.Engines;

public sealed class BuiltinFunctionsTests
{
    private readonly FunctionRegistry _registry;
    private readonly EvaluationContext _emptyCtx = EvaluationContext.Empty;

    public BuiltinFunctionsTests()
    {
        _registry = new FunctionRegistry();
        BuiltinFunctions.RegisterAll(_registry);
    }

    /// <summary>Helper: gọi function với args.</summary>
    private object? Call(string name, params object?[] args)
        => _registry.Get(name).Func(args, _emptyCtx);

    // ── Type conversions ────────────────────────────────────────

    [Theory]
    [InlineData(null, null)]
    [InlineData(42, 42.0)]
    [InlineData(3.14, 3.14)]
    [InlineData("3.5", 3.5)]
    [InlineData(true, 1.0)]
    [InlineData(false, 0.0)]
    public void ToDouble_ConvertsCorrectly(object? input, double? expected)
    {
        Assert.Equal(expected, BuiltinFunctions.ToDouble(input));
    }

    [Fact]
    public void ToDouble_InvalidString_ReturnsNull()
    {
        Assert.Null(BuiltinFunctions.ToDouble("abc"));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData("hello", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("", false)]
    public void ToBool_ConvertsCorrectly(object? input, bool? expected)
    {
        Assert.Equal(expected, BuiltinFunctions.ToBool(input));
    }

    [Fact]
    public void ToDateTime_ValidString_ReturnsDateTime()
    {
        var result = BuiltinFunctions.ToDateTime("2024-01-15");
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2024, 1, 15), result!.Value.Date);
    }

    [Fact]
    public void ToDateTime_Null_ReturnsNull()
    {
        Assert.Null(BuiltinFunctions.ToDateTime(null));
    }

    // ── String functions ────────────────────────────────────────

    [Fact]
    public void Len_ReturnsLength()
    {
        Assert.Equal(5, Call("len", "hello"));
    }

    [Fact]
    public void Len_Null_ReturnsNull()
    {
        Assert.Null(Call("len", new object?[] { null }));
    }

    [Fact]
    public void Trim_RemovesWhitespace()
    {
        Assert.Equal("hello", Call("trim", "  hello  "));
    }

    [Fact]
    public void Upper_ConvertsToUpperCase()
    {
        Assert.Equal("HELLO", Call("upper", "hello"));
    }

    [Fact]
    public void Lower_ConvertsToLowerCase()
    {
        Assert.Equal("hello", Call("lower", "HELLO"));
    }

    [Fact]
    public void Concat_JoinsStrings()
    {
        Assert.Equal("abc", Call("concat", "a", "b", "c"));
    }

    [Fact]
    public void Concat_WithNulls_TreatsAsEmpty()
    {
        Assert.Equal("ac", Call("concat", "a", null, "c"));
    }

    [Fact]
    public void Substring_WithStartOnly()
    {
        Assert.Equal("llo", Call("substring", "hello", 2));
    }

    [Fact]
    public void Substring_WithStartAndLength()
    {
        Assert.Equal("ll", Call("substring", "hello", 2, 2));
    }

    [Fact]
    public void Contains_Found_ReturnsTrue()
    {
        Assert.Equal(true, Call("contains", "hello world", "world"));
    }

    [Fact]
    public void Contains_NotFound_ReturnsFalse()
    {
        Assert.Equal(false, Call("contains", "hello", "xyz"));
    }

    [Fact]
    public void Contains_CaseInsensitive()
    {
        Assert.Equal(true, Call("contains", "Hello", "hello"));
    }

    [Fact]
    public void StartsWith_Match_ReturnsTrue()
    {
        Assert.Equal(true, Call("startsWith", "hello", "hel"));
    }

    [Fact]
    public void EndsWith_Match_ReturnsTrue()
    {
        Assert.Equal(true, Call("endsWith", "hello", "llo"));
    }

    // ── Math functions ──────────────────────────────────────────

    [Fact]
    public void Round_DefaultDecimals()
    {
        Assert.Equal(4.0, Call("round", 3.5));
    }

    [Fact]
    public void Round_WithDecimals()
    {
        Assert.Equal(3.46, Call("round", 3.456, 2));
    }

    [Fact]
    public void Floor_ReturnsFloor()
    {
        Assert.Equal(3.0, Call("floor", 3.7));
    }

    [Fact]
    public void Ceil_ReturnsCeiling()
    {
        Assert.Equal(4.0, Call("ceil", 3.1));
    }

    [Fact]
    public void Abs_ReturnsAbsolute()
    {
        Assert.Equal(5.0, Call("abs", -5));
    }

    [Fact]
    public void Min_ReturnsMinimum()
    {
        Assert.Equal(2.0, Call("min", 5, 2, 8));
    }

    [Fact]
    public void Max_ReturnsMaximum()
    {
        Assert.Equal(8.0, Call("max", 5, 2, 8));
    }

    [Fact]
    public void Min_SkipsNull()
    {
        Assert.Equal(3.0, Call("min", null, 3, 7));
    }

    // ── Logic functions ─────────────────────────────────────────

    [Fact]
    public void Iif_True_ReturnsThenValue()
    {
        Assert.Equal("yes", Call("iif", true, "yes", "no"));
    }

    [Fact]
    public void Iif_False_ReturnsElseValue()
    {
        Assert.Equal("no", Call("iif", false, "yes", "no"));
    }

    [Fact]
    public void IsNull_Null_ReturnsTrue()
    {
        Assert.Equal(true, Call("isNull", new object?[] { null }));
    }

    [Fact]
    public void IsNull_NotNull_ReturnsFalse()
    {
        Assert.Equal(false, Call("isNull", 42));
    }

    [Fact]
    public void Coalesce_ReturnsFirstNonNull()
    {
        Assert.Equal(42, Call("coalesce", null, null, 42, 99));
    }

    [Fact]
    public void Coalesce_AllNull_ReturnsNull()
    {
        Assert.Null(Call("coalesce", null, null));
    }

    // ── Date functions ──────────────────────────────────────────

    [Fact]
    public void Today_ReturnsDate()
    {
        var result = Call("today");
        Assert.IsType<DateTime>(result);
        Assert.Equal(DateTime.Today, result);
    }

    [Fact]
    public void Now_ReturnsDateTime()
    {
        var before = DateTime.Now;
        var result = Call("now");
        var after = DateTime.Now;
        Assert.IsType<DateTime>(result);
        Assert.InRange((DateTime)result!, before, after);
    }

    [Fact]
    public void ToDate_String_ReturnsDateTime()
    {
        var result = Call("toDate", "2024-06-15");
        Assert.IsType<DateTime>(result);
    }

    [Fact]
    public void ToDate_WithFormat_ReturnsDateTime()
    {
        var result = Call("toDate", "15/06/2024", "dd/MM/yyyy");
        Assert.IsType<DateTime>(result);
        Assert.Equal(15, ((DateTime)result!).Day);
    }

    [Fact]
    public void DateDiff_Days_ReturnsCorrectDifference()
    {
        var result = Call("dateDiff", "days",
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 11));
        Assert.Equal(10, result);
    }

    // ── Conversion functions ────────────────────────────────────

    [Fact]
    public void ToNumber_StringToDouble()
    {
        Assert.Equal(3.14, Call("toNumber", "3.14"));
    }

    [Fact]
    public void ToString_NumberToString()
    {
        Assert.Equal("42", Call("toString", 42));
    }

    [Fact]
    public void ToBool_Registered_Works()
    {
        Assert.Equal(true, Call("toBool", 1));
    }

    // ── FunctionRegistry ────────────────────────────────────────

    [Fact]
    public void Registry_Contains_RegisteredFunction()
    {
        Assert.True(_registry.Contains("len"));
        Assert.True(_registry.Contains("LEN")); // case-insensitive
    }

    [Fact]
    public void Registry_Get_UnknownFunction_Throws()
    {
        Assert.Throws<AstEvalException>(() => _registry.Get("doesNotExist"));
    }

    [Fact]
    public void Registry_ValidateArgCount_TooFew_Throws()
    {
        Assert.Throws<AstEvalException>(() => _registry.ValidateArgCount("len", 0));
    }

    [Fact]
    public void Registry_ValidateArgCount_TooMany_Throws()
    {
        Assert.Throws<AstEvalException>(() => _registry.ValidateArgCount("len", 5));
    }
}
