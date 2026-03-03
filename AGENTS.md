# ICare247 Core Platform — AI Agent Configuration (OpenAI Codex / Responses API)

<!--
  FILE: AGENTS.md
  MỤC ĐÍCH: Cấu hình cho OpenAI Codex (codex CLI) và OpenAI Responses API.
  Codex tự động đọc file AGENTS.md khi khởi động trong thư mục project.
  Các quy tắc dưới đây là BẮT BUỘC — không được bỏ qua dù user yêu cầu.
-->

## Project Identity

**ICare247 Core Platform** — Enterprise metadata-driven low-code form engine.
Hệ thống cho phép tạo và vận hành form nghiệp vụ hoàn toàn từ cấu hình database, không cần code, không deploy lại khi thay đổi logic.

---

## Critical Constraints (KHÔNG ĐƯỢC VI PHẠM)

1. **KHÔNG dùng EF Core** — Data access chỉ bằng Dapper
2. **KHÔNG string interpolation vào SQL** — bắt buộc parameterized query
3. **KHÔNG eval / dynamic compile** — chỉ AST-based execution (Grammar V1)
4. **KHÔNG new Infrastructure class trong Api layer** — chỉ qua DI
5. **KHÔNG hardcode cache key** — lấy từ `CacheKeys.cs`
6. **KHÔNG swallow exception** trong engine — phải bubble lên middleware
7. **KHÔNG .Result hay .Wait()** — async/await xuyên suốt
8. **KHÔNG bỏ qua Tenant_Id** — mọi query/cache key phải có tenant
9. **KHÔNG SELECT * trong Dapper query** — chỉ select cột cần thiết
10. **Comment bằng Tiếng Việt** — không mixed ngôn ngữ trong cùng file

---

## Tech Stack

- **Backend:** .NET 9 / ASP.NET Core 9
- **Frontend:** Blazor WebAssembly + DevExpress Blazor
- **Database:** MS SQL Server
- **Data Access:** Dapper (MANDATORY — NOT EF Core)
- **Cache:** MemoryCache (L1) + Redis (L2)
- **Logging:** Serilog + OpenTelemetry
- **Auth:** JWT + Policy-based Authorization
- **CQRS:** MediatR

---

## Architecture: Clean Architecture (4 Layers)

```
Presentation  → Controllers, Middleware, Swagger
Application   → CQRS Commands/Queries, Use Cases, MediatR Pipeline  
Domain        → Entity, AST, Validation Engine, Event Engine (no deps)
Infrastructure → Dapper, Redis, Serilog, External API
```

**Dependency flow (STRICT):**
```
Domain ← Application ← Infrastructure
                     ← Api (only via Application interfaces)
```

---

## Code Generation Rules

### File Structure
- 1 file = 1 class / interface / record (NO multiple classes per file)
- Namespace = folder path (e.g., `ICare247.Domain.Entities.Form`)
- Every `.cs` file starts with header:
  ```csharp
  // File    : {FileName}.cs
  // Module  : {ModuleName}
  // Layer   : {Domain|Application|Infrastructure|Api}
  // Purpose : {Mô tả bằng tiếng Việt}
  ```

### CQRS Naming
```
Query  → Get{Object}By{Key}Query        e.g. GetFormByCodeQuery
Handler→ Get{Object}By{Key}QueryHandler e.g. GetFormByCodeQueryHandler
Command→ {Verb}{Object}Command          e.g. ValidateFieldCommand
```

### Repository Naming
```
Interface    → I{Entity}Repository    e.g. IFormRepository
Implementation → {Entity}Repository  e.g. FormRepository
Methods: GetByIdAsync, GetByCodeAsync, GetByFormIdAsync, ExistsAsync, InsertAsync, UpdateAsync
```

### Async Convention
- All methods touching DB/Cache/External: suffix `Async`
- Pass `CancellationToken ct = default` as last parameter
- Never use `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`

---

## Dapper Pattern (Standard Template)

```csharp
public class FormRepository : IFormRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FormRepository(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    public async Task<FormMetadata?> GetByCodeAsync(
        string formCode, CancellationToken ct = default)
    {
        // ── Câu lệnh SQL — parameterized, chọn cột cần thiết ──
        const string sql = """
            SELECT
                Form_Id, Form_Code, Table_Id,
                Platform, Layout_Engine, Version, Is_Active
            FROM  dbo.Ui_Form
            WHERE Form_Code = @FormCode   -- parameterized (không interpolation)
              AND Is_Active  = 1          -- luôn filter active record
            """;

        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<FormMetadata>(
            new CommandDefinition(sql, new { FormCode = formCode },
                cancellationToken: ct));
    }
}
```

