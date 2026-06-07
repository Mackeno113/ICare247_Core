# Architecture Decision Records (ADR)

## ADR-001: Api.csproj reference Infrastructure (2026-03-03)
- **Context:** Program.cs cần gọi AddInfrastructure()
- **Decision:** Chấp nhận reference trực tiếp cho composition root
- **Rule:** Controllers KHÔNG được new class Infrastructure — chỉ Program.cs

## ADR-002: Scalar thay Swagger UI (2026-03-03)
- **Decision:** Dùng Scalar cho API docs
- **Reason:** Hiện đại hơn, tích hợp tốt .NET 9 OpenAPI

## ADR-003: Prism 9 namespace changes (2026-03-03, 2026-03-04)
- `Prism.Navigation.Regions` thay `Prism.Regions`
- `Prism.Dialogs` thay `Prism.Services.Dialogs`
- **Áp dụng:** Tất cả WPF modules

## ADR-004: MaterialDesignThemes reference trực tiếp (2026-03-03)
- Module dùng MaterialDesign XAML cần reference trực tiếp
- XAML compiler cần để nhận attached properties

## ADR-005: InverseBoolToVisConverter chuyển vào Core (2026-03-26, cập nhật từ 2026-03-04)
- **Cũ:** Converter nằm riêng ở Forms và Grammar module (duplicate)
- **Mới:** Gộp vào `ConfigStudio.WPF.UI.Core/Converters/` — Core đã đổi sang `net9.0-windows` + `<UseWPF>true</UseWPF>`
- Forms.Converters và Grammar.Converters đã xóa (D status trong git)
- FieldConfigView.xaml dùng 2 namespace: `conv:` (Forms — ColSpanConverter) + `coreconv:` (Core — InverseBoolToVisConverter)

## ADR-006: DependencyViewer trong Grammar module (2026-03-03)
- Gần với AST services, tránh tạo thêm module mới cho 1 screen

## ADR-008: FieldType trong DB là PascalCase enum, Blazor normalize về lowercase (2026-03-23)
- **Context:** DB lưu `Editor_Type` = 'TextBox', 'DateEdit', 'NumberEdit',... nhưng `FieldRenderer` dùng switch lowercase
- **Decision:** `NormalizeFieldType()` trong `FormRunner.razor` làm bridge: DB enum → renderer lowercase
- **Mapping:** TextBox→text, TextArea→textarea, NumberEdit→number, DateEdit→date, DateTimeEdit→datetime, CheckBox→bool, ComboBox/LookupEdit→select
- **Không đổi DB** — giữ PascalCase vì WPF ConfigStudio cũng dùng cùng giá trị

## ADR-009: GetByCodeAsync thêm langCode parameter (2026-03-23)
- **Context:** Label_Key cần resolve qua Sys_Resource theo ngôn ngữ
- **Decision:** `IFormRepository.GetByCodeAsync` thêm `langCode = "vi"` (optional, default "vi")
- **Caller pattern:** Các handler không cần label chỉ cần thêm `ct:` named arg
- **Query chain:** FormController → GetFormByCodeQuery.LangCode → QueryHandler → Repository → SQL @LangCode

## ADR-010 (revised by ADR-017): Is_Required là cột trong Ui_Field (2026-03-26)
- **Context:** `Is_ReadOnly` đã là cột `bit` trong `Ui_Field`. `Is_Required` lại được lưu như `Val_Rule` với `Rule_Type_Code='Required'` — không nhất quán.
- **Decision (original 2026-03-26):** Thêm `Is_Required bit DEFAULT 0` và `Is_Enabled bit DEFAULT 1` vào `Ui_Field`.
- **Revised (2026-05-17 — ADR-017):** Bỏ `Is_Enabled` (semantics overlap với ReadOnly + Visible).
  - `Is_Visible`, `Is_ReadOnly`, `Is_Required` = 3 cột tĩnh trong `Ui_Field`
  - `Lock_On_Edit` (mới) = readonly khi FormMode=Edit, editable khi Create
  - `Val_Rule` type `Required` → **deprecated**
- **Migration:** `010_field_behavior_columns.sql` (add) + `017_lock_on_edit_replace_is_enabled.sql` (revise)

## ADR-017: Bỏ Is_Enabled, thêm Lock_On_Edit (2026-05-17)
- **Context:** Sau khi review thực tế, `Is_Enabled` chỉ khác `Is_ReadOnly` ở một điểm:
  ReadOnly **vẫn submit**, Disabled **KHÔNG submit**. Nhưng ICare247:
  1. BE chưa có partial-update API (PATCH) → ReadOnly+full update vẫn ghi value cũ OK
  2. % case dùng Is_Enabled thực sự nhỏ
  3. Pattern thường gặp là "field nhập lúc create, khóa khi update" — cần riêng cờ này
- **Decision:**
  - Drop `Ui_Field.Is_Enabled`
  - Add `Ui_Field.Lock_On_Edit bit DEFAULT 0`
  - EffectiveReadOnly = `Is_ReadOnly OR (Lock_On_Edit AND FormMode=Edit)`
  - Blazor SET_ENABLED action → alias sang SET_READONLY (backward compat seed)
  - ValidationEngine required check skip → đổi từ `!IsEnabled` sang `!IsVisible`
