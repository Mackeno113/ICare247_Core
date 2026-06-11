# Resource Key Convention — ICare247 Validation Messages

**Spec:** 10_RESOURCE_KEY_CONVENTION
**Phiên bản:** 1.0
**Ngày:** 2026-03-26

---

## 1. Tổng quan

Mọi text hiển thị (label, placeholder, tooltip, thông báo validation) đều lưu trong bảng `Sys_Resource` thay vì hardcode trong code.
Mục tiêu: **đa ngôn ngữ**, **tùy chỉnh per-form**, **fallback tự động**.

---

## 1b. Field Display Keys (Label / Placeholder / Tooltip)

### Convention

```
{formCode}.field.{fieldCode}.{qualifier}
```

| Phần | Mô tả | Ví dụ |
|------|-------|-------|
| `formCode` | `Ui_Form.Form_Code` viết thường | `nhanvien` |
| `field` | cố định — phân biệt namespace với `val` | `field` |
| `fieldCode` | `Ui_Field.Column_Code` viết thường | `manhanvien` |
| `qualifier` | `label` / `placeholder` / `tooltip` | `label` |

### Ví dụ — field `MaNhanVien` trong form `NhanVien`

| Key | Nội dung |
|-----|---------|
| `nhanvien.field.manhanvien.label` | `"Mã nhân viên"` |
| `nhanvien.field.manhanvien.placeholder` | `"Nhập mã nhân viên..."` |
| `nhanvien.field.manhanvien.tooltip` | `"Mã định danh duy nhất"` |

### Auto-generate Key (nút "+ Tạo key")

Khi user nhấn **+ Tạo key** trong FieldConfigView:
1. Hệ thống build key theo pattern: `{formCode}.field.{fieldCode}.{qualifier}`
2. Pre-fill vào input — user có thể sửa trước khi lưu
3. Nếu key đã tồn tại trong `Sys_Resource` → cảnh báo, cho phép dùng tiếp hoặc hủy
4. Label Key tạo xong → Placeholder + Tooltip key tự gợi ý theo (chỉ thay qualifier), user có thể tách biệt nếu cần

### Fallback

Nếu `Label_Key` để trống hoặc không tìm thấy trong `Sys_Resource`:
- Fallback về `Column_Code` / `Column_Name` của cột DB
- Không throw error — luôn có giá trị hiển thị

### Cột lưu trong `Ui_Field`

| Cột | Qualifier |
|-----|-----------|
| `Label_Key` | `label` |
| `Placeholder_Key` | `placeholder` |
| `Tooltip_Key` | `tooltip` |

---

## 1bb. Tab Title Keys (Tiêu đề tab)

### Convention

```
{tableCode}.tab.{tabCode}.title
```

| Phần | Mô tả | Ví dụ |
|------|-------|-------|
| `tableCode` | `Sys_Table.Table_Code` viết thường — bảng nghiệp vụ gắn với form | `dm_trinhdovanhoa` |
| `tab` | cố định — phân biệt namespace với `section` / `field` / `val` | `tab` |
| `tabCode` | `Ui_Tab.Tab_Code` viết thường | `thongtin` |
| `title` | cố định — tab chỉ có 1 qualifier hiển thị | `title` |

> **Scope = `table_code`, KHÔNG phải `form_code`.** Vì section/tab gắn với bảng dữ liệu, key scope theo Table giúp tái sử dụng bản dịch khi nhiều form cùng bind 1 bảng.

### Ví dụ — tab `ThongTin` (form `DS_TrinhDoVanHoa` bind bảng `DM_TrinhDoVanHoa`)

| Key | Nội dung |
|-----|---------|
| `dm_trinhdovanhoa.tab.thongtin.title` | `"Thông tin"` |

### Cột lưu trong `Ui_Tab`

| Cột | Vai trò | Đa ngôn ngữ |
|-----|---------|-------------|
| `Tab_Code` | Mã kỹ thuật — unique trong form | ❌ cố định |
| `Title_Key` | Resource key → `Sys_Resource` | ✅ qua key |
| `Icon_Key` | Icon tùy chọn (không phải text dịch) | — |

### Auto-generate Key (nút "+ Tạo key")

Khi user nhấn **+ Tạo key** trong panel Thuộc tính của tab:
1. Hệ thống build key theo pattern: `{formCode}.tab.{tabCode}.title`
2. Pre-fill vào input — user có thể sửa trước khi lưu
3. Nếu key đã tồn tại trong `Sys_Resource` → cảnh báo, cho phép dùng tiếp hoặc hủy

