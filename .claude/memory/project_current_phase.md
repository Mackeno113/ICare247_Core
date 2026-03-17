# Project Current Phase

> Cập nhật lần cuối: 2026-03-17

## Backend (.NET 9) — Phase 1 Foundation

| Task | Status |
|---|---|
| Domain entities + AST nodes + Engine interfaces | ✅ Done |
| Application interfaces (IFormRepository, IFieldRepository, ...) | 🟡 Todo — **TIẾP THEO** |
| CacheKeys.cs | 🟡 Todo |
| GetFormByCodeQuery + Handler (skeleton) | 🟡 Todo |
| Infrastructure: SqlConnectionFactory, Repositories | 🟡 Todo |
| HybridCacheService (Memory + Redis) | 🟡 Todo |
| FormController + ExceptionHandlingMiddleware | 🟡 Todo |

**Ưu tiên:** Hoàn thành Phase 1 Foundation trước → mở khóa Phase 2 (Grammar/AST).

## ConfigStudio (WPF) — Chờ P0 UX

| Task | Status |
|---|---|
| 11 skeleton screens | ✅ Done |
| 6 placeholder → UI thật | ✅ Done |
| P0: Auto-save, Undo/Redo, Live Linting, Impact Preview | 🟡 Todo — phụ thuộc Backend engine |

## Blockers
- P0 UX Features (ConfigStudio) phụ thuộc Backend Phase 2+ engine hoạt động
