# AI Handoff Log — ICare247 Core

Ghi lại mỗi khi bàn giao task giữa Claude Code và Codex.
**Newest first.** Cập nhật ngay khi bắt đầu hoặc hoàn thành một task.

---

## Template

```
### [YYYY-MM-DD] <TASK-ID> — <from> → <to>
- Status: in_progress | blocked | done
- Files: <danh sách file chính đã/sẽ sửa>
- Cần biết: <thông tin quan trọng cho agent nhận>
- Bước tiếp theo: <1 hành động cụ thể>
```

---

## Entries

### [2026-06-15] CAT-CFG-1 (danh mục nền tảng — code + routing) — claude

- Status: code done (build FE **0/0**); ⏳ chạy `db/052` + **CAT-CFG-2 cấu hình tay** + sync.
- Files:
  - DB: `db/052_create_vw_danhmuc.sql` — `vw_DM_TinhThanhPho` (+TenQuocGia), `vw_DM_PhuongXa` (+TenTinhThanhPho).
  - FE: `Navigation/AppNav.cs` — module `catalog` ("Danh mục", nhóm system) + 7 NavScreen Route `/view/Grid_*`.
- 7 danh mục (db/037): DM_QuocGia, DM_TinhThanhPho(FK), DM_PhuongXa(FK), DM_DonViTinh, DM_NganHang,
  TC_CapCongTy, TC_CapPhongBan. Chỉ 2 bảng FK cần view; 5 phẳng đăng ký base table.
- ⏳ **CAT-CFG-2 — bạn cấu hình tay trong ConfigStudio** (thứ tự phụ thuộc: Quốc gia → Tỉnh → Phường/Xã):
  1. Chạy `db/052` (Data DB).
  2. Mỗi danh mục: Sys_Table (đăng ký base table; với Tỉnh/Phường dùng VIEW để lưới hiện tên cha) → tự sinh cột.
  3. Ui_Form Popup trên base table (DM_*/TC_Cap*); Tỉnh: lookup QuocGia_Id→DM_QuocGia; Phường/Xã: lookup
     TinhThanhPho_Id→DM_TinhThanhPho (cascade từ Tỉnh); i18n nhãn; bỏ audit/Id.
  4. Ui_View Grid **View_Code=`Grid_{Bang}`** (khớp Route AppNav: Grid_DM_QuocGia, Grid_DM_TinhThanhPho,
     Grid_DM_PhuongXa, Grid_DM_DonViTinh, Grid_DM_NganHang, Grid_TC_CapCongTy, Grid_TC_CapPhongBan); nguồn =
     base table (phẳng) hoặc view (Tỉnh/Phường); cột hiển thị Ma/Ten/… + tên cha; Edit_Form = form bước 3.
  5. Chạy config-sync → mở Danh mục › từng màn.
- → Sau khi 7 danh mục chạy = đủ lookup cho màn Công ty (ORG-CFG-3) và các màn nghiệp vụ tham chiếu khác.

### [2026-06-15] ORG-CFG-1/2/4 (engine-hóa màn Công ty — phần CODE) — claude

- Status: code done (build WPF + FE **0/0**); ⏳ chạy `db/051` + **ORG-CFG-3 cấu hình tay** + sync.
- Files:
  - WPF: `Infrastructure/SchemaInspectorService.cs` — `GetTableNamesAsync` lấy cả VIEW (`TABLE_TYPE IN ('BASE TABLE','VIEW')`).
  - DB: `db/051_create_vw_tc_congty.sql` — view `vw_TC_CongTy` (JOIN cấp/phường-xã/tỉnh/ngân hàng/cha; FK id + tên; IsDeleted=0).
  - FE: `Navigation/AppNav.cs` (NavScreen +Route; company Route="/view/Tree_TC_CongTy"), `Layout/NavMenu.razor`
    (fallback dùng Route), `Pages/ScreenView.razor` (redirect khi màn có Route).