### Fallback

`Title_Key` cho phép **NULL**:
- `NULL` → không hiện label tab (xem `Ui_Tab.Title_Key` schema)
- Có key nhưng không tìm thấy trong `Sys_Resource` → fallback về `Tab_Code`
- Không throw error

### Seed Data Gợi ý

```sql
INSERT INTO Sys_Resource (Resource_Key, Lang_Code, Resource_Value) VALUES
  ('dm_trinhdovanhoa.tab.thongtin.title', 'vi', N'Thông tin'),
  ('dm_trinhdovanhoa.tab.thongtin.title', 'en', N'Information');
```

---

## 1a. Form Title Key (Tiêu đề form)

```
{tableCode}.form.title
```

| Phần | Mô tả | Ví dụ |
|------|-------|-------|
| `tableCode` | `Sys_Table.Table_Code` viết thường | `dm_trinhdovanhoa` |
| `form` | cố định | `form` |
| `title` | cố định — form có 1 tiêu đề | `title` |

- Lưu key ở `Ui_Form.Title_Key`; backend resolve → `FormMetadata.FormName` theo `Lang_Code`.
- Hiển thị: popup "Thêm mới", header FormRunner, trang Master Data.
- Cấu hình trong WPF (tab Thông tin Form) qua popup 🌐 I18nEditorDialog.

Ví dụ: `dm_trinhdovanhoa.form.title` → vi "Trình độ văn hóa", en "Education Level".

---

## 1c. Section Title Keys (Tiêu đề group/panel)

### Convention

```
{tableCode}.section.{sectionCode}.title
```

| Phần | Mô tả | Ví dụ |
|------|-------|-------|
| `tableCode` | `Sys_Table.Table_Code` viết thường — bảng nghiệp vụ gắn với form | `dm_trinhdovanhoa` |
| `section` | cố định — phân biệt namespace với `tab` / `field` / `val` | `section` |
| `sectionCode` | `Ui_Section.Section_Code` viết thường | `thongtinchung` |
| `title` | cố định — section chỉ có 1 qualifier hiển thị | `title` |

> **Scope = `table_code`, KHÔNG phải `form_code`** (giống Tab — xem §1bb).

### Ví dụ — section `ThongTinChung` (form `DS_TrinhDoVanHoa` bind bảng `DM_TrinhDoVanHoa`)

| Key | Nội dung |
|-----|---------|
| `dm_trinhdovanhoa.section.thongtinchung.title` | `"Thông tin chung"` |

### Cột lưu trong `Ui_Section`

| Cột | Vai trò | Đa ngôn ngữ |
|-----|---------|-------------|
| `Section_Code` | Mã kỹ thuật — unique trong form, dùng cho event (`SET_VISIBLE` target section) | ❌ cố định |
| `Title_Key` | Resource key → `Sys_Resource` | ✅ qua key |

### Auto-generate Key (nút "+ Tạo key")

Khi user nhấn **+ Tạo key** trong panel Thuộc tính của section:
1. Hệ thống build key theo pattern: `{formCode}.section.{sectionCode}.title`
2. Pre-fill vào input — user có thể sửa trước khi lưu
3. Nếu key đã tồn tại trong `Sys_Resource` → cảnh báo, cho phép dùng tiếp hoặc hủy

### Fallback

`Title_Key` cho phép **NULL**:
- `NULL` → section không hiện thanh tiêu đề (group chỉ gom layout, không header)
- Có key nhưng không tìm thấy trong `Sys_Resource` → fallback về `Section_Code`
- Không throw error — luôn có giá trị hiển thị (hoặc không hiện header nếu NULL)

### Seed Data Gợi ý

```sql
INSERT INTO Sys_Resource (Resource_Key, Lang_Code, Resource_Value) VALUES
  ('dm_trinhdovanhoa.section.thongtinchung.title', 'vi', N'Thông tin chung'),
  ('dm_trinhdovanhoa.section.thongtinchung.title', 'en', N'General Information');
```

---

## 1d. View Keys (Grid / TreeList — tiêu đề màn, cột, nút)

> Áp cho cụm `Ui_View` / `Ui_View_Column` / `Ui_View_Action` (xem `14_VIEW_CONFIG_SPEC.md`).
> **Scope = `table_code`** (giống Tab/Section/Form) → tái dùng bản dịch khi nhiều view bind cùng bảng.
> Category cố định: **`view`**.

