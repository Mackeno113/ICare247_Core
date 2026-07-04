# Hướng dẫn cấu hình **màn Công ty** (lưới cây + popup) trong ConfigStudio — ORG-CFG-3

> Màn Công ty = **engine-driven no-code** (ADR-024): lưới **TreeList** `Tree_TC_CongTy` (cây công ty mẹ–con)
> + popup `TC_CONGTY` nhập/sửa thông tin + các **LookupBox** (Cấp công ty / Công ty cha) và **2 cặp cascade có field ảo**:
> **Tỉnh/Thành → Phường/Xã** và **Ngân hàng → Chi nhánh ngân hàng** — trong đó **chỉ cấp con** (`PhuongXa_Id`, `ChiNhanhNganHang_Id`)
> **lưu DB**, còn cấp cha (Tỉnh/Thành, Ngân hàng) là **field ảo** chỉ để lọc, không lưu.
> KHÔNG code màn — chỉ cấu hình metadata trong ConfigStudio rồi đồng bộ xuống tenant. Engine TreeList runtime ĐÃ sẵn.
>
> 📖 Cơ chế field ảo + cascade: [12_CASCADE_LOOKUP_GUIDE.md](../spec/12_CASCADE_LOOKUP_GUIDE.md) (Filter SQL `@<FieldCode cha>` + ô *Tự reload*).
> 🛡️ **Chống cấu hình sai** (quy tắc vàng + bảng lỗi): [cau-hinh-field-ao-cascade.md](cau-hinh-field-ao-cascade.md).
>
> Đọc trước: [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md) (luồng đăng ký bảng/form) +
> [cau-hinh-man-quan-ly-view.md](cau-hinh-man-quan-ly-view.md) (7 tab màn Quản Lý View).

---

## Yêu cầu trước (1 lần)

- **Data DB `ICare247_Solution`** đã chạy: `db/037` (bảng `TC_CongTy` + danh mục `TC_CapCongTy`/`DM_TinhThanhPho`/`DM_PhuongXa`/`DM_NganHang`/`DM_ChiNhanhNganHang`)
  và **`db/051`** (`vw_TC_CongTy` — view JOIN sẵn tên hiển thị cho lưới).
- ConfigStudio **Settings → Target DB** trỏ **`ICare247_Solution`** (combobox bảng + Auto-generate đọc từ đây).
- **Cấu hình danh mục TRƯỚC** (đủ nguồn lookup, đúng thứ tự phụ thuộc): `TC_CapCongTy`, `DM_TinhThanhPho` → `DM_PhuongXa`,
  `DM_NganHang` → `DM_ChiNhanhNganHang` đã đăng ký `Sys_Table` (theo [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md)).
  Lookup chỉ cần bảng nguồn có trong `Sys_Table`.

> ⚠️ **Tiền đề schema — cần migration riêng TRƯỚC khi cấu hình (chưa có trong repo):** mô hình này **chỉ lưu cấp con** vào `TC_CongTy`.
> - **Phường/Xã:** cột `PhuongXa_Id` đã có sẵn (db/037); Tỉnh/Thành **không** lưu (suy qua `DM_PhuongXa.TinhThanhPho_Id`). ✅ đủ, không cần đổi schema.
> - **Chi nhánh ngân hàng:** `TC_CongTy` hiện chỉ có `NganHang_Id` (ngân hàng), **CHƯA có** `ChiNhanhNganHang_Id`. Theo quyết định
>   *"chỉ lưu chi nhánh"*: cần migration **thêm `ChiNhanhNganHang_Id BIGINT NULL` (FK → `DM_ChiNhanhNganHang`) và bỏ `NganHang_Id`**
>   (di trú dữ liệu cũ nếu có → suy ngân hàng qua chi nhánh), rồi **cập nhật `vw_TC_CongTy`** JOIN chi nhánh (+ ngân hàng qua chi nhánh).
>   Sau đó ngân hàng chỉ còn là **field ảo lọc**, không lưu.

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
| `PhuongXa_Id` | **LookupBox** | Phường/Xã | **cấp con cascade** — lookup `DM_PhuongXa`, lọc theo *Tỉnh/Thành* (field ảo, 2b) |
| `DienThoai` | TextBox | Điện thoại | |
| `Email` | TextBox | Email | |
| `Website` | TextBox | Website | |
| `NguoiDaiDien` | TextBox | Người đại diện | |
| `GiamDoc` | TextBox | Giám đốc | |
| `KeToanTruong` | TextBox | Kế toán trưởng | |
| `ChiNhanhNganHang_Id` | **LookupBox** | Chi nhánh ngân hàng | **cấp con cascade** — lookup `DM_ChiNhanhNganHang`, lọc theo *Ngân hàng* (field ảo, 2b). *(Cột mới sau migration — xem Tiền đề)* |
| `SoTaiKhoan` | TextBox | Số tài khoản | |
| `TrangThai` | LookupComboBox | Trạng thái | `Sys_Lookup` (vd `TRANGTHAI_CONGTY`) hoặc TextBox tạm |

