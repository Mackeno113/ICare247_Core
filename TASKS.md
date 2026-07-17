# ICare247 Core — Task Tracking

> 📦 Lịch sử hạng mục đã hoàn thành đã chuyển sang **[TASKS_ARCHIVE.md](TASKS_ARCHIVE.md)**
> (giảm context mỗi session). File này chỉ giữ việc **đang mở / đang làm** + roadmap còn dang dở.

## 📋 Roadmap — Shared Data Picker Controls (spec 31, session 87 — 2026-07-16, SPEC DRAFT chờ duyệt)

Control dữ liệu dùng chung 2 thế giới (bespoke RCL + engine template) — chi tiết `docs/spec/31_SHARED_PICKER_CONTROLS_SPEC.md`.
Đã chốt với user: làm SONG SONG 2 tầng · spec trước code sau · IcCompanyPicker 1 control 2 chế độ (Single/MultiCheck).

- [x] PICKER-P2 (2026-07-16): IcCompanyPicker 2 chế độ trong Shared RCL (`Components/Pickers/`) + ICompanyPickerSource (MeCompanyApiService cài, tự nạp khi không truyền Items) + refactor 3 chỗ tự chế (CompanySwitcher, tab Công ty màn Người dùng — NodeExtra cắm radio/badge, view Phạm vi công ty). Build UI 0W/0E; UI runtime chưa smoke (server đang tắt) — logic cây chuyển nguyên trạng từ code đã verify session 87
- [x] PICKER-P3 (2026-07-16): PickersController `GET /api/v1/pickers/dia-ban` (tỉnh / xã theo parentId+keyword+top / ?id= resolve) + IPickerRepository/PickerRepository (schema db/037, không có ALTER sau — SQL Server tắt nên chưa đối chiếu live) + Shared IDiaBanPickerSource + IcAddressBlock (địa chỉ + tỉnh + xã search debounce 300ms, tỉnh suy ra không lưu trùng) + host PickerApiService (cache L0 tỉnh). Build BE+FE 0W/0E; runtime chưa smoke (server tắt) — màn Phòng ban sẽ là khách hàng đầu tiên
- [x] PICKER-P4 (2026-07-16): db/083 Ui_Lookup_Template + Ui_Field_Lookup.Template_Code/Param_Map + seed 3 mẫu (TPL_CONG_TY qua fn_CongTyTheoQuyen db/084 — custom_sql bị chặn chuỗi con "IsDeleted"; TPL_NHAN_VIEN đợi NS_) · engine resolve template + Param_Map (field/@token/hằng số) + token tự resolve mọi @param thiếu qua IContextParamResolver + **cache key hash SAU khi bind đủ tham số** (token theo user phải vào key) + merge Param_Map→Reload_Trigger_Fields + fallback legacy khi tenant chưa migrate · ConfigSync descriptor · ConfigStudio WPF section "Mẫu lookup dùng chung" (combo + lưới map, IO phòng thủ). Build BE + WPF 0W/0E. CHƯA chạy 083/084 vào DB; CHƯA smoke runtime; hover-help topic cho section mới để sau
- [ ] PICKER-P5: nguồn nhan-vien + IcEmployeePicker (chờ đợt NS_NhanVien; ThoiDiem = tham số canonical, màn bind field ngày riêng)

## 🔴 Đang làm — REFACTOR FieldConfigViewModel (WPF, 4.030 dòng → tách dần; session 87 — 2026-07-16)

Kế hoạch 5 bước đã duyệt (phân tích 8 nhóm trách nhiệm + smell trong chat session 87; KHÔNG đổi hành vi/XAML):
- [x] **B1 — 3 service thuần** (commit `a3d63fa` + `3313e69` + B1.3): ControlPropsJsonService (schema/parse/coerce/BuildJson từ snapshot) · FieldI18nKeyService (sinh key spec 10 + orchestration ghi bản dịch, VM áp Result qua SetResolvedValue giữ non-dirty) · FieldConfigExplainService (sinh text diễn giải). VM 4.030 → 3.234 dòng. Build 0W/0E. Text/JSON output giữ nguyên từng ký tự
- [x] B2 — FieldNavigatorVm (commit `59e372f` + B2.2): cây field + bulk move + navigate tách nguyên trạng sang VM con (ctor nhận Func<Context> chụp ngữ cảnh root + Func<CancellationToken> + onLoaded→cascade); XAML/code-behind đổi sang `Navigator.*`, ủy quyền đã xóa sạch. VM 3.234 → 2.888 dòng. Build 0W/0E
- [x] B3 — FieldRulesEventsVm: 2 tab Rules/Events (danh sách + load + mở editor + xóa confirm + reindex) tách nguyên trạng; **fix smell async void ExecuteDeleteEvent → async Task** trong DelegateCommand; OperationCanceled ném tiếp để root abort chuỗi load như cũ; XAML 14 binding → `RulesEvents.*` cùng commit (binding không được compile-check nên không để trạng thái lệch). VM 2.888 → 2.740 dòng. Build module 0W/0E (app đang chạy → khoan build đè exe)
- [~] B4.1 — FkLookupConfigVm dạng FACADE (strangler): ủy quyền 1-1 ~65 member mà 2 panel bind về root + bridge notify re-raise cùng tên; LookupBoxPropsPanel + ComboBoxPropsPanel đổi `DataContext="{Binding FkLookup}"` (binding bên trong + Visibility + AncestorType=UserControl đều resolve qua VM con). XAML từ giờ TÁCH KHỎI root — B4.2+ dời state/logic (Fk*, template, control-props, thay lambda += bằng handler đặt tên) vào con theo nhóm, không đụng XAML nữa. Build module 0W/0E
- [ ] B4.2+ — dời state nhóm Fk*/Cb*/template/control-props từ root vào FkLookupConfigVm (từng nhóm 1 commit)
- [ ] B5 — phân rã ResetFieldStateForNew theo VM con
- Smoke thủ công sau mỗi bước: mở field TextBox/LookupBox-template/ComboBox → sửa → Lưu → mở lại đối chiếu

## ✅ Đã xong — Switcher công ty dạng cây + màn Người dùng + phân quyền cây công ty (session 87 — 2026-07-16, commit `e6bc0b8`)

**Mô hình đã chốt với user:** Vai trò (HT_VaiTro) = khái niệm nhóm duy nhất, gánh 2 trục kế thừa ĐỘNG:
chức năng (HT_VaiTro_Quyen, có sẵn) + phạm vi công ty (HT_VaiTro_CongTy, MỚI db/082). Quyền công ty
hiệu lực = gán riêng (HT_NguoiDung_CongTy) ∪ theo vai trò. Switcher chọn node = scope đúng node đó
(@CongTyID_Active đơn, không gộp nhánh). Cây checkbox WYSIWYG: tick cha auto-tick nhánh trên UI,
bỏ tick tự do, lưu đúng tập tick; bỏ tick con KHÔNG ảnh hưởng cha; công ty tạo sau không tự có quyền.

- [x] db/082_create_ht_vaitro_congty.sql (bảng map vai trò × công ty; node menu administration.users đã có sẵn từ db/045)
- [x] BE: MeCompanyRepository union gán riêng ∪ vai trò + trả ParentId/CanAccess (cây, tổ tiên disabled)
- [x] BE: AdminUserController /api/v1/admin/users (list/detail/create PBKDF2/update/reset-password/delete mềm/roles/companies) + feature Admin/Users + IUserAdminRepository/UserAdminRepository
- [x] BE: AdminPermissionController GET/PUT roles/{id}/companies + PermissionAdminRepository mở rộng
- [x] FE: CompanySwitcher.razor dropdown cây (indent, node CanAccess=false mờ, giữ localStorage/reload)
- [x] FE: UserManagementPage.razor (/m/administration/users) master-detail 3 tab: Thông tin / Vai trò / Công ty truy cập (cây WYSIWYG + radio mặc định + badge "Theo vai trò")
- [x] FE: PermissionMatrixPage thêm view "Phạm vi công ty" (cây WYSIWYG, 1 CTA Lưu chung)
- [x] Verify trên app thật (user restart server): login admin → màn Người dùng render + lưu gán công ty 204 "Đã lưu"; view Phạm vi công ty render; fix bug Dictionary key null khi dựng cây switcher (bắt được lúc verify)
- [x] Commit `e6bc0b8` (bỏ 3 file i18n theo quy tắc)
- [ ] **User chạy db/082 vào Data DB bằng SSMS** (chưa chạy thì nút Lưu ở view "Phạm vi công ty" lỗi; đọc vẫn OK nhờ OBJECT_ID guard)
- [ ] Verify mắt thường switcher cây trên header sau fix (user tự xem — browser tool bị từ chối)
- [ ] Test tạo user mới + đăng nhập bằng user thường (quyền hạn chế) — smoke test phân quyền end-to-end

## ✅ Đã xong — 3 bug runtime lộ khi chạy thật màn danh mục (session 81 — 2026-07-10, commit `b53329c` + `a302c37`)

Đều là bug **có sẵn** (không do ADR-035), lộ ra khi E2E LookupBox/MasterData lần đầu:

1. **`InsertAsync` — `CreatedBy` NULL** (nút "➕ Thêm mới" trên LookupBox). `DynamicLookupRepository`
   dựng INSERT thuần từ giá trị client, không bơm cột audit; mà `CreatedBy` NOT NULL không DEFAULT (db/061).
   Sửa: dò cột audit (`INFORMATION_SCHEMA`) → bơm `CreatedBy` (claim sub) + `CreatedAt`; chặn client tự gửi
   cột audit; tách schema khỏi `Source_Name`; nối `userId` từ controller → command → repo.
2. **`ReferenceCheckService` chặn nhầm MỌI thao tác xóa** ("Bị tham chiếu (giữ lại)"). Fallback đoán-theo-tên
   viết cho PK tên riêng; ADR-019 đổi PK thành `Id` → `@Pk='Id'` khớp mọi cột `Id`/`*_Id` toàn Data DB.
   Sửa: **GỠ HẲN** `GetCandidatesByNameConventionAsync`. Chỉ 2 nguồn tường minh: `Sys_Relation` + FK vật lý
   (`sys.foreign_keys`). Thực thi đúng lời ADR-019 "KHÔNG đoán theo tên".
3. **`PublishCheckService` — 3 query cột không tồn tại** (`Ui_Field.ColumnCode`, `Sys_Dependency.Source_Field_Code`,
   `Sys_Language.Is_Active`). Query #2 trong `catch` trần → check vòng lặp phụ thuộc CHƯA BAO GIỜ chạy.

**📌 Decisions Log:** Soft-check FK **chỉ** từ khai báo tường minh (`Sys_Relation` HOẶC FK vật lý trong Data DB) —
KHÔNG suy luận theo tên cột. Đây là **thực thi ADR-019**, không phải ADR mới.

**⏳ Verify (user):** build+restart API → thử thêm/xóa ngân hàng. ⚠️ `DM_NganHang` chưa khai quan hệ tới
`DM_ChiNhanhNganHang` → xóa sẽ mồ côi chi nhánh; muốn chặn thì thêm FK vật lý hoặc `Sys_Relation`.

## ✅ Đã xong — Bỏ hẳn cột `Tenant_Id` (ADR-035, session 81 — 2026-07-10, commits `904fbb3`/`83717b2`/`a302c37`/`ff7653b`)

**Chốt (user):** cô lập tenant ở **tầng connection**, không ở tầng cột. Đích = **0 bảng** có `Tenant_Id`,
cả Config lẫn Data DB. Giữ `TenantId` runtime (resolver + cache key). Quy tắc: `.claude-rules/database-design.md`.

**Khảo sát DB live (2026-07-10):** 9 bảng còn cột (`db/*.sql` + spec 02 KHÔNG phản ánh đủ).
`SoTenantKhacNhau = 1` mọi bảng → 1 Config DB = 1 tenant, tiền đề vững. `Sys_Table` 11/11 tenant-specific,
`Ui_View` 8/8, `Sys_Lookup` 13 global + 3 tenant (GENDER), `Sys_Config`/`Sys_Role`/`Doc_*` rỗng.
Không có va chạm `(Lookup_Code, Item_Code)` → drop cột không vỡ UNIQUE.

- [x] **TID-1 (backend)** — Gỡ 24 chỗ SQL ở 9 repository + `LookupItem.TenantId` + `ViewMetadata.TenantId`
      + `ViewInfoResponse.TenantId` + 8 doc-comment. Thu gọn CTE `ROW_NUMBER` thừa ở `LookupRepository`
      và `ViewRepository`. Build xanh, 145/145 test qua.
- [x] **TID-0 (bug)** — `LookupRepository` lọc `OR Tenant_Id = 0` trong khi db/009 đã đổi global sang `NULL`
      → **13 lookup global im lặng biến mất** (`HINHTHUC_2FA`, `LOAI_TAIKHOAN`, `TRANGTHAI_DONVI`,
      `TRANGTHAI_NGUOIDUNG`). Đã sửa cùng TID-1.
- [x] **TID-2 (ConfigStudio WPF)** — 9 file + 3 record DTO. Sửa luôn 2 bug lộ ra khi rà:
      - `SysLookupDataService` mang **đúng bản sao bug `Tenant_Id = 0`** (5 chỗ) → màn lookup cũng giấu 13 dòng global.
      - `PublishCheckService` (6 chỗ) truy vấn `Ui_Field.Tenant_Id` / `Sys_Dependency.Tenant_Id` — **cột không tồn tại**
        → `Invalid column name` lúc chạy. Bug có sẵn; gỡ predicate sửa luôn.
      - `FormDataService` (~1200 dòng): gỡ nhánh dò cột `useTenantFromForm/SysTable`; đổi tên
        `IsTableInTenantAsync`→`IsTableActiveAsync`, `ResolveTableIdForTenantAsync`→`ResolveDefaultTableIdAsync`.
      - Build WPF xanh.
- [x] **TID-3 (migration `db/078_drop_tenant_id.sql`)** — Guard (dừng nếu >1 tenant hoặc trùng khóa) → drop 5 FK
      → drop filtered index → drop DEFAULT → drop cột (9 bảng) → dựng lại UNIQUE thường + `IX_Sys_Lookup_Code`
      `(Lookup_Code, Is_Active)` → **DROP TABLE `Sys_Tenant`**. Toàn bộ trong **1 batch + 1 transaction + TRY/CATCH**
      (chèn `GO` giữa chừng sẽ khiến guard không chặn được các bước sau). Gỡ code `Doc_Template`/`Doc_Proc_Registry`
      cùng commit (cột `NOT NULL` không default). ✅ **ĐÃ CHẠY** trên Config DB (2026-07-10).
- [x] **TID-4 (spec)** — `docs/spec/02_DATABASE_SCHEMA.md` cập nhật: mục Tenant Isolation viết lại,
      `Sys_Tenant` đánh dấu ĐÃ DROP, gỡ 5 dòng cột + 4 cặp filtered index, sửa sơ đồ quan hệ.
      Cũng sửa `.claude-rules/dapper-patterns.md` (dòng 5–6 dạy ngược lại ADR-035).

- [x] **TID-5 (bug WPF — `PublishCheckService`)** — 3 query tham chiếu **cột KHÔNG tồn tại**, phát hiện khi rà TID-2:
      - `CheckLabelKeysAsync`: `SELECT ColumnCode FROM Ui_Field` → `Ui_Field` chỉ có `Column_Id`.
        Sửa: `COALESCE(Field_Code, Sys_Column.Column_Code, 'Field#<id>')` + LEFT JOIN (db/019 + db/020).
      - `CheckCircularDependencyAsync`: `SELECT Source_Field_Code, Target_Field_Code FROM Sys_Dependency`
        → bảng lưu `(Source_Type, Source_Id)`. Query nằm trong `catch` TRẦN nên **im lặng báo
        "Sys_Dependency chưa được build"** — check phát hiện vòng lặp CHƯA BAO GIỜ chạy. Sửa query
        (bám mẫu `DependencyRepository`) + thu hẹp `catch` về `SqlException.Number == 208`.
      - `CheckI18nCompletenessAsync`: `Sys_Language WHERE Is_Active = 1` → bảng không có cột đó.
      Build WPF xanh.

- [x] **TID-6 (dọn Sys_Lookup Manager)** — Màn hình **đã có sẵn** (View 296 dòng + VM 428 dòng, nav Alt+3),
      và đã chọn đúng hướng "code mới giữ ở client" (`ExecuteAddCode` không chạm DB — code chỉ tồn tại
      khi có ≥1 item). Ba thứ hở, đã xử lý:
      - `AddLookupCodeAsync` = tàn dư thiết kế cũ (`return exists == 0 || true;` → luôn `true`, không INSERT,
        0 caller). **Đã xóa** khỏi interface + impl.
      - `DeleteCodeCommand` khai báo nhưng **không nút nào trong XAML**, thân hàm chỉ hiện thông báo.
        User chốt "làm thật" → thêm `DeleteCodeAsync` (DELETE theo `Lookup_Code`), nút 🗑, dialog xác nhận
        (`MessageBox` YesNo — bám mẫu `FieldConfigViewModel`), đếm item **từ DB** chứ không từ `Items.Count`
        (`LoadItemsAsync` fire-and-forget → có thể còn rỗng lúc bấm ⇒ bỏ sót dòng thật).
      - `ExecuteAddCode` là `async void` không `await` gì → đổi về `void` (async void nuốt exception).
      Build WPF xanh.

