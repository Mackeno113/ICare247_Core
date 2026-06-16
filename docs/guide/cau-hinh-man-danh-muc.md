# Hướng dẫn cấu hình màn engine-driven (no-code) trong ConfigStudio

> Áp dụng cho mọi màn nghiệp vụ chuẩn (danh mục, màn 1:1, lưới cây). Triết lý: **KHÔNG code màn**,
> chỉ cấu hình metadata (`Sys_Table` → `Ui_Form`/`Ui_Field` → `Ui_View`) trong ConfigStudio rồi đồng bộ
> xuống tenant — engine generic tự render. (ADR-024)
>
> Ví dụ xuyên suốt: **Cấp công ty (`TC_CapCongTy`)** — danh mục phẳng, không khóa ngoại (đơn giản nhất).

---

## Yêu cầu trước (1 lần)

- Bảng `TC_CapCongTy` đã có trong **Data DB `ICare247_Solution`** (db/037 — đã chạy).
- ConfigStudio **Settings → Target DB** trỏ vào **`ICare247_Solution`** (KHÔNG phải `QLNS_Demo`/Config).
  Combobox chọn bảng + nút Auto-generate đều đọc danh sách bảng từ Target DB này.
- Danh mục **phẳng** (Cấp công ty/phòng ban, Đơn vị tính, Ngân hàng, Quốc gia): **không cần** view.
  Danh mục **có FK** (Tỉnh/TP → Quốc gia, Phường/Xã → Tỉnh): cần view `db/051`/`db/052` (xem mục cuối).

---

## Bước 1 — Đăng ký bảng vào `Sys_Table`

1. Menu trái: **Forms › Sys Table**.
2. Panel **Tạo mới** → combobox **"Chọn bảng / view có sẵn (Target DB)"** → chọn `dbo.TC_CapCongTy`
   (gõ để lọc nhanh). → `Table_Code` + `Schema_Name` tự điền.
3. (Tuỳ) sửa **Table_Name** = `Cấp công ty` (tên hiển thị).
4. **Lưu**.

> `Table_Code` PHẢI khớp đúng tên bảng/view thật — engine đọc dữ liệu bằng `Schema_Name` + `Table_Code`.

---

## Bước 2 — Tạo `Ui_Form` (màn nhập/sửa, Popup)

1. Menu: **Forms › New Form**.
2. Tab **Thông tin**:
   - **Form Code**: `TC_CAPCONGTY` *(CHỮ HOA + số + `_` — ràng buộc validate; không cần khớp route).*
   - **Bảng nguồn dữ liệu**: chọn `TC_CapCongTy` (dropdown đọc từ `Sys_Table` đã đăng ký ở Bước 1).
   - **Display Mode**: **Popup**.
3. Bấm **Tạo Form** (lưu lần đầu để bật các thao tác field).
4. Bấm **Auto-generate fields** (panel Field — "Tạo fields tự động từ cấu trúc cột Target DB"):
   - Dialog liệt kê cột → **tick `Ma`, `Ten`, `ThuTu`**; **bỏ** `Id` + cột audit
     (`CreatedBy/At`, `UpdatedBy/At`, `IsDeleted`, `Ver`).
   - **Generate** → 3 field xuất hiện.
5. Tinh chỉnh từng field (editor + nhãn i18n, nút **🌐 Dịch**):
   - `Ma` → TextBox, nhãn **"Mã"**, bắt buộc.
   - `Ten` → TextBox, nhãn **"Tên"**, bắt buộc.
   - `ThuTu` → NumericBox (số), nhãn **"Thứ tự"**.
6. **Lưu thay đổi**.

---

## Bước 3 — Tạo `Ui_View` (lưới danh sách, Grid)

1. Menu: **Forms › Views (Grid/Tree)**.
2. Tạo mới:
   - **View_Type**: **Grid**.
   - **View_Code**: gõ hậu tố `TC_CapCongTy` → thành **`Grid_TC_CapCongTy`**
     *(PHẢI khớp route `/view/Grid_TC_CapCongTy` đã cấu hình trong menu).*
   - **Bảng nguồn**: `TC_CapCongTy` (danh mục phẳng → dùng thẳng base table).
3. Tab **Cột**: thêm `Ma`, `Ten`, `ThuTu` (nút 🌐 đặt caption: Mã / Tên / Thứ tự).
4. **Edit_Form**: chọn form `TC_CAPCONGTY` (Bước 2) — để double-click/Thêm mở popup.
5. **Lưu**.

---

## Bước 4 — Đẩy cấu hình xuống tenant

- App web → **Quản trị › Đồng bộ cấu hình** → **Xem trước** → **Áp dụng từ master**.
- *(Dev: master = tenant nên cấu hình đã ở đúng DB; bước này chủ yếu set cờ `Is_System`.)*

## Bước 5 — Kiểm tra

- Mở **Danh mục › Cấp công ty** → lưới Grid hiện ra; **Thêm/Sửa** mở popup 3 trường. ✅

---

## Biến thể cho các danh mục/màn khác

| Loại | Khác biệt |
|---|---|
| **ĐVT / Ngân hàng / Cấp phòng ban / Quốc gia** (phẳng) | Y hệt ví dụ. Đổi tên bảng + `View_Code` = `Grid_DM_DonViTinh` / `Grid_DM_NganHang` / `Grid_TC_CapPhongBan` / `Grid_DM_QuocGia`. |
| **Tỉnh/TP, Phường/Xã** (có FK) | Bước 1 đăng ký **VIEW** (`vw_DM_TinhThanhPho` / `vw_DM_PhuongXa`) làm **nguồn lưới** (để hiện tên cha); Bước 2 form thêm **lookup** (Tỉnh→Quốc gia; Phường/Xã→Tỉnh, cascade). Cần chạy `db/052` trước. |
| **Công ty** (cây) | Bước 1 đăng ký `vw_TC_CongTy` (cần `db/051`). Bước 3: **View_Type = TreeList**, `View_Code = Tree_TC_CongTy`, **Key = Id**, **Parent = CongTy_Cha_Id**; lookup Cấp/Phường-Xã/Ngân hàng/Công ty cha. |

## Quy ước nhanh

- **Form_Code**: CHỮ HOA + số + `_` (vd `TC_CAPCONGTY`). Không cần khớp route.
- **View_Code**: `{View_Type}_{Bảng}` — `Grid_*` cho lưới phẳng, `Tree_*` cho lưới cây. **PHẢI khớp** route `/view/{View_Code}` khai trong menu (`AppNav.NavScreen.Route` hoặc `HT_ChucNang.DuongDan`).
- **Thứ tự cấu hình theo phụ thuộc**: danh mục (Quốc gia → Tỉnh → Phường/Xã; Cấp công ty…) **TRƯỚC** → rồi màn tham chiếu (Công ty…), để đủ nguồn lookup.
- **Không cấu hình cột audit** (Id/CreatedBy/At/UpdatedBy/At/IsDeleted/Ver) vào form — engine tự xử lý.
