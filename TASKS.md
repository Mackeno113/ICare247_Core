# ICare247 Core — Task Tracking

## 🔴 Đang làm (In Progress)

_(Trống — chọn task tiếp theo từ 🟠 Kế hoạch)_

---

## 🟠 Kế hoạch (Next Up)

### Backend — Claude Code

- [ ] **BE-001** — Implement `IMetadataEngine` (orchestration FormRepo + FieldRepo + Cache)
- [ ] **BE-002** — Integration tests: ValidationEngine + EventEngine + MetadataEngine
- [ ] **BE-003** — Test Blazor end-to-end với API + DB thật (form sys_UI_Design)
- [ ] **BE-004** — Apply Design System tokens vào Blazor components

### WPF ConfigStudio — Codex

- [ ] **WPF-13** ⭐ — Pass `tableCode` khi navigate FieldConfig → I18nManager
- [ ] **WPF-10** ⭐ — ValidationRuleEditor: Compare rule field list → ComboBoxEdit (~45 phút)
- [ ] **WPF-11** — FormSummaryDto: thêm EventCount subquery (~30 phút)
- [ ] **WPF-12** — I18n Manager: Export/Import CSV/JSON (~1.5 giờ)
- [ ] **WPF-14** — Test LookupBox end-to-end (GioiTinh + PhongBanID)

---

## ✅ Done (Wave ComboBox/LookupBox System — 2026-03-28)

### Wave — ComboBox/LookupBox System (2026-03-28)

> **Bối cảnh:** Hệ thống có 2 dạng dropdown Blazor hoàn toàn khác nhau: DxComboBox (static/dynamic list) và DxDropDownBox (FK lookup với popup grid + template phức tạp). Cần typed ControlProps models, WPF dedicated panels, và Blazor renderer thật thay placeholder.

#### WAVE 1 — Nền tảng

- [x] **T1** — Domain: `ComboBoxControlProps.cs` + `LookupBoxControlProps.cs` — typed C# models cho Control_Props_Json _(2026-03-28)_
- [x] **T2** — DB Migration `014_ui_field_lookup_add_cols.sql`: thêm `Reload_Trigger_Field`, `EditBox_Mode`, `Code_Field`, `DropDown_Width`, `DropDown_Height` vào `Ui_Field_Lookup` _(2026-03-28)_
- [x] **T3** — `FieldLookupConfig.cs` domain entity + `FieldLookupConfigRecord.cs` WPF DTO + `FieldDataService.cs` SELECT/UPSERT: thêm 5 fields mới từ T2 _(2026-03-28)_

#### WAVE 2 — WPF Config Studio

- [x] **T4** — WPF NEW: `Views/Panels/ControlProps/ComboBoxPropsPanel.xaml` — 3 sections: Data Source + Search + Display _(2026-03-28)_
- [x] **T5** — WPF NEW: `Views/Panels/ControlProps/LookupBoxPropsPanel.xaml` — FK source + EditBox mode + Popup grid + Diễn giải _(2026-03-28)_
- [x] **T6** — `FieldConfigViewModel.cs`: thêm IsLookupOrComboBoxEditor + IsRadioGroupEditor + props ComboBox (SearchMode, SearchFilterCondition, AllowUserInput, NullTextKey, DropDownWidthMode, ClearButton, GroupFieldName, DisabledFieldName) _(2026-03-28)_
- [x] **T7** — `FieldConfigViewModel.cs`: thêm props LookupBox (EditBoxMode, CodeField, DropDownWidth, DropDownHeight, ReloadTriggerField) + SaveAsync/LoadAsync updated _(2026-03-28)_
- [x] **T8** — `FieldConfigView.xaml`: thêm `panels:` namespace, RadioGroup section, LookupBoxPropsPanel thay FK Lookup inline, ComboBoxPropsPanel _(2026-03-28)_

#### WAVE 3 — Blazor Runtime

- [x] **T9** — Blazor NEW: `Services/ILookupQueryService.cs` + `Services/LookupQueryService.cs` — POST /api/v1/lookups/query-dynamic _(2026-03-28)_
- [x] **T10** — Blazor NEW: `Components/FieldRenderers/ComboBoxRenderer.razor` — HTML select với dynamic data + cascade reload _(2026-03-28)_
- [x] **T11** — Blazor NEW: `Components/FieldRenderers/LookupComboBoxRenderer.razor` — DxComboBox static Sys_Lookup, searchMode/allowUserInput/clearButton, thay HTML select _(2026-03-31)_
- [x] **T12** — Blazor NEW: `Components/FieldRenderers/LookupBoxRenderer.razor` — popup grid + 3 EditBox modes + search _(2026-03-28)_
- [x] **T13** — `FieldRenderer.razor` + `FormRunner.razor`: add combobox/fklookup cases + Context param + NormalizeFieldType _(2026-03-28)_

#### WAVE 4 — Backend API

- [x] **T14** — `POST /api/v1/lookups/query-dynamic` trong `LookupController.cs` + `IDynamicLookupRepository` + `DynamicLookupRepository` Dapper + CQRS handler _(2026-03-28)_
- [x] **DB** — Chạy migration `014_ui_field_lookup_add_cols.sql` trên DB thật _(xác nhận 2026-04-25: schema canonical 000_create_schema.sql)_

#### WAVE 5 — Bug Fix: Migration 014 columns bị bỏ quên (2026-03-30)

> **Root cause:** 3 SQL queries trong backend không SELECT các cột mới từ Migration 014 (`EditBox_Mode`, `Code_Field`, `DropDown_Width`, `DropDown_Height`, `Reload_Trigger_Field`).
> `DynamicLookupRepository.BuildSafeSql` chỉ SELECT 2 cột (ValueColumn + DisplayColumn) — popup grid không thấy cột bổ sung.

- [x] **B1** — `FormRepository.cs` `sqlLookupConfigs`: thêm 5 cột Migration 014 _(2026-03-30)_
- [x] **B2** — `FieldRepository.cs` batch load SQL: thêm 5 cột Migration 014 _(2026-03-30)_
- [x] **B3** — `FieldRepository.cs` `LoadLookupConfigAsync` single SQL: thêm 5 cột Migration 014 _(2026-03-30)_
- [x] **B4** — `DynamicLookupRepository.cs`: thêm `Code_Field` vào config SQL + `LookupCfgRow` _(2026-03-30)_
- [x] **B5** — `DynamicLookupRepository.BuildSafeSql`: mở rộng SELECT gồm cột từ `PopupColumnsJson` + `CodeField` — dùng `BuildSelectColumns()` helper _(2026-03-30)_

#### WAVE 6 — Bug Fix: Runtime 500 errors khi load /form/sys_UI_Design (2026-03-30)

