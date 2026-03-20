// File    : EventEngine.cs
// Module  : Engines
// Layer   : Application
// Purpose : Concrete implementation của IEventEngine — xử lý form events và tạo UI deltas.

using System.Diagnostics;
using System.Text.Json;
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
/// execute actions tuần tự → build UiDelta list → trả UiDeltaResponse.
/// </summary>
public sealed class EventEngine : IEventEngine
{
    private readonly IEventRepository _eventRepo;
    private readonly IAstEngine _astEngine;
    private readonly IValidationEngine _validationEngine;
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
        IValidationEngine validationEngine,
        ILogger<EventEngine> logger)
    {
        _eventRepo = eventRepo;
        _astEngine = astEngine;
        _validationEngine = validationEngine;
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

                var actionDeltas = await ExecuteActionAsync(
                    action, context, formEvent, ct);

                if (actionDeltas.Count > 0)
                {
                    deltas.AddRange(actionDeltas);

                    // Cập nhật context với giá trị mới từ SET_VALUE
                    // để actions sau thấy state mới nhất
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

    // ── Condition evaluation ────────────────────────────────────────

    /// <summary>
    /// Evaluate Condition_Expr — NULL = luôn true, exception = false (an toàn).
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

    // ── Action execution ────────────────────────────────────────────

    /// <summary>
    /// Dispatch action theo Action_Code → handler tương ứng.
    /// Trả về list UiDelta (có thể rỗng nếu action không tạo delta).
    /// </summary>
    private async Task<IReadOnlyList<UiDelta>> ExecuteActionAsync(
        EventAction action,
        EvaluationContext context,
        FormEvent formEvent,
        CancellationToken ct)
    {
        try
        {
            return action.ActionCode.ToUpperInvariant() switch
            {
                "SET_VALUE" => ExecuteSetValue(action, context),
                "SET_VISIBLE" => ExecuteSetVisible(action, context),
                "SET_REQUIRED" => ExecuteSetRequired(action, context),
                "SET_READONLY" => ExecuteSetReadOnly(action, context),
                "RELOAD_OPTIONS" => ExecuteReloadOptions(action, context),
                "TRIGGER_VALIDATION" => await ExecuteTriggerValidationAsync(
                    action, context, formEvent, ct),
                _ => HandleUnknownAction(action)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Action execute fail: ActionId={ActionId}, Code={ActionCode}",
                action.ActionId, action.ActionCode);
            return [];
        }
    }

    // ── SET_VALUE ───────────────────────────────────────────────────

    /// <summary>
    /// SET_VALUE: evaluate valueExpression → tạo delta set giá trị mới cho targetField.
    /// Param JSON: { "targetField": "Total", "valueExpression": {...AST JSON...} }
    /// </summary>
    private IReadOnlyList<UiDelta> ExecuteSetValue(
        EventAction action, EvaluationContext context)
    {
        var param = ParseParam(action);
        if (param is null) return [];

        var targetField = GetString(param, "targetField");
        if (targetField is null) return [];

        // Evaluate AST expression để lấy value mới
        var valueExprElement = GetElement(param, "valueExpression");
        if (valueExprElement is null) return [];

        var valueExprJson = valueExprElement.Value.GetRawText();
        var newValue = _astEngine.Evaluate(valueExprJson, context);

        return
        [
            new UiDelta(targetField, "SET_VALUE",
                new Dictionary<string, object?> { ["value"] = newValue })
        ];
    }

    // ── SET_VISIBLE ─────────────────────────────────────────────────

    /// <summary>
    /// SET_VISIBLE: evaluate conditionExpression → tạo delta show/hide targetField.
    /// Param JSON: { "targetField": "SecondaryPhone", "conditionExpression": {...} }
    /// </summary>
    private IReadOnlyList<UiDelta> ExecuteSetVisible(
        EventAction action, EvaluationContext context)
    {
        var (targetField, conditionResult) = EvaluateConditionAction(action, context);
        if (targetField is null) return [];

        return
        [
            new UiDelta(targetField, "SET_VISIBLE",
                new Dictionary<string, object?> { ["visible"] = conditionResult })
        ];
    }

    // ── SET_REQUIRED ────────────────────────────────────────────────

    /// <summary>
    /// SET_REQUIRED: evaluate conditionExpression → tạo delta toggle required cho targetField.
    /// Param JSON: { "targetField": "TaxCode", "conditionExpression": {...} }
    /// </summary>
    private IReadOnlyList<UiDelta> ExecuteSetRequired(
        EventAction action, EvaluationContext context)
    {
        var (targetField, conditionResult) = EvaluateConditionAction(action, context);
        if (targetField is null) return [];

        return
        [
            new UiDelta(targetField, "SET_REQUIRED",
                new Dictionary<string, object?> { ["required"] = conditionResult })
        ];
    }

    // ── SET_READONLY ────────────────────────────────────────────────

    /// <summary>
    /// SET_READONLY: evaluate conditionExpression → tạo delta toggle readonly cho targetField.
    /// Param JSON: { "targetField": "OrderCode", "conditionExpression": {...} }
    /// </summary>
    private IReadOnlyList<UiDelta> ExecuteSetReadOnly(
        EventAction action, EvaluationContext context)
    {
        var (targetField, conditionResult) = EvaluateConditionAction(action, context);
        if (targetField is null) return [];

        return
        [
            new UiDelta(targetField, "SET_READONLY",
                new Dictionary<string, object?> { ["readOnly"] = conditionResult })
        ];
    }

    // ── RELOAD_OPTIONS ──────────────────────────────────────────────

    /// <summary>
    /// RELOAD_OPTIONS: tạo delta yêu cầu client reload dropdown options.
    /// Server chỉ gửi delta — client gọi API endpoint để fetch options mới.
    /// Param JSON: { "targetField": "District", "apiEndpoint": "/api/options/districts?provinceId={Province}", "dependsOn": ["Province"] }
    /// </summary>
    private IReadOnlyList<UiDelta> ExecuteReloadOptions(
        EventAction action, EvaluationContext context)
    {
        var param = ParseParam(action);
        if (param is null) return [];

        var targetField = GetString(param, "targetField");
        var apiEndpoint = GetString(param, "apiEndpoint");
        if (targetField is null || apiEndpoint is null) return [];

        // Resolve placeholders trong apiEndpoint: {Province} → giá trị thực
        var resolvedEndpoint = ResolvePlaceholders(apiEndpoint, context);

        // Lấy dependsOn nếu có
        var dependsOn = GetStringArray(param, "dependsOn");

        var data = new Dictionary<string, object?>
        {
            ["apiEndpoint"] = resolvedEndpoint,
            ["dependsOn"] = dependsOn
        };

        return
        [
            new UiDelta(targetField, "RELOAD_OPTIONS", data)
        ];
    }

    // ── TRIGGER_VALIDATION ──────────────────────────────────────────

    /// <summary>
    /// TRIGGER_VALIDATION: gọi ValidationEngine cho danh sách fields.
    /// Param JSON: { "targetFields": ["DateOfBirth", "Age"] }
    /// </summary>
    private async Task<IReadOnlyList<UiDelta>> ExecuteTriggerValidationAsync(
        EventAction action,
        EvaluationContext context,
        FormEvent formEvent,
        CancellationToken ct)
    {
        var param = ParseParam(action);
        if (param is null) return [];

        var targetFields = GetStringArray(param, "targetFields");
        if (targetFields is null || targetFields.Count == 0) return [];

        var deltas = new List<UiDelta>();

        foreach (var fieldCode in targetFields)
        {
            var value = context.GetValue(fieldCode);
            var response = await _validationEngine.ValidateFieldAsync(
                formEvent.FormId, fieldCode, value, context,
                formEvent.TenantId, ct);

            // Tạo delta chứa validation errors
            var errors = response.Results
                .Select(r => new Dictionary<string, object?>
                {
                    ["ruleId"] = r.RuleId,
                    ["severity"] = r.Severity,
                    ["message"] = r.Message
                })
                .ToList();

            deltas.Add(new UiDelta(fieldCode, "TRIGGER_VALIDATION",
                new Dictionary<string, object?>
                {
                    ["isValid"] = response.IsValid,
                    ["errors"] = errors
                }));
        }

        return deltas;
    }

    // ── Unknown action ──────────────────────────────────────────────

    /// <summary>
    /// Xử lý action code không nhận diện — log warning, không crash.
    /// </summary>
    private IReadOnlyList<UiDelta> HandleUnknownAction(EventAction action)
    {
        _logger.LogWarning(
            "Action code không nhận diện: {ActionCode}, ActionId={ActionId}",
            action.ActionCode, action.ActionId);
        return [];
    }

    // ── Helper methods ──────────────────────────────────────────────

    /// <summary>
    /// Parse Action_Param_Json thành JsonElement.
    /// Trả null nếu JSON rỗng hoặc không hợp lệ.
    /// </summary>
    private JsonElement? ParseParam(EventAction action)
    {
        if (string.IsNullOrWhiteSpace(action.ActionParamJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(action.ActionParamJson);
            // Clone để dùng sau khi dispose — tránh ObjectDisposedException
            return doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Action_Param_Json không hợp lệ: ActionId={ActionId}",
                action.ActionId);
            return null;
        }
    }

    /// <summary>
    /// Evaluate pattern chung cho SET_VISIBLE, SET_REQUIRED, SET_READONLY:
    /// parse param → get targetField + conditionExpression → evaluate.
    /// </summary>
    private (string? targetField, bool conditionResult) EvaluateConditionAction(
        EventAction action, EvaluationContext context)
    {
        var param = ParseParam(action);
        if (param is null) return (null, false);

        var targetField = GetString(param, "targetField");
        if (targetField is null) return (null, false);

        var condExprElement = GetElement(param, "conditionExpression");
        if (condExprElement is null) return (null, false);

        var condExprJson = condExprElement.Value.GetRawText();
        var result = _astEngine.Evaluate(condExprJson, context);
        var boolResult = BuiltinFunctions.ToBool(result) ?? false;

        return (targetField, boolResult);
    }

    /// <summary>
    /// Lấy string property từ JsonElement. Trả null nếu không tồn tại.
    /// </summary>
    private static string? GetString(JsonElement? element, string propertyName)
    {
        if (element is null) return null;
        return element.Value.TryGetProperty(propertyName, out var prop)
            ? prop.GetString()
            : null;
    }

    /// <summary>
    /// Lấy nested JsonElement property. Trả null nếu không tồn tại.
    /// </summary>
    private static JsonElement? GetElement(JsonElement? element, string propertyName)
    {
        if (element is null) return null;
        return element.Value.TryGetProperty(propertyName, out var prop)
            ? prop
            : null;
    }

    /// <summary>
    /// Lấy string array từ JsonElement. Trả null nếu không tồn tại hoặc không phải array.
    /// </summary>
    private static IReadOnlyList<string>? GetStringArray(JsonElement? element, string propertyName)
    {
        if (element is null) return null;
        if (!element.Value.TryGetProperty(propertyName, out var prop)) return null;
        if (prop.ValueKind != JsonValueKind.Array) return null;

        return prop.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString()!)
            .ToList();
    }

    /// <summary>
    /// Resolve placeholders {FieldCode} trong string bằng giá trị từ context.
    /// Ví dụ: "/api/options?province={Province}" → "/api/options?province=HN"
    /// </summary>
    private static string ResolvePlaceholders(string template, EvaluationContext context)
    {
        // Pattern đơn giản: tìm {word} → replace bằng context value
        var result = template;
        var startIdx = 0;

        while (startIdx < result.Length)
        {
            var openBrace = result.IndexOf('{', startIdx);
            if (openBrace < 0) break;

            var closeBrace = result.IndexOf('}', openBrace + 1);
            if (closeBrace < 0) break;

            var fieldCode = result[(openBrace + 1)..closeBrace];
            var value = context.GetValue(fieldCode);
            var valueStr = value?.ToString() ?? string.Empty;

            result = string.Concat(result.AsSpan(0, openBrace), valueStr, result.AsSpan(closeBrace + 1));
            startIdx = openBrace + valueStr.Length;
        }

        return result;
    }

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
