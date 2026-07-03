# AI Onboarding Gaps — ICare247 Core

> Mục đích: tổng hợp các đánh giá read-only của Codex về mức độ sẵn sàng bàn giao dự án.
> Tài liệu này dành cho cả Codex và Claude Code khi cần hiểu nhanh những phần còn thiếu,
> rủi ro bàn giao, và các tài liệu cần bổ sung để một lập trình viên mới có thể làm việc độc lập.
>
> Phạm vi đánh giá: chỉ dựa trên nội dung đọc được trong repository. Không suy đoán từ hệ thống ngoài repo.

---

## 1. Kết luận điều hành

Repo hiện có nền tảng tài liệu kỹ thuật khá tốt cho phần **architecture, coding rules, metadata engine,
API debug theo một số luồng chính**. Một lập trình viên mới có thể đọc để hiểu khung kỹ thuật tổng thể.

Tuy nhiên, người mới **chưa thể vận hành và xử lý độc lập end-to-end** nếu không có thêm thông tin ngoài repo,
đặc biệt ở các mảng:

1. Trạng thái database thật và migration đã chạy.
2. Quy trình provision tenant / deploy / rollback.
3. Dữ liệu mẫu và tài khoản test chính thức.
4. Đặc tả nghiệp vụ ERP chi tiết.
5. Mapping đầy đủ từ màn hình → API → handler → repository → bảng.
6. Kiến thức ngầm về config-sync, menu/permission, và ConfigStudio.

Mức sẵn sàng hiện tại theo đánh giá:

| Hạng mục | Điểm |
|---|---:|
| Hiểu nghiệp vụ | 7/10 |
| Hiểu kiến trúc | 8.5/10 |
| Hiểu database | 7/10 |
| Hiểu coding convention | 8.5/10 |
| Có thể tự xử lý bug phạm vi rõ | 7.5/10 |
| Có thể tự xử lý task mới lớn | 6.5/10 |

---

## 2. Những gì đã xác minh được từ repo

### 2.1 Mục đích hệ thống

ICare247 Core Platform là **enterprise metadata-driven low-code form engine**. Form, field, validation rule,
event/action, view/list được định nghĩa bằng metadata trong database, nhằm giảm nhu cầu deploy lại khi thay đổi
logic nghiệp vụ.

Nguồn chính:

- `README.md`
- `BRAIN.md`
- `docs/spec/00_PROJECT_OVERVIEW.md`
- `docs/spec/22_ERP_DIRECTION.md`

Theo định hướng ERP mới, lõi engine vẫn giữ metadata-driven, nhưng sản phẩm hướng tới ERP nội bộ nông nghiệp /
thương mại. Các danh mục và form đơn giản dùng engine; nghiệp vụ phức tạp có thể viết tay bằng Blazor + CQRS.

### 2.2 Kiến trúc tổng thể

Kiến trúc backend là Clean Architecture 4 tầng:

```text
Domain
Application
Infrastructure
Api
```

Quy tắc chính:

- `Domain`: entity, AST node, engine interface, không phụ thuộc tầng khác.
- `Application`: CQRS/MediatR, interface repository/service, import Domain.
- `Infrastructure`: Dapper, Redis, cache, auth, repository implementation.
- `Api`: controller, middleware, composition root; controller không `new` Infrastructure class.

Nguồn chính:

- `docs/spec/01_ARCHITECTURE.md`
- `.claude-rules/architecture.md`
- `BRAIN.md`

### 2.3 Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Backend | .NET 9 / ASP.NET Core 9 |
| Runtime frontend | Blazor WebAssembly + DevExpress Blazor |
| Admin desktop tool | WPF + DevExpress WPF + Prism 9 |
| Database | Microsoft SQL Server |
| Data access | Dapper only |
| Cache | MemoryCache L1 + Redis L2 |
| Logging | Serilog + OpenTelemetry |
| Auth | JWT + policy/permission-based authorization |
| CQRS | MediatR |

Nguồn chính:

- `BRAIN.md`
- `README.md`
- `src/backend/src/ICare247.Api/ICare247.Api.csproj`
- `src/frontend/ICare247_UI/ICare247_UI.csproj`
- `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI/ConfigStudio.WPF.UI.csproj`

### 2.4 Solution và startup

Solution chính:

