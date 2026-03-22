# Project Current Phase

> Cập nhật lần cuối: 2026-03-20

## Backend (.NET 9) — Hoàn thành Phase 1-5

| Phase | Status |
|---|---|
| Phase 1 — Foundation (entities, repos, cache, controller) | ✅ Done |
| Phase 2 — Grammar V1 / AST Engine (parser, compiler, 25 functions) | ✅ Done |
| Phase 3 — Validation Engine (rules, dependencies, topological sort) | ✅ Done |
| Phase 4 — Event Engine (6 action handlers, UiDelta) | ✅ Done |
| Phase 5 — API Infrastructure (middleware, JWT, OpenTelemetry, RuntimeController) | ✅ Done |
| Phase 6 — Form Management CRUD (Backend API) | ✅ Done |
| DB Schema — 30 tables, 5 modules (000_create_schema.sql) | ✅ Done |
| DB Seed — Lookup data (001_seed_lookup_data.sql) | ✅ Done |

**Remaining backend tasks:**
- MetadataEngine implementation (IMetadataEngine)
- Integration tests

## ConfigStudio (WPF) — Hoàn thành

| Component | Status |
|---|---|
| 11 skeleton screens | ✅ Done |
| 6 screens UI thật (FormEditor, FormManager, RuleEditor, EventEditor, Grammar, I18n) | ✅ Done |
| Direct DB Wave 1-4 (6 services, 15 DTOs, 6 implementations) | ✅ Done |
| P0 UX: Auto-save, Undo/Redo, Live Linting, Impact Preview | ✅ Done |
| Shell, Navigation, FormDetail, FormEditDialog, SysTableManager | ✅ Done |

**Remaining WPF tasks:**
- Wire Impact Preview vào DependencyViewer UI
- Pass `tableCode` khi navigate từ FieldConfig → I18nManager
- **Chạy migration 003**: `docs/migrations/003_remove_val_rule_field.sql` trên DB thật
- **Chạy migration 004**: `docs/migrations/004_add_sys_lookup.sql` trên DB thật
- Test LookupBox end-to-end (cấu hình GioiTinh + PhongBanID)
- Feature "Diễn giải cấu hình" — hiển thị ý nghĩa JSON bằng tiếng Việt
- Màn hình quản lý Sys_Lookup trong ConfigStudio (thêm/sửa/xóa lookup code)

## Next Priorities
1. MetadataEngine (IMetadataEngine) — backend
2. Integration tests — backend
3. Blazor runtime frontend