- **Tradeoff:** Bỏ khả năng "field hiện thấy nhưng không submit" runtime. Khi cần sẽ
  invest @FormMode infra cho Event engine sau (Wave riêng).
- **Migration:** `017_lock_on_edit_replace_is_enabled.sql`

## ADR-011: Val_Rule type bổ sung — Length và Compare (2026-03-26)
- **Context:** Hiện có Required/Range/Regex/Numeric/Custom. Thiếu 2 type phổ biến.
- **Decision:**
  - `Length` — kiểm tra `len(value) >= min && len(value) <= max` cho string field
  - `Compare` — so sánh với field khác trong cùng form: `value >= {OtherField}`
  - Không dùng `Required` rule nữa (→ `Ui_Field.Is_Required`)
- **Migration:** `011_add_rule_types.sql`

## ADR-012: Evt_Action type bổ sung — SET_ENABLED, CLEAR_VALUE, SHOW_MESSAGE (2026-03-26)
- **Context:** Event engine thiếu 3 action type phổ biến trong low-code form engines.
- **Decision:**
  - `SET_ENABLED` — enable/disable field (grayout + exclude from submit)
  - `CLEAR_VALUE` — xóa giá trị field khi field phụ thuộc thay đổi (VD: clear Huyện khi đổi Tỉnh)
  - `SHOW_MESSAGE` — hiển thị toast/popup thông báo cho user (`info`/`warn`/`error`)
- **Migration:** `012_add_action_types.sql`

## ADR-013: Col_Span 3-column → 4-column grid (2026-03-26)
- **Context:** Full HD layout có đủ chỗ. 3-col tạo ra tỉ lệ 1/3, 2/3 không tự nhiên. 4-col cho 1/4, half, 3/4, full — phù hợp form y tế nhiều field nhỏ.
- **Decision:** `Col_Span BETWEEN 1 AND 4`; mapping: 1=1/4, 2=half, 3=3/4, 4=full. Data migration: 3(full cũ)→4(full mới).
- **Blazor:** `.fields-grid { grid-template-columns: repeat(4, 1fr) }` + `grid-column: span @State.ColSpan` per FieldRenderer.
- **WPF:** RadioButton 1/4, 1/2, 3/4, Full (ConverterParameter 1/2/3/4).
- **Migration:** `013_colSpan_4col.sql`

## ADR-007: ConfigStudio.WPF.UI — Direct DB, KHÔNG qua backend API (2026-03-20)
- **Decision:** WPF admin tool kết nối trực tiếp SQL Server qua Dapper + connection string
- **KHÔNG** gọi HTTP API (backend API chỉ dành cho Blazor runtime)
- **Áp dụng:** Tất cả service trong ConfigStudio phải implement Dapper trực tiếp (IDbConnectionFactory hoặc SqlConnection)
- **Why:** Admin tool nội bộ, cần tốc độ + offline capability, không cần HTTP overhead

## ADR-014: ConfigCache facade — đọc config qua cache, hạn chế chọc thẳng DB (2026-06-07)
- **Context:** `MetadataEngine` đã cache FormMetadata+ResourceMap (L1 mem 5' → L2 Redis 30' → DB) nhưng nhiều chỗ vẫn đọc DB trực tiếp (i18n resolve message, lookup options, permission). Cần 1 lớp cache config thống nhất.
- **Decision:**
  - **`IConfigCache` = facade DUY NHẤT** đọc mọi *config* (form metadata, i18n resource, lookup options, permission, rule/event def). Web/Handler **CẤM inject repository config trực tiếp** — chỉ qua facade. Repo config chỉ được facade gọi.
  - **Phân biệt Config vs Data:** Config (metadata, đổi hiếm) → cache. Data (bản ghi nghiệp vụ) → KHÔNG cache.
  - **Cache-aside, L1(MemoryCache)+L2(Redis)** qua `ICacheService` sẵn có. Key: `{tenant}` + `{lang}`(nếu i18n) + `{version}`.
  - **Invalidation = Version-stamp (target scale-out) + Event-remove (bổ trợ) + TTL (lưới an toàn):**
    - Redis `cfgver:{tenant}:{form}` (metadata) / `cfgver:{tenant}` (i18n, lookup). Sửa config → INCR version → key đổi → mọi instance miss → reload (KHÔNG cần pub/sub).
    - Version cache L1 TTL ngắn (10–30s) để khỏi hit Redis mỗi request → cửa sổ stale ~30s (chấp nhận vì config đổi hiếm).
    - 1 instance: tạm dùng Event-remove (đã có) nhưng key phải chừa chỗ `:v{n}` để bật version-stamp khi scale-out, không phải refactor.
  - **Bổ sung:** stampede lock per-key khi miss; negative cache (TTL ngắn) cho key không tồn tại.
- **Anti-pattern cần dọn:** handler validation/trùng (`SaveMasterDataCommandHandler`, `InsertLookupCommandHandler`) đang gọi `IResourceRepository` thẳng → route qua `IConfigCache`. Lookup options (`LookupApiService`), permission tương tự.
- **Lộ trình:** xem TASKS.md mục "ConfigCache facade".
