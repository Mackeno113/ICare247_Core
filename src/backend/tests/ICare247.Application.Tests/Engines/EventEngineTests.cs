// File    : EventEngineTests.cs
// Module  : Engines
// Layer   : Tests
// Purpose : Characterization tests cho EventEngine — KHÓA hành vi dispatch action hiện tại trước khi
//           refactor sang Strategy (AUDIT-5). Dùng AstEngine THẬT + stub IEventRepository; _config/
//           _validationEngine = null! (chỉ TRIGGER_VALIDATION dùng, không test ở đây).

using ICare247.Application.Engines;
using ICare247.Application.Engines.EventActions;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Event;
using ICare247.Domain.Engine.Models;
using ICare247.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace ICare247.Application.Tests.Engines;

public sealed class EventEngineTests
{
    private readonly StubEventRepository _repo = new();
    private readonly EventEngine _engine;

    public EventEngineTests()
    {
        var registry = new FunctionRegistry();
        BuiltinFunctions.RegisterAll(registry);
        var astEngine = new AstEngine(new AstParser(), new AstCompiler(registry));

        var handlers = new IEventActionHandler[]
        {
            new SetValueActionHandler(astEngine),
            new SetVisibleActionHandler(astEngine),
            new SetRequiredActionHandler(astEngine),
            new SetReadOnlyActionHandler(astEngine),
            new SetEnabledActionHandler(astEngine),
            new ClearValueActionHandler(),
            new ShowMessageActionHandler(astEngine),
            new ReloadOptionsActionHandler(),
            new TriggerValidationActionHandler(validationEngine: null!, config: null!), // không dùng trong các test này
        };
        _engine = new EventEngine(_repo, astEngine, handlers, NullLogger<EventEngine>.Instance);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static EventAction Action(string code, string paramJson) =>
        new() { ActionId = 1, EventId = 1, ActionCode = code, ActionParamJson = paramJson, OrderNo = 1 };

    private static EventDefinition Ev(string? conditionExpr, params EventAction[] actions) =>
        new() { EventId = 1, FormId = 10, TriggerCode = "OnChange", ConditionExpr = conditionExpr, OrderNo = 1, Actions = actions };

    private Task<UiDeltaResponse> HandleAsync(EvaluationContext? ctx = null, string eventType = "FIELD_CHANGED") =>
        _engine.HandleEventAsync(new FormEvent(eventType, "SrcField", 10, 1, ctx ?? EvaluationContext.Empty));

    // ── SET_VALUE ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetValue_EvaluatesExpression_EmitsDelta()
    {
        _repo.Events = [Ev(null, Action("SET_VALUE",
            """{"targetField":"Total","valueExpression":{"type":"literal","value":"HELLO"}}"""))];

        var r = await HandleAsync();

        var d = Assert.Single(r.Delta);
        Assert.Equal("Total", d.FieldCode);
        Assert.Equal("SET_VALUE", d.Action);
        Assert.Equal("HELLO", d.Data!["value"]);
    }

    // ── SET_VISIBLE (condition) ───────────────────────────────────────────────

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public async Task SetVisible_UsesConditionResult(string literal, bool expected)
    {
        var json = "{\"targetField\":\"Phone\",\"conditionExpression\":{\"type\":\"literal\",\"value\":" + literal + "}}";
        _repo.Events = [Ev(null, Action("SET_VISIBLE", json))];

        var r = await HandleAsync();

        var d = Assert.Single(r.Delta);
        Assert.Equal("SET_VISIBLE", d.Action);
        Assert.Equal(expected, d.Data!["visible"]);
    }

    // ── CLEAR_VALUE ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ClearValue_EmitsNullValueDelta()
    {
        _repo.Events = [Ev(null, Action("CLEAR_VALUE", """{"targetField":"District"}"""))];

        var r = await HandleAsync();

        var d = Assert.Single(r.Delta);
        Assert.Equal("CLEAR_VALUE", d.Action);
        Assert.Equal("District", d.FieldCode);
        Assert.Null(d.Data!["value"]);
    }

    // ── SHOW_MESSAGE ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ShowMessage_NoCondition_EmitsMessageDelta()
    {
        _repo.Events = [Ev(null, Action("SHOW_MESSAGE",
            """{"targetField":"Age","messageKey":"msg.age.under18","severity":"Warning"}"""))];

        var r = await HandleAsync();

        var d = Assert.Single(r.Delta);
        Assert.Equal("SHOW_MESSAGE", d.Action);
        Assert.Equal("msg.age.under18", d.Data!["messageKey"]);
        Assert.Equal("Warning", d.Data!["severity"]);
    }

    // ── RELOAD_OPTIONS (placeholder resolve) ──────────────────────────────────

    [Fact]
    public async Task ReloadOptions_ResolvesPlaceholderFromContext()
    {
        _repo.Events = [Ev(null, Action("RELOAD_OPTIONS",
            """{"targetField":"District","apiEndpoint":"/api/opt?p={Province}","dependsOn":["Province"]}"""))];
        var ctx = new EvaluationContext(new Dictionary<string, object?> { ["Province"] = "HN" });

        var r = await HandleAsync(ctx);

        var d = Assert.Single(r.Delta);
        Assert.Equal("RELOAD_OPTIONS", d.Action);
        Assert.Equal("/api/opt?p=HN", d.Data!["apiEndpoint"]);
    }

    // ── Dispatch edge cases ────────────────────────────────────────────────────

    [Fact]
    public async Task UnknownActionCode_ProducesNoDelta()
    {
        _repo.Events = [Ev(null, Action("FLY_TO_MOON", """{"targetField":"X"}"""))];
        Assert.Empty((await HandleAsync()).Delta);
    }

    [Fact]
    public async Task EventConditionFalse_SkipsActions()
    {
        _repo.Events = [Ev("""{"type":"literal","value":false}""",
            Action("CLEAR_VALUE", """{"targetField":"District"}"""))];
        Assert.Empty((await HandleAsync()).Delta);
    }

    [Fact]
    public async Task InvalidEventType_ReturnsEmpty()
    {
        _repo.Events = [Ev(null, Action("CLEAR_VALUE", """{"targetField":"District"}"""))];
        Assert.Empty((await HandleAsync(eventType: "BOGUS_TYPE")).Delta);
    }

    [Fact]
    public async Task MultipleActions_EmitDeltaEach()
    {
        _repo.Events = [Ev(null,
            Action("CLEAR_VALUE", """{"targetField":"A"}"""),
            Action("CLEAR_VALUE", """{"targetField":"B"}"""))];

        Assert.Equal(2, (await HandleAsync()).Delta.Count);
    }

    // ── Stub ────────────────────────────────────────────────────────────────────

    private sealed class StubEventRepository : IEventRepository
    {
        public IReadOnlyList<EventDefinition> Events { get; set; } = [];

        public Task<IReadOnlyList<EventDefinition>> GetByTriggerAsync(
            int formId, string triggerCode, string? fieldCode, int tenantId, CancellationToken ct = default)
            => Task.FromResult(Events);
    }
}