### Convention

```
{tableCode}.view.{viewCode}.title                        -- tiêu đề màn
{tableCode}.view.{viewCode}.col.{fieldName}.caption      -- tiêu đề cột
{tableCode}.view.{viewCode}.action.{actionCode}.label    -- nhãn nút
{tableCode}.view.{viewCode}.action.{actionCode}.tooltip  -- tooltip nút
{tableCode}.view.{viewCode}.action.{actionCode}.confirm  -- xác nhận (vd Xóa)

-- Panel lọc nâng cao (Ui_View_Filter — spec 14 §9, ADR-016)
{tableCode}.view.{viewCode}.filter.{filterCode}.label        -- nhãn control lọc
{tableCode}.view.{viewCode}.filter.{filterCode}.placeholder  -- placeholder
{tableCode}.view.{viewCode}.filter.{filterCode}.tooltip      -- tooltip
-- Nút Tìm/Đặt lại dùng key CHUNG (không scope theo view): common.filter.search / common.filter.reset
-- Thông báo thiếu tham số: common.validation.required = "{0} là bắt buộc"
```

| Phần | Mô tả | Ví dụ |
|------|-------|-------|
| `tableCode` | `Sys_Table.Table_Code` viết thường | `dm_nhanvien` |
| `view` | cố định — namespace | `view` |
| `viewCode` | `Ui_View.View_Code` viết thường | `ds_nhanvien` |
| `fieldName` | `Ui_View_Column.Field_Name` viết thường | `manhanvien` |
| `actionCode` | `Ui_View_Action.Action_Code` viết thường | `export` |

### Ví dụ — Grid `DS_NhanVien` bind bảng `DM_NhanVien`

| Key | vi | en |
|-----|----|----|
| `dm_nhanvien.view.ds_nhanvien.title` | `"Danh sách nhân viên"` | `"Employees"` |
| `dm_nhanvien.view.ds_nhanvien.col.manhanvien.caption` | `"Mã NV"` | `"Emp. Code"` |
| `dm_nhanvien.view.ds_nhanvien.action.export.label` | `"Xuất file"` | `"Export"` |

### Cột lưu (đều là KEY — không literal)

| Bảng | Cột text → key |
|---|---|
| `Ui_View` | `Title_Key`, `Export_File_Name_Key` |
| `Ui_View_Column` | `Caption_Key`, `Export_Caption_Key`, `Cell_Template_Key` |
| `Ui_View_Action` | `Label_Key`, `Tooltip_Key`, `Confirm_Key` |

### Fallback — tiêu đề cột (tái dùng label field, tránh dịch trùng)

```
1. Caption_Key (override riêng cho view)          ← ưu tiên cao nhất
2. Nếu cột bound → Ui_Field.Label_Key của field    ← tái dùng "Mã nhân viên" đã có
3. Field_Name / Column_Code                         ← fallback cuối, không throw
```

> Mặc định `Caption_Key = NULL` ⇒ tự lấy `Label_Key` của field. Chỉ set khi muốn caption khác label form.

### Export đa ngôn ngữ

Header cột khi xuất file = resolve `Export_Caption_Key ?? Caption_Key` theo **đúng `langCode` đang chọn**
→ file xlsx/pdf/docx có tiêu đề đúng ngôn ngữ. Ô dữ liệu vẫn lấy **giá trị thuần** (xem spec 14 §4).

### Auto-generate + seed

Giống nút "+ Tạo key": build sẵn theo pattern, pre-fill, cảnh báo nếu trùng, **auto seed vi+en** khi lưu
(pattern `RegisterI18nKeysAsync`). ResourceMap loader (§5.1) nạp thêm prefix `{tableCode}.view.%`.

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
  ('sys.val.Length',   'en', N'{0} must not exceed {1} characters'),
  -- Unique (field Is_Unique) — chống trùng. Override per-field: {form}.val.{field}.Unique
  ('sys.val.Unique',   'vi', N'{0} đã tồn tại'),
  ('sys.val.Unique',   'en', N'{0} already exists');

-- Form-specific overrides (nhanvien)
INSERT INTO Sys_Resource (Resource_Key, Lang_Code, Resource_Value) VALUES
  ('nhanvien.val.manhanvien.Required', 'vi', N'Mã nhân viên không được để trống'),
  ('nhanvien.val.ngaysinh.Required',   'vi', N'Ngày sinh không được để trống');
```