> **Root cause 1:** `EventRepository.cs` dùng `uf.Field_Code` — cột không tồn tại trên `Ui_Field`. FieldCode thực ra là `Sys_Column.Column_Code` (join qua `Ui_Field.Column_Id`).
> **Root cause 2:** `DynamicLookupRepository` throw `InvalidOperationException` khi `Source_Name` NULL/empty trong DB — nên return `[]` gracefully thay vì 500.

- [x] **B6** — `EventRepository.cs`: thêm `LEFT JOIN Sys_Column sc ON sc.Column_Id = uf.Column_Id`, đổi `uf.Field_Code` → `sc.Column_Code` ở cả SELECT + WHERE _(2026-03-30)_
- [x] **B7** — `DynamicLookupRepository.cs`: guard `string.IsNullOrWhiteSpace(cfg.SourceName)` → return `[]` (không throw 500) _(2026-03-30)_

#### WAVE 7 — Documentation: Form Runtime Flow (2026-03-30)

- [x] **D1** — `docs/form-runtime-flow.puml` — PlantUML sequence diagram đầy đủ 8 phase (LOAD → RENDER → INTERACT → SUBMIT) _(2026-03-30)_
- [x] **D2** — `docs/form-runtime-flow.txt` — ASCII text version, readable trong bất kỳ editor nào _(2026-03-30)_

---

## ✅ Done — Field Config Schema Fix (2026-03-26)

> **Bối cảnh:** Phân tích lại schema phát hiện inconsistency: `Is_ReadOnly` là cột trong `Ui_Field` nhưng `Is_Required` lại được lưu như `Val_Rule`. Quyết định ADR-010: cả 3 (`Is_Visible`, `Is_ReadOnly`, `Is_Required`) phải là cột tĩnh trong `Ui_Field`.
> Đồng thời bổ sung `Is_Enabled` (disabled ≠ readonly), 2 rule type mới (`Length`, `Compare`), 3 action type mới (`SET_ENABLED`, `CLEAR_VALUE`, `SHOW_MESSAGE`).

### Wave A — Database Migration ✅ (2026-04-25)

- [x] Tạo `db/migrations/010_field_behavior_columns.sql` _(gộp vào 000_create_schema.sql)_
  - `ALTER TABLE Ui_Field ADD Is_Required bit NOT NULL DEFAULT 0`
  - `ALTER TABLE Ui_Field ADD Is_Enabled  bit NOT NULL DEFAULT 1`
- [x] Tạo `db/migrations/011_add_rule_types.sql` _(gộp vào 001_seed_all.sql)_
  - INSERT `Val_Rule_Type`: `Length` + `Compare`
- [x] Tạo `db/migrations/012_add_action_types.sql` _(gộp vào 001_seed_all.sql)_
  - INSERT `Evt_Action_Type`: `SET_ENABLED`, `CLEAR_VALUE`, `SHOW_MESSAGE`
- [x] **Chạy migrations 010–012 trên DB thật** _(xác nhận Wave C commit 707c882)_

### Wave B — Backend ✅ (2026-03-26)

- [x] `FieldMetadata.cs` — thêm `IsRequired`, `IsEnabled`
- [x] `FieldRepository.cs` — thêm `Is_Required`, `Is_Enabled` vào SELECT
- [x] `ValidationEngine.cs` — Length + Compare rules: supported via AST evaluation (len() đã có trong BuiltinFunctions) + unit tests xác nhận (145/145 pass)
- [x] `EventEngine.cs` — thêm handler cho `SET_ENABLED`, `CLEAR_VALUE`, `SHOW_MESSAGE`
- [x] `UiDelta.cs` — thêm comment 3 action types mới
- [x] Required rule: giữ backward compat (deprecated comment), không xóa (ADR-011)
- [x] Resource System: IResourceRepository, ResourceResolver, MetadataEngine, CacheKeys mới
- [x] RuntimeController: dùng IMetadataEngine thay IFormRepository
- [x] Build verify — 0 errors, 0 warnings, 145/145 tests

### Wave C — ConfigStudio WPF ✅ (commit 707c882, 2026-03-26)

- [x] `db/migrations/010`: chạy migration trước Wave C
- [x] `FieldConfigRecord.cs` (Core/Data) — thêm `IsRequired`, `IsEnabled`
- [x] `IFieldDataService.cs` — không thay đổi interface (IsRequired/IsEnabled đã trong record)
- [x] `FieldDataService.cs` — cập nhật SQL SELECT/UPSERT thêm `Is_Required`, `Is_Enabled`
- [x] `FieldConfigViewModel.cs` — thêm `IsEnabled`; xóa `ToggleRequiredRule`
- [x] `FieldConfigView.xaml` — Behavior card 2×2 Grid: IsVisible + IsReadOnly / IsRequired + IsEnabled
- [x] `ValidationRuleEditorViewModel.cs` — xóa `Required`, thêm `Length` + `Compare` (IsCompareType, EditCompareField, EditCompareOp, CompareOpOptions, preview)
- [x] `ValidationRuleEditorView.xaml` — thêm section Compare + preview Border
- [x] `EventEditorViewModel.cs` — thêm `SET_ENABLED`, `CLEAR_VALUE`, `SHOW_MESSAGE`
- [x] Build verify WPF — 0 errors, 0 warnings

### Wave D — Tài liệu ✅ (2026-03-26)

- [x] Tạo `docs/spec/09_FIELD_CONFIG_GUIDE.md` — hướng dẫn end-user đầy đủ (2026-03-26)
- [x] Cập nhật `docs/spec/02_DATABASE_SCHEMA.md` — thêm cột `Is_Required`, `Is_Enabled` vào bảng `Ui_Field`; cập nhật Val_Rule_Type + Evt_Action_Type
- [x] Cập nhật `docs/spec/04_ENGINE_SPEC.md` — thêm rule type `Length`, `Compare`; action type `SET_ENABLED`, `CLEAR_VALUE`, `SHOW_MESSAGE`; fix Val_Rule_Field → Field_Id trực tiếp
- [x] Cập nhật `docs/spec/05_ACTION_RULE_PARAM_SCHEMA.md` — deprecated `Required`; thêm schema đầy đủ `Length`, `Compare`, `SET_ENABLED`, `CLEAR_VALUE`, `SHOW_MESSAGE`

---

## ✅ Done (session 2026-04-17)

### Wave 10 — i18n captionKey + WPF UX fixes (2026-04-17) ✅

> i18n hóa caption cột popup LookupBox, fix 4 bugs WPF ConfigStudio liên quan đến LookupBoxPropsPanel.

