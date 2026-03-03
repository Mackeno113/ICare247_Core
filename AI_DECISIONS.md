# AI Decision Log - ICare247 Core

## Purpose

Track technical and process decisions that affect both Claude Code and Codex.
Newest valid decision overrides older conflicting notes.

---

## Decision Template

### DEC-YYYY-MM-DD-XXX

- Date:
- Status: `proposed|accepted|superseded|rejected`
- Context:
- Decision:
- Consequences:
- Affected files/modules:
- Supersedes:

---

## Decisions

### DEC-2026-03-03-001

- Date: 2026-03-03
- Status: `accepted`
- Context:
  - Two AI agents need a shared operating model to avoid conflicting edits and drift.
- Decision:
  - Use four governance artifacts as mandatory collaboration protocol:
    - `AI_PROJECT_BRIEF.md` as mission + constraints baseline
    - `AI_TASKS.yaml` as owner/status task board
    - `AI_HANDOFF.md` as shift handoff log
    - `AI_DECISIONS.md` as technical/process decision ledger
- Consequences:
  - Every task status transition must be reflected in handoff log.
  - Scope or convention changes must be recorded as a decision entry.
- Affected files/modules:
  - Repository root governance files
- Supersedes:
  - none

### DEC-2026-03-03-002

- Date: 2026-03-03
- Status: `accepted`
- Context:
  - Existing project rules are split across `AGENTS.md`, `CLAUDE.md`, and spec documents.
- Decision:
  - Conflict precedence order is fixed as:
    1. `AGENTS.md` hard constraints
    2. `docs/spec/*` domain and architecture specs
    3. `AI_DECISIONS.md` latest accepted decision
    4. task-local notes in `AI_TASKS.yaml`
- Consequences:
  - Reduces contradictory behavior between agents.
- Affected files/modules:
  - All
- Supersedes:
  - none