> Mọi nhãn dùng nút **🌐 Dịch** (sinh key `tc_cong_ty.field.*` rồi nhập vi/en) — KHÔNG gõ tiếng Việt thẳng vào ô `_Key`.
> ⚠️ **KHÔNG** tick `NganHang_Id` ở Auto-generate (cột đã bỏ theo Tiền đề). Ngân hàng là **field ảo** — thêm tay ở **2a-bis**.

### 2a-bis — Thêm 2 **field ảo** (cấp cha của cascade)

Field cha (Tỉnh/Thành, Ngân hàng) **không có cột** trong `TC_CongTy` nên Auto-generate không sinh — **thêm tay**:
Panel Field → **+ Thêm field** → mỗi field đặt như sau, rồi tab **Behavior** bật **🔮 Field ảo** (ô **Field Code** hiện ra bên dưới — nhập đúng mã):

| Field ảo | Field Code | Editor | Nhãn (🌐) | Nguồn lookup | Ghi chú |
|---|---|---|---|---|---|
| Tỉnh/Thành | `TinhThanhPho_Id` | **LookupBox** | Tỉnh/Thành | `DM_TinhThanhPho` (Value `Id`, Display `Ten`) | 🔮 Field ảo = ✓ · **không** lưu DB · chỉ để lọc Phường/Xã |
| Ngân hàng | `NganHang_Id` | **LookupBox** | Ngân hàng | `DM_NganHang` (Value `Id`, Display `Ten`) | 🔮 Field ảo = ✓ · **không** lưu DB · chỉ để lọc Chi nhánh |

> 🔑 **Field Code của field ảo = tên `@param`** mà field con dùng trong Filter SQL (2b). Đặt cha **trước/ở trên** con trong form (chọn cha trước).
> Bật 🔮 Field ảo → backend tự **loại field khỏi payload lưu** (`Is_Virtual=1`) — không cần cột DB, không gây lỗi ghi.

### 2b — Cấu hình LookupBox (mỗi field FK)
Chọn field FK → đổi **Editor = LookupBox** → panel **Cấu hình Lookup** (`Ui_Field_Lookup`). Query_Mode = **table** cho tất cả:

**Field độc lập (không cascade):**

| Field | Source_Name | Value_Column | Display_Column | Code_Field | Ghi chú |
|---|---|---|---|---|---|
| `CapCongTy_Id` | `TC_CapCongTy` | `Id` | `Ten` | `Ma` | |
| `CongTy_Cha_Id` | `TC_CongTy` | `Id` | `Ten` | `Ma` | self-ref; (tuỳ) TreePicker; bật **Tìm kiếm** |

**Cấp cha ảo (nguồn lọc — bật 🔮 Field ảo ở 2a-bis):**

| Field ảo | Source_Name | Value_Column | Display_Column | Ghi chú |
|---|---|---|---|---|
| Tỉnh/Thành (`TinhThanhPho_Id`) | `DM_TinhThanhPho` | `Id` | `Ten` | không Filter_Sql — chọn tự do; bật **Tìm kiếm** |
| Ngân hàng (`NganHang_Id`) | `DM_NganHang` | `Id` | `Ten` | không Filter_Sql; bật **Tìm kiếm** |

**Cấp con (LƯU DB — cascade theo cha ảo):** ngoài Source/Value/Display, điền thêm **Filter SQL** + ô **"Tự reload khi field thay đổi"**:

| Field con | Source_Name | Value/Display | **Filter SQL** | **Tự reload khi field thay đổi** |
|---|---|---|---|---|
| `PhuongXa_Id` | `DM_PhuongXa` | `Id` / `Ten` | `TinhThanhPho_Id = @TinhThanhPho_Id` | `TinhThanhPho_Id` |
| `ChiNhanhNganHang_Id` | `DM_ChiNhanhNganHang` | `Id` / `Ten` | `NganHang_Id = @NganHang_Id` | `NganHang_Id` |

- 🔑 **`@param` PHẢI trùng `Field Code` của field cha ảo** (bên trái `=` là cột FK trong bảng con; bên phải là `@` + Field Code cha).
  Sai tên ⇒ danh sách con luôn rỗng. Xem [12_CASCADE_LOOKUP_GUIDE.md §1](../spec/12_CASCADE_LOOKUP_GUIDE.md).