- [x] **i18n captionKey** — Đổi `Caption` → `CaptionKey` (i18n resource key) trong `FkColumnConfig`; WPF auto-gen key theo pattern `{table}.col.{snake_case}` khi nhập FieldName via `WireFkColumnHandlers` _(2026-04-17)_
- [x] **RegisterI18nKeysAsync** — Khi lưu field, tự động INSERT key vào `Sys_Resource` với default value = FieldName (user vào I18nManager để đặt bản dịch thật) _(2026-04-17)_
- [x] **MetadataEngine resolve captionKey** — `ResolvePopupColumnCaptionsAsync`: batch load từ `Sys_Resource` qua `GetByKeysAsync`, rewrite JSON với `caption` resolved trước khi cache; Blazor nhận text đã dịch _(2026-04-17)_
- [x] **FieldLookupConfig** — `PopupColumnsJson` đổi `init` → `set` để MetadataEngine mutate sau construction _(2026-04-17)_
- [x] **SpinEdit race condition** — `UpdateSourceTrigger=PropertyChanged` cho Width/DropDownWidth/DropDownHeight → lưu đúng giá trị khi nhấn "Lưu Field" _(2026-04-17)_
- [x] **SysLookupManagerView XamlParseException** — `AutoGenerateColumns="False"` → `"None"` (DX enum) _(2026-04-17)_
- [x] **MainWindow fullscreen che taskbar** — Hook `WM_GETMINMAXINFO` (0x0024) dùng `MonitorFromWindow` + `GetMonitorInfo` giới hạn MaxSize = WorkArea; `DragMove()` chỉ gọi khi `WindowState == Normal` _(2026-04-17)_
- [x] **Popup columns UX** — Thêm nút ▲▼ di chuyển thứ tự cột; nút xóa ✕ đỏ rõ ràng; fix `columns: []` trong JSON preview (gọi `RebuildControlPropsJson()` sau khi load xong từ DB) _(2026-04-17)_

**Quyết định thiết kế:**
- captionKey pattern: `{table_lower}.col.{field_snake_case}` — auto-gen khi nhập FieldName, chỉ overwrite khi còn empty hoặc vẫn theo auto-gen pattern
- `RegisterI18nKeysAsync` dùng `IF NOT EXISTS INSERT` để không overwrite bản dịch user đã nhập
- WM_GETMINMAXINFO hook dùng `MonitorFromWindow(MONITOR_DEFAULTTONEAREST)` để hỗ trợ multi-monitor

---

## ✅ Done (session 2026-04-16)

### Wave 9 — Bug Fix: Blazor Renderer UI bugs (2026-04-16) ✅

> Phân tích cấu hình JSON export, xác định và fix 6 bugs liên quan đến rendering ComboBox/LookupBox/DatePicker.

- [x] **ComboBoxRenderer** — đổi `<select>` HTML → `DxComboBox` với dynamic data (map `DynamicRows` → `List<LookupItem>` typed để DxComboBox bind được) _(2026-04-16)_
- [x] **LookupBoxRenderer** — fix `PopupColDef`: `Column`/`Title` → thêm `[JsonPropertyName("fieldName")]`/`[JsonPropertyName("caption")]` khớp với WPF output _(2026-04-16)_
- [x] **DynamicLookupRepository** — fix `PopupColEntry`: thêm `[JsonPropertyName("fieldName")]` → BUILD SELECT đúng cột popup _(2026-04-16)_
- [x] **LookupBoxRenderer.razor.css** — tạo mới CSS scoped: `position: absolute` cho popup → không còn chiếm layout inline _(2026-04-16)_
- [x] **LookupComboBoxRenderer** — đổi `@bind-Value` từ `ValueField=ItemCode` sang bind `LookupOptionDto?` trực tiếp → fix DX hiển thị item đầu tiên khi null _(2026-04-16)_
- [x] **LookupComboBoxRenderer** — thêm `Task.Delay(50)` trong `HandleLostFocus` → tránh race condition với `ValueChanged` _(2026-04-16)_
- [x] **FormRunner** — race condition fix trong `OnFieldBlur`: snapshot value trước API, bỏ qua kết quả nếu value đã thay đổi _(2026-04-16)_
- [x] **FormRunner** — thêm `ExportConfigJsonAsync()` + button "⬇ Export config JSON" → download `{FormCode}_config.json` _(2026-04-16)_
- [x] **DatePickerRenderer** — thêm `ClearButtonDisplayMode="Auto"` → nút xóa ngày hiển thị đúng _(2026-04-16)_
- [x] **index.html** — thêm `icare.downloadJson()` JS helper cho blob download _(2026-04-16)_

**Quyết định thiết kế:**
- `PopupColumnsJson` key format là `fieldName`/`caption`/`width` (WPF output) — không phải `column`/`title`. Tất cả consumer phải dùng `[JsonPropertyName]`.
- `LookupComboBoxRenderer` bind `LookupOptionDto?` (full object) thay vì `string` (ItemCode via ValueField) — tránh DevExpress null display bug.

---

## ✅ Done (session 2026-04-15)

### Wave 8 — Bug Fix: Repository SQL mismatches + UI bugs (2026-04-15) ✅

> Phân tích 4 bugs giao diện FormRunner từ screenshots, fix toàn bộ SQL column mismatches giữa code vs DB schema.

- [x] Bug fix: `DependencyRepository.cs` dùng `src.Field_Code` / `tgt.Field_Code` không tồn tại → JOIN `Sys_Column sc_src/sc_tgt`, dùng `Column_Code` _(2026-04-15)_
- [x] Bug fix: `FormRepository.sqlFields` thiếu `Is_Required`, `Is_Enabled` → FieldState.IsRequired luôn false → asterisk * không hiện _(2026-04-15)_
- [x] Bug fix: `FormRepository.sqlCloneFields` thiếu `Is_Required`, `Is_Enabled`, `Col_Span`, `Lookup_Source`, `Lookup_Code` → clone form mất config _(2026-04-15)_
- [x] Bug fix: `FieldRepository.GetByFormIdAsync` trả `Label_Key` thô → thêm `langCode` param + JOIN `Sys_Resource` → COALESCE(Resource_Value, Label_Key) _(2026-04-15)_
- [x] Bug fix: `IFieldRepository` + `ValidationEngine` — thêm `langCode` param, truyền đúng vào cả 2 calls _(2026-04-15)_
- [x] Bug fix: `LookupOptionDto.ToString()` override → DxComboBox render đúng label trong dropdown list _(2026-04-15)_
- [x] Bug fix: `StubFieldRepository` trong tests — update signature mới _(2026-04-15)_
- [x] Doc fix: `FieldMetadata.Label` comment sai "đã resolve" → mô tả đúng cả 2 repository _(2026-04-15)_

