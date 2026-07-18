# Cấu hình control AddressBox — Khối địa chỉ (Tỉnh → Xã + địa chỉ chi tiết)

> **AddressBox** là control composite dùng chung cho địa chỉ hành chính VN. Một khối gồm:
> ô **địa chỉ chi tiết** (text) + chọn **Tỉnh/Thành** → **Xã/Phường** (lọc theo tỉnh, tìm server-side).
> Cấu hình hoàn toàn no-code qua ConfigStudio, không viết SQL, không code.

---

## 1. Mô hình dữ liệu — điểm QUAN TRỌNG NHẤT

AddressBox **lưu 2 cột**, dùng **2 Ui_Field**:

| Giá trị | Cột DB | Field | Ghi chú |
|---|---|---|---|
| Xã/Phường | `PhuongXa_Id` (int/bigint) | **field NEO** — mang EditorType `AddressBox` | Giá trị chính; Value = Id xã đã chọn |
| Địa chỉ chi tiết | `DiaChi` (nvarchar) | **field COMPANION** (text) | Số nhà, đường… Field này **tự ẩn**, không hiện riêng |
| Tỉnh/Thành phố | — (KHÔNG có cột) | — | Chỉ để **lọc** xã; suy từ `DM_PhuongXa.TinhThanhPho_Id` khi mở Sửa. **KHÔNG lưu** |

> ⚠️ Vì backend chỉ ghi cột của field đã khai (`BuildColumnParams`), khối địa chỉ **buộc phải là 2 field**.
> Field neo render cả khối và tự ghi giá trị vào field companion; companion bị đánh dấu **ẩn-render
> nhưng-vẫn-lưu** nên vẫn được ghi xuống DB bình thường.

---

## 2. Điều kiện tiên quyết

| Hạng mục | Yêu cầu |
|---|---|
| Cột DB | Bảng có 1 cột **int/bigint** cho xã (VD `PhuongXa_Id`) + 1 cột **nvarchar** cho địa chỉ (VD `DiaChi`) |
| Danh mục địa bàn | `DM_TinhThanhPho` + `DM_PhuongXa` đã seed (db/037); `DM_PhuongXa` có `TinhThanhPho_Id` |
| API | `PickersController` `/api/v1/pickers/dia-ban` đang chạy (tỉnh/xã/resolve-id) — có sẵn |
| Build | Đã rebuild **`ICare247_UI`** (Web) sau khi thêm editor type; hard reload trình duyệt |

---

## 3. Cấu hình trong ConfigStudio (từng bước)

Trên form (VD Thông tin công ty), cần **2 field** cho địa chỉ:

**Bước 1 — Field địa chỉ chi tiết (text)**
- Khai 1 field map cột `DiaChi`, EditorType = **TextBox**.
- Không cần cấu hình gì thêm. Field này sẽ tự ẩn khi có AddressBox trỏ tới.

**Bước 2 — Field xã/phường (AddressBox)**
- Khai 1 field map cột `PhuongXa_Id` (int/bigint).
- Tab **Cơ bản** → **Editor Type = AddressBox**.
- **Độ rộng (Col Span) = Full** — để khối hiển thị cân đối.

**Bước 3 — Trỏ field companion**
- Tab **Control Props** → panel **Khối địa chỉ (AddressBox)**.
- Ô **"Field địa chỉ chi tiết (text)"** → chọn field ở **Bước 1** (VD `DiaChi`).
- Lưu Field.

**Bước 4 — Dọn field thừa (nếu form cũ đã có)**
- Nếu form từng có field **Tỉnh/Thành phố riêng** → **XÓA** field đó. Tỉnh giờ nằm **bên trong** khối
  (bộ lọc, không lưu) → để lại sẽ có **2 chỗ chọn tỉnh**, dễ lệch dữ liệu.
- Label của field AddressBox (hiện phía trên khối) nên để trống hoặc đổi thành "Địa chỉ" — tránh
  trùng chữ "Xã/Phường" (label field) với "Xã/Phường" (trong khối).

**Bước 5 — Đồng bộ**
- Chạy **ConfigSync** đẩy Form master → tenant (Web đọc tenant).

---

## 4. Hành vi runtime

- Khối hiện: ô "Địa chỉ chi tiết" (full-width) + hàng [Tỉnh/Thành ⌄] [Xã/Phường ⌄ + tìm].
- Chọn Tỉnh → lọc Xã theo tỉnh; đổi Tỉnh → **xóa** xã đang chọn.
- Gõ tìm Xã: server-side, debounce 300ms.
- **Lưu**: ghi `PhuongXa_Id` (xã) + `DiaChi` (địa chỉ). Tỉnh không lưu.
- **Mở Sửa**: từ `PhuongXa_Id` đã lưu → resolve tên xã + suy tỉnh → chọn sẵn; địa chỉ text nạp lại.

