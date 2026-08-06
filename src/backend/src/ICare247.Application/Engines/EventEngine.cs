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
    private readonly IConfigCache _config;
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
        IConfigCache config,
        ILogger<EventEngine> logger)
    {
        _eventRepo = eventRepo;
        _astEngine = astEngine;
        _validationEngine = validationEngine;
        _config = config;
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
                "SET_ENABLED" => ExecuteSetEnabled(action, context),
                "CLEAR_VALUE" => ExecuteClearValue(action),
                "SHOW_MESSAGE" => ExecuteShowMessage(action, context),
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

        var targetField = EventActionParam.GetString(param, "targetField");
        if (targetField is null) return [];

        // Evaluate AST expression để lấy value mới
        var valueExprElement = EventActionParam.GetElement(param, "valueExpression");
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

    // ── SET_ENABLED ─────────────────────────────────────────────────

    /// <summary>
    /// SET_ENABLED: evaluate conditionExpression → tạo delta bật/tắt enabled cho targetField.
    /// false = grayout, không tương tác, không submit giá trị (ADR-012).
    /// Param JSON: { "targetField": "BankAccount", "conditionExpression": {...} }
    /// </summary>
    private IReadOnlyList<UiDelta> ExecuteSetEnabled(
        EventAction action, EvaluationContext context)
    {
        var (targetField, conditionResult) = EvaluateConditionAction(action, context);
        if (targetField is null) return [];

        return
        [
            new UiDelta(targetField, "SET_ENABLED",
                new Dictionary<string, object?> { ["enabled"] = conditionResult })
        ];
    }

    // ── CLEAR_VALUE ─────────────────────────────────────────────────

    /// <summary>
    /// CLEAR_VALUE: đặt giá trị field về null — không cần condition.
    /// Param JSON: { "targetField": "District" }
    /// </summary>
    private IReadOnlyList<UiDelta> ExecuteClearValue(EventAction action)
    {
        var param = ParseParam(action);
        if (param is null) return [];

        var targetField = EventActionParam.GetString(param, "targetField");
        if (targetField is null) return [];

        return
        [
            new UiDelta(targetField, "CLEAR_VALUE",
                new Dictionary<string, object?> { ["value"] = null })
        ];
    }

    // ── SHOW_MESSAGE ────────────────────────────────────────────────

    /// <summary>
    /// SHOW_MESSAGE: hiển thị thông báo inline tại field — không block submit.
    /// conditionExpression là tùy chọn; nếu có thì evaluate trước, false → không show.
    /// Param JSON: { "targetField": "Age", "messageKey": "msg.age.under_18", "severity": "Warning", "conditionExpression": {...} }
    /// </summary>
    private IReadOnlyList<UiDelta> ExecuteShowMessage(
        EventAction action, EvaluationContext context)
    {
        var param = ParseParam(action);
        if (param is null) return [];

        var targetField = EventActionParam.GetString(param, "targetField");
        var messageKey  = EventActionParam.GetString(param, "messageKey");
        var severity    = EventActionParam.GetString(param, "severity") ?? "Info";

        if (targetField is null || messageKey is null) return [];

        // conditionExpression là optional — nếu có thì evaluate; false → không tạo delta
        var condExprElement = EventActionParam.GetElement(param, "conditionExpression");
        if (condExprElement is not null)
        {
            var condExprJson = condExprElement.Value.GetRawText();
            var result = _astEngine.Evaluate(condExprJson, context);
            if (!(BuiltinFunctions.ToBool(result) ?? false))
                return [];
        }

        return
        [
            new UiDelta(targetField, "SHOW_MESSAGE",
                new Dictionary<string, object?>
                {
                    ["messageKey"] = messageKey,
                    ["severity"]   = severity
                })
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

        var targetField = EventActionParam.GetString(param, "targetField");
        var apiEndpoint = EventActionParam.GetString(param, "apiEndpoint");
        if (targetField is null || apiEndpoint is null) return [];

        // Resolve placeholders trong apiEndpoint: {Province} → giá trị thực
        var resolvedEndpoint = EventActionParam.ResolvePlaceholders(apiEndpoint, context);

        // Lấy dependsOn nếu có
        var dependsOn = EventActionParam.GetStringArray(param, "dependsOn");

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

        var targetFields = EventActionParam.GetStringArray(param, "targetFields");
        if (targetFields is null || targetFields.Count == 0) return [];

        var deltas = new List<UiDelta>();

        // Resource map qua facade (ADR-014) → message validation resolve i18n thay vì raw Error_Key.
        var resourceMap = string.IsNullOrEmpty(formEvent.FormCode)
            ? null
            : await _config.GetResourceMapAsync(formEvent.FormCode, formEvent.LangCode, formEvent.TenantId, ct);

        foreach (var fieldCode in targetFields)
        {
            var value = context.GetValue(fieldCode);
            var response = await _validationEngine.ValidateFieldAsync(
                formEvent.FormId, fieldCode, value, context,
                formEvent.TenantId, langCode: formEvent.LangCode,
                resourceMap: resourceMap, formCode: formEvent.FormCode, ct: ct);

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

        var parsed = EventActionParam.Parse(action.ActionParamJson);
        if (parsed is null)
            _logger.LogWarning("Action_Param_Json không hợp lệ: ActionId={ActionId}", action.ActionId);
        return parsed;
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

        var targetField = EventActionParam.GetString(param, "targetField");
        if (targetField is null) return (null, false);

        var condExprElement = EventActionParam.GetElement(param, "conditionExpression");
        if (condExprElement is null) return (null, false);

        var condExprJson = condExprElement.Value.GetRawText();
        var result = _astEngine.Evaluate(condExprJson, context);
        var boolResult = BuiltinFunctions.ToBool(result) ?? false;

        return (targetField, boolResult);
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