**Vấn đề còn tồn đọng:**
- `DefaultValueJson` orphan property — `FieldMetadata` có property nhưng DB không có cột. Luôn null. Cần quyết định: thêm migration hoặc xóa.
- `ComboBoxRenderer` dùng native `<select>` — visual inconsistency so với DxComboBox (chưa cần fix ngay).

---

## ✅ Done (session 2026-04-01)

### Wave — FormRunner Renderers (2026-03-29 → 2026-04-01) ✅

> Nâng DX 24.2→25.2.3, xây đầy đủ 6 renderer (TextBox/Memo/CheckBox/NumericBox/DatePicker/LookupComboBox), fix CSS, fix backend SQL bugs.

- [x] Fix DX v25 CSS: theme file `blazing-berry.bs5.min.css` (không phải `blazing-berry.min.css`) _(2026-04-01)_
- [x] NumericBoxRenderer, DatePickerRenderer, LookupComboBoxRenderer _(2026-03-31)_
- [x] TextBoxRenderer, MemoRenderer, CheckBoxRenderer _(2026-03-30)_
- [x] WPF ControlProps schemas sync _(2026-03-30)_
- [x] Bug fix: RuleRepository `Field_Code` → `Sys_Column.Column_Code` _(2026-03-31)_
- [x] Bug fix: DynamicLookupRepository dùng sai DB (Config vs Data) _(2026-03-31)_
- [x] Bug fix: EventRepository `Field_Code` → `Sys_Column.Column_Code` _(2026-03-30)_
- [x] Bug fix: DynamicLookupRepository guard SourceName NULL _(2026-03-30)_

### Wave — ComboBox/LookupBox System (2026-03-28) ✅

> 14 tasks hoàn thành: Domain models, DB migration 014, WPF panels, Blazor renderers, Backend API.

---

## ✅ Done (session 2026-03-25)

### Phase 10 — Schema Extension: Tab + Lookup (2026-03-25)

> Thảo luận thiết kế + viết migration SQL + cập nhật spec

- [x] Thiết kế multi-tab form: Ui_Form → Ui_Tab → Ui_Section → Ui_Field
- [x] `005_add_ui_tab.sql` — bảng Ui_Tab (Tab_Code unique, filtered index Is_Default)
- [x] `006_alter_ui_section_add_tab.sql` — thêm Tab_Id nullable FK vào Ui_Section
- [x] `007_alter_ui_field_add_cols.sql` — thêm Col_Span (tinyint 1-3), Lookup_Source, Lookup_Code + 3 constraints
- [x] `008_add_ui_field_lookup.sql` — bảng Ui_Field_Lookup (1-1 với Ui_Field, Query_Mode/Source/Filter/Popup_Columns_Json)
- [x] `009_fix_sys_lookup_tenant.sql` — fix Sys_Lookup.Tenant_Id: DEFAULT 0 → NULL, rebuild filtered indexes trong transaction
- [x] Cập nhật `docs/spec/02_DATABASE_SCHEMA.md` — toàn bộ schema mới + sơ đồ quan hệ + thống kê 33 bảng

**Việc tiếp theo (máy khác):**
- [x] Chạy migrations 003→009 trên DB thật (theo thứ tự) — done trước session này
- [x] Cập nhật Domain entities: SectionMetadata (+ TabId), FieldMetadata (+ ColSpan, LookupSource, LookupCode), TabMetadata, FieldLookupConfig
- [x] Cập nhật Repositories: FormRepository, FieldRepository (query Ui_Tab, Col_Span, Lookup_Source, Lookup_Code, Ui_Field_Lookup)
- [x] Cập nhật ConfigStudio: IFieldDataService + FieldDataService + FieldConfigViewModel + FieldConfigView (ColSpan radio, FK Lookup config, transaction save)
- [x] Blazor: FieldType `select` (static Sys_Lookup) + `fklookup` placeholder — LookupApiService, FormRunner, FieldRenderer

### Design System — Brand & UI Foundation

- [x] Thảo luận chốt brand direction: "I Care 24/7" — ấm áp, tận tâm, đa ngành
- [x] Chọn color strategy: Multi-color 3 màu (Coral → Violet → Teal), gradient logo
- [x] Chọn typography: Plus Jakarta Sans (heading) + Inter (body)
- [x] Generate `docs/design-system/tokens.css` — toàn bộ CSS custom properties
- [x] Generate `docs/design-system/README.md` — documentation Design System
- [x] Tạo `.claude/agents/design-agent.md` — custom agent cho thiết kế UI
- [x] Tạo `.claude/commands/design.md` — slash command `/design`

---

## 🟡 Cần làm (Todo)

### Phase 1 — Foundation

- [x] Tạo Domain entities (FormMetadata, FieldMetadata, SectionMetadata, RuleMetadata)
- [x] Tạo Domain AST nodes (IExpressionNode, LiteralNode, IdentifierNode, BinaryNode, ...)
- [x] Tạo Domain Engine interfaces (IAstEngine, IValidationEngine, IEventEngine, IMetadataEngine)
- [x] Tạo EvaluationContext value object
- [x] Tạo Application interfaces (IFormRepository, IFieldRepository, IDbConnectionFactory, ICacheService)
- [x] Tạo CacheKeys.cs
- [x] Tạo GetFormByCodeQuery + Handler (skeleton)
- [x] Implement Infrastructure: SqlConnectionFactory
- [x] Implement Infrastructure: FormRepository (Dapper)
- [x] Implement Infrastructure: FieldRepository (Dapper)
- [x] Implement Infrastructure: HybridCacheService (Memory + Redis)
- [x] Tạo FormController (skeleton)
- [x] Tạo ExceptionHandlingMiddleware

### Phase 2 — Grammar V1 / AST Engine

- [x] Implement AstParser (Expression_Json → IExpressionNode)
- [x] Implement AstCompiler (IExpressionNode → Func<context, object?>)
- [x] Implement FunctionRegistry + BuiltinFunctions (len, trim, iif, toDate, today, ...)
- [x] Unit tests cho AstEngine (125 tests)

### Phase 3 — Validation Engine

- [x] Implement ValidationEngine (validate field/form, Required/Custom rules, condition check)
- [x] Implement IRuleRepository + RuleRepository (Dapper, Val_Rule + Val_Rule_Field)
- [x] Implement IDependencyRepository + DependencyRepository (Dapper, Sys_Dependency topological sort)
- [x] Unit tests cho ValidationEngine (16 tests)

### Phase 4 — Event Engine

- [x] Tạo Domain entities (EventDefinition, EventAction) — Entities/Event/
- [x] Tạo IEventRepository interface — Application/Interfaces/
- [x] Implement EventEngine (handle event → evaluate condition → execute actions → UiDelta)
- [x] 6 action handlers: SET_VALUE, SET_VISIBLE, SET_REQUIRED, SET_READONLY, RELOAD_OPTIONS, TRIGGER_VALIDATION
- [x] DI registration (scoped) trong DependencyInjection.cs
- [x] Build verify — 0 errors, 0 warnings

