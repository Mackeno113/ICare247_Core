# BRAIN.md — ICare247 Core Platform

<!--
  FILE: BRAIN.md
  MỤC ĐÍCH: Single source of truth cho CẢ 2 AI agents (Claude Code + Codex).
  Đọc file này ĐẦU TIÊN trước khi làm bất cứ thứ gì.
  CLAUDE.md và AGENTS.md chỉ còn pointer + agent-specific config.
-->

## 1. Project Identity

- **Tên:** ICare247 Core Platform
- **Mô tả:** Enterprise metadata-driven low-code form engine — tạo và vận hành form nghiệp vụ hoàn toàn từ cấu hình database, không cần code, không deploy lại khi thay đổi logic
- **Ngôn ngữ comment:** Tiếng Việt (bắt buộc trong mọi file .cs và .xaml)
- **Giao tiếp với user:** Tiếng Việt

---

## 2. Tech Stack

| Thành phần | Dùng | KHÔNG dùng |
|---|---|---|
| Backend | .NET 9 / ASP.NET Core 9 | - |
| Frontend (runtime) | Blazor WASM + DevExpress Blazor | - |
| Frontend (admin tool) | WPF + DevExpress WPF + Prism 9 | MaterialDesign trong WPF |
| Database | MS SQL Server | MySQL, PostgreSQL |
| Data Access | **Dapper** | **EF Core (cấm tuyệt đối)** |
| Cache | MemoryCache (L1) + Redis (L2) | - |
| Logging | Serilog + OpenTelemetry | Console.WriteLine |
| Auth | JWT + Policy-based | - |
| CQRS | MediatR | - |

---

## 3. Hard Constraints — KHÔNG ngoại lệ, KHÔNG vi phạm

1. **Không EF Core** — data access chỉ bằng Dapper
2. **Không string interpolation vào SQL** — bắt buộc parameterized query
3. **Không eval / dynamic compile** — chỉ AST-based execution (Grammar V1)
4. **Không new Infrastructure class trong Api layer** — chỉ qua DI (trừ Program.cs)
5. **Không hardcode cache key** — lấy từ `CacheKeys.cs`
6. **Không swallow exception trong engine** — bubble lên middleware
7. **Không `.Result` hay `.Wait()`** — async/await xuyên suốt
8. **Không bỏ qua Tenant_Id** — mọi query/cache key phải có tenant
9. **Không `SELECT *`** — chỉ select cột cần thiết
10. **Không tự commit/push** — chỉ khi user yêu cầu rõ ràng
11. **Không xóa DB mà không confirm** — dialog xác nhận, default = No

---

## 4. Architecture — Clean Architecture (4 layers)

```
Domain        ← pure C#, không import gì khác
Application   ← import Domain only (CQRS, MediatR, interfaces)
Infrastructure ← import Application (Dapper, Redis, Serilog)
Api           ← import Application only (KHÔNG import Infrastructure trực tiếp)
```

**ConfigStudio WPF:** Kết nối trực tiếp SQL Server qua Dapper — KHÔNG gọi HTTP API backend.

---

## 5. Ownership Map — ai làm gì, không chồng lấn

| Vùng | Owner | Ghi chú |
|---|---|---|
| `src/backend/` (.NET 9 toàn bộ) | **Claude Code** | Domain, Application, Infrastructure, Api |
| `src/blazor/` (Blazor WASM) | **Claude Code** | FormRunner, Renderers, Services |
| `src/frontend/ConfigStudio.WPF/` | **Codex** | MVVM, Views, Modules, Prism |
| `db/` (SQL migrations) | **Codex** | Tạo file migration SQL |
| `tests/` (unit + integration) | **Codex** | xUnit, test coverage |
| `docs/spec/` | **Claude Code** | Maintain + update khi thay đổi |
| `.claude/memory/` | **Claude Code** | Update sau mỗi session, commit master |
| `.codex/memory/` | **Codex** | Update sau mỗi session, commit master |
| `TASKS.md` | **Cả 2** | Ai làm task nào thì update |
| `AI_HANDOFF.md` | **Cả 2** | Bắt buộc update khi bàn giao |
| `AI_DECISIONS.md` | **Cả 2** | Ghi mọi quyết định kiến trúc quan trọng |
| `BRAIN.md` | **Cả 2** | Chỉ sửa khi có thay đổi cơ bản |

**Quy tắc conflict:** Chỉ 1 agent edit 1 file tại 1 thời điểm. Nếu cần sửa file của agent kia → ghi vào `AI_HANDOFF.md` và đợi.

---

## 6. Coding Standards

### File header (mọi .cs file)
```csharp
// File    : {FileName}.cs
// Module  : {ModuleName}
// Layer   : {Domain|Application|Infrastructure|Api}
// Purpose : {Mô tả bằng tiếng Việt}
```

### CQRS Naming
```
Query   → Get{Object}By{Key}Query         → Get{Object}By{Key}QueryHandler
Command → {Verb}{Object}Command           → {Verb}{Object}CommandHandler
```

