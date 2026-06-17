# Last Session Summary

> Cập nhật: 2026-06-17 (session 55 — Menu Builder web + WPF grid layout persistence + 2 guide)

## Session 55 (2026-06-17) — đã làm

> Việc ad-hoc (ngoài roadmap F1/F2): màn cấu hình menu web + lưu layout lưới WPF + tài liệu. Build BE 0/0, FE 0/0. CHƯA commit.

- **MENU-BUILDER** — Màn web "Quản lý menu" (`/m/administration/menu`) cấu hình cây `HT_ChucNang` bằng **chọn**
  (không gõ tay/SQL). Kiến trúc chốt: **1 service / 2 factory, KHÔNG tách microservice** — ghi **đơn-DB**
  `HT_ChucNang` (Data tenant) qua `/api/v1/admin/menu`, picker đọc Config DB qua `/api/v1/views` sẵn có.
  - BE: `MenuAdminController` + CQRS `Features/Admin/Menu/*` (GetMenuTree/UpsertMenuNode/DeleteMenuNode) +
    `MenuAdminRepository` (`IDataDbConnectionFactory`) + DI. Sau ghi → `INavigationCache.InvalidateTenant`.
  - FE: `MenuBuilderPage.razor` (DxTreeList + form chọn) + `MenuAdminApiService` + DI Program.cs.
  - DB: `db/054_seed_ht_chucnang_menu_admin.sql` — node `administration.menu` + grant SUPERADMIN (idempotent).
  - CRUD đầy đủ; chặn xóa node hệ thống (LaHeThong=1) / còn con; chống vòng lặp cha-con (server + client).
  - **`NodeKind` {Group, View, Form}** — server xử lý CẢ Form (whitelist); UI v1 = **LC1** (Group+View, Form ẩn
    `disabled`). **Nâng LC1→LC3 = chỉ sửa web** (bỏ disabled + thêm dropdown Form từ `FormApiService.GetFormsListAsync`).
- **WPF-GRID-LAYOUT** — Mọi `dxg:GridControl` ConfigStudio: **kéo chỉnh độ rộng cột** + **tự lưu/phục hồi layout**
  ra file local (`%LOCALAPPDATA%\ICare247\ConfigStudio\GridLayouts\*.xml`, KHÔNG vào DB). Gắn 1 lần qua implicit
  `TableView` style (`Themes/Controls.xaml`): `AutoWidth=False` (điều kiện kéo giãn) + `AllowResizing=True` +
  attached behavior `Infrastructure/Behaviors/GridLayoutBehavior` (key = tên UserControl + chữ ký cột, hash FNV-1a).
- **DOC-GUIDES** — `docs/guide/cau-hinh-man-quan-ly-view.md` (7 tab màn Quản Lý View) +
  `docs/guide/cau-hinh-menu.md` (cẩm nang dùng Menu Builder, kiểu user-manual).

### Quyết định (ADR-026 + grid)
- Menu Builder = monolith 2-factory (phân tích: tách microservice tái tạo chi phí mạng mà cache in-process đã khử).
- WPF grid `AutoWidth=False` (user chốt pixel cố định + cuộn ngang) — điều kiện kéo giãn cột trên lưới nhiều cột.

### ⏳ Việc cần làm
- Chạy `db/054` trên Data DB tenant → E2E Menu Builder (login admin → Quản trị → Quản lý menu).
- (Tùy) đóng ConfigStudio → rebuild để nạp bản WPF grid-layout mới (bin bị khóa khi app đang chạy).
- Tiếp tục roadmap chính: **F1 E2E** (db/050 + config-sync) → **F2 engine-hóa màn Công ty**.

---

> Cập nhật: 2026-06-16 (session 54 — F1/F2 nối tiếp: màn web config-sync, engine-hóa Công ty, danh mục, ConfigStudio picker, i18n token validation)

## Session 54 (2026-06-16) — đã làm

> Nối tiếp F1 (CFGSYNC) + khởi động F2 (engine-hóa màn) + công cụ cấu hình. Build BE/FE/WPF 0/0. Đã push tới `96c0d9d`.

- **CFGSYNC-3 UI** (commit `73184e5`) — màn web "Đồng bộ cấu hình" `/m/administration/config-sync`: `ConfigSyncApiService`
  + `ConfigSyncPage` (Xem trước dry-run + Áp dụng từ master có xác nhận, badge + bảng kết quả). i18n `admin.cfgsync.*`.
- **F2 engine-hóa màn Công ty — code** (commit `422d904`): ORG-CFG-1 (`SchemaInspector` liệt kê cả VIEW),
  ORG-CFG-2 (`db/051` `vw_TC_CongTy`), ORG-CFG-4 (`NavScreen.Route` + `ScreenView` redirect → menu Công ty vào
  `/view/Tree_TC_CongTy`). ORG-CFG-3 (Ui_Form/Ui_View) = cấu hình tay trong ConfigStudio.
- **Danh mục nền tảng** (commit `6a8c846`): `db/052` (`vw_DM_TinhThanhPho`/`vw_DM_PhuongXa`) + AppNav module "Danh mục"
  (7 màn route `/view/Grid_*`). CAT-CFG-2 = cấu hình tay.
- **ConfigStudio Sys Table picker** (commit `96c0d9d`): combobox chọn bảng/view thật từ Target DB → tự điền
  Table_Code/Schema. + `db/dev/reset_config.sql` (xóa cấu hình Config DB, dev only).
- **i18n token validation** (CHƯA commit): `required`+`unique` hỗ trợ `{0}`=giá trị nhập, `{1}`=nhãn (per-field + template);
  `ResourceResolver`/`SaveMasterData`/`InsertLookup` + `db/053`.
- **Guide** (CHƯA commit): `docs/guide/cau-hinh-man-danh-muc.md`.
- **Fix cấu hình**: `TargetDb` của ConfigStudio (appsettings local) đổi `QLNS_Demo` → `ICare247_Solution` (Live) —
  combobox/auto-generate đọc đúng bảng nghiệp vụ.

### ⏳ Việc cần làm
- Chạy SQL: `db/050` (Config), `db/051`/`db/052` (Live), `db/053` (Config). Khởi động lại ConfigStudio (TargetDb mới).
- Cấu hình tay: danh mục (CAT-CFG-2) → màn Công ty (ORG-CFG-3) theo `docs/guide/cau-hinh-man-danh-muc.md` + AI_HANDOFF.
- Commit + push phần i18n token validation + guide.

---

> Cập nhật: 2026-06-15 (session 53 — đồng bộ code GitHub + F1 config-sync full-stack: CFGSYNC-0→3)

## Session 53 (2026-06-15) — đã làm

### Đồng bộ code từ GitHub
- Nhánh master chậm 5 commit → fast-forward `a49dcbd`→`be573aa` (bỏ 3 thay đổi local chỉ là timestamp i18n tự sinh).
- Code mới về: pivot engine-driven (ADR-024/025), baseline DxGrid/DxTreeList, 14 migration DB, spec 16 config-sync,
  skill `icare247-admin-ui` +4 reference.

### F1 — Đồng bộ config master→tenant (CFGSYNC-0→3) — code đủ để chạy, build BE 0/0

- **CFGSYNC-0** — chốt **5 quyết định mở** (§10 spec 16), toàn bộ theo khuyến nghị: master=Config DB canonical ·
  bảo vệ **row-level** (`Is_Customized`) · giữ bản tenant khi xung đột · trigger provisioning+nút thủ công · xóa=`Is_Active=0`.
  Ghi vào spec 16 §10 + TASKS.md.
- **CFGSYNC-1** — `db/050_alter_config_sync_flags.sql` (vùng Codex, Claude tạo thay): thêm 4 cờ **tên tiếng Anh**
  `Is_System`/`Is_Customized`/`Synced_At`/`Source_Ver` (nhất quán config DB, KHÁC `LaHeThong/DaTuyBien` của Data DB)
  vào 11 bảng (spec §7) + bảng `Sys_Config_Sync_Log`. Idempotent. ⏳ CHƯA chạy DB.
- **CFGSYNC-2** — engine **descriptor-driven** (Application `IConfigSyncService`+`ConfigSyncOptions`/`ConfigSyncResult`;
  Infrastructure `ConfigTableDescriptor`/`ConfigSyncTables`/`ConfigSyncService`). UPSERT theo MÃ + re-link FK theo mã
  (map 2 chiều Code↔Id, KHÔNG bê Id identity), khóa nghiệp vụ = [khóa cha re-link]+mã con (sep U+0001), 1 transaction,
  dry-run, tombstone `Is_Active=0`, giữ `Is_Customized`, ghi log. Đọc cột qua INFORMATION_SCHEMA (không SELECT *).
  Master=`ConnectionStrings:Config` (dev trùng tenant→vô hại). **VERTICAL SLICE 5 bảng**: Sys_Table→Sys_Column→
  Ui_Form→Ui_Section→Ui_Field.
- **CFGSYNC-3** — `SyncConfigCommand`+Handler + `AdminConfigSyncController`: `POST /api/v1/admin/config-sync` (áp thật,
  `[RequirePermission("administration.config-sync", Sua)]`) + `/preview` (dry-run, `Xem`). SUPERADMIN bypass; invalidate
  menu cache sau khi áp. TriggeredBy từ claim.

