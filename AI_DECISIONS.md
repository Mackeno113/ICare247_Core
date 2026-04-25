# AI Decision Log — ICare247 Core

Ghi lại các quyết định kỹ thuật và quy trình ảnh hưởng đến cả Claude Code và Codex.
**Entry mới hơn ghi đè entry cũ hơn nếu mâu thuẫn.**

---

## DEC-2026-04-25-001

- **Date:** 2026-04-25
- **Status:** `accepted`
- **Context:** BRAIN.md được tạo làm single source of truth. DEC-2026-03-03-002 (AGENTS.md là source ưu tiên cao nhất) trở nên lỗi thời và mâu thuẫn.
- **Decision:** Thứ tự ưu tiên config mới:
  1. `BRAIN.md` — hard constraints, tech stack, ownership map (cao nhất)
  2. `docs/spec/*` — domain và architecture specs
  3. `AI_DECISIONS.md` — decision mới nhất được accepted
  4. Task-local notes trong `AI_TASKS.yaml`
  - `CLAUDE.md` và `AGENTS.md` chỉ còn agent-specific protocol, không phải nguồn truth
- **Consequences:** Khi có conflict → luôn follow BRAIN.md.
- **Supersedes:** DEC-2026-03-03-002

---

## DEC-2026-04-25-002

- **Date:** 2026-04-25
- **Status:** `accepted`
- **Context:** Các migration files 003-015 tồn tại độc lập, gây khó maintain và dễ mất đồng bộ với DB thực tế.
- **Decision:** Gộp toàn bộ migrations thành 2 file canonical:
  - `docs/migrations/000_create_schema.sql` — full schema (30 tables)
  - `docs/migrations/001_seed_all.sql` — seed data
  - File 000/001 dùng cho DB mới. DB hiện tại đã có schema đầy đủ.
- **Consequences:** Không tạo thêm migration file lẻ nữa. Khi thay đổi schema → update 000_create_schema.sql + ghi decision.
- **Supersedes:** none

---

## DEC-2026-03-03-001

- **Date:** 2026-03-03
- **Status:** `accepted`
- **Context:** Hai AI agent cần operating model chung để tránh conflict và drift.
- **Decision:** Dùng 4 governance artifacts làm collaboration protocol bắt buộc:
  - `AI_TASKS.yaml` — task board với owner/status
  - `AI_HANDOFF.md` — handoff log khi bàn giao
  - `AI_DECISIONS.md` — decision ledger
  - `BRAIN.md` (thay thế AI_PROJECT_BRIEF.md từ 2026-04-25)
- **Consequences:** Mọi task status transition phải reflect trong handoff log. Thay đổi convention → ghi decision entry.
- **Supersedes:** none

---

## DEC-2026-03-03-002

- **Date:** 2026-03-03
- **Status:** `superseded`
- **Superseded by:** DEC-2026-04-25-001
- **Decision cũ (không còn áp dụng):** AGENTS.md là source có ưu tiên cao nhất.
