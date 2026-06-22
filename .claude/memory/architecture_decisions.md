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

## ADR-023: Menu động + phân quyền — server-driven từ HT_ChucNang, master→tenant (2026-06-13)
- **Context:** `AppNav.cs` vẽ menu tĩnh, `NavMenu.CanShow()` luôn `true` → mọi tài khoản thấy đủ. DB tenant
  ĐÃ có sẵn mô hình quyền đầy đủ (`HT_ChucNang` cây chức năng self-ref, `HT_VaiTro`, `HT_VaiTro_Quyen`
  Xem/Them/Sua/Xoa/Duyet/InAn, `HT_NguoiDung_VaiTro`) nhưng chưa nối vào menu. → khoảng trống `phase-auth`.
- **Quyết định (qua hỏi-đáp nhiều vòng):**
  1. **Server là nguồn sự thật:** backend đọc `HT_ChucNang ⨝ HT_VaiTro_Quyen (Xem=1)` theo role user →
     trả cây menu; frontend chỉ render. `AppNav.cs` → **seed + fallback dev**.
  2. **2 nửa bắt buộc:** (a) client lọc menu (UX) + (b) **server enforce** `[RequirePermission(Ma, Op)]`
     deny-by-default trên controller engine (ẩn menu ≠ bảo mật).
  3. **2 khái niệm tách bạch:** ① ĐỊNH NGHĨA menu = **DEV qua WPF**, ghi **Config DB** (`Sys_Menu`,
     `Sys_MenuCatalog` master). ② PHÂN QUYỀN = **end user qua Web/API**, ghi **Data DB** tenant. End user
     KHÔNG đụng route/icon/API (không thể biết).
  4. **Master → tenant (Cách 3 lai):** menu base ở `Sys_MenuCatalog` (Config DB) → đồng bộ **UPSERT theo `Ma`**
     xuống `HT_ChucNang` mỗi tenant (`LaHeThong=1`); DEV thêm node riêng/khách (`LaHeThong=0`); đồng bộ
     không đụng custom. Tenant = khách, **mỗi khách 1 Data DB** (ADR-018).
  5. **Bổ sung cột `HT_ChucNang`:** `Menu_Id`, `LaHeThong` (base/custom), `KichHoat` (tenant bật/tắt),
     `ViTriHienThi` (`Sidebar`/`TrongMan`/`Ca2`). Bảng mới Config DB: `Sys_Menu`, `Sys_MenuCatalog`.
  6. **1 cây sâu tùy ý** (self-ref `ChucNang_Cha_Id`) cho MỌI chức năng cần quyền; tách "vị trí render"
     bằng `ViTriHienThi` → sidebar nông, các "quá trình" của 1 bản ghi render sub-nav TRONG màn. Mỗi node =
     1 màn (có quyền riêng); **bản ghi/giá trị bên trong màn = dữ liệu, KHÔNG vào cây**.
  7. **5 cờ quyền:** Xem · Thêm · Sửa · Xóa · In. **`Duyet` → workflow** (giữ cột, không hiện ở Phân quyền).
  8. **UI cấu hình:** màn Phân quyền = **bespoke** (`DxTreeList` × 5 checkbox); Vai trò/Người dùng = **engine
     MasterData no-code**. Nguyên tắc chung: **A (no-code) mặc định, bespoke chỉ khi đặc thù**.
  9. **API:** gộp `GET /api/v1/me/navigation` + `/me/permissions` (1 call sau login), cache theo tenant+role
     (`IConfigCache`, ADR-014), recursive CTE cho cây sâu.
- **Hoãn:** `ChucNangCon` (quyền cấp nút), `Sys_Menu` nhiều bộ menu (Top/Mobile), `Duyet` (workflow).
- **Spec:** `docs/spec/15_AUTHZ_NAVIGATION_SPEC.md`.
- **Liên quan:** ADR-018 (DB-per-tenant), ADR-007 (ConfigStudio Direct DB), ADR-022 (tiền tố `HT_`/`Sys_`), ADR-014 (ConfigCache).
- **Status:** 📋 thiết kế CHỐT — chưa code. Việc theo `TASKS.md` phase-auth.