### Phase 5 — API + Infrastructure bổ sung

- [x] TenantMiddleware — extract X-Tenant-Id → ITenantContext (scoped)
- [x] CorrelationMiddleware — extract/generate X-Correlation-Id → Serilog LogContext + response header
- [x] JWT Auth — SymmetricSecurityKey, TokenValidationParameters từ appsettings.json
- [x] OpenTelemetry TracerProvider — ASP.NET Core + HttpClient instrumentation
- [x] Scalar UI (đã có sẵn) + Health Check endpoint /health
- [x] appsettings.json + appsettings.Development.json — JWT, Serilog config
- [x] Database seed script — db/001_seed_lookup_data.sql (triggers, actions, functions)
- [x] ValidationBehavior — MediatR pipeline auto-validate trước handler
- [x] EventRepository — Dapper multi-mapping (Evt_Definition + Evt_Action)
- [x] RuntimeController — POST validate-field, validate, handle-event
- [x] Build verify — 0 errors, 0 warnings

### Phase 6 — Form Management CRUD (Backend API)

> Dựa trên phân tích DB + wireframe Ui_Form (2026-03-18)

**Application Layer — Queries**
- [x] `GetFormsListQuery` + Handler — phân trang, filter theo Platform/Table_Id/Is_Active, search theo Form_Code
- [x] `GetFormByIdQuery` + Handler — lấy form metadata + related counts (dùng chung GetByCodeAsync)
- [x] `GetFormAuditLogQuery` + Handler — lấy từ Sys_Audit_Log WHERE Object_Type='Form'

**Application Layer — Commands**
- [x] `CreateFormCommand` + Handler — validate Form_Code unique, set Version=1, Checksum, insert Sys_Audit_Log
- [x] `UpdateFormCommand` + Handler — Version++, recalc Checksum, invalidate cache + Sys_Audit_Log
- [x] `DeactivateFormCommand` + Handler — set Is_Active=0, invalidate cache, insert Sys_Audit_Log
- [x] `RestoreFormCommand` + Handler — set Is_Active=1, insert Sys_Audit_Log
- [x] `CloneFormCommand` + Handler — sao chép Form + Sections + Fields sang Form_Code mới

**Infrastructure Layer**
- [x] `FormRepository.GetListAsync` — Dapper query với paging, multi-filter, ORDER BY Form_Code
- [x] `FormRepository.GetByIdAsync` — JOIN Sys_Table để lấy Table_Name, Tenant context
- [x] `FormRepository.CreateAsync` — INSERT Ui_Form, trả về Form_Id mới
- [x] `FormRepository.UpdateAsync` — UPDATE Ui_Form + Version++ + Checksum
- [x] `FormRepository.SetActiveAsync` — UPDATE Is_Active (dùng chung cho Deactivate + Restore)
- [x] `FormRepository.ExistsCodeAsync` — CHECK Form_Code unique (dùng trong validation)
- [x] `FormRepository.CloneAsync` — transaction: copy Form → Sections → Fields → Events
- [x] `AuditLogRepository` — INSERT + GetByObject (Dapper, phân trang)

**API Layer**
- [x] `GET  /api/v1/config/forms` — list + filter + paging
- [x] `GET  /api/v1/config/forms/{code}` — detail by Form_Code
- [x] `POST /api/v1/config/forms` — tạo form mới
- [x] `PUT  /api/v1/config/forms/{code}` — cập nhật form
- [x] `POST /api/v1/config/forms/{code}/deactivate` — vô hiệu hóa
- [x] `POST /api/v1/config/forms/{code}/restore` — khôi phục
- [x] `POST /api/v1/config/forms/{code}/clone` — nhân bản
- [x] `GET  /api/v1/config/forms/{code}/audit` — audit log

### Phase 7 — Form Management CRUD (ConfigStudio WPF)

> Màn hình quản lý Ui_Form theo wireframe đã thiết kế (2026-03-18)
> Stack: Prism 9 + MaterialDesign + MVVM — đặt trong module Forms

**Screen 02 nâng cấp — FormManagerView (List + Filter)**
- [x] `FormManagerViewModel` — thêm ObservableCollection, filter properties (Platform, TableId, SearchText), paging support
- [x] Filter toolbar: ComboBox Platform (`Tất cả/web/mobile/wpf`), ComboBox Table (load mock/API), SearchBox debounce
- [x] DataGrid nâng cấp: thêm cột Version, Is_Active (toggle chip), row style mờ khi inactive
- [x] Actions per row: Edit, Clone, Preview, Deactivate/Restore (ẩn/hiện theo Is_Active)
- [x] Summary bar footer: tổng / active / inactive count
- [x] Navigate sang FormDetailView khi click vào Form_Code link

**Screen mới — FormDetailView (readonly)**
- [x] `FormDetailView.xaml` + `FormDetailViewModel` — Prism navigation, nhận param `FormCode`
- [x] Header metadata: Form_Code, Platform, Table, Version, Is_Active chip, Checksum, Updated
- [x] TabControl: Sections (count) | Fields (count) | Events (count) | Rules (count) | Audit Log
- [x] Sub-DataGrid cho từng tab (readonly, không edit inline)
- [x] Tab Audit Log: DataGrid Thời gian / Action / User / Correlation_Id

**Screen 02 nâng cấp — FormEditDialog (Create/Edit)**
- [x] `FormEditDialogView.xaml` + `FormEditDialogViewModel` — IDialogAware (Prism Dialog)
- [x] Tab 1 — Thông tin cơ bản: TextBox Form_Code (regex validate `^[A-Z0-9_]+$`, unique check), ComboBox Platform, ComboBox Table, ComboBox Layout_Engine, TextBox Description, ToggleButton Is_Active, readonly Version/Checksum
- [x] Tab 2 — Sections & Fields: ListBox sections trái, DataGrid fields phải (navigate sang FieldConfig)
- [x] Tab 3 — Events: DataGrid (Trigger, Field_Target, Condition snippet, Actions count), navigate sang EventEditor
- [x] Tab 4 — Permissions: DataGrid roles × CheckBox (Can_Read, Can_Write, Can_Submit)
- [x] ValidationSummary TextBlock hiện lỗi khi submit
- [x] Dirty check: hiện ConfirmDialog khi đóng mà chưa lưu

**Dialogs**
- [x] `DeactivateFormDialog.xaml` + VM — Dialog confirm: tên form, impact (sections/fields/events), nút Xác nhận/Hủy
- [x] `CloneFormDialog.xaml` + VM — TextBox Form_Code mới (regex + unique validate realtime), nút Clone/Hủy