- Backend: `src/backend/ICare247.slnx`
- Web UI mới: `src/frontend/ICare247_UI.slnx`
- ConfigStudio WPF: `src/frontend/ConfigStudio.WPF.UI/ConfigStudio.WPF.UI.slnx`

Startup chính:

- API: `src/backend/src/ICare247.Api/Program.cs`
- Web UI: `src/frontend/ICare247_UI/ICare247_UI.csproj`
- WPF ConfigStudio: `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI/ConfigStudio.WPF.UI.csproj`

Run scripts:

- `run-api.bat`
- `run-ui.bat`
- `run-wpf.bat`
- `run-blazor.bat`
- `run-all.bat`

### 2.5 Database chính

Các nguồn DB đã xác minh:

- Config DB canonical schema: `docs/migrations/000_create_schema.sql`
- Config DB canonical seed: `docs/migrations/001_seed_all.sql`
- Data DB foundation: `db/037_create_data_db_foundation.sql`
- Migration/scripts rời: `db/000_create_schema.sql` đến `db/056_create_ht_nguoidung_luoilayout.sql`

Theo memory/docs:

- Config DB thường được gọi là `ICare247_Config`.
- Data DB/LiveData ví dụ là `ICare247_Solution`.
- Redis là tùy chọn; nếu không có Redis, audit/cache có fallback nhất định.

Nguồn:

- `.codex/memory/current_phase.md`
- `docs/backend-debug/README.md`
- `docs/backend-debug/redis-setup.md`

---

## 3. Module chính đã xác định

### Backend/API

| Module | Controller / điểm vào | Repository / service chính |
|---|---|---|
| Auth | `AuthController` | `AuthRepository`, `RefreshTokenRepository`, `JwtTokenService` |
| Forms metadata | `FormController` | `FormRepository`, `FieldRepository`, `ConfigCache` |
| Runtime form | `RuntimeController` | `MetadataEngine`, `ValidationEngine`, `EventEngine` |
| Master Data CRUD | `MasterDataController` | `MasterDataRepository`, `ReferenceCheckService` |
| Views/Grid/TreeList | `ViewController` | `ViewRepository`, `UserGridLayoutRepository` |
| Lookups | `LookupController` | `LookupRepository`, `DynamicLookupRepository` |
| Navigation/Menu | `MeController`, `MenuAdminController` | `NavigationRepository`, `MenuAdminRepository` |
| Permissions | `AdminPermissionController` | `PermissionAdminRepository`, `PermissionService` |
| Config Sync | `AdminConfigSyncController` | `ConfigSyncService` |
| Cache admin | `CacheAdminController` | `IConfigCache`, cache service |

### Frontend/UI

| Module | File/vùng chính |
|---|---|
| Auth screens | `src/frontend/ICare247_UI/Pages/Auth/*` |
| Dashboard | `src/frontend/ICare247_UI/Pages/Dashboard.razor` |
| Runtime form | `src/frontend/ICare247_UI/Pages/FormRunner.razor` |
| Master Data | `Pages/MasterData/*`, `Components/MasterData/*` |
| View/Grid | `Pages/View/ViewPage.razor`, `Components/View/DataView.razor` |
| Admin menu | `Pages/Admin/MenuBuilderPage.razor` |
| Permission matrix | `Pages/Admin/PermissionMatrixPage.razor` |
| Config sync page | `Pages/Admin/ConfigSyncPage.razor` |
| i18n tools | `Pages/Dev/I18nToolsPage.razor` |

### ConfigStudio WPF

Các module WPF đã thấy:

- `ConfigStudio.WPF.UI.Modules.Forms`
- `ConfigStudio.WPF.UI.Modules.Rules`
- `ConfigStudio.WPF.UI.Modules.Events`
- `ConfigStudio.WPF.UI.Modules.Grammar`
- `ConfigStudio.WPF.UI.Modules.I18n`
- `ConfigStudio.WPF.UI.Core`

---

## 4. Bảng quan trọng nhất

### Config / metadata DB

1. `Sys_Table`
2. `Sys_Column`
3. `Sys_Resource`
4. `Sys_Language`
5. `Sys_Lookup`
6. `Sys_Relation`
7. `Sys_Dependency`
8. `Sys_Config`
9. `Sys_Config_Sync_Log`
10. `Ui_Form`
11. `Ui_Tab`
12. `Ui_Section`
13. `Ui_Field`
14. `Ui_Field_Lookup`
15. `Ui_View`
16. `Ui_View_Column`
17. `Ui_View_Action`
18. `Ui_View_Filter`
19. `Val_Rule`
20. `Evt_Definition`
21. `Evt_Action`
22. `Gram_Function`
23. `Gram_Operator`

