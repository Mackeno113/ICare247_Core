# CLAUDE.md — ICare247 Core Platform (Claude Code)

<!--
  FILE: CLAUDE.md
  MỤC ĐÍCH: Config riêng cho Claude Code. Mọi rule chung → đọc BRAIN.md.
  ĐỒNG BỘ: Git-tracked → tự động sync qua nhiều máy khi git pull/push.
-->

## ⛔ NGUYÊN TẮC SỐ 1 — LUÔN HỎI TRƯỚC KHI QUYẾT ĐỊNH

> **LUÔN HỎI TRƯỚC khi quyết định làm bất cứ việc gì.** Không tự ý chọn cách làm,
> không tự bịa dữ liệu mẫu (mock), không tự suy diễn yêu cầu. Khi yêu cầu chưa rõ
> hoặc có nhiều hướng (VD: dữ liệu thật theo cấu hình vs. mock; chọn API/bảng/field;
> phạm vi tính năng) → **DỪNG LẠI và HỎI user**, rồi mới làm.
> Mặc định: dữ liệu lấy THẬT theo thông số cấu hình của hệ thống, không phải dữ liệu giả.

---

## ĐỌC ĐẦU TIÊN

> **[BRAIN.md](BRAIN.md)** — Single source of truth: project identity, tech stack,
> hard constraints, architecture, ownership map. Đọc trước khi làm bất cứ thứ gì.
>
> **[docs/ai/README.md](docs/ai/README.md)** — Mục lục hạ tầng AI: 14 agent + 20 command +
> governance template. Cửa vào duy nhất khi cần tra "có tooling gì để dùng".

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
| `.claude-rules/blazor-ui.md` | Blazor WASM: tránh `oninput` gây re-render lag, debounce, ghim header lưới |

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
| `docs/spec/09_FIELD_CONFIG_GUIDE.md` | Field config UI — behavior, layout, i18n |
| `docs/spec/10_RESOURCE_KEY_CONVENTION.md` | i18n key convention: validation + field display (label/placeholder/tooltip) |
| `docs/spec/11_DATA_DB_SCHEMA.md` | Data DB per-tenant — nền tảng HT_/DM_/TC_ (người dùng, danh mục, tổ chức) |

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

<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **ICare247_Core** (9336 symbols, 16837 relationships, 300 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.

> Index stale? Run `node .gitnexus/run.cjs analyze` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? `npx gitnexus analyze` (npm 11 crash → `npm i -g gitnexus`; #1939).

## Always Do

- **MUST run impact analysis before editing any symbol.** Before modifying a function, class, or method, run `impact({target: "symbolName", direction: "upstream"})` and report the blast radius (direct callers, affected processes, risk level) to the user.
- **MUST run `detect_changes()` before committing** to verify your changes only affect expected symbols and execution flows. For regression review, compare against the default branch: `detect_changes({scope: "compare", base_ref: "master"})`.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- When exploring unfamiliar code, use `query({search_query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `context({name: "symbolName"})`.
- For security review, `explain({target: "fileOrSymbol"})` lists taint findings (source→sink flows; needs `analyze --pdg`).

## Never Do

- NEVER edit a function, class, or method without first running `impact` on it.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis.
- NEVER rename symbols with find-and-replace — use `rename` which understands the call graph.
- NEVER commit changes without running `detect_changes()` to check affected scope.

## Resources

| Resource | Use for |
|----------|---------|
| `gitnexus://repo/ICare247_Core/context` | Codebase overview, check index freshness |
| `gitnexus://repo/ICare247_Core/clusters` | All functional areas |
| `gitnexus://repo/ICare247_Core/processes` | All execution flows |
| `gitnexus://repo/ICare247_Core/process/{name}` | Step-by-step execution trace |

## CLI

| Task | Read this skill file |
|------|---------------------|
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->
