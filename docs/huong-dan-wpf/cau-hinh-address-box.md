# Cấu hình control AddressBox — Khối địa chỉ (Tỉnh → Xã + địa chỉ chi tiết)

> **Tài liệu này dành cho ai?** Người cấu hình form trong ConfigStudio (Admin, Business Analyst, IT
> triển khai) — không cần biết lập trình, không cần viết SQL. Nếu bạn là lập trình viên/AI cần tra
> cứu nhanh, đi thẳng xuống [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật).
>
> **Bài này dùng để làm gì?** Gắn 1 "khối địa chỉ" hoàn chỉnh vào form — gồm ô **địa chỉ chi tiết**
> (gõ tay) + chọn **Tỉnh/Thành** → **Xã/Phường** (Xã/Phường tự lọc theo Tỉnh đã chọn, gõ để tìm
> nhanh) — thay vì phải dựng tay từng ô riêng lẻ.
>
> Ví dụ xuyên suốt cả bài: form **Thông tin công ty** (`TC_CongTy`) — cột `PhuongXa_Id` (xã) + cột
> `DiaChi` (địa chỉ chi tiết).

---

## Vài thuật ngữ cần biết trước khi đọc

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **AddressBox** | 1 loại control (Editor Type) dựng sẵn nguyên khối địa chỉ VN — chọn Tỉnh/Thành, Xã/Phường + gõ địa chỉ chi tiết, không phải ghép tay từng ô. |
| **Field chính (field neo)** | Field mang Editor Type AddressBox — đại diện cho cả khối, tự vẽ ra toàn bộ giao diện chọn Tỉnh/Xã + ô địa chỉ chi tiết. |
| **Field phụ (field companion)** | Field text chứa địa chỉ chi tiết, đứng "núp" phía sau field chính — không hiển thị riêng nhưng vẫn được lưu xuống DB bình thường. |
| **Field ảo (IsVirtual)** | Field không gắn với 1 cột dữ liệu thật. Field phụ ở đây **KHÔNG** được để chế độ này — vì cần lưu thật xuống cột `DiaChi`. |
| **Control Props** | Tab cấu hình riêng cho từng loại control — nơi bạn trỏ field chính tới field phụ. |
| **ConfigSync** | Thao tác đưa cấu hình từ nơi khai báo (master) xuống nơi người dùng thật sự dùng (tenant). |

---

## Phần A — Làm theo từng bước

### Chuẩn bị trước khi bắt đầu

Trước khi cấu hình, cần chắc chắn 3 điều sau (thường IT đã chuẩn bị sẵn, bạn chỉ cần hỏi lại nếu
chưa rõ):

1. **Bảng dữ liệu đã có sẵn đúng 2 cột**: 1 cột số (`int`/`bigint`) để lưu Xã/Phường (VD
   `PhuongXa_Id`) và 1 cột chữ (`nvarchar`) để lưu địa chỉ chi tiết (VD `DiaChi`). Nếu chưa có, báo
   IT tạo cột trước.
2. **Danh mục Tỉnh/Thành + Xã/Phường đã có sẵn dữ liệu** trong hệ thống (IT đã nạp sẵn).
3. **Ứng dụng web** (nơi nhân viên dùng) **đã được IT rebuild ít nhất 1 lần** sau khi control
   AddressBox được thêm vào hệ thống — việc này chỉ cần làm 1 lần cho toàn hệ thống, không lặp lại
   mỗi form.

---

### Bước 1 — Field địa chỉ chi tiết (text)

**Mục đích:** chứa phần địa chỉ người dùng gõ tay (số nhà, đường, thôn/xóm…) — phần này AddressBox
không tự sinh ra được, nên cần 1 field text riêng đứng "ẩn phía sau".

**Làm gì:** khai 1 field map vào cột `DiaChi`, **Editor Type = TextBox**. Không cần cấu hình gì
thêm — field này sẽ **tự ẩn** khi có AddressBox trỏ tới (làm ở Bước 3).

**Bạn sẽ thấy gì:** field xuất hiện bình thường trong danh sách field của form (sẽ tự ẩn khỏi vị trí
riêng sau khi hoàn tất Bước 3).

**Lỗi thường gặp:** nếu để field này ở chế độ **Field ảo (IsVirtual bật)** → giá trị sẽ **không được
lưu** xuống DB; field phụ bắt buộc phải map cột thật.

---

### Bước 2 — Field xã/phường (chính là AddressBox)