### Data DB / ERP foundation

1. `HT_NguoiDung`
2. `HT_RefreshToken`
3. `HT_VaiTro`
4. `HT_NguoiDung_VaiTro`
5. `HT_ChucNang`
6. `HT_VaiTro_Quyen`
7. `HT_NguoiDung_CongTy`
8. `HT_PhanHe`
9. `HT_NguoiDung_LuoiLayout`
10. `DM_QuocGia`
11. `DM_TinhThanhPho`
12. `DM_PhuongXa`
13. `DM_DonViTinh`
14. `DM_NganHang`
15. `TC_CapCongTy`
16. `TC_CapPhongBan`
17. `TC_CongTy`
18. `TC_PhongBan`
19. `NK_NhatKyHoatDong`

Nguồn:

- `docs/spec/02_DATABASE_SCHEMA.md`
- `docs/spec/11_DATA_DB_SCHEMA.md`
- `docs/spec/14_VIEW_CONFIG_SPEC.md`
- `db/037_create_data_db_foundation.sql`
- `db/040_create_nk_audit.sql`
- `db/055_create_ht_phanhe.sql`
- `db/056_create_ht_nguoidung_luoilayout.sql`

---

## 5. Stored procedure

Kết quả quét repo: **KHÔNG TÌM THẤY stored procedure được định nghĩa trong repo**.

Đã quét các pattern:

- `CREATE PROC`
- `CREATE PROCEDURE`
- `ALTER PROCEDURE`

Chỉ thấy `EXEC sys.sp_executesql` trong `db/050_alter_config_sync_flags.sql`.

Lưu ý: `Ui_View.Source_Type = 'Sp'` và `ViewRepository.GetFilteredDataAsync` có hỗ trợ gọi stored procedure
theo `Source_Object`, nhưng repo hiện không có SP mẫu. Đây là một lỗ hổng tài liệu/vận hành quan trọng nếu
team dự kiến dùng View nguồn SP.

---

## 6. Truy vết 3 chức năng mẫu

### 6.1 Đăng nhập

Luồng:

```text
Login.razor
  -> AuthService.LoginAsync
  -> POST /api/v1/auth/login
  -> AuthController.Login
  -> LoginCommandHandler
  -> AuthRepository / PasswordHasher / JwtTokenService / RefreshTokenRepository
  -> HT_NguoiDung / HT_NguoiDung_VaiTro / HT_VaiTro / HT_RefreshToken
```

Files:

- UI: `src/frontend/ICare247_UI/Pages/Auth/Login.razor`
- Frontend service: `src/frontend/ICare247.UI.Shared/Services/Auth/AuthService.cs`
- Controller: `src/backend/src/ICare247.Api/Controllers/AuthController.cs`
- Handler: `src/backend/src/ICare247.Application/Features/Auth/Login/*`
- SQL repository: `src/backend/src/ICare247.Infrastructure/Repositories/AuthRepository.cs`
- Refresh token repository: `src/backend/src/ICare247.Infrastructure/Repositories/RefreshTokenRepository.cs`

### 6.2 Master Data generic CRUD

Luồng:

```text
MasterDataListPage / MasterDataForm
  -> MasterDataApiService
  -> /api/v1/master-data/{formCode}
  -> MasterDataController
  -> GetMasterData* / SaveMasterData / DeleteMasterData handlers
  -> MasterDataRepository / ReferenceCheckService
  -> metadata: Ui_Form, Ui_Field, Sys_Table, Sys_Column
  -> data table: resolved from Sys_Table.Schema_Name + Sys_Table.Table_Code
```

Files:

- UI: `src/frontend/ICare247_UI/Pages/MasterData/MasterDataListPage.razor`
- Form component: `src/frontend/ICare247_UI/Components/MasterData/MasterDataForm.razor`
- Frontend service: `src/frontend/ICare247_UI/Services/MasterDataApiService.cs`
- Controller: `src/backend/src/ICare247.Api/Controllers/MasterDataController.cs`
- Repository: `src/backend/src/ICare247.Infrastructure/Repositories/MasterDataRepository.cs`
- Soft-check: `src/backend/src/ICare247.Infrastructure/Repositories/ReferenceCheckService.cs`

