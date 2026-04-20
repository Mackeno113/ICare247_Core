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
- ~~Wire Impact Preview vào DependencyViewer UI~~ ✅ Done (verified 2026-03-29)
- ~~Màn hình quản lý Sys_Lookup trong ConfigStudio~~ ✅ Done (verified 2026-03-29)
- ~~ExecuteSaveAsync / ExecuteSaveSectionAsync~~ ✅ Done (verified 2026-03-29)
- ~~Control Props JSON preview sidebar~~ ✅ Done (commit c5027b2, 2026-03-29)
- ~~TreePickerPropsPanel — UI cấu hình TreePicker (tree config + cascading)~~ ✅ Done (session 25-26, Migration 016)
- ~~LookupBoxPropsPanel multi-trigger — ReloadTriggerFields thay ReloadTriggerField~~ ✅ Done (session 25-26)
- Pass `tableCode` khi navigate từ FieldConfig → I18nManager
- Test LookupBox end-to-end (cấu hình GioiTinh + PhongBanID)
- WPF-10: ValidationRuleEditor Compare rule field list dropdown

## Blazor RuntimeCheck — Đang hoàn thiện

| Component | Status |
|---|---|
| Project structure, FormRunner, FieldRenderer, Home | ✅ Done |
| API infra: LocalConfigLoader, DebugLogger, ConnectionChecker | ✅ Done |
| 2 connection strings (Config DB + Data DB) | ✅ Done |
| Fix 3 SQL bugs FormRepository sqlFields | ✅ Done (session 4) |
| NormalizeFieldType (TextBox→text, DateEdit→date,...) | ✅ Done (session 4) |
| DebugMode ?debug=1 — badge + console log | ✅ Done (session 4) |
| DevExpress.Blazor upgrade 24.2 → 25.2.3, CSS blazing-berry theme | ✅ Done (session 15) |
| Design System tokens.css + app.css refactor | ✅ Done (session 14) |
| ControlShowcase.razor — 10 sections test DX controls | ✅ Done (session 14) |
| TextBoxRenderer.razor — DxTextBox full spec (BindValueMode/InputDelay/ClearButton/etc) | ✅ Done (session 16) |
| MemoRenderer.razor — DxMemo, EditorType "TextArea" riêng biệt | ✅ Done (session 16) |
| CheckBoxRenderer.razor — DxCheckBox + ToggleSwitch (IsSwitch), CheckType.Checkbox | ✅ Done (session 16) |
| FieldType `select` — static Sys_Lookup, LookupApiService, batch load | ✅ Done (session 7) |
| FieldType `fklookup` — LookupBoxRenderer (popup grid) | ✅ Done (session 8) |
| FieldType `combobox` — ComboBoxRenderer (dynamic data) | ✅ Done (session 8) |
| FieldType `treepicker` — TreePickerRenderer (dropdown cây, flat→tree, multi-trigger) | ✅ Done (session 25, Migration 016) |
| Multi-trigger cascading — ComboBoxRenderer + LookupBoxRenderer dùng snapshot list | ✅ Done (session 25, Migration 016) |
| NumericBoxRenderer (DxSpinEdit) | 🔴 Pending |
| DatePickerRenderer (DxDateEdit) | 🔴 Pending |
| Test end-to-end FormRunner với API + DB thật | 🔴 Pending |

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

## Phase 10 — Schema Extension: Tab + Lookup (sessions 6-7)

| Component | Status |
|---|---|
| Migration 003-009 | ✅ Chạy trên DB thật rồi |
| Spec 02_DATABASE_SCHEMA.md | ✅ Cập nhật xong |
| Domain entities: TabMetadata, FieldLookupConfig, SectionMetadata.TabId, FieldMetadata.ColSpan/LookupSource | ✅ Done (session 7) |
| Repositories: FormRepository + FieldRepository (Ui_Tab, Col_Span, Lookup, Ui_Field_Lookup) | ✅ Done (session 7) |
| ConfigStudio: FieldDataService transaction + FieldConfigViewModel FK Lookup + ColSpan UI | ✅ Done (session 7) |
| Blazor: LookupApiService + FormRunner + FieldRenderer case "select"/"fklookup" | ✅ Done (session 7) |

## Blazor RuntimeCheck — Status cập nhật

| Component | Status |
|---|---|
| Project structure, FormRunner, FieldRenderer, Home | ✅ Done |
| API infra: LocalConfigLoader, DebugLogger, ConnectionChecker | ✅ Done |
| NormalizeFieldType (TextBox→text, DateEdit→date,...) | ✅ Done |
| DebugMode ?debug=1 — badge + console log | ✅ Done |
| FieldType `select` — static Sys_Lookup, LookupApiService, batch load | ✅ Done (session 7) |
| FieldType `fklookup` — placeholder text input | ✅ Done (session 7) |
| Test end-to-end (labels, field values, debug mode, select options) | 🔴 Pending |

## Next Priorities
1. **MetadataEngine** — implement IMetadataEngine (backend)
2. **Integration tests** — backend
3. **Wire Impact Preview** vào DependencyViewer UI (ConfigStudio)
4. **Test Blazor end-to-end** — form có select field + Sys_Lookup options
5. **Apply Design System tokens** vào Blazor components
6. **Màn hình quản lý Sys_Lookup** trong ConfigStudio