---

## 🔜 CÒN LẠI sau ADR-035 — gom 1 chỗ (2026-07-10)

> Code đã sạch `Tenant_Id`, `db/078` đã chạy. Phần dưới là **những gì CHƯA làm**, xếp theo mức nguy hiểm.

### ✅ R-1 — 4 migration seed đã sửa (session 83 — 2026-07-14)

Đã gỡ tham chiếu `Tenant_Id` khỏi 4 file → chạy lại sau `db/078` không còn `Invalid column name`:

| File | Đã sửa |
|---|---|
| `db/032_seed_default_views.sql` | Bỏ cột `Tenant_Id` + `t.Tenant_Id` khỏi INSERT/SELECT; NOT EXISTS chỉ theo `View_Code` |
| `db/047_seed_ui_form_ht_vaitro.sql` | Bỏ `AND Tenant_Id IS NULL` (4 chỗ) |
| `db/065_config_grid_chinhanhnganhang_fk.sql` | Bỏ `ORDER BY CASE WHEN Tenant_Id IS NULL...` |
| `db/066_config_grid_chinhanhnganhang_autojoin.sql` | như trên |

> Tám file khác (`000`, `001`, `002`, `009`, `031`, `043`, `044`, `077`) **CREATE** cột — chạy trước 078 nên
> không vỡ. `002/009/031/043/044/077` (db/) chưa dọn (chỉ "dựng lên rồi phá đi", không gấp).
> `docs/migrations/000` + `001` (bản snapshot) đã dọn ở R-2.

**KHÔNG đụng:** `db/036_create_catalog.sql` (Catalog DB — `dbo.Tenant.Tenant_Id` là PK, hợp lệ) ·
`db/015_create_cf_data_schema.sql` (**tham khảo, không dùng** — user chốt để nguyên) ·
`db/029`/`037`/`056` (chỉ là comment).

### ✅ R-2 — 6 spec + 2 snapshot migration đã sửa (session 83 — 2026-07-14)

- `01_ARCHITECTURE.md` — viết lại mục Multi-tenant: cô lập ở tầng connection (resolver), **không** lọc SQL
  theo `Tenant_Id`; cache key vẫn giữ `TenantId` (Redis L2 dùng chung).
- `08_CONVENTIONS.md` — bỏ `f.Tenant_Id = @TenantId` + param `TenantId` khỏi mẫu Dapper + note ADR-035.
- `09_FIELD_CONFIG_GUIDE.md` + `12_CASCADE_LOOKUP_GUIDE.md` — gỡ `Tenant_Id = @TenantId` khỏi mọi ví dụ
  Filter SQL/Function/SELECT + gỡ `@TenantId` khỏi bảng tham số hệ thống + thêm cảnh báo "gõ vào sẽ lỗi runtime".
- `14_VIEW_CONFIG_SPEC.md` — DDL `Ui_View`: bỏ cột `Tenant_Id` + `FK_Ui_View_Tenant`; gộp 2 filtered index
  thành `UQ_Ui_View_Code`.
- `28_DOC_TEMPLATE_SPEC.md` — bỏ cột `Tenant_Id` khỏi `Doc_Template`/`Doc_Proc_Registry`, §13-A, tham số proc,
  whitelist filter.
- `docs/migrations/000_create_schema.sql` — **viết lại đầy đủ**: DROP `Sys_Tenant` + gỡ `Tenant_Id` (và `Is_Tenant`)
  khỏi `Sys_Table`/`Sys_Lookup`/`Sys_Role`/`Sys_Config` + gộp filtered index → unique thường. Header ghi rõ vẫn là
  snapshot 000–016 (chưa gồm 017+).
- `docs/migrations/001_seed_all.sql` — gỡ block seed `Sys_Tenant` + cột `Tenant_Id` khỏi MERGE `Sys_Lookup` GENDER.

> ⚠️ Chưa build/E2E (chỉ là docs + migration seed chưa chạy lại). Không có code C# thay đổi.

### 🧊 R-3 — HOÃN: `Sys_Menu` / `Sys_MenuCatalog` — giàn giáo cho pha nâng cấp menu

**Chốt (user, 2026-07-10):** menu server-driven **chưa áp dụng** vào project hiện tại; nó đến từ
**project cũ đang chờ nâng cấp lên**. Không xử lý gì trong pha này.

> Ghi lại đầy đủ vì đã phải tra 2 lần: hai bảng này **không phải bảng rác**, cũng **chưa phải tính năng
> đang chạy**. Chúng là **định nghĩa gốc (master) của cây chức năng / menu**, dựng sẵn chờ code đồng bộ.

#### Hai bảng chứa gì

**`Sys_Menu` = một *bộ* menu.** `Menu_Code` (`MAIN`, `MOBILE`…), `Menu_Type` (Sidebar/Top/Mobile/Context).
Seed đúng 1 dòng: `MAIN` — "Menu chính", Sidebar. Tồn tại để sau này có nhiều bộ menu song song
(sidebar web, menu mobile, thanh trên), mỗi bộ một cây riêng.

**`Sys_MenuCatalog` = cây chức năng base** thuộc một bộ menu. 45 dòng seed = bộ khung ứng dụng:

```
dashboard (/)                    ManHinh
group.operations "Vận hành"      Menu
  ├─ organization /m/organization   TC
  ├─ hr           /m/hr             NS
  └─ payroll      /m/payroll        TL
group.business   "Kinh doanh"    Menu
group.system     "Hệ thống"      Menu
devtools         /dev/forms      ManHinh
...
```

Cột: `Func_Code` (khóa nghiệp vụ ổn định) · `Func_Name` · `Parent_Code` · `Func_Type` (Menu/ManHinh/
ChucNangCon) · `Module` (`TC`/`NS`/`TL`/`TM`/`CN`/`BC`/`HT` — đúng bộ tiền tố ADR-022) · `Route` · `Icon` ·
`Display_Pos` · `Display_Order` · `Default_Enabled` · `Version`.

#### Vì sao tồn tại (ADR-023) — tách ĐỊNH NGHĨA khỏi PHÂN QUYỀN

| | Định nghĩa | Phân quyền |
|---|---|---|
| Ai làm | DEV / builder | Admin của tenant |
| Công cụ | ConfigStudio WPF | Web, qua API |
| Ghi vào | **Config DB** — `Sys_Menu`, `Sys_MenuCatalog` | **Data DB tenant** — `HT_VaiTro_Quyen` |
| Thấy gì | route, icon, cấu trúc cây | chỉ Tên + tick Xem/Thêm/Sửa/Xóa/In |

DEV định nghĩa "hệ thống có màn nào, route gì, icon gì" **một lần ở master**. Khi provision/nâng cấp 1 tenant,
cây đó **UPSERT theo `Func_Code`** xuống `HT_ChucNang` (Data DB của tenant). Admin tenant chỉ tick quyền trên
chức năng đã có, **không bao giờ thấy** route hay API.

Hai chi tiết cho thấy bảng được nghĩ cho **đồng bộ**, không phải để đọc trực tiếp:
- Cây khai bằng **`Parent_Code`, KHÔNG phải `Parent_Id`** — `Id` mỗi tenant một khác, chỉ code là bền.
- `Default_Enabled` = giá trị khởi tạo `HT_ChucNang.KichHoat`; `Version` để lần sync sau biết bản nào mới.
  `db/042` đã thêm sẵn vào `HT_ChucNang` 4 cột khớp cặp: `Menu_Id`, `LaHeThong`, `KichHoat`, `ViTriHienThi`.
  **`LaHeThong` đóng vai trò y hệt `Is_System` của ConfigSync**: bản gốc từ master → re-sync ghi đè;
  bản tenant tự thêm → không đụng.

#### Trạng thái thật

- **Schema xong:** `db/042` (mở rộng `HT_ChucNang`) · `db/043` (tạo 2 bảng) · `db/044` (seed 45 dòng) ·
  `db/045` (seed `HT_ChucNang` base). Đã chạy — bảng và dữ liệu có thật (`Sys_Menu` 1 dòng,
  `Sys_MenuCatalog` 45 dòng, **toàn global, 0 override**). `db/078` gỡ cột `Tenant_Id` khỏi chúng, không mất gì.
- **Phía TENANT đã code xong** (đừng nhầm là chưa làm gì): `NavMenu.razor` nạp menu từ API `/me/navigation`
  (server-driven, đã lọc theo quyền, đọc `HT_ChucNang`); `AppNav.cs` chỉ là **fallback tĩnh khi API rỗng/lỗi**.
  Có cả màn **Menu Builder** sửa cây `HT_ChucNang` qua `MenuAdminController` (`/api/v1/admin/menu/*`)
  + `MenuAdminApiService`, và màn Phân quyền dùng `HT_VaiTro_Quyen`.
- **Thứ DUY NHẤT thiếu = tiến trình đồng bộ master → tenant** (`Sys_MenuCatalog` → `HT_ChucNang`, UPSERT theo
  `Func_Code`). Vì vậy 2 bảng master có **0 tham chiếu C#** — grep không ra là do đây, KHÔNG phải bảng thừa.
- ⚠️ **Mâu thuẫn spec — chưa cần quyết, vì chưa code nào phụ thuộc câu trả lời:**
  `15_AUTHZ_NAVIGATION_SPEC.md` §4 vẽ hai bảng ở "Config DB dùng chung";
  `16_CONFIG_SYNC_SPEC.md` §1 nói `ICare247_Master` (catalog) mới giữ `Sys_MenuCatalog`.
  Chốt khi bắt tay vào pha nâng cấp.

### 🧹 R-4 — Nợ nhỏ

- `Sys_Resource` mồ côi: `DeleteItemAsync` lẫn `DeleteCodeAsync` đều KHÔNG xóa `Label_Key` tương ứng.
  Nếu muốn dọn thì làm 1 task cho cả hai đường xóa (đừng sửa lệch nhau).
- 4 file i18n (`catalog.json` ×2, `en.json`, `i18n-report.md`) dirty từ trước session 81 — chưa ai commit.

---

## 📌 Trạng thái triển khai ADR — **NGUỒN SỰ THẬT DUY NHẤT**

> **`architecture_decisions.md` KHÔNG còn dòng `Status:`.** ADR = bản ghi **quyết định bất biến** (có ngày,
> không sửa). Trạng thái triển khai đổi theo từng commit ⇒ chỉ sống ở đây. Sửa 1 nơi, không phải 2.
>
> **Vì sao đổi:** rà 2026-07-10 thấy **8/18 dòng `Status:` đang SAI**, tất cả lệch cùng một hướng — nói ít
> hơn thực tế ("chưa code" trong khi code đã chạy). Nguyên nhân: ADR trộn phần bất biến với phần biến động;
> không ai dám "sửa ADR" nên dòng Status hóa thạch. `"chưa code"` là **khẳng định phủ định** — lúc viết thì
> đúng, và khi code xuất hiện thì không có gì buộc ai quay lại. **Đừng viết trạng thái vào ADR nữa.**

**Đã verify bằng code ngày 2026-07-10** (✅ = có file/symbol thật · ⚠️ = ADR ghi sai, đã đối chiếu):

| ADR | Chủ đề | Trạng thái thật | Neo code / còn lại |
|---|---|---|---|
| 015 | `Ui_View` tách khỏi form | ⚠️ ADR ghi *"triển khai chưa bắt đầu"* → **đã xong** | `ViewRepository`, `ViewDataService`, `Ui_View` 8 dòng |
| 016 | Filter panel lưới | ✅ xong | `Components/View/FilterPanel.razor` |
| 018 | DB-per-tenant + catalog | ⚠️ ADR ghi *"chưa code"* → **đã code** | `TenantConnectionResolver`, `TenantConnectionProtector` |
| 019 | Convention Data DB | ⚠️ ghi *"chờ thiết kế Data DB"* → **đã thiết kế** | Spec 11 + `db/037`; soft-check FK = 2 nguồn tường minh (`Sys_Relation` + FK vật lý `sys.foreign_keys`), **đã gỡ fallback đoán-theo-tên** (session 81) |
| 020 | Audit-log JSON diff | ⚠️ ghi *"chưa code"* → **code rồi, MỘT PHẦN** | ✅ `AuditNkWriter`/`AuditQueue`/`AuditBackgroundService`/`HttpAuditWriter` · ❌ **cờ `Audit_Enabled` bật/tắt theo bảng+màn CHƯA có** |
| 021 | Scale-out + file storage | ⚠️ ghi file storage *"backlog, chưa code"* → **đã code** | `AddDataProtection` (Program.cs) · `Infrastructure/Files/`: `DbFileStore`, `FileSystemFileStore`, `ObjectFileStore`, `FileStoreSelector` |
| 022 | Tiền tố bảng Data DB | ⚠️ ghi *"chưa có bảng nào ngoài NK_*"* → **đã tạo** | `db/037`: `DM_QuocGia`, `DM_TinhThanhPho`, `TC_*`, `HT_*` |
| 023 | Menu động + phân quyền | ⚠️ ghi *"chưa code"* → **phía tenant xong** | ✅ `NavMenu.razor` ← API `/me/navigation` · `MenuAdminController` · `AdminPermissionController` · ❌ **đồng bộ `Sys_MenuCatalog` → `HT_ChucNang` CHƯA có** (xem R-3) |
| 025 | ConfigSync master→tenant | ⚠️ ghi *"chưa code"* → **đã code** | `ConfigSync/ConfigSyncService.cs`, `ConfigSyncTables.cs`, `db/050` |
| 028 | — | ✅ code xong, compile xanh | build fail chỉ do file-lock (app đang chạy) |
| 029 | Save/validation hook | ✅ code xong | ⏳ E2E `SVHOOK-6` (chạy SQL) |
| 030 | Context param `@NguoiDungID` | ✅ cấu hình xong | ⏳ chạy `db/059`+`db/060`; chạy lại 2 proc `spc_`/`sp_AfterSave_Grid_DM_PhuongXa` |
| 031 | Theme ConfigStudio WPF | ⏳ build PENDING | rủi ro: editor mất template nếu DX theme phản ứng → `BasedOn` style mặc định DX |
| 032 | MasterData path | ✅ code xong | ⏳ restart API + hard-reload WASM. `DM_CHINHANHNGANHANG` thiếu PK (user chưa cho sửa DB) |
| 033 | View-based grid | ✅ Pha 1 xong (live Tenant 1) | Pha 2 → ADR-034; **Pha 3 (template) chưa làm** |
| 034 | Import Excel | ✅ code xong (session 78) + addendum DevExpress (session 80) | ⏳ `db/071–073` + `db/procs/*` CHƯA chạy DB; E2E ⏳; v1 = Grid phẳng, TreeGrid sau |
| 035 | Bỏ hẳn `Tenant_Id` | ✅ **HOÀN TẤT** (session 81) | `db/078` đã chạy. Còn lại → mục "🔜 CÒN LẠI sau ADR-035" |
| Sec | CORS + JWT SecretKey | ✅ #2/#3 code xong (Program.cs) | #1 (tenant từ claim) đã đóng bằng ADR-018 |

> **Quy tắc từ nay:** khi một thay đổi làm ADR nào đó "bắt kịp", cập nhật **bảng này**, không sửa ADR.
> `/finish-task` có bước nhắc. Nếu buộc phải nhắc tới code trong ADR, **trỏ vào symbol/file cụ thể**
> (kiểm được bằng grep), tuyệt đối không viết "chưa code" (không kiểm được, và sẽ mục).

## ✅ Đã xong — Import: đổi thư viện Excel ClosedXML → DevExpress Spreadsheet (session 80 — 2026-07-09, commits `c48bbc5`/`ac1bd27`/`4257491`, ĐÃ push)

**Chốt (user):** đồng nhất 1 thư viện Office (DevExpress) cho cả in biểu mẫu lẫn import, **cô lập giống in biểu mẫu**; chấp nhận watermark trial + license → mua Universal sau. Đảo điểm 1 ADR-034 (có addendum).
- Seam `ISpreadsheetReader`+`SheetGrid` (Application, 0-based lưới ô thuần) → impl DevExpress `Spreadsheets/SpreadsheetReader` + `ImportTemplateBuilder` ở `Infrastructure.Documents` (project DevExpress DUY NHẤT). `ImportEngine` ở `Infrastructure` KHÔNG tham chiếu DevExpress — chỉ dùng seam.
- Gỡ hẳn ClosedXML (package + builder cũ). DI: `AddDocuments` đăng ký reader+builder. API verify reflection. Build backend **0 error** (chỉ warning license trial DX1000/DX1001).
- **Fix theo test template (user):** (1) ghi chú trống — `Comments.Add(range,string)` là AUTHOR không phải nội dung → dùng `Add(range,author,text)` (`ac1bd27`). (2) dropdown FK hiện **"Mã — Tên"** (cột nối ở sheet phụ); import cắt lấy Mã qua `ImportConventions.ExtractFkCode` (có nối→cắt; không→cả ô là Mã) (`4257491`).

**⏳ Còn:** E2E (validate/commit .xlsx thật); dọn header Spec 25 (còn ghi "ClosedXML").

