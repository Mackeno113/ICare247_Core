# ICare247 Core Platform — AI Agent Configuration (Claude Code)

<!--
  FILE: CLAUDE.md
  MỤC ĐÍCH: Cấu hình hành vi cho Claude Code khi làm việc trong repo ICare247.
  Claude Code tự động đọc file này khi khởi động session trong thư mục project.
  Tất cả quy tắc dưới đây là BẮT BUỘC — không được bỏ qua.
-->

## Project Identity

- **Tên dự án:** ICare247 Core Platform
- **Loại:** Enterprise metadata-driven low-code form engine
- **Ngôn ngữ code:** C# (.NET 9)
- **Ngôn ngữ comment:** Tiếng Việt (bắt buộc)
- **Pattern chính:** Clean Architecture + CQRS + Metadata-driven

---

## Tech Stack — Bắt Buộc Dùng

| Thành phần  | Công nghệ                       | KHÔNG dùng                  |
| ----------- | ------------------------------- | --------------------------- |
| Backend     | .NET 9 / ASP.NET Core 9         | -                           |
| Frontend    | Blazor WebAssembly + DevExpress | -                           |
| Database    | MS SQL Server                   | MySQL, PostgreSQL           |
| Data Access | **Dapper**                      | **EF Core (cấm tuyệt đối)** |
| Cache       | MemoryCache + Redis             | -                           |
| Logging     | Serilog + OpenTelemetry         | Console.WriteLine           |
| Auth        | JWT + Policy-based              | -                           |

---

## Architecture Rules — Luật Bất Biến

### Layer Dependency (Clean Architecture)

```
Domain          ← KHÔNG import gì (pure C#, no ORM)
Application     ← chỉ import Domain
Infrastructure  ← import Application (để implement interfaces)
Api             ← chỉ import Application (KHÔNG import Infrastructure trực tiếp)
```

### Quy tắc khi generate code

- `Api` layer **không bao giờ** `new` trực tiếp bất kỳ class nào từ `Infrastructure`
- DI registration chỉ trong `DependencyInjection.cs` của từng project
- `Program.cs` chỉ gọi:
    ```csharp
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure();
    ```

---

## Coding Rules — Checklist Bắt Buộc Trước Khi Output

```
✅ Namespace phải match folder path (ICare247.Domain.Entities.Form)
✅ Mỗi file = đúng 1 class / interface / record (không gộp)
✅ Repository method suffix = Async (GetByCodeAsync, không GetByCode)
✅ Query = IRequest<TResponse>, Command = IRequest<TResponse>
✅ Handler = IRequestHandler<TRequest, TResponse>
✅ KHÔNG new Infrastructure class trong Api layer
✅ KHÔNG dùng EF Core — chỉ Dapper
✅ Mọi Dapper query = parameterized (không string interpolation vào SQL)
✅ Cache key lấy từ CacheKeys.cs (không hardcode string rải rác)
✅ Exception bubble lên — không swallow trong engine
✅ Async/await xuyên suốt — không .Result hay .Wait()
✅ CancellationToken truyền xuyên suốt tất cả async method
✅ Multi-tenant: mọi query/cache key phải có Tenant_Id
✅ Không eval / dynamic compile — chỉ AST-based execution
✅ Grammar: chỉ function/operator whitelist từ Gram_Function, Gram_Operator
```

---

## Naming Conventions

### C# General

| Loại           | Convention       | Ví dụ                           |
| -------------- | ---------------- | ------------------------------- |
| Class          | PascalCase       | `FormMetadata`, `AstParser`     |
| Interface      | `I` + PascalCase | `IFormRepository`, `IAstEngine` |
| Method         | PascalCase       | `GetByCodeAsync`, `Evaluate`    |
| Property       | PascalCase       | `FormCode`, `IsActive`          |
| Private field  | `_` + camelCase  | `_repository`, `_cache`         |
| Local variable | camelCase        | `formMetadata`, `ruleList`      |
| Constant       | PascalCase       | `MaxAstDepth`, `DefaultTimeout` |
| Async method   | suffix `Async`   | `GetFormByCodeAsync`            |

