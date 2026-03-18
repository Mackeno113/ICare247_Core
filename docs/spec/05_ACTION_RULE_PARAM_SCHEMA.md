# 05 — Action / Rule Param Schema

> **Cập nhật:** Đồng bộ với schema thực tế từ `02_DATABASE_SCHEMA.md` — 2026-03-18.

---

## Rule Schema (`Val_Rule` + `Val_Rule_Field`)

Validation rule được lưu ở 2 bảng:
- **`Val_Rule`** — định nghĩa rule (expression, error key, điều kiện áp dụng)
- **`Val_Rule_Field`** — liên kết rule với field, kèm thứ tự evaluate

### Cấu trúc Val_Rule (dữ liệu trong DB)

```json
{
  "Rule_Id": 101,
  "Rule_Type_Code": "Custom",
  "Error_Key": "validation.date_of_birth.not_future",
  "Expression_Json": {
    "type": "binary",
    "op": "<=",
    "left": { "type": "identifier", "name": "DateOfBirth" },
    "right": { "type": "function_call", "name": "today", "args": [] }
  },
  "Condition_Expr": null,
  "Is_Active": true
}
```

### Cấu trúc Val_Rule_Field (liên kết field ↔ rule)

```json
{
  "Rule_Field_Id": 55,
  "Field_Id": 12,
  "Rule_Id": 101,
  "Order_No": 1
}
```

### Trường quan trọng — Val_Rule

| Column | Type | Mô tả |
|---|---|---|
| `Rule_Type_Code` | string | FK → `Val_Rule_Type`: `'Required'`, `'Regex'`, `'Range'`, `'Custom'` |
| `Error_Key` | string | Resource key → tra `Sys_Resource` để lấy text lỗi theo ngôn ngữ |
| `Expression_Json` | object | AST node JSON — xem `03_GRAMMAR_V1_SPEC.md`. Bắt buộc trừ khi `Rule_Type_Code = 'Required'` |
| `Condition_Expr` | object \| null | AST điều kiện áp dụng rule. `null` = luôn áp dụng |

### Trường quan trọng — Val_Rule_Field

| Column | Type | Mô tả |
|---|---|---|
| `Field_Id` | int | FK → `Ui_Field` |
| `Rule_Id` | int | FK → `Val_Rule` |
| `Order_No` | int | Thứ tự evaluate khi không có dependency graph |

### Val_Rule_Type (lookup)

| Rule_Type_Code | Mô tả | Expression_Json bắt buộc? |
|---|---|---|
| `Required` | Kiểm tra not null / not empty | ❌ Không |
| `Regex` | Kiểm tra regex pattern | ✅ Có |
| `Range` | Kiểm tra min/max | ✅ Có |
| `Custom` | Rule tùy chỉnh bằng AST | ✅ Có |

---

## Action Schema

### SET_VALUE
> Gán giá trị cho field. `valueExpression` là AST node được evaluate tại runtime.

```json
{
  "Action_Code": "SET_VALUE",
  "Action_Param_Json": {
    "targetField": "TotalAmount",
    "valueExpression": {
      "type": "binary",
      "op": "*",
      "left": { "type": "identifier", "name": "Quantity" },
      "right": { "type": "identifier", "name": "UnitPrice" }
    }
  }
}
```

### SET_VISIBLE
> Ẩn/hiện field hoặc section dựa trên điều kiện.

```json
{
  "Action_Code": "SET_VISIBLE",
  "Action_Param_Json": {
    "targetField": "SecondaryPhone",
    "conditionExpression": {
      "type": "binary",
      "op": "==",
      "left": { "type": "identifier", "name": "HasSecondaryContact" },
      "right": { "type": "literal", "value": true }
    }
  }
}
```

### SET_REQUIRED
> Bật/tắt required động dựa trên điều kiện. Không cần tạo `Val_Rule` mới.

```json
{
  "Action_Code": "SET_REQUIRED",
  "Action_Param_Json": {
    "targetField": "TaxCode",
    "conditionExpression": {
      "type": "binary",
      "op": "==",
      "left": { "type": "identifier", "name": "CustomerType" },
      "right": { "type": "literal", "value": "company" }
    }
  }
}
```

### SET_READONLY
> Bật/tắt readonly động.

```json
{
  "Action_Code": "SET_READONLY",
  "Action_Param_Json": {
    "targetField": "OrderCode",
    "conditionExpression": {
      "type": "identifier",
      "name": "IsSubmitted"
    }
  }
}
```

### RELOAD_OPTIONS
> Reload danh sách options của dropdown/combobox dựa trên giá trị field khác.

```json
{
  "Action_Code": "RELOAD_OPTIONS",
  "Action_Param_Json": {
    "targetField": "District",
    "dependsOn": ["Province"],
    "apiEndpoint": "/api/options/districts?provinceId={Province}"
  }
}
```

### TRIGGER_VALIDATION
> Kích hoạt validate một hoặc nhiều field.

```json
{
  "Action_Code": "TRIGGER_VALIDATION",
  "Action_Param_Json": {
    "targetFields": ["DateOfBirth", "Age"]
  }
}
```

---

## Event Schema

> Event được tạo phía client và gửi lên server. `Trigger_Code` phải khớp với giá trị trong bảng `Evt_Trigger_Type`.

```json
{
  "formCode": "patient_registration",
  "formId": 42,
  "tenantId": 1,
  "triggerCode": "OnChange",
  "sourceFieldCode": "Province",
  "contextSnapshot": {
    "Province": "HN",
    "District": null,
    "CustomerType": "company"
  }
}
```

### Trigger Codes (`Evt_Trigger_Type`)

| Trigger_Code | Khi nào fire |
|---|---|
| `OnChange` | User thay đổi giá trị field |
| `OnBlur` | User rời khỏi field (mất focus) |
| `OnLoad` | Form vừa load xong |
| `OnSubmit` | User bấm submit form |
| `OnSectionToggle` | User mở/đóng section |

---

## Evt_Action_Type — Param Schema

Mỗi `Evt_Action_Type` có `Param_Schema` (JSON Schema) mô tả cấu trúc hợp lệ của `Action_Param_Json`.

| Action_Code | Param bắt buộc | Param tùy chọn |
|---|---|---|
| `SET_VALUE` | `targetField`, `valueExpression` | — |
| `SET_VISIBLE` | `targetField`, `conditionExpression` | — |
| `SET_REQUIRED` | `targetField`, `conditionExpression` | — |
| `SET_READONLY` | `targetField`, `conditionExpression` | — |
| `RELOAD_OPTIONS` | `targetField`, `apiEndpoint` | `dependsOn` |
| `TRIGGER_VALIDATION` | `targetFields` | — |

---

## Val_Rule_Type — Param Schema

Mỗi `Val_Rule_Type` có `Param_Schema` mô tả cấu trúc `Expression_Json` hợp lệ.

| Rule_Type_Code | Param Schema đặc trưng |
|---|---|
| `Required` | Không có — engine tự kiểm tra null/empty |
| `Regex` | `Expression_Json` chứa literal pattern: `{ "type": "literal", "value": "^[0-9]{10}$" }` |
| `Range` | `Expression_Json` là binary AST: `value >= min && value <= max` |
| `Custom` | `Expression_Json` là bất kỳ AST expression nào trả về `bool` |