## ADR-024: Màn nghiệp vụ chuẩn = engine-driven (no-code), KHÔNG bespoke — màn Công ty pivot (2026-06-15)
- **Context:** Đã dựng màn Công ty **bespoke full-stack** (commit `d658ff8`: RCL `ICare247.UI.Organization` +
  `CompaniesController` + `CongTyRepository` + CQRS). User yêu cầu **linh động cấu hình per-tenant** (super admin
  copy giao diện → config, sửa riêng, fallback mặc định) — đúng triết lý nền tảng no-code đã có (`Sys_Table`/`Ui_*`).
- **Decision:** Màn nghiệp vụ chuẩn (vd Công ty) **không bespoke** mà **engine-driven**:
  - List/cây = `Ui_View` TreeList → `DataView`; form Thêm/Sửa = `Ui_Form` (Display_Mode=**Popup**) → `FormRunner`;
    CRUD = `SaveMasterDataCommand` generic (tự bơm audit). Bắt buộc/validation/lookup/i18n = **cấu hình** `Ui_Field`.
  - **Thiết kế ở ConfigStudio WPF** (ghi thẳng Config DB, **KHÔNG SQL seed**); dùng bản bespoke cũ làm **tham chiếu**
    field/kiểu/nhãn/source. Web config editor cho super admin → **hoãn** (tiết kiệm dev).
  - **Đọc dữ liệu qua SQL View** (vd `vw_TC_CongTy`) thay vì bảng thô; **lọc theo phân quyền** = RLS
    (`SESSION_CONTEXT`, phương án P1) — **thiết kế sau** (phân quyền dữ liệu hoãn).
- **Action:** **GỠ bespoke** (`d658ff8`) → commit `0fae3f7` (giữ history làm tham chiếu). Giữ baseline lưới + rule
  (`41ce53a`) + blueprint Công ty.
- **Liên quan:** ADR-025 (config sync), spec `docs/spec/16_CONFIG_SYNC_SPEC.md`, blueprint
  `.claude/skills/icare247-admin-ui/references/blueprint-company.md`.

## ADR-025: 1 DB / 1 tenant (Config DB + Data DB per-tenant) → cần đồng bộ config master→tenant (2026-06-15)
- **Context:** User chốt **mỗi tenant một bộ DB riêng**, gồm **cả Config DB** (không chỉ Data DB). Config
  (`Sys_Table`+`Ui_*`) thiết kế 1 lần qua WPF nhưng phải tới được Config DB từng tenant.
- **Decision:** Cần **nền tảng F1 — đồng bộ config master → tenant** (làm TRƯỚC engine-hóa màn = "Cách 2"):
  - Master = Config DB "vàng" canonical (cùng schema tenant). Sync **UPSERT theo MÃ** (`*_Code`) + **re-link FK
    theo mã** (không bê `*_Id` identity) — tái dùng pattern menu `Sys_MenuCatalog→HT_ChucNang` (ADR-023).
  - Cờ **`LaHeThong`** (hệ thống/tenant) + **`DaTuyBien`** → tenant chỉnh không mất khi re-sync. Xóa = `Is_Active=0`.
  - Incremental theo version; tích hợp invalidate `ConfigCache`.
- **Status:** 📋 thiết kế CHỐT (spec `16_CONFIG_SYNC_SPEC.md`) — chưa code; chờ duyệt 5 quyết định mở (§10 spec).
- **Liên quan:** ADR-018 (DB-per-tenant), ADR-023 (master→tenant menu), ADR-024 (engine-driven), ADR-014 (ConfigCache).

## ADR-026: Menu Builder web — ghi đơn-DB (Data) + picker đọc Config; LC1 (Group+View) dựng sẵn lên LC3 (+Form) (2026-06-17)
- **Context:** Cần đưa View đã cấu hình lên menu web. Trước đó node `HT_ChucNang` chỉ thêm bằng SQL seed tay;
  chưa có UI. Câu hỏi kèm: có nên tách "đọc Config" và "ghi Data" thành 2 service độc lập không.
