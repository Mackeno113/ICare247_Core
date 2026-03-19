# Project Current Phase

> Cập nhật lần cuối: 2026-03-19

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

## ConfigStudio (WPF) — Phase 1 UI

| Screen / Component | Status |
|---|---|
| 11 skeleton screens | ✅ Done |
| Shell + Navigation (ShellViewModel) | ✅ Done |
| FormManagerView — search, filter, pagination | ✅ Done |
| FormDetailView — fields, sections, events, rules, audit log | ✅ Done |
| FormEditDialogView — tạo/sửa form + tab Permissions | ✅ Done |
| SysTableManagerView — quản lý sys_table + lookup | ✅ Done |
| FormEditorView — field editor cơ bản | ✅ Done |
| FieldConfigView/ViewModel — config chi tiết 1 field | 🟡 Todo |
| ValidationRuleEditorView/ViewModel | 🟡 Todo |
| EventEditorView/ViewModel | 🟡 Todo |
| GrammarLibrary, ExpressionBuilderDialog | 🟡 Stub |
| DependencyViewerView | 🟡 Stub |
| I18nManagerView | 🟡 Stub |
| P0: Auto-save, Undo/Redo, Live Linting, Impact Preview | 🔴 Blocked — phụ thuộc Backend engine |

## Blockers
- P0 UX Features (ConfigStudio) phụ thuộc Backend Phase 2+ engine hoạt động
