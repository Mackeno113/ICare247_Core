# Last Session Summary

> Cập nhật: 2026-06-13 (session 47 — Data DB nền tảng: chốt tiền tố + spec HT_/DM_/TC_ + migration 037/038/039)

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