- **Decision:** **KHÔNG tách microservice** — giữ 1 API monolith + 2 connection factory đã có
  (`IDbConnectionFactory`=Config, `IDataDbConnectionFactory`=Data). Menu Builder:
  - **Ghi đơn-DB:** node menu chỉ ghi `HT_ChucNang` (Data DB tenant) qua `/api/v1/admin/menu` → không cross-DB tx.
  - **Đọc chéo chỉ để picker:** dropdown View đọc Config DB qua `/api/v1/views` (đã có). Sau ghi → `INavigationCache.InvalidateTenant`.
  - **Discriminator `NodeKind`** {Group, View, Form}: server `UpsertMenuNodeCommand.ResolveKind` xử lý CẢ Form
    (whitelist → `Loai`/`DuongDan`/`DoiTuong`/`LoaiDoiTuong`). UI v1 = **LC1** (Group+View); **Form ẩn**.
  - **Nâng LC1→LC3 = chỉ sửa web** (bỏ `disabled` option Form + thêm dropdown Form từ `FormApiService.GetFormsListAsync`).
    KHÔNG đụng backend/DB/endpoint.
  - Quyền: chức năng mới `administration.menu` (`RequirePermission`); seed `db/054`. CRUD đầy đủ; chặn xóa node
    `LaHeThong=1` và node còn con; chống vòng lặp cha-con (server + client).
- **Files:** `MenuAdminController` · `Features/Admin/Menu/*` · `MenuAdminRepository` · `MenuBuilderPage.razor` +
  `MenuAdminApiService` · `db/054`. Guide: `docs/guide/cau-hinh-menu.md`.
- **Liên quan:** ADR-018 (DB-per-tenant), ADR-023 (menu/authz), phân tích 2-factory (đọc Config vs ghi Data).

## ADR-027: Cơ chế sắp xếp cây cha-con dùng chung — `ThuTu` (input) + `ThuTuCay`/`DuongDanCay` (dẫn xuất) (2026-06-21)
- **Context:** Mọi cây tự tham chiếu trong nền tảng (`TC_CongTy`, `TC_PhongBan`, `HT_ChucNang`, + cây generic qua
  `Ui_View.Parent_Field`) cần thứ tự hiển thị nhất quán: gốc theo thứ tự, con nằm ngay dưới cha, đánh số liên tục
  trên→dưới để show UI + truy vấn nhanh. Tham khảo SP `PhongBan` (recursive CTE → chuỗi đường dẫn → renumber).