### ConfigStudio.WPF.UI — Skeleton Screens

- [x] Screen 01: Shell + Navigation
- [x] Screen 02: Form Manager
- [x] Screen 03: Form Editor
- [x] Screen 04: Field Config (4 tabs: Basic, Control Props, Validation Rules, Events)
- [x] Screen 05: Validation Rule Editor (skeleton)
- [x] Screen 06: Event Editor (skeleton)
- [x] Screen 07: Expression Builder Dialog (5 models, 4 AST services, IDialogAware)
- [x] Screen 08: Dependency Viewer (Canvas graph, auto-layout, filter, detail panel)
- [x] Screen 09: Grammar Library (skeleton)
- [x] Screen 10: i18n Manager (skeleton)
- [x] Screen 11: Publish Checklist (11 check items, async run, jump-to navigation)

### ConfigStudio.WPF.UI — Triển khai UI thật (thứ tự ưu tiên)

- [x] Screen 03: Form Editor — TreeView sections/fields, toolbar, property panel, navigate FieldConfig
- [x] Screen 02: Form Manager — DataGrid danh sách form, search/filter, Add/Edit/Delete, navigate FormEditor
- [x] Screen 05: Validation Rule Editor — DataGrid rules, add/edit/delete, link Expression Builder
- [x] Screen 06: Event Editor — DataGrid events, trigger config, action config
- [x] Screen 09: Grammar Library — 2 tab Functions/Operators, DataGrid whitelist, add/edit
- [x] Screen 10: i18n Manager — DataGrid key/language matrix, filter, import/export

### ConfigStudio.WPF.UI — Direct DB (Hướng B: Dapper trực tiếp, không qua API)

> WPF admin tool kết nối SQL Server trực tiếp. Backend API giữ nguyên cho Blazor runtime sau.

**Wave 1 — Foundation + Standalone modules**
- [x] Tạo 6 service interfaces (IFormDetailDataService, IFieldDataService, IRuleDataService, IEventDataService, IGrammarDataService, II18nDataService)
- [x] Tạo 15 record DTOs (Core/Data/)
- [x] Tạo 6 service implementations (Infrastructure/) — Dapper
- [x] Register DI trong App.xaml.cs
- [x] Migrate GrammarLibraryViewModel → IGrammarDataService
- [x] Migrate I18nManagerViewModel → II18nDataService
- [x] Build verify Wave 1

**Wave 2 — FormDetail (read-only)**
- [x] Migrate FormDetailViewModel → IFormDetailDataService (header, sections, fields, events, rules, audit)
- [x] Build verify Wave 2

**Wave 3 — FieldConfig**
- [x] Migrate FieldConfigViewModel → IFieldDataService + II18nDataService + IRuleDataService + IEventDataService
- [x] Build verify Wave 3

**Wave 4 — Rule + Event editors**
- [x] Migrate ValidationRuleEditorViewModel → IRuleDataService
- [x] Migrate EventEditorViewModel → IEventDataService
- [x] Build verify Wave 4

### ConfigStudio.WPF.UI — P0 UX Features (sau khi Direct DB hoàn thành)

- [x] Auto-save — AutoSaveService: debounce 3s, status indicator (Idle/Pending/Saving/Saved/Error)
- [x] Undo/Redo — UndoRedoService: stack-based (max 50), JSON snapshot, Ctrl+Z/Y commands
- [x] Live Linting — LintingService: debounce 500ms, 8 lint rules (naming, duplicate, required, structure)
- [x] Impact Preview — ImpactPreviewService: scan rules/events cho field references, expression analysis
- [x] FormEditorViewModel integrated: all 4 services wired in OnNavigatedTo/OnNavigatedFrom
- [x] Build verify — 0 errors, 0 warnings (backend + WPF 7 projects)

### Phase 8 — Auto-generate Fields từ DB Schema (ConfigStudio WPF)

> Phân tích: 2026-03-21 | Mục tiêu: giảm thời gian cấu hình form bằng cách đọc cấu trúc cột DB thực sự và tự động sinh fields

**Bước 1 — Target DB Connection trong SettingsView** ✅ Done (2026-03-21)
- [x] Thêm `TargetServer`, `TargetDatabase`, `TargetUserId`, `TargetPassword`, `TargetTrustServerCertificate` vào `AppConfig` model + persist file
- [x] Thêm card "Target Database" vào `SettingsView.xaml` (giống card Config DB hiện tại)
- [x] Thêm properties tương ứng + `TestTargetConnectionCommand` vào `SettingsViewModel`
- [x] Thêm `TargetConnectionString` property (computed) vào `IAppConfigService`

**Bước 2 — Schema Inspector Service** ✅ Done (2026-03-21)
- [x] Tạo interface `ISchemaInspectorService` — `GetColumnsAsync` + `GetTableNamesAsync`
- [x] Tạo `ColumnSchemaDto` — đầy đủ: ColumnName, DataType, NetType, IsNullable, IsIdentity, IsPrimaryKey, OrdinalPosition, MaxLength, Precision, Scale + computed ShouldSkip, DisplayLabel
- [x] Implement `SchemaInspectorService` — query `INFORMATION_SCHEMA.COLUMNS + KEY_COLUMN_USAGE + COLUMNPROPERTY(IsIdentity)`
- [x] Tạo `DataTypeMapper` (Core/Helpers) — map 25 SQL types → NetType + EditorType
- [x] Register DI trong `App.xaml.cs`

**Bước 3 — Auto-generate Fields trong FormEditorView** ✅ Done (2026-03-21)
- [x] Thêm `AutoGenerateColumnItem` (BindableBase), `SectionOptionItem` (record)
- [x] Tạo `AutoGenerateFieldsDialogViewModel` (IDialogAware) — inject ISchemaInspectorService + IAppConfigService, load columns async, SelectAll/DeselectAll, trả OK + selectedColumns + targetSectionCode
- [x] Tạo `AutoGenerateFieldsDialog.xaml` — header stats, column list CheckBox, EditorType badge, section ComboBox, footer buttons
- [x] `FormEditorViewModel` — inject ISchemaInspectorService + IDialogService, thêm `AutoGenerateFieldsCommand`, `ExecuteAutoGenerateFieldsAsync` (mở dialog → add FormTreeNode → IsDirty)
- [x] `FormEditorView.xaml` — thêm button "✨ Tạo Fields tự động" trong toolbar
- [x] `FormsModule.cs` — register `AutoGenerateFieldsDialog`
- [x] `ViewNames.cs` — thêm constant `AutoGenerateFieldsDialog`
- [x] Build verify — 0 errors, 0 warnings

