# Hướng dẫn cấu hình **lưới dữ liệu dạng tham chiếu** (có khóa ngoại) — ConfigStudio

> **Tài liệu này dành cho ai?** Người cấu hình hệ thống (Admin, Business Analyst, IT triển khai) —
> **không cần biết lập trình**. Nếu bạn là lập trình viên/AI cần tra cứu kỹ thuật, đi thẳng xuống
> [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật).
>
> **Bài này dùng để làm gì?** Với 1 màn danh sách (lưới) mà dữ liệu có **liên kết tới bảng khác** (ví
> dụ Tỉnh/Thành phải chọn thuộc Quốc gia nào), bài này hướng dẫn làm sao để lưới **hiện TÊN** (ví dụ
> "Việt Nam") thay vì chỉ hiện **con số Id** khó hiểu.
>
> Ví dụ xuyên suốt cả bài: **`DM_TinhThanhPho`** — cột `QuocGia_Id` (khóa ngoại) trỏ tới bảng
> `DM_QuocGia`.

---

## Vài thuật ngữ cần biết trước khi đọc

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **Khóa ngoại (FK)** | Cách 1 bảng dữ liệu "trỏ" tới đúng 1 dòng của bảng khác bằng con số Id — ví dụ cột "Quốc gia" trong bảng Tỉnh/Thành trỏ tới đúng 1 dòng của bảng Quốc gia. |
| **LookupBox** | Ô nhập trên Form cho phép **chọn** 1 bản ghi có sẵn thay vì gõ tay (xem chi tiết ở [cau-hinh-lookupbox.md](cau-hinh-lookupbox.md)). |
| **Lưới (View)** | Màn hình danh sách nhiều bản ghi dạng bảng. |
| **SQL View** | Một "bảng ảo" do IT kỹ thuật viết sẵn, ghép dữ liệu từ nhiều bảng thật lại với nhau — chỉ cần khi cách đơn giản (không viết SQL) không đủ dùng. |
| **ConfigStudio** | Ứng dụng desktop dùng để khai báo cấu hình. |

---

## Phần A — Làm theo từng bước

### Chuẩn bị trước khi bắt đầu

- Bài này giả định bảng `DM_TinhThanhPho` và Form/Field của nó **đã cấu hình xong cơ bản** (nếu chưa
  biết cách đăng ký bảng/tạo Form/tạo màn danh sách, xem
  [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md) và
  [cau-hinh-man-quan-ly-view.md](cau-hinh-man-quan-ly-view.md) trước).
- Field `QuocGia_Id` trên Form sửa **đã là LookupBox** trỏ tới bảng `DM_QuocGia` (xem
  [cau-hinh-lookupbox.md](cau-hinh-lookupbox.md) nếu chưa cấu hình).
