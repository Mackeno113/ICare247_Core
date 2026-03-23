# Last Session Summary

> Cập nhật: 2026-03-23 (session 4)

## Đã làm (session 23/03 — session 4)

### 3. Fix bug Blazor giao diện render sai (FormRepository + FieldRenderer)

**Root cause:** 3 mapping sai trong `sqlFields` của `FormRepository.GetByCodeAsync`:

| Cột | Trước (sai) | Sau (đúng) |
|---|---|---|
| Label | `fi.Label_Key AS Label` (raw key) | `COALESCE(r.Resource_Value, fi.Label_Key) AS Label` + LEFT JOIN Sys_Resource |
| DefaultValue | `fi.Control_Props_Json AS DefaultValueJson` | `fi.Control_Props_Json AS ControlPropsJson` |
| IsRequired | `fi.Is_Visible AS IsRequired` | `fi.Is_Visible AS IsVisible` |

**Thêm cột còn thiếu:**
- `LEFT JOIN Sys_Column sc → sc.Column_Code AS FieldCode`
- `fi.Is_ReadOnly AS IsReadOnly`
- Parameter `@LangCode` truyền xuống SQL

**IFormRepository.GetByCodeAsync** — thêm `langCode = "vi"` parameter:
- 5 caller cũ dùng named param `ct:` để không nhầm type
- `GetFormByCodeQueryHandler` truyền `request.LangCode` xuống repo

**FieldMetadata.cs (Domain)** — thêm properties:
- `IsVisible` (bool, default true)
- `IsReadOnly` (bool)
- `ControlPropsJson` (string?)
- Comment: `IsRequired` xác định bởi Val_Rule, không phải Ui_Field

**FieldMetadataDto.cs (Blazor)** — mirror đúng với domain entity

**FormRunner.razor:**
- Khởi tạo `FieldState` với `IsVisible` + `IsReadOnly` từ metadata
- `FieldCode` fallback: `field_{FieldId}` nếu Sys_Column chưa map
- Thêm `NormalizeFieldType()`: map DB enum → Renderer lowercase
  (`TextBox→text`, `DateEdit→date`, `NumberEdit→number`, `CheckBox→bool`, `ComboBox→select`)

### 4. DebugMode cho Blazor form

- `FieldRenderer.razor`: nhận `[Parameter] bool DebugMode` — hiển thị badge `[FieldCode | type | visible | ro]`
- `FormRunner.razor`: `?debug=1` hoặc `?debug=true` bật debug
  - `SupplyParameterFromQuery` dùng `string?` thay `bool` (Blazor router không parse "1" thành bool)
  - Log từng `FieldState` ra browser console (F12) khi DebugMode=true

### 5. Infra đã thêm trong session trước (cần deploy)

| File | Mục đích |
|---|---|
| `LocalConfigLoader.cs` | Đọc `%APPDATA%\ICare247\Api\appsettings.local.json` — giấu connection string |
| `DebugLogger.cs` | Static logger thread-safe, ghi file, bật/tắt qua config |
| `ExceptionExtensions.cs` | `ToReadable()` / `ToShort()` / `ToDetail()` |
| `ConnectionChecker.cs` | Test SQL Config + SQL Data + Redis on startup |
| `IDataDbConnectionFactory` | Interface riêng cho Data DB |
| `run-api.bat` / `run-blazor.bat` / `run-all.bat` | Script chạy từng project |

---

## Đã làm (session 23/03 — session 3)

### 1. Sys_Lookup Manager (WPF ConfigStudio)

| Thành phần | Chi tiết |
|---|---|
| `ISysLookupDataService` mở rộng | Thêm CRUD: GetItemsForEditAsync, AddItemAsync, UpdateItemAsync, DeleteItemAsync, AddLookupCodeAsync, ItemCodeExistsAsync |
| `LookupItemEditRecord` DTO | Bao gồm LabelVi + LabelEn để edit inline |
| `SysLookupDataService` | Implement đầy đủ CRUD — transaction khi write, upsert Sys_Resource vi/en theo pattern `{lookup_code_lower}.{item_code_lower}` |
| `SysLookupManagerViewModel` | Left panel: code list + thêm code mới; Right panel: items DataGrid + editor form |
| `SysLookupManagerView.xaml` | Layout 2 cột (ListBox codes | DataGrid items + editor panel), `IsNotBusy` / `HasNewCodeError` / `HasEditorError` để tránh converter phức tạp |
| Navigation | ViewNames.SysLookupManager, FormsModule, ShellViewModel nav item "Sys Lookup" |

**Lưu ý Razor gotchas:**
- Biến `@code` trong HTML bị parse nhầm là Razor directive → dùng `@(code)` hoặc đổi tên biến
- Biến `@section` trong HTML → dùng `@(section.Property)` hoặc đổi tên biến thành `sec`
- Lồng `"..."` trong HTML attribute + Razor expr → dùng biến cục bộ trước

### 2. ICare247.Blazor.RuntimeCheck (Blazor WASM)

| Thành phần | Chi tiết |
|---|---|
| Project tại | `src/ICare247.Blazor.RuntimeCheck/` — net9.0 BlazorWebAssembly |
| `Program.cs` | HttpClient với BaseAddress + header `X-Tenant-Id` từ `ApiSettings` (appsettings.json) |
| `FormApiService` | GET `/api/v1/config/forms/{code}` + danh sách form |
| `RuntimeApiService` | POST validate-field, validate, handle-event |
| `FormRunner.razor` | Load metadata → FORM_LOAD event → render → FIELD_CHANGED → UiDelta → submit validate |
| `FieldRenderer.razor` | Render text/number/date/datetime/bool/textarea |
| `Home.razor` | Landing page: danh sách form từ API + nhập thủ công Form_Code |
| CSS | Custom CSS không dùng Bootstrap class, responsive grid fields |

**Cấu hình:**
- API URL: `wwwroot/appsettings.json` → `ApiSettings.BaseUrl` = `https://localhost:7001`
- TenantId mặc định = 1

## Trạng thái

- Build Application + Infrastructure + Blazor: **0 errors, 0 warnings**
- Build API: file lock (process đang chạy) — không phải lỗi C#
- Commit mới nhất: `17660e0`

## Việc còn lại

1. **Test end-to-end:** Restart API + Blazor, mở `/form/sys_UI_Design?debug=1`, verify:
   - Label hiển thị tên đúng (không còn raw key `nhanvien.field.hoten`)
   - Không còn JSON `{"lookupCode":"GENDER"}` trong text field
   - Badge debug hiển thị đúng FieldCode, FieldType
   - Console F12 log đúng FieldState
2. Blazor: thêm support FieldType `select` (ComboBox/LookupBox — gọi GET Sys_Lookup)
3. MetadataEngine (IMetadataEngine) — backend
4. Integration tests backend
5. Chạy migration `003` + `004` trên DB thật (WPF)