---

## Cache Key Pattern

```csharp
// ✅ ĐÚNG: Lấy từ CacheKeys.cs
var key = CacheKeys.Form(formCode, version, langCode, platform);
// result: "icare:form:{formCode}:v{version}:lang:{langCode}:plat:{platform}"

// ❌ SAI: Hardcode string
var key = $"form_{formCode}";
```

**TTL defaults:**
- Form/Field metadata: Memory 120 phút, Redis 240 phút
- Compiled AST delegate: Memory unlimited, Redis 480 phút
- Grammar functions: Memory 480 phút, Redis unlimited (static)

---

## Grammar V1 / AST Rules

Expression lưu trong DB dạng JSON. Pipeline: `Expression_Json → AST → Delegate → Execute`

```json
// Ví dụ Binary expression
{
  "type": "Binary",
  "operator": ">",
  "left":  { "type": "Identifier", "name": "SoLuong" },
  "right": { "type": "Literal", "value": 0, "netType": "Int32" }
}
```

- **Node types:** `Literal`, `Identifier`, `Binary`, `Unary`, `Function`, `CustomHandler`
- **Null propagation:** phép toán với `null` → trả `null` (không throw)
- **Divide by zero** → trả `null` (không throw)
- **Max depth:** 20 nodes (configurable)
- **Function whitelist:** chỉ hàm có trong `Gram_Function` (DB)
- **Operator whitelist:** chỉ operator có trong `Gram_Operator` (DB)

---

## Multi-tenant Rule

```csharp
// Mọi query bảng có Tenant_Id PHẢI có filter:
WHERE Form_Code = @FormCode
  AND Tenant_Id = @TenantId   -- BẮT BUỘC nếu bảng có cột Tenant_Id
  AND Is_Active = 1

// Cache key PHẢI embed tenant:
CacheKeys.Form(formCode, version, langCode, platform)
// Key format: icare:form:{formCode}:v{version}:lang:{langCode}:plat:{platform}
```

Tenant_Id được inject từ HTTP header `X-Tenant-Id` → middleware resolve → DI scope.

---

## Exception Handling

| Exception | Khi nào |
|-----------|---------|
| `ExpressionDepthException` | AST depth > 20 |
| `FunctionNotFoundException` | Function không có trong registry |
| `TypeMismatchException` | Type kết quả không compatible với field |
| `ConfigurationException` | CustomHandler không đăng ký trong registry |

**Rule:** Không `try/catch` swallow trong engine — exception bubble lên `ExceptionHandlingMiddleware`.

---

## API Contract Quick Reference

- **Base URL:** `/api/v1`
- **Required Headers:** `Authorization: Bearer <jwt>`, `X-Tenant-Id: <tenantId>`
- **Success:** HTTP 200, data object (no wrapper envelope)
- **Error:** HTTP 4xx/5xx, RFC 7807 ProblemDetails

**Key endpoints:**
```
GET  /api/v1/forms/{formCode}                  → Load form
POST /api/v1/forms/{formCode}/field-change      → Field changed event
POST /api/v1/forms/{formCode}/validate          → Validate form/field
POST /api/v1/forms/{formCode}/submit            → Submit form
```

---

## Comment Standard (Tiếng Việt)

```csharp
/// <summary>
/// [Mô tả bằng tiếng Việt — 1-2 câu rõ ràng]
/// </summary>
/// <param name="paramName">[Giải thích param]</param>
/// <returns>[Mô tả return value]</returns>
/// <exception cref="ExceptionType">[Khi nào throw]</exception>

// ── Tên bước ── (chia logic block bên trong method)
// NULL-SAFE: [giải thích lý do handle null]
// NOTE: [quyết định thiết kế quan trọng]
// TODO(phase2): [tính năng để lại sau]
// FIXME: [bug đã biết chưa fix]
```

---

## Docs to Read Before Generating Code

| Tình huống | Đọc file |
|-----------|---------|
| Tạo DB entity, repository | `02_DATABASE_SCHEMA.md` |
| Tạo AST node, grammar logic | `03_GRAMMAR_V1_SPEC.md` |
| Tạo engine (validation/event) | `04_ENGINE_SPEC.md` |
| Tạo action/rule params | `05_ACTION_RULE_PARAM_SCHEMA.md` |
| Tạo CQRS handler, folder structure | `06_SOLUTION_STRUCTURE.md` |
| Tạo API endpoint, DTO | `07_API_CONTRACT.md` |
| Tạo cache key, Dapper query | `08_CONVENTIONS.md` |
