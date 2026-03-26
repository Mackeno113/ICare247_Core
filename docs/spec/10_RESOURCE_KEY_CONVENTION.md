# Resource Key Convention — ICare247 Validation Messages

**Spec:** 10_RESOURCE_KEY_CONVENTION
**Phiên bản:** 1.0
**Ngày:** 2026-03-26

---

## 1. Tổng quan

Mọi thông báo lỗi validation đều lưu trong bảng `Sys_Resource` thay vì hardcode trong code.
Mục tiêu: **đa ngôn ngữ**, **tùy chỉnh per-form**, **fallback tự động**.

---

## 2. Key Convention

### 2.1 Cấu trúc key

```
{scope}.{category}.{fieldCode}.{qualifier}
```

| Phần        | Mô tả                                                            | Ví dụ              |
|-------------|------------------------------------------------------------------|--------------------|
| `scope`     | `{formCode}` = form cụ thể, hoặc `sys` = global template       | `nhanvien`, `sys`  |
| `category`  | `val` = validation, `label`, `hint`, `tooltip`                  | `val`              |
| `fieldCode` | `Ui_Field.Field_Code` (lowercase)                               | `manhanvien`, `ngaysinh` |
| `qualifier` | Loại rule: `Required`, `Integer`, `Length`, `Compare`, `Regex`  | `Required`         |

### 2.2 Ví dụ keys

| Key                                    | Nội dung                                  |
|----------------------------------------|-------------------------------------------|
| `nhanvien.val.manhanvien.Required`     | "Mã nhân viên không được để trống"        |
| `nhanvien.val.ngaysinh.Required`       | "Ngày sinh không được để trống"           |
| `nhanvien.val.email.Regex`             | "Email không đúng định dạng"              |
| `sys.val.Required`                     | `"{0} không được để trống"` (template)   |
| `sys.val.Integer`                      | `"{0} chỉ được nhập số nguyên"`          |
| `sys.val.Length`                       | `"{0} không được vượt quá {1} ký tự"`    |
| `sys.val.Compare`                      | `"{0} phải lớn hơn hoặc bằng {1}"`       |

---

## 3. Fallback Hierarchy

Khi resolve thông báo cho một field, engine tìm kiếm theo thứ tự:

```
1. {formCode}.val.{fieldCode}.{qualifier}   ← form+field specific (ưu tiên cao nhất)
2. sys.val.{qualifier}                       ← global template (format với field label)
3. Hardcoded fallback                        ← khi Sys_Resource chưa setup
```

### Ví dụ: field `manhanvien` trong form `nhanvien`, qualifier `Required`

1. Tìm `nhanvien.val.manhanvien.Required` → nếu có → dùng
2. Tìm `sys.val.Required` → `"{0} không được để trống"` → format với label `"Mã nhân viên"` → `"Mã nhân viên không được để trống"`
3. Fallback: `"Mã nhân viên không được để trống"` (tự build từ label + hardcode)

---

## 4. Mapping Val_Rule.Error_Key

Mỗi `Val_Rule` đã lưu `Error_Key` theo pattern `{table}.val.{column}.{type}`:

```sql
-- Ví dụ trong Val_Rule
Error_Key = 'nhanvien.val.email.Regex'
```

→ `ValidationEngine` tra `ResourceMap[Error_Key]` để lấy text. Nếu không tìm thấy → dùng `Error_Key` làm fallback.

---

## 5. ResourceMap Loading

### 5.1 Query (ResourceRepository)

```sql
SELECT Resource_Key, Resource_Value
FROM   dbo.Sys_Resource
WHERE  Lang_Code = @LangCode
  AND  (Resource_Key LIKE @FormCode + '.%'
        OR Resource_Key LIKE 'sys.val.%'
        OR Resource_Key LIKE 'sys.hint.%')
```

> Load toàn bộ key của form + global sys keys trong **1 query duy nhất**.

### 5.2 Caching (MetadataEngine)

- **Cache key:** `meta:rt:{tenantId}:{formCode}:{langCode}`
- **L1 TTL:** 5 phút (MemoryCache)
- **L2 TTL:** 30 phút (Redis)
- **Invalidate:** gọi `IMetadataEngine.InvalidateFormCacheAsync(formCode, tenantId)` sau khi admin cập nhật form

---

## 6. ResourceResolver API

```csharp
// Resolve một resource key thành text
ResourceResolver.Resolve(map, key, fallback)

// Resolve thông báo "bắt buộc nhập" cho field
ResourceResolver.ResolveRequired(map, formCode, fieldCode, fieldLabel, langCode)

// Resolve thông báo lỗi rule (Error_Key → text)
ResourceResolver.ResolveRuleMessage(map, errorKey, fieldLabel, fallback)
```

---

## 7. Multi-language Support

Mỗi record `Sys_Resource` gắn với `Lang_Code`. Khi load:

- Truyền `langCode` (`"vi"` / `"en"`) vào `ResourceRepository.GetByFormAsync`
- Cache key bao gồm `langCode` → mỗi ngôn ngữ cache riêng
- Blazor client lấy `langCode` từ query string `?lang=vi`

---

## 8. Seed Data Gợi ý

```sql
-- Global templates (sys)
INSERT INTO Sys_Resource (Resource_Key, Lang_Code, Resource_Value) VALUES
  ('sys.val.Required', 'vi', N'{0} không được để trống'),
  ('sys.val.Required', 'en', N'{0} is required'),
  ('sys.val.Integer',  'vi', N'{0} chỉ được nhập số nguyên'),
  ('sys.val.Integer',  'en', N'{0} must be an integer'),
  ('sys.val.Length',   'vi', N'{0} không được vượt quá {1} ký tự'),
  ('sys.val.Length',   'en', N'{0} must not exceed {1} characters');

-- Form-specific overrides (nhanvien)
INSERT INTO Sys_Resource (Resource_Key, Lang_Code, Resource_Value) VALUES
  ('nhanvien.val.manhanvien.Required', 'vi', N'Mã nhân viên không được để trống'),
  ('nhanvien.val.ngaysinh.Required',   'vi', N'Ngày sinh không được để trống');
```
