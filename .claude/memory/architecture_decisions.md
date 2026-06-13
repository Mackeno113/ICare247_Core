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

## ADR-018: Multi-tenant database-per-tenant + Catalog DB master, nhận tenant qua subdomain (2026-06-12)
- **Context:** 1 IIS API + 1 IIS app phải chạy cho nhiều DB khác nhau; mỗi tenant có cấu hình +
  dữ liệu riêng. `SqlConnectionFactory` hiện là **singleton + connection string cố định** (single-tenant cứng).
- **Decision:**
  - **Database-per-tenant**: mỗi tenant = **1 Config DB** (metadata Sys_/Ui_/Val_/Evt_/Gram_) + **1 Data DB**
    (dữ liệu vận hành) riêng, cô lập vật lý.
  - **Catalog DB master riêng** (vd `ICare247_Master`): bảng Tenants map `tenant → (Config conn, Data conn)`.
    Conn string của catalog nằm trong `appsettings.local.json` của API (1 dòng cố định).
  - **Nhận diện tenant qua SUBDOMAIN** (`congtyA.icare247.vn` → API & app suy từ Host) — bắt buộc vì phải
    biết tenant TRƯỚC login (bảng user nằm ở Data DB).
  - **Connection string mã hóa cột** trong catalog, key giải mã ở `appsettings.local.json`.
  - `SqlConnectionFactory`: singleton-cố-định → **scoped + `ITenantConnectionResolver`** (tra catalog,
    cache in-memory). Repository KHÔNG sửa (vẫn `CreateConnection()`). **Fallback:** chưa cấu hình catalog →
    dùng conn cố định hiện có (dev không vỡ).
  - `TenantMiddleware`: suy tenant từ Host thay vì tin header `X-Tenant-Id` → đồng thời khép lỗ hổng bảo mật #1.
- **Hybrid on-prem + cloud:** thiết kế chạy cả 2 (SQL Server on-prem lẫn Azure SQL); chọn hạ tầng qua config.
- **Status:** 🔴 thiết kế chốt — chưa code (infra resolver + catalog).

## ADR-019: Quy ước Data DB — tên tiếng Việt + cột hệ thống tiếng Anh + PK 'Id' + soft-check qua Sys_Relation (2026-06-12)
- **Context:** Data DB là dữ liệu vận hành người dùng/khách nhìn trực tiếp; tách khỏi Config DB (metadata).
- **Decision:**
  - **Tên bảng**: tiếng Việt không dấu PascalCase + **tiền tố nhóm** (vd `DS_TinhThanhPho` = danh sách).
    Bộ tiền tố **đã chốt theo module nghiệp vụ → xem ADR-022**. Config DB GIỮ tiền tố Anh.
  - **Cột nghiệp vụ tiếng Việt không dấu** (`Ma`, `Ten`, `MoTa`...); **cột hệ thống/auto tiếng Anh**.
  - **Cột auto bắt buộc MỌI bảng:** `Id` BIGINT IDENTITY (PK) · `CreatedBy` BIGINT · `CreatedAt` DATETIME ·
    `UpdatedBy` BIGINT NULL · `UpdatedAt` DATETIME NULL · `IsDeleted` BIT · `Ver` INT (optimistic concurrency).
    `Ma` là cột riêng có unique.
  - **PK = `Id` đồng nhất** (tốt cho engine generic + Dapper `splitOn` mặc định). FK = `{Table}Id` (đặt tên
    ngữ nghĩa, có thể tiền tố vai trò: `NoiSinh_TinhThanhPhoID`).
  - **Soft-check FK khi xóa = đọc registry tường minh `Sys_Relation`** (KHÔNG đoán theo tên) → xử lý đúng
    nhiều FK cùng nguồn. `Sys_Relation` mở rộng (migration 035): `Detail_FK_Column`, `Master_Key_Column`,
    `On_Delete`, `Relation_Code`. `ReferenceCheckService` **hybrid**: Sys_Relation trước → fallback name-match
    cho bảng chưa khai (giai đoạn chuyển tiếp).
- **Sys_User → Data DB** (không phải Config DB) → auth repo dùng `IDataDbConnectionFactory`.
- **Status:** Sys_Relation + soft-check ✅ code xong (REL-1/REL-2); convention bảng/cột 🔴 chờ thiết kế Data DB.

## ADR-020: Audit-log chi tiết — bắt diff tầng Application, JSON, bật/tắt theo bảng+màn hình (2026-06-12)
- **Context:** Cần log toàn bộ thao tác user: ai tạo/sửa/xóa dữ liệu nào, **cột nào sửa, cũ→mới**;
  CHỈ thao tác từ giao diện, bỏ qua SQL tay vào DB; bật/tắt theo từng bảng + màn hình.
- **Decision:**
  - **Bắt diff ở tầng Application** (handler CRUD generic), KHÔNG dùng trigger → tự động chỉ tính thao tác UI
    (SQL tay không qua handler). Update: load row cũ → diff cột đổi (cũ→mới). Create/Delete: snapshot.
  - **Lưu header + JSON diff** ở Data DB (vd `NK_ThayDoi`: TenBang, BanGhiId, HanhDong, NguoiThaoTacID,
    ThoiGian, Form_Code, ChiTiet JSON `[{Cot,Cu,Moi}]`). Truy vấn được bằng OPENJSON.
  - **Bật/tắt theo bảng + màn hình:** cờ `Sys_Table.Audit_Enabled` + `Ui_Form.Audit_Enabled` (màn hình đè bảng).