**Mục đích:** đây là field "cầm trịch" cả khối địa chỉ — người dùng thao tác trên field này, nó tự
vẽ ra cả ô chọn Tỉnh/Thành + Xã/Phường + hiện field địa chỉ chi tiết cạnh nó.

**Làm gì:** khai 1 field map vào cột `PhuongXa_Id` (int/bigint). Ở tab **Cơ bản**, đặt
**Editor Type = AddressBox**, và **Độ rộng (Col Span) = Full** để khối hiển thị cân đối trên form.

**Bạn sẽ thấy gì:** field hiện tạm thời như 1 field bình thường — khối chưa hiện đủ cho tới khi làm
xong Bước 3.

---

### Bước 3 — Trỏ field địa chỉ chi tiết vào field AddressBox

**Mục đích:** cho AddressBox biết field text nào (Bước 1) là nơi lưu phần "địa chỉ chi tiết" của
khối.

**Làm gì:** mở field AddressBox (Bước 2) → tab **Control Props** → panel **Khối địa chỉ
(AddressBox)** → ô **"Field địa chỉ chi tiết (text)"** → chọn field đã tạo ở Bước 1 (VD `DiaChi`) →
**Lưu Field**.

**Bạn sẽ thấy gì:** sau khi rebuild + đồng bộ (Bước 5), field địa chỉ chi tiết tự ẩn khỏi vị trí cũ
và xuất hiện lồng bên trong khối AddressBox — không còn hiện 2 lần riêng lẻ.

**Lỗi thường gặp:** ô địa chỉ chi tiết hiện nhưng gõ gì cũng **không lưu được** → quên làm bước này
(chưa trỏ field phụ).

---

### Bước 4 — Dọn field thừa (nếu form cũ đã có field Tỉnh/Thành riêng)

**Mục đích:** tránh có **2 chỗ chọn tỉnh** trên cùng 1 form (1 chỗ cũ + 1 chỗ mới nằm trong khối),
dễ gây lệch dữ liệu.

**Làm gì:** nếu form từng có field **Tỉnh/Thành phố riêng** → **XÓA** field đó (Tỉnh giờ nằm sẵn bên
trong khối AddressBox, chỉ dùng để lọc, không lưu riêng). Đồng thời nên để trống **Label** của field
AddressBox, hoặc đổi thành "Địa chỉ" — tránh trùng chữ "Xã/Phường" giữa label field và chữ bên trong
khối.

**Bạn sẽ thấy gì:** form chỉ còn đúng **1 chỗ** chọn Tỉnh/Thành (bên trong khối).

**Lỗi thường gặp:** bỏ qua bước này → 2 chỗ chọn tỉnh gây nhầm lẫn khi nhập liệu.

---

### Bước 5 — Đồng bộ + kiểm tra

**Mục đích:** đưa cấu hình từ ConfigStudio ra hệ thống thật để xem đúng như thiết kế.

**Làm gì:** chạy **ConfigSync** đẩy Form từ master xuống tenant (nơi Web đọc cấu hình). Sau đó mở
web, **hard reload trình duyệt** (Ctrl+Shift+R), vào form vừa cấu hình.

**Bạn sẽ thấy gì:** khối hiện đủ: ô "Địa chỉ chi tiết" (rộng hết hàng) + hàng chọn [Tỉnh/Thành ⌄]
[Xã/Phường ⌄ + ô tìm]. Chọn Tỉnh → Xã/Phường lọc theo đúng tỉnh đó; đổi Tỉnh khác → Xã/Phường đang
chọn tự xóa. Gõ tìm Xã/Phường có độ trễ nhỏ (chờ gõ xong mới tìm, không giật lag). Bấm **Lưu** → ghi
đúng Xã/Phường + địa chỉ chi tiết xuống DB (Tỉnh không lưu, chỉ để lọc). Mở lại bản ghi đã lưu → tự
chọn sẵn đúng Xã/Phường và suy ra đúng Tỉnh, địa chỉ chi tiết nạp lại nguyên văn.

**Lỗi thường gặp:** field vẫn hiện như ô text/lookup thường, chưa thành khối → Web chưa được rebuild
sau khi thêm control này, hoặc chưa chạy ConfigSync, hoặc chưa hard reload trình duyệt. Xem thêm
bảng [Lỗi thường gặp](#6-lỗi-thường-gặp) đầy đủ ở Phần B.

---

## Phần B — Tra cứu kỹ thuật

> Dành cho người đã quen cách làm ở Phần A, hoặc lập trình viên/AI cần tra nhanh tên trường kỹ thuật.
> Nội dung dưới đây là bản kỹ thuật đầy đủ — không giải thích lại từ đầu.

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
