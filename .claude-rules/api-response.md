# API Response Format — ICare247

## Thành công

Trả data trực tiếp (KHÔNG wrap envelope):

```json
HTTP 200 OK
{ ...data object... }
```

## Lỗi — RFC 7807 ProblemDetails

```json
HTTP 4xx/5xx
{
  "type": "https://icare247.vn/errors/{error-code}",
  "title": "Mô tả ngắn",
  "status": 400,
  "detail": "Mô tả chi tiết",
  "correlationId": "abc-123"
}
```

## Headers bắt buộc

| Header            | Ví dụ          | Bắt buộc    |
| ----------------- | -------------- | ----------- |
| `Authorization`   | `Bearer {jwt}` | ✅          |
| `X-Tenant-Id`     | `1`            | ✅          |
| `X-Correlation-Id`| `uuid-v4`      | Recommended |

## Exception Policy

- Repository methods: **không catch** SQL exceptions — bubble up
- Handler: catch domain exceptions, convert sang ProblemDetails
- GlobalExceptionMiddleware: catch tất cả unhandled → 500 ProblemDetails + log Error
- **KHÔNG swallow** exception (không `catch {}` rỗng)