### ⏳ Để chạy/verify thật
- Chạy `db/050` trên Config DB → đăng nhập `admin` → POST /preview rồi /config-sync → kiểm `Sys_Config_Sync_Log`.

→ **Bước tiếp:** E2E F1; mở rộng descriptor các bảng còn lại (Sys_Resource/Sys_Lookup/Ui_Tab/Ui_View*/Val_Rule);
hook provisioning full-sync; invalidate ConfigCache version-stamp (CC-4); UI nút "Cập nhật cấu hình từ master";
seed node `administration.config-sync` vào HT_ChucNang. Sau F1 → **F2 engine-hóa màn Công ty** (ORG-CFG-1→4).

## Session 52 (2026-06-15) — đã làm

### UI-GRID-BASELINE + UI-RULE (commit `41ce53a`)
- **Baseline lưới** ở component dùng chung: `DataView.razor` (DxGrid + DxTreeList) + `MasterDataGrid.razor`:
  `TextWrapEnabled=true`, `ColumnResizeMode=ColumnsContainer` (**cột giữ độ rộng + cuộn ngang**, không ép vừa màn),
  `AllowColumnReorder`/`HighlightRowOnHover`/`FocusedRowEnabled`, **cột chọn ghim trái + khai báo đầu tiên**;
  TreeList thêm `DxTreeListSelectionColumn`. **Fix** `FilterRowCellVisible`→`FilterRowEditorVisible` (property crash DX v25.2.3).
- **Rule chuẩn** vào skill `icare247-admin-ui`: tách **DxGrid** + **DxTreeList** thành **2 file độc lập**
  (`references/grid-dxgrid.md`, `grid-dxtreelist.md`), thêm `references/i18n.md` (**i18n bắt buộc — cấm hardcode**)
  + `blueprint-company.md`; bổ sung ràng buộc i18n + bảng tham chiếu vào `SKILL.md`.

### Màn Công ty: bespoke → GỠ → pivot engine-driven (commit `d658ff8` rồi `0fae3f7`)
- Đã dựng **bespoke full-stack** `TC_CongTy` (RCL `ICare247.UI.Organization` + `CompaniesController` +
  `CongTyRepository` + CQRS + tree/form/cascade) — `d658ff8`.
- User chốt hướng **engine-driven (no-code)** thay bespoke → **gỡ sạch** ở `0fae3f7` (history `d658ff8` giữ làm
  tham chiếu thiết kế). **ADR-024**.

### Thiết kế (CHỐT, chưa code) — chuỗi quyết định kiến trúc
- **Hướng A (ADR-024):** màn Công ty = engine: list `Ui_View` TreeList + `Ui_Form` Popup + `Ui_Field` (bắt
  buộc/validation/lookup/i18n cấu hình); thiết kế ở **ConfigStudio WPF** (ghi thẳng Config DB, **không SQL seed**);
  đọc qua **SQL View** + RLS phân quyền (**hoãn**). Khảo sát: ConfigStudio đã có `SchemaInspectorService` +
  `AutoGenerateFields` (đọc schema thật) — gap nhỏ: inspector chỉ liệt kê BASE TABLE, cần thêm VIEW.
- **1 DB / 1 tenant (ADR-025):** mỗi khách có **Config DB + Data DB riêng** → cần **F1 — đồng bộ config
  master→tenant** (UPSERT theo MÃ + re-link FK theo mã + cờ `LaHeThong`/`DaTuyBien`). Spec đầy đủ:
  `docs/spec/16_CONFIG_SYNC_SPEC.md`. Chốt **Cách 2: làm F1 trước → F2 (engine-hóa màn) sau**.

→ **Bước tiếp:** chốt **5 quyết định mở** (§10 spec 16: master ở đâu · mức bảo vệ tùy biến · xử lý dòng đã tùy
biến · trigger · chính sách xóa) → code F1 (CFGSYNC-1→3). Phân quyền dữ liệu (RLS) thiết kế sau.

## Session 51 (2026-06-15) — đã làm

### CFG-CONN-FIX — Chuẩn hóa LocalConfigLoader template

- Template cũ có key `"Data"` nhưng `ConnectionChecker` kiểm tra `"LiveData"`, `"Demo"`, `"Audit"` → 3 DB báo "chưa cấu hình" dù đã điền.
- Cập nhật template trong `LocalConfigLoader.cs`: đổi `"Data"` → `"LiveData"`, bổ sung `"Demo"` + `"Audit"`, **bỏ toàn bộ placeholder** (Server=localhost, CHANGE_ME...) — tất cả để `""`.
- Áp dụng chung: `Jwt.Issuer/Audience/SecretKey` cũng để trống (không điền giả).
- **Lưu ý:** file `appsettings.local.json` trên máy đã tồn tại → template mới KHÔNG tự ghi đè. User cần update tay hoặc xóa file để tạo lại.

### HOTFIX — 500 `/api/v1/me/navigation`

- Nguyên nhân: `NavigationRepository` SELECT `c.DoiTuong`, `c.LoaiDoiTuong` nhưng bảng `HT_ChucNang` trong `ICare247_Solution` chưa có 2 cột này (db/046 chưa chạy).
- Đã áp thủ công qua sqlcmd: `ALTER TABLE dbo.HT_ChucNang ADD DoiTuong NVARCHAR(100) NULL, LoaiDoiTuong NVARCHAR(50) NULL` — **verify OK**.
- Ghi chú: db/046 đã áp bằng lệnh trực tiếp; file script vẫn giữ để documentation.

### DB-ANNOTATE — Thêm `-- Database:` header toàn bộ migration files

- 15 file `db/*.sql` thiếu chỉ định DB target → đã thêm dòng `-- Database: <tên DB>` vào header.
- Bảng mapping đầy đủ:
  - **ICare247_Config**: 000–035, 039 (pending), 043, 044, 047
  - **ICare247_Solution** (Data/LiveData per-tenant): 037, 038, 042, 045, 046, 048, 049
  - **ICare247_Solution_Audit**: 040
  - **ICare247_Master** (Catalog): 036, 041
  - **Legacy/tham khảo**: 015 (Cf_* cà phê — Data DB cũ)

→ **Bước tiếp:** chạy `db/047` (Config DB) + `db/048`, `db/049` (Data DB) → restart API → test navigation đầy đủ.

---

## Session 50 (2026-06-14) — đã làm

### i18n cấu hình đa ngôn ngữ ở Web UI — tách 2 phần + hợp nhất cơ chế hand-coded

**Bối cảnh — 2 phần i18n tách bạch:**
- **Phần 1 — màn WPF tạo (metadata-driven):** label/placeholder/tooltip/caption lưu key `Sys_Resource`,
  backend `MetadataEngine` resolve theo `Lang_Code`. **Đã có i18n** — không đụng.
- **Phần 2 — màn/control viết tay trong Web UI:** trước đây chuỗi cứng tiếng Việt → **đợt này bơm i18n**.

**Quyết định user (chốt qua hỏi):** phạm vi = TOÀN BỘ phần 2; **key trước, dịch sau** (en.json để rỗng,
fallback vi tại chỗ gọi); key theo **nguyên tắc WPF (spec 10)** = `common.*` + `{scope}.{category}.*`;
**hợp nhất** mọi chuỗi hand-coded về **`LocalizationService.L(key, fallback)`** (shell, JSON overlay client-side),
**bỏ `I18nService`** (kho `common.*` qua `Sys_Resource` backend — trùng namespace, đã xóa).

**Đã làm:**
- Đăng ký **nguồn i18n host** `Loc.RegisterSource("i18n")` trong `MainLayout` trước `InitializeAsync`;
  tạo `wwwroot/i18n/en.json` rỗng (Shared vẫn là nguồn `common.*`/shell).
- **Migrate 3 màn** khỏi `I18nService.T()` → `Loc.L()`: `FilterPanel`, `ViewPage`, `MasterDataForm`
  (gỡ `EnsureCommonLoadedAsync`); **gỡ DI + xóa** `Services/I18nService.cs`.
- **Bơm `L()`** (thêm `@inherits LocalizedComponentBase`) cho trang bespoke: `Home`, `MasterDataListPage`,
  `MasterDataTabPage`, `FormRunner`; và chrome trong component: `MasterDataGrid` (cột "Thao tác", nút Sửa/Xóa),
  `ConfirmDeleteDialog`, `LookupAddDialog`, `LookupBoxRenderer`, `TreeLookupBoxRenderer`.
- **Bộ key chuẩn:** `common.action.*` (save/cancel/create/update/edit/delete/refresh/run/close/clear/back/
  hide/show/understood/saving), `common.filter.*`, `common.validation.required`, `common.search.placeholder`,
  `common.label.error`, `common.column.actions`, `common.lookup.*`, `common.msg.saveFailed`; namespace riêng
  trang: `dev.forms.*`, `masterdata.*`, `view.*`, `formrunner.*`, `delete.*`, `lookupadd.*`.
- Build frontend **0 error / 0 warning**.