### 6.3 View/Grid/TreeList

Luồng:

```text
ViewPage
  -> ViewApiService
  -> /api/v1/views/{code}/info + /data or /search
  -> ViewController
  -> GetViewByCode / GetViewData / GetViewFilteredData handlers
  -> ViewRepository
  -> metadata: Ui_View, Ui_View_Column, Ui_View_Action, Ui_View_Filter, Sys_Table, Sys_Resource
  -> data: table/view/SP/SQL source from metadata
```

Files:

- UI page: `src/frontend/ICare247_UI/Pages/View/ViewPage.razor`
- Component: `src/frontend/ICare247_UI/Components/View/DataView.razor`
- Filter component: `src/frontend/ICare247_UI/Components/View/FilterPanel.razor`
- Frontend service: `src/frontend/ICare247_UI/Services/ViewApiService.cs`
- Controller: `src/backend/src/ICare247.Api/Controllers/ViewController.cs`
- Repository: `src/backend/src/ICare247.Infrastructure/Repositories/ViewRepository.cs`

---

## 7. Coding conventions và hard constraints

Các luật quan trọng nhất:

1. Dapper only; EF Core bị cấm.
2. Không string interpolation giá trị vào SQL; mọi value phải parameterized.
3. Không `SELECT *` theo convention, dù hiện source có một vài chỗ cần review riêng.
4. Không `eval`, không dynamic compile; expression chạy qua AST Grammar V1.
5. Không `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`.
6. DB/cache/external method dùng async + `CancellationToken`.
7. Cache key lấy từ `CacheKeys.cs`, không hardcode rải rác.
8. Query/cache có dữ liệu tenant phải tenant-aware.
9. Engine không swallow exception.
10. Comment code tiếng Việt.
11. File `.cs` cần header chuẩn.
12. WPF dùng Prism 9 + DevExpress WPF, không MaterialDesign.
13. XAML/CS/MD/YAML/SQL có tiếng Việt phải giữ UTF-8; ưu tiên `apply_patch`, tránh rewrite toàn file bằng PowerShell mặc định.

Nguồn:

- `BRAIN.md`
- `AGENTS.md`
- `.claude-rules/dapper-patterns.md`
- `.claude-rules/comment-rules.md`
- `.claude-rules/wpf-configstudio.md`
- `.codex/memory/coding_style_feedback.md`

---

## 8. Kiến thức ngầm đã tìm thấy trong docs/memory

### 8.1 Governance AI

- `BRAIN.md` là single source of truth.
- `AGENTS.md` và `CLAUDE.md` chủ yếu là protocol riêng theo agent.
- Codex owner chính: WPF ConfigStudio, tests, DB migrations.
- Claude owner chính: backend, Blazor, docs/spec.
- Nếu sửa vùng của agent kia cần handoff.

Nguồn:

- `BRAIN.md`
- `AGENTS.md`
- `CLAUDE.md`
- `AI_DECISIONS.md`
- `AI_HANDOFF.md`

### 8.2 Handoff mới nhất cần biết

Tại thời điểm đọc, `AI_HANDOFF.md` có các điểm nổi bật:

- `ViewPage` là lưới duy nhất cho end-user.
- Thêm/Sửa trên ViewPage mở popup inline, không nhảy sang `/master`.
- MasterDataListPage giữ lại cho mục đích nội bộ khác.
- Codex có thể cần đặt default `Ui_Form.Display_Mode = Popup` khi tạo form mới trong ConfigStudio.
- `Ui_Form.Form_Columns` điều khiển số cột popup; `Ui_Field.Show_In_List` điều khiển cột lưới.

### 8.3 i18n hai hệ

Web UI có hai hệ i18n:

1. Metadata-driven: text của form/field/view lưu `Sys_Resource`.
2. Hand-coded: text viết tay dùng JSON overlay và `LocalizationService.L(key, fallback)`.

Không dùng `Sys_Resource` cho chuỗi hand-coded.

Nguồn:

- `docs/HUONG_DAN_I18N.md`
- `docs/spec/10_RESOURCE_KEY_CONVENTION.md`

### 8.4 Auth forgot/reset

Forgot password và reset password hiện là stub/chưa tích hợp email/token reset thật.

Nguồn:

- `docs/backend-debug/auth-forgot-reset.md`

