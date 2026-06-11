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

## ADR-015: Ui_View — cấu hình hiển thị danh sách tách khỏi form sửa (2026-06-07)
- **Context:** Màn danh mục đang lấy cột lưới từ `Ui_Field.Show_In_List` — trộn 2 mối quan tâm
  (form sửa vs hiển thị danh sách). Cần thêm TreeList, datasource tùy biến, thuộc tính cột giàu
  (width/align/format/sort/filter/group/frozen), nút in/xuất file (xlsx/pdf/docx), render cột HTML.
  `Ui_Field` không mô hình được quan hệ 1 bảng → N view, cũng không có chỗ cho hierarchy/toolbar.
- **Decision:** Tạo cụm bảng cấu hình **view** generic trong Config DB — tách hẳn khỏi `Ui_Form`/`Ui_Field`:
  - **`Ui_View`** (header): `View_Type` (Grid|TreeList|Cards), datasource (`Source_Type`/`Source_Object`:
    Table|View|Sp|Api), `Edit_Form_Id` (link form Thêm/Sửa), hành vi lưới (paging/filter/group/search/
    selection/CRUD), export/print, TreeList (`Key_Field`/`Parent_Field`/`Expand_Level`), master-detail.
  - **`Ui_View_Column`**: cột + thuộc tính — `Render_Mode` (Text|Html|…), sort/filter/group, frozen,
    conditional format (`Style_Rule_Json` qua Grammar V1 AST), và **export rule**.
  - **`Ui_View_Action`**: nút toolbar/row — CRUD mở rộng, export, print, navigate, event, api.
  - **i18n bắt buộc:** mọi text là `_Key` → `Sys_Resource`, **scope = `table_code`**, category `view`
    (xem spec 10 §1d). Caption cột fallback về `Ui_Field.Label_Key` để không dịch trùng.
  - **Render giàu ≠ dữ liệu xuất:** ô có thể render HTML; **export luôn lấy giá trị thuần**
    (`Export_Format ?? Display_Format`, bỏ `Render_Mode`); cột HTML-only → `Allow_Export=0`.
  - **Export engine:** xlsx/xls/csv = DxGrid client-side; **pdf/docx = server-side theo template**
    (DxGrid không xuất docx); xuất toàn bộ (vượt trang) ⇒ bắt buộc server-side.
- **Tương thích:** `Ui_Field.Show_In_List` bị thay; migration auto-sinh 1 Grid view mặc định cho mỗi
  `Ui_Form` hiện có. Route `/master/{FormCode}` → `/view/{ViewCode}` (giữ alias chuyển tiếp).
- **Runtime model:** `MasterDataGridConfig`/`MasterDataColumnDto` (đã code session này) là model runtime
  mà `Ui_View*` map vào; component Blazor `DataView` chọn `DxGrid`/`DxTreeList` theo `View_Type`.
- **Cache:** `IConfigCache.GetViewAsync(viewCode, tenant, lang)`, key `{tenant}:{lang}:v{n}` (ADR-014);
  ResourceMap loader nạp thêm prefix `{tableCode}.view.%`.
- **Ownership:** db + ConfigStudio = Codex; Domain/Application/Infrastructure/Api/Blazor = Claude.
- **Chi tiết schema đầy đủ:** `docs/spec/14_VIEW_CONFIG_SPEC.md`.
- **Status:** thiết kế chốt; triển khai chưa bắt đầu (cần handoff Codex).

---

## ADR-016: Lưới nâng cao — panel lọc trái + tham số SP/SQL (Ui_View_Filter) (2026-06-11)
- **Context:** Cần "lưới nâng cao": panel control lọc bên trái + nút Tìm (i18n) → đẩy tham số vào
  câu SQL/Stored Procedure → đổ kết quả ra lưới, kèm nút thao tác. `Ui_View` đã có `Source_Type`
  (Table|View|Sp|Api) + cột/action/export, nhưng **thiếu panel lọc tham số**: `Show_Filter_Row`
  chỉ là ô lọc trong cột, `Default_Filter_Json` là lọc cố định — không có control→tham số SP.
- **Decision (Hướng A — mở rộng `Ui_View`, KHÔNG tạo `View_Type='QueryGrid'` hay store riêng):**
  - Trục "nguồn dữ liệu" (`Source_Type`) **trực giao** với trục "render" (`View_Type`). Panel lọc là
    tính năng bật/tắt theo nguồn, không phải loại lưới mới → tái dùng 100% `Ui_View_Column`/`Ui_View_Action`.
  - **Bảng mới `Ui_View_Filter`** (per-View, FK `View_Id`): mỗi control lọc = 1 dòng. **MỖI THAM SỐ =
    1 DÒNG riêng** (DateRange tách `tu_ngay` Operator '>=' + `den_ngay` '<='; KHÔNG có cột Param_Name_To).
    Lý do: thông báo i18n + focus chính xác từng ô khi thiếu tham số (báo "Từ ngày là bắt buộc" được).
  - **Whitelist tham số:** engine chỉ bind `@param` khai báo trong `Ui_View_Filter`; ép kiểu theo
    `Param_Type`; bọc `%...%` khi `Operator='LIKE'`; giá trị rỗng → NULL (SP nên xử lý `@x IS NULL` = bỏ lọc).
    Giá trị **luôn parameterized** (Dapper) → chống injection. SP name validate identifier (schema.proc).
  - **Cờ panel = cột tường minh trên `Ui_View`** (không nhét Options_Json): `Filter_Panel_Enabled`,
    `Filter_Panel_Position` (left|top), `Filter_Collapsible`, `Auto_Search_On_Load` (default 0 = chờ bấm
    Tìm, tránh SP nặng), `Search_Label_Key`, `Reset_Label_Key`.
  - **Engine rule:** panel chỉ render khi `Filter_Panel_Enabled=1` AND `Source_Type ∈ {Sp,Sql}` AND có
    ≥1 filter (`ViewMetadata.HasFilterPanel`). Bấm Tìm → validate Required → bind whitelist → gọi SP/SQL,
    trả nguyên tập đã lọc (client phân trang, TotalCount = số dòng).
  - **i18n triệt để:** mọi text UI là `_Key` → `Sys_Resource`. `Param_Name`/`Default_Value`/`Lookup_Code`
    là literal nghiệp vụ (KHÔNG i18n). Key convention: `{table}.view.filter.{filter_code}.label/.placeholder/
    .tooltip`; nút `common.filter.search`/`.reset`; thông báo `common.validation.required` = "{0} là bắt buộc".
  - **MultiSelect → IN:** hoãn đợt 2 (engine đã có khung tách mảng theo dấu phẩy cho Source_Type='Sql').
- **API:** `POST /api/v1/views/{code}/search` (body `{ filters: {filterCode: value} }`) →
  `GetViewFilteredDataQuery` → `IViewRepository.GetFilteredDataAsync`. Tham số sai/thiếu → 400.
- **Ownership:** db migration (034) + ConfigStudio = Codex; Domain/App/Infra/Api/Blazor = Claude.
  *(Session 2026-06-11: user yêu cầu Claude làm trọn gói cả db + ConfigStudio.)*
- **Chi tiết schema:** `docs/spec/14_VIEW_CONFIG_SPEC.md §9`. Migration `db/034_create_ui_view_filter.sql`.
- **Status:** ✅ thiết kế chốt; backend (Domain/App/Infra/Api) đã code + build xanh (2026-06-11);
  Blazor FilterPanel + ConfigStudio đang triển khai.