- ⏳ **ORG-CFG-3 — bạn làm tay trong ConfigStudio WPF** (no-code, ADR-024):
  1. Chạy `db/051` (Data DB) + `db/050` (Config DB) trước.
  2. Sys_Table: đăng ký `dbo.vw_TC_CongTy` (giờ inspector liệt kê được) + `dbo.TC_CongTy` → tự sinh cột.
  3. Ui_Form (FormEditor): form trên `TC_CongTy`, Display_Mode=Popup, "Tự sinh trường" → set lookup
     (CapCongTy_Id→TC_CapCongTy, PhuongXa_Id→DM_PhuongXa, NganHang_Id→DM_NganHang, CongTy_Cha_Id→TC_CongTy),
     i18n nhãn; bỏ cột audit/Id/Logo.
  4. Ui_View (ViewManager): View_Type=TreeList, **View_Code=`Tree_TC_CongTy`** (PHẢI khớp Route ở AppNav),
     nguồn=vw_TC_CongTy, Key=Id, Parent=CongTy_Cha_Id, cột hiển thị Ma/Ten/TenCapCongTy/TenTinhThanhPho/…,
     Edit_Form=form bước 3.
  5. (Server-driven menu) set `HT_ChucNang` node organization.company `DuongDan=/view/Tree_TC_CongTy`
     + DoiTuong=Tree_TC_CongTy/LoaiDoiTuong=View. (Fallback AppNav đã có Route sẵn.)
  6. Chạy config-sync (màn Quản trị › Đồng bộ cấu hình) → mở Tổ chức › Công ty.
- Lưu ý backend: GetViewData SELECT theo Sys_Table.Schema_Name+Table_Code → đăng ký vw_TC_CongTy như 1 Sys_Table là chạy.

### [2026-06-15] CFGSYNC-3-UI (màn web đồng bộ cấu hình) — claude

- Status: done (build FE `ICare247_UI` **0 error**); ⏳ chưa E2E (cần backend + db/050 + login admin; trang sau cổng login).
- Files (frontend ICare247_UI):
  - `Models/ConfigSyncModels.cs` (ConfigSyncResultVm/ConfigSyncTableVm — totals tính FE từ Tables).
  - `Services/ConfigSyncApiService.cs` (PreviewAsync/ApplyAsync, trích ProblemDetails khi lỗi — không nuốt).
  - `Pages/Admin/ConfigSyncPage.razor` + `.css` (route `/m/administration/config-sync`).
  - `Program.cs` DI `AddScoped<ConfigSyncApiService>`; `Navigation/AppNav.cs` thêm screen `config-sync`.
- Cần biết:
  - Theo skill `icare247-admin-ui`: toolbar mỏng, surface phẳng, **1 CTA primary** ("Áp dụng từ master") + secondary
    ("Xem trước" dry-run); **bước xác nhận** trước khi áp thật (ghi DB); badge text-màu; số canh phải tabular-nums.
  - i18n đầy đủ scope `admin.cfgsync.*` qua `Loc.L(key, "fallback vi")` — en.json để rỗng (overlay, dịch sau).
  - Gọi `POST /api/v1/admin/config-sync(/preview)`; SUPERADMIN truy cập được (server bypass). Lỗi thiếu db/050 →
    backend trả 500 ProblemDetails → hiện ở dải `.cfg-error`.
- Bước tiếp: E2E (chạy db/050 → login admin → vào menu Quản trị › Đồng bộ cấu hình → Xem trước → Áp dụng).

### [2026-06-15] CFGSYNC-3 (action super admin đồng bộ) — claude

- Status: done (build BE `ICare247.slnx` **0/0**); ⏳ chưa E2E (cần chạy db/050 + đăng nhập SUPERADMIN gọi endpoint).
- Files (backend):
  - Application: `Features/Admin/ConfigSync/SyncConfigCommand.cs` (command + handler).
  - Api: `Controllers/AdminConfigSyncController.cs`.
- Cần biết:
  - Endpoint: `POST /api/v1/admin/config-sync` (áp thật, gate `[RequirePermission("administration.config-sync", Sua)]`)
    + `POST /api/v1/admin/config-sync/preview` (dry-run, `Xem`). Cả 2 `[Authorize]`; **SUPERADMIN tự bypass** kiểm quyền.
  - Handler gọi `IConfigSyncService.SyncAsync` → nếu áp thật & Success thì `INavigationCache.InvalidateTenant`.
    TriggeredBy = unique_name/Name/sub (cho log sync). Trả `ConfigSyncResult` (số dòng I/U/deactivate/skip theo bảng).
  - **CÒN THIẾU**: (1) hook provisioning full-sync khi tạo tenant mới; (2) invalidate **ConfigCache** toàn tenant
    (version-stamp CC-4 chưa có — hiện chỉ xóa cache menu); (3) seed node `administration.config-sync` vào
    `HT_ChucNang` nếu muốn cấp quyền cho role ngoài SUPERADMIN (super admin chạy được ngay).
