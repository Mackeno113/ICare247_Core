# Hướng dẫn cấu hình **lưới dữ liệu dạng tham chiếu** (có khóa ngoại) — ConfigStudio

> Áp dụng cho mọi màn nghiệp vụ mà bảng dữ liệu **trỏ FK sang bảng cha** và lưới cần
> hiển thị **TÊN cha** thay vì Id (vd Tỉnh/TP → Quốc gia, Phường/Xã → Tỉnh, Đơn hàng → Khách hàng).
> Triết lý vẫn là **KHÔNG code màn** — chỉ cấu hình metadata. (ADR-024)
>
> Ví dụ xuyên suốt: **`DM_TinhThanhPho`** (`QuocGia_Id → DM_QuocGia`).

> 📖 Nền tảng quy trình (đăng ký bảng, tạo form, tạo view) xem [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md)
> và [cau-hinh-man-quan-ly-view.md](cau-hinh-man-quan-ly-view.md). Tài liệu này chỉ nêu **phần KHÁC** của
> màn tham chiếu so với danh mục phẳng.

---

## Khác biệt cốt lõi so với danh mục phẳng

| | Danh mục **phẳng** (DM_QuocGia) | Lưới **tham chiếu** (DM_TinhThanhPho) |
|---|---|---|
| Nguồn **lưới** | base table | **VIEW** (JOIN sẵn để có tên cha) |
| Đăng ký `Sys_Table` | 1 đối tượng (table) | **2 đối tượng**: VIEW (lưới) + TABLE (form) |
| Field trong form | TextBox/Number… | thêm **1 LookupBox** (FK → bảng cha) |
| Cột lưới | cột thật | hiện **`TenCha`** (từ view), **ẩn** cột Id FK |

Nguyên tắc bất biến: **lưới ĐỌC qua VIEW**, **form GHI vào BASE TABLE**. Engine không bao giờ INSERT/UPDATE vào view.

---

## Yêu cầu trước (1 lần)

1. **Tạo VIEW đọc** trên Data DB `ICare247_Solution` — LEFT JOIN bảng cha, lấy cột tên, lọc `IsDeleted = 0`:

   ```sql
   CREATE OR ALTER VIEW dbo.vw_DM_TinhThanhPho
   AS
   SELECT  t.Id, t.Ma, t.Ten, t.LoaiHinh, t.QuocGia_Id,
           qg.Ten AS TenQuocGia                       -- ← cột tên cha cho lưới
   FROM        dbo.DM_TinhThanhPho t
   LEFT JOIN   dbo.DM_QuocGia       qg ON qg.Id = t.QuocGia_Id
   WHERE       t.IsDeleted = 0;
   ```
   *(File mẫu: [db/052_create_vw_danhmuc.sql](../../db/052_create_vw_danhmuc.sql) — `CREATE OR ALTER`, chạy lại an toàn.)*

   - **Giữ nguyên cột FK** (`QuocGia_Id`) trong view — không bắt buộc cho lưới nhưng tiện debug.
   - **LEFT JOIN** (không INNER) để bản ghi con vẫn hiện khi cha bị xóa mềm.

2. **Bảng cha đã cấu hình xong trước** (làm nguồn lookup). Đúng thứ tự phụ thuộc: cấu hình **Quốc gia → Tỉnh → Phường/Xã**.

3. ConfigStudio → **Settings → Target DB** trỏ `ICare247_Solution`.

---

## Bước 1 — Đăng ký **2 đối tượng** vào `Sys_Table` (Forms › Sys Table)

| Đăng ký | Schema | Dùng cho |
|---|---|---|
| **VIEW** `vw_DM_TinhThanhPho` | `dbo` | nguồn **lưới** (Grid) — để hiện `TenQuocGia` |
| **TABLE** `DM_TinhThanhPho` | `dbo` | nguồn **form** Thêm/Sửa/Xóa (ghi trực tiếp) |

> Combobox "Chọn bảng / view có sẵn (Target DB)" liệt kê cả view — gõ để lọc. `Table_Code` phải khớp
> đúng tên thật (engine đọc data bằng `Schema_Name` + `Table_Code`).

---

## Bước 2 — Tạo `Ui_Form` (Forms › New Form) — nguồn = **BASE TABLE**

1. **Form Code**: `DM_TINHTHANHPHO` · **Bảng nguồn**: `DM_TinhThanhPho` (**KHÔNG** chọn view) · **Display Mode**: Popup.
2. **Tạo Form** → **Auto-generate fields**: tick `Ma`, `Ten`, `LoaiHinh`, `QuocGia_Id`; bỏ `Id` + cột audit.
3. Tinh chỉnh field (🌐 dịch nhãn):