**Bước 4 — Schema Sync / Fallback khi DB thay đổi** ✅ Done (2026-03-21)
- [x] Tạo `SchemaDiffResult` + `TypeMismatchItem` record + `OrphanedFieldItem` (BindableBase)

**Bước 5 — Section Properties Panel: TitleKey + Sys_Resource inline** ✅ Done (2026-03-21)
- [x] Thêm `TitleKey`, `ResourceVi`, `ResourceEn` vào `FormTreeNode` — lưu i18n data trực tiếp trên node
- [x] Tạo `SectionUpsertRequest` record (Core.Data) — upsert Ui_Section + detect TitleKey rename
- [x] Thêm `UpsertSectionAsync` vào `IFormDetailDataService` + implement trong `FormDetailDataService` — transaction rename Resource_Key + INSERT/UPDATE Ui_Section
- [x] `FormEditorViewModel` — inject `II18nDataService`, thêm `SectionTitleKeyPreview` (computed realtime), `SectionCodeError`, `ValidateSectionCode` ([a-z0-9_]), `LoadSectionResourcesAsync`, `ExecuteSaveSectionAsync`, `SaveSectionCommand`, `CancelSectionCommand`
- [x] `FormEditorView.xaml` — redesign Section Properties: Section Code + validation error, Title Key readonly auto-gen, Thứ tự + Is Active row, TÊN HIỂN THỊ card (ResourceVi + ResourceEn), nút Lưu Section + Hủy
- [x] Build verify — 0 errors, 0 warnings
- [x] Tạo `SyncSchemaDialogViewModel` (IDialogAware) — 3 tab, SelectAll/DeselectAll, ApplyCommand trả columnsToAdd + fieldsToRemove + targetSectionCode
- [x] Tạo `SyncSchemaDialog.xaml` — 3 tab: "Có thể thêm" / "⚠ Cảnh báo" / "Type Mismatch" + section picker + footer
- [x] `FormEditorViewModel` — `CheckSchemaDiffAsync` (auto-run sau LoadFromDatabase, 15s timeout, fail-silent), `SyncSchemaCommand`, `SchemaSyncBadgeCount`, `HasSchemaSyncIssues`
- [x] `FormEditorView.xaml` — nút "🔄 Đồng bộ Schema" + badge cam trong toolbar
- [x] `ViewNames.cs` + `FormsModule.cs` — register `SyncSchemaDialog`
- [x] Build verify — 0 errors, 0 warnings

### Phase 9 — Bug fixes + Refactor Val_Rule (2026-03-22)

**Bugs đã fix:**
- [x] `SectionTitleKeyPreview` dùng `TableCode` thay `FormCode` (lowercase)
- [x] `ExistsFormCodeAsync` báo trùng sai khi edit — thêm `excludeFormId` loại chính nó
- [x] Auto-generate fields: FK violation `ColumnId=0` → thêm `EnsureColumnExistsAsync`
- [x] Auto-generate fields: section chưa persist trước khi insert fields
- [x] Auto-generate fields: `DisplayName` hiển thị tên cột thô → PascalCase split
- [x] Field summary panel cho sửa nhầm → set `IsReadOnly=True` + `Mode=OneWay`
- [x] Field `DisplayName` trong TreeView load từ `Sys_Resource` (ngôn ngữ vi)
- [x] Nút back từ `FieldConfig` restore đúng field đang chọn trên `FormEditor`
- [x] Bug "MaNhanVien → SoLuong": `catch` nuốt lỗi → fallback mock sai — tách catch, show error banner

**i18n Manager nâng cấp:**
- [x] Thêm nút Back (GoBackCommand qua IRegionNavigationJournal)
- [x] Thêm bộ lọc Table/Form động
- [x] Cho sửa inline trên lưới (`AllowEditing=True`, `CellValueChanged`)
- [x] Auto-save khi commit cell qua `SaveCellCommand`

**Validation Rules Editor redesign:**
- [x] Breadcrumb `← Cấu hình Field › Section › FieldCode`
- [x] Grid: badge style RuleType, color Severity, ẩn Expression khi Required
- [x] Auto-generate `ErrorKey` theo pattern `{table}.val.{column}.{ruletype}` (readonly)
- [x] Auto-init `Sys_Resource` vi/en khi save rule (IF NOT EXISTS)
- [x] Xác nhận trước khi xóa rule — default No

**Refactor Val_Rule — bỏ bảng junction Val_Rule_Field:**
- [x] Tạo `docs/migrations/003_remove_val_rule_field.sql` — migration trong transaction
- [x] `Val_Rule` thêm `Field_Id` (FK→Ui_Field), `Severity`, `Order_No` trực tiếp
- [x] Cập nhật `RuleDataService` — query thẳng Val_Rule, không JOIN junction
- [x] Cập nhật `RuleRepository` (backend) — bỏ JOIN Val_Rule_Field
- [x] Fix tất cả 5 chỗ còn reference `Val_Rule_Field` trong SQL: `FormDetailDataService`, `PublishCheckService`
- [x] Thêm `InitResourceIfMissingAsync` vào `II18nDataService` + implement
- [x] Build verify — 0 errors, 0 warnings (backend + frontend)

> ⚠️ **Chưa thực hiện**: Chạy `003_remove_val_rule_field.sql` trên DB thật

---

## ✅ Đã xong (Done)

- [x] Setup git repo
- [x] Tạo AI agent configuration files (CLAUDE.md, AGENTS.md)
- [x] Tạo solution structure (4 projects: Domain, Application, Infrastructure, Api)
- [x] Cài NuGet packages (Dapper, MediatR, FluentValidation, Serilog, Redis, JWT, Scalar)
- [x] Sửa ICare247.Api.csproj — thêm JWT Bearer, Serilog, Scalar
- [x] Viết lại Program.cs — Serilog bootstrap + AddApplication/AddInfrastructure + middleware pipeline
- [x] Tạo Application/DependencyInjection.cs — MediatR + FluentValidation auto-scan
- [x] Tạo Infrastructure/DependencyInjection.cs — skeleton với TODO placeholders
- [x] Tạo docs/spec/ (00 → 08) — 9 spec files đầy đủ
- [x] ConfigStudio Screen 04 — FieldConfig: Models (5 DTO), ViewModel, View (4 tabs), đăng ký navigation
- [x] ConfigStudio Screen 07 — ExpressionBuilderDialog: 5 models, 4 AST services, ViewModel IDialogAware, 3-column XAML
- [x] ConfigStudio Screen 08 — DependencyViewer: 2 models, ViewModel (graph + auto-layout), Canvas-based XAML
- [x] ConfigStudio Screen 11 — PublishChecklist: ChecklistItem model, ViewModel (async checks), XAML checklist
- [x] **Toàn bộ 11 screens ConfigStudio.WPF.UI đã có skeleton** — sẵn sàng P0 UX features
- [x] **Toàn bộ 6 placeholder screens đã có UI thật** — DataGrid, TreeView, mock data, filter, CRUD, navigation