## ✅ Đã xong — Dọn nợ tách RCL: chuyển LookupAddDialog vào RCL (session 80 — 2026-07-09, commit `417a8fe`, ĐÃ push)

Nợ sót session 79: `LookupBoxRenderer` ở RCL nhưng `LookupAddDialog` còn ở host → thẻ render literal (RZ10012), nút "➕ Thêm mới" hỏng. Chuyển dialog + CSS scoped vào RCL; ẩn phụ thuộc host sau `ILookupQueryService.GetAddFormAsync` → `LookupAddForm(Title, List<FieldState>)` (host dựng FieldState + options). Build web **0 warning 0 error**. ⏳ E2E: mở LookupBox có `AddFormCode` xác nhận dialog bung.

## ✅ Đã xong — Doc Template GĐ4: gắn mẫu vào màn lưới (session 80 — 2026-07-09, commit `1c2dad2`, ĐÃ push)

**Cơ chế (grid-first, KHÔNG thêm bảng):** gắn mẫu = 1 dòng `Ui_View_Action` (`Type=Export|Print`, `Engine=Server`, `Export_Format=docx|pdf`, **`Target=Doc_Template.Ma`**, `Require_Selection`). Bấm nút → dòng đang chọn làm `keyParams` → `Doc_Template_Param(Nguon='key')` ánh xạ @proc.
- BE: `GetTemplateIdByCodeAsync` + `RenderByCodeAsync` + `POST /doc-templates/by-code/{code}/render`.
- Web: `DocTemplateApiService` + `DataView.TryServerRenderAsync` → `ViewPage.OnServerRenderAsync` (render+tải).
- ConfigStudio: combo "Bộ mẫu (Xuất tài liệu)" ở tab Actions điền `Target`.
- Docs: guide `cau-hinh-xuat-tai-lieu.md` + Spec 28 §7.4. Fix build web `293182f` (sao dòng chọn sang Dictionary — IReadOnlyDictionary).

**⏳ Còn:** E2E (chạy `db/077` + đăng ký `Doc_Proc_Registry`/`Doc_Template_Param` bằng SQL + soạn mẫu → xuất). Pha sau: `Ui_Form_Action` (nút trên form chi tiết), `Scope='Row'`, in hàng loạt (§13-D).

## ✅ Đã xong — Xuất Word/PDF theo mẫu (Doc Template) + tách RCL control động (session 79 — 2026-07-09, ĐÃ commit + push)

**Bối cảnh:** hỏi–chốt nhiều vòng về DevExpress Office File API (license/deploy IIS/trial) → dùng cho xuất hợp đồng Word/PDF. Kèm tách RCL control động (dọn nợ nhân bản RuntimeCheck).

### Tách RCL control động (commit `fb26e33`)
- Rút `FieldRenderer` + 11 renderer + `FieldState`/models sang RCL mới **`ICare247.UI.DynamicForms`** (dùng chung host + Portal tương lai); interface hóa `ILookupQueryService`/`IAttachmentApiService` (impl ở lại host, DI). **Xóa hẳn project mồ côi `ICare247.Blazor.RuntimeCheck`**.

### Doc Template — backend GĐ1 (commits `0542e5d`, `79d2818`, `4609752`)
- Spec `docs/spec/28_DOC_TEMPLATE_SPEC.md` + migration `db/077` (4 bảng Config DB: `Doc_Template`/`_Detail`/`_Proc_Registry`/`_Param` — ⏳ CHƯA chạy).
- Kiến trúc **ghép-fragment**: master A4 dọc + N detail A4 ngang → 1 file (đổi hướng giấy per-section: AppendSection + set Landscape).
- **`ICare247.Infrastructure.Documents`** (project DevExpress DUY NHẤT backend, gói `DevExpress.Document.Processor 25.2.4`): engine merge+ghép+PDF + repo Config DB + proc runner (whitelist) + `IDocTemplateRenderer`. API `GET describe` + `POST {id}/render`.
- **PoC runtime OK**: xuất PDF 2 trang dọc/ngang, tiếng Việt chuẩn (watermark trial).

### Doc Template — soạn WPF GĐ3 (commits `4b6d3ea`, `43cc0b4`)
- Module mới **`ConfigStudio.WPF.UI.Modules.DocTemplate`**: `RichEditControl` + panel biến (tái dùng `ISchemaInspectorService` dm_exec_describe Target DB) + chèn MERGEFIELD + đổi hướng giấy; menu "📄 Mẫu tài liệu".
- `IDocTemplateDataService` (Config DB): tạo/list bộ mẫu + mảnh, nạp/lưu bytes fragment. Editor: picker bộ mẫu+đích, tạo mẫu/thêm mảnh, Nạp/Lưu DB.
- WPF verify **compile** (không GUI ở môi trường). `UseWindowsForms=true` (RichEdit interop).

### 📌 Decisions Log
- **DevExpress Office File API**: xuất tài liệu; cô lập 1 project backend + dùng DevExpress WPF/Blazor sẵn có (KHÔNG phát sinh license mới). Per-seat-dev, runtime royalty-free; deploy IIS không cần license máy chạy; **trial = binary có hạn + watermark** → mua Universal khi prod.
- **Mọi query qua stored proc** (không T-SQL trần) + whitelist `Doc_Proc_Registry`; tenant-local Config DB (ngoài ConfigSync); tham số ánh xạ `Doc_Template_Param`.
- Detail = "nạp nguyên bảng" (header=tên cột); region-merge template-driven = tùy chọn sau.

### ⏳ Còn lại
- Chạy `db/077` + đăng ký proc `Doc_Proc_Registry` + soạn stored proc → E2E. Mua license Universal khi prod.
- Tùy chọn: UI ánh xạ `Doc_Template_Param` + quản lý `Doc_Proc_Registry`; **GĐ2 soạn Web** (Blazor DxRichEdit).
- Solution `.slnx` đã dọn RuntimeCheck + thêm `Infrastructure.Documents`/`Modules.DocTemplate`/`UI.DynamicForms`.

## ✅ Đã xong — Hệ đính kèm / Upload file tổng quát (session 77 — 2026-07-07, CHƯA commit)

**Tính năng:** upload file tổng quát gắn field Form Engine, 4 trục: tối ưu ảnh · UX file lớn (streaming+progress) · bảo mật (allowlist+magic-byte+chặn mã thực thi) · lưu trữ linh hoạt (di dời gốc, đa-node). Spec `docs/spec/26_FILE_UPLOAD_SPEC.md` + hướng dẫn WPF `docs/huong-dan-wpf/cau-hinh-attachment.md`.

### Backend (Application/Infrastructure/Api) — build 0/0
- [x] **P1 Nền storage**: `IFileStore` 3 provider (Db/FileSystem/Object) + `IFileStoreSelector` + `IStorageKeyBuilder` (key tương đối bất biến) + `FileSystemFileStore` (guard path-traversal + ghi-tạm→rename) + `ObjectFileStore` (stub) + `FileStorageStartupCheck` (fail-fast). Migration `db/070` (TT_TepBlob dedup + mở rộng TT_TepDinhKem) + file gộp `db/dev/create_tt_attachment_full.sql`.
- [x] **P2 Bảo mật** `FileValidator`: allowlist đuôi + magic-byte đa định dạng + sniff mã thực thi/script/SVG-XSS + double-extension. Serve: Content-Disposition attachment + nosniff + ETag/304.
- [x] **P3 Streaming**: controller spool multipart ra tệp tạm (DeleteOnClose) → không full RAM.
- [x] **P4 Tối ưu ảnh** `SkiaImageOptimizer` (SkiaSharp MIT): resize/nén + thumbnail; client nén canvas.
- [x] **P5 Dedup** `TepBlobRepository` (MERGE HOLDLOCK theo checksum + RefCount) + xóa → giảm ref → dọn vật lý.
- [x] **Đa-tệp-khi-thêm-mới**: upload treo (Owner_Id NULL) → `POST /attachments/link` gắn sau khi Lưu (guard Owner_Id NULL + CreatedBy).

### Frontend (ICare247_UI) — build 0/0
- [x] **P6 Control `AttachmentRenderer`** — 2 chế độ TỰ CHỌN theo `IsVirtual`: field ảo → đa tệp (bảng phụ); field map cột → 1 tệp (Id vào cột, kiểu Logo_Id). Upload JS (XHR progress + nén ảnh + Bearer) + preview thumbnail (data-URL giữ auth) + tải (fetch token) + xóa. `AttachmentApiService`.
- [x] **Tích hợp Form Engine**: dispatch `case "attachment"` + `NormalizeFieldType` ở **cả FormRunner VÀ MasterDataForm** (bug: quên MasterDataForm → ra ô text). Host bơm `__ownerTable/__ownerId` vào Context.
- [x] **WPF ConfigStudio**: thêm `AttachmentBox` vào `AvailableEditorTypes` + guide 2 chế độ.
- [x] **Fix UX**: cảnh báo lỗi upload dính (thêm ✕ gỡ + tự dọn khi chọn tệp mới); từ ngữ toolbar note "1 tệp" → "Tối đa 1 tệp".

### 📌 Decisions Log
- **Storage Hybrid provider-driven** (user chốt "cả hai, cấu hình được"): DB nhỏ / FileSystem-shared-mount / Object; key tương đối + BaseRoot config → di dời gốc = đổi 1 config; startup fail-fast chống ghi local sau LB.
- **SkiaSharp (MIT)** thay ImageSharp (Split License) — ICare247 SaaS thương mại, tránh phí license.
- **2 chế độ đính kèm auto theo IsVirtual** (user "hỗ trợ cả hai"): ảo=đa-tệp owner-based (file→record); cột=1-tệp column-based (record→file, chạy được cả khi thêm mới).
- **ownerTable = FormCode** (không phải bảng vật lý) — frontend không có tên bảng thật; đủ nhất quán cho save/list/link.

### ⏳ Deploy để thấy kết quả
- [ ] Chạy migration (file gộp `db/dev/create_tt_attachment_full.sql`) trên Data DB tenant (sửa lỗi "Invalid object name TT_TepDinhKem" khi thiếu 063).
- [ ] Rebuild + restart API + rebuild web (ICare247_UI) + hard reload. Rebuild ConfigStudio WPF.
- [ ] Cấu hình `FileStorage` trong appsettings.local.json nếu dùng FileSystem/Object (mặc định Db chạy ngay).
- [ ] Kiểm thử E2E trình duyệt (CHƯA chạy — mới verify compile).

### 🔮 Hoãn / spec-only (giai đoạn sau)
- [ ] **Job dọn tệp mồ côi** → HOÃN, gộp vào "quản lý tiến trình nền" chung có UI (spawn task `task_56b62113`). Tệp mồ côi chỉ tốn dung lượng, không sai nghiệp vụ.
- [ ] **Quản lý thông số hệ thống** (FileStorage/DebugLog/Cache... qua web, schema-driven, hybrid file+DB) → **spec `docs/spec/27_SYSTEM_SETTINGS_SPEC.md` đã viết, CHƯA code**. Còn 5 điểm chốt §11.

## ✅ Đã xong — UI/UX loạt màn: Field Navigator, form web chia section, modal ghim, TreeList (session 76 — 2026-07-06, CHƯA commit)

### ConfigStudio WPF — Field Navigator (FieldConfigView)
- [x] **Bulk multi-select + chuyển Section/Tab** (parity FormEditor): checkbox tick field + context-menu "Chuyển N field đã chọn sang…". Xử luật: cột "CHƯA TẠO FIELD" không tick; group đích rỗng tự tạo; reindex Order_No; persist DB trước rồi mới đổi UI. Model mới `FieldMoveTargetItem`.
- [x] **Hiển thị tên** section + field (thay mã thô): resolve i18n `_i18nService.ResolveKeyAsync(key,"vi")` **2-pass** (list hiện mã ngay → tên điền dần qua INotifyPropertyChanged). Field: tên dòng chính + `EditorType · mã` dòng phụ; header section hiện tên (fallback mã).

### Web (ICare247_UI) — MasterDataForm + modal
- [x] **Chia cụm theo section** (mirror FormRunner): `.section-card` + tiêu đề + `.form-body` gap. Backend [FormRepository.cs] resolve `SectionName` từ `Ui_Section.Title_Key` (trước hardcode `''`).
- [x] **Modal ghim header/footer**: `.dm-modal` flex-column, chỉ `.dm-body` cuộn; footer Hủy/Lưu `position:sticky bottom`; **toast lỗi** sticky-top + căn phải (nút đóng); **auto-focus** field lỗi đầu tiên (`icare.focusField`).

### Web (ICare247_UI) — DataView TreeList (parity grid)
- [x] **Sửa/Xóa theo dòng** + **double-click sửa** + **toolbar dùng chung** + CSS header. Luật xóa: 🗑️ chỉ ở node **lá / cha không còn con** (`__parentKey`).
- [x] **#1 Cấu trúc cây**: backend [ViewRepository.cs] emit `b.[ParentField] AS [__parentKey]` (id cha THÔ) cho TreeList — auto-JOIN đã đổi cột cha thành TÊN; frontend `ParentKeyFieldName="__parentKey"`.
- [x] **#2 Bộ lọc** (FilterRowEditorVisible + FilterMenu) · **#3 STT** (VisibleIndex) · **#4 Lưu layout** (`LayoutAuto*` + `TreeListPersistentLayout`).

### 📌 Decisions Log
- **Field Navigator selection = COPY pattern** sang FieldConfig (không refactor shared) → KHÔNG đụng FormEditor đang chạy ổn (chốt với user).
- **Toast lỗi modal = gộp sticky-top + căn phải** (option 1+3 user chọn); tự chứa trong MasterDataForm, KHÔNG đụng DraggableModal (tránh plumbing qua 2-3 file).
- **TreeList cây lồng** (user: "ParentField đã lưu WPF = CongTy_Cha_Id"): backend emit id cha thô dưới **alias riêng `__parentKey`** → giữ cột tên hiển thị, không đụng cấu hình View. Luật xóa cũng dùng `__parentKey` (không dùng ParentField vì đã thành tên).
- **API DevExpress xác minh qua reflection `DevExpress.Blazor.v25.2.dll` 25.2.3** (không đoán): `DxTreeList.RowDoubleClick`/`GetDataItem`, `TreeListDataColumnCellDisplayTemplateContext.VisibleIndex`, `LayoutAutoLoading/Saving`+`TreeListPersistentLayout`, `DxTreeListDataColumn.FilterRowEditorVisible/FilterMenuButtonDisplayMode`, enum `TreeListColumnFixedPosition.Right`.

### ⏳ Deploy để thấy kết quả
- [ ] Backend đổi (`FormRepository` + `ViewRepository`) → **rebuild + restart API + Xóa cache** (tên section web + cây lồng).
- [ ] Rebuild web (ICare247_UI) + ConfigStudio WPF.
- [ ] Kiểm thử trực quan: Field Navigator (tick+chuyển+tên), form web (section+tiêu đề+modal ghim+toast+focus), TreeList (cây lồng+lọc+STT+lưu layout+xóa lá/cha-không-con).

## ✅ Đã xong — Cascade lookup + Multi-Trigger + Cache (session 73 — 2026-07-05, đã commit + push master)

- [x] **Fix gốc cascade field ảo** — `MasterDataForm` bỏ lọc `!IsVirtual` → Tỉnh/Ngân hàng render + vào context (gốc lỗi 500 `Must declare @param`). `c2ffcff`
- [x] **Reload đa-@param** — LookupBoxRenderer tự dò @param Filter SQL; ẩn reloadOnChange, single→Nâng cao, bỏ P3. `47e3b2d`+`a2d6bdf`
- [x] **Multi-Trigger** (`Reload_Trigger_Fields`, db/068) + **TreeLookupBox Selectable_Level** (all/leaf/branch, db/069). `b9e30d5`+`3ffdc03`
- [x] **Cache lookup động** (cache-aside, thay lazy) — version theo (tenant, bảng nguồn) + hash @param; invalidation khi SaveMasterData. `da7ff83`
- [x] **Badge Is_Configured** (db/067) + reset field mới + STT chèn-sau + thu gọn diễn giải. `47e3b2d`

### 📌 Decisions Log
- **Cache thay lazy-load** cho lookup động (user chốt): đơn giản hơn, lợi mọi lookup; **invalidation B** (bump version bảng khi lưu danh mục, tách khỏi cache form).
- **TreePicker (nhánh dynamic-tree) = bản đầy đủ hơn TreeLookupBox** → KHÔNG merge nhánh (base cũ, phân kỳ ~60 file); **port chọn lọc** Multi-Trigger + Selectable_Level lên master.
- **Cascade parent field phải là field ảo VISIBLE** (không lưu DB nhưng phải render + vào context để bơm @param).

### ⏳ Còn mở
- [ ] Chạy `db/067, 068, 069` trên Config DB + rebuild/restart để deploy.
- [ ] Lazy-load TreePicker (Load_Mode + Root_Filter) — hoãn; chỉ làm nếu cây cực lớn.
- [ ] Xử 3 file i18n pre-existing (chưa commit); cân nhắc xóa nhánh remote `origin/claude/dynamic-tree-control-bLerc`.

## ⏸ TẠM DỪNG — FK lookup auto-JOIN hiện TÊN cha cho cột lưới (session 72 — 2026-06-29, CHƯA commit, branch `master`)

