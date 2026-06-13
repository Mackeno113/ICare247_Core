# Debug: Runtime Form (validate + handle-event — Engine)

> Khi user **tương tác** với form trên Blazor: gõ field → validate, đổi field → chạy event/rule.
> Đây là nơi 3 engine chạy: **Metadata · Validation · Event**. Bối cảnh: [README.md](README.md).

## 1. API (route gốc `api/v1/forms/{formCode}`)

| Method | URL | Mục đích |
|---|---|---|
| POST | `/{formCode}/validate-field` | Validate 1 field (on blur/change) |
| POST | `/{formCode}/validate` | Validate toàn form (trước submit) |
| POST | `/{formCode}/handle-event` | Xử lý event → trả UI delta |

Header bắt buộc: `X-Tenant-Id: 1`. Query `?lang=vi` (mặc định).

## 2. Payload

- **validate-field**:
  ```json
  { "fieldCode": "Email", "value": "a@b.com",
    "contextSnapshot": { "Email": "a@b.com", "Tuoi": 30 } }
  ```
  Response: `{ "isValid": true, "errors": [ { "ruleId", "severity", "message" } ] }`
- **validate**: `{ "contextSnapshot": { ...toàn bộ field... } }` →
  `{ "isValid": false, "fields": { "Email": [ {ruleId,severity,message} ] } }`
- **handle-event**:
  ```json
  { "eventType": "FIELD_CHANGED", "sourceField": "PhongBan",
    "contextSnapshot": { "PhongBan": 3 } }
  ```
  `eventType` ∈ FIELD_CHANGED | FIELD_BLUR | FORM_LOAD | FORM_SUBMIT | SECTION_TOGGLE.
  Response: `{ "delta": [ ...UI delta... ] }` (set visible/enabled/value…).

## 3. Code ở lớp nào

| Lớp | File |
|---|---|
| Api | `Controllers/RuntimeController.cs` (inject thẳng 3 engine + `ITenantContext`, **không** qua MediatR) |
| Application (engine) | `Engines/ValidationEngine.cs`, `Engines/EventEngine.cs`, `Engines/MetadataEngine.cs`, `Engines/ConfigCache.cs` |
| Application (AST) | `Domain/Engine/*` (AstParser/AstCompiler/AstEngine) — đánh giá biểu thức rule |
| Infrastructure | `Repositories/RuleRepository.cs`, `EventRepository.cs` (Config DB); lookup/validation chạm Data DB qua `DynamicLookupRepository` |

> Lưu ý: RuntimeController **không** dùng MediatR — gọi engine trực tiếp. Đây là ngoại lệ so với
> các module CRUD (Forms/MasterData/Views dùng Command/Query).

## 4. Luồng (validate-field)

```
RuntimeController.ValidateField
  ├─ MetadataEngine.GetFormMetadataAsync(formCode, lang, "web", tenant)   ← cache L1+L2
  │     • null → 404 form-not-found
  ├─ BuildContext(contextSnapshot) → EvaluationContext
  └─ ValidationEngine.ValidateFieldAsync(formId, fieldCode, value, context, resourceMap…)
       ├─ nạp rule của field (RuleRepository, cache)
       ├─ AstEngine đánh giá biểu thức (Grammar V1)
       └─ resolve message i18n (ResourceMap / ConfigCache)
  → 200 { isValid, errors[] }
```

`handle-event`: tương tự nhưng gọi `EventEngine.HandleEventAsync(FormEvent)` → trả `Delta` (danh sách
thay đổi UI). `FormEvent` mang theo `FormCode`/`LangCode` để engine resolve message (vd TRIGGER_VALIDATION).

## 5. Breakpoint
1. `RuntimeController.ValidateField/HandleEvent` — `formCode`, `body`, `_tenant.TenantId`.
2. `MetadataEngine.GetFormMetadataAsync` — form null? cache hit?
3. `ValidationEngine.ValidateFieldAsync` / `EventEngine.HandleEventAsync` — rule nào chạy, kết quả AST.
4. `AstEngine` (nếu nghi sai logic biểu thức) — xem `.claude-rules/ast-grammar.md`.

## 6. Lỗi thường gặp
- **404** → `formCode` sai/không active, hoặc sai tenant.
- **Rule không chạy / sai kết quả** → biểu thức Grammar V1; bật log, kiểm AST. Đọc
  `docs/spec/03_GRAMMAR_V1_SPEC.md` + `04_ENGINE_SPEC.md`.
- **Message hiện ra key thay vì text** → thiếu `Sys_Resource` cho key đó hoặc sai `lang`.
- **Delta không áp dụng ở UI** → kiểm `eventType`/`sourceField` client gửi đúng tên Field_Code.
