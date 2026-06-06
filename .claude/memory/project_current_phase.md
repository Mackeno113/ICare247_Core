# Project Current Phase

> Cập nhật lần cuối: 2026-06-06

## Đợt mới nhất — Master Data CRUD full-stack (session 38, 2026-06-06)

Feature metadata-driven CRUD cho màn hình danh mục: Tầng 0 (DB migrations 023+024) + Tầng 1 (backend generic CRUD + soft-check FK) + Tầng 2 (Blazor List/Form/Delete Popup↔Tab) + Tầng 3 (WPF Display_Mode dropdown + Show_In_List checkbox). Build backend 0/0, WPF 0/0.

→ **Migrations 023+024 cần chạy trên DB trước khi run app sau khi pull.**

## Đợt trước — Wave A + D + 017 (session 28, 2026-05-17/18)

| Wave | Status | Mô tả |
|---|---|---|
| Wave A — Navigation UX | ✅ Done | Double-click, Context menu, Shortcuts, KeepAlive, Breadcrumb |
| Wave D — Power editing | ✅ Done | Quick Property Bar, Bulk Editor, Grid-edit Mode (FormEditor) |
| Wave 017 — ADR-017 | ✅ Done | Drop Is_Enabled, add Lock_On_Edit + EffectiveReadOnly Blazor logic |

→ **Migration 017 cần chạy trên DB trước khi run app sau khi pull.**

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
- ~~MetadataEngine implementation~~ ✅ Done (verified 2026-05-31)
- Integration tests (BE-002) ❌ Chưa làm — không có test file nào
- Apply Design System tokens vào Blazor (BE-004) ❌ Chưa làm

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
- ~~Pass `tableCode` khi navigate từ FieldConfig → I18nManager~~ ✅ Done (code đã có từ trước, verified 2026-05-31)
- ~~WPF-10: ValidationRuleEditor Compare rule field list dropdown~~ ✅ Done (commit 044219e + follow-up 2026-05-31)
- Test LookupBox end-to-end (cấu hình GioiTinh + PhongBanID) — **manual test**, cần chạy app + DB thật

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
| NumericBoxRenderer (DxSpinEdit) | ✅ Done (verified 2026-05-31) |
| DatePickerRenderer (DxDateEdit) | ✅ Done (verified 2026-05-31) |
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

## Next Priorities (cập nhật 2026-06-06)

1. **DB-RUN** — Chạy `db/023_ui_form_display_mode.sql` + `db/024_ui_field_show_in_list.sql` trên DB thật
2. **E2E test Master Data** — sau khi chạy migrations, test Blazor `/master/{FormCode}` với DB thật
3. **BE-002 Integration tests** — backend (ValidationEngine + EventEngine + MetadataEngine) ❌ Chưa làm
4. **BE-004 Apply Design System tokens** — vào Blazor components ❌ Chưa làm
5. **WPF-14** — Manual E2E test LookupBox (GioiTinh + PhongBanID) ⏳ Cần DB thật
