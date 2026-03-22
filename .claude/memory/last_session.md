# Last Session Summary

> Cập nhật: 2026-03-22 (session 2)

## Đã làm (session 22/03 — LookupBox Config + Data Design + UX fixes)

### 1. Sys_Lookup + thiết kế dữ liệu tham chiếu

| Quyết định | Chi tiết |
|---|---|
| Bỏ tinyint cho lookup | Dùng `nvarchar` lưu `Item_Code` (VD: `'NAM'`, `'NU'`) thay số |
| Tạo `Sys_Lookup` | `Lookup_Code` + `Item_Code` + `Label_Key` → resolve i18n qua Sys_Resource |
| Migration 004 | `docs/migrations/004_add_sys_lookup.sql` — tạo bảng + seed GENDER |
| Quy tắc Unicode SQL | Mọi string literal SQL phải có `N'...'` prefix |

### 2. Control Props — LookupBox / RadioGroup nâng cấp

**queryMode 3 chế độ:**
- `table` — bảng hoặc view, filter qua WHERE
- `function` — Table-Valued Function, params vào thẳng hàm
- `sql` — full SELECT tùy ý

**filterParams:** map `@Alias` trong SQL → `FieldCode` trong form (dynamic, reload khi field thay đổi)

**dataSourceConditions:** đổi hẳn bảng nguồn theo điều kiện field khác

**reloadOnChange:** list FieldCode khi thay đổi → reload lookup

### 3. UI FieldConfigView — Control Props nâng cấp

- RadioButton chọn `queryMode`: `[Bảng/View] [Function] [SQL]`
- Panel `filterParams` inline: Alias + FieldRef + Kiểu + nút xóa
- Panel `reloadOnChange`: nhập FieldCode + Thêm/Xóa
- Panel `dataSourceConditions`: cấu hình điều kiện đổi datasource
- Panel `functionParams`: tham số TVF (param + nguồn: field/system)
- `FkFilterParam.cs` model mới
- Fix: `IsChecked="{Binding ..., Mode=OneWay}"` cho RadioButton

### 4. Behavior section xóa khỏi FieldConfigView

Visible/ReadOnly/Required static toggles → xóa (useless, nên dùng Rules)

### 5. Coding rules cập nhật

- `N'...'` bắt buộc cho mọi string SQL literal
- `Mode=OneWay` bắt buộc cho `IsChecked` binding vào computed property

## Trạng thái

- Build: **0 errors, 0 warnings** (backend + frontend)

## ⚠️ Việc còn lại QUAN TRỌNG

1. **Chạy migration trên DB**: `docs/migrations/003_remove_val_rule_field.sql`
2. **Chạy migration 004**: `docs/migrations/004_add_sys_lookup.sql`
3. Test LookupBox end-to-end: cấu hình field GioiTinh + PhongBanID, lưu JSON, load lại
4. `ExecuteManageI18n` trong FieldConfigViewModel pass `tableCode`
5. Move Required checkbox từ Behavior → Rules tab (Behavior section đã xóa)

## Task tiếp theo (gợi ý)

1. Chạy 2 migrations trên DB thật
2. Test LookupBox — chọn GENDER, lưu, mở lại xem JSON đúng không
3. Diễn giải cấu hình đã setup (feature hiển thị ý nghĩa JSON bằng tiếng Việt)
4. MetadataEngine backend
5. Blazor runtime frontend
