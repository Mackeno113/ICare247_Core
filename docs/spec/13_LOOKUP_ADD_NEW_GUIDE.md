# Hướng dẫn: Thêm mới entity ngay trên LookupBox

> Cho phép người dùng tạo nhanh một bản ghi danh mục (vd Xã/Phường, Phòng ban) ngay
> trong dropdown LookupBox mà không rời form đang nhập. Bật/tắt theo **từng field**.

---

## 1. Luồng tổng thể

```
[ConfigStudio WPF]  bật AllowAddNew + chọn AddFormCode
        ↓ lưu Ui_Field_Lookup
[LookupBox runtime] đọc config → hiện nút "➕ Thêm mới"
        ↓ click
[LookupAddDialog]   render Ui_Form (AddFormCode) → nhập → Lưu
        ↓ POST /api/v1/lookups/insert
[Backend]           INSERT vào bảng nguồn → trả {value, display}
        ↓
[LookupBox]         reload list + auto-select bản ghi mới
```

---

## 2. Cấu hình (ConfigStudio WPF)

Form Editor → field LookupBox → tab **Control Props** → section **Thêm mới entity**:

| Mục | Giá trị |
|---|---|
| **Cho phép thêm mới (➕)** | ✓ bật |
| **Form Code dialog thêm mới** | Form_Code của Ui_Form bound vào **bảng nguồn** (vd `DS_XaPhuong`) |

Lưu vào `Ui_Field_Lookup`: `Allow_Add_New` + `Add_Form_Code` (Migration 022).

> ⚠️ Chỉ lưu khi đã nhập Form Code — bật mà bỏ trống thì coi như tắt.

---

## 3. Chuẩn bị form đích (AddFormCode)

Form trỏ bởi `AddFormCode` phải:
1. **Bound đúng bảng nguồn** của LookupBox (vd `DS_XaPhuong`).
2. Mỗi field nhập liệu có **FieldCode = tên cột DB** (đúng với field gắn cột — `FieldCode = COALESCE(Field_Code, Column_Code)`).
3. Field cha trong cascade (vd `TinhThanhID`) có thể đặt sẵn để người dùng chọn trong dialog.

Dialog tái dùng `FieldRenderer` nên hỗ trợ mọi loại control, kể cả LookupBox lồng + cascade bên trong dialog.

---

## 4. Backend insert — an toàn

`DynamicLookupRepository.InsertAsync`:
- Đọc `Source_Name` từ `Ui_Field_Lookup` theo `fieldId` — **không nhận tên bảng từ client**.
- Verify tenant (JOIN `Ui_Form → Sys_Table`) → chặn cross-tenant.
- Chỉ cho `Query_Mode = 'table'`.
- Validate `Source_Name` + tên cột qua `SafeIdentifierRegex`, chặn keyword DDL/DML.
- Bỏ qua cột trùng `Value_Column` (identity).
- `INSERT ... OUTPUT INSERTED.{ValueColumn}` parameterized (Dapper params, không nối chuỗi).

Endpoint: `POST /api/v1/lookups/insert`
```json
// Request
{ "fieldId": 12, "values": { "Ten_Xa": "Xã ABC", "TinhThanhID": 68 } }
// Response
{ "value": 25099, "display": "Xã ABC" }
```

---

## 5. Auto-select sau khi thêm

`LookupBoxRenderer.OnAddSaved`: reload data source → `State.Value = value mới` →
`OnChange` fire → form cha cập nhật + cascade con reload theo `ReloadTriggerField`.

---

## 6. Giới hạn hiện tại

| Giới hạn | Ghi chú |
|---|---|
| Insert **không** chạy ValidationEngine server-side | Dialog chỉ check `required` ở client. Cần nối thêm nếu muốn full validation. |
| Field virtual bị loại khỏi insert | Chỉ insert field gắn cột DB thật. |
| FieldCode phải = tên cột DB | Nếu form đích dùng Field_Code tùy ý khác tên cột → insert sai cột. |

---

## 7. Tham chiếu mã nguồn

- Migration: [db/022_ui_field_lookup_add_addnew.sql](../../db/022_ui_field_lookup_add_addnew.sql)
- Backend insert: [DynamicLookupRepository.cs](../../src/backend/src/ICare247.Infrastructure/Repositories/DynamicLookupRepository.cs) (`InsertAsync`)
- Endpoint: [LookupController.cs](../../src/backend/src/ICare247.Api/Controllers/LookupController.cs) (`POST insert`)
- Dialog: [LookupAddDialog.razor](../../src/frontend/ICare247.Blazor.RuntimeCheck/Components/LookupAddDialog.razor)
- Renderer wiring: [LookupBoxRenderer.razor](../../src/frontend/ICare247.Blazor.RuntimeCheck/Components/FieldRenderers/LookupBoxRenderer.razor)
- WPF config UI: [LookupBoxPropsPanel.xaml](../../src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Modules.Forms/Views/Panels/ControlProps/LookupBoxPropsPanel.xaml)