### Repository Naming
```
Interface → I{Entity}Repository    (IFormRepository)
Impl      → {Entity}Repository     (FormRepository)
Methods   → GetByIdAsync, GetByCodeAsync, ExistsAsync, InsertAsync, UpdateAsync
```

### Async convention
- Mọi method DB/Cache/External: suffix `Async`, param `CancellationToken ct = default`
- Không `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`

### Dapper pattern chuẩn
```csharp
using var conn = _connectionFactory.CreateConnection();
return await conn.QueryFirstOrDefaultAsync<T>(
    new CommandDefinition(sql, parameters, cancellationToken: ct));
```

### Cache key pattern
```csharp
// ✅ ĐÚNG
var key = CacheKeys.Form(formCode, version, langCode, platform);
// ❌ SAI
var key = $"form_{formCode}";
```

### Comment blocks
```csharp
// ── Tên bước ──
// NULL-SAFE: [lý do]
// NOTE: [quyết định quan trọng]
// TODO(phase2): [để lại]
// FIXME: [bug đã biết]
```

### WPF/XAML (Codex)
- UI library: DevExpress (`dx:`, `dxe:`, `dxg:`) + pure WPF — KHÔNG có MaterialDesign
- Icon: Unicode text (⚙ ✎ 👁 ⧉) — không dùng PackIcon
- `<Run Text="{Binding Prop, Mode=OneWay}" />` — bắt buộc Mode=OneWay
- Đọc ít nhất 1 view hiện có trong module trước khi viết XAML mới

---

## 7. Key Files — tra cứu khi cần

### Coding rules (đọc khi liên quan)
| File | Nội dung |
|---|---|
| `.claude-rules/architecture.md` | Layer dependency, DI, CQRS pattern |
| `.claude-rules/csharp-naming.md` | Naming conventions |
| `.claude-rules/dapper-patterns.md` | SQL patterns, connection factory |
| `.claude-rules/caching.md` | CacheKeys.cs, L1/L2 TTL |
| `.claude-rules/ast-grammar.md` | AST nodes, operators, functions |
| `.claude-rules/api-response.md` | Response format, RFC 7807 |
| `.claude-rules/comment-rules.md` | File header, class/method comments |
| `.claude-rules/wpf-configstudio.md` | Prism 9, DevExpress WPF, MVVM |
| `.claude-rules/debug-logger.md` | DebugLogger thay Console.WriteLine |

### Specification (tra cứu trước khi code)
| File | Khi nào đọc |
|---|---|
| `docs/spec/02_DATABASE_SCHEMA.md` | Tạo entity, repository, migration |
| `docs/spec/03_GRAMMAR_V1_SPEC.md` | AST node, grammar logic |
| `docs/spec/04_ENGINE_SPEC.md` | Validation/Event engine |
| `docs/spec/06_SOLUTION_STRUCTURE.md` | Folder structure, naming |
| `docs/spec/07_API_CONTRACT.md` | API endpoint, DTO |
| `docs/spec/08_CONVENTIONS.md` | Cache key, Dapper query |
| `docs/spec/11_BLAZOR_CONTROL_RENDERER_SPEC.md` | Blazor renderers |

---

## 8. Session Protocol (áp dụng cho cả 2 agents)

### Mở session
1. `git pull origin master` — sync memory và config
2. Đọc memory file của agent mình (`last_session.md`)
3. Đọc `TASKS.md` — task đang In Progress và Todo
4. Tóm tắt cho user + hỏi task hôm nay

### Trong session
- Đọc `.claude-rules/` + `docs/spec/` liên quan trước khi code
- Code → build verify → report kết quả, DỪNG, chờ user
- KHÔNG tự commit/push

### Kết session
1. Build verify pass
2. Cập nhật `TASKS.md` (move task → ✅ Done)
3. Cập nhật memory file của agent mình (`last_session.md`, `project_current_phase.md`)
4. Nếu có quyết định kiến trúc → ghi vào `AI_DECISIONS.md`
5. Commit memory files lên **master** (không phải feature branch)
6. Hỏi user commit message + push

### Khi chuyển máy
1. Máy hiện tại: commit + push memory → `git push origin master`
2. Máy mới: `git pull origin master` → mở session bình thường
3. Memory tự động sync qua git — không cần làm thêm gì

---

## 9. Task Protocol

- Task tracking: `TASKS.md`
- WPF tasks: `docs/ICare247 Config Studio/TASKS_WPF.md`
- Khi bắt đầu task → move sang 🔴 In Progress
- Khi xong → move sang ✅ Done + commit
- Branch: `feat/<task-id>-<short>` hoặc `fix/<task-id>-<short>`
- Commit: `<task-id>: <summary tiếng Việt>`

---

## 10. Definition of Done

Task hoàn thành khi:
1. Acceptance criteria thỏa mãn
2. Build pass (0 error, 0 warning)
3. Không vi phạm Hard Constraints (mục 3)
4. `AI_HANDOFF.md` cập nhật nếu bàn giao cho agent kia
5. Memory file cập nhật