- Bước tiếp: E2E (chạy db/050 → đăng nhập admin → POST /preview rồi POST áp thật → kiểm `Sys_Config_Sync_Log`);
  mở rộng descriptor các bảng còn lại; UI nút "Cập nhật cấu hình từ master" ở màn admin.

### [2026-06-15] CFGSYNC-2 (engine đồng bộ config) — claude

- Status: done (vertical slice — build BE `ICare247.slnx` **0 error**); ⏳ chưa verify chạy thật (cần db/050 + trigger CFGSYNC-3).
- Files (backend, không phải vùng Codex):
  - Application: `ConfigSync/ConfigSyncOptions.cs`, `ConfigSync/ConfigSyncResult.cs`, `Interfaces/IConfigSyncService.cs`.
  - Infrastructure: `ConfigSync/ConfigTableDescriptor.cs`, `ConfigSync/ConfigSyncTables.cs`, `ConfigSync/ConfigSyncService.cs`;
    DI `services.AddScoped<IConfigSyncService, ConfigSyncService>()`.
- Cần biết:
  - **Engine descriptor-driven**: 1 routine UPSERT generic + danh sách mô tả bảng (`ConfigSyncTables.Order`).
    Mỗi descriptor: tên bảng, IdColumn, LocalKeyColumn (mã), ContextParent (cha tạo ngữ cảnh khóa),
    RelinkParents (FK cần dịch sang Id tenant), ActiveColumn (tombstone), VersionColumn (→Source_Ver).
  - **Khóa nghiệp vụ** = [khóa cha đã re-link] + mã con (ngăn cách bằng control char U+0001). Map 2 chiều
    Code↔Id (master + tenant) trong `TableSyncState` → bảng con re-link FK qua mã, KHÔNG bê Id identity.
  - **Master = `ConnectionStrings:Config`** (dev: trùng tenant → sync vô hại). Khi tách 1 DB/tenant: đổi nguồn
    master trong ctor `ConfigSyncService` (vd thêm key `ConfigMaster`).
  - **Tenant đích = `IDbConnectionFactory`** (scoped, tenant-aware). Ghi trong 1 transaction; dry-run chỉ đọc.
  - Đọc cột thực tế qua INFORMATION_SCHEMA (không SELECT *); tập ghi = giao cột master∩tenant. Bắt buộc đủ
    4 cờ db/050 (thiếu → lỗi thân thiện). Đọc dòng dạng `IDictionary<string,object>` (Dapper dynamic).
  - **SLICE 5 bảng**: `Sys_Table→Sys_Column→Ui_Form→Ui_Section→Ui_Field`. Ui_Field không có Is_Active
    (dùng Is_Visible) → KHÔNG tombstone; Field_Code nullable → dòng thiếu mã bị Skip.
- Bước tiếp theo: **CFGSYNC-3** — action super admin "Cập nhật cấu hình từ master" (`[RequirePermission]`/SUPERADMIN,
  CQRS command + endpoint, gọi `SyncAsync`) + invalidate ConfigCache; rồi mở rộng các bảng còn lại vào `ConfigSyncTables`.

### [2026-06-15] CFGSYNC-1 (cờ đồng bộ config) — claude (làm thay codex ở `db/`) → codex (FYI)

- Status: done (script viết xong); ⏳ migration CẦN CHẠY trên **ICare247_Config** (master + mỗi tenant).
- Files (db, vùng Codex — Claude tạo thay):
  - `db/050_alter_config_sync_flags.sql` — thêm 4 cột cờ vào **11 bảng config** + tạo bảng log sync.