- Có **2 cách** làm bài này — bài chọn cách đơn giản nhất trước (không cần viết SQL):
  - **Cách đơn giản (khuyến nghị, gọi là "Cách A" trong Phần B)** — hệ thống tự ghép TÊN, chỉ cần
    khai báo trong ConfigStudio. Dùng được khi field FK ở trên đã là LookupBox kiểu "Bảng/View" và
    chỉ lấy 1 cột tên đơn giản.
  - **Cách nâng cao ("Cách B")** — cần nhờ IT kỹ thuật viết 1 SQL View riêng, dùng khi cách trên
    không đủ (ví dụ cần ghép tên từ nhiều bảng, hoặc hiển thị phức tạp). Xem
    [Phần B](#phần-b--tra-cứu-kỹ-thuật).

---

### Bước 1 — Kiểm tra đủ điều kiện dùng cách đơn giản

**Mục đích:** xác nhận màn hình này dùng được cách không cần viết SQL — đỡ tốn công nhờ IT.

**Làm gì:** mở Form sửa của bảng (ví dụ `DM_TINHTHANHPHO`), kiểm tra field `QuocGia_Id`:
- Editor Type là **LookupBox**.
- Chế độ truy vấn (Query Mode) là **Bảng / View**.
- Cột Value và Cột Display đều là **1 cột đơn** (không phải công thức ghép nhiều cột).

**Bạn sẽ thấy gì:** nếu cả 3 điều trên đúng → dùng được cách đơn giản, làm tiếp Bước 2. Nếu không (ví
dụ tên hiển thị phải ghép từ nhiều cột) → báo IT kỹ thuật làm theo cách nâng cao (Cách B trong
Phần B).

**Lỗi thường gặp:** field FK dùng **LookupComboBox** thay vì **LookupBox** — 2 control khác nhau,
cách đơn giản chỉ áp dụng cho LookupBox (khóa ngoại thật).

---

### Bước 2 — Chỉnh màn danh sách đọc thẳng bảng gốc

**Mục đích:** cho màn danh sách biết vẫn đọc thẳng bảng dữ liệu gốc, không cần đọc qua view riêng.

**Làm gì:** mở màn hình **Views** của lưới (ví dụ `Grid_DM_TinhThanhPho`), ở tab **Cơ bản**:
- Bảng nguồn: để nguyên là bảng gốc `DM_TinhThanhPho`.
- Ô **Key_Field**: chọn `Id`.
- Ô **Edit_Form**: chọn form sửa tương ứng (`DM_TINHTHANHPHO`).

**Bạn sẽ thấy gì:** cấu hình lưới không đổi gì nhiều — không cần tạo thêm view mới.

**Lỗi thường gặp:** không có gì đặc biệt, bước này chỉ xác nhận lại cấu hình sẵn có đã đúng.

---

### Bước 3 — Nối cột khóa ngoại với LookupBox để tự hiện tên

**Mục đích:** báo cho hệ thống biết cột `QuocGia_Id` trên lưới này nên hiện **tên** Quốc gia, lấy đúng
theo cấu hình LookupBox đã có ở Form sửa.

**Làm gì:**
1. Cần biết **mã số (Field_Id)** của LookupBox `QuocGia_Id` trên Form sửa — mã này là số kỹ thuật,
   nhờ IT kỹ thuật chạy giúp câu lệnh tra cứu (có sẵn ở [Phần B](#phần-b--tra-cứu-kỹ-thuật)) để lấy
   đúng số.
2. Ở màn **Views**, tab **Cột**, tìm dòng cột `QuocGia_Id` → bật hiển thị (**Is_Visible**), đặt tiêu
   đề "Quốc gia".
3. Tìm ô **"FK lookup (Field_Id)"** (cạnh cột Render) → nhập đúng số Field_Id vừa lấy ở bước 1.
   ConfigStudio sẽ **tự ghi cấu hình** phía sau — bạn không cần gõ JSON hay SQL tay.

**Bạn sẽ thấy gì:** sau khi Lưu và mở lại màn, cột `QuocGia_Id` trên lưới hiện đúng **tên Quốc gia**
thay vì con số Id.

**Lỗi thường gặp:** nhập sai số Field_Id (nhầm field khác) → cột lưới hiện sai tên hoặc rỗng; quên
bật **Is_Visible** cho cột → cột không hiện trên lưới dù đã cấu hình đúng.

---

### Bước 4 — Lưu, đồng bộ và kiểm tra kết quả

- Bấm **Lưu**.
- Mở ứng dụng web → **Quản trị › Đồng bộ cấu hình** → **Xem trước** → **Áp dụng từ master**.
- Mở màn Tỉnh/Thành → cột **Quốc gia** trên lưới hiện đúng tên (ví dụ "Việt Nam") thay vì số Id. ✅
- Lọc/sắp xếp theo cột Quốc gia vẫn hoạt động bình thường.

Nếu màn hình cần hiển thị phức tạp hơn (ghép nhiều bảng, ghép chữ) mà cách trên không hiện đúng tên,
xem **Cách B** ở [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật) — cần nhờ IT kỹ thuật viết 1
SQL View riêng. Nếu màn còn cần lọc theo **nhiều cấp** (Phường/Xã → Tỉnh → Quốc gia), xem thêm bài
[cau-hinh-field-ao-cascade.md](cau-hinh-field-ao-cascade.md).

---

## Phần B — Tra cứu kỹ thuật

> Dành cho người đã quen cách làm ở Phần A, hoặc lập trình viên/AI cần tra nhanh. 📖 Nền tảng quy
> trình (đăng ký bảng, tạo form, tạo view) xem [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md)
> và [cau-hinh-man-quan-ly-view.md](cau-hinh-man-quan-ly-view.md). Tài liệu dưới đây chỉ nêu **phần
> KHÁC** của màn tham chiếu so với danh mục phẳng.

## Hai cách hiển thị TÊN khóa ngoại ở lưới (chọn 1)

| | **Cách A — FK auto-JOIN** (mặc định, no-code) | **Cách B — SQL View tay** (escape hatch) |
|---|---|---|
| Viết SQL View | ❌ không cần | ✅ phải viết view JOIN |
| Nguồn lưới (`Source_Type`) | `Table` (base table) | `View` |
| Đăng ký `Sys_Table` | 1 (table) | 2 (view + table) |
| Engine JOIN | **tự sinh** theo `Props_Json.fkLookup` của cột | JOIN nằm sẵn trong view |
| Lọc / sắp xếp / xuất theo tên | ✅ | ✅ |
| Khi nào dùng | hầu hết FK đơn (nguồn **table** + cột Value/Display **đơn**) | hiển thị **phức tạp**: ghép nhiều bảng, multi-hop, biểu thức `CONCAT(...)`, nguồn `custom_sql`/TVF |

> Cơ sở: [docs/spec/25_FK_LOOKUP_SPEC.md](../spec/25_FK_LOOKUP_SPEC.md) §5 · ADR-033. **Ưu tiên Cách A**; chỉ rơi về Cách B khi auto-JOIN không đủ.

---

## Cách A — FK auto-JOIN (khuyến nghị, KHÔNG cần view)

Engine tự sinh `LEFT JOIN` tới bảng cha theo **định nghĩa lookup của chính field FK trong form sửa** (`Ui_Field_Lookup`),
rồi đổi cột FK ở lưới thành **TÊN** (in-place). Không viết view, không đăng ký 2 đối tượng, không sửa code.

**Điều kiện dùng được:**
- Form sửa đã có **LookupBox** cho cột FK ở **chế độ Bảng/View** (`Query_Mode='table'`), cột Value/Display là **cột đơn**.
- Nếu Display là biểu thức (`CONCAT(...)`), hoặc nguồn `custom_sql`/TVF → auto-JOIN **bỏ qua** (hiện id thô) → dùng **Cách B**.

**Các bước cấu hình tay** — ví dụ `DM_ChiNhanhNganHang` (FK `NganHang_Id → DM_NganHang`):

1. **`Ui_View` đọc base table** — Tab Cơ bản: `Source_Type = Table`, `Source_Object` **để trống**, `Key_Field = Id`,
   `Edit_Form = DM_CHINHANHNGANHANG`. *(Không cần đăng ký view vào `Sys_Table`.)*
2. **Tìm `Field_Id`** của LookupBox FK trong form sửa (truy vấn bên dưới) — ví dụ `34`.
3. **Cột FK ở lưới** = chính cột Id (`NganHang_Id`): để **hiển thị** (`Is_Visible=1`), caption "Ngân hàng",
   và đặt **`Props_Json`** trỏ định nghĩa FK:

   ```json
   {"fkLookup":{"fieldId":34}}
   ```

   → Engine đọc `Ui_Field_Lookup` của field 34 (`Source_Name=DM_NganHang`, `Value_Column=Id`, `Display_Column=Ten`)
   và tự JOIN. Cột `NganHang_Id` hiện **tên** ngân hàng; lọc/sort/xuất đều theo tên.

   > **Đặt `fkLookup` ở đâu cũng được** (engine suy cột FK gốc từ `Field_Id` → `Sys_Column`):
   > - **Tại chỗ (Model 2):** đặt trên **chính cột `NganHang_Id`** → cột đó hiện tên (ví dụ trên).
   > - **Cột tên riêng (Model 1, giống `TenCha`):** thêm cột `TenNganHang` (`Column_Id=NULL`), đặt `fkLookup` trên nó,
   >   **ẩn** `NganHang_Id`. Engine JOIN theo `NganHang_Id`, hiện tên ở cột `TenNganHang`. *(Mẫu: [db/066](../../db/066_config_grid_chinhanhnganhang_autojoin.sql).)*

**Tìm `Field_Id` của LookupBox FK** (chạy trên Config DB):

```sql
SELECT fi.Field_Id, fi.Editor_Type, fl.Source_Name, fl.Value_Column, fl.Display_Column
FROM   dbo.Ui_Field        fi
JOIN   dbo.Ui_Form         fm ON fm.Form_Id  = fi.Form_Id
JOIN   dbo.Ui_Field_Lookup fl ON fl.Field_Id = fi.Field_Id
WHERE  fm.Form_Code = N'DM_CHINHANHNGANHANG';     -- form sửa của màn
```

**SQL tương đương** (tham chiếu — [db/066](../../db/066_config_grid_chinhanhnganhang_autojoin.sql)):

```sql
-- 1) Đọc base table (engine tự JOIN)
UPDATE dbo.Ui_View SET Source_Type=N'Table', Source_Object=NULL
WHERE  View_Code=N'Grid_DM_ChiNhanhNganHang';
-- 2) Cột NganHang_Id hiện TÊN — trỏ định nghĩa FK qua Props_Json
UPDATE dbo.Ui_View_Column SET Is_Visible=1, Render_Mode=N'Text',
       Props_Json=N'{"fkLookup":{"fieldId":34}}'
WHERE  View_Id=@ViewId AND Field_Name=N'NganHang_Id';
```

> ✅ **ConfigStudio** (tab **Cột**) có cột **"FK lookup (Field_Id)"** (cạnh cột Render): nhập `Field_Id` (vd `34`) →
> app tự ghi `{"fkLookup":{"fieldId":34}}` vào `Props_Json` khi Lưu. Để trống = cột thường. *(Không cần gõ JSON/SQL tay.)*

**Giới hạn Cách A (v1):** chỉ nguồn **table/view + cột Value/Display đơn**; phân quyền **dòng** chưa áp ở lưới
(để pha RLS). Cần hiển thị phức tạp → **Cách B** dưới đây.

---

## Cách B — Lưới tham chiếu qua SQL View (escape hatch)

> Phần còn lại của tài liệu (Yêu cầu trước, Bước 1–5, cascade, checklist) là quy trình **Cách B**.

## Khác biệt cốt lõi so với danh mục phẳng

| | Danh mục **phẳng** (DM_QuocGia) | Lưới **tham chiếu** (DM_TinhThanhPho) |
|---|---|---|
| Nguồn **lưới** | base table | **VIEW** (JOIN sẵn để có tên cha) |
| Đăng ký `Sys_Table` | 1 đối tượng (table) | **2 đối tượng**: VIEW (lưới) + TABLE (form) |
| Field trong form | TextBox/Number… | thêm **1 LookupBox** (FK → bảng cha) |
| Cột lưới | cột thật | hiện **`TenCha`** (từ view), **ẩn** cột Id FK |

Nguyên tắc Cách B: **lưới ĐỌC qua VIEW**, **form GHI vào BASE TABLE**. Engine không bao giờ INSERT/UPDATE vào view.
*(Với Cách A, lưới đọc thẳng base table — form vẫn GHI base table như nhau.)*

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

### Cách A — auto-JOIN (không view)
- [ ] Form sửa có **LookupBox** FK chế độ Bảng/View (`Query_Mode='table'`, cột Value/Display đơn).
- [ ] `Ui_View`: `Source_Type = Table`, `Source_Object` trống, `Key_Field = Id`, gắn `Edit_Form`.
- [ ] Cột Id FK: `Is_Visible=1`, caption tên cha, **`Props_Json = {"fkLookup":{"fieldId":<Field_Id>}}`**.
- [ ] Mở màn: cột FK hiện **tên**, lọc/sort/xuất theo tên chạy.

### Cách B — qua SQL View (escape hatch)
- [ ] View đọc đã tạo (LEFT JOIN cha, `IsDeleted = 0`).
- [ ] Bảng cha cấu hình xong **trước**.
- [ ] `Sys_Table`: đăng ký **VIEW** (lưới) **và** **TABLE** (form).
- [ ] Form nguồn = **base table**; field FK = **LookupBox** (Value = cột Id FK, Display = tên cha).
- [ ] View nguồn = **VIEW** (`Source_Type = View`); cột hiện **`TenCha`**, ẩn cột Id FK; `Key_Field = Id`; gắn `Edit_Form`.
- [ ] Route `/view/{View_Code}` khai trong menu → đồng bộ tenant.
- [ ] Mọi text người dùng thấy đều qua key i18n (nút 🌐).
