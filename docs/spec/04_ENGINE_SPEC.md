# 04 — Engine Specification

## 1. MetadataEngine

**Nhiệm vụ:** Load form/field/section metadata, cache 2 lớp.

```
GetFormMetadataAsync(formCode, langCode, platform, tenantId)
    → Check L1 MemoryCache (key: CacheKeys.Form(...))
    → Check L2 Redis
    → Load từ DB (IFormRepository, IFieldRepository, ISectionRepository)
    → Build FormMetadata object
    → Set L2 Redis (TTL 30 phút)
    → Set L1 Memory (TTL 5 phút)
    → Return FormMetadata
```

**Interface:**
```csharp
public interface IMetadataEngine
{
    Task<FormMetadata?> GetFormMetadataAsync(
        string formCode, string langCode, string platform,
        int tenantId, CancellationToken ct = default);

    Task InvalidateFormCacheAsync(string formCode, int tenantId);
}
```

## 2. AstEngine

**Nhiệm vụ:** Parse Expression_Json → compile → execute.

```
Parse:   string (Expression_Json) → IExpressionNode
Compile: IExpressionNode → Func<EvaluationContext, object?>
Execute: Func<EvaluationContext, object?>(context) → object?
```

**Interface:**
```csharp
public interface IAstEngine
{
    IExpressionNode Parse(string expressionJson);
    Func<EvaluationContext, object?> Compile(IExpressionNode node);
    object? Evaluate(string expressionJson, EvaluationContext context);
}
```

**Cache compile:** Compiled delegates được cache theo `hash(expressionJson)`.

## 3. ValidationEngine

**Nhiệm vụ:** Evaluate danh sách rule theo dependency order.

```
ValidateFieldAsync(formId, fieldCode, value, context, tenantId)
    → Load rules của field từ Sys_Rule
    → Resolve dependencies qua Sys_Dependency
    → Sort rules theo dependency graph (topological sort)
    → Evaluate từng rule (AstEngine.Evaluate)
    → Collect ValidationResult list
    → Return ValidationResponse
```

**Interface:**
```csharp
public interface IValidationEngine
{
    Task<ValidationResponse> ValidateFieldAsync(
        int formId, string fieldCode, object? value,
        EvaluationContext context, int tenantId,
        CancellationToken ct = default);

    Task<ValidationResponse> ValidateFormAsync(
        int formId, EvaluationContext context, int tenantId,
        CancellationToken ct = default);
}
```

## 4. EventEngine

**Nhiệm vụ:** Xử lý event → trigger actions → build UI delta.

```
HandleEventAsync(event)
    → Lookup event handlers từ metadata
    → Evaluate điều kiện trigger (AstEngine)
    → Execute actions (ActionExecutor)
    → Build UiDelta (field visibility, value, validation state)
    → Return UiDeltaResponse
```

**Action Types:**
- `SET_VALUE`: Gán giá trị field
- `SET_VISIBLE`: Ẩn/hiện field/section
- `SET_REQUIRED`: Bật/tắt required
- `SET_READONLY`: Bật/tắt readonly
- `RELOAD_OPTIONS`: Reload dropdown options
- `TRIGGER_VALIDATION`: Kích hoạt validate field

**Interface:**
```csharp
public interface IEventEngine
{
    Task<UiDeltaResponse> HandleEventAsync(
        FormEvent formEvent, EvaluationContext context,
        int tenantId, CancellationToken ct = default);
}
```
