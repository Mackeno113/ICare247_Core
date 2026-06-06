# Last Session Summary

> Cập nhật: 2026-06-06 (session 38 — Master Data CRUD full-stack Tầng 0-3)

## Trạng thái cuối session

- **Branch:** `master`
- **Build:** backend `ICare247.slnx` 0/0 ✅, WPF `ConfigStudio.WPF.UI.slnx` 0/0 ✅

## Session 38 — Master Data CRUD (Tầng 0→3 hoàn thành)

Feature metadata-driven CRUD cho màn hình danh mục: List bản ghi + Thêm/Sửa (Popup hoặc Tab) + Xóa cứng có soft-check FK.

### Tầng 0 — DB Migrations
- `db/023_ui_form_display_mode.sql`: cột `Display_Mode nvarchar(20) DEFAULT 'Popup'` + CHK constraint (Popup|Tab) trên Ui_Form
- `db/024_ui_field_show_in_list.sql`: cột `Show_In_List bit DEFAULT 0` trên Ui_Field
- `docs/spec/02_DATABASE_SCHEMA.md` cập nhật 2 cột mới
- ⚠️ **DB-RUN**: chạy 023+024 trên DB thật (manual step chưa làm)

### Tầng 1 — Backend (0/0)
- `IMasterDataRepository` + `MasterDataRepository`: generic CRUD, tên bảng từ `[Schema_Name].[Table_Code]` (đọc server-side từ Ui_Form→Sys_Table), SafeIdentifierRegex, Dapper params
- `IReferenceCheckService` + `ReferenceCheckService`: soft-check FK quy ước tên (PK → `%_PK`), quét Sys_Column không lọc Is_Active, try/catch per candidate, log chi tiết
- CQRS `Features/MasterData/`: 4 query + 2 command + DTOs
- `MasterDataController`: 7 endpoint, 422 validation fail, 409 conflict + blockedBy[]
- Fix PK resolve: fallback INFORMATION_SCHEMA khi Sys_Column.Is_PK=0 (DB thật toàn False)
- Fix label: LEFT JOIN Sys_Resource + COALESCE(Resource_Value, Label_Key)
- DI registered, build 0/0

### Tầng 2 — Blazor (0/0 + verify live)
- `Services/MasterDataApiService.cs`
- `Pages/MasterData/MasterDataListPage.razor` (`@page "/master/{FormCode}"`) — switch Popup↔Tab
- `Components/MasterData/MasterDataGrid.razor` — cột theo Show_In_List (fallback all)
- `Components/MasterData/MasterDataForm.razor` — reuse FieldRenderer
- `Pages/MasterData/MasterDataTabPage.razor` (`@page "/master/{FormCode}/edit/{Id?}"`)
- `Components/MasterData/ConfirmDeleteDialog.razor` — soft-check on open + server-enforce 409
- CSS classes `.md-list-*`, `.md-grid*`, `.md-form*`, `.md-modal*`
- `Program.cs` DI: `MasterDataApiService`
- Verified live với API↔DB QLNS_Demo: PK NhanVienID/TrinhDoVanHoaID, list 7 bản ghi, label tiếng Việt

### Tầng 3 — WPF (0/0)
**WPF-1 (FormEditor — Display_Mode):**
- `FormDetailRecord.cs`: thêm `DisplayMode` property
- `FormDetailDataService.cs`: thêm `ISNULL(f.Display_Mode, 'Popup') AS DisplayMode` vào SELECT
- `IFormDataService.cs`: thêm `displayMode` param vào `CreateFormAsync` + `UpdateFormMetadataAsync`
- `FormDataService.cs`: thêm Display_Mode vào INSERT/SET clause
- `FormEditorViewModel.cs`: thêm `DisplayMode` property + `DisplayModeOptions = ["Popup","Tab"]`; reset khi tạo mới; load từ DB; 3 call sites UpdateFormMetadataAsync đều thêm `displayMode:`
- `FormEditorView.xaml`: thay block "Layout Engine" → "Chế độ mở form" ComboBox bind `DisplayMode`

**WPF-2 (FieldConfig — Show_In_List):**
- `FieldConfigRecord.cs`: thêm `ShowInList bool`
- `FieldDataService.cs`: thêm `fi.Show_In_List AS ShowInList` SELECT, `Show_In_List = @ShowInList` INSERT+UPDATE, `f.ShowInList` BuildFieldParam
- `FieldConfigViewModel.cs`: thêm `ShowInList` property + load (field.ShowInList) + save (ShowInList = ShowInList)
- `FieldConfigView.xaml`: thêm Border Col 2 Row 4 "📋 Hiện trong danh sách" ToggleSwitchEdit bind ShowInList

---

## Session 37 — Responsive form grid + SysTable UX polish (đã commit 7621c71)

1. Blazor responsive: `--cols` biến + media query (992/768px) + `min(--col-span,--cols)` clamp
2. SysTableManagerView: bỏ nút trùng, SaveButtonText "Tạo mới"→"Lưu", ScrollViewer error, AutoGenerateColumns="None"

---

## DB cần chạy trước khi run app

- `db/017_lock_on_edit_replace_is_enabled.sql`
- `db/018_add_is_virtual_field.sql`
- `db/019_ui_field_column_id_nullable.sql`
- `db/020_ui_field_add_field_code.sql`
- `db/021_ui_field_lookup_add_parent_column.sql`
- `db/022_ui_field_lookup_add_addnew.sql`
- **`db/023_ui_form_display_mode.sql`** ← MỚI (session 38)
- **`db/024_ui_field_show_in_list.sql`** ← MỚI (session 38)

## Pending tiếp theo

| Task | Status |
|---|---|
| **DB-RUN** Chạy 023+024 trên DB thật | ⏳ Manual step |
| **BE-002** Integration tests ValidationEngine + EventEngine | ❌ Chưa làm |
| **BE-004** Apply Design System tokens Blazor | ❌ Chưa làm |
| **WPF-14** Test LookupBox end-to-end | ⏳ Cần DB thật |
| Test Master Data CRUD end-to-end với DB thật | ⏳ Cần chạy DB migrations trước |
| Cascade dropdown Tỉnh→Xã: backend JOIN + trả đủ hierarchy ID | ❌ Chưa implement |
