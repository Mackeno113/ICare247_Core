# CLAUDE.md — ICare247 Core Platform (Claude Code)

<!--
  FILE: CLAUDE.md
  MỤC ĐÍCH: Config riêng cho Claude Code. Mọi rule chung → đọc BRAIN.md.
  ĐỒNG BỘ: Git-tracked → tự động sync qua nhiều máy khi git pull/push.
-->

## ĐỌC ĐẦU TIÊN

> **[BRAIN.md](BRAIN.md)** — Single source of truth: project identity, tech stack,
> hard constraints, architecture, ownership map. Đọc trước khi làm bất cứ thứ gì.

---

## Session Protocol (Claude Code)

1. Đọc `BRAIN.md` — nắm project context + ownership
2. Đọc `.claude/memory/last_session.md` — biết session trước làm gì
3. Đọc `.claude/memory/project_current_phase.md` — biết phase hiện tại
4. Đọc `TASKS.md` — biết việc cần làm
5. Tóm tắt cho user + hỏi: "Hôm nay làm task nào?"
6. Đọc `.claude-rules/` liên quan đến task (xem bảng bên dưới)
7. Đọc `docs/spec/` nếu cần tra cứu schema/API
8. Code → build verify → commit
9. Cập nhật `TASKS.md` + `.claude/memory/last_session.md`
10. Nếu có quyết định quan trọng → `/save-memory`

---

## Coding Rules → đọc file tương ứng khi cần

| File | Nội dung |
|---|---|
| `.claude-rules/architecture.md` | Layer dependency, DI registration, CQRS pattern |
| `.claude-rules/csharp-naming.md` | Naming conventions, CQRS/Repository naming |
| `.claude-rules/dapper-patterns.md` | SQL patterns, connection factory, async methods |
| `.claude-rules/caching.md` | CacheKeys.cs, L1/L2 TTL, hybrid strategy |
| `.claude-rules/ast-grammar.md` | AST nodes, operators, functions, null rules |
| `.claude-rules/api-response.md` | Response format, ProblemDetails, RFC 7807 |
| `.claude-rules/comment-rules.md` | File header, class/method/logic block comments |
| `.claude-rules/wpf-configstudio.md` | Prism 9, DevExpress WPF, MVVM, navigation |

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

---

## Memory (Git-tracked — sync qua nhiều máy)

| File | Mục đích |
|---|---|
| `.claude/memory/MEMORY.md` | Index tất cả memory files |
| `.claude/memory/last_session.md` | Session trước làm gì → đọc ĐẦU TIÊN |
| `.claude/memory/project_current_phase.md` | Phase hiện tại, priorities |
| `.claude/memory/architecture_decisions.md` | ADR — quyết định kiến trúc |
| `.claude/memory/coding_style_feedback.md` | User corrections |
| `.claude/memory/user_profile.md` | Preferences của user |

> Khi kết thúc session → commit memory files + push để sync máy khác.

---

## Slash Commands

| Command | Mô tả |
|---|---|
| `/start-session` | Đọc memory + TASKS.md → tóm tắt → hỏi user làm gì |
| `/pick-task` | Liệt kê top 5 task → user chọn → bắt đầu code |
| `/finish-task` | Build verify → cập nhật TASKS.md + memory → commit |
| `/review-changes` | Review `git diff` theo checklist ICare247 rules |
| `/save-memory` | Lưu quyết định/feedback vào memory |

---

## Task Tracking

- **Backend + Blazor:** `TASKS.md` (git root)
- **ConfigStudio WPF:** `docs/ICare247 Config Studio/TASKS_WPF.md`
- Khi bắt đầu task → 🔴 In Progress | Hoàn thành → ✅ Done + commit
- Commit sau mỗi task hoàn chỉnh — không commit code dở

---

## File Sync (Multi-machine)

> Xem [MACHINE_SWITCH.md](MACHINE_SWITCH.md) để biết protocol khi đổi máy.