### 8.5 Redis

Redis không bắt buộc để chạy app. Nếu không có Redis, audit có fallback ghi DB trực tiếp và cache chỉ còn L1.

Nguồn:

- `docs/backend-debug/redis-setup.md`

---

## 9. Thiếu hụt theo mức độ rủi ro

### P0 — Rủi ro rất cao

#### 1. Không có trạng thái DB thật đã chạy migration nào

Repo có nhiều script trong `db/` và canonical schema trong `docs/migrations/`, nhưng không có migration ledger cho
từng DB thật.

Người mới không biết:

- Config DB đang ở script nào.
- Data DB đang ở script nào.
- Tenant nào đã chạy `037`, `042`, `045`, `050`, `055`, `056`.
- Script nào chỉ tham khảo, script nào bắt buộc chạy.

Tài liệu cần bổ sung:

- `docs/operations/DB_MIGRATION_LEDGER.md`
- Bảng: environment, DB name, script, applied date, applied by, checksum/result.

#### 2. Không có ERD/schema runtime cuối cùng

Spec có mô tả bảng, nhưng thiếu ERD hiện hành sau toàn bộ script.

Rủi ro:

- Sửa sai quan hệ `Sys_Table -> Ui_Form -> Ui_Field -> Data DB`.
- Nhầm Config DB với Data DB.
- Nhầm lookup static `Sys_Lookup` với dynamic `Ui_Field_Lookup`.

Tài liệu cần bổ sung:

- ERD Config DB.
- ERD Data DB foundation.
- Sơ đồ cross-DB ở mức application, ghi rõ không FK cross-DB.

#### 3. Không có runbook provision tenant end-to-end

Thiếu checklist:

```text
tạo tenant
  -> tạo Config DB
  -> tạo Data DB
  -> chạy migration nào
  -> seed admin/user/role
  -> sync config master
  -> cấp quyền/menu
  -> smoke test
```

Tài liệu cần bổ sung:

- `docs/operations/TENANT_PROVISIONING_RUNBOOK.md`

#### 4. Không có deploy production runbook

Thiếu hướng dẫn:

- Publish API.
- Publish Blazor WASM.
- Host IIS/Nginx/Windows service.
- HTTPS/certificate.
- CORS.
- DataProtection keyring.
- Redis.
- Log folder permission.
- DB backup/restore.
- Rollback release.

Tài liệu cần bổ sung:

- `docs/operations/DEPLOYMENT_RUNBOOK.md`
- `docs/operations/ROLLBACK_RUNBOOK.md`

#### 5. Không có cấu hình local/staging/prod mẫu đủ dùng

API phụ thuộc local config ngoài repo:

- `%APPDATA%\ICare247\Api\appsettings.local.json`

Thiếu template chuẩn cho:

- `ConnectionStrings:Config`
- `ConnectionStrings:LiveData`
- `ConnectionStrings:Redis`
- `Jwt`
- `Cors:AllowedOrigins`
- `DataProtection`
- `DebugLog`

Tài liệu/file cần bổ sung:

- `docs/operations/appsettings.local.sample.json`
- Không chứa secret thật.

#### 6. Không có golden dataset/account test chính thức

Thiếu:

- Tenant test.
- User/password test.
- SUPERADMIN role.
- Form mẫu.
- View mẫu.
- Lookup mẫu.
- Case cascade lookup mẫu.
- Case permission mẫu.

Tài liệu cần bổ sung:

- `docs/testing/LOCAL_SMOKE_DATA.md`
- Seed script hoặc hướng dẫn seed dữ liệu test.

### P1 — Rủi ro cao

#### 7. Nghiệp vụ ERP chưa được đặc tả đủ

Đã có hướng ERP, nhưng thiếu spec chi tiết cho các module:

- Nhân sự
- Công lương
- Thu mua cafe nhân
- Thu mua hồ tiêu
- Phân bón
- Kho
- Công nợ
- Báo cáo

Tài liệu cần bổ sung:

- `docs/business/HRM_SPEC.md`
- `docs/business/PAYROLL_SPEC.md`
- `docs/business/PURCHASE_COFFEE_SPEC.md`
- `docs/business/WAREHOUSE_SPEC.md`
- v.v.

#### 8. Chưa có ma trận quyết định engine-driven vs code tay

Hiện mới có nguyên tắc chung. Thiếu bảng phân loại:

