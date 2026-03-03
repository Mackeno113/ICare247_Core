# 05 — Action / Rule Param Schema

## Rule Schema (Sys_Rule)

```json
{
  "ruleId": 101,
  "fieldCode": "DateOfBirth",
  "ruleType": "validation",
  "severity": "error",
  "expressionJson": {
    "type": "binary",
    "op": "<=",
    "left": { "type": "identifier", "name": "DateOfBirth" },
    "right": { "type": "function_call", "name": "today", "args": [] }
  },
  "errorMessage": "Ngày sinh không được là ngày trong tương lai.",
  "sortOrder": 1
}
```

### Trường quan trọng

| Field           | Type   | Mô tả                                      |
| --------------- | ------ | ------------------------------------------ |
| `ruleType`      | string | `validation` / `visibility` / `required`   |
| `severity`      | string | `error` / `warning` / `info`               |
| `expressionJson`| object | AST node JSON (xem 03_GRAMMAR_V1_SPEC.md)  |
| `sortOrder`     | int    | Thứ tự evaluate khi không có dependency    |

## Action Schema

### SET_VALUE
```json
{
  "actionType": "SET_VALUE",
  "params": {
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
```json
{
  "actionType": "SET_VISIBLE",
  "params": {
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
```json
{
  "actionType": "SET_REQUIRED",
  "params": {
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

### RELOAD_OPTIONS
```json
{
  "actionType": "RELOAD_OPTIONS",
  "params": {
    "targetField": "District",
    "dependsOn": ["Province"],
    "apiEndpoint": "/api/options/districts?provinceId={Province}"
  }
}
```

## Event Schema

```json
{
  "eventType": "FIELD_CHANGED",
  "sourceField": "Province",
  "formId": 42,
  "tenantId": 1,
  "contextSnapshot": {
    "Province": "HN",
    "District": null,
    "CustomerType": "company"
  }
}
```

### Event Types

| Event           | Trigger                          |
| --------------- | -------------------------------- |
| `FIELD_CHANGED` | User thay đổi giá trị field      |
| `FIELD_BLUR`    | User rời khỏi field              |
| `FORM_LOAD`     | Form vừa load xong               |
| `FORM_SUBMIT`   | User submit form                 |
| `SECTION_TOGGLE`| User mở/đóng section             |
