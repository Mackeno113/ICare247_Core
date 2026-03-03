# AI Project Brief - ICare247 Core

## 1) Mission

Build and evolve `ICare247 Core Platform` as an enterprise metadata-driven low-code form engine.

Primary outcomes:

- Stable backend foundation on .NET 9 Clean Architecture
- Dapper-only data access with strict multi-tenant safety
- Grammar V1 AST-based rule/event execution (no eval/dynamic compile)
- Config Studio workflow and UX planning artifacts ready for implementation

## 2) Single Source Of Truth

All AI agents must read these files first, in order:

1. `AGENTS.md`
2. `CLAUDE.md`
3. `docs/spec/00_PROJECT_OVERVIEW.md` -> `08_CONVENTIONS.md`
4. `TASKS.md`
5. This file: `AI_PROJECT_BRIEF.md`

If instructions conflict:

- Security and architecture constraints in `AGENTS.md` win
- Domain specs in `docs/spec/*` win over assumptions
- Latest decision in `AI_DECISIONS.md` wins over older notes

## 3) Hard Constraints (Non-Negotiable)

- No EF Core, only Dapper
- No SQL string interpolation, only parameterized query
- No eval/dynamic compile, only AST-based execution
- No Infrastructure object creation in Api layer outside DI
- No hardcoded cache keys, use `CacheKeys.cs`
- No swallowed exceptions in engine, bubble to middleware
- No `.Result` / `.Wait()`, async/await end-to-end
- No tenant omission in query/cache scope
- No `SELECT *`, select explicit columns

## 4) Architecture Scope

- `src/backend/src/ICare247.Domain`
- `src/backend/src/ICare247.Application`
- `src/backend/src/ICare247.Infrastructure`
- `src/backend/src/ICare247.Api`

Related planning/docs:

- `docs/spec/*`
- `docs/ICare247 Config Studio/*`

## 5) Definition Of Done (Per Task)

A task is done only when:

1. Acceptance criteria in `AI_TASKS.yaml` are satisfied
2. Build passes for affected projects
3. No violation of hard constraints
4. Tests are added/updated where relevant (or explicit test gap noted)
5. `AI_HANDOFF.md` is updated with evidence and next steps

## 6) Agent Collaboration Protocol

- Work ownership is declared in `AI_TASKS.yaml` (`owner: claude|codex`)
- Only one agent edits the same file at a time
- Each handoff must update `AI_HANDOFF.md`
- Any architecture or convention change must be logged in `AI_DECISIONS.md`

## 7) Branch And PR Convention

- Branch naming: `feat/<task-id>-<short-name>` or `fix/<task-id>-<short-name>`
- Commit prefix: `<task-id>: <summary>`
- PR title: `[<task-id>] <summary>`

## 8) Current Focus (Editable)

- Phase: Foundation + UX planning alignment
- Priority lane: P0 UX technical decomposition + backend skeleton completion
- Active tracker: `TASKS.md` and `docs/ICare247 Config Studio/UX_P0_TECH_TASKS_BY_MODULE.md`
