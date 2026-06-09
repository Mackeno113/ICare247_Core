# Last Session Summary

> Cập nhật: 2026-06-09 (session 43 — WPF VIEW-4d + 4e: i18n, column picker, polish UX)

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
- **VIEW-3c** (một phần): Render_Mode Text/Boolean/Html (Html→MarkupString). Image/Link/Badge/Template + Style_Rule_Json AST chưa làm.
- **VIEW-3d** (một phần): CRUD qua Edit_Form. Nút động từ Ui_View_Action + export client/server chưa làm.
- **Build**: `ICare247.Blazor.RuntimeCheck.csproj` **0 error** (2 warning DevExpress license).
- **Chưa làm**: VIEW-3c render giàu + conditional format, VIEW-3d nút động + export, VIEW-3e export rule, VIEW-2e export server-side, VIEW-1b seed, VIEW-1c spec 02, alias `/master/*`→view.

### Việc tiếp theo gợi ý
- Test E2E: tạo 1 View ở ConfigStudio (sau khi có data) → mở `/view/{code}` Blazor xem render thật.
- VIEW-3e export client xlsx/csv qua DxGrid (giá trị thuần).

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