| Loại màn | Engine no-code | Code tay |
|---|---|---|
| Danh mục phẳng | có | không |
| Lưới cây | có | tùy |
| Master-detail nhiều rule | tùy | thường có |
| Công lương | không rõ | có thể |

Tài liệu cần bổ sung:

- `docs/architecture/ENGINE_VS_BESPOKE_DECISION_MATRIX.md`

#### 9. Thiếu route/API/table trace map đầy đủ

Debug docs có Auth/MasterData/View tốt, nhưng chưa phủ hết:

- Menu builder.
- Permission matrix.
- Config sync.
- Cache tools.
- Lookup add-new.
- Runtime event/validation chi tiết.
- WPF ConfigStudio flows.

Tài liệu cần bổ sung:

- `docs/debug/TRACE_MAP.md`
- Bảng: route/page, API, controller, CQRS, repository, DB tables.

#### 10. Không có menu/permission state hiện hành

Thiếu tài liệu cho:

- Node nào đang seed từ `Sys_MenuCatalog`.
- Node nào nằm trong `HT_ChucNang`.
- Route nào fallback từ `AppNav`.
- Permission nào cần cấp để thấy từng menu.

Tài liệu cần bổ sung:

- `docs/operations/MENU_PERMISSION_MATRIX.md`

#### 11. Config-sync thiếu runbook vận hành

Spec đã có, code đã có, nhưng thiếu:

- Khi nào preview.
- Khi nào apply.
- Bảng nào đang sync trong code hiện tại.
- Cách xử lý conflict customized row.
- Cách rollback khi sync sai.
- Cách verify sau sync.

Tài liệu cần bổ sung:

- `docs/operations/CONFIG_SYNC_RUNBOOK.md`

#### 12. Thiếu rollback database

Các script idempotent không thay thế được rollback.

Tài liệu cần bổ sung:

- Backup trước migration.
- Cách restore Config DB/Data DB.
- Cách revert config sync.
- Cách kiểm tra metadata bị hỏng.

#### 13. Stored procedure strategy chưa rõ

View engine hỗ trợ `Source_Type = Sp`, nhưng repo không có stored procedure mẫu.

Tài liệu cần bổ sung:

- Naming convention SP.
- Input/output convention.
- Paging/filter convention.
- Security/permission.
- Ví dụ SP dùng với `Ui_View_Filter`.

#### 14. Thiếu backup/restore policy

Đặc biệt cần rõ:

- Config master DB.
- Config tenant DB.
- Data tenant DB.
- Audit DB/table.
- Redis persistence có cần backup không.

### P2 — Rủi ro trung bình cao

#### 15. Design system có mâu thuẫn tài liệu

`docs/design-system/README.md` còn mô tả brand colorful/playful, trong khi `docs/spec/22_ERP_DIRECTION.md`
chốt hướng ERP trung tính / Fluent Light.

Rủi ro: người mới build UI sai phong cách.

Tài liệu cần bổ sung/sửa:

- Đánh dấu design README cũ là deprecated hoặc cập nhật theo ERP.
- Một file `docs/design-system/CURRENT_UI_DIRECTION.md`.

#### 16. Thiếu checklist UI admin thống nhất

Có nhiều guideline, nhưng cần checklist ngắn:

- Toolbar.
- Grid density.
- Popup behavior.
- Permission button states.
- Loading/error/empty.
- i18n.
- Responsive.

#### 17. Debug docs chưa phủ toàn hệ thống

Đã tốt cho một số luồng. Cần bổ sung cho:

- Permission matrix.
- Menu builder.
- Config sync.
- Cache invalidation.
- Audit log.
- Lookup add-new.
- Cascade lookup.
- WPF ConfigStudio.

#### 18. Thiếu full local setup checklist

Run scripts có sẵn, nhưng thiếu quy trình:

```text
cài SDK
cài SQL Server
cài Redis tùy chọn
restore/create DB
chạy scripts
tạo appsettings.local
chạy API
chạy UI
login
smoke test 5 màn chính
```

Tài liệu cần bổ sung:

- `docs/onboarding/LOCAL_SETUP.md`

#### 19. Thiếu test strategy tổng thể

Thiếu:

- Test pyramid.
- Test DB setup.
- Integration tests dùng DB thật hay test container.
- E2E browser tests.
- CI command chuẩn.