- Cần biết: nền tảng **F1 — đồng bộ config master→tenant** (spec `docs/spec/16_CONFIG_SYNC_SPEC.md`, ADR-024/025).
  - 5 quyết định mở §10 **đã duyệt 2026-06-15** (toàn bộ theo khuyến nghị): master=Config DB canonical ·
    bảo vệ **row-level** · giữ bản tenant khi xung đột · trigger provisioning+nút thủ công · xóa=`Is_Active=0`.
  - 4 cột mới (tên **tiếng Anh** cho nhất quán config DB, KHÁC `LaHeThong/DaTuyBien` của db/042 Data DB):
    `Is_System` (1=từ master) · `Is_Customized` (1=tenant đã sửa → sync bỏ qua) · `Synced_At` · `Source_Ver`.
  - 11 bảng (theo spec §7): `Sys_Table`, `Sys_Resource`, `Sys_Lookup`, `Ui_Form`, `Ui_Tab`, `Ui_Section`,
    `Ui_Field`, `Ui_View`, `Ui_View_Column`, `Ui_View_Action`, `Val_Rule`. Bảng con sát (Sys_Column,
    Ui_Field_Lookup, lookup items) **đi theo cha** — bảo vệ ở mức cha. Tombstone dùng `Is_Active` sẵn có.
  - Bảng `Sys_Config_Sync_Log` (audit + dry-run): I/U/deactivate/skip · version · status · Detail_Json.
  - Idempotent (COL_LENGTH/OBJECT_ID guard) — chạy lại an toàn.
- Bước tiếp theo: chạy `db/050` trên Config DB → code **CFGSYNC-2** (`IConfigSyncService` UPSERT theo mã, §3).

### [2026-06-13] AUDIT-1 (NK_ nhật ký hoạt động) — claude (làm thay codex ở `db/`) → codex (FYI)

- Status: done (code backend); ⏳ migration CẦN CHẠY trên Data DB tenant (vd ICare247_Solution).
- Files (db, vùng Codex — Claude tạo thay):
  - `db/040_create_nk_audit.sql` — bảng `dbo.NK_NhatKyHoatDong` (append-only, không FK, 3 index).
- Cần biết: cơ chế audit **non-blocking** — request chỉ enqueue (`IAuditWriter`→`IAuditQueue` bounded,
  drop khi đầy); `AuditBackgroundService` (Infrastructure) tiêu thụ: có Redis → Redis Stream
  `ic247:audit` (consumer group) → SqlBulkCopy NK_; không Redis → ghi thẳng DB. Event chỉ mang
  `TenantId` (KHÔNG mang connstring — resolve qua `ITenantConnectionResolver` lúc ghi).
  Đã gắn enqueue: Auth (login/logout/refresh), MasterData (create/update/delete).
- Bước tiếp theo: chạy `db/040` trên Data DB; (tuỳ chọn) cài Redis theo `docs/backend-debug/redis-setup.md`.

### [2026-06-12] REL-1 (Sys_Relation) — claude (làm thay codex) → codex (FYI)

- Status: done (Claude đã làm thay ở vùng Codex: `db/` + ConfigStudio WPF) — build WPF 0/0.
- Files đã tạo/sửa:
  - `db/035_extend_sys_relation.sql` — **migration idempotent** mở rộng `Sys_Relation`: thêm
    `Relation_Code` (unique), `Master_Key_Column` (default 'Id'), `Detail_FK_Column`, `On_Delete`
    (CHECK Restrict/Cascade/SetNull/NoAction) + index `IX_Sys_Relation_Master`. **⏳ CẦN CHẠY trên DB.**
  - WPF Modules.Forms: màn "Quản lý quan hệ" — `RelationManagerView.xaml(.cs)` + `RelationManagerViewModel`
    (Core: `RelationRecord`, `IRelationDataService`; Infra: `RelationDataService` Dapper Config DB).
  - Wire: `ViewNames.RelationManager`, `FormsModule` RegisterForNavigation, `App.xaml.cs` DI, `ShellViewModel`
    nav "Quan hệ (Relation)" dưới nhóm Forms.
  - `docs/spec/02_DATABASE_SCHEMA.md` — cập nhật Sys_Relation (cột mới + ghi chú soft-check).
- Cần biết:
  - **Mục đích**: registry quan hệ tường minh phục vụ (1) **soft-check FK khi xóa** + (2) **Master-Detail 1:N**.
    Giữ tách với `Ui_Field_Lookup` (lookup N:1 vẫn ở đó). Quyết định: PK Data DB = `Id` đồng nhất → soft-check
    KHÔNG đoán theo tên, đọc `Detail_FK_Column` từ Sys_Relation (xử lý đúng nhiều FK cùng nguồn).
  - **REL-2 ĐÃ XONG** (xem dưới): `ReferenceCheckService` giờ ưu tiên Sys_Relation, fallback name-match.