**Tính năng:** cột khóa ngoại ở lưới engine-driven **tự JOIN bảng cha → hiện TÊN thay vì id**, lọc/sắp xếp/xuất theo tên. No-code (không cần SQL View tay). Spec `docs/spec/25_FK_LOOKUP_SPEC.md` + **ADR-033**.

### Đã xong (build BE 0/0; WPF Modules.Forms 0/0 — project app chỉ lock DLL do ConfigStudio đang chạy, KHÔNG phải lỗi code)
- [x] **Engine BE** `ViewRepository.GetDataAsync` tự sinh `LEFT JOIN` từ `Props_Json.fkLookup` của cột; suy cột FK gốc + bảng cha/Value/Display từ `Ui_Field_Lookup` (theo Field_Id). Whitelist identifier; chỉ `Query_Mode='table'` + cột Value/Display đơn (còn lại bỏ qua → escape-hatch view). Hàm mới `ResolveFkJoinsAsync`. Verify SQL trả đúng tên trên DB live.
- [x] **WPF tab Cột**: dropdown **"FK lookup (cha)"** chọn field FK của form sửa theo tên (`NganHang_Id → DM_NganHang (Ten)`) → app tự lưu Field_Id vào `Props_Json.fkLookup`. **User đã build + test ✅ chạy đúng** (combo bind RelativeSource→VM, không cần BindingProxy).
- [x] **DB đã áp live (tenant 1)**: `db/066` cấu hình auto-JOIN in-place (Model 2 — `NganHang_Id` hiện tên). `db/064` (view `vw_DM_ChiNhanhNganHang` — ví dụ escape-hatch §5b, KHÔNG dùng cho màn này). `db/065` (Model 1 qua view — đã bị 066 thay).
- [x] **Docs/memory**: spec 25 + ADR-033 + `cau-hinh-luoi-tham-chieu.md` (Cách A auto-JOIN / Cách B view) + README + 2 memory (GitNexus CLI, WPF combo binding).

### ⏳ CÒN LẠI (dở dang — làm khi quay lại)
1. [ ] **Build + restart API** để LƯỚI WEB (Blazor) hiện tên — engine mới CHƯA deploy (ConfigStudio đã build). ⚠️ Đừng để engine CŨ chạy với cấu hình mới (sẽ lỗi).
2. [ ] **Commit** — đang ở `master` → **tạo branch trước**. Message đã chuẩn bị sẵn (cuối section). Trước commit nên chạy `node .gitnexus/run.cjs detect-changes`.
3. [ ] **Pha 2 (Import Mã→Id, lọc quyền)** + **Pha 3 (xuất template import nhiều sheet)** — CHƯA làm. **Q3 (thư viện Excel: ClosedXML/EPPlus/OpenXML) còn MỞ**. Lưu ý: `Ui_Field_Lookup.Code_Field` của Field 34 đang **NULL** → Pha 2 cần set `'Ma'` để có cầu Mã↔Id.
4. [ ] (Dọn tùy chọn) `db/065` đã bị 066 thay; Sys_Resource key `...col.tennganhang.caption` (db/065) thừa nhưng vô hại; view `vw_DM_ChiNhanhNganHang` giữ làm ví dụ escape-hatch.
5. [ ] (Tùy) WPF chưa có ô **Props_Json thô** (chỉ có fkLookup dropdown) — nếu cần Props khác phải qua SQL.

### Files thuộc feature (phân biệt với thay đổi nền có sẵn từ session trước)
- **BE**: `src/backend/src/ICare247.Infrastructure/Repositories/ViewRepository.cs`
- **WPF**: `…Core/Data/ViewColumnRecord.cs`, `…Core/Interfaces/IViewDataService.cs`, `…/Infrastructure/ViewDataService.cs`, `…Modules.Forms/ViewModels/ViewManagerViewModel.cs`, `…Modules.Forms/Views/ViewManagerView.xaml`, `…Core/Data/FkLookupFieldOption.cs` (mới)
- **DB**: `db/064`, `db/065`, `db/066` (mới — đã áp live)
- **Docs**: `docs/spec/25_FK_LOOKUP_SPEC.md` (mới), `docs/huong-dan-wpf/cau-hinh-luoi-tham-chieu.md`, `docs/huong-dan-wpf/README.md`, `.claude/memory/architecture_decisions.md` (ADR-033)
- **KHÔNG thuộc feature** (M sẵn từ trước / không do session này): `AGENTS.md`, `CLAUDE.md`, `.claude/memory/last_session.md`, 2× `i18n/catalog.json`, `i18n-report.md`, `.claude/skills/gitnexus/` (cài GitNexus) — cân nhắc commit riêng.

### ✅ Message commit chuẩn bị sẵn (copy-paste khi commit FK feature)
```
feat(view): FK lookup auto-JOIN hiện TÊN cha cho cột lưới (engine + ConfigStudio)

Cột khóa ngoại ở lưới engine-driven tự JOIN bảng cha để hiển thị TÊN thay vì id;
lọc/sắp xếp/xuất Excel theo tên. Cấu hình no-code, không cần viết SQL View tay.

Backend (engine):
- ViewRepository.GetDataAsync tự sinh LEFT JOIN từ Props_Json.fkLookup của cột;
  suy cột FK gốc + bảng cha/Value/Display từ Ui_Field_Lookup theo Field_Id.
  Whitelist identifier; chỉ Query_Mode='table' + cột đơn (còn lại bỏ qua -> view).

ConfigStudio (WPF):
- Tab Cột thêm dropdown "FK lookup (cha)" chọn field FK của form sửa theo tên
  (NganHang_Id -> DM_NganHang (Ten)); lưu Field_Id vào Props_Json.fkLookup.
- ViewColumnRecord.FkLookupFieldId, IViewDataService.GetFormLookupFieldsAsync,
  FkLookupFieldOption.

DB (đã áp live tenant 1):
- db/066 cấu hình auto-JOIN in-place (Model 2 - NganHang_Id hiện tên) [hiện hành]
- db/064 vw_DM_ChiNhanhNganHang (ví dụ escape-hatch, không dùng cho màn này)
- db/065 cấu hình qua view (Model 1) - đã bị 066 thay

Docs: docs/spec/25_FK_LOOKUP_SPEC.md + ADR-033 +
  docs/huong-dan-wpf/cau-hinh-luoi-tham-chieu.md (Cách A auto-JOIN / Cách B view).

Pha 1 (lưới) xong. Pha 2 (import Mã->Id) / Pha 3 (template) chưa làm.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
```

## 🔴 Đang làm — Import Excel Pha 2 (FK lookup) — ADR-034 / spec 25 §11–§14 (session 78 — 2026-07-07)

**Tính năng:** import dữ liệu Excel vào Data DB theo cấu hình `Ui_View` grid. Xuất template (sheet chính + mỗi FK 1 sheet phụ + ô FK là combobox chọn Mã); validate (trim, định dạng, FK Mã→Id lọc quyền, trùng khoá); **upsert khoá ghép** `Import_Key_Fields`; partial commit; 2 hook proc (mỗi dòng = `sp_AfterSave_Grid_<T>` spec 18, sau import = `sp_AfterImport_<T>` mới); log import + masking theo cột. **v1 = Grid phẳng.** Thư viện **ClosedXML** (MIT).

**Chốt thiết kế:** ADR-034 + spec 25 §11–§14 (2026-07-07). Xem Decisions Log ADR-034.

### ⏳ Tasks (theo thứ tự code)
- [~] **IMPORT-0 (DB)** — ✅ viết migration (⏳ CHƯA chạy DB): `db/071` (Config DB: `Ui_View.Import_Key_Fields` + `Sys_Column.Is_Log_Masked`/`Log_Mask_Mode` + set `Ui_Field_Lookup.Code_Field='Ma'` Field 34) + `db/072` (Data DB: `Sys_Import_Log` + `Sys_Import_Log_Detail`) + `db/073` (Config DB: seed i18n `import.*` vi/en). CÒN: mở rộng `sp_AfterSave_Grid_<T>` (+`@Source`,`@ImportSessionId`) + skeleton `sp_AfterImport_<T>` → gộp vào IMPORT-6.
- [x] **IMPORT-1** — `IFkLookupResolver` (App) + `FkLookupResolver` (Infra) — build BE 0/0. `GetFkColumnsAsync` (tường minh `Props_Json.fkLookup.fieldId` → ngầm `Edit_Form_Id+Column_Id`) + `BuildCodeMapAsync` (SELECT Code/Value/Display trên Data DB, bind token qua `IContextParamResolver`, ORDER BY whitelist). `FkCodeMap.TryResolve` chuẩn hóa trim+upper-invariant. DI đăng ký. Dùng chung template + import.
- [x] **IMPORT-2** — `IImportTemplateBuilder` (App) + `ImportTemplateBuilder` (Infra, ClosedXML) — build 0/0. Sheet chính (tiêu đề i18n bôi đậm + `*` bắt buộc + comment kiểu/FK, ghim dòng 1) + mỗi FK 1 sheet phụ `{Ma,Ten}` + Data Validation dropdown chọn Mã (2000 dòng). Sheet-name sanitize ≤31 ký tự + unique. ClosedXML thêm vào CPM (`Directory.Packages.props` 0.104.2) + Infrastructure.csproj. DI đăng ký.
- [x] **IMPORT-3** — `IImportEngine` (App) + `ImportEngine` (Infra, ClosedXML) — build BE 0/0. `BuildPlanAsync`: mở workbook + map tiêu đề (Caption bỏ `*` → FieldName) + FK nạp `FkCodeMap` (lọc quyền) + duyệt dòng (trim → required → kiểu `TryConvert` → FK Mã→Id → composite-key). Nạp 1 lần tập khoá hiện có (`LoadExistingKeysAsync`) → phân loại NEW/UPDATE/ERROR + trùng khoá trong file. Masking Row_Json (Full/Partial/Hash) chỉ dòng lỗi. DTO `ImportPlan/ImportRow/ImportCellError` (App). DI đăng ký. **Chưa ghi DB** (commit ở IMPORT-4).
- [x] **IMPORT-4** — `ImportController` (3 endpoint: `GET .../import/template`, `POST .../validate`, `POST .../commit`) + 3 handler MediatR + `IImportMetadataProvider` (dựng cột nhập/kiểu/bắt buộc/khoá ghép/masking từ View+Form+Sys_Column, đọc phòng thủ cột mới) + `IImportLogRepository` (Sys_Import_Log/_Detail + hook `sp_AfterImport_<T>` opt-in) + seed i18n `db/073`. Commit ghi mỗi dòng qua `SaveMasterDataCommand` (→ `sp_AfterSave_` tự nổ, rollback-on-fail) + kiểm quyền Form.Them/Sua + partial commit + ghi log + hook sau import. Build BE 0/0. **CÒN:** contract `sp_AfterSave_` +`@Source`/`@ImportSessionId` (per-row hook hiện đã có @NguoiDungID + @Id 0/>0 = ai + thao tác) → gộp IMPORT-6.
- [x] **IMPORT-5** — `ImportWizard.razor` + `.razor.css` (Components/View) + `ImportApiService` (3 endpoint, multipart upload, tải template qua `icare.downloadBytes` JS mới) + nút "⬆ Import Excel" trên toolbar ViewPage + modal DraggableModal + `OnImported`→reload. 3 bước: Upload (tải template + chọn file) → Preview (chip Thêm/Sửa/Lỗi + bảng dòng lỗi) → Result (trạng thái commit). DI Program.cs. Build FE 0/0. ⚠ Nếu server đang chạy lúc build → restart API/web + hard-reload.
- [x] **IMPORT-6** — Hook mỗi-dòng nhận ngữ cảnh import + hook sau-import — build BE+WPF 0/0. (a) `SaveMasterDataCommand`/`SaveWithHooksAsync`/`RunHookProcAsync` thêm `Source`/`ImportSessionId`; **EXEC chỉ thêm `@Source`/`@ImportSessionId` khi import** (`importSessionId!=null`) ⇒ save tay giữ contract cũ, proc chưa nâng cấp KHÔNG vỡ (zero-regression). Commit truyền `Source="IMPORT"`+sessionId. (b) Proc contract v2: `db/procs/sp_AfterSave_Grid_DM_PhuongXa.sql` +2 param DEFAULT; `db/procs/sp_AfterImport_DM_PhuongXa.sql` skeleton mới. (c) Codegen `HookStoreTemplate`: after-save v2 + `BuildAfterImportProc` + `BuildProcBatches` gồm 3 proc; `SysTableManagerViewModel` kiểm/tạo cả after-import. (Bỏ IHookStoreCatalog cho after-import: OBJECT_ID 1 lần/mẻ, không đáng cache.)

### ✅ TỔNG KẾT IMPORT (session 78) — build BE 0/0 · FE 0/0 · WPF Core+Forms 0/0
Backend + frontend + WPF codegen hoàn tất. **⏳ Deploy để chạy thật:**
- Chạy `db/071` (Config), `db/072` (Data), `db/073` (Config seed i18n) trên tenant.
- Chạy lại `db/procs/sp_AfterSave_Grid_DM_PhuongXa.sql` (contract v2) + (tùy) `sp_AfterImport_DM_PhuongXa.sql` trên Data DB.
- Rebuild+restart API · rebuild web (hard-reload) · rebuild ConfigStudio.
- E2E: mở 1 View có Edit_Form → nút "⬆ Import Excel" → tải template → điền → validate → commit → kiểm `Sys_Import_Log`.
- ⚠ Cần cấu hình `Ui_View.Import_Key_Fields` (nếu muốn upsert) + `Sys_Column.Is_Log_Masked` (nếu muốn làm mờ) qua ConfigStudio/SQL.

### 📌 Decisions Log (ADR-034)
- **ClosedXML** (MIT) — đóng Q3 ADR-033. Grid phẳng v1; TreeGrid pha sau.
- **Upsert khoá ghép** `Ui_View.Import_Key_Fields` (CSV, gồm cả cột FK); so khớp SAU resolve FK, trim+culture-invariant, dictionary RAM.
- **Partial commit** + dry-run bắt buộc (`importSessionId`).
- **2 hook = hướng ④ tái dùng proc spec 18** (KHÔNG field-SQL Ui_View, KHÔNG Event Engine). Mỗi dòng tự nổ `sp_AfterSave_` (rollback-on-fail); sau import = proc mới.
- **Log import Data DB** + **masking theo cột** `Sys_Column.Is_Log_Masked/Log_Mask_Mode` (làm mờ TRƯỚC khi ghi, cả Row_Json lẫn Error_Args).

## ✅ MasterData: lỗi rõ ràng + i18n khi bảng thiếu PK + fix LockOnEdit field lookup động (session 71 — 2026-06-28, build BE+FE 0/0, CHƯA commit)
- [x] **Lỗi "chưa có khóa chính" rõ ràng + i18n** (thay 500 chung): backend `MetadataConfigurationException(Code="metadata.no_primary_key")` ném ở `MasterDataRepository.GetFormInfoAsync` (chokepoint mọi CRUD) khi cả `Sys_Column.Is_PK` lẫn PK vật lý đều rỗng → middleware map **500** kèm `code`/`formCode`/`correlationId` (KHÔNG 422 vì `SaveAsync` coi 422 là body kết quả). Frontend `ApiProblemException` bóc `code` → `ApiErrorLocalizer.Describe(Loc, ex)` → `Loc.L("error.metadata.noPrimaryKey", …)`. Áp 3 chỗ catch (List/Form load+save). **ADR-032**.
- [x] **Fix LockOnEdit rớt cho field lookup ĐỘNG**: `FormRepository.GetByCodeAsync` tạo lại `new FieldMetadata{…}` khi gắn `LookupConfig` nhưng **quên copy `LockOnEdit`** → field LookupBox luôn về `false` (field TextBox không bị). Thêm `LockOnEdit = f.LockOnEdit` ([FormRepository.cs:236](src/backend/src/ICare247.Infrastructure/Repositories/FormRepository.cs:236)). VERIFY: DB `true` vs API `false` → khoanh đúng backend.
- [x] **Memory**: ADR-032 (lỗi-có-mã→i18n) + feedback `[Debug] So DB↔API↔UI trước khi đọc code`.
- [ ] (Verify) Restart API + hard-reload WASM: form thiếu PK hiện thông báo rõ; ô Ngân hàng khóa khi Sửa.
- [ ] (Theo dõi) Ô Ngân hàng còn trống "-- Chọn --" khi Sửa — độc lập, kiểm nạp giá trị nếu vẫn lỗi sau restart.
- ⛔ KHÔNG sửa DB (user chốt): bảng `DM_ChiNhanhNganHang` vẫn cần thêm PK để dùng được — chỉ làm rõ lỗi.

## 🔴 Đang làm (In Progress)

**F1 — Đồng bộ config master→tenant: CFGSYNC-0→3 ĐÃ CODE XONG (session 53, build BE 0/0). `db/050` ✅ ĐÃ CHẠY 2026-06-21. ⏳ CÒN: E2E.**
Spec: `docs/spec/16_CONFIG_SYNC_SPEC.md`. Đã chốt: Cách 2 (F1 trước → F2 sau), engine-driven (ADR-024), 1 DB/tenant (ADR-025).
5 quyết định mở DUYỆT 2026-06-15 (toàn bộ khuyến nghị): master=Config DB canonical · bảo vệ **row-level** (`Is_Customized`) ·
giữ bản tenant khi xung đột · trigger provisioning+nút thủ công super admin · xóa=`Is_Active=0` tombstone.

