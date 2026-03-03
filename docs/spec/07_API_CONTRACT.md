# 07 — API Contract

## Base URL
`https://{host}/api/v1`

## Headers bắt buộc
| Header           | Ví dụ         | Bắt buộc |
| ---------------- | ------------- | -------- |
| `Authorization`  | `Bearer {jwt}`| ✅       |
| `X-Tenant-Id`    | `1`           | ✅       |
| `X-Correlation-Id`| `uuid-v4`   | Recommended |

## Response Format

### Thành công
```
HTTP 200 OK
{...data object trực tiếp, không wrap envelope...}
```

### Lỗi (RFC 7807)
```json
HTTP 4xx / 5xx
{
  "type": "https://icare247.vn/errors/{error-code}",
  "title": "Mô tả ngắn tiếng Việt",
  "status": 400,
  "detail": "Mô tả chi tiết",
  "correlationId": "abc-123-def"
}
```

---

## Endpoints

### GET /api/v1/forms/{formCode}/metadata
Load metadata đầy đủ của form.

**Query params:**
- `langCode` (optional, default: `vi`)
- `platform` (optional, default: `web`)

**Response 200:**
```json
{
  "formId": 42,
  "formCode": "PATIENT_INTAKE",
  "version": 3,
  "sections": [
    {
      "sectionCode": "PERSONAL_INFO",
      "sortOrder": 1,
      "fields": [
        {
          "fieldCode": "FullName",
          "fieldType": "text",
          "isRequired": true,
          "defaultValue": null
        }
      ]
    }
  ]
}
```

**Response 404:**
```json
{
  "type": "https://icare247.vn/errors/form-not-found",
  "title": "Form không tồn tại",
  "status": 404,
  "detail": "Form với code 'PATIENT_INTAKE' không tồn tại hoặc không active.",
  "correlationId": "..."
}
```

---

### POST /api/v1/forms/{formCode}/validate-field
Validate một field.

**Request body:**
```json
{
  "fieldCode": "DateOfBirth",
  "value": "2030-01-01",
  "context": {
    "CustomerType": "individual",
    "Province": "HN"
  }
}
```

**Response 200:**
```json
{
  "fieldCode": "DateOfBirth",
  "isValid": false,
  "errors": [
    {
      "ruleId": 101,
      "severity": "error",
      "message": "Ngày sinh không được là ngày trong tương lai."
    }
  ]
}
```

---

### POST /api/v1/forms/{formCode}/handle-event
Xử lý event và trả về UI delta.

**Request body:**
```json
{
  "eventType": "FIELD_CHANGED",
  "sourceField": "Province",
  "context": {
    "Province": "HCM",
    "District": null
  }
}
```

**Response 200:**
```json
{
  "delta": [
    {
      "fieldCode": "District",
      "action": "RELOAD_OPTIONS",
      "data": { "dependsOn": "Province" }
    },
    {
      "fieldCode": "Ward",
      "action": "SET_VALUE",
      "data": { "value": null }
    }
  ]
}
```