- Bước tiếp theo: (đã chạy migration 035 + REL-2). Khi vào Data DB tiếng Việt + PK `Id`: khai quan hệ
  trong màn "Quan hệ (Relation)" để soft-check chạy đường tường minh.

### [2026-06-12] REL-2 (soft-check đọc Sys_Relation) — claude

- Status: done — build backend `ICare247.slnx` **0/0**. Migration 035 user đã chạy.
- Files: `src/backend/src/ICare247.Infrastructure/Repositories/ReferenceCheckService.cs` (viết lại).
- Cần biết:
  - **Hybrid** (user chưa chốt rõ → Claude chọn an toàn): ① đọc `Sys_Relation` theo `Master_Table_Id`
    (active, có `Detail_FK_Column`) → candidate chính xác (xử lý đúng nhiều FK cùng nguồn). ② Bảng CHƯA
    khai quan hệ nào → **fallback** dò theo quy ước tên PK như cũ (không mất chặn xóa giai đoạn chuyển tiếp).
  - Query Sys_Relation bọc try/catch: DB chưa migrate 035 → trả rỗng → tự fallback (không nổ).
  - Logic đếm usage (Data DB) + validate identifier + IsLegacy giữ nguyên, tách thành helper `CountUsagesAsync`.
  - Nếu sau muốn **bỏ fallback** (chỉ Sys_Relation, hướng D thuần) → xóa nhánh ② trong `CheckUsageAsync`.

### [2026-06-07] VIEW-0 (Ui_View) — claude → codex

- Status: in_progress (thiết kế chốt; chờ Codex làm DB + ConfigStudio)
- Files (Codex sẽ tạo/sửa):
  - `db/0xx_create_ui_view.sql` — migration tạo 3 bảng + seed view mặc định từ `Ui_Form`
  - ConfigStudio WPF: module/màn "Quản lý View" (header + grid cột + actions) — đặt theo cấu trúc module hiện có
  - `docs/ICare247 Config Studio/TASKS_WPF.md` — thêm task VIEW-0
- Cần biết:
  - **Thiết kế đã chốt** — đọc `docs/spec/14_VIEW_CONFIG_SPEC.md` (DDL đầy đủ 3 bảng) + **ADR-015** trong
    `.claude/memory/architecture_decisions.md` + i18n convention `docs/spec/10_RESOURCE_KEY_CONVENTION.md` §1d.
  - 3 bảng: `Ui_View` (header + datasource + hành vi + export/print + TreeList), `Ui_View_Column`
    (cột + render/export/format), `Ui_View_Action` (nút toolbar/row).
  - **DDL bám convention hiện có**: IDENTITY, `Tenant_Id` FK nullable, `Version`, `Is_Active`,
    `Created_At/Updated_At`, unique index global/tenant (xem mẫu `Sys_Table` trong `db/000_create_schema.sql`).
  - **Migration tương thích**: auto-sinh 1 Grid view mặc định cho mỗi `Ui_Form` đang có (lấy field
    `Show_In_List`, `Edit_Form_Id` = chính form đó) → màn `/master/*` cũ không vỡ.
  - **Mọi text là `_Key`** (i18n, scope `table_code`, category `view`); ConfigStudio cần nút "+ Tạo key"
    + auto-seed vi+en như field/section/tab.
  - Theo ADR-007: ConfigStudio đọc/ghi **trực tiếp DB qua Dapper**, không gọi API.
  - Phần Domain/Application/Infrastructure/Api/Blazor (`IViewRepository`, `ViewController`, component
    `DataView` chọn DxGrid/DxTreeList) **Claude sẽ làm sau** khi bảng DB sẵn sàng — Codex KHÔNG đụng backend.
  - Lưu ý code đã có: `MasterDataGridConfig`/`MasterDataColumnDto` (Blazor RuntimeCheck) là runtime model
    sẽ map vào `Ui_View*` — Claude lo việc nối, Codex không cần quan tâm.
