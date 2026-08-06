// File    : EventActionParamTests.cs
// Module  : Engines
// Layer   : Tests
// Purpose : Unit tests cho EventActionParam — đọc Action_Param_Json + resolve placeholder (AUDIT-5).

using ICare247.Application.Engines;
using ICare247.Domain.ValueObjects;

namespace ICare247.Application.Tests.Engines;

public sealed class EventActionParamTests
{
    // ── Parse ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{ not valid json")]
    [InlineData("[unterminated")]
    public void Parse_EmptyOrInvalid_ReturnsNull(string? json)
    {
        Assert.Null(EventActionParam.Parse(json));
    }

    [Fact]
    public void Parse_ValidJson_ReturnsElement()
    {
        var el = EventActionParam.Parse("""{"targetField":"Total"}""");
        Assert.NotNull(el);
        Assert.Equal("Total", EventActionParam.GetString(el, "targetField"));
    }

    // ── GetString ───────────────────────────────────────────────────────────

    [Fact]
    public void GetString_ExistingProperty_ReturnsValue()
    {
        var el = EventActionParam.Parse("""{"messageKey":"msg.age","severity":"Warning"}""");
        Assert.Equal("msg.age", EventActionParam.GetString(el, "messageKey"));
        Assert.Equal("Warning", EventActionParam.GetString(el, "severity"));
    }

    [Fact]
    public void GetString_MissingOrNullElement_ReturnsNull()
    {
        var el = EventActionParam.Parse("""{"a":"x"}""");
        Assert.Null(EventActionParam.GetString(el, "notThere"));
        Assert.Null(EventActionParam.GetString(null, "a"));
    }

    // ── GetElement ──────────────────────────────────────────────────────────

    [Fact]
    public void GetElement_Nested_ReturnsElement_ElseNull()
    {
        var el = EventActionParam.Parse("""{"valueExpression":{"type":"literal","value":42}}""");
        var nested = EventActionParam.GetElement(el, "valueExpression");
        Assert.NotNull(nested);
        Assert.Contains("literal", nested.Value.GetRawText());
        Assert.Null(EventActionParam.GetElement(el, "missing"));
    }

    // ── GetStringArray ──────────────────────────────────────────────────────

    [Fact]
    public void GetStringArray_ArrayOfStrings_ReturnsList()
    {
        var el = EventActionParam.Parse("""{"targetFields":["DateOfBirth","Age"]}""");
        Assert.Equal(new[] { "DateOfBirth", "Age" }, EventActionParam.GetStringArray(el, "targetFields"));
    }

    [Fact]
    public void GetStringArray_MixedTypes_KeepsOnlyStrings()
    {
        var el = EventActionParam.Parse("""{"xs":["a",1,"b",null,true]}""");
        Assert.Equal(new[] { "a", "b" }, EventActionParam.GetStringArray(el, "xs"));
    }

    [Fact]
    public void GetStringArray_NotArrayOrMissing_ReturnsNull()
    {
        var el = EventActionParam.Parse("""{"x":"notArray"}""");
        Assert.Null(EventActionParam.GetStringArray(el, "x"));
        Assert.Null(EventActionParam.GetStringArray(el, "missing"));
    }

    // ── ResolvePlaceholders ─────────────────────────────────────────────────

    [Fact]
    public void ResolvePlaceholders_SubstitutesContextValues()
    {
        var ctx = new EvaluationContext(new Dictionary<string, object?>
        {
            ["Province"] = "HN",
            ["District"] = "BaDinh"
        });
        Assert.Equal(
            "/api/options?province=HN&district=BaDinh",
            EventActionParam.ResolvePlaceholders("/api/options?province={Province}&district={District}", ctx));
    }

    [Fact]
    public void ResolvePlaceholders_MissingField_ReplacedWithEmpty()
    {
        var ctx = EvaluationContext.Empty;
        Assert.Equal("/api?p=", EventActionParam.ResolvePlaceholders("/api?p={Nope}", ctx));
    }

    [Fact]
    public void ResolvePlaceholders_NoPlaceholder_Unchanged()
    {
        Assert.Equal("/api/static", EventActionParam.ResolvePlaceholders("/api/static", EvaluationContext.Empty));
    }
}