---

## 5. i18n (chữ trong khối)

Chữ trong khối dùng key chung `common.address.*` (fallback tiếng Việt trong code). Đổi/dịch =
điền value vào overlay i18n (VD `en.json`), **không sửa code**. Áp dụng cho **mọi** AddressBox.

| Chữ | Key |
|---|---|
| Địa chỉ chi tiết | `common.address.detail` |
| Số nhà, đường, thôn/xóm… (placeholder) | `common.address.detail.hint` |
| Tỉnh/Thành phố | `common.address.province` |
| — Chọn tỉnh/thành — | `common.address.province.placeholder` |
| Xã/Phường | `common.address.ward` |
| — Chọn xã/phường — | `common.address.ward.placeholder` |
| Gõ để tìm… / Không có kết quả | `common.picker.search` / `common.picker.noresult` |

> Label ngay **trên** khối là `Ui_Field.Label_Key` của field AddressBox (riêng, không thuộc `common.address.*`).

---

## 6. Lỗi thường gặp

| Triệu chứng | Nguyên nhân | Cách xử |
|---|---|---|
| Field vẫn là ô text/lookup thường, không thành khối | Web chưa rebuild + hard reload, hoặc chưa ConfigSync | Rebuild `ICare247_UI` + Ctrl+Shift+R; chạy ConfigSync |
| Có 2 chỗ chọn tỉnh | Còn field Tỉnh/Thành phố **riêng** trên form | Xóa field tỉnh riêng (Bước 4) |
| Ô "địa chỉ chi tiết" hiện nhưng không lưu | Chưa trỏ **Field địa chỉ text** ở Control Props | Làm Bước 3 |
| Field địa chỉ text **hiện 2 lần** (riêng + trong khối) | Field companion chưa được đánh dấu ẩn | Kiểm tra addressTextField trỏ đúng FieldCode field text; rebuild Web |
| Mở Sửa: **địa chỉ hiện, nhưng Tỉnh/Xã trống** | (đã fix) giá trị số về dạng JsonElement chưa unwrap | Đảm bảo dùng bản Web sau fix `AddressRenderer` (ToLong bắt JsonElement) |
| Field companion không được lưu | Field companion bị đặt **Field ảo (IsVirtual)** | Bỏ cờ ảo — companion phải map cột thật để lưu |

---

## 7. Phụ lục kỹ thuật (cho người bảo trì)

- **Editor type → runtime**: `Ui_Field.Editor_Type = 'AddressBox'` → `NormalizeFieldType` → `"address"`
  (3 nơi: `MasterDataForm`, `FormRunner`, `LookupQueryService`) → `FieldRenderer` case `"address"`.
- **Renderer**: `ICare247.UI.DynamicForms/Components/FieldRenderers/AddressRenderer.razor` — host
  `IcAddressBlock` (Shared), bắn `OnChange` cho **cả** field neo (xã) lẫn companion (địa chỉ text).
- **Ẩn companion**: `CompositeFieldHelper.MarkAddressCompanions` quét field neo, đọc
  `ControlProps.addressTextField`, set `FieldState.IsHiddenByComposite = true` cho field text.
  `FieldRenderer` bỏ render field đó; payload Lưu vẫn giữ (lọc `IsVisible && !IsVirtual`). Gọi ở cả
  2 host sau khi dựng field states.
- **ControlProps JSON**: `{"addressTextField":"<FieldCode field text>"}` — dựng bởi
  `ControlPropsJsonService.BuildJson`, cấu hình qua `AddressBoxPropsPanel` (bind root VM:
  `AddressTextField` + `AddressTextFieldOptions`).
- **Backend**: KHÔNG đổi. Tái dùng `PickersController` + `IDiaBanPickerSource` (DI sẵn ở host).
- **Data model**: `TC_CongTy` mẫu — `PhuongXa_Id` (FK xã) + `DiaChi` (text); tỉnh suy từ
  `DM_PhuongXa.TinhThanhPho_Id`.

---

**Liên quan:** [cau-hinh-lookupbox.md](cau-hinh-lookupbox.md) · [cau-hinh-man-cong-ty.md](cau-hinh-man-cong-ty.md) ·
`docs/spec/31_SHARED_PICKER_CONTROLS_SPEC.md` (IcAddressBlock §4.2).
