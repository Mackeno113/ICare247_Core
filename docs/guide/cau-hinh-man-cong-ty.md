# Hướng dẫn cấu hình **màn Công ty** (lưới cây + popup) trong ConfigStudio — ORG-CFG-3

> Màn Công ty = **engine-driven no-code** (ADR-024): lưới **TreeList** `Tree_TC_CongTy` (cây công ty mẹ–con)
> + popup `TC_CONGTY` nhập/sửa thông tin + các **LookupBox** (Cấp công ty / Phường-Xã / Ngân hàng / Công ty cha).
> KHÔNG code màn — chỉ cấu hình metadata trong ConfigStudio rồi đồng bộ xuống tenant. Engine TreeList runtime ĐÃ sẵn.
>
> Đọc trước: [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md) (luồng đăng ký bảng/form) +
> [cau-hinh-man-quan-ly-view.md](cau-hinh-man-quan-ly-view.md) (7 tab màn Quản Lý View).

---

## Yêu cầu trước (1 lần)

- **Data DB `ICare247_Solution`** đã chạy: `db/037` (bảng `TC_CongTy` + danh mục `TC_CapCongTy`/`DM_PhuongXa`/`DM_NganHang`)
  và **`db/051`** (`vw_TC_CongTy` — view JOIN sẵn tên Cấp/Phường-Xã/Ngân hàng/Công ty cha cho lưới).
- ConfigStudio **Settings → Target DB** trỏ **`ICare247_Solution`** (combobox bảng + Auto-generate đọc từ đây).
- **Cấu hình danh mục TRƯỚC** (đủ nguồn lookup): `TC_CapCongTy`, `DM_PhuongXa`, `DM_NganHang` đã đăng ký `Sys_Table`
  (theo [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md)). Lookup chỉ cần bảng nguồn có trong `Sys_Table`.

> **Lưu ý nguồn dữ liệu:** lưới cây đọc **VIEW** `vw_TC_CongTy` (có sẵn tên hiển thị); form ghi vào **TABLE** `TC_CongTy`.
> → đăng ký **cả hai** vào `Sys_Table` (Bước 1).

---

## Bước 1 — Đăng ký bảng + view vào `Sys_Table`

**Forms › Sys Table** → panel **Tạo mới** → combobox "Chọn bảng / view có sẵn (Target DB)":

1. Chọn **`dbo.TC_CongTy`** → `Table_Code` tự điền `TC_CongTy`; (tuỳ) Table_Name = `Công ty` → **Lưu**.
2. Chọn **`dbo.vw_TC_CongTy`** → `Table_Code` = `vw_TC_CongTy`; Table_Name = `Công ty (view cây)` → **Lưu**.

> Inspector liệt kê cả VIEW (ORG-CFG-1). `Table_Code` PHẢI khớp đúng tên thật — engine đọc data theo `Schema_Name`+`Table_Code`.

---

## Bước 2 — Tạo `Ui_Form` popup `TC_CONGTY` (màn nhập/sửa)

**Forms › New Form** → tab **Thông tin**:

- **Form Code**: `TC_CONGTY` *(CHỮ HOA + `_`; không cần khớp route)*.
- **Bảng nguồn dữ liệu**: `TC_CongTy` (TABLE — nơi ghi dữ liệu, KHÔNG chọn view).
- **Display Mode**: **Popup**.
- **Số cột (Form_Columns)**: **2** (form nhiều trường → 2 cột cho gọn).
- **Tạo Form** (lưu lần đầu để bật thao tác field).

### 2a — Auto-generate fields
Panel Field → **Auto-generate fields** → tick các cột nghiệp vụ, **BỎ** `Id` + cột audit
(`CreatedBy/At`, `UpdatedBy/At`, `IsDeleted`, `Ver`):

