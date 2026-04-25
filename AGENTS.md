# AGENTS.md — ICare247 Core Platform (OpenAI Codex)

<!--
  FILE: AGENTS.md
  MỤC ĐÍCH: Config riêng cho OpenAI Codex CLI. Mọi rule chung → đọc BRAIN.md.
  Codex tự động đọc file này khi khởi động trong thư mục project.
-->

## ĐỌC ĐẦU TIÊN

> **[BRAIN.md](BRAIN.md)** — Single source of truth: project identity, tech stack,
> hard constraints, architecture, ownership map. Đọc trước khi làm bất cứ thứ gì.

---

## Session Protocol (Codex)

1. Đọc `BRAIN.md` — nắm project context + ownership
2. Đọc `.codex/memory/last_session.md` — biết session trước làm gì
3. Đọc `.codex/memory/current_phase.md` — biết phase hiện tại
4. Đọc `AI_TASKS.yaml` — xem task có `agent: codex`
5. Đọc `AI_HANDOFF.md` — biết Claude Code đang làm gì, tránh conflict
6. Code → build/test verify → cập nhật `AI_TASKS.yaml`
7. Cập nhật `.codex/memory/last_session.md` + commit + push

---

## Ownership Codex (xem đầy đủ trong BRAIN.md § Ownership)

Codex **chịu trách nhiệm chính** (không cần hỏi Claude Code):
- `src/ConfigStudio.WPF/` — toàn bộ WPF desktop tool
- `tests/` — unit + integration tests
- `db/` — schema, seed, migrations

Codex **KHÔNG tự ý sửa** (phải có handoff từ Claude Code):
- `src/ICare247.Api/`
- `src/ICare247.Application/`
- `src/ICare247.Domain/`
- `src/ICare247.Infrastructure/`
- `src/ICare247.Blazor.WASM/`

---

## Handoff Protocol

Trước khi bắt đầu → đọc `AI_HANDOFF.md` để biết Claude Code đang làm gì.

Sau khi hoàn thành task → cập nhật `AI_HANDOFF.md`:
```markdown
### [DATE] Codex → Claude Code
- Hoàn thành: [tên task]
- Files changed: [danh sách file]
- Cần Claude Code biết: [thông tin quan trọng]
```

---

## Docs to Read Before Generating Code

| Tình huống | Đọc file |
|---|---|
| Tạo DB entity, repository | `docs/spec/02_DATABASE_SCHEMA.md` |
| Tạo AST node, grammar logic | `docs/spec/03_GRAMMAR_V1_SPEC.md` |
| Tạo engine (validation/event) | `docs/spec/04_ENGINE_SPEC.md` |
| Tạo action/rule params | `docs/spec/05_ACTION_RULE_PARAM_SCHEMA.md` |
| Tạo CQRS handler, folder structure | `docs/spec/06_SOLUTION_STRUCTURE.md` |
| Tạo API endpoint, DTO | `docs/spec/07_API_CONTRACT.md` |
| Tạo cache key, Dapper query | `docs/spec/08_CONVENTIONS.md` |

---

## Memory (Codex — Git-tracked)

| File | Mục đích |
|---|---|
| `.codex/memory/last_session.md` | Session trước Codex làm gì |
| `.codex/memory/current_phase.md` | Phase hiện tại, WPF priorities |
| `.codex/memory/coding_style_feedback.md` | User corrections dành cho Codex |

> Khi kết thúc session → commit memory files + push để sync máy khác.

---

## File Sync (Multi-machine)

> Xem [MACHINE_SWITCH.md](MACHINE_SWITCH.md) để biết protocol khi đổi máy.