### CQRS Pattern

```csharp
// Query (đọc dữ liệu)
public record GetFormByCodeQuery(string FormCode, string LangCode) : IRequest<FormDto>;

// Query Handler
public class GetFormByCodeQueryHandler : IRequestHandler<GetFormByCodeQuery, FormDto>
{
    public async Task<FormDto> Handle(GetFormByCodeQuery request, CancellationToken ct) { }
}

// Command (ghi/thực thi)
public record ValidateFieldCommand(int FormId, string FieldCode, object Value)
    : IRequest<ValidateFieldResponse>;
```

### Repository Pattern

- Interface: `I{Entity}Repository` (VD: `IFormRepository`)
- Implementation: `{Entity}Repository` (VD: `FormRepository`)
- Method: `GetByCodeAsync`, `GetByIdAsync`, `GetByFormIdAsync`

---

## File Header — Bắt Buộc Cho Mọi File .cs

```csharp
// File    : {FileName}.cs
// Module  : {ModuleName}
// Layer   : {Domain | Application | Infrastructure | Api}
// Purpose : {Mô tả ngắn bằng tiếng Việt}
```

---

## Comment Rules (Tiếng Việt)

### Class/Interface

```csharp
/// <summary>
/// Repository truy vấn metadata form từ bảng <c>Ui_Form</c> qua Dapper.
/// Tất cả query phải parameterized, filter <c>Is_Active = 1</c>.
/// </summary>
public class FormRepository : IFormRepository
```

### Public Method

```csharp
/// <summary>
/// Load metadata form theo Form_Code. Trả về <c>null</c> nếu không tìm thấy.
/// </summary>
/// <param name="formCode">Ui_Form.Form_Code — unique identifier của form.</param>
/// <param name="ct">Cancellation token để hủy query nếu request bị cancel.</param>
/// <returns><see cref="FormMetadata"/> nếu tìm thấy; <c>null</c> nếu không tồn tại.</returns>
/// <exception cref="SqlException">Throw khi DB lỗi — không swallow.</exception>
public async Task<FormMetadata?> GetByCodeAsync(string formCode, CancellationToken ct = default)
```

### Logic Block trong Method

```csharp
// ── 1. Check cache ───────────────────────────────────────
// ── 2. Load from DB ─────────────────────────────────────
// ── 3. Build response ────────────────────────────────────
```

### Edge Case / Null Check

```csharp
// NULL-SAFE: Identifier không tồn tại trong context → trả null, không throw.
// Lý do: form có thể chưa có giá trị khi mới load → không phải lỗi.
if (!context.TryGetValue(node.Name, out var value))
    return null;
```

### TODO Tags

```csharp
// TODO(phase2): Hỗ trợ array index trong dot-notation path
// FIXME: Race condition nếu 2 request cùng compile cùng 1 expression
// NOTE: Dùng OrdinalIgnoreCase vì Column_Code là technical name
// HACK: Workaround tạm thời, cần refactor
```

---

## Dapper Query Rules

```csharp
// ✅ ĐÚNG — Parameterized
const string sql = """
    SELECT Form_Id, Form_Code, Version
    FROM   dbo.Ui_Form
    WHERE  Form_Code = @FormCode   -- luôn dùng named parameter
      AND  Is_Active = 1           -- luôn filter active
    """;
await conn.QueryFirstOrDefaultAsync<FormMetadata>(
    new CommandDefinition(sql, new { FormCode = formCode }, cancellationToken: ct));

// ❌ SAI — String interpolation (SQL injection risk)
string sql = $"SELECT * FROM Ui_Form WHERE Form_Code = '{formCode}'";

// ❌ SAI — SELECT * (không rõ ràng, tốn băng thông)
SELECT * FROM dbo.Ui_Form
```

**Quy tắc bổ sung:**

