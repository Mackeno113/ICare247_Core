# ICare247 Core Platform — AI Agent Configuration

<!--
  FILE: CLAUDE.md
  MỤC ĐÍCH: Router cấu hình cho Claude Code — chỉ chứa tổng quan + pointer.
  Agent đọc thêm file chi tiết trong .claude-rules/ và docs/spec/ KHI CẦN.

  ĐỒNG BỘ: Mọi config nằm trong repo → git sync qua nhiều máy.
  - .claude/settings.json     → team settings (git-tracked)
  - .claude/settings.local.json → per-machine (gitignored qua *.local.json)
  - .claude/commands/          → slash commands (git-tracked)
  - .claude/memory/            → agent memory (git-tracked)
  - .claude-rules/             → coding rules (git-tracked)
-->

## Project Identity

- **Dự án:** ICare247 Core Platform — Enterprise metadata-driven low-code form engine
- **Code:** C# (.NET 9) | **Comment:** Tiếng Việt (bắt buộc) | **Pattern:** Clean Architecture + CQRS + Metadata-driven

## Tech Stack

| Thành phần  | Dùng                            | KHÔNG dùng                  |
| ----------- | ------------------------------- | --------------------------- |
| Backend     | .NET 9 / ASP.NET Core 9         | -                           |
| Frontend    | Blazor WASM + DevExpress        | -                           |
| DB          | MS SQL Server                   | MySQL, PostgreSQL           |
| Data Access | **Dapper**                      | **EF Core (cấm tuyệt đối)** |
| Cache       | MemoryCache + Redis             | -                           |
| Logging     | Serilog + OpenTelemetry         | Console.WriteLine           |
| Auth        | JWT + Policy-based              | -                           |

## Luật bất biến (KHÔNG ngoại lệ)

1. Domain layer = pure C#, không import gì
2. Application chỉ import Domain
3. Infrastructure import Application
4. Api KHÔNG import Infrastructure trực tiếp (trừ composition root)
5. KHÔNG dùng EF Core — chỉ Dapper
6. Mọi SQL = parameterized (không string interpolation)
7. Mọi query/cache key phải có Tenant_Id
8. Async/await xuyên suốt — không .Result, không .Wait()
9. CancellationToken truyền xuyên suốt tất cả async method
10. Không eval / dynamic compile — chỉ AST-based execution

## Quy tắc chi tiết → đọc file tương ứng khi cần

| File | Nội dung |
|---|---|
| `.claude-rules/architecture.md` | Layer dependency, DI registration, CQRS pattern |
| `.claude-rules/csharp-naming.md` | Naming conventions, CQRS/Repository naming |
| `.claude-rules/dapper-patterns.md` | SQL patterns, connection factory, async methods |
| `.claude-rules/caching.md` | CacheKeys.cs, L1/L2 TTL, hybrid strategy |
| `.claude-rules/ast-grammar.md` | AST nodes, operators, functions, null rules |
| `.claude-rules/api-response.md` | Response format, ProblemDetails, RFC 7807 |
| `.claude-rules/comment-rules.md` | File header, class/method/logic block comments |
| `.claude-rules/wpf-configstudio.md` | Prism 9, MaterialDesign, MVVM, navigation |

## Specification → đọc khi cần tra cứu

| File | Nội dung |
|---|---|
| `docs/spec/00_PROJECT_OVERVIEW.md` | Tổng quan, mục tiêu, tech stack |
| `docs/spec/01_ARCHITECTURE.md` | Clean Architecture, caching, security |
| `docs/spec/02_DATABASE_SCHEMA.md` | Toàn bộ bảng DB, columns, constraints |
| `docs/spec/03_GRAMMAR_V1_SPEC.md` | Grammar V1, AST node types, null logic |
| `docs/spec/04_ENGINE_SPEC.md` | 4 engines: Metadata, AST, Validation, Event |
| `docs/spec/05_ACTION_RULE_PARAM_SCHEMA.md` | Action/Rule param schema JSON |
| `docs/spec/06_SOLUTION_STRUCTURE.md` | Folder structure, naming conventions |
| `docs/spec/07_API_CONTRACT.md` | API endpoints, request/response schemas |
| `docs/spec/08_CONVENTIONS.md` | Cache keys, Dapper patterns, comment rules |

> **Khi có câu hỏi về spec** → tra cứu docs/spec/ trước khi tự suy luận.

## Memory (Git-tracked — đồng bộ qua nhiều máy)

| File | Mục đích |
|---|---|
| `.claude/memory/MEMORY.md` | Index tất cả memory files |
| `.claude/memory/last_session.md` | Session trước làm gì → đọc ĐẦU TIÊN mỗi session |
| `.claude/memory/project_current_phase.md` | Phase hiện tại, priorities |
| `.claude/memory/architecture_decisions.md` | ADR — quyết định kiến trúc |
| `.claude/memory/coding_style_feedback.md` | User corrections |
| `.claude/memory/user_profile.md` | Preferences của user |

> **Quy tắc memory:** Khi có quyết định quan trọng hoặc feedback → ghi vào file tương ứng.
> Khi kết thúc session → cập nhật `last_session.md`.

## Slash Commands (dùng `/command-name` trong chat)

| Command | Mô tả |
|---|---|
| `/start-session` | Đọc memory + TASKS.md → tóm tắt → hỏi user làm gì |
| `/pick-task` | Liệt kê top 5 task → user chọn → bắt đầu code |
| `/finish-task` | Build verify → cập nhật TASKS.md + memory → commit |
| `/review-changes` | Review `git diff` theo checklist ICare247 rules |
| `/save-memory` | Lưu quyết định/feedback vào memory |

## Task Tracking

- **Backend tasks:** `TASKS.md` (git root)
- **ConfigStudio WPF tasks:** `docs/ICare247 Config Studio/TASKS_WPF.md`
- Khi bắt đầu task → move sang 🔴 In Progress
- Khi hoàn thành → move sang ✅ Done + commit
- Mọi quyết định thiết kế quan trọng → ghi vào Decisions Log + memory
- Commit sau mỗi task hoàn chỉnh (không commit code dở)

## Session Protocol

1. Đọc `.claude/memory/last_session.md` — biết session trước làm gì
2. Đọc `.claude/memory/project_current_phase.md` — biết đang ở đâu
3. Đọc `TASKS.md` — biết việc cần làm
4. Tóm tắt cho user + hỏi: "Hôm nay làm task nào?"
5. Đọc `.claude-rules/` liên quan đến task
6. Đọc `docs/spec/` nếu cần tra cứu schema/API
7. Code → build verify → commit
8. Cập nhật TASKS.md + `.claude/memory/last_session.md`
9. Nếu có quyết định quan trọng → `/save-memory`