---

## 🐛 Bugs / Issues

<!-- Ghi lại bug phát sinh trong quá trình code -->

---

## 📝 Decisions Log

| Ngày       | Quyết định                                                                 | Lý do                                                     |
| ---------- | -------------------------------------------------------------------------- | --------------------------------------------------------- |
| 2026-03-03 | Api.csproj giữ reference đến Infrastructure                                | Program.cs cần gọi AddInfrastructure() — chấp nhận exception này cho composition root |
| 2026-03-03 | Dùng Scalar thay vì Swagger UI                                             | Scalar hiện đại hơn, tích hợp tốt với .NET 9 OpenAPI     |
| 2026-03-03 | Docs spec đặt trong docs/spec/ (không phải docs/ trực tiếp)               | docs/ đã có files AI config, tách biệt để rõ ràng hơn    |
| 2026-03-03 | Forms module thêm PackageReference MaterialDesignThemes                    | Module dùng MaterialDesign XAML cần reference trực tiếp để XAML compiler nhận attached properties |
| 2026-03-03 | Prism 9 dùng `Prism.Navigation.Regions` thay `Prism.Regions`              | Breaking change trong Prism 9.x — áp dụng cho tất cả module cần IRegionManager |
| 2026-03-03 | Screen 08 (DependencyViewer) đặt trong Grammar module                     | Gần với AST services, tránh tạo thêm module mới cho 1 screen                   |
| 2026-03-03 | Screen 11 (PublishChecklist) đặt trong Forms module                        | Launch từ FormEditor, là phần cuối của form lifecycle                           |
| 2026-03-04 | Prism 9 dùng `Prism.Dialogs` thay `Prism.Services.Dialogs`                | Breaking change trong Prism 9.x — áp dụng cho Rules, Events modules            |
| 2026-03-04 | Rules, Events, I18n modules thêm PackageReference MaterialDesignThemes     | Giống Forms module — cần reference trực tiếp cho XAML attached properties       |
| 2026-03-04 | InverseBoolToVisConverter đặt trong Forms module (không phải Core)          | Core project dùng net9.0 (không net9.0-windows) — không có WPF types           |
| 2026-03-18 | Form delete = soft delete (Is_Active=0), không xóa vật lý                  | Child records (Section, Field, Event) phải giữ lại cho audit/rollback           |
| 2026-03-18 | Form_Code là natural key trong toàn bộ API — không dùng Form_Id             | API contract dùng Form_Code, cache key cũng dùng Form_Code                     |
| 2026-03-18 | Chưa có cột Status (Draft/Published) trong Ui_Form schema                   | Nếu cần workflow phức tạp hơn, phải thêm cột Status vào schema trước           |
| 2026-03-21 | TitleKey = `{form_code_lower}.section.{section_code_lower}` — ghép auto    | Convention rõ ràng, không hardcode, dễ trace; user chỉ nhập phần section_code  |
| 2026-03-21 | Section Code enforce lowercase [a-z0-9_] tại ViewModel layer                | Tránh TitleKey bị mixed-case; enforce ngay khi gõ không cần converter XAML     |
| 2026-03-21 | UpsertSectionAsync dùng transaction: rename Resource_Key trước, sau đó UPDATE Section | Đảm bảo atomic — nếu rename fail thì Section không update, tránh dangling key  |
| 2026-03-22 | Bỏ bảng junction `Val_Rule_Field` — Field_Id gộp trực tiếp vào `Val_Rule`   | ErrorKey pattern `{table}.val.{column}.{type}` đã unique per field → quan hệ thực tế là 1-N, không cần N-N |
| 2026-03-22 | ErrorKey pattern: `{table}.val.{column}.{ruletype}` — auto-generate, readonly | Enforces naming convention, không để người dùng nhập tùy ý, tránh trùng lặp   |
| 2026-03-22 | `InitResourceIfMissingAsync` — IF NOT EXISTS INSERT cho Sys_Resource          | Khi save rule tự động tạo bản dịch mặc định nhưng không overwrite bản dịch người dùng đã nhập |
| 2026-03-22 | Mọi thao tác xóa dữ liệu DB phải confirm dialog, default = No                | Xóa không thể hoàn tác — user cần cơ hội từ chối nếu nhấn nhầm               |
| 2026-03-22 | `LoadFromDatabaseAsync` tách catch: lỗi bước chính → error banner, KHÔNG fallback mock | Fallback mock với data sai (SoLuong) gây hiểu nhầm nghiêm trọng — lỗi phải hiện rõ |
| 2026-03-25 | Multi-tab form dùng bảng Ui_Tab riêng (Option A), 0-1 tab = render phẳng như cũ | Rõ ràng, backward compat, không breaking change với form cũ |
| 2026-03-25 | Col_Span là column riêng trên Ui_Field (tinyint 1-3), không để trong Control_Props_Json | FormRunner cần đọc trực tiếp để build CSS grid — layout/structure phải là column riêng |
| 2026-03-25 | Ui_Field.Lookup_Source phân biệt 'static' (Sys_Lookup) / 'dynamic' (Ui_Field_Lookup) / NULL | Type-safe, integrity rõ ràng, không phụ thuộc vào JSON string parsing |
| 2026-03-25 | Ui_Field_Lookup bảng riêng 1-1, Popup_Columns_Json dùng JSON array | Load tách biệt chỉ khi cần, popup columns ít thay đổi + không cần query riêng từng cột |
| 2026-03-25 | Sys_Lookup.Tenant_Id đổi DEFAULT 0 → NULL = global | Nhất quán với toàn hệ thống (Sys_Table, Sys_Config, Sys_Role đều dùng NULL = global) |
| 2026-03-25 | FK lookup config deprecated JSON → Ui_Field_Lookup table (Option B) | JSON cũ không có integrity; Ui_Field_Lookup có FK constraint, query trực tiếp, transaction-safe |
| 2026-03-25 | SaveFieldAsync dùng transaction: UPSERT Ui_Field + Ui_Field_Lookup atomically | Nếu save field thành công nhưng lookup config fail → data inconsistent; transaction bắt buộc |
| 2026-03-25 | FieldRenderer `case "select"`: fallback text input khi Options.Count == 0 | Options chưa load hoặc LookupCode không tồn tại — graceful degradation, không crash |
| 2026-03-25 | `fklookup` tách biệt với `select` trong NormalizeFieldType | FK dynamic lookup cần UI khác (popup search), chưa implement; placeholder text input tránh nhầm lẫn với static select |
