# Last Session Summary

> Cập nhật: 2026-03-23 (session 3)

## Đã làm (session 23/03)

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

- Build WPF: **0 errors, 0 warnings**
- Build Blazor: **0 errors, 0 warnings**
- Commit: `15d6ab4`

## Việc còn lại

1. **Test trực tiếp:** Chạy backend API + Blazor WASM, nhập Form_Code, verify events/validation
2. Cấu hình backend API `appsettings.json` nếu port khác `7001`
3. MetadataEngine (IMetadataEngine) — backend
4. Blazor: thêm support FieldType `select` (LookupBox — gọi GET Sys_Lookup)
5. Integration tests backend
