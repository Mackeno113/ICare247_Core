# Project Current Phase

> Cập nhật lần cuối: 2026-03-23

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

## Blazor RuntimeCheck — Đang hoàn thiện

| Component | Status |
|---|---|
| Project structure, FormRunner, FieldRenderer, Home | ✅ Done |
| API infra: LocalConfigLoader, DebugLogger, ConnectionChecker | ✅ Done |
| 2 connection strings (Config DB + Data DB) | ✅ Done |
| Fix 3 SQL bugs FormRepository sqlFields | ✅ Done (session 4) |
| NormalizeFieldType (TextBox→text, DateEdit→date,...) | ✅ Done (session 4) |
| DebugMode ?debug=1 — badge + console log | ✅ Done (session 4) |
| Test end-to-end (labels, field values, debug mode) | 🔴 Pending |
| FieldType `select` — LookupBox với GET Sys_Lookup | 🔴 Pending |

## Design System — Khởi động (session 5)

| Component | Status |
|---|---|
| Brand direction chốt: "I Care 24/7", đa ngành, Colorful/Playful | ✅ Done |
| Color system: Coral → Violet → Teal, gradient logo | ✅ Done |
| Typography: Plus Jakarta Sans + Inter | ✅ Done |
| `docs/design-system/tokens.css` — CSS custom properties đầy đủ | ✅ Done |
| `docs/design-system/README.md` — documentation | ✅ Done |
| `.claude/agents/design-agent.md` — custom agent | ✅ Done |
| Module colors assignment | 🔴 Pending |
| Apply tokens vào Blazor components thực tế | 🔴 Pending |

## Next Priorities
1. **Test end-to-end Blazor** — mở `/form/sys_UI_Design?debug=1`, verify labels + field values
2. **Assign module colors** — khi chốt danh sách module → update `tokens.css`
3. Blazor: support FieldType `select` (ComboBox — gọi Sys_Lookup API)
4. MetadataEngine (IMetadataEngine) — backend
5. Integration tests — backend