→ **Bước tiếp:** dịch `en.json` (Shared `common.*` + host page keys) khi cần bản tiếng Anh; cân nhắc trang
`/dev/i18n` báo key thiếu (gói #7) + xuất Excel cho người dịch (gói #5).

## Session 49 (2026-06-13) — đã làm

### UI — Redesign sidebar (đã code + commit `f038b90`)
- Thay icon emoji → bộ **Lucide line đơn sắc** (`currentColor`), khử "đơn điệu"/lệch nét.
- Gom 7 phân hệ thành **3 nhóm có nhãn**: Vận hành / Kinh doanh / Hệ thống (`NavGroup` trong `AppNav.cs`).
- Active state: thanh accent trái + chữ/icon xanh; màn con có đường rail dọc; caret SVG xoay.
- Tách component **`<Icon>` dùng chung** ở `ICare247.UI.Shared` (`Name/Size/StrokeWidth`, registry path Lucide);
  `NavModule.Icon` giữ **tên icon** (tách khóa nghiệp vụ ↔ icon); xóa `NavIcon` cũ. `ScreenView` dùng `<Icon>`.
- **Quyết định icon:** Lucide (chính) + Tabler (bổ sung), copy path từ lucide.dev/tabler.io vào `Icon.razor`.

### Thiết kế phase-auth — Menu động + Phân quyền (CHỐT, CHƯA code) → ADR-023
- **Server-driven menu** từ `HT_ChucNang` (lọc `HT_VaiTro_Quyen.Xem=1` theo role user); `AppNav`→seed+fallback.
- **2 khái niệm tách:** ① ĐỊNH NGHĨA menu = DEV/WPF→Config DB (`Sys_Menu`,`Sys_MenuCatalog` master); ② PHÂN QUYỀN
  = end user/Web→Data DB (`HT_VaiTro_Quyen`,`HT_NguoiDung_VaiTro`). End user KHÔNG đụng route/icon/API.
- **Master→tenant (lai):** base ở `Sys_MenuCatalog` → đồng bộ UPSERT theo `Ma` xuống `HT_ChucNang` mỗi tenant
  (`LaHeThong=1`); DEV thêm node riêng (`=0`); tenant=khách, mỗi khách 1 Data DB.
- **1 cây sâu tùy ý** (self-ref) cho mọi chức năng cần quyền; tách "render" bằng cột **`ViTriHienThi`**
  (Sidebar/TrongMan/Ca2) → sidebar nông, "quá trình" của 1 bản ghi render sub-nav TRONG màn. Node=màn; bản ghi=data.
- **Bổ sung cột `HT_ChucNang`:** `Menu_Id, LaHeThong, KichHoat, ViTriHienThi`.
- **5 cờ quyền:** Xem/Thêm/Sửa/Xóa/In. **Duyệt → workflow** (giữ cột).
- **UI:** Phân quyền = bespoke `DxTreeList`×5 checkbox; Vai trò/User = engine MasterData. Nguyên tắc: **A no-code
  mặc định, bespoke khi đặc thù**.
- **Tài liệu đã viết:** `docs/spec/15_AUTHZ_NAVIGATION_SPEC.md` + ADR-023 (architecture_decisions.md) + roadmap
  AUTHZ-* trong `TASKS.md`.

### Triển khai phase-auth Stage 1–3 (đã code + commit; CHƯA áp SQL)
- **Stage 1 DB (commit 7dbcea7):** `db/042` ALTER HT_ChucNang (+Menu_Id/LaHeThong/KichHoat/ViTriHienThi+index);
  `db/043` CREATE Sys_Menu/Sys_MenuCatalog (Config DB); `db/044` seed MAIN + 45 node master từ AppNav;
  `db/045` seed HT_ChucNang base (nối cha-con theo Ma) + grant SUPERADMIN 5 cờ. **Chưa chạy vào DB.**
  Thứ tự chạy: 037→038→042→(043,044 Config)→045.
- **Stage 2 BE (commit 5ddb171):** `MeNavigationDto/MeNavNodeDto`, `GetMyNavigationQuery`+Handler,
  `INavigationRepository`+`NavigationRepository` (Dapper recursive CTE: Xem=1 + tổ tiên, cờ MAX OR vai trò),
  `MeController [Authorize] GET /api/v1/me/navigation` (userId từ claim sub), DI. Compile sạch.
- **Stage 3 FE (commit fbdd353):** `MeNavModels` + `NavigationApiService` + `NavMenu` dựng VM từ API,
  **rỗng/lỗi → fallback AppNav** (app vẫn chạy khi chưa seed). Build xanh.
- **Stage 4 (commit 4c25504 BE, 9282b1c FE):** API admin phân quyền (`GET /api/v1/admin/roles`,
  `GET/PUT roles/{id}/permissions` — upsert HT_VaiTro_Quyen MERGE/transaction; CQRS + `PermissionAdminRepository`)
  + màn **Phân quyền** `/m/administration/permissions` (DxTreeList cây + 5 DxCheckBox Xem/Thêm/Sửa/Xóa/In →
  PUT lưu; `AdminPermissionApiService`). Build BE compile sạch / FE xanh.
- **Stage 4 mở rộng (đã code + commit, SQL 042–045 user ĐÃ chạy):**
  - Màn Phân quyền: **cascade tri-state** (cha derived từ con, indeterminate khi lẫn) + **check nhanh**
    (cột "Tất cả"/dòng · checkbox header cả cột · ô master) + UX (checkbox to, hover dòng, zebra, ShowAllRows).
  - **AUTHZ-SEC:** `IPermissionService` + `[RequirePermission]` → gắn AdminPermissionController (bịt lỗ ai cũng sửa quyền).
  - **AUTHZ-SEC-2:** `db/046` HT_ChucNang +DoiTuong/LoaiDoiTuong; `HasPermissionForTargetAsync` **enforce-if-mapped**;
    `[RequirePermissionForTarget]` → MasterData(Form CRUD)/View(Xem)/Runtime(Xem)/FormController.GetByCode(Xem).
  - **BE-2:** `INavigationCache` (IMemoryCache + token hủy theo tenant) cache /me/navigation, invalidate sau lưu quyền.
  - **FE-3:** nav trả DoiTuong; `PermissionState` (ForTarget); MasterDataListPage/Grid ẩn Thêm/Sửa/Xóa theo quyền.
  - **DxTreeList reference:** `docs/reference/DEVEXPRESS_DXTREELIST_PROPERTIES.md` + tái dựng tool `tools/DxReflect`.
  - **AUTHZ-UI-2 (Vai trò):** Engine MasterData **tự bơm audit** (CreatedBy/At insert · UpdatedBy/At update theo
    cột tồn tại; userId luồn `SaveMasterDataCommand`←claim). `db/047` seed form `HT_VaiTro`; `db/048` nối menu
    `administration.roles` → `/master/HT_VaiTro` + DoiTuong. (Phát hiện: engine cũ KHÔNG bơm audit → HT_* insert lỗi.)
- **CÒN LẠI:** AUTHZ-UI-2b (Người dùng HT_NguoiDung = màn **bespoke**, field nhạy cảm) · FE-3b (sub-nav TrongMan +
  ẩn nút DataView, chờ màn HR thật) · scale-out: NavigationCache token → Redis (ADR-021). Xem TASKS.md AUTHZ-*.
- **⏳ SQL cần chạy:** db/046 (cột DoiTuong) · db/047 (Config: form HT_VaiTro) · db/048 (Data: nối menu) + **restart API**.
- **Cách bật khóa 1 màn:** chạy `db/046` → set HT_ChucNang.DoiTuong=mã form/view + LoaiDoiTuong=Form/View →
  cấp quyền ở màn Phân quyền. Nhớ **restart API** để attribute áp.

---

## Session 48 (2026-06-13) — đã làm

### CFG-CONN — connstring ngoài repo (đổi tên + thêm Live/Audit)
- `appsettings.local.json` (ngoài git): đổi `Data`→**`Demo`** (QLNS_Demo); thêm **`LiveData`**=ICare247_Solution
  (login + nghiệp vụ đọc từ đây) + **`Audit`**=ICare247_Solution_Audit (DB nhật ký riêng); sinh `Jwt:SecretKey` thật 64 ký tự.
- Repo: `appsettings.json` placeholder + readme keys mới; `TenantConnectionResolver` đọc `LiveData` (fallback Data→Config)
  + mang thêm `AuditConnectionString`; `ConnectionChecker` log Live/Demo/Audit.

### AUTH-BE — backend đăng nhập full-stack (JWT)
- Package: `Microsoft.Extensions.Identity.Core` + `System.IdentityModel.Tokens.Jwt` (Infra).
- Domain `Entities/Auth/NguoiDung`. Interfaces: `IAuthRepository`/`IRefreshTokenRepository`/`IJwtTokenService`/`IPasswordHasher`.
- `Features/Auth`: **Login** (verify PBKDF2, kiểm Local/khóa/TrangThai/HetHan, lockout 5 lần→khóa 15', cấp JWT+refresh,
  cập nhật LanDangNhapCuoi), **Refresh** (rotation: thu hồi cũ + cấp mới), **Logout** (thu hồi), **Forgot/Reset = STUB**.
  `AuthResult`/`AuthStatus`. Validator login.
- Infra: `AuthRepository`/`RefreshTokenRepository` (Dapper, IDataDbConnectionFactory=Live), `JwtTokenService`
  (HMAC-SHA256, claim sub/unique_name/tenant/admin/role; refresh=32B random + hash SHA256), `IdentityPasswordHasher`.
- Api: `AuthController` `POST /api/v1/auth/{login,refresh,logout,forgot-password,reset-password}` `[AllowAnonymous]`,
  map AuthStatus→HTTP (401/403/423/501). Verify hash seed `admin`/`Admin@12345` (Identity v3) bằng PasswordHasher.

### AUTH-FE — 3 màn Auth + nối API thật
- `wwwroot/css/auth.css` (tách riêng, token `--auth-*`, split 2 cột, 3 motif, reduced-motion).
- Components `Components/Auth/{AuthShell,AuthInput,AuthButton,BrandPanel}`. Bộ chọn ngôn ngữ đặt trong cột form (cạnh logo).
- Màn `Pages/Auth/{Login(thật),ForgotPassword,ResetPassword(UI+stub)}`; mọi text qua `Loc.L`. **Đăng ký: bỏ** (cổng ứng viên sau).
- Shared: `TokenStore` (localStorage), `JwtParser`, `JwtAuthenticationStateProvider`, `AuthService` thật (POST login,
  lưu token, gắn `Bearer`, notify). DI Shared + `AddAuthorizationCore`; RCL thêm package `Microsoft.AspNetCore.Components.Authorization`.
- `App.razor` bọc `CascadingAuthenticationState`; `MainLayout` guard (chưa login→/login + tên user + nút đăng xuất);
  `Login.razor` đã login→điều hướng `/`. index.html nạp auth.css.

### AUDIT-1 — log hành vi non-blocking (Auth + MasterData)
- `IAuditWriter`/`AuditEvent`/`IAuditQueue` (Application). Infra: `AuditQueue` (bounded 20k, **DropWrite** — không chặn),
  `AuditNkWriter` (gom theo tenant → resolve audit-conn → **SqlBulkCopy** NK_), `AuditBackgroundService` (có Redis →
  XADD `ic247:audit` + consumer group XREADGROUP→DB→XACK; không Redis → ghi thẳng DB). Api `HttpAuditWriter` (làm giàu
  actor/IP/correlation/tenant từ HttpContext). Enqueue: Login(success/failed/locked)/Logout/Refresh + MasterData(create/update/delete).
- DB audit **RIÊNG per-tenant** (user chốt): `db/040_create_nk_audit.sql` (bảng `NK_NhatKyHoatDong` append-only, 3 index) chạy
  trên DB audit; `db/041_add_audit_conn_to_tenant.sql` (cột `Audit_Conn_Encrypted` ở catalog — chỉ cần khi bật đa tenant thật).
  Event chỉ mang TenantId (không lộ connstring qua Redis).

### DOCS-DEBUG — bộ tài liệu debug backend
- `docs/backend-debug/`: README (bản đồ 4 lớp + pipeline middleware + breakpoint theo lớp + bảng lỗi + template) +
  trang từng tính năng: `auth-login`(mẫu chi tiết), `auth-refresh-logout`, `auth-forgot-reset`, `forms-config`,
  `runtime-form`, `master-data`, `views`, `redis-setup` (hướng dẫn cài Redis Windows: Docker/Memurai/WSL).

### Build
- `src/backend/ICare247.slnx` **0/0** (sau khi dừng API để gỡ file-lock). `src/frontend/ICare247.UI.slnx` **0/0**.

### ✅ Đã verify (user xác nhận)
1. ✅ Tạo DB `ICare247_Solution_Audit` + chạy `db/040` trên nó.
2. ✅ Redis (cài/cấu hình).
3. ✅ E2E thật: login `admin`/`Admin@12345` → `NK_NhatKyHoatDong` ghi nhận đăng nhập. Pipeline audit chạy OK.

### ⏳ Việc cần làm (pha sau — để sau)
4. Forgot/Reset nối SMTP thật; diff `GiaTriCu` (giá trị cũ) cho MasterData audit; siết `NhanVien_Id` NOT NULL (đợt NS_).
- Mở rộng audit cho các module khác (Forms/Views/Lookup) nếu cần; trang xem nhật ký (admin).

### Memory gợi ý lưu (auto-memory)
- Cơ chế audit non-blocking + DB audit riêng per-tenant (project); login full-stack fallback X-Tenant-Id (project).

---

## Session 47 (2026-06-13) — đã làm

### Chốt bộ tiền tố bảng Data DB (theo module) — ADR-022
- Qua hỏi-đáp: tiền tố **theo MODULE nghiệp vụ** (không theo bản chất dữ liệu DS_/GD_/CT_) → nhất quán Config DB
  (`Sys_/Ui_/...`) + 8 module UI. **Trade gộp `TM_`**; bảng hạ tầng **tách riêng** (DM_/HT_/NK_/TT_).
- **10 tiền tố:** `HT_` Hệ Thống · `TC_` Tổ Chức · `DM_` Danh Mục · `NS_` Nhân Sự · `TL_` Tiền Lương ·
  `TM_` Thương Mại · `CN_` Công Nợ · `BC_` Báo Cáo · `NK_` Nhật Ký · `TT_` Tệp Tin.
- Ghi **ADR-022** trong `.claude/memory/architecture_decisions.md` + update ADR-019 (gỡ "HOÃN").

### Spec nền tảng Data DB — `docs/spec/11_DATA_DB_SCHEMA.md` (16 bảng)
- `DM_` (5): QuocGia, TinhThanhPho, **PhuongXa** (2 cấp, trực thuộc thẳng Tỉnh — bỏ Huyện theo mô hình VN 2025),
  DonViTinh, NganHang.
- `TC_` (4): CapCongTy, CapPhongBan, **CongTy** + **PhongBan** = **2 cây tree tự tham chiếu** (parent + cấp).
  TC_CongTy chỉ giữ `PhuongXa_Id` (Tỉnh suy qua PhuongXa — user yêu cầu bỏ `TinhThanhPho_Id`).
- `HT_` (7): NguoiDung, VaiTro, NguoiDung_VaiTro, ChucNang (cây menu), VaiTro_Quyen (Xem/Thêm/Sửa/Xóa/Duyệt/In),
  NguoiDung_CongTy (switcher), RefreshToken. **Phân quyền toàn bộ ở Data DB**.
- **Convention chốt:** không `Tenant_Id` (DB-per-tenant); khối auto universal = `Id/CreatedBy/CreatedAt/UpdatedBy/UpdatedAt/IsDeleted/Ver`
  (§0.1); `Ma`/`Ten`/`MoTa` = cột theo **archetype** (§0.2), **generic không entity-suffix** (giữ engine generic),
  FK thì `{Bang}_Id`. INSERT/seed set audit tường minh.
- **HT_NguoiDung** đối chiếu bảng legacy `SYS_NguoiDung`: gọn còn auth (bỏ Salt→PBKDF2 nhúng; bỏ Token*→HT_RefreshToken;
  bỏ HoTen/Email/ĐT/ảnh→lấy qua NhanVien). Thêm 2FA/LoaiTaiKhoan(AD/SSO/Portal)/KichHoatMobile/HetHanTaiKhoan.
  `NhanVien_Id` bắt buộc nghiệp vụ (nullable tạm; siết NOT NULL+FK+UNIQUE đợt NS_). Chicken-egg bootstrap → §6.7.

### Sinh SQL migration + chạy thật
- `db/037_create_data_db_foundation.sql` (Data DB): DDL 16 bảng idempotent, FK, filtered-unique `Ma WHERE IsDeleted=0`,
  CreatedBy/UpdatedBy **không FK** (tránh vòng lặp bootstrap), phá vòng `TC_PhongBan↔HT_NguoiDung` bằng ALTER FK cuối.
- `db/038_seed_data_db_bootstrap.sql` (Data DB): super-admin **`admin`/`Admin@12345`** (hash PBKDF2 Identity v3 tạo bằng
  PowerShell, verify được) + vai trò SUPERADMIN + cấp công ty/phòng ban + Quốc gia VN. Set `CreatedAt` tường minh (user nhắc).
- `db/039_seed_config_lookup_foundation.sql` (Config DB): `Sys_Lookup` + `Sys_Resource` (vi/en) cho
  `TRANGTHAI_NGUOIDUNG`/`TRANGTHAI_DONVI`/`LOAI_TAIKHOAN`/`HINHTHUC_2FA`.
- **User đã tạo DB `ICare247_Solution` + chạy đủ 3 script** (verify thật). KHÔNG dotnet build (chỉ SQL/docs/memory).

### Quyết định #1/#2/#3 (cho HT_NguoiDung)
- #1 TrangThai → **Sys_Lookup ở Config DB** (Data lưu Item_Code; code bất biến, label i18n đổi được; ConfigCache resolve).
- #2 Hash → **PBKDF2** `PasswordHasher<T>` (.NET built-in), `nvarchar(256)`.
- #3 `Cf_*` (015) → **giữ làm tham khảo** cho module mua bán cà phê nhân (`TM_` sau), không migrate.

### Memory mới
- Git-tracked: **ADR-022**. Auto-memory: `feedback-explicit-audit-columns`, update `project-multitenant-and-data-conventions` (gỡ HOÃN tiền tố).

### Việc tiếp theo gợi ý
- Đợt `TT_` (file/logo: `TT_TepDinhKem`) — hiện chỉ để cột FK `*_Id`.
- Đăng ký `Sys_Table` + `Sys_Relation` cho FK Data DB (đang dựa name-match) — khi đưa bảng vào metadata Engine.
- Module thật đầu tiên: `TC_`/`HT_` (Organization/Administration) hoặc `NS_` (siết `NhanVien_Id`) + pha **Auth/login**.
- Seed danh mục hành chính VN (Tỉnh/Phường-Xã) khi có nguồn chuẩn.

---

## Session 46 (2026-06-12) — đã làm

### FE-MOVE — chuyển RuntimeCheck sang frontend + sửa path
- User di chuyển folder `ICare247.Blazor.RuntimeCheck` `src/backend/src/` → `src/frontend/`. Sửa mọi path tham chiếu: `run-blazor.bat`/`run-all.bat` (cd `src\frontend`), `.claude/launch.json`, `src/backend/ICare247.slnx` (`../frontend/ICare247.Blazor.RuntimeCheck/...`), `docs/spec/12_CASCADE_LOOKUP_GUIDE.md` + `13_LOOKUP_ADD_NEW_GUIDE.md`. `settings.local.json` (allowlist cũ) để nguyên.

### FE-KHUNG — dựng khung ICare247_UI (modular monolith)
- **Quyết định** (user chốt qua hỏi-đáp): end-user app = `ICare247_UI`; **mỗi module nghiệp vụ = 1 RCL riêng**; host + RCL cross-cutting `ICare247.UI.Shared`.
- Tạo **RCL `ICare247.UI.Shared`**: `Services/Http/ApiClientBase`, `Services/Auth/IAuthService`+`AuthService`(stub), `State/AppState` (công ty hiện hành), `DependencyInjection.AddIcare247UiShared()`. Host `ICare247_UI` thêm ProjectReference + DI + `_Imports`.
- Solution frontend mới `src/frontend/ICare247.UI.slnx` (host + Shared + RuntimeCheck harness).

### FE-SHELL — shell ERP responsive + menu data-driven
- `Navigation/AppNav.cs`: cây 8 phân hệ (Organization/Hr/Payroll/Trade/Finance/Reporting/Administration + Auth) × màn con + helper `NavKeys` (suy key i18n từ slug) + field `Permission` (#4 hook).
- `Layout/`: `MainLayout` (sidebar + topbar + nút ☰ off-canvas mobile, backdrop, tự đóng khi điều hướng), `NavMenu` (accordion từ AppNav, lọc theo quyền stub), `AuthLayout`.
- `Pages/`: `Dashboard` (route `/`, KPI placeholder), `ScreenView` (`/m/{module}` lưới thẻ + `/m/{module}/{screen}` stub + breadcrumb). Trang dev `Home` đổi route `/`→`/dev/forms`.
- Màu/kích thước dùng biến `tokens.css` (Fluent Light, accent `#0F6CBD`) — không hardcode.
- **Verify chạy thật** (preview): desktop accordion + điều hướng OK; mobile (375px) ☰ off-canvas + backdrop OK; 0 lỗi console.

### FE-RUNUI — bat mở web
- `run-ui.bat` (gốc repo): chạy `ICare247_UI` profile https → https://localhost:7027, tự mở trình duyệt; shell không cần backend. Sửa `launchSettings` cổng phụ `5040`→`5173` (5040 bị OS chặn — gặp khi preview). Verify bind OK.

### FE-I18N — i18n shell (gói #1–#4 + #6)
- **Nguyên tắc (user chốt)**: KEY thuộc cấu trúc/code (suy từ slug), base vi nằm tại chỗ gọi (`Loc.L("key","base vi")`), JSON chỉ là **value overlay** (KHÔNG gõ key tay), thiếu → fallback base → "key có trước, dịch sau". Tách khỏi `Sys_Resource`/`I18nService` (chỉ cho nội dung động form/field/view).
- RCL Shared: `Services/I18n/LocalizationService` (lazy-load 1 ngôn ngữ; gộp overlay đa nguồn `RegisterSource` cho module RCL — #2; fetch URL tuyệt đối theo `NavigationManager.BaseUri`, KHÔNG dùng BaseAddress API; localStorage `ic247.lang`; set `CultureInfo` → DevExpress/số-ngày tự dịch — #3; tham số `{0}` — #1; pseudo-loc `qps` — #6) + `Components/LocalizedComponentBase` + `Components/LanguageSwitcher` + `wwwroot/i18n/{languages.json (vi,en), en.json rỗng}`.
- Bind `NavMenu/MainLayout/Dashboard/ScreenView` qua `Loc.L`. Thêm bộ chuyển ngôn ngữ vào topbar.
- **Verify**: ⟦Pseudo⟧ → mọi chuỗi shell bọc ngoặc (chỉ brand "ICare247" không, đúng) ⇒ không còn hardcode lọt lưới; English (en.json rỗng) → fallback vi, không vỡ. 0 lỗi console.

### Thảo luận (không code)
- Phân tích **DDD**: chưa áp dụng (giữ Clean Architecture); nếu sau này → DDD-lite chọn lọc cho module phức tạp (Payroll/Trade/Finance), Dapper repo mức aggregate-root, KHÔNG DDD-hóa nhánh metadata-engine.
- Gợi ý phương pháp thiết kế phù hợp: Vertical Slice + CQRS, Result pattern, Domain/Integration Events + Outbox, Read Model cho Reporting, Contract-first.

### Build
- `src/backend/ICare247.slnx` **0/0**; `src/frontend/ICare247.UI.slnx` **0/0**. (3 warning DevExpress license = pre-existing trial.)

### Việc tiếp theo gợi ý
- i18n: #5 (xuất skeleton Excel cho người dịch + import) hoặc #7 (trang `/dev/i18n` báo key thiếu/độ phủ).
- Bắt đầu module thật đầu tiên dưới dạng RCL (vd `ICare247.UI.Organization`) làm template, hoặc nối màn Auth (`auth.css`/`.razor`) theo design đã chốt.

---

## Session 45 (2026-06-11) — đã làm

### Theme DevExpress: blazing-berry (tím) → Fluent Light (commit `5fc36c4`)
- **Bối cảnh:** task "thay đổi phong cách" (ADR-012) mới làm nửa — `tokens.css` đã palette ERP
  nhưng `app.css` vẫn theme tím `#7c3aed`/`#845EF7`, và DxGrid vẫn berry tím `#5f368d`.
- **Điều tra (đối chiếu trực tiếp DLL theme trong NuGet cache):**
  - Khối map `--dx-*` trong tokens.css là **code chết** — blazing-berry v25.2.3 KHÔNG có biến
    `--dx-color-*`/`--dx-grid-*`. Theme dùng tiền tố `--dxbl-*`, đặt NGAY trên selector component.
  - Màu berry `#5f368d` **nướng cứng vào ~50 biến `--dxbl-*`** (checkbox, button, grid-focus,
    calendar, tabs, pager…) → override `--bs-primary` không đủ, phải đập từng cái.
  - **`office-white`** accent cam hardcode (cùng vấn đề). **`bootstrap-external`** dùng
    `var(--bs-primary)` (đổi 1 biến) nhưng cần thêm Bootstrap 5.
  - **Phát hiện theme Fluent** (gói `DevExpress.Blazor.Themes.Fluent` đã cài sẵn): default mới
    của DX, 11 accent + dark mode, **dễ đổi màu**. → chọn **Fluent Light, accent xanh mặc định**.
- **Bẫy đã sửa:** link `bootstrap/fluent-light.bs5.min.css` (332KB) **thiếu `core.min.css`**
  (1.36MB chứa layout grid) → grid vỡ, icon filter khổng lồ, text trợ năng lộ ra. Fluent là
  theme **lắp 4 file**: `global → core → modes/light → accents/blue` (đúng template DX).
- **Thay đổi (5 file):**
  - `index.html`: 4 link Fluent modular thay 1 link blazing-berry.
  - `csproj`: thêm `PackageReference DevExpress.Blazor.Themes.Fluent` 25.2.3.
  - `app.css`: viết lại dùng token ERP; **gỡ sạch** override ép navy (`--bs-primary` + `.dxbl-grid`).
  - `tokens.css` (2 bản docs + wwwroot): bỏ khối `--dx-*` chết; thêm token `--input-*`/`--font-body`;
    accent `--color-primary` đổi navy `#1E3A5F` → **xanh Fluent `#0F6CBD`** cho đồng bộ.
- **Đổi theme về sau = thay 1 file**: `accents/*.min.css` (11 màu: steel/storm/cool-blue… cho tông
  navy-ERP) hoặc `modes/dark.min.css`. KHÔNG override `--dxbl-*` thủ công nữa.
- Build RuntimeCheck **0 error**. Verify chạy thật OK (user xác nhận).
- **Docs:** cập nhật `docs/spec/09_ERP_DIRECTION.md` ADR-012 (theme Fluent + accent xanh, BE-004 đóng).

### Tồn đọng / lưu ý
- ADR-012 giờ accent **xanh Fluent** (không phải navy như ghi ban đầu). Nếu muốn tông navy-ERP →
  thay `accents/blue.min.css` bằng `steel`/`storm`/`cool-blue` (1 dòng).
- `tokens.css` navy cũ (`#1E3A5F`) đã thay xanh; các file khác (`README.md` design-system,
  `design-agent.md`) vẫn còn mô tả cũ — chưa rà.

## Session 44 (2026-06-10) — đã làm

### VIEW-3f — Test Grid + list views + bug fix DxGrid
- **Endpoint list views** `GET /api/v1/views`: `Features/Views/Queries/GetViewsList` (Query+Handler) → `IViewRepository.GetListAsync` (Dapper, `ROW_NUMBER OVER(PARTITION BY View_Code ...)` khử trùng code ưu tiên tenant-specific > global; join Sys_Table + Sys_Resource Title, đếm cột; search + paging). DTO `ViewListItem`. `ViewController` `[HttpGet]` list. Không cache (như Form list).
- **Trang `Pages/TestGrid.razor`** (`/test-grid`, link ở `MainLayout`): tải danh sách View → chọn → `GetInfoAsync` + `GetDataAsync` → render `DataView`. Có panel **debug** (in cột metadata + khóa data) + nút xem **JSON `/info` `/data`** (`ViewApiService.GetRawJsonAsync`) — **công cụ tạm, chưa quyết định giữ/gỡ**.
- **🐞 Bug `FilterRowCellVisible`**: thuộc tính KHÔNG tồn tại ở DX 25.2.3 → `DxGridDataColumn` ném `InvalidOperationException` khi set params → **rớt toàn bộ cột Data** (chỉ còn cột lệnh). Sửa thành **`FilterRowEditorVisible`**. Ảnh hưởng cả `/view/{code}`.

### VIEW-3f (grid UX) + 3f.1 (filter operator) — DataView
- Grid: `ColumnResizeMode=NextColumn`, `AllowColumnReorder`, `HighlightRowOnHover`, `FocusedRowEnabled`, `KeyboardNavigationEnabled`.
- Cột: `MinWidth`, **ghim `FixedPosition`** (helper `FixedOf` none/left/right), **sort mặc định** `SortIndex`+`SortOrder` (helper `SortOrderOf` asc/desc). Thêm 3 field `FixedPosition/SortOrder/SortIndex` vào `ViewColumnDto` (`/info` đã trả sẵn).
- Filter operator **Mức 1**: `FilterOpOf` (text→Contains, số/boolean→Equal) + `FilterMenuButtonDisplayMode=Always` cho user đổi operator runtime. Enum verify: `GridFilterRowOperatorType` (Contains/StartsWith/EndsWith/Equal/…).

### VIEW-4f — ConfigStudio WPF tab "Cột"
- `ViewManagerView.xaml`: thêm 4 cột chỉnh **MinWidth / Ghim(FixedPosition combo) / SortMặc định(SortOrder combo) / SortIdx(SortIndex)** + 2 array resource `FixedPositions`/`SortOrders`. Model `ViewColumnRecord` + `ViewDataService` (SELECT/INSERT/UPDATE) **đã lưu sẵn** — chỉ thiếu UI. **Web không cần sửa thêm** (đã consume 4 field). Build WPF 0/0.

### Tài liệu + memory
- `docs/reference/DEVEXPRESS_DXGRID_PROPERTIES.md` + `DEVEXPRESS_CONTROLS_PROPERTIES.md` — reflect DLL `DevExpress.Blazor.v25.2` v25.2.3 (DxGrid 113 prop + 32 control). Kỹ thuật: console net9 + `FrameworkReference Microsoft.AspNetCore.App` (PowerShell 5.1 không load được net8.0 DLL). `DxPopover` không tồn tại → `DxFlyout`.
- Memory mới: `feedback-always-ask-first`, `feedback-devexpress-verify-api`. **NGUYÊN TẮC SỐ 1** ở đầu CLAUDE.md.

### Tồn đọng / cần xử lý tiếp
- **Kiểm tra dữ liệu**: cột "Tên trình độ văn hóa" hiển thị `1/12`,`2/12` — nghi map nhầm Field_Name hoặc data thật.
- Quyết định **giữ/gỡ panel debug + nút JSON** ở TestGrid.
- **VIEW-3g** (lưu layout grid/user — localStorage vs bảng per-user+auth), **VIEW-3h** (filter operator Mức 2 metadata-driven — DB migration + cột WPF `Filter_Operator`).
- Build: `ICare247.slnx` **0/0**, `ConfigStudio.WPF.UI.slnx` **0/0**.

## Session 43 (2026-06-09) — đã làm

### VIEW-4d — hoàn tất màn "Quản lý View" WPF (i18n + column picker)
- **`ViewManagerViewModel`**: inject thêm `IFieldDataService` (nạp `Sys_Column`) + `IDialogService` (mở popup). Thêm 5 command:
  - `OpenTitleI18nCommand` / `OpenExportFileNameI18nCommand` / `OpenColumnCaptionI18nCommand` (cột đang chọn) / `OpenActionLabelI18nCommand` (action đang chọn) — mở `I18nEditorDialog` (tái dùng); **tự sinh key** theo convention `{tableCode}.view.{viewCode}.{suffix}` (spec 10 §1d: `title` / `export.filename` / `col.{field}.caption` / `action.{code}.label`) khi field key đang trống, rồi popup tự lưu Sys_Resource mọi ngôn ngữ.
  - `BrowseColumnCommand` — mở `ColumnPickerDialog` (tái dùng), nạp lười `AvailableColumns` theo `EditTable.TableId` (cache `_columnsLoadedForTableId`); chọn cột → set `FieldName`+`ColumnId` cho cột đang chọn (tạo dòng mới nếu chưa chọn).
- **`ViewManagerView.xaml`**: nút 🌐 cạnh Title_Key + Export_File_Name_Key (DockPanel); toolbar tab Cột thêm "🔍 Chọn cột" + "🌐 Dịch caption"; toolbar tab Actions thêm "🌐 Dịch nhãn".
- **Build**: `ConfigStudio.WPF.UI.slnx` **0/0**. Commit `2c314b3`, `c7db9ae`, `6266e73`, `4e79639`.

### VIEW-4e — polish UX màn Quản lý View (cùng session 43)
- **View_Code = `{View_Type}_` + hậu tố**: tách `EditViewCodeSuffix` + `ViewCodePrefix`, `EditViewCode` computed; badge tiền tố + dòng preview. Đổi View_Type giữ hậu tố. **Đổi View_Code tự rekey** mọi i18n key đã sinh qua `RekeyForViewCodeChange` (thay `.view.{cũ}.`→`.view.{mới}.` ở Title/Export + Caption/ExportCaption/CellTemplate cột + Label/Tooltip/Confirm action); guard `_suppressRekey` khi nạp/reset.
- **Nút lưu** đổi nhãn "Lưu" (bỏ "Tạo View/Cập nhật View"); **"Tạo mới"** thêm `MessageBox` cảnh báo Yes/No trước khi xóa trắng.
- **Thứ tự tab Cơ bản**: ① View_Type → ② View_Code → ③ Bảng nguồn → ④ Source (View_Type trước vì quyết định tiền tố).
- **Caption_Key/Label_Key** trong grid Cột/Actions: đổi thành cột i18n **khóa gõ tay** + nút **🌐 mỗi dòng** (`OpenColumnCaptionI18nRowCommand`/`OpenActionLabelI18nRowCommand`, DelegateCommand<record> + CellTemplate `RowData.Row`).
- **ColumnPickerDialog multi-select**: model `ColumnPickItem` (bọc DTO + IsSelected/IsAlreadyUsed); VM 2 chế độ (param `multiSelect`/`usedColumns`, trả `selectedColumns` list hoặc `selectedColumn`); XAML checkbox + badge "đã thêm" + nút "Chọn (N)". **Giữ tương thích single-select màn FieldConfig** (mặc định). Caller View truyền multiSelect=true + cột đã dùng → thêm nhiều dòng 1 lần.
- **GridSplitter** kéo co giãn 2 panel master-detail (MinWidth trái 280 / phải 420).
- **Build**: `ConfigStudio.WPF.UI.slnx` **0/0** (full solution, sau khi đóng app).
- **Hết phần WPF cho cụm View** (VIEW-4a→4e done).

### VIEW-1a + VIEW-2 backend (cùng session 43) — Claude làm thay Codex
- **VIEW-1a**: `db/031_create_ui_view.sql` idempotent (3 bảng theo spec 14) — bổ sung repo (user đã chạy DDL trên DB dev). Commit `b76036f`.
- **VIEW-2a** Domain: `Entities/View/ViewMetadata` + `ViewColumn` + `ViewAction` (text i18n resolve sẵn).
- **VIEW-2b** `IViewRepository`/`ViewRepository` (Dapper Config DB): `GetByCodeAsync` header+cột+action, resolve Sys_Resource theo langCode, ưu tiên tenant-specific > global (`ORDER BY Tenant_Id DESC`). DI đăng ký scoped. `CacheKeys.View`.
- **VIEW-2d/2e** (metadata): `Features/Views/Queries/GetViewByCode` (cache-aside qua `ICacheService`, mirror `GetFormByCode`) + `ViewController` GET `api/v1/views/{code}/info` (header X-Tenant-Id).
- **VIEW-2c**: `IConfigCache.GetViewAsync` (cache-aside L1+L2, `CacheKeys.View`) + `InvalidateViewAsync`; inject `IViewRepository` vào `ConfigCache`; `GetViewByCode` handler ủy quyền facade (đúng ADR-014, bỏ ICacheService trực tiếp).
- **VIEW-2d**: `Features/Views/Queries/GetViewData` — nạp metadata qua facade → `ViewRepository.GetDataAsync` SELECT cột Data (Field_Name whitelist regex) từ bảng nguồn (Data DB, resolve Sys_Table.Schema_Name+Table_Code), search LIKE CAST-NVARCHAR + OFFSET/FETCH. Source ≠ Table → NotSupportedException.
- **VIEW-2e**: `ViewController` GET `{code}/info`, GET `{code}/data`, POST `{code}/invalidate-cache`. Export server-side (pdf/docx) **hoãn** (chưa có template engine).
- **Build**: `src/backend/ICare247.slnx` **0 error** (2 warning DevExpress license pre-existing).
### VIEW-3 Blazor runtime (cùng session 43)
- **VIEW-3a**: `ViewApiService` + DTO (`ViewMetadataDto`/`ViewColumnDto`/`ViewActionDto`/`ViewDataResultDto`) gọi `api/v1/views/{code}/info` + `/data` (unwrap JsonElement → CLR). Đăng ký DI Program.cs.
- **VIEW-3b**: `Components/View/DataView.razor` render `DxGrid` (Grid) / `DxTreeList` (TreeList theo Key/Parent field); cột Data theo Order_No, command column Sửa/Xóa khi có Edit_Form. `Pages/View/ViewPage.razor` route `/view/{ViewCode}` (search + Add/Edit/Delete điều hướng route `/master/{editForm}/edit`).
- **VIEW-3c**: Render_Mode Text/Boolean/Html/**Image/Link/Badge** (RenderTreeBuilder trong `RenderCell(row,col)`) + **conditional format** `Style_Rule_Json` — format JSON đơn giản client-eval `[{when:{field,op,value}, style:{color/background/fontWeight}}]`, ops `= != > >= < <=` (số ưu tiên, fallback chuỗi), rule đầu khớp thắng; cache parse theo ViewId. DTO thêm `ViewColumnDto.StyleRuleJson`. CSS `.dv-badge/.dv-cell-img`. Template → fallback text (token chưa render).
- **VIEW-3d**: toolbar render nút động từ `Ui_View_Action` (Scope Toolbar/Both, Order_No) — Export→client, BuiltIn add/refresh→callback, Navigate→Target; row Sửa/Xóa qua Edit_Form. Print/Event/Api/export-server → `OnUnhandledAction` báo chưa hỗ trợ (ViewPage `_notice`).
- **VIEW-3e** (một phần): export client xlsx/csv qua `DxGrid.ExportToXlsxAsync/ExportToCsvAsync` (giá trị thuần theo FieldName); pdf/docx Engine=Server → báo chưa hỗ trợ. DTO thêm `ExportFileName` (header) + `ExportFormat`/`ExportEngine` (action). CSS `.dv-toolbar/.dv-action/.md-list-notice`.
- **Build**: `ICare247.Blazor.RuntimeCheck.csproj` **0/0**.
### VIEW-1b + VIEW-1c (cùng session 43)
- **VIEW-1b**: `db/032_seed_default_views.sql` — seed 1 Grid view `Grid_{Form_Code}` / form active + cột từ `Show_In_List=1` (Field_Name=Column_Code, Caption_Key=Label_Key, Edit_Form=chính form). Idempotent. ⏳ cần chạy DB.
- **VIEW-1c**: spec 02 thêm 3 bảng `Ui_View*` (cuối module UI, ref spec 14 + migration 031/032).
- **Chưa làm**: VIEW-3c render Template token, VIEW-3e Allow_Export per-column + header langCode, VIEW-2e export server-side (pdf/docx), alias `/master/*`→view, E2E test với DB thật.

### Việc tiếp theo gợi ý
- E2E: seed 1 View (VIEW-1b) → mở `/view/{code}` Blazor xem render + export thật.
- VIEW-3c: render giàu (Image/Link/Badge) + conditional format Style_Rule_Json qua AST engine.

---

> Cập nhật: 2026-06-08 (session 42b — NumericBox locale format real-time)

## Session 42b (2026-06-08) — NumericBox locale format real-time

### NumericBox real-time thousand separator + locale format
- **Vấn đề:** `DxSpinEdit.DisplayFormat="N0"` chỉ format khi blur — khi đang gõ hiện raw số không có separator.
- **Fix:**
  - `index.html`: thêm `icare.setupNumericInput(inputId, locale)` — JS listener `input` event, format real-time giữ cursor đúng vị trí.
  - `NumericBoxRenderer.razor`: inject `IJSRuntime`, gọi JS sau mỗi render. `DxSpinEdit` thêm `Culture` param. Prop `locale` trong `NumericBoxProps` (`""` = en-US, `"vi"` = vi-VN).
  - Bỏ `UseThousandSeparator` (luôn format). `DisplayFormat` luôn `N{d}`.
- **Kết quả:** `locale=""` → `9,999.05` real-time; `locale="vi"` → `9.999,05` real-time.
- **TODO:** đọc `locale` mặc định từ system config (CC-config-number-format, làm sau).
- Build 0/0. ✅

---

> (cũ) Cập nhật: 2026-06-09 (session 42 — WPF màn "Quản lý View" Grid/TreeGrid)

## Session 42 (2026-06-09) — đã làm

### Màn cấu hình Grid/Tree Grid trong ConfigStudio WPF (VIEW-4a/4b/4c) — Claude làm thay Codex
- **Core/Data**: `ViewRecord` (header Ui_View), `ViewColumnRecord` + `ViewActionRecord` (BindableBase — editable inline trong GridControl), `ViewDetailRecord` (header+cột+action), `ViewUpsertRequest`.
- **Core/Interfaces**: `IViewDataService` (GetViews / GetViewDetail / SaveView / DeactivateView).
- **Infrastructure**: `ViewDataService` (Dapper, Config DB) — join Sys_Table lấy Table_Code; SaveView trong transaction (insert/update header optimistic-concurrency theo Version, xóa→ghi lại cột+action nguyên khối); `EnsureSchemaAsync` ném lỗi thân thiện nếu chưa có bảng Ui_View (migration VIEW-1 chưa chạy).
- **Modules.Forms**: `ViewManagerViewModel` (master-detail, dropdown Table/Form + literal options, AddColumn/Remove/MoveUp-Down, AddAction/Remove, Save/New/Deactivate, filter search+inactive) + `ViewManagerView.xaml` (DXTabControl 6 tab: Cơ bản/Hành vi/Export-Print/Cây/Cột/Actions; 2 lưới con editable với combo cell qua x:Array resource).
- **Wiring**: `ViewNames.ViewManager`; FormsModule `RegisterForNavigation`; App DI `IViewDataService→ViewDataService`; ShellViewModel thêm nav "Views (Grid/Tree)" dưới nhóm Forms.
- **Build**: `ConfigStudio.WPF.UI.slnx` **0/0**. (Đã dọn artifact stale: xóa `*_wpftmp.csproj` + obj/bin của Modules.Grammar — lỗi MC3074 DevExpress tag là do obj cũ, không liên quan code mới.)
- **Còn lại (VIEW-4d)**: nút 🌐 i18n cho Title/Caption/Label key (tái dùng I18nEditorDialog) + column picker từ Sys_Column. ⚠️ Màn cần migration `Ui_View` (VIEW-1) chạy trên DB mới hoạt động thật.

---

## Session 41 (cũ)

> Cập nhật: 2026-06-08 (session 41 — Master Data DxGrid + thiết kế Ui_View/ADR-015)

## Session 41 (2026-06-08) — đã làm

### 1. Lưới danh mục dùng DevExpress DxGrid + fix Is_Virtual (commit `1fb982b`, pushed)
- `MasterDataGrid.razor`: HTML table → `DxGrid`, cấu hình qua `MasterDataGridConfig` (cấp lưới: paging/selector, filter row, group panel, selection, summary đếm) + `MasterDataColumnDto` mở rộng (Width/Align/DisplayFormat/AllowSort/Filter/Group). Cột động (Dictionary) đọc qua `CellDisplayTemplate` + `AsRow`.
- **Fix bug:** `MasterDataRepository.GetFormInfoAsync` thêm `AND uf.Is_Virtual = 0` — trước đây virtual field có `Column_Id` lọt vào cột lưới/list/save.
- `MasterDataApiService.GetListAsync`: unwrap `JsonElement` → kiểu CLR (long/decimal/bool/string) để DxGrid sort/filter/format đúng kiểu.
- `MasterDataListPage` truyền `_gridConfig`. Build Blazor + slnx 0/0.

### 2. Thiết kế Ui_View — cấu hình hiển thị danh sách tách khỏi form sửa (commit `8dad2ea`, pushed)
- **3 bảng** (Config DB): `Ui_View` (header + datasource + hành vi + export/print + TreeList), `Ui_View_Column` (cột + render/export/format + conditional), `Ui_View_Action` (nút toolbar/row).
- Quyết định: display ≠ edit; 1 bảng → N view; Grid+TreeList dùng chung; mọi text qua i18n scope `table_code`; **render giàu ≠ dữ liệu xuất** (export lấy giá trị thuần); pdf/docx server-side template, xlsx/csv DxGrid client.
- Tài liệu: `docs/spec/14_VIEW_CONFIG_SPEC.md` (DDL đầy đủ), **ADR-015**, `docs/spec/10` §1d View Keys, `AI_HANDOFF.md` VIEW-0 (→ Codex), TASKS.md roadmap VIEW-0→VIEW-4c.

### Trạng thái
- Cả 2 commit đã push lên `origin/master` (`49738e7..8dad2ea`).
- **VIEW-0 Done**; đường tới hạn: Codex chạy VIEW-1 (migration + seed view mặc định) → handoff → Claude vào VIEW-2 (backend).

---

## (Session trước) Cập nhật: 2026-06-07 (session 39 — Tab tier + i18n popup + Layout config + Unique check + ConfigCache design)

## Trạng thái cuối session
- **Branch:** `master`
- **Build:** ⚠️ CHƯA verify (user tự build). Nhiều thay đổi WPF + backend + Blazor.
- **Migrations CHƯA chạy:** `db/025` → `db/030` (xem dưới) — phải chạy trên DB thật.

## Đã làm (session 39)

### 1. Spec resource key (docs/spec/10)
- Convention i18n cho **Form/Tab/Section title**: `{table_code}.form|tab|section.{code}.title` + `sys.val.unique`.

### 2. Tầng Tab cho FormEditor (full-stack WPF)
- Quyết định: KHÔNG dựng tree 3 tầng (đụng ~50 chỗ `Sections.SelectMany`). Thay bằng **TabItem "📑 Tabs" riêng** (master-detail) + dropdown "Thuộc Tab" trong panel Section.
- DTO `TabDetailRecord`/`TabUpsertRequest`; `IFormDetailDataService` Get/Upsert/DeleteTab; `FormTabItem`; FormEditorViewModel + View. Clone form copy Ui_Tab (CloneTabsAsync + remap Tab_Id).

### 3. I18nEditorDialog (popup i18n dùng chung) — Modules.I18n
- `I18nValueRow` + `I18nEditorDialogViewModel` + `I18nEditorDialog.xaml`; `ViewNames.I18nEditorDialog`; RegisterDialog.
- Nút 🌐 Dịch tích hợp: Section, Tab, Field (Label/Placeholder/Tooltip/Required), Event SHOW_MESSAGE (structured editor `ActionItemDto` messageKey/severity).
- Form title i18n (`Ui_Form.Title_Key`) — backend resolve → FormMetadata.FormName → dialog "Thêm mới: {tên}".

### 4. Layout form per-form (`db/027`)
- `Ui_Form.Max_Width` + `Form_Columns`; backend FormMetadata + FormRunner áp `max-width` + `--form-cols` (responsive giữ qua `min()`). WPF card "BỐ CỤC HIỂN THỊ".

### 5. Blazor FormRunner/MasterData
- 1 section → render phẳng; ≥2 → card group. Default ColSpan = Half.
- **Fix bug:** LookupAddDialog render label 2 lần → bỏ label thủ công.

### 6. Chống trùng mã (Is_Unique) — full-stack (`db/029`)
- Cờ `Ui_Field.Is_Unique` + toggle "🔑 Duy nhất" + section "Thông báo khi trùng (i18n)" trong FieldConfig.
- Backend check 2 đường: MasterData (`ExistsValueAsync`) + Lookup add-new (`DuplicateValueException`).
- Message i18n: handler **resolve key→text server-side** qua `IResourceRepository` (key `{table}.val.{column}.unique`, fallback `sys.val.unique`), default `vi`. Auto-tạo key khi lưu field (RegisterI18nKeysAsync vi+en).
- Chuẩn hóa UI: 5 section i18n key cùng layout (input → nút dưới → preview dưới).

### 7. Thiết kế ConfigCache (ADR-014) + roadmap — CHỈ TÀI LIỆU
- `IConfigCache` facade đọc config qua cache (L1/L2); web/handler cấm chọc repo config trực tiếp. Invalidation: Version-stamp + Event + TTL → ADR-014.
- **Làm rõ kiến trúc:** RuntimeCheck chỉ là **Blazor WASM test client** gọi API; IConfigCache nằm tầng **Application (backend)**, viết 1 lần dùng chung cho mọi web app qua API. Web app thật = thêm 1 client.
- Roadmap trong TASKS.md: ConfigCache (CC-0a→CC-4) + tách `ICare247.ApiClient` SDK (SDK-1→4).

## Session 40 (2026-06-07) — đã làm
- ✅ Chạy migrations 025→030 trên DB thật (029 đã ổn — `ALTER ... ADD Is_Unique` trong batch riêng có `GO`, idempotent `IF NOT EXISTS`).
- ✅ Build verify backend + WPF: **0/0**. Sửa lỗi build commit 49738e7 — `InsertLookupCommandHandler.cs` CS0136 (biến `v` trùng scope catch vs method) → đổi tên `newValue`.
- ✅ Re-save field Is_Unique seed key i18n.
- ✅ **CC-0a**: tạo `IConfigCache` (Application/Interfaces) + entity `FormPermission` (Domain/Entities/Permission, deny-by-default).
- ✅ **CC-0b**: `ConfigCache` (Application/Engines) — form metadata ủy quyền `MetadataEngine`; resource map + lookup cache-aside; `ResolveKeyAsync` derive scope từ prefix key; key `ConfigResourceMap/ConfigLookup/ConfigPermission` gắn slot `:v{version}` (const 0). Permission tạm null (CC-3).
- ✅ **CC-0d (DI)**: đăng ký `IConfigCache→ConfigCache` scoped. Build backend 0/0.

- ✅ **CC-1a**: `InsertLookupCommandHandler` + `SaveMasterDataCommandHandler` bỏ inject `IResourceRepository`, resolve message trùng qua `IConfigCache.ResolveKeyAsync`. Build 0/0.
- ✅ **CC-0c**: helper `GetOrLoadAsync<T>` trong `ConfigCache` — stampede lock `SemaphoreSlim` per-key + negative cache `NegTtl=30s` cho kết quả rỗng. Áp cho resource map + lookup. Application compile 0/0.

- ✅ Full backend build `ICare247.slnx` verify **0/0** (đã stop API rồi build lại).

- ✅ Commit `98e699a` cụm CC-0/CC-1.
- ✅ **CC-2**: `GetLookupByCodeQueryHandler` delegate `IConfigCache.GetLookupOptionsAsync` (xóa dead `CacheKeys.Lookup`). Thêm `InvalidateLookupAsync` + endpoint `POST /api/v1/lookups/{code}/invalidate-cache`. Build 0/0.
- ✅ **CC-1b**: rà runtime i18n. Sửa 2 bug thật — `SaveMasterDataCommandHandler` (resourceMap null → lấy qua facade) + `EventEngine` TRIGGER_VALIDATION (thêm `FormCode`/`LangCode` vào `FormEvent`, inject `IConfigCache`). RuntimeController vốn đã OK. Build 0/0.

## Commits session 40
- `98e699a` — CC-0a/0b/0c/0d(DI) + CC-1a (facade nền tảng + dọn anti-pattern i18n).
- `47f1e8d` — CC-2 (lookup options qua facade) + CC-1b (sửa 2 bug i18n runtime).

> ⚠️ API đã bị **stop** để build verify — khởi động lại khi cần chạy app.

## ⏳ Việc cần làm ngay (đầu session sau)
1. **CC-3 (permission) — HOÃN**: chờ chốt schema bảng `Sys_Permission` (role/user × form × CRUD, tenant scope). Sub-task CC-3a→3d đã ghi trong TASKS.md. `GetFormPermissionsAsync` hiện trả null (deny-by-default), entity `FormPermission` là contract sẵn.
2. **CC-4** — version-stamp scale-out + WPF wiring invalidate (chỉ cần khi ≥2 instance).
3. Các việc tồn khác: BE-002 integration tests, BE-004 Design System tokens, E2E test Master Data với DB thật.

## Điểm vào việc tiếp theo
- **CC-0a** (nếu code ConfigCache): tạo `ICare247.Application/Interfaces/IConfigCache.cs` + record `FormPermission` — chỉ interface, build vẫn xanh. Xem TASKS.md roadmap ConfigCache + ADR-014.
- **SDK-1** (nếu dựng web app mới): tạo `ICare247.ApiClient` class lib, gom client + DTO; refactor RuntimeCheck dùng SDK.

## Migrations tích lũy cần có trên DB (017→030)
017 lock_on_edit · 018 is_virtual · 019 column_id_nullable · 020 field_code · 021 lookup_parent · 022 lookup_addnew · 023 display_mode · 024 show_in_list · **025 section .title** · **026 fix sys_language** · **027 form layout** · **028 form title_key** · **029 field is_unique** · **030 sys.val.unique**