| Cột TC_CongTy | Editor | Nhãn (🌐 Dịch) | Ghi chú |
|---|---|---|---|
| `Ma` | TextBox | Mã | **Bắt buộc** + **Unique** |
| `Ten` | TextBox | Tên công ty | **Bắt buộc** |
| `TenVietTat` | TextBox | Tên viết tắt | |
| `CapCongTy_Id` | **LookupBox** | Cấp công ty | **Bắt buộc** — lookup `TC_CapCongTy` (xem 2b) |
| `CongTy_Cha_Id` | **LookupBox** (hoặc TreePicker) | Công ty cha | NULL = gốc; lookup `TC_CongTy` (self) |
| `MaSoThue` | TextBox | Mã số thuế | |
| `DiaChi` | TextBox/Memo | Địa chỉ | |
| `PhuongXa_Id` | **LookupBox** | Phường/Xã | lookup `DM_PhuongXa` |
| `DienThoai` | TextBox | Điện thoại | |
| `Email` | TextBox | Email | |
| `Website` | TextBox | Website | |
| `NguoiDaiDien` | TextBox | Người đại diện | |
| `GiamDoc` | TextBox | Giám đốc | |
| `KeToanTruong` | TextBox | Kế toán trưởng | |
| `NganHang_Id` | **LookupBox** | Ngân hàng | lookup `DM_NganHang` |
| `SoTaiKhoan` | TextBox | Số tài khoản | |
| `TrangThai` | LookupComboBox | Trạng thái | `Sys_Lookup` (vd `TRANGTHAI_CONGTY`) hoặc TextBox tạm |

> Mọi nhãn dùng nút **🌐 Dịch** (sinh key `tc_cong_ty.field.*` rồi nhập vi/en) — KHÔNG gõ tiếng Việt thẳng vào ô `_Key`.

### 2b — Cấu hình LookupBox (mỗi field FK)
Chọn field FK → đổi **Editor = LookupBox** → panel **Cấu hình Lookup** (`Ui_Field_Lookup`):

| Field | Source_Name | Value_Column | Display_Column | Code_Field | Ghi chú |
|---|---|---|---|---|---|
| `CapCongTy_Id` | `TC_CapCongTy` | `Id` | `Ten` | `Ma` | Query_Mode = **table** |
| `PhuongXa_Id` | `DM_PhuongXa` | `Id` | `Ten` | `Ma` | bật **Tìm kiếm** (dữ liệu lớn) |
| `NganHang_Id` | `DM_NganHang` | `Id` | `Ten` | `Ma` | |
| `CongTy_Cha_Id` | `TC_CongTy` | `Id` | `Ten` | `Ma` | self-ref; (tuỳ) TreePicker để chọn theo cây |

- **Tìm kiếm (Search_Enabled)**: bật cho Phường/Xã, Công ty cha (danh sách dài).
- Để trống `Filter_Sql` (không cascade) — TC_CongTy không có cột cha cấp trên cho Phường/Xã.
- **Lưu thay đổi**.

---

## Bước 3 — Tạo `Ui_View` TreeList `Tree_TC_CongTy` (lưới cây)

**Forms › Views (Grid/Tree)** → **Tạo mới**:

### Tab 1 — Cơ bản
| Trường | Giá trị |
|---|---|
| **View_Type** | **TreeList** *(chọn TRƯỚC — quyết định tiền tố code)* |
| **View_Code** | gõ hậu tố `TC_CongTy` → thành **`Tree_TC_CongTy`** *(PHẢI khớp route `/view/Tree_TC_CongTy`)* |
| **Bảng nguồn (Table)** | **`vw_TC_CongTy`** (view — có sẵn tên Cấp/Phường-Xã/Cha) |
| **Source_Type** | **View** |
| **Source_Object** | `vw_TC_CongTy` |
| **Title_Key** | 🌐 Dịch → `tc_cong_ty.view.tree.title` = "Danh sách công ty" |
| **Form Thêm/Sửa (Edit_Form)** | **`TC_CONGTY`** (form Bước 2) → double-click/Thêm mở popup |
| **Key_Field** | **`Id`** *(bắt buộc cho TreeList)* |

### Tab 4 — Cây
| Trường | Giá trị |
|---|---|
| **Parent_Field** | **`CongTy_Cha_Id`** |
| **Expand_Level** | `2` (mở sẵn 2 cấp) |