### ⏳ VIỆC CÒN LẠI CỦA F1 (theo thứ tự)
1. ✅ **E2E xác minh 2026-06-21** — db/050 đã chạy, login `admin` → preview + apply → ghi `Sys_Config_Sync_Log`
   (#4 Status=Success, Updated 32/Skipped 9). Trong lúc E2E sửa 3 bug: JWT bypass SUPERADMIN (commit `9851c8a`),
   thiếu cờ `Sys_Column` + transaction nhánh apply (commit `9abec54`).
2. ✅ **Mở rộng descriptor (2026-06-25)** — `ConfigSyncTables.Order` phủ **14 bảng**: +Ui_Tab, Val_Rule,
   Ui_Field_Lookup, Sys_Resource, Sys_Lookup, Ui_View, Ui_View_Column, Ui_View_Action, Ui_View_Filter. Nâng engine:
   khóa ghép nhiều cột (`LocalKeyColumns`: Sys_Resource/Sys_Lookup) + bảng KHÔNG Id identity (Sys_Resource — match/UPDATE
   theo khóa nghiệp vụ) + khóa CHỈ-theo-cha cho bảng mở rộng 1-1 (`Ui_Field_Lookup`, KeyColumns rỗng).
   **Migration mới `db/062`** (cấp 4 cờ sync cho Ui_Field_Lookup + Ui_View_Filter — db/050 bỏ sót) — ⏳ CẦN chạy trên Config DB.
   Build Infrastructure 0/0. ⏳ CÒN: E2E preview/apply xác minh số dòng; rà self-FK `Ui_View.Detail_View_Id` (master-detail).
   Ngoài phạm vi: `Sys_Relation` (chưa engine-hóa master-detail); `Val_Rule_Field` (đã DROP ở migration 003).
3. ✅ **CC-4 (2026-06-25)** — `SyncConfigCommandHandler` bump `ICacheVersion` sau khi apply thành công → vô hiệu
   TOÀN BỘ cache config dùng chung (form/view/lookup/resource) của tenant → thấy cấu hình mới NGAY, không cần restart.
   Build Application 0/0.
   ⏸️ **Hook provisioning full-sync — BỊ CHẶN** (không có luồng tạo tenant để gắn): catalog chưa dựng (chạy fallback
   1-tenant, `TenantConnectionResolver`), chưa có endpoint/command tạo tenant. `SyncConfigCommand(DryRun=false)` ĐÃ là
   full-sync sẵn → khi dựng subsystem provisioning chỉ cần gọi nó cho tenant mới. Hoãn tới khi có catalog + tenant-create.
   ✅ **db/057 (2026-06-21)** seed node `administration.config-sync` vào `HT_ChucNang` + grant SUPERADMIN
   → màn hiện trên menu Quản trị (verify `/me/navigation` trả node sau khi flush cache).

✅ **UI web (session 53)** — màn "Đồng bộ cấu hình" `/m/administration/config-sync`: `ConfigSyncApiService` +
`ConfigSyncModels` + `ConfigSyncPage.razor(.css)` (toolbar mỏng, nút "Xem trước" dry-run + CTA "Áp dụng từ master"
có bước xác nhận, badge trạng thái + bảng số dòng I/U/Ngừng/Bỏ qua). Menu `administration.config-sync` (AppNav) +
DI. i18n đầy đủ (`admin.cfgsync.*`). Build FE 0/0. ⏳ E2E cần backend + db/050 + login admin.

> Xong F1 → **F2 engine-hóa màn Công ty** (ORG-CFG-1→4).

**Decisions Log (session 53):**
- **Cờ đồng bộ tên tiếng Anh** `Is_System`/`Is_Customized`/`Synced_At`/`Source_Ver` (config DB dùng cột tiếng Anh) —
  KHÁC `LaHeThong`/`DaTuyBien` của Data DB (`db/042`). Khi code entity/repo nhớ mapping này.
- **Engine descriptor-driven** (1 routine UPSERT generic + `ConfigSyncTables.Order`): re-link FK theo **mã** (map 2 chiều
  Code↔Id, KHÔNG bê Id identity); khóa nghiệp vụ = [khóa cha re-link] + mã con (sep control char U+0001).
- **Master = `ConnectionStrings:Config`** (user chốt dùng key sẵn có; dev master trùng tenant → sync vô hại). Tách 1 DB/
  tenant về sau = đổi nguồn master trong ctor `ConfigSyncService`.
- **CFGSYNC-2 phạm vi = vertical slice 5 bảng** (user chốt) để verify pattern trước khi mở rộng toàn bộ §2.

## 🔒 Nâng cấp bảo mật (Tầng 1→5) — Spec `docs/spec/20_SECURITY_HARDENING_SPEC.md`

> **Chi tiết thi công đã xong + bảng trạng thái đầy đủ → [spec 20 §9](docs/spec/20_SECURITY_HARDENING_SPEC.md)**
> (SEC1→SEC5 đã code, E2E Tầng 1 xác minh 2026-06-25). Dưới đây chỉ liệt kê **việc còn mở**.

- [~] **SEC1-5** — Chống IDOR: DB-per-tenant + SEC1-3 đã chặn cross-tenant. ⏳ Row-ownership trong cùng tenant (tầng phân quyền) — cần rà thêm.
- [ ] **SEC1-4 (TODO)** — Tinh chỉnh Lookup-insert: resolve `Source_Name`→form→`HasPermissionForTargetAsync("Form",formCode,Them)` (xem `TODO(SEC1-4)` tại `LookupController.Insert`).
- [ ] **SEC2-4** — (tùy chọn) MFA/2FA cho admin.
- [ ] **SEC2/3 E2E** — Kiểm thử Tầng 2 Step 1 (cookie `ic247.rt` HttpOnly/Secure/Lax, body không refreshToken, refresh/logout-all) + Tầng 3 (header bảo mật mọi response, >200 req/10s→429, >10 login/phút→429).
- [ ] **SEC3-4 (chờ hosting)** — CSP chống XSS cho APP đặt ở host phục vụ WASM (web.config/nginx), KHÔNG ở API.
- [~] **SEC4-1** — Validator coverage 6/39; đường ghi trọng yếu đã an toàn (robustness) — bổ sung validator command còn lại khi rảnh.
- [ ] **SEC5-2 (DB least-privilege)** — deployment (không code-fixable): account app KHÔNG `db_owner`/`sysadmin`; tách account migration khỏi runtime; runtime chỉ CRUD + EXECUTE proc cần thiết.
## 📋 Backlog — Tối ưu code (phát hiện 2026-06-23, chưa xử lý)

> Phân tích toàn bộ codebase 2026-06-23. Không phải bug nghiêm trọng — tối ưu performance + clean-up.
> Ưu tiên: 🔴 Ngay / 🟡 Sớm / 🟠 Sau / 🟢 Cleanup.

### 🔴 Ngay
- [ ] **OPT-1** — `MasterDataRepository.GetByIdAsync` dùng `SELECT *` (vi phạm Hard Constraint #9).
      Fix: dùng danh sách cột từ `info.Columns` (như `GetListAsync` line 166–171). File: `MasterDataRepository.cs:229`.
- [ ] **OPT-2** — `GetAuditColumnsAsync` query `INFORMATION_SCHEMA` mỗi lần INSERT/UPDATE (thêm 1 roundtrip DB mỗi save).
      Fix: cache `AuditColumns: HashSet<string>` vào `MasterDataFormInfo` khi `GetFormInfoAsync`, dùng lại cùng request. File: `MasterDataRepository.cs:266,311`.

### 🟡 Sớm
- [ ] **OPT-3** — `GetListAsync` (MasterDataRepository + ViewRepository) gửi 2 query riêng (list + count) thay vì 1 batch.
      Fix: dùng `QueryMultipleAsync` gộp 2 câu SQL thành 1 roundtrip. Files: `MasterDataRepository.cs:200–207`, `ViewRepository.cs:692–695`.
- [ ] **OPT-4** — `ViewRepository.GetByCodeAsync` thực hiện 4 roundtrip tuần tự (header → columns → actions → filters) trong 1 method 709 dòng.
      Fix: gộp bằng `QueryMultipleAsync` → 1 roundtrip; xem xét tách thành các private method nhỏ hơn. File: `ViewRepository.cs:53–296`.

### 🟠 Sau
- [ ] **OPT-5** — Bulk delete ở `ViewPage.razor` xóa từng dòng bằng vòng lặp API riêng → không rollback khi lỗi giữa chừng.
      Fix: thêm endpoint `/views/{code}/bulk-delete` nhận danh sách IDs, xóa trong 1 transaction server-side.
- [ ] **OPT-6** — `CacheVersion` dùng `Dictionary<int,long>` in-memory → không sync cross-instance khi scale-out (ADR-021 đã ghi nhận).
      Fix: chuyển sang Redis `INCR cfgver:{tenantId}` để version-stamp đồng bộ across instances. File: `CacheVersion.cs`.

### 🟢 Cleanup
- [ ] **OPT-7** — Magic string i18n key (`"sys.val.unique"`, `"sys.val.required"`) hardcode trong `SaveMasterDataCommandHandler`.
      Fix: khai báo constants trong `CacheKeys.cs` hoặc class `I18nKeys` riêng. File: `SaveMasterDataCommandHandler.cs`.
- [ ] **OPT-8** — `StateHasChanged()` gọi rải rác 14+ chỗ trong codebase Blazor thay vì batch ở cuối async method.
      Fix: batch `StateHasChanged()` cuối method; tránh gọi giữa chừng từng bước async.

---

## 📋 Roadmap — Save hook store per màn (validate trước + hậu xử lý) — ADR-029

> Mỗi màn engine-driven (vd Xã/Phường) thêm 2 store TÙY CHỌN ở pipeline `SaveMasterData`:
> **`spc_Grid_<Table>`** (validate TRƯỚC ghi) + **`sp_AfterSave_Grid_<Table>`** (hậu xử lý SAU ghi).
> Nhận: toàn bộ field màn (JSON) + người thực hiện + `Id` (`0`=thêm mới). Lỗi trả `error_key`+args (i18n
> server resolve). Spec: `docs/spec/18_SAVE_VALIDATION_HOOK_SPEC.md`. ADR-029.
>
> **Chốt:** result-set lỗi (key/args/field/severity) · JSON+OPENJSON · server resolve i18n · bọc transaction ·
> store thiếu = opt-in (`OBJECT_ID`) · codegen skeleton rỗng pass-through trong ConfigStudio WPF · Id null→0.
>
> **✅ CODE XONG (session 60, 2026-06-22)** — build BE `ICare247.slnx` 0/0, FE/WPF 0/0. ⏳ CÒN: chạy SQL + E2E (SVHOOK-6).

**Decisions Log (session 60 — SVHOOK):**
- **Store trả KEY, không trả text** → số bản dịch = số rule (hữu hạn), giải bài "thông báo vô chừng". Handler resolve
  i18n **server-side** qua `IConfigCache.ResolveKeyAsync` (nhất quán code unique cũ). Token `{0}`=giá trị·`{1}`=nhãn (db/053/058).
- **JSON+OPENJSON cho field động + context param rời**; TVP chỉ khi save batch; Id `null→0` (quy ước ID=0 thêm mới).
- **Opt-in qua `OBJECT_ID`** → màn chưa có store chạy y như cũ; app account KHÔNG cần quyền DDL. Tạo store qua
  **codegen ConfigStudio** (skeleton rỗng, `IF OBJECT_ID IS NULL`, không ghi đè). Store nguồn thật dùng `CREATE OR ALTER`.
- **Bọc 1 transaction** validate→ghi→after-save → after-save lỗi rollback cả bản ghi.

- [x] **SVHOOK-1** — `db/058_seed_sys_val_general_messages.sql`: seed (MERGE idempotent) `sys.val.Invalid/Forbidden/Conflict/NotFound`
      (cấp form) + `sys.val.Integer/Numeric/Regex/Length/MinLength/Range/Compare` (template field) + `sys.msg.raw` (passthrough),
      vi+en. Token `{0}`=giá trị · `{1}`=nhãn · `{2}/{3}`=giới hạn (đồng nhất `ResourceResolver.ApplyTokens`/db/053).
      KHÔNG đụng `sys.val.Required/Unique` (db/053). ⏳ **CHƯA chạy DB** (Config DB; sẽ sync xuống tenant qua config-sync).
- [x] **SVHOOK-2** — `IMasterDataRepository`/`MasterDataRepository`: `SaveWithHooksAsync` + DTO `MasterDataHookSaveResult`/`ProcError`.
      Gói `spc_Grid_<T> → INSERT/UPDATE → sp_AfterSave_Grid_<T>` trong **1 transaction** Data DB; opt-in qua `OBJECT_ID`
      (store thiếu → bỏ qua); spc_/after-save trả lỗi → rollback. Tách `InsertCoreAsync`/`UpdateCoreAsync` (nhận conn+tx,
      tái dùng `BuildColumnParams`+audit); `GetAuditColumnsAsync` thêm tham số tx. EXEC: field động→`@PayloadJson` (JSON),
      context→param rời; `@Id` null→0. Đọc result set qua Dapper row (RowStr — không phụ thuộc underscore map). Build Infra 0/0.
- [x] **SVHOOK-3** — `SaveMasterDataCommandHandler`: chuyển ghi DB sang `_repo.SaveWithHooksAsync` (thay Insert/UpdateAsync);
      store fail → resolve `ProcError`(key+args)→text qua `IConfigCache.ResolveKeyAsync` (fallback=errorKey), thay token
      `{0..n}` theo vị trí args (arg là KEY i18n → resolve lồng), trả `MasterDataFieldError(field_name, text)`. Audit gộp
      Create/Update theo `r.Id`. ValidationEngine + unique-check giữ nguyên (chạy TRƯỚC). Build App+Infra 0/0. langCode="vi"
      (luồng thật = cải tiến sau).
- [x] **SVHOOK-4** — `MasterDataForm.razor`: gom lỗi `FieldCode` rỗng/không khớp field → `_formMessages` (banner dạng list);
      lỗi khớp field vẫn tô đỏ ô. Banner hiện dòng tóm tắt "Có N lỗi" (chỉ đếm lỗi field) + danh sách lỗi cấp form.
      Reset `_formMessages` ở Load/Save. CSS `.md-form-banner-list` (app.css). Build FE 0/0.
- [x] **SVHOOK-5** — ConfigStudio WPF: nút **"⚙ Sinh store"** ở màn Sys_Table (SysTableManagerView) → sinh
      `spc_Grid_<Table>.sql` + `sp_AfterSave_Grid_<Table>.sql` skeleton **rỗng pass-through** cho bảng đang chọn vào
      `db/procs` (tự dò repo root, fallback `%APPDATA%\ICare247\ConfigStudio\procs`). **KHÔNG ghi đè** file đã có;
      skeleton bọc `IF OBJECT_ID IS NULL EXEC('CREATE PROC…')`. Helper `HookStoreTemplate` (Core, pure string) +
      `GenerateHookStoreCommand` (VM). Build WPF Forms+Core 0/0. ⚠️ Đóng ConfigStudio để rebuild nạp bản mới.
- [x] **SVHOOK-7** — Cache tồn tại store (`IHookStoreCatalog` cache-aside L1/L2 + version-stamp) → save **đọc cache thay
      vì query `OBJECT_ID`** mỗi lần lưu. Nạp sẵn lúc mở list (`GetMasterDataFormInfoQueryHandler`); cold-miss = 1 query
      gộp 2 OBJECT_ID (mem 10′/redis 60′). `SaveWithHooksAsync` nhận `hasValidate/hasAfterSave` (bỏ `ProcExistsAsync`);
      handler đọc `_hookCatalog.GetAsync(info.TableName,…)`. Flush cache = nhận store mới. Màn View tự nạp ở lần lưu đầu.
      `CacheKeys.HookStore` + DI. Build BE 0/0. (Bổ sung sau phản hồi user.)
      **+ Dedup form-info trong save path:** `SaveWithHooksAsync` + `ExistsValueAsync` đều nhận `MasterDataFormInfo info`
      (handler truyền vào) thay vì `formCode` → bỏ HẾT `GetFormInfoAsync` thừa khi lưu (trước: 1 ở SaveWithHooks +
      1 mỗi field unique; nay: chỉ 1 lần duy nhất ở đầu handler). Nặng hơn nhiều so với OBJECT_ID nên đây là phần
      giảm query thực sự khi lưu.
- [~] **SVHOOK-6** — 2 store thật cho **DM_PhuongXa** (`db/procs/spc_Grid_DM_PhuongXa.sql` + `sp_AfterSave_Grid_DM_PhuongXa.sql`,
      `CREATE OR ALTER`). spc_: Required (Ma/Ten/TinhThanhPho_Id) + Unique Ma (IsDeleted=0, loại trừ @Id, STRING_ESCAPE) +
      referential Tỉnh tồn tại (store-only, engine không làm được). after-save: pass-through + ví dụ. ⏳ **CẦN: chạy 2 file
      trên Data DB + rebuild/restart API + chạy db/058 (Config) → E2E** màn Xã/Phường (nhập sai → thấy lỗi store i18n).

## 📋 Roadmap — Bộ lọc liên kết (cascade) + token ngữ cảnh + prefill Thêm mới — ADR-030

> **Phạm vi đợt này = THIẾT KẾ** (user chốt "thiết kế trước, duyệt mô hình rồi mới code"). Spec + migration + cấu hình
> WPF + đồng nhất hook store đã viết; **runtime CHƯA code**. Xem `docs/spec/14` §10 + `docs/spec/19` + ADR-030.

**Đã làm (thiết kế, session này):**
- [x] **VFILTER-D1** — `db/059_alter_ui_view_filter_cascade.sql`: +`Depends_On`/`Default_To_Field`/`Default_Lock`.
- [x] **VFILTER-D2** — `db/060_create_sys_context_param.sql`: bảng registry + seed 4 token (NguoiDungID/TenantId/LangCode/CongTyID_Active).
- [x] **VFILTER-D3** — spec `14` §10 (cascade + prefill) + spec mới `19_CONTEXT_PARAM_SPEC.md` + ADR-030.
- [x] **VFILTER-D4** — ConfigStudio WPF tab "Bộ lọc": `ViewFilterRecord` +3 thuộc tính + lưới +5 cột (LookupSrc/Lookup_Sql/Phụ thuộc/Đổ vào field/Khóa) + `ViewDataService` SQL.
- [x] **VFILTER-D5** — Đồng nhất hook store `@NguoiThucHien`→`@NguoiDungID`: 2 proc + `MasterDataRepository`/`HookStoreTemplate` + spec 18.

**⏳ CẦN chạy/đồng bộ:**
- Chạy `db/059` + `db/060` trên **Config DB**.
- Chạy lại `db/procs/spc_Grid_DM_PhuongXa.sql` + `sp_AfterSave_Grid_DM_PhuongXa.sql` trên **Data DB** (vì đổi `@NguoiDungID`) → rebuild/restart API.
- Đóng ConfigStudio → rebuild để nạp lưới Bộ lọc mới.

**Runtime (đã code — build BE `ICare247.slnx` 0/0 + FE `ICare247_UI.slnx` 0/0):**
- [x] **VFILTER-1 + CTXPARAM-2** — `ViewRepository.GetFilterOptionsAsync` (static Sys_Lookup / dynamic Lookup_Sql)
      + bind whitelist (filter cha theo Depends_On) ∪ token ngữ cảnh (regex `@name` cho SQL · `sys.parameters` cho SP)
      vào `GetFilteredDataAsync`. Endpoint `POST /views/{code}/filter-options/{filterCode}` + Query/Handler.
- [x] **CTXPARAM-1** — `ContextParam` entity + `IContextParamRepository`/`ContextParamRepository` (Config DB) +
      `IContextParamResolver`/`ContextParamResolver` (Claim/Header/ActiveScope + Validate_Sql, fail-safe Default) +
      `IRequestContextAccessor`/`HttpRequestContextAccessor` (Api, claim/header). DI Infra + Api.
- [x] **VFILTER-2** — `FilterPanel.razor` cascade: render Combo/MultiSelect/Radio nạp options qua API; cha đổi →
      nạp lại con + xóa giá trị con (đệ quy xuống cháu); `ViewApiService.GetFilterOptionsAsync` + `FilterOptionDto`.
- [x] **VFILTER-3** — Prefill: `FilterPanel.OnValuesChanged` → `ViewPage` dựng map Default_To_Field → giá trị →
      `MasterDataForm.InitialValues`/`LockedFields` (Default_Lock=1 → `IsReadOnly`).
- [x] **VFILTER-DOC** — Hướng dẫn cấu hình `docs/guide/cau-hinh-bo-loc-lien-ket.md` (token ngữ cảnh, cascade, scope
      theo tài khoản, prefill, ví dụ Công ty→Phòng ban→Năm→Nhân viên, thêm token no-code, khắc phục sự cố).

> **Build verify (finish-task 2026-06-22):** Backend `ICare247.slnx` 0/0 · Frontend `ICare247_UI.slnx` 0/0 ·
> ConfigStudio WPF `ConfigStudio.WPF.UI.slnx` 0/0.

**⏳ Runtime còn lại:**
- [ ] **CTXPARAM-3** — ConfigStudio WPF màn "Tham số ngữ cảnh" (CRUD `Sys_Context_Param`); hiện seed SQL đủ chạy.
- [x] **VFILTER-ACTIVE (2026-06-25)** — Company-switcher full-stack. BE: `GET /api/v1/me/companies`
      (`GetMyCompaniesQuery`/Handler + `IMeCompanyRepository`/`MeCompanyRepository` — JOIN HT_NguoiDung_CongTy×TC_CongTy
      theo @NguoiDungID, fallback mọi công ty active nếu bảng trống/chưa có, LaMacDinh lên đầu). FE: `AppState` refactor
      (`ActiveCompanyId` long + `Companies` + `CompanyOption`), `ActiveScopeHandler` (DelegatingHandler NGOÀI CÙNG đính
      `X-Active-CongTy`), `MeCompanyApiService`, `CompanySwitcher.razor` (topbar, localStorage `ic247.activeCongTy`,
      đổi → reload trang), MainLayout nạp sớm id + Program.cs wiring. Build BE 0/0 · FE 0/0.
      Chọn "Tất cả công ty" (null) → bỏ header → server default `@CongTyID_Active`=0. ⏳ E2E: cần TC_CongTy có dữ liệu + login.
- [ ] **VFILTER-OPEN** — Chốt bảng phân công user↔công ty thật. ✅ Đã xác minh `HT_NguoiDung_CongTy` là bảng THẬT
      (db/037: NguoiDung_Id/CongTy_Id/LaMacDinh/IsDeleted) — khớp Validate_Sql `CongTyID_Active`. Còn: seed dữ liệu phân công.

## 📋 Roadmap — Engine-hóa màn nghiệp vụ + Đồng bộ config (ADR-024/025)

> Màn Công ty (và mọi màn nghiệp vụ chuẩn) = **no-code engine-driven**, KHÔNG bespoke. Thiết kế ở
> ConfigStudio WPF (không SQL seed). Thứ tự: **F1 (nền đồng bộ config) TRƯỚC → F2 (engine-hóa màn)**.
> Spec: `docs/spec/16_CONFIG_SYNC_SPEC.md`.

### F1 — Đồng bộ config master → tenant (nền tảng, làm trước)
- [x] **CFGSYNC-0** — Chốt **5 quyết định mở** (§10 spec 16): ✅ DUYỆT 2026-06-15, toàn bộ theo khuyến nghị
      (master=Config DB canonical · row-level `DaTuyBien` · giữ bản tenant · provisioning+nút thủ công · `Is_Active=0`).
- [x] **CFGSYNC-1** — `db/050_alter_config_sync_flags.sql`: thêm cờ **`Is_System`+`Is_Customized`** (+`Synced_At`+`Source_Ver`,
      tên tiếng Anh cho nhất quán config DB) vào 11 bảng `Sys_Table`/`Sys_Resource`/`Sys_Lookup`/`Ui_Form`/`Ui_Tab`/
      `Ui_Section`/`Ui_Field`/`Ui_View*`/`Val_Rule` + bảng log `Sys_Config_Sync_Log`. Idempotent. ✅ **ĐÃ CHẠY 2026-06-21** (Config DB; 11 bảng + log).
- [x] **CFGSYNC-2** — `IConfigSyncService` (Application) + `ConfigSyncService` (Infrastructure): engine **descriptor-driven**
      UPSERT theo MÃ + re-link FK theo mã, đúng thứ tự phụ thuộc, một chiều, transaction/tenant, dry-run, tombstone
      `Is_Active=0`, giữ bản `Is_Customized`. Ghi `Sys_Config_Sync_Log`. Master = `ConnectionStrings:Config`.
      **✅ PHỦ TRỌN §2 + config-con (2026-06-25)** — `ConfigSyncTables.Order` **14 bảng**: `Sys_Table→Sys_Column→
      Ui_Form→Ui_Section→Ui_Field→Ui_Tab→Val_Rule→Ui_Field_Lookup→Sys_Resource→Sys_Lookup→Ui_View→Ui_View_Column→
      Ui_View_Action→Ui_View_Filter`. Engine nâng: khóa ghép nhiều cột (`LocalKeyColumns`: Sys_Resource=[Resource_Key,
      Lang_Code], Sys_Lookup=[Lookup_Code,Item_Code]) + bảng KHÔNG Id identity (Sys_Resource — INSERT không OUTPUT,
      match/UPDATE theo khóa nghiệp vụ) + khóa CHỈ-theo-cha cho bảng mở rộng 1-1 (Ui_Field_Lookup). Build Infrastructure 0/0.
      `db/050` đã cấp cờ 12 bảng; **`db/062`** cấp cờ cho Ui_Field_Lookup + Ui_View_Filter — ⏳ CẦN chạy trên Config DB.
      ⏳ CÒN: E2E preview/apply xác minh số dòng.
      **Lưu ý:** `Val_Rule_Field` đã DROP (migration 003) → chỉ sync `Val_Rule`; `Sys_Relation` ngoài phạm vi;
      `Ui_View.Detail_View_Id` self-FK an toàn khi NULL (rủi ro nếu master-detail tham chiếu vòng).
- [~] **CFGSYNC-3** — Action super admin "Cập nhật cấu hình từ master": `SyncConfigCommand`+Handler (gọi
      `IConfigSyncService`, invalidate `INavigationCache`) + `AdminConfigSyncController` `POST /api/v1/admin/config-sync`
      (áp thật, `[RequirePermission(administration.config-sync, Sua)]`) + `/preview` (dry-run, `Xem`). SUPERADMIN bypass.
      Build BE 0/0. ✅ **CC-4 (2026-06-25)**: handler bump `ICacheVersion` sau apply → vô hiệu cache config tenant ngay.
      ⏸️ **provisioning full-sync BỊ CHẶN** (chưa có luồng tạo tenant — catalog fallback; `SyncConfigCommand` đã là full-sync,
      chỉ cần gọi khi dựng tenant-create sau). · ✅ seed node `administration.config-sync` (db/057, 2026-06-21).

### F2 — Engine-hóa màn Công ty (sau F1)
- [x] **ORG-CFG-1** — `SchemaInspectorService.GetTableNamesAsync`: `TABLE_TYPE IN ('BASE TABLE','VIEW')` → liệt kê cả
      VIEW để design trên `vw_TC_CongTy`. Build WPF 0/0.
- [x] **ORG-CFG-2** — `db/051_create_vw_tc_congty.sql` (Data DB): `CREATE OR ALTER VIEW vw_TC_CongTy` JOIN cấp/phường-xã/
      tỉnh/ngân hàng/cha (FK id + tên), lọc IsDeleted=0, expose Id+CongTy_Cha_Id cho TreeList. ✅ **ĐÃ CHẠY 2026-06-21** (Data DB).
- [ ] **ORG-CFG-3** — ⏳ **THAO TÁC TAY trong ConfigStudio** (no-code, không SQL seed): đăng ký `vw_TC_CongTy`+`TC_CongTy`
      → `Ui_Form` Popup (TC_CongTy, lookup CapCongTy/PhuongXa/NganHang/Cha, i18n) + `Ui_View` TreeList **View_Code=`Tree_TC_CongTy`**
      (Key=Id, Parent=CongTy_Cha_Id, Edit_Form=form trên) → chạy config-sync.
      📖 **Hướng dẫn đầy đủ từng bước (2026-06-25): [docs/guide/cau-hinh-man-cong-ty.md](docs/guide/cau-hinh-man-cong-ty.md)**
      (khảo sát xác nhận: runtime TreeList ĐÃ đủ; chỉ thiếu DỮ LIỆU cấu hình — không có seed nào đăng ký màn này).
- [x] **ORG-CFG-4** — Routing placeholder→engine: `NavScreen.Route` (tuỳ chọn) + màn Công ty `Route="/view/Tree_TC_CongTy"`;
      NavMenu fallback dùng Route; `ScreenView` redirect khi màn có Route. Server-driven path dùng `HT_ChucNang.DuongDan` (data).
      Build FE 0/0.
### Danh mục nền tảng (cấu hình TRƯỚC màn tham chiếu — nguồn lookup)
> 7 danh mục db/037, module "Danh mục" (nhóm Hệ thống). Thứ tự config theo phụ thuộc:
> Quốc gia → Tỉnh/TP → Phường/Xã (cascade); ĐVT/Ngân hàng/Cấp công ty/Cấp phòng ban độc lập.
- [x] **CAT-CFG-1 (code)** — `db/052_create_vw_danhmuc.sql`: `vw_DM_TinhThanhPho` (+TenQuocGia) + `vw_DM_PhuongXa`
      (+TenTinhThanhPho). 5 danh mục phẳng dùng base table. ✅ **ĐÃ CHẠY 2026-06-21** (Data DB). + AppNav module `catalog`
      (7 NavScreen Route `/view/Grid_*`). Build FE 0/0.
- [ ] **CAT-CFG-2 (cấu hình tay)** — ConfigStudio: mỗi danh mục → Sys_Table (base/vw) + `Ui_Form` Popup + `Ui_View`
      Grid **View_Code=`Grid_{Bang}`** (khớp Route). Tỉnh: lookup QuocGia; Phường/Xã: lookup Tỉnh (cascade). → config-sync.
- [ ] **DATA-SCOPE** — (HOÃN) phân quyền dữ liệu: đọc qua SQL View + RLS `SESSION_CONTEXT` (P1). Thiết kế sau.

## 📋 Module upload file (TT_) — logo công ty + đính kèm (2026-06-26)

> **Phương án lưu trữ = A (bytes trong DB)** — chốt qua phân tích DB-blob vs đường-dẫn vs object-storage:
> logo nhỏ/ít → nhất quán giao dịch, portable đa máy, cô lập tenant, bảo mật qua endpoint. Cột `Storage_Kind`
> + `Storage_Key` để VỀ SAU cắm FileSystem (đường dẫn **tương đối**) / Object storage cho file lớn.
> TUYỆT ĐỐI tránh đường dẫn tuyệt đối (vỡ khi đổi máy + path traversal).

- [x] **FILE-DB (db/063)** — `TT_TepDinhKem` (Data DB, đúng chuẩn audit/IsDeleted) + FK `TC_CongTy.Logo_Id`→TT_TepDinhKem.
      ⏳ CẦN chạy trên Data DB tenant.
- [x] **FILE-BE** — `FilesController`: `POST /api/v1/files` (multipart, [Authorize], validate MIME allowlist png/jpeg/webp
      + **magic-byte** + size ≤2MB, sha256→Checksum) + `GET /api/v1/files/{id}` (stream inline + ETag/Cache-Control, 304).
      CQRS `UploadFileCommand`/`GetFileQuery` + `IFileAttachmentRepository`/`FileAttachmentRepository` (VARBINARY bind
      DbType.Binary size=-1) + DI. Build BE 0/0.
- [ ] **FILE-FE** — (đợt sau, user chốt "DB+BE trước") Editor_Type **`ImageUpload`** → `ImageUploadRenderer.razor`
      (InputFile → POST /files → set field = Id; preview qua HttpClient+objectURL vì serve cần Bearer) + đăng ký
      `Ui_Control_Map` + normalizer `Editor_Type→FieldType`. Wire `Logo_Id` vào form Công ty.
- [ ] **FILE-SEC** — cân nhắc cho phép/sanitize SVG (hiện CẤM SVG vì nhúng script). Dọn file mồ côi (Logo_Id thay đổi).

## 📋 Màn Công ty — sửa theo schema thật (2026-06-26)

> Schema đã đổi (user cung cấp DDL): `TC_CongTy` bỏ `NganHang_Id`, thêm `ChiNhanhNganHang_Id`/`Logo_Id`/`NgayThanhLap`;
> `vw_TC_CongTy` JOIN `DM_ChiNhanhNganHang→DM_NganHang`, expose `TinhThanhPho_Id`/`TenTinhThanhPho`.

- [ ] **CONGTY-GUIDE-FIX** — sửa [cau-hinh-man-cong-ty.md](docs/guide/cau-hinh-man-cong-ty.md): Công ty cha = **TreePicker**
      (`treelookup`); **Tỉnh ảo→Phường/Xã** cascade (`Reload_Trigger_Field`/`Parent_Column`=TinhThanhPho_Id);
      **Ngân hàng ảo→ChiNhanhNganHang** cascade; Logo = editor ImageUpload; thêm NgayThanhLap.
- [ ] **CHINHANH-FIX** — (khuyến nghị) chuẩn hóa `DM_ChiNhanhNganHang`: `Id BIGINT IDENTITY PK`, FK `NganHang_Id→DM_NganHang`,
      thêm audit + IsDeleted (mọi bảng nghiệp vụ phải có — ADR-022). ⚠️ Đổi Id→IDENTITY cần rebuild bảng nếu đã có dữ liệu.

## 📋 Roadmap — Menu động + Phân quyền (phase-auth) — ADR-023

> Thiết kế CHỐT: `docs/spec/15_AUTHZ_NAVIGATION_SPEC.md` + ADR-023. Menu server-driven từ
> `HT_ChucNang` (lọc `HT_VaiTro_Quyen.Xem=1` theo role). Master `Sys_MenuCatalog` (Config DB/WPF)
> → đồng bộ xuống `HT_ChucNang` mỗi tenant. `AppNav.cs` → seed + fallback. Chưa code.

### Giai đoạn 1 — Migration + seed ✅ (scripts viết xong — CHƯA áp vào DB)
- [x] **AUTHZ-DB-1** — `db/042_alter_ht_chucnang_authz.sql`: thêm `Menu_Id, LaHeThong, KichHoat, ViTriHienThi` + index (idempotent).
- [x] **AUTHZ-DB-2** — `db/043_create_sys_menu_catalog.sql` (tạo `Sys_Menu`+`Sys_MenuCatalog`) + `db/044_seed_sys_menu_catalog.sql` (seed `MAIN` + 45 node từ `AppNav`).
- [x] **AUTHZ-DB-3** — `db/045_seed_ht_chucnang_base.sql`: seed `HT_ChucNang` base (`LaHeThong=1`), nối cha-con theo `Ma`; grant `SUPERADMIN` Xem/Thêm/Sửa/Xóa/In toàn bộ. CreatedBy=admin tường minh.
- [ ] **AUTHZ-DB-APPLY** — ⏳ Chạy 042→(043,044 Config DB)→045 trên DB thật (chưa áp).

### Giai đoạn 2 — Backend đọc
- [x] **AUTHZ-BE-1** — `MeNavigationDto`/`MeNavNodeDto` + `GetMyNavigationQuery`+Handler + `MeController [Authorize] GET /api/v1/me/navigation` (userId từ claim sub; gộp navigation + cờ quyền 1 call). Compile sạch (build fail chỉ do file-lock app đang chạy).
- [x] **AUTHZ-BE-1b** — `INavigationRepository` + `NavigationRepository` (Dapper, recursive CTE: Xem=1 + tổ tiên, cờ MAX theo OR vai trò, không `SELECT *`) + DI.
- [ ] **AUTHZ-BE-2** — Cache `IConfigCache` theo tenant+role + invalidate khi sửa quyền/menu. _(chưa làm)_

### Giai đoạn 3 — Frontend tiêu thụ
- [x] **AUTHZ-FE-1** — `NavigationApiService` (GET /me/navigation, lỗi→rỗng) + model `MeNavNode`/`MeNavigationResult` + DI. Build xanh.
- [x] **AUTHZ-FE-2** — `NavMenu.razor` dựng VM từ API (node phẳng → group/module/screen, lọc ViTriHienThi Sidebar/Ca2); **rỗng/lỗi → fallback AppNav** (app vẫn chạy khi chưa seed DB). Bỏ `CanShow`.
- [x] **AUTHZ-FE-3 (ẩn nút)** — nav trả `DoiTuong/LoaiDoiTuong`; `PermissionState` (nạp /me/navigation, `ForTarget(type,code)`, chưa map→cho phép); `MasterDataListPage` ẩn **Thêm** + truyền `CanEdit/CanDelete`; `MasterDataGrid` ẩn **Sửa/Xóa**. Build xanh.
- [x] **AUTHZ-FE-CACHE** — `PermissionState` (scoped) nạp 1 lần/phiên, dùng chung cho ẩn nút (+ BE đã cache /me/navigation). NavMenu vẫn gọi riêng (rẻ vì BE cache).
- [ ] **AUTHZ-FE-3b** — sub-nav `ViTriHienThi=TrongMan` + ẩn nút trên `DataView` (View) — chờ màn HR thật. _(sau)_

### Giai đoạn 4 — Bảo mật + cấu hình (trước production)
- [x] **AUTHZ-SEC (hạ tầng + admin)** — `IPermissionService.HasPermissionAsync` (Dapper HT_VaiTro_Quyen) + attribute `[RequirePermission(funcCode, Op)]` (IAsyncAuthorizationFilter: userId từ claim sub, bypass role SUPERADMIN, 403 deny-by-default) + DI. Gắn vào `AdminPermissionController` (Xem/Sua trên `administration.permissions`) — bịt lỗ "ai đăng nhập cũng sửa quyền".
- [x] **AUTHZ-SEC-2** — `db/046` thêm `DoiTuong`+`LoaiDoiTuong` vào `HT_ChucNang` (link form/view); `IPermissionService.HasPermissionForTargetAsync` (**enforce-if-mapped**) + `[RequirePermissionForTarget]`; gắn `MasterData`(Form: Xem/Thêm/Sửa/Xóa) · `View`(View: Xem) · `Runtime`(Form: Xem). Build xanh. ✅ db/046 đã áp thủ công 2026-06-15 (sqlcmd trực tiếp — cột thiếu gây 500 /me/navigation).
  - [ ] `FormController` (config metadata) enforce + map form↔chức năng tự động khi dựng màn thật. _(sau)_
- [x] **AUTHZ-UI-1 (BE)** — API admin phân quyền: `GET /api/v1/admin/roles`, `GET roles/{id}/permissions` (cây + cờ), `PUT roles/{id}/permissions` (upsert `HT_VaiTro_Quyen` trong transaction). CQRS + `IPermissionAdminRepository`/`PermissionAdminRepository` + `AdminPermissionController [Authorize]` + DI. Compile sạch.
- [x] **AUTHZ-UI-1 (FE)** — Màn Phân quyền `/m/administration/permissions` (route literal ưu tiên hơn ScreenView): chọn vai trò → `DxTreeList` cây + 5 `DxCheckBox` (Xem/Thêm/Sửa/Xóa/In) → PUT lưu. `AdminPermissionApiService` + models + DI + CSS. Build xanh. _(Cascade tick + invalidate cache: TODO)_
- [x] **AUTHZ-UI-2 (Vai trò)** — Engine MasterData **tự bơm audit** (CreatedBy/At insert · UpdatedBy/At update theo cột tồn tại; userId luồn qua `SaveMasterDataCommand`←claim). `db/047` seed `Sys_Table`/`Sys_Column`/`Ui_Form`/`Ui_Field` cho `HT_VaiTro`; `db/048` nối menu `administration.roles` → `/master/HT_VaiTro` + `DoiTuong`. Build BE xanh. ⏳ chạy db/047 (Config) + db/048 (Data) + restart API.
  - [ ] **AUTHZ-UI-2b (Người dùng)** — `HT_NguoiDung` field nhạy cảm (MatKhauHash/2FA) → màn **bespoke** (đặt mật khẩu đúng cách), KHÔNG form generic. _(sau)_

### Pha sau
- [ ] `ChucNangCon` (quyền cấp nút) · `Sys_Menu` nhiều bộ menu (Top/Mobile) · `Duyet` cho workflow engine.

## 📋 Roadmap — ConfigCache facade (đọc config qua cache, hạn chế chọc DB) — ADR-014

> Mục tiêu: 1 facade `IConfigCache` đọc mọi *config* (metadata, i18n, lookup, permission) qua
> L1(mem)+L2(Redis); web/handler chỉ dùng cache, miss mới đọc DB. Tách Config (cache) vs Data (không cache).
> Invalidation: Version-stamp (scale-out) + Event-remove + TTL. Chi tiết: ADR-014.

### Giai đoạn 0 — Nền tảng facade
- [x] **CC-0a** — `IConfigCache` (Application/Interfaces): `GetFormMetadata`, `GetResourceMap(scope,lang,tenant)`, `ResolveKey(key,lang,tenant)`, `GetLookupOptions(code,lang,tenant)`, `GetFormPermissions(formId,tenant)`. ✅ (2026-06-07) + entity `FormPermission` (Domain/Entities/Permission, deny-by-default). Build backend+WPF 0/0. _Kèm fix build commit 49738e7: `InsertLookupCommandHandler` CS0136 (biến `v` trùng scope)._
- [x] **CC-0b** — `ConfigCache` (Application/Engines) ✅ (2026-06-07). Form metadata ủy quyền `MetadataEngine` (không double-cache); i18n resource map + lookup options cache-aside qua `ICacheService`. `ResolveKeyAsync` derive scope = đoạn trước dấu `.` đầu → reuse resource map (gồm cả global `sys.*`). Key mới `ConfigResourceMap`/`ConfigLookup`/`ConfigPermission` gắn slot `:v{version}` (const 0, version-stamp ready CC-4a). Permission tạm trả null (đợi repo CC-3). Build 0/0.
- [x] **CC-0d** (phần DI) — đăng ký `IConfigCache→ConfigCache` scoped trong `Application/DependencyInjection.cs` ✅. _Còn lại: enforce convention cấm inject repo config trực tiếp (làm cùng CC-1a)._
- [x] **CC-0c** — Stampede lock per-key + negative cache ✅ (2026-06-07). Helper `GetOrLoadAsync<T>` trong `ConfigCache`: check cache → giành `SemaphoreSlim` per-key (`ConcurrentDictionary` static) → double-check → load → cache. Kết quả rỗng (`isEmpty`) dùng `NegTtl=30s` thay TTL dài (config mới xuất hiện sớm). Áp cho resource map + lookup options. Full backend build `ICare247.slnx` **0/0** (đã stop API rồi build lại).

### Giai đoạn 1 — i18n resource (ưu tiên — dọn anti-pattern hiện tại)
- [x] **CC-1a** — `SaveMasterDataCommandHandler` + `InsertLookupCommandHandler` ✅ (2026-06-07): bỏ inject `IResourceRepository`, resolve message trùng qua `IConfigCache.ResolveKeyAsync` (per-field key → `sys.val.unique` template → hardcode). Grep xác nhận 2 handler chỉ còn nhắc `IResourceRepository` trong comment. Build 0/0.
- [x] **CC-1b** — Rà toàn bộ runtime path resolve i18n ✅ (2026-06-07). Khảo sát 3 caller Validate*: RuntimeController (đã truyền `form.ResourceMap` — OK). Phát hiện + sửa **2 bug i18n thật**: (1) `SaveMasterDataCommandHandler` truyền `resourceMap: null` → rule message hiện raw Error_Key; giờ lấy map qua `IConfigCache.GetResourceMapAsync`. (2) `EventEngine` TRIGGER_VALIDATION không truyền map → thêm `FormCode`+`LangCode` vào `FormEvent`, EventEngine inject `IConfigCache` lấy map. `ResourceResolver` = pure helper trên map có sẵn (không bypass); repo resolve label bằng SQL = data layer facade bọc (hợp lệ). Build 0/0.

### Giai đoạn 2 — Lookup options
- [x] **CC-2** — Lookup options qua facade ✅ (2026-06-07). `GetLookupByCodeQueryHandler` bỏ tự cache-aside (key cũ `CacheKeys.Lookup` — đã xóa dead code) → delegate `IConfigCache.GetLookupOptionsAsync` (hưởng stampede+negative cache CC-0c). Thêm `IConfigCache.InvalidateLookupAsync` + endpoint `POST /api/v1/lookups/{code}/invalidate-cache` (mirror form invalidate). Build 0/0. _Wiring WPF gọi endpoint khi sửa Sys_Lookup = CC-4b._

### Giai đoạn 3 — Permission (✅ schema CHỐT tại ADR-023 — gộp vào roadmap phase-auth ở trên)
> **Cập nhật 2026-06-13:** schema quyền đã chốt = **`HT_VaiTro_Quyen`** (role × chức năng × Xem/Thêm/Sửa/Xóa/In,
> Data DB per-tenant) — KHÔNG dùng `Sys_Permission` (Config DB) như dự kiến cũ. Quyền theo **role** (không theo
> user trực tiếp), node = `HT_ChucNang`. Việc triển khai nằm ở roadmap **phase-auth (ADR-023)** đầu file
> (AUTHZ-*). `ConfigCache.GetFormPermissionsAsync`/entity `FormPermission` map từ `HT_VaiTro_Quyen` theo role.
- [ ] **CC-3a** — Thiết kế + migration bảng `Sys_Permission` (role/user × form × CRUD, tenant scope). _Tiền đề._
- [ ] **CC-3b** — `IPermissionRepository` + impl Dapper (đọc theo form+tenant, map sang `FormPermission`).
- [ ] **CC-3c** — `ConfigCache.GetFormPermissionsAsync` đọc qua repo + cache key `ConfigPermission` (đã có) + `InvalidatePermissionAsync` + endpoint invalidate.
- [ ] **CC-3d** — Runtime enforce: web/handler đọc `GetFormPermissions` → ẩn nút + chặn thao tác (deny-by-default).

### Giai đoạn 4 — Invalidation nâng cấp (khi scale-out ≥2 instance)
- [~] **CC-4a** — Version-stamp: ✅ **mức 1 instance (session 56)** — `ICacheVersion` in-memory theo tenant gắn vào
      ConfigCache + MetadataEngine keys (`:v{n}`); flush qua `POST /api/v1/admin/cache/flush`. ⏳ Scale-out ≥2 instance →
      chuyển version sang Redis `INCR cfgver:{tenant}` (`cfgver:{tenant}:{form}` cho metadata nếu cần tách).
- [ ] **CC-4b** — ConfigStudio (WPF) write path → INCR version (hoặc gọi API invalidate) sau khi lưu cấu hình.
- [ ] **CC-4c** — Bỏ dần Event-remove khi version-stamp ổn (giữ TTL làm lưới an toàn).

### Nguyên tắc cứng (review checklist)
- Config → cache; Data (bản ghi nghiệp vụ) → KHÔNG cache.
- Web/Handler không đọc repo config trực tiếp — chỉ `IConfigCache`.
- Mọi cache key có `{tenant}` (+ `{lang}` nếu i18n) (+ `:v{n}` version-ready).

---

## 📋 Roadmap — Tách `ICare247.ApiClient` SDK dùng chung cho web app

> Bối cảnh: `ICare247.Blazor.RuntimeCheck` (WASM, chỉ là project test) tự viết `FormApiService`,
> `MasterDataApiService`, `LookupApiService` (HttpClient) + DTO. Web app thật sau này cần y hệt
> → tách thư viện client SDK dùng chung, tránh lặp code mỗi frontend.
>
> Kiến trúc: backend (Api @ :7130) giữ nguyên; mọi web app là CLIENT gọi API qua SDK này.
> Cache/config nằm server-side sau API — client không tự cache config / không chọc DB.

- [ ] **SDK-1** — Tạo project `ICare247.ApiClient` (class lib net9.0): các client `FormApiClient`,
      `MasterDataApiClient`, `LookupApiClient`, `RuntimeApiClient` (validate/event) + helper gắn
      `X-Tenant-Id` header và `?lang`.
- [ ] **SDK-2** — Chuyển DTO chia sẻ vào SDK (FormMetadataDto, FieldMetadataDto, MasterData DTOs,
      LookupItem…) — nguồn sự thật chung, khỏi viết lại mỗi web app.
- [ ] **SDK-3** — Refactor `RuntimeCheck` dùng `ICare247.ApiClient` thay cho service/DTO nhúng riêng
      (verify chạy lại như cũ).
- [ ] **SDK-4** — Web app thật: tạo project presentation mới, reference `ICare247.ApiClient`, build UI.
      (Backend KHÔNG sửa — chỉ thêm client.)
- [ ] **SDK-note** — Cân nhắc generate client từ OpenAPI (Scalar/NSwag) thay vì viết tay nếu API ổn định.

---

## 📋 Roadmap — Ui_View (cấu hình hiển thị danh sách: Grid/TreeList) — ADR-015

> Mục tiêu: cấu hình **hiển thị danh sách** (Grid/TreeList) metadata-driven, **tách khỏi form sửa**.
> 3 bảng: `Ui_View` (header + datasource + hành vi + export/print + TreeList), `Ui_View_Column`
> (cột + render/export/format), `Ui_View_Action` (nút toolbar/row). Mọi text qua i18n (scope `table_code`).
> Thiết kế chốt: `docs/spec/14_VIEW_CONFIG_SPEC.md` + ADR-015 + spec 10 §1d. Handoff: `AI_HANDOFF.md` (VIEW-0).
> Render giàu ≠ dữ liệu xuất: export lấy giá trị thuần; pdf/docx = server-side template, xlsx/csv = DxGrid.

### Giai đoạn 0 — Thiết kế (✅ chốt 2026-06-07)
- [x] **VIEW-0** — Chốt thiết kế 3 bảng + i18n + engine rules ✅ (spec 14, ADR-015, spec 10 §1d, handoff VIEW-0).

### Giai đoạn 1 — Database + tương thích (owner: Codex)
- [x] **VIEW-1a** — Migration `db/031_create_ui_view.sql`: tạo `Ui_View` + `Ui_View_Column` + `Ui_View_Action` theo DDL spec 14 (idempotent IF OBJECT_ID/IF NOT EXISTS, FK Sys_Table/Ui_Form/Sys_Tenant/Sys_Column, unique global/tenant). Đã chạy DB dev + bổ sung repo (session 43, Claude).
- [x] **VIEW-1b** — `db/032_seed_default_views.sql`: seed 1 Grid view (`Grid_{Form_Code}`) cho mỗi `Ui_Form` active, cột từ field `Show_In_List=1` (bỏ ảo, Field_Name=Column_Code, Caption_Key=Label_Key), `Edit_Form_Id`=chính form. Idempotent (NOT EXISTS). ⏳ Cần chạy trên DB.
- [x] **VIEW-1c** — Cập nhật `docs/spec/02_DATABASE_SCHEMA.md`: thêm `Ui_View` + `Ui_View_Column` + `Ui_View_Action` (cuối module UI, tham chiếu spec 14 + migration 031/032).
- [x] **VIEW-1run** — Migration đã chạy trên DB dev ✅ (user).

### Giai đoạn 2 — Backend (owner: Claude)
- [x] **VIEW-2a** — Domain: `ViewMetadata` / `ViewColumn` / `ViewAction` (`Entities/View`) — text i18n resolve sẵn (Title/Caption/Label/...). Build backend 0/0.
- [x] **VIEW-2b** — `IViewRepository` + `ViewRepository` (Dapper, Config DB): `GetByCodeAsync` (header + columns + actions), resolve i18n qua Sys_Resource theo langCode, ưu tiên bản tenant-specific hơn global; đăng ký DI. (Fallback caption `Label_Key` field → Field_Name để tầng Blazor VIEW-3 xử lý.)
- [x] **VIEW-2c** — `IConfigCache.GetViewAsync` (cache-aside L1+L2, key `CacheKeys.View` slot `:v0`) + `InvalidateViewAsync`; `GetViewByCode` handler ủy quyền facade (đúng ADR-014); inject `IViewRepository` vào `ConfigCache`. (ResourceMap prefix `{tableCode}.view.%` chưa cần — caption resolve trực tiếp trong repo.)
- [x] **VIEW-2d** — CQRS `Features/Views/Queries/`: `GetViewByCode` (metadata) + `GetViewData` (data, Source_Type='Table'): nạp metadata qua facade → `ViewRepository.GetDataAsync` SELECT cột Data (Field_Name whitelist) từ bảng nguồn (Data DB), search LIKE (CAST NVARCHAR) + paging. (View/Sp/Api source → `NotSupportedException`, làm sau.)
- [~] **VIEW-2e** — `ViewController`: GET `{code}/info` (metadata) ✅, GET `{code}/data` (data list) ✅, POST `{code}/invalidate-cache` ✅. Còn: export server-side (pdf/docx theo template) — **hoãn**, cần template engine chưa có.

### Giai đoạn 3 — Blazor runtime (owner: Claude)
- [x] **VIEW-3a** — `ViewApiService` + DTO (`ViewMetadataDto`/`ViewColumnDto`/`ViewActionDto`/`ViewDataResultDto`) gọi `api/v1/views/{code}/info` + `/data` (unwrap JsonElement → CLR). Component đọc metadata trực tiếp (không cần map sang MasterDataGridConfig).
- [~] **VIEW-3b** — Component `DataView` render `<DxGrid>` / `<DxTreeList>` (theo `View_Type` + Key/Parent field) ✅; page `ViewPage` route `/view/{ViewCode}` (search + paging + Add/Edit/Delete điều hướng Edit_Form) ✅. Còn: alias `/master/*` chuyển tiếp sang view mặc định.
- [x] **VIEW-3c** — Render `Render_Mode` Text/Boolean/Html/**Image/Link/Badge** (RenderTreeBuilder) + **conditional format** `Style_Rule_Json` (mảng rule `{when:{field,op,value}, style:{color/background/fontWeight}}`, client-eval, rule đầu khớp thắng). Template fallback text; Grammar V1 AST đầy đủ (thay format JSON đơn giản) làm sau nếu cần điều kiện phức tạp.
- [x] **VIEW-3d** — Toolbar render nút động từ `Ui_View_Action` (Scope Toolbar/Both, Order_No): Export→client; BuiltIn add/refresh→callback; Navigate→Target; row Sửa/Xóa qua Edit_Form. Print/Event/Api/export-server → `OnUnhandledAction` báo "chưa hỗ trợ". (Confirm_Key cho Xóa chưa wire.)
- [~] **VIEW-3e** — Export client xlsx/csv qua `DxGrid.ExportToXlsxAsync/ExportToCsvAsync` (giá trị thuần theo FieldName, bỏ template) ✅; pdf/docx (Engine='Server') → báo chưa hỗ trợ. Còn: tôn trọng `Allow_Export=0` per-column (hiện DxGrid xuất mọi cột Data) + header export theo langCode.
- [x] **VIEW-3f** — Endpoint list views `GET /api/v1/views` (CQRS `GetViewsList` + `IViewRepository.GetListAsync`, ROW_NUMBER khử trùng code ưu tiên tenant) + trang `TestGrid` (`/test-grid`) chọn View từ danh sách → render `DataView`. **Fix bug** `DxGridDataColumn.FilterRowCellVisible` (không tồn tại DX 25.2.3) → `FilterRowEditorVisible` (rớt toàn bộ cột Data). Đi dây thuộc tính grid UX: grid `ColumnResizeMode/AllowColumnReorder/HighlightRowOnHover/FocusedRowEnabled/KeyboardNavigationEnabled`; cột `MinWidth` + **ghim `FixedPosition`** (Fixed_Position) + sort mặc định `SortIndex/SortOrder` (thêm field vào `ViewColumnDto`). Doc tra cứu: `docs/reference/DEVEXPRESS_CONTROLS_PROPERTIES.md` (reflect DLL v25.2.3).
- [x] **VIEW-3f.1** — Filter operator **Mức 1**: ô filter row tự chọn toán tử mặc định theo kiểu (text→`Contains`, boolean/cột canh phải→`Equal`) + `FilterMenuButtonDisplayMode=Always` để user đổi `Contains/StartsWith/EndsWith/…` lúc chạy. Helper `FilterOpOf` trong `DataView`. Enum: `GridFilterRowOperatorType` (Default/Equal/NotEqual/StartsWith/EndsWith/Contains/Less/LessOrEqual/Greater/GreaterOrEqual).
- [ ] **VIEW-3i** (để sau) — Khôi phục **Xóa trên /view/{code}**: từ session 44 đã gỡ kebab khỏi `DataView` (dùng chung) → ViewPage mất nút Xóa (Sửa double-click + Thêm vẫn còn; `OnDelete` nối nhưng không được gọi). Port cơ chế **chọn nhiều dòng + toolbar Xóa** (soft-FK all-or-nothing, confirm) từ `TestGrid` sang `ViewPage` cho nhất quán. Tạm thời /view xóa qua màn /master.
- [ ] **VIEW-3h** (để sau) — Filter operator **Mức 2 metadata-driven**: thêm cột `Filter_Operator` (+ tuỳ chọn `Filter_Mode`=Value/DisplayText) vào `Ui_View_Column` (DB migration) → entity `ViewColumn` → `ViewRepository` SQL → `ViewColumnDto` → `DataView` (thay `FilterOpOf` suy đoán bằng giá trị cấu hình) → ConfigStudio grid cột (dropdown chọn toán tử). Mặc định giữ logic Mức 1 khi cấu hình trống.
- [ ] **VIEW-3g** (để sau — chờ quyết định) — **Lưu giao diện grid theo user** (độ rộng/thứ tự/ghim/sort/group/filter). DxGrid có sẵn `LayoutAutoSaving/LayoutAutoLoading` + `SaveLayout/LoadLayout`. Hướng A: localStorage theo `View_Code` (không cần auth, theo trình duyệt). Hướng B: bảng `Ui_View_User_Layout(User_Id, View_Id, Layout_Json)` + endpoint save/load (cần hệ thống đăng nhập — RuntimeCheck hiện chỉ có `X-Tenant-Id`, chưa có User). Chốt hướng trước khi làm.

### Giai đoạn 4 — ConfigStudio WPF (owner: Codex → Claude làm session 42)
- [x] **VIEW-4a** — Màn "Quản lý View" (`ViewManagerView`/`ViewManagerViewModel`, module Forms): list + CRUD `Ui_View` (header, datasource, hành vi, export/print, TreeList) qua `IViewDataService`/`ViewDataService` (Dapper, transaction, optimistic concurrency). Build WPF 0/0.
- [x] **VIEW-4b** — Lưới cấu hình cột (`Ui_View_Column`) editable: Field_Name, caption key, kind, render mode, width, align, format, visible, sort/filter/group, summary, export + up/down order.
- [x] **VIEW-4c** — Lưới `Ui_View_Action` editable (code/type/scope/label key/icon/export format/engine/target/require-selection). Lưu cột+action nguyên khối cùng View.
- [x] **VIEW-4d** — nút "🌐 Dịch" i18n (tái dùng `I18nEditorDialog`) cho Title_Key (tab Cơ bản), Export_File_Name_Key (tab Export), Caption_Key (toolbar tab Cột — cột đang chọn), Label_Key (toolbar tab Actions — action đang chọn) + tự sinh key theo convention `{tableCode}.view.{viewCode}.{suffix}` (spec 10 §1d) khi trống; column picker từ `Sys_Column` (`ColumnPickerDialog`, nạp lười theo bảng nguồn). Build WPF 0/0. ⚠️ Màn chỉ chạy được sau khi chạy migration `Ui_View` (VIEW-1) — service ném lỗi thân thiện nếu thiếu bảng.
- [x] **VIEW-4e** (polish UX, session 43) — (1) View_Code = `{View_Type}_` + hậu tố user nhập (badge tiền tố + preview); đổi View_Type giữ hậu tố; **đổi View_Code tự rekey** mọi i18n key đã sinh (`.view.{cũ}.`→`.view.{mới}.`). (2) Nút lưu đổi nhãn "Lưu"; "Tạo mới" có MessageBox cảnh báo. (3) Tab Cơ bản sắp lại thứ tự ①View_Type→②View_Code→③Bảng nguồn→④Source. (4) Caption_Key/Label_Key trong 2 grid đổi thành cột i18n khóa-gõ-tay + nút 🌐 mỗi dòng. (5) `ColumnPickerDialog` thêm **multi-select** (checkbox + "Chọn (N)") + khóa cột đã có (badge "đã thêm") — giữ tương thích single-select màn FieldConfig. (6) `GridSplitter` kéo co giãn 2 panel master-detail. Build WPF slnx 0/0.

- [x] **VIEW-4f** — Tab "Cột" (`ViewManagerView.xaml`) bổ sung 4 cột chỉnh: **MinWidth**, **Ghim** (`FixedPosition` combo none/left/right), **SortMặc định** (`SortOrder` combo asc/desc), **SortIdx** (`SortIndex`) — để admin cấu hình tính năng grid mới (đồng bộ Blazor VIEW-3f). Model/`ViewDataService` đã lưu sẵn, chỉ thiếu UI. Build WPF 0/0.

### Nguyên tắc cứng (review checklist)
- Mọi text hiển thị = `_Key` (i18n, scope `table_code`); không literal caption.
- Export = giá trị thuần (bỏ HTML); pdf/docx server-side; xlsx/csv client DxGrid.
- Cache view qua `IConfigCache` (key có tenant + lang + version); ConfigStudio đọc/ghi DB trực tiếp (ADR-007).

---

## 📋 Roadmap — Vertical Thu mua nông sản ký gửi (Ngọc Chương) — Spec 29 (2026-07-15)

> Spec: `docs/spec/29_THU_MUA_NONG_SAN_SPEC.md` — trích xuất từ legacy `src/frontend/source_can_update/`
> (WPF .NET 4.8, 63 VM CategoryNgocChuong). Đã chốt: tiền tố `KD_`/`KT_`, thiết kế generic ngành
> thu mua nông sản (cà phê = tenant đầu), posting engine C# cùng transaction (khớp ADR-029),
> spec viết trọn trước khi code. KHÔNG port code legacy — chỉ dùng làm spec nghiệp vụ.

- [x] **TM-000** — Viết spec 29: glossary tiếng Việt thống nhất (hết nhồi nghĩa cột), schema `KD_`/`KT_`, cây nghiệp vụ + định khoản cấu hình, lộ trình 6 đợt ✅ Done (2026-07-15)
- [ ] **TM-001** — Chốt 5 câu hỏi mở (spec 29 §9): giá xuất kho, cách tính lãi vay, số dư đầu kỳ, đa DVT, thứ tự code ADR-027
- [ ] **TM-002** — Đ1: migration `KD_`/`KT_`/`DM_TaiKhoan`/`DM_HangHoa_QuyDoi` + màn ConfigStudio cấu hình cây nghiệp vụ & định khoản + seed mẫu cà phê (phụ thuộc ADR-027 chưa code)
- [ ] **TM-003** — Đ2: posting engine C# (sinh `KT_ButToan` từ config, cùng transaction) + unit test bất biến Nợ=Có
- [ ] **TM-004** — Đ3→Đ6: các cụm màn theo spec 29 §8 (mua/cân/ký gửi → chốt giá → sơ chế/tín dụng → bán/báo cáo)

---

## 📋 Roadmap — Form chứng từ master-detail (capability Form Engine) — Spec 30 (2026-07-15)

> Spec: `docs/spec/30_FORM_CHUNG_TU_SPEC.md`. Xuất phát: đánh giá 3 kiểu màn vertical thu mua
> (spec 29 §10) — kiểu ③ "1 đơn / 1 khách / n dòng hàng / nhiều giá" là gap lớn nhất nền tảng,
> nhưng là năng lực chung mọi module ERP cần. Trụ kiến trúc: AstParser/AstCompiler ở
> `ICare247.Domain` thuần C# → WASM tham chiếu trực tiếp ⇒ công thức dòng chạy client-side.

- [x] **FDOC-000** — Viết spec 30: `Ui_Form_Detail` (cột lưới = Ui_Field form con), `Ui_Field.Formula_Json` (AST), DetailGridRenderer, event server mức dòng (UiDelta + RowContext), save-document 1 transaction ✅ Done (2026-07-15)
- [x] **FDOC-001** — ~~Chốt 4 câu hỏi mở~~ ✅ Done (2026-07-15, spec 30 §8): EditMode = **cả 3 chế độ** per lưới, mặc định **EntryPanel** (khu nhập trên + lưới dưới kiểu legacy) · aggregate lên master **ngay FDOC-3** · vệ tinh 1-1 = **section field** (không lưới 1 dòng) · **có ca 100+ dòng** → virtual scroll từ FDOC-2
- [ ] **FDOC-1→6** — Triển khai theo spec 30 §7 (migration+ConfigStudio → renderer read → EntryPanel+công thức client → CellInline/RowPopup → save tổ hợp → event giá → ConfigSync+E2E màn Mua hàng tươi)
- [ ] **VIEWMD-001** — Master-detail 2 lưới cho màn list: hiện thực `Detail_View_Id` theo spec 14 §11 (runtime ViewPage + khóa liên kết Options_Json + ô cấu hình ConfigStudio) — kiểu màn ② spec 29 §10 đang thiếu đúng mảnh này

---

## 🟠 Kế hoạch (Next Up)

### Backend — Claude Code

- [x] **BE-001** — ~~Implement `IMetadataEngine`~~ ✅ Done — MetadataEngine.cs đã implement đầy đủ (verified 2026-05-31)
- [x] **BE-005** — ~~Is_Virtual field~~ ✅ Done (commit 49f9daf, 2026-05-31) — db/018, Domain, FieldRepository, FormRepository, Blazor, WPF
- [x] **WPF-15** — ~~Đồng bộ Schema không lưu DB~~ ✅ Done (commit ba7e2ac, 2026-06-01) — thêm `DeleteFieldAsync` + `PersistSyncSchemaAsync`
- [x] **WPF-16** — ~~+ Field không lưu được~~ ✅ Done (commits d93539e, 2d244d1, dcc42f6, 2026-06-01) — temp Id âm + auto-open FieldConfigView + Column_Id nullable (migration 019)
- [x] **WPF-17** — ~~Virtual field full stack~~ ✅ Done (2026-06-01) — db/019+020, FieldCode, Section picker, Column TextEdit, UX Behavior tab
- [ ] **BE-002** — Integration tests: ValidationEngine + EventEngine + MetadataEngine ❌ **Chưa làm**
- [ ] **BE-003** — Test Blazor end-to-end với API + DB thật ⏳ Manual test
- [x] **BE-004** — ~~Apply Design System tokens + wire `--dx-*`~~ ✅ Done (commit `5fc36c4`, 2026-06-11) — chuyển theme DevExpress sang **Fluent Light** (lắp 4 file modular) + accent xanh `#0F6CBD`; `app.css` viết lại theo token ERP; **bỏ hướng `--dx-*` override** (DLL không có biến đó, Fluent tự lo accent). Đổi theme/màu về sau = thay 1 file `accents/*`/`modes/*`.

### WPF ConfigStudio

- [x] **WPF-13** — ~~Pass `tableCode` khi navigate FieldConfig → I18nManager~~ ✅ Done (code đã có, verified 2026-05-31)
- [x] **WPF-10** — ~~ValidationRuleEditor: Compare rule field list → ComboBoxEdit~~ ✅ Done (commit 044219e)
- [x] **WPF-11** — ~~FormSummaryDto: thêm EventCount subquery~~ ✅ Done (verified 2026-05-31)
- [x] **WPF-12** — ~~I18n Manager: Export/Import CSV/JSON~~ ✅ Done (commit 037bc34)
- [ ] **WPF-14** — Test LookupBox end-to-end (GioiTinh + PhongBanID) ⏳ Manual test

---

## 🐛 Bugs / Issues

<!-- Ghi lại bug phát sinh trong quá trình code -->

---