- Bước tiếp theo (Codex): viết migration `db/0xx_create_ui_view.sql` (3 bảng + seed view mặc định), chạy thử
  trên DB, rồi báo lại qua handoff để Claude wire backend (`GetViewAsync` + `ViewController` + `DataView`).

---

### [2026-05-31] WPF-10/WPF-13 - codex -> claude

- Status: done
- Files: `AI_TASKS.yaml`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Core/Interfaces/IRuleDataService.cs`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI/Infrastructure/RuleDataService.cs`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Modules.Rules/ViewModels/ValidationRuleEditorViewModel.cs`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Modules.Rules/Views/ValidationRuleEditorView.xaml`
- Can biet: WPF-13 marked done. WPF-10 now matches AI_TASKS acceptance: Compare field dropdown loads field codes from `IRuleDataService.GetFieldCodesInFormAsync(formId)` and the UI no longer allows free-text field entry.
- Buoc tiep theo: Reconcile old AI_TASKS WPF-11/WPF-12 status against TASKS_WPF done log before choosing the next WPF item.

---

### [2026-05-31] WPF-UI-PHASE4 - codex -> claude

- Status: done
- Files: `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Modules.Forms/ViewModels/FieldConfigViewModel.cs`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Modules.Events/ViewModels/EventEditorViewModel.cs`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Modules.Rules/ViewModels/ValidationRuleEditorViewModel.cs`, `docs/ICare247 Config Studio/TASKS_WPF.md`
- Can biet: Phase 4 verification passed. Navigation registrations match all shell/sidebar/requested routes. Startup smoke launched the WPF app for 6s without crash. Polished Rule/Event navigation so `formId`, `fieldCode`, `tableCode`, and `sectionName` are preserved when returning to FieldConfig.
- Buoc tiep theo: Manual visual pass on a real desktop session if pixel-level layout review is required.

---

### [2026-05-31] WPF-UI-PHASE3 - codex -> claude

- Status: done
- Files: `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI/MainWindow.xaml`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI/Themes/Shell.xaml`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI/Themes/Shell.SlateProfessional.xaml`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI/Themes/Controls.xaml`, workflow XAML views, `docs/ICare247 Config Studio/TASKS_WPF.md`
- Can biet: Phase 3 UI consistency done for WPF shell/theme/workflow screens. Shell uses Segoe UI and solid admin-tool surfaces; shared command bar, dirty indicator, state/error banner, and DevExpress grid density styles are in `Controls.xaml`.
- Buoc tiep theo: Phase 4 visual smoke/navigation smoke when app can be opened interactively.

---

### [2026-04-25] GOV-001 — claude → both

- Status: done
- Files: `BRAIN.md`, `CLAUDE.md`, `AGENTS.md`, `.codex/memory/*`, `MACHINE_SWITCH.md`, `AI_TASKS.yaml`, `AI_DECISIONS.md`, `AI_HANDOFF.md`
- Cần biết: Toàn bộ AI config đã được rebuild. BRAIN.md là single source of truth. Codex giờ có `.codex/memory/` riêng. Đọc MACHINE_SWITCH.md trước khi đổi máy.
- Bước tiếp theo: Codex bắt đầu WPF-10 (Compare rule dropdown) hoặc WPF-13 (pass tableCode).

---

### [2026-04-17] Wave 10 — claude → codex

- Status: done
- Files: `FieldLookupConfig.cs`, `MetadataEngine.cs`, `I18nManagerViewModel.cs`, `FieldConfigViewModel.cs`, `MainWindow.xaml.cs`, `LookupBoxPropsPanel.xaml`
- Cần biết: i18n captionKey hoàn chỉnh. MetadataEngine resolve popup column captions từ Sys_Resource. WPF: SpinEdit race condition fix, SysLookupManager fix, MainWindow fullscreen fix, popup columns UX (▲▼ + ✕).
- Bước tiếp theo: Test LookupBox end-to-end với GioiTinh + PhongBanID.

---

### [2026-03-03] GOV-001 — codex → claude

- Status: done
- Files: `AI_PROJECT_BRIEF.md` (đã xóa), `AI_TASKS.yaml`, `AI_HANDOFF.md`, `AI_DECISIONS.md`
- Cần biết: Governance files ban đầu được tạo.
- Bước tiếp theo: Confirm owners cho CORE-001, APP-001, INF-001, API-001.
