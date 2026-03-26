# 05 — Action / Rule Param Schema

> **Cập nhật:** Đồng bộ ADR-010/011/012 — 2026-03-26.

---

## Rule Schema (`Val_Rule`)

> **Migration 003:** Bảng junction `Val_Rule_Field` đã bị loại bỏ.
> `Val_Rule` có quan hệ **1 field → nhiều rules** (1-N) qua `Field_Id` trực tiếp.

### Cấu trúc Val_Rule (dữ liệu trong DB)

```json
{
  "Rule_Id": 101,
  "Field_Id": 12,
  "Rule_Type_Code": "Custom",
  "Error_Key": "ui_field.val.date_of_birth.not_future",
  "Expression_Json": {
    "type": "binary",
    "op": "<=",
    "left": { "type": "identifier", "name": "DateOfBirth" },
    "right": { "type": "function_call", "name": "today", "args": [] }
  },
  "Condition_Expr": null,
  "Order_No": 1,
  "Is_Active": true
}
```

### Trường quan trọng — Val_Rule

| Column | Type | Mô tả |
|---|---|---|
| `Field_Id` | int | FK → `Ui_Field` — field sở hữu rule này (1-N) |
| `Rule_Type_Code` | string | FK → `Val_Rule_Type`: `'Regex'`, `'Range'`, `'Length'`, `'Compare'`, `'Custom'` |
| `Error_Key` | string | Resource key → tra `Sys_Resource` để lấy text lỗi theo ngôn ngữ |
| `Expression_Json` | object | AST node JSON — **bắt buộc** với mọi rule type (xem `03_GRAMMAR_V1_SPEC.md`) |
| `Condition_Expr` | object \| null | AST điều kiện áp dụng rule. `null` = luôn áp dụng |
| `Order_No` | int | Thứ tự evaluate khi không có dependency graph |

### Val_Rule_Type (lookup)

| Rule_Type_Code | Mô tả | Expression_Json |
|---|---|---|
| ~~`Required`~~ | *(Deprecated — ADR-011: dùng `Is_Required` cột DB trên Ui_Field)* | — |
| `Regex` | Kiểm tra regex pattern | Literal AST chứa pattern string |
| `Range` | Kiểm tra min/max số hoặc ngày | Binary AST: `value >= min && value <= max` |
| `Length` | Kiểm tra độ dài chuỗi | Binary AST: `len(value) >= min && len(value) <= max` |
| `Compare` | So sánh cross-field (hai field với nhau) | Binary AST: `FieldA op FieldB` |
| `Custom` | Rule tùy chỉnh bằng AST trả về bool | Bất kỳ AST expression nào |

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

### SET_ENABLED
> Bật/tắt enabled của field. `false` = grayout, không tương tác, **không submit** giá trị.

```json
{
  "Action_Code": "SET_ENABLED",
  "Action_Param_Json": {
    "targetField": "BankAccount",
    "conditionExpression": {
      "type": "binary",
      "op": "==",
      "left": { "type": "identifier", "name": "PaymentMethod" },
      "right": { "type": "literal", "value": "bank_transfer" }
    }
  }
}
```

### CLEAR_VALUE
> Xóa giá trị field — đặt về null hoặc empty string. Không cần condition.

```json
{
  "Action_Code": "CLEAR_VALUE",
  "Action_Param_Json": {
    "targetField": "District"
  }
}
```

### SHOW_MESSAGE
> Hiển thị thông báo inline tại field (không block submit). Dùng cho warning/hint runtime.

```json
{
  "Action_Code": "SHOW_MESSAGE",
  "Action_Param_Json": {
    "targetField": "Age",
    "messageKey": "msg.age.under_18_warning",
    "severity": "Warning",
    "conditionExpression": {
      "type": "binary",
      "op": "<",
      "left": { "type": "identifier", "name": "Age" },
      "right": { "type": "literal", "value": 18 }
    }
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
| `SET_ENABLED` | `targetField`, `conditionExpression` | — |
| `CLEAR_VALUE` | `targetField` | — |
| `SHOW_MESSAGE` | `targetField`, `messageKey`, `severity` | `conditionExpression` |
| `RELOAD_OPTIONS` | `targetField`, `apiEndpoint` | `dependsOn` |
| `TRIGGER_VALIDATION` | `targetFields` | — |

---

## Val_Rule_Type — Param Schema

Mỗi `Val_Rule_Type` có `Param_Schema` mô tả cấu trúc `Expression_Json` hợp lệ.

| Rule_Type_Code | Param Schema đặc trưng |
|---|---|
| ~~`Required`~~ | *(Deprecated — ADR-011: dùng `Is_Required` cột DB)* |
| `Regex` | `Expression_Json` chứa literal pattern: `{ "type": "literal", "value": "^[0-9]{10}$" }` |
| `Range` | `Expression_Json` là binary AST: `value >= min && value <= max` |
| `Length` | `Expression_Json` là binary AST: `len(value) >= min && len(value) <= max` |
| `Compare` | `Expression_Json` là binary AST: `FieldA op FieldB` (op: `==`, `!=`, `<`, `<=`, `>`, `>=`) |
| `Custom` | `Expression_Json` là bất kỳ AST expression nào trả về `bool` |

### Rule — Length (ví dụ)
> Kiểm tra độ dài chuỗi của field `PhoneNumber` từ 10 đến 15 ký tự.

```json
{
  "Rule_Id": 201,
  "Field_Id": 15,
  "Rule_Type_Code": "Length",
  "Error_Key": "ui_field.val.phone_number.length",
  "Expression_Json": {
    "type": "binary",
    "op": "&&",
    "left": {
      "type": "binary",
      "op": ">=",
      "left": { "type": "function_call", "name": "len", "args": [{ "type": "identifier", "name": "PhoneNumber" }] },
      "right": { "type": "literal", "value": 10 }
    },
    "right": {
      "type": "binary",
      "op": "<=",
      "left": { "type": "function_call", "name": "len", "args": [{ "type": "identifier", "name": "PhoneNumber" }] },
      "right": { "type": "literal", "value": 15 }
    }
  }
}
```

### Rule — Compare (ví dụ)
> Kiểm tra `EndDate` phải lớn hơn hoặc bằng `StartDate`.

```json
{
  "Rule_Id": 202,
  "Field_Id": 20,
  "Rule_Type_Code": "Compare",
  "Error_Key": "ui_field.val.end_date.before_start",
  "Expression_Json": {
    "type": "binary",
    "op": ">=",
    "left": { "type": "identifier", "name": "EndDate" },
    "right": { "type": "identifier", "name": "StartDate" }
  }
}
```
