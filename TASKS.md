# ICare247 Core — Task Tracking

## 🔴 Đang làm (In Progress)

<!-- Không có task đang làm dở -->

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