Tài liệu cần bổ sung:

- `docs/testing/TEST_STRATEGY.md`

#### 20. Không có CI/CD documented

Thiếu:

- Build command chuẩn.
- Test command chuẩn.
- Artifact publish.
- Versioning.
- Release flow.
- Rollback release.

#### 21. Ownership theo người/team chưa rõ

Repo có ownership giữa AI agents, nhưng khi bàn giao cho người mới cần owner thực tế:

- Ai duyệt DB migration.
- Ai chạy production migration.
- Ai giữ secrets.
- Ai duyệt UI.
- Ai quyết định nghiệp vụ.

### P3 — Rủi ro trung bình

#### 22. Coding convention nằm rải rác

Nguồn rule hiện nằm ở:

- `BRAIN.md`
- `AGENTS.md`
- `CLAUDE.md`
- `.claude-rules/*`
- `.codex/memory/*`
- docs/spec

Cần một handbook ngắn cho dev người thật.

Tài liệu cần bổ sung:

- `docs/onboarding/DEVELOPER_HANDBOOK.md`

#### 23. ADR/index quyết định chưa gom một chỗ

`AI_DECISIONS.md` có một số quyết định, nhiều ADR khác được nhắc trong docs/memory Claude.

Cần:

- ADR index.
- Status accepted/superseded.
- Link đến file áp dụng.

#### 24. Thiếu runbook ConfigStudio kỹ thuật

Docs có user guide, nhưng người mới cần quy trình kỹ thuật chắc chắn:

```text
Sys_Table
  -> auto generate Sys_Column
  -> Ui_Form
  -> Ui_Field
  -> Ui_View
  -> i18n
  -> menu
  -> permission
  -> config-sync
  -> runtime verify
```

#### 25. i18n cần quy tắc cuối cùng dạng checklist

Hai hệ i18n dễ nhầm. Cần bảng rất rõ:

- Text metadata đi `Sys_Resource`.
- Text viết tay đi JSON overlay.
- Không đưa hand-coded string vào `Sys_Resource`.
- Khi nào chạy scanner.

#### 26. Logging/observability production chưa rõ

Thiếu:

- Log path theo môi trường.
- Retention.
- CorrelationId lookup.
- Alerting.
- Debug production incident.

#### 27. Performance constraints chưa rõ

Thiếu:

- SLA/API latency mục tiêu.
- Max rows/grid.
- Paging/virtual scroll policy.
- Cache TTL cuối cùng.
- Khi nào invalidate cache.

#### 28. Security checklist chưa đủ thành runbook

Cần checklist:

- JWT secret rotation.
- HTTPS.
- CORS whitelist.
- Password policy.
- Lockout.
- Refresh token rotation.
- Permission enforcement.
- Audit bắt buộc.

### P4 — Rủi ro thấp nhưng hao thời gian

#### 29. Tên thư mục cũ/mới chưa nhất quán

Một số docs nhắc `src/ICare247.ConfigStudio.WPF`, thực tế đang là:

- `src/frontend/ConfigStudio.WPF.UI`

#### 30. Một số docs có dấu hiệu lỗi thời

Ví dụ README trỏ một số docs theo path cũ `docs/00_PROJECT_OVERVIEW.md`, trong khi thực tế nằm ở:

- `docs/spec/00_PROJECT_OVERVIEW.md`

#### 31. Thiếu glossary

Cần định nghĩa nhanh:

- Config DB
- Data DB
- Master Config
- Tenant
- LiveData
- ConfigStudio
- FormRunner
- ViewPage
- MasterData
- Ui_Form
- Ui_View
- Sys_Table

#### 32. Thiếu danh sách "không chạy / không đụng"

Ví dụ `db/015_create_cf_data_schema.sql` được spec data DB ghi là giữ tham khảo, không chạy vào Data DB chuẩn mới.

Cần file:

- `docs/operations/DANGEROUS_OR_DEPRECATED_FILES.md`

#### 33. Thiếu onboarding máy mới

Cần checklist:

- .NET SDK version.
- SQL Server.
- Redis optional.
- DevExpress license/feed nếu cần.
- Local config.
- Restore DB.
- Run scripts.

---

## 10. Tài liệu đề xuất cần tạo thêm

Ưu tiên cao nhất:

