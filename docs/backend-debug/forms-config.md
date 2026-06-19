# Debug: Cấu hình Form (Form metadata — Config DB)

> Module quản lý **metadata form** (Ui_Form/Ui_Field/Ui_Tab…) ở **Config DB**. Chủ yếu phục vụ
> ConfigStudio + render form. Khác với [master-data.md](master-data.md) (dữ liệu nghiệp vụ ở Live DB).
> Bối cảnh chung: [README.md](README.md).

## 1. API (route gốc `api/v1/config/forms`)

| Method | URL | Mục đích | Command/Query |
|---|---|---|---|
| GET | `/` | Danh sách form (filter + paging) | `GetFormsListQuery` |
| GET | `/{code}` | Metadata đầy đủ 1 form | `GetFormByCodeQuery` |
| GET | `/{code}/audit` | Lịch sử thay đổi form | `GetFormAuditLogQuery` |
| POST | `/` | Tạo form mới → **201** | `CreateFormCommand` |
| PUT | `/{code}` | Cập nhật → **204** | `UpdateFormCommand` |
| POST | `/{code}/deactivate` | Ẩn (soft) | `DeactivateFormCommand` |
| POST | `/{code}/restore` | Khôi phục | `RestoreFormCommand` |
| POST | `/{code}/clone` | Nhân bản | `CloneFormCommand` |
| POST | `/{code}/invalidate-cache` | Xóa cache form | `IConfigCache.InvalidateFormAsync` |

Header bắt buộc: `X-Tenant-Id: 1`.

> **Fix cache form (2026-06-19) — 2 bug khiến đổi `Form_Columns`/cấu hình không ăn dù đã flush:**
> 1. `MetadataEngine` rebuild `enriched` **bỏ sót `Columns`/`MaxWidth`** → form luôn mất `Form_Columns` (đã copy đủ).
> 2. `GetFormByCodeQueryHandler` (đường `/config/forms/{code}` mà popup Thêm/Sửa dùng) dùng **version cache cứng = 0**
>    → "Cưỡng chế làm mới" (Bump version) không chạm tới → đổi sang `_version.Get(tenantId)`.
> 3. `InvalidateFormCacheAsync` giờ xóa **cả 2 key**: `CacheKeys.RuntimeForm` (MetadataEngine) + `CacheKeys.Form` (web+mobile).
>
> Số cột FORM popup = `Ui_Form.Form_Columns` (1..4 → CSS `--form-cols`); số cột LƯỚI = `Ui_Field.Show_In_List`.

## 2. Payload tiêu biểu

- **GET list** (query): `?platform=web&tableId=5&isActive=true&search=abc&page=1&pageSize=20`
- **GET {code}** (query): `?lang=vi&platform=web`
- **POST create** (body):
  ```json
  { "formCode": "FRM_NHANVIEN", "tableId": 5, "platform": "web",
    "layoutEngine": "Grid", "description": "Form nhân viên" }
  ```

## 3. Code ở lớp nào

| Lớp | File |
|---|---|
| Api | `Controllers/FormController.cs` |
| Application | `Features/Forms/Queries/*` + `Features/Forms/Commands/*` (mỗi cái có Command/Query + Handler + Validator) |
| Application (cache) | `Engines/ConfigCache.cs` (facade L1+L2, ADR-014) + `Engines/MetadataEngine.cs` |
| Infrastructure | `Repositories/FormRepository.cs`, `FieldRepository.cs`, `AuditLogRepository.cs` — **Config DB** (`IDbConnectionFactory`) |

## 4. Luồng (đọc metadata — `GET /{code}`)

```
FormController.GetByCode → GetFormByCodeQuery → GetFormByCodeQueryHandler
  └─ IConfigCache.GetFormAsync(code, lang, platform, tenant)      ★ cache-aside
       ├─ hit L1 (MemoryCache) → trả ngay
       ├─ hit L2 (Redis) → trả + nạp L1
       └─ miss → MetadataEngine → FormRepository (Config DB) → cache lại
→ 200 FormMetadata (tabs, sections, fields, rules…)
```

Luồng **ghi** (`POST`/`PUT`): Controller → Command → Handler → `FormRepository.Insert/Update`
(Config DB) → **không tự xóa cache** ở mọi nơi; gọi `/invalidate-cache` sau khi sửa ở ConfigStudio.

## 5. Breakpoint
1. `FormController.GetByCode` — `code`, `GetTenantId()`.
2. `GetFormByCodeQueryHandler.Handle` — vào cache hay xuống repo?
3. `ConfigCache.GetFormAsync` — L1/L2 hit/miss (đặt ở nhánh `GetOrLoadAsync`).
4. `FormRepository.GetByCodeAsync` — SQL + tenant filter.

## 6. Lỗi thường gặp
- **Sửa form ở ConfigStudio nhưng API trả cũ** → cache chưa xóa: gọi `POST /{code}/invalidate-cache`.
- **404 form-not-found** → sai `code`, sai `tenant`, hoặc form đã `deactivate` (`Is_Active=0`).
- **List rỗng** → filter `platform`/`isActive` loại hết; bỏ filter để kiểm.
- Đây là **Config DB** — nếu nghi sai DB, kiểm `IDbConnectionFactory` (Config), KHÔNG phải LiveData.
