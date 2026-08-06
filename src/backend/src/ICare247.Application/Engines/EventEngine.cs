// File    : EventEngine.cs
// Module  : Engines
// Layer   : Application
// Purpose : Concrete implementation của IEventEngine — xử lý form events và tạo UI deltas.
//           Dispatch action qua registry IEventActionHandler (OCP): thêm action = thêm handler + đăng ký
//           DI, KHÔNG sửa engine. Xem Engines/EventActions/.

using System.Diagnostics;
using ICare247.Application.Engines.EventActions;
using ICare247.Application.Interfaces;
using ICare247.Domain.Engine;
using ICare247.Domain.Engine.Models;
using ICare247.Domain.Entities.Event;
using ICare247.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Engines;

/// <summary>
/// IEventEngine implementation.
/// Flow: Nhận FormEvent → map trigger code → load event handlers → evaluate conditions →
/// execute actions tuần tự (dispatch qua registry) → build UiDelta list → trả UiDeltaResponse.
/// </summary>
public sealed class EventEngine : IEventEngine
{
    private readonly IEventRepository _eventRepo;
    private readonly IAstEngine _astEngine;
    private readonly IReadOnlyDictionary<string, IEventActionHandler> _handlers;
    private readonly ILogger<EventEngine> _logger;

    /// <summary>
    /// Mapping từ FormEvent.EventType (client) sang DB Trigger_Code.
    /// </summary>
    private static readonly Dictionary<string, string> EventTypeToTrigger = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FIELD_CHANGED"] = "OnChange",
        ["FIELD_BLUR"] = "OnBlur",
        ["FORM_LOAD"] = "OnLoad",
        ["FORM_SUBMIT"] = "OnSubmit",
        ["SECTION_TOGGLE"] = "OnSectionToggle"
    };

    public EventEngine(
        IEventRepository eventRepo,
        IAstEngine astEngine,
        IEnumerable<IEventActionHandler> actionHandlers,
        ILogger<EventEngine> logger)
    {
        _eventRepo = eventRepo;
        _astEngine = astEngine;
        // Registry theo Action_Code (không phân biệt hoa/thường — như switch ToUpperInvariant cũ).
        _handlers = actionHandlers.ToDictionary(h => h.ActionCode, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UiDeltaResponse> HandleEventAsync(
        FormEvent formEvent,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // ── 1. Map event type sang trigger code ─────────────────────
        if (!EventTypeToTrigger.TryGetValue(formEvent.EventType, out var triggerCode))
        {
            _logger.LogWarning(
                "EventType không hợp lệ: {EventType}, FormId={FormId}",
                formEvent.EventType, formEvent.FormId);
            return UiDeltaResponse.Empty;
        }

        // ── 2. Load event handlers từ repository ────────────────────
        var events = await _eventRepo.GetByTriggerAsync(
            formEvent.FormId,
            triggerCode,
            formEvent.SourceField,
            formEvent.TenantId,
            ct);

        if (events.Count == 0)
            return UiDeltaResponse.Empty;

        // ── 3. Process từng event — evaluate condition + execute actions ──
        var deltas = new List<UiDelta>();
        var context = formEvent.Context;

        foreach (var eventDef in events)
        {
            ct.ThrowIfCancellationRequested();

            // Evaluate condition — skip nếu false
            if (!EvaluateCondition(eventDef, context))
                continue;

            // Execute actions tuần tự theo OrderNo
            foreach (var action in eventDef.Actions)
            {
                ct.ThrowIfCancellationRequested();

                var actionDeltas = await ExecuteActionAsync(action, context, formEvent, ct);

                if (actionDeltas.Count > 0)
                {
                    deltas.AddRange(actionDeltas);

                    // Cập nhật context với giá trị mới từ SET_VALUE để actions sau thấy state mới nhất.
                    context = ApplySetValueToContext(actionDeltas, context);
                }
            }
        }

        sw.Stop();

        if (deltas.Count > 0)
        {
            _logger.LogInformation(
                "EventEngine xử lý {TriggerCode} trên FormId={FormId}, " +
                "tạo {DeltaCount} deltas trong {ElapsedMs}ms",
                triggerCode, formEvent.FormId, deltas.Count, sw.ElapsedMilliseconds);
        }

        return new UiDeltaResponse(deltas);
    }

    // ── Condition evaluation (event-level) ──────────────────────────

    /// <summary>
    /// Evaluate Condition_Expr của event — NULL = luôn true, exception = false (an toàn).
    /// </summary>
    private bool EvaluateCondition(EventDefinition eventDef, EvaluationContext context)
    {
        if (string.IsNullOrWhiteSpace(eventDef.ConditionExpr))
            return true;

        try
        {
            var result = _astEngine.Evaluate(eventDef.ConditionExpr, context);
            return BuiltinFunctions.ToBool(result) ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Condition eval fail cho EventId={EventId}, skip event",
                eventDef.EventId);
            return false;
        }
    }

    // ── Action dispatch (Strategy + registry) ───────────────────────

    /// <summary>
    /// Dispatch action theo Action_Code → <see cref="IEventActionHandler"/> tương ứng.
    /// Code không nhận diện → log + rỗng; handler ném → nuốt + log (1 action lỗi không vỡ cả event).
    /// </summary>
    private async Task<IReadOnlyList<UiDelta>> ExecuteActionAsync(
        EventAction action,
        EvaluationContext context,
        FormEvent formEvent,
        CancellationToken ct)
    {
        if (!_handlers.TryGetValue(action.ActionCode, out var handler))
            return HandleUnknownAction(action);

        try
        {
            return await handler.ExecuteAsync(new EventActionContext(action, context, formEvent), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Action execute fail: ActionId={ActionId}, Code={ActionCode}",
                action.ActionId, action.ActionCode);
            return [];
        }
    }

    /// <summary>Xử lý action code không nhận diện — log warning, không crash.</summary>
    private IReadOnlyList<UiDelta> HandleUnknownAction(EventAction action)
    {
        _logger.LogWarning(
            "Action code không nhận diện: {ActionCode}, ActionId={ActionId}",
            action.ActionCode, action.ActionId);
        return [];
    }

    // ── Context update ──────────────────────────────────────────────

    /// <summary>
    /// Cập nhật context khi có SET_VALUE delta — actions sau thấy giá trị mới.
    /// Chỉ apply cho SET_VALUE, các loại khác không thay đổi context.
    /// </summary>
    private static EvaluationContext ApplySetValueToContext(
        IReadOnlyList<UiDelta> deltas, EvaluationContext context)
    {
        var updated = context;

        foreach (var delta in deltas)
        {
            if (!delta.Action.Equals("SET_VALUE", StringComparison.OrdinalIgnoreCase))
                continue;

            if (delta.FieldCode is null || delta.Data is null)
                continue;

            if (delta.Data.TryGetValue("value", out var newValue))
            {
                updated = updated.WithValue(delta.FieldCode, newValue);
            }
        }

        return updated;
    }
}