- **Tìm kiếm (Search_Enabled)**: bật cho Phường/Xã, Chi nhánh, Công ty cha (danh sách dài).
- Luồng: chọn Tỉnh (ảo) → Phường/Xã reload theo tỉnh (chỉ `PhuongXa_Id` lưu). Chọn Ngân hàng (ảo) → Chi nhánh reload theo ngân hàng (chỉ `ChiNhanhNganHang_Id` lưu).
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
| `TenPhuongXa` | Phường/Xã | tên cấp con đã lưu (`PhuongXa_Id`) |
| `TenChiNhanhNganHang` | Chi nhánh NH | tên cấp con đã lưu (`ChiNhanhNganHang_Id`) — cần view cập nhật (Tiền đề) |
| `DienThoai` | Điện thoại | |
| `TrangThai` | Trạng thái | Render = Badge (tuỳ) |

> Cột audit (Id/CreatedBy…) **không thêm**. Cấp cha ảo (Tỉnh/Thành, Ngân hàng) **không lưu** nên **không có cột** ở lưới —
> nếu muốn hiện tên tỉnh/ngân hàng thì suy qua chuỗi JOIN trong view (`TenTinhThanhPho` qua `DM_PhuongXa.TinhThanhPho_Id`;
> `TenNganHang` qua `DM_ChiNhanhNganHang.NganHang_Id`). Alias JOIN tuỳ `vw_TC_CongTy` thực tế (mở view xem chính xác).

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
2. **Thêm** → popup 2 cột; LookupBox Cấp/Công ty cha chọn được (có tìm kiếm). ✅
3. **Cascade Tỉnh→Phường/Xã:** chọn Tỉnh (field ảo) → danh sách Phường/Xã chỉ còn thuộc tỉnh đó; đổi Tỉnh → Phường/Xã tự xóa + nạp lại. ✅
4. **Cascade Ngân hàng→Chi nhánh:** chọn Ngân hàng (field ảo) → Chi nhánh lọc theo ngân hàng; đổi Ngân hàng → Chi nhánh reload. ✅
5. **Lưu** → kiểm DB: `TC_CongTy` chỉ ghi `PhuongXa_Id` + `ChiNhanhNganHang_Id` (**không** có cột Tỉnh/Ngân hàng — field ảo bị loại khỏi payload). ✅
6. Lưu công ty con (chọn Công ty cha) → cây hiện đúng phân cấp. ✅

---

## Checklist nhanh

- [ ] **Tiền đề schema:** migration `TC_CongTy` thêm `ChiNhanhNganHang_Id` + bỏ `NganHang_Id` + cập nhật `vw_TC_CongTy` (chưa có trong repo)
- [ ] Sys_Table: `TC_CongTy` (form) + `vw_TC_CongTy` (lưới)
- [ ] Ui_Form `TC_CONGTY` Popup 2 cột + fields + LookupBox độc lập (Cấp/Cha) + i18n
- [ ] **2 field ảo** (Tỉnh/Thành `TinhThanhPho_Id`, Ngân hàng `NganHang_Id`): 🔮 Field ảo=✓, Field Code đúng, nguồn lookup
- [ ] **2 field con cascade** (`PhuongXa_Id`, `ChiNhanhNganHang_Id`): Filter SQL `<FK> = @<FieldCode cha>` + ô *Tự reload* = FieldCode cha
- [ ] Ui_View `Tree_TC_CongTy`: Source=View `vw_TC_CongTy`, Key=`Id`, Parent=`CongTy_Cha_Id`, Edit_Form=`TC_CONGTY`
- [ ] Cột lưới + caption i18n (tên cấp con: `TenPhuongXa`, `TenChiNhanhNganHang`); Hành vi (selection/CRUD)
- [ ] Route menu `/view/Tree_TC_CongTy` + quyền
- [ ] Đồng bộ cấu hình → kiểm tra (cascade chạy + payload chỉ lưu cấp con)

> **Quy ước:** `View_Code` cây = `Tree_<Bảng>`; `Form_Code` CHỮ HOA; mọi text người dùng = `*_Key` dịch qua 🌐;
> LookupBox = FK int sang bảng nghiệp vụ (Value=`Id`, Display=`Ten`); **field ảo** = cấp cha chỉ lọc, không lưu (`Is_Virtual`);
> `@param` cascade = Field Code field cha. Cấu hình **danh mục trước** rồi mới tới Công ty.