- **Decision — 2 tầng tách bạch:**
  - **Tầng 1 — `ThuTu` (INT, input người dùng):** thứ tự **trong cùng cha/cùng cấp**, người dùng tự đặt.
    - ▲▼ = **swap `ThuTu`** với anh em liền kề (2 dòng).
    - Kéo-thả = đổi `Cha_Id` (nếu sang cha khác) + đánh lại `ThuTu` **dày đặc 1..n** cho nhóm anh em đích;
      **chặn vòng lặp** (không thả node vào con-cháu của chính nó). KHÔNG dùng gap/sparse hay LexoRank (quá mức cho quy mô này).
  - **Tầng 2 — cột DẪN XUẤT, tính lại tự động NGAY SAU KHI GHI (recompute-on-write):**
    - **`DuongDanCay`** NVARCHAR — chuỗi `001.002.003`, **3 chữ số/bậc** (max 999 node/cấp), nối `.`;
      mỗi bậc = `ROW_NUMBER() PARTITION BY Cha_Id ORDER BY ThuTu, Id`. Sort chuỗi = duyệt cây cha→con, trên→dưới.
    - **`ThuTuCay`** INT — `ROW_NUMBER() OVER (ORDER BY DuongDanCay)` → **liên tục 1..N cả bảng**.
  - **Hiện thực = 1 stored proc GENERIC** (tham số: bảng, cột Key/Parent/Order, + cột Scope tùy chọn để xếp gốc),
    do **write-path (repository C#) gọi sau khi ghi** — logic CTE nằm ở SQL, KHÔNG lặp proc per-table.
- **Quy ước chuẩn ICare247:**
  - **Gốc chính = `Cha_Id IS NULL` HOẶC `Cha_Id = 0`** (hỗ trợ cả 2 quy ước).
  - Luôn lọc `IsDeleted = 0` (và `KichHoat`/`TrangThai` nếu cây đó có).
  - **Tiebreaker `Id`** trong `ORDER BY ThuTu, Id` — `ThuTu` có thể trùng → tránh thứ tự bất định.
  - **`TC_PhongBan`:** cây = toàn bộ cấu trúc công ty (công ty đóng vai gốc → tổ → bộ phận) → đánh số **liên tục cả
    bảng** là đúng; xếp gốc theo **công ty trước, rồi `ThuTu`** để mỗi công ty nằm liền khối (công ty 1 → công ty 2 → …).
- **Áp dụng:** `TC_CongTy`, `TC_PhongBan`, `HT_ChucNang` (+ cây generic). Triển khai khi code: (1) migration thêm
  `ThuTuCay`/`DuongDanCay` + index; (2) proc generic; (3) nối write-path (▲▼ / kéo-thả).
- **Tài liệu:** `docs/spec/17_TREE_ORDERING_SPEC.md`.
- **Liên quan:** ADR-023 (cây menu HT_ChucNang), session 58 (sidebar order theo `ThuTu`), nav repo (tiebreaker `Id`).

## ADR-028: Đọc cột cho `Ui_View` theo Source_Type — Table từ Sys_Column, View/SP từ Target DB (2026-06-21)
- **Context:** Màn "Quản lý View" (ConfigStudio WPF), tab Cột: khi `Source_Type=View` (vd `vw_DM_PhuongXa`,
  Table_Id=5) column picker vẫn query `SELECT … FROM dbo.Sys_Column WHERE Table_Id=@TableId` → **rỗng**, vì
  `Sys_Column` chỉ lưu cột của **BASE TABLE** (do sync/seed). View/SP không bao giờ ghi vào đó. Nhưng View/SP là
  đối tượng thật trong DB (đã có Table_Code) → đọc cấu trúc trực tiếp được — đó mới đúng luồng.
- **Decision — rẽ nhánh theo `Source_Type` khi nạp cột cho `Ui_View`:**
  - **`Table`** → giữ nguyên: đọc `Sys_Column` (Config DB), `Column_Id` thật.
  - **`View`/`Sp`** → đọc cấu trúc **trực tiếp từ Target DB** (`IAppConfigService.TargetConnectionString`):
    - View dùng `SchemaInspectorService.GetColumnsAsync` (INFORMATION_SCHEMA.COLUMNS — liệt kê cột View y như table).
    - SP dùng method mới `GetProcedureColumnsAsync` qua `sys.dm_exec_describe_first_result_set` (phân tích tĩnh,
      **không thực thi** SP; chỉ hợp SP **inline-table** = luôn trả đúng 1 result-set, theo chốt của user).
    - Cột View/SP → `Ui_View_Column.Column_Id = NULL` (cột vốn nullable = "unbound/computed"); chỉ giữ `Field_Name`.
      Trong VM map `ColumnId 0 → null` để tránh vi phạm `FK_Ui_View_Column_Column`.
  - **Chưa cấu hình Target DB → báo lỗi rõ ràng, KHÔNG fallback** sang Sys_Column (tránh trả rỗng gây hiểu nhầm).
  - Tên đối tượng: ưu tiên `Source_Object` (hỗ trợ `schema.object`), trống → dùng `Table_Code`; schema fallback `dbo`.
    Cache theo khóa `SourceType|TableId|SourceObject`.
- **Files:** `ISchemaInspectorService`/`SchemaInspectorService` (thêm `GetProcedureColumnsAsync` + parse
  `system_type_name`); `ViewManagerViewModel` (`EnsureColumnsLoadedAsync` rẽ nhánh + `ResolveSourceObject`).
  `Api`/`Sql` để sau (api: nguồn cột chưa rõ).
- **Liên quan:** ADR-015 (Ui_View), ADR-016 (Ui_View_Filter Source_Type Sp/Sql), ADR-007 (ConfigStudio Direct DB).
- **Status:** ✅ code xong, compile xanh (build chỉ fail copy DLL do app đang chạy — cần đóng app build lại để chạy thử).

## ADR-029: Save hook stored-proc per màn — `spc_Grid_<T>` (validate trước) + `sp_AfterSave_Grid_<T>` (hậu xử lý) (2026-06-22)
- **Context:** Màn engine-driven (vd Xã/Phường, pipeline `SaveMasterData`) cần thêm lớp validation cuối ở DB +
  hậu xử lý sau ghi, nhận **toàn bộ field của màn** + **người thực hiện** + **khóa chính `Id`** (`Id=0` = thêm mới).
  Câu hỏi gốc của user: "số lượng thông báo lỗi vô chừng thì dịch i18n thế nào?" và "truyền data khi view không cố
  định trường — ngoài JSON còn cách nào?".
- **Quyết định (qua hỏi-đáp nhiều vòng):**
  1. **Lỗi vô chừng = dịch hữu hạn:** store **KHÔNG trả chuỗi tiếng Việt**, trả `error_key` + `args`. Số bản dịch =
     số RULE (hữu hạn), không phải số lần lỗi. Khớp `sys.val.*` (spec 10).
  2. **Cơ chế lỗi:** store `SELECT error_key, args_json, field_name, severity` (rỗng = hợp lệ). `field_name=NULL` =
     thông báo cấp form (banner). Thông báo bất kỳ biết trước = 1 key (`sys.val.Invalid`); text tự do = escape hatch
     `sys.msg.raw` = "{0}" (mất đa ngôn ngữ, chỉ debug/1 ngôn ngữ).
  3. **Truyền data = JSON + OPENJSON** cho field động; **context cố định** (`@Id/@TenantId/@NguoiThucHien/@LangCode`)
     = param rời. (TVP chỉ khi save batch; XML/dynamic-sql loại.)
  4. **i18n resolve SERVER-SIDE** qua `IConfigCache.ResolveKeyAsync` (giống code unique hiện có) → DTO
     `MasterDataFieldError` giữ nguyên (mang text đã dịch).
  5. **Bọc transaction:** `spc_ → INSERT/UPDATE → sp_AfterSave_` trong 1 transaction Data DB; after-save lỗi → rollback.
  6. **Store thiếu = opt-in:** runtime `OBJECT_ID IS NULL` → bỏ qua (app account KHÔNG cần quyền DDL). Tạo store qua
     **nút codegen trong ConfigStudio WPF** → sinh file `db/procs/*.sql` skeleton **rỗng pass-through** (review rồi
     chạy tay; `IF OBJECT_ID IS NULL CREATE` → không ghi đè logic đã viết).
  7. **Id:** command `null` (insert) → quy đổi `0` khi vào store.
- **Naming:** validate trước = `spc_Grid_<TableCode>` · hậu xử lý = `sp_AfterSave_Grid_<TableCode>` (TableCode =
  `Sys_Table.Table_Code`, vd `spc_Grid_DM_PhuongXa`).
- **Điểm chạm:** `MasterDataRepository` (method transaction + opt-in) · `SaveMasterDataCommandHandler` (dòng 84:
  gọi proc-validate + merge + resolve) · `MasterDataForm.razor` (dòng 170: gom lỗi field rỗng vào banner) ·
  ConfigStudio WPF (nút codegen) · migration seed `sys.val.*`/`sys.val.Invalid`/`sys.msg.raw`.
- **Cache tồn tại store (không query khi lưu — bổ sung sau phản hồi user):** `IHookStoreCatalog` cache-aside
  (L1/L2 + version-stamp tenant) thay vì `OBJECT_ID` mỗi lần lưu. Nạp sẵn ở `GetMasterDataFormInfoQueryHandler`
  (mở list MasterData); save đọc cờ → truyền `hasValidate/hasAfterSave` vào `SaveWithHooksAsync` (bỏ `ProcExistsAsync`
  trong repo). Cold-miss = 1 query gộp 2 `OBJECT_ID`; flush cache (bump `ICacheVersion`) = nhận store mới. Màn View
  tự nạp ở lần lưu đầu (không pre-warm vì key store = bảng edit-form, không phải view nguồn).
- **Spec:** `docs/spec/18_SAVE_VALIDATION_HOOK_SPEC.md` (§9 cache).
- **Liên quan:** ADR-024 (engine-driven), ADR-014 (ConfigCache resolve i18n + cache-aside), spec 10 (Resource Key), spec 14 (View).
- **Status:** ✅ code xong (session 60, build BE/FE/WPF 0/0) + cache-aside cờ store. ⏳ E2E (SVHOOK-6: chạy SQL).