### Tab 5 — Cột
**🔍 Chọn cột** từ `vw_TC_CongTy` → thêm + đặt caption (🌐):

| Field_Name | Caption | Ghi chú |
|---|---|---|
| `Ma` | Mã | Ghim trái |
| `Ten` | Tên công ty | cột cây (TreeSpin tự ở cột đầu) |
| `TenVietTat` | Viết tắt | |
| `TenCapCongTy` | Cấp | tên đã JOIN trong view |
| `MaSoThue` | MST | |
| `TenPhuongXa` | Phường/Xã | |
| `DienThoai` | Điện thoại | |
| `TrangThai` | Trạng thái | Render = Badge (tuỳ) |

> Cột audit (Id/CreatedBy…) **không thêm**. Tên cột JOIN (`TenCapCongTy`/`TenPhuongXa`/`TenCongTyCha`) tuỳ theo
> `vw_TC_CongTy` thực tế (mở view xem alias chính xác).

### Tab 2 — Hành vi
- `Selection_Mode` = `multiple` (cho xóa hàng loạt) · `Allow_Add/Edit/Delete` = ✓ (đã gắn Edit_Form).
- `Virtual_Scroll` = ✓ nếu cây lớn.

→ **💾 Lưu**.

> **Bộ lọc theo công ty đang chọn (company-switcher):** màn Công ty hiển thị **chính cây công ty** nên thường
> KHÔNG lọc theo `@CongTyID_Active`. Token đó dành cho màn con (Phòng ban/Nhân viên). Không cần tab Bộ lọc ở đây.

---

## Bước 4 — Khai route vào menu (nếu chưa)

ORG-CFG-4 đã set màn Công ty `Route="/view/Tree_TC_CongTy"` trong `AppNav`. Nếu dùng menu server-driven
(`HT_ChucNang`), đảm bảo node Công ty có `DuongDan = /view/Tree_TC_CongTy` + cấp quyền Xem cho vai trò.

---

## Bước 5 — Đồng bộ xuống tenant

App web → **Quản trị › Đồng bộ cấu hình** → **Xem trước** → **Áp dụng từ master**.
- Cấu hình nay phủ đủ **14 bảng** (gồm `Ui_View`/`Ui_View_Column`/`Ui_Form`/`Ui_Field`/`Ui_Field_Lookup`/`Sys_Resource`)
  → 1 lần áp là màn + lookup + i18n xuống tenant trọn vẹn (CFGSYNC-2).
- Sau khi áp, cache config tự vô hiệu (CC-4) → không cần restart API.

## Bước 6 — Kiểm tra

1. Mở **Tổ chức › Công ty** (hoặc route `/view/Tree_TC_CongTy`) → **lưới cây** hiện công ty mẹ–con, mở sẵn 2 cấp. ✅
2. **Thêm** → popup 2 cột; LookupBox Cấp/Phường-Xã/Ngân hàng/Công ty cha chọn được (có tìm kiếm). ✅
3. Lưu công ty con (chọn Công ty cha) → cây hiện đúng phân cấp. ✅

---

## Checklist nhanh

- [ ] Sys_Table: `TC_CongTy` (form) + `vw_TC_CongTy` (lưới)
- [ ] Ui_Form `TC_CONGTY` Popup 2 cột + fields + 4 LookupBox (Ui_Field_Lookup) + i18n
- [ ] Ui_View `Tree_TC_CongTy`: Source=View `vw_TC_CongTy`, Key=`Id`, Parent=`CongTy_Cha_Id`, Edit_Form=`TC_CONGTY`
- [ ] Cột lưới + caption i18n; Hành vi (selection/CRUD)
- [ ] Route menu `/view/Tree_TC_CongTy` + quyền
- [ ] Đồng bộ cấu hình → kiểm tra

> **Quy ước:** `View_Code` cây = `Tree_<Bảng>`; `Form_Code` CHỮ HOA; mọi text người dùng = `*_Key` dịch qua 🌐;
> LookupBox = FK int sang bảng nghiệp vụ (Value=`Id`, Display=`Ten`). Cấu hình **danh mục trước** rồi mới tới Công ty.