- Luôn dùng `async`: `QueryAsync`, `QueryFirstOrDefaultAsync`, `ExecuteAsync`
- Connection từ `IDbConnectionFactory` — không `new SqlConnection()` trực tiếp
- Bảng có `Tenant_Id` → **bắt buộc** có `AND Tenant_Id = @TenantId` trong WHERE
- Tránh `SELECT * ` — chỉ SELECT cột cần thiết

---

## Cache Key Rules

```csharp
// Tất cả key lấy từ CacheKeys.cs — không hardcode string
var key = CacheKeys.Form(formCode, version, langCode, platform);
// → "icare:form:{formCode}:v{version}:lang:{langCode}:plat:{platform}"

// ❌ SAI — hardcode string rải rác
var key = $"form_{formCode}_{version}";
```

---

## AST / Grammar Rules

- Expression lưu dạng JSON trong DB (`Expression_Json`)
- Parse: `Expression_Json` → `IExpressionNode` (AST)
- Compile: `IExpressionNode` → `Func<EvaluationContext, object>` (delegate)
- Execute: delegate với `Dictionary<string, object>` context
- **Max depth = 20** (configurable `appsettings.Grammar.MaxAstDepth`)
- **Null propagation**: phép toán với null → trả null (không throw)
- **Divide by zero**: trả null (không throw)
- Không `eval()`, không `Roslyn.Compile`, không `dynamic` SQL

---

## API Response Format

**Thành công:** Trả data trực tiếp (không wrap envelope)

```json
HTTP 200 OK
{ ...data object... }
```

**Lỗi:** RFC 7807 ProblemDetails

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

---

## Solution Structure Tham Khảo

```
ICare247/
├── src/
│   ├── backend/
│   │   ├── ICare247.Domain/          ← Entities, AST nodes, Engine interfaces
│   │   ├── ICare247.Application/     ← CQRS Queries/Commands/Handlers, Interfaces
│   │   ├── ICare247.Infrastructure/  ← Dapper repos, Cache, AST engine impl
│   │   └── ICare247.Api/             ← Controllers, Middleware, Extensions
│   └── frontend/
│       └── ICare247.Client/          ← Blazor WASM
├── db/
│   ├── ICare247_Config.sql
│   └── ICare247_SeedData.sql
└── docs/                             ← Toàn bộ specs .md
```

---

## Docs Reference

| File                                  | Nội dung                                     |
| ------------------------------------- | -------------------------------------------- |
| `docs/00_PROJECT_OVERVIEW.md`         | Tổng quan, mục tiêu, tech stack              |
| `docs/01_ARCHITECTURE.md`             | Clean Architecture, caching, security        |
| `docs/02_DATABASE_SCHEMA.md`          | Toàn bộ bảng DB, columns, constraints        |
| `docs/03_GRAMMAR_V1_SPEC.md`          | Grammar V1, AST node types, null logic       |
| `docs/04_ENGINE_SPEC.md`              | Các engine: Metadata, AST, Validation, Event |
| `docs/05_ACTION_RULE_PARAM_SCHEMA.md` | Action/Rule param schema JSON                |
| `docs/06_SOLUTION_STRUCTURE.md`       | Folder structure, naming conventions         |
| `docs/07_API_CONTRACT.md`             | API endpoints, request/response schemas      |
| `docs/08_CONVENTIONS.md`              | Cache keys, Dapper patterns, comment rules   |

> **Khi có câu hỏi về spec** → tra cứu docs/ trước khi tự suy luận.

## Task Tracking

- File tracking: `TASKS.md` (git root)
- Khi bắt đầu task mới → move từ 🟡 Todo sang 🔴 In Progress
- Khi hoàn thành → move sang ✅ Done + commit
- Mọi quyết định thiết kế quan trọng → ghi vào "Decisions Log"
- Commit sau mỗi task hoàn chỉnh (không commit code dở)

```

---

## Tóm tắt Flow
```

Mở terminal
↓
cd D:/ICare247_Core && claude
↓
"Đọc TASKS.md, hôm nay làm [task X]"
↓
Claude code → bạn review
↓
"Xong rồi, cập nhật TASKS.md và commit"
↓
git push