1. `docs/onboarding/LOCAL_SETUP.md`
2. `docs/operations/DB_MIGRATION_LEDGER.md`
3. `docs/operations/TENANT_PROVISIONING_RUNBOOK.md`
4. `docs/operations/DEPLOYMENT_RUNBOOK.md`
5. `docs/operations/ROLLBACK_RUNBOOK.md`
6. `docs/operations/CONFIG_SYNC_RUNBOOK.md`
7. `docs/testing/LOCAL_SMOKE_DATA.md`
8. `docs/debug/TRACE_MAP.md`
9. `docs/architecture/ENGINE_VS_BESPOKE_DECISION_MATRIX.md`
10. `docs/onboarding/GLOSSARY.md`

Ưu tiên tiếp theo:

11. `docs/testing/TEST_STRATEGY.md`
12. `docs/operations/MENU_PERMISSION_MATRIX.md`
13. `docs/operations/BACKUP_RESTORE_POLICY.md`
14. `docs/operations/OBSERVABILITY_RUNBOOK.md`
15. `docs/security/PRODUCTION_SECURITY_CHECKLIST.md`
16. `docs/design-system/CURRENT_UI_DIRECTION.md`
17. `docs/database/STORED_PROCEDURE_CONVENTION.md`
18. `docs/onboarding/DEVELOPER_HANDBOOK.md`

---

## 11. Checklist cho người mới trước khi nhận task

Trước khi sửa bug hoặc làm task mới, người mới nên xác minh:

- [ ] Đang làm trên branch nào.
- [ ] DB local đang trỏ tới đâu.
- [ ] Config DB/Data DB đã chạy script nào.
- [ ] User test là gì, role gì.
- [ ] Màn đang debug là engine-driven hay code tay.
- [ ] Route gọi API nào.
- [ ] API đi qua controller/handler/repository nào.
- [ ] Bảng dữ liệu thật là bảng nào.
- [ ] Cache có thể đang stale không.
- [ ] Permission/menu có chặn UI/API không.
- [ ] Có cần config-sync sau khi sửa metadata không.
- [ ] File thuộc ownership Codex hay Claude theo governance.
- [ ] Nếu sửa XAML/CS/SQL có tiếng Việt, giữ UTF-8 và scan mojibake.

---

## 12. Nguồn đã đọc / dùng làm cơ sở

Các nguồn chính đã được dùng trong phân tích:

- `AGENTS.md`
- `BRAIN.md`
- `CLAUDE.md`
- `.cursorrules`
- `.claude-rules/*`
- `.codex/memory/*`
- `AI_DECISIONS.md`
- `AI_HANDOFF.md`
- `AI_TASKS.yaml`
- `MACHINE_SWITCH.md`
- `README.md`
- `docs/spec/*`
- `docs/backend-debug/*`
- `docs/design-system/*`
- `docs/huong-dan-wpf/*`
- `docs/COMMANDS_GUIDE.md`
- `docs/human/AI_WORKFLOW_GUIDE.md`
- `docs/HUONG_DAN_I18N.md`
- `docs/huong-dan-wpf/ConfigStudio_User_Guide.md`
- `db/*.sql`
- `src/backend/ICare247.slnx`
- `src/frontend/ICare247_UI.slnx`
- `src/frontend/ConfigStudio.WPF.UI/ConfigStudio.WPF.UI.slnx`
- `src/backend/src/ICare247.Api/Program.cs`
- `src/backend/src/ICare247.Api/Controllers/*`
- `src/backend/src/ICare247.Infrastructure/Repositories/*`
- `src/frontend/ICare247_UI/Pages/*`
- `src/frontend/ICare247_UI/Services/*`

---

## 13. Ghi chú cho Codex và Claude Code

- Khi nhận task mới, không chỉ đọc file source liên quan; cần kiểm `AI_HANDOFF.md` trước để tránh đạp vùng của agent kia.
- Nếu task liên quan DB/schema/config sync, cần xác minh DB thật hoặc yêu cầu người dùng cung cấp trạng thái migration.
- Nếu task liên quan UI admin/HRM, cần ưu tiên hướng ERP/Fluent Light mới; không dựa mù vào design-system README cũ nếu mâu thuẫn.
- Nếu task yêu cầu stored procedure, hiện repo chưa có SP mẫu; cần thống nhất convention trước khi viết.
- Nếu task là bug runtime metadata, luôn kiểm cả Config DB metadata, Data DB data, cache, permission, và route/menu.
