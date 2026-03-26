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
    → Load Field_Id từ fieldCode + formId
    → Load rules của field từ Val_Rule WHERE Field_Id = Field_Id ORDER BY Order_No
          (Migration 003: bỏ bảng junction Val_Rule_Field — Field_Id trực tiếp trên Val_Rule)
    → Resolve dependencies qua Sys_Dependency
    → Sort rules theo dependency graph (topological sort)
    → Evaluate từng rule:
          - Nếu Val_Rule.Condition_Expr != NULL → evaluate điều kiện trước
          - Nếu điều kiện = false → skip rule
          - Rule_Type_Code = 'Regex'  → AstEngine.Evaluate(Expression_Json) → regex match
          - Rule_Type_Code = 'Range'  → AstEngine.Evaluate → value >= min && value <= max
          - Rule_Type_Code = 'Length' → AstEngine.Evaluate → len(value) >= min && len(value) <= max
          - Rule_Type_Code = 'Compare'→ AstEngine.Evaluate → cross-field comparison (op: ==,!=,<,<=,>,>=)
          - Rule_Type_Code = 'Custom' → AstEngine.Evaluate(Expression_Json) → bool
    → Resolve error text: Val_Rule.Error_Key → Sys_Resource (đa ngôn ngữ)
    → Collect ValidationResult list
    → Return ValidationResponse
```

> **Quan hệ DB (sau Migration 003):** Quan hệ **1 field → nhiều rules** (1-N). `Val_Rule.Field_Id` là FK trực tiếp.
> Bảng junction `Val_Rule_Field` đã bị loại bỏ — một rule không chia sẻ giữa nhiều fields.
> `Val_Rule.Order_No` quyết định thứ tự evaluate khi không có dependency graph.
>
> **ADR-011:** Rule_Type `Required` **deprecated** — `Is_Required` là cột DB trên `Ui_Field`.
> Rule type `Length` (kiểm tra độ dài chuỗi) và `Compare` (so sánh cross-field) được thêm mới.

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
- `SET_VALUE`: Gán giá trị field (valueExpression là AST)
- `SET_VISIBLE`: Ẩn/hiện field/section
- `SET_REQUIRED`: Bật/tắt required động (runtime override Is_Required)
- `SET_READONLY`: Bật/tắt readonly động (runtime override Is_ReadOnly)
- `SET_ENABLED`: Bật/tắt enabled — grayout field, không tương tác, không submit *(ADR-012)*
- `CLEAR_VALUE`: Xóa giá trị field (đặt về null/empty) *(ADR-012)*
- `SHOW_MESSAGE`: Hiển thị thông báo inline tại field *(ADR-012)*
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