| Field | Editor | Nhãn | Ghi chú |
|---|---|---|---|
| `Ma` | TextBox | Mã | bắt buộc |
| `Ten` | TextBox | Tên | bắt buộc |
| `LoaiHinh` | LookupComboBox *(nếu là danh mục tĩnh)* | Loại hình | nguồn `Sys_Lookup`; nếu chữ tự do → TextBox |
| `QuocGia_Id` | **LookupBox** | Quốc gia | bắt buộc — cấu hình bên dưới |

### Cấu hình LookupBox `QuocGia_Id` (chế độ Bảng/View — khuyến nghị)

| Property | Giá trị |
|---|---|
| Cột Value (lưu DB) | `QuocGia_Id` |
| Cột Display | `Ten` |
| Tên bảng / View | `DM_QuocGia` |
| Filter SQL (tuỳ) | `IsDeleted = 0` |
| ORDER BY | `Ten ASC` |
| Cột hiển thị popup (tuỳ) | `[{"column":"Ma","title":"Mã","width":80},{"column":"Ten","title":"Tên","width":220}]` |

> **Đừng dùng LookupComboBox cho FK** — nó lưu mã chuỗi từ `Sys_Lookup`, không tạo khóa ngoại `int`.
> Phân biệt 3 editor: [09_FIELD_CONFIG_GUIDE.md §2.2.1](../spec/09_FIELD_CONFIG_GUIDE.md).

4. **Lưu thay đổi**.

---

## Bước 3 — Tạo `Ui_View` (Forms › Views) — nguồn = **VIEW**

1. **View_Type**: Grid · **View_Code**: `Grid_DM_TinhThanhPho` *(PHẢI khớp route `/view/Grid_DM_TinhThanhPho` trong menu)*.
2. Tab **Cơ bản**:
   - **Bảng nguồn**: `vw_DM_TinhThanhPho` (đối tượng VIEW đã đăng ký).
   - **Source_Type**: `View` · **Source_Object**: `vw_DM_TinhThanhPho`.
   - **Key_Field**: `Id` (cần cho Sửa/Xóa theo dòng).
   - **Edit_Form**: `DM_TINHTHANHPHO` (mở popup ghi vào base table).
3. Tab **Cột** (🔍 Chọn cột → 🌐 caption): `Ma` (Mã) · `Ten` (Tên) · `LoaiHinh` (Loại hình) · **`TenQuocGia`** (Quốc gia).
   - ⚠️ **KHÔNG** đưa `QuocGia_Id` (số) ra lưới — chỉ hiện `TenQuocGia`.
4. Tab **Hành vi**: bật **Allow_Add / Edit / Delete**.
5. **Lưu**.

---

## Bước 4–5 — Đẩy & kiểm tra

- Khai route `/view/Grid_DM_TinhThanhPho` vào menu (HT_ChucNang) → **Quản trị › Đồng bộ cấu hình** → Xem trước → Áp dụng từ master.
- Mở màn: lưới hiện Mã / Tên / Loại hình / **Quốc gia (tên)**; Thêm/Sửa mở popup, ô **Quốc gia** chọn từ `DM_QuocGia`. ✅

---

## Biến thể: tham chiếu **nhiều cấp** (cascade) — vd Phường/Xã

Khi bản ghi phụ thuộc 2 cấp (Phường/Xã → Tỉnh → Quốc gia), form cần **lookup phụ thuộc**:

1. View `vw_DM_PhuongXa` JOIN `DM_TinhThanhPho` lấy `TenTinhThanhPho` (xem db/052).
2. Form có **2 LookupBox**:
   - `Tinh_Id` (chọn trước) → nguồn `DM_TinhThanhPho`.
   - `PhuongXa`/cấp con → nguồn lọc theo tỉnh: **Filter SQL** `TinhThanhPho_Id = @TinhId` và đặt
     **"Tự động reload khi field thay đổi"** = `TinhId`. Đổi tỉnh → danh sách con reload, giá trị cũ tự xóa nếu không còn hợp lệ.
3. Lưới đọc view, hiện `TenTinhThanhPho`.

> Cơ chế cascade (`@{FieldCode}` trong Filter SQL + reload theo field): [09_FIELD_CONFIG_GUIDE.md §3.7](../spec/09_FIELD_CONFIG_GUIDE.md).

---

## Checklist nhanh

- [ ] View đọc đã tạo (LEFT JOIN cha, `IsDeleted = 0`).
- [ ] Bảng cha cấu hình xong **trước**.
- [ ] `Sys_Table`: đăng ký **VIEW** (lưới) **và** **TABLE** (form).
- [ ] Form nguồn = **base table**; field FK = **LookupBox** (Value = cột Id FK, Display = tên cha).
- [ ] View nguồn = **VIEW** (`Source_Type = View`); cột hiện **`TenCha`**, ẩn cột Id FK; `Key_Field = Id`; gắn `Edit_Form`.
- [ ] Route `/view/{View_Code}` khai trong menu → đồng bộ tenant.
- [ ] Mọi text người dùng thấy đều qua key i18n (nút 🌐).