- **Phụ thuộc:** user context (NguoiThaoTacID) từ JWT claim → cần pha Auth.
- **Status:** 🔴 thiết kế chốt — chưa code (cần Data DB + Auth).

## ADR-021: Scale-out nhiều IIS — stateless + Redis chia sẻ + DataProtection shared + file ở DB qua abstraction (2026-06-12)
- **Context:** Sau này tách site (app/web/từng module = IIS riêng) → phải đảm bảo "ở đâu cũng lấy được dữ liệu".
- **Decision:**
  - **Không giữ state trong process/đĩa cục bộ IIS.** Phiên/cache per-user → **Redis (L2 chia sẻ)**
    (đã có `HybridCacheService` L1+L2); JWT stateless để node nào cũng validate.
  - **DataProtection keyring PHẢI shared** (Redis) — thiếu → antiforgery/cookie mã hóa vỡ khi đổi node.
    Cấu hình có **fallback dev** (Redis vắng → keyring local).
  - **File/ảnh upload (user chốt lưu DB, xử lý nhược điểm; hybrid):** **metadata LUÔN ở Data DB** + **bytes qua
    provider cắm được** (`db|blob|filestream`) qua abstraction **`IFileStorage`**. Mặc định portable =
    `varbinary(max)` ở **filegroup/"Files DB" riêng per-tenant** (DB nghiệp vụ gọn). Xử lý nhược điểm:
    chunked streaming (không nạp cả file vào RAM) · cache+ETag (Redis/CDN) · dedup SHA-256 + RefCount.
    ⚠️ FILESTREAM hợp on-prem nhưng **Azure SQL không hỗ trợ** → giữ provider cắm được (on-prem→FILESTREAM,
    cloud→Blob), default varbinary chạy mọi nơi.
- **Status:** DataProtection 🔴 đang code đợt này; file storage 🔴 backlog (user chọn chưa code).

## ADR-Sec: Đóng lỗ hổng CORS + JWT SecretKey (2026-06-12)
- **#2 CORS:** bỏ `AllowAnyOrigin`; whitelist `Cors:AllowedOrigins` + `AllowCredentials`; dev tự nhận loopback;
  prod rỗng → dừng khởi động. **#3 JWT:** fail-fast `Jwt:SecretKey` ngoài Development (rỗng/<32/placeholder → throw).
  **#1 (tenant từ claim):** hoãn sang pha Auth (xem ADR-018 — sẽ đóng khi suy tenant từ Host/claim).

## ADR-022: Bộ tiền tố bảng Data DB — theo module nghiệp vụ (2026-06-12)
- **Context:** ADR-019 chốt tên bảng = "tiếng Việt không dấu PascalCase + tiền tố nhóm" nhưng để HOÃN bộ
  tiền tố cụ thể. Config DB đã dùng tiền tố theo module/engine (`Sys_/Ui_/Val_/Gram_/Evt_`).
- **Quyết định (qua hỏi-đáp, chốt 3 điểm):**
  1. **Triết lý = theo MODULE nghiệp vụ** (không theo bản chất dữ liệu DS_/GD_/CT_) → nhất quán với Config DB
     và 8 module UI; mỗi module = 1 nhóm bảng → dễ phân quyền + tách site.
  2. **Trade GỘP một** `TM_` (không tách MH_/BH_/KHO_).
  3. **Bảng hạ tầng dùng chung TÁCH nhóm riêng** (không gộp hết vào HT_).
- **Bộ tiền tố (10 nhóm):**

  | Prefix | Nhóm | Phạm vi |
  |---|---|---|
  | `HT_` | Hệ Thống | Người dùng + phân quyền cấp data (Identity + Administration) |
  | `TC_` | Tổ Chức | Organization (công ty, phòng ban) |
  | `DM_` | Danh Mục | Danh mục dùng chung (tỉnh/thành, ĐVT…) — tách riêng |
  | `NS_` | Nhân Sự | Hr |
  | `TL_` | Tiền Lương | Payroll |
  | `TM_` | Thương Mại | Trade — gộp hàng hóa/mua/bán/kho |
  | `CN_` | Công Nợ | Finance |
  | `BC_` | Báo Cáo | Reporting (cấu hình báo cáo lưu) |
  | `NK_` | Nhật Ký | Audit-log (`NK_ThayDoi`) — tách riêng |
  | `TT_` | Tệp Tin | File đính kèm — tách riêng |

- **Ví dụ:** `TM_PhieuNhapKho`, `NS_NhanVien`, `HT_NguoiDung`, `DM_TinhThanhPho`, `NK_ThayDoi`.
- **Liên quan:** ADR-019 (quy ước tên/cột Data DB), ADR-020 (`NK_ThayDoi`), ADR-018 (DB-per-tenant).
- **Status:** ✅ convention chốt — áp dụng khi thiết kế bảng Data DB thật (chưa có bảng nào ngoài `NK_*` thiết kế).
- **Status:** #2/#3 ✅ code xong (Program.cs), build xanh; #1 hoãn.
