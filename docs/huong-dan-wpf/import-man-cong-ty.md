# Hướng dẫn cấu hình **Import Excel cho màn Công ty** (Tree_TC_CongTy)

> Bật và cấu hình tính năng **Import Excel** cho màn Công ty (`/view/Tree_TC_CongTy`). Đây là **bổ sung cấu hình**
> trên màn đã dựng theo [cau-hinh-man-cong-ty.md](cau-hinh-man-cong-ty.md); cơ chế import chung xem
> [huong-dan-import-excel.md](huong-dan-import-excel.md). Spec: [25](../spec/25_FK_LOOKUP_SPEC.md) §11–§14 · ADR-034.
>
> Đọc trước: [huong-dan-import-excel.md](huong-dan-import-excel.md) (trợ lý 3 bước, khóa ghép, làm mờ, hook).

---

## 0. Điều kiện & giới hạn (đọc kỹ trước khi cấu hình)

Màn Công ty là **lưới CÂY** (`TreeList`) đọc **view JOIN tay** (`vw_TC_CongTy`) — nên có 2 giới hạn ở **v1 (lưới phẳng)**:

1. **Import phẳng — công ty cha phải tồn tại TRƯỚC.**
   Cột *Công ty cha* nhập bằng **Mã** của công ty đã có trong DB. Hệ **chưa** sắp xếp cây trong 1 file (topo-sort để sau).
   → Nhập **công ty gốc trước** (để trống cột Cha), rồi mới nhập công ty con ở **mẻ sau** (điền Mã cha).

2. **Cột cascade (`Phường/Xã`, `Chi nhánh ngân hàng`) — mặc định KHÔNG import được.**
   Hai cột này lọc theo **field cha ảo** (Tỉnh/Thành, Ngân hàng); import **không có ngữ cảnh chọn cha** nên danh sách rỗng ⇒
   điền Mã sẽ **báo lỗi**. Hai lựa chọn:
   - **`Chi nhánh ngân hàng`:** nếu **mã chi nhánh duy nhất toàn cục** → bật **`Import_Global_Code=1`** cho field
     `ChiNhanhNganHang_Id` (tra Mã toàn bảng, bỏ lọc ngân hàng) + đặt `Code_Field=Ma` → **import được**. Xem
     [huong-dan-import-excel.md §5.5](huong-dan-import-excel.md).
   - **`Phường/Xã`:** mã xã thường **trùng giữa các tỉnh** ⇒ **không** bật global được (engine từ chối nếu trùng). **Để trống**
     khi import, cập nhật sau bằng nhập tay.

   *(Field ảo Tỉnh/Thành, Ngân hàng không lưu DB nên **không** xuất hiện trong template.)*

> Các cột **import bình thường**: `Ma`, `Ten`, `TenVietTat`, `MaSoThue`, `DiaChi`, `DienThoai`, `Email`, `Website`,
> `NguoiDaiDien`, `GiamDoc`, `KeToanTruong`, `SoTaiKhoan`, `TrangThai`, **`Cấp công ty` (Mã)**, **`Công ty cha` (Mã)**.

---

## 1. Cấu hình bắt buộc — cầu Mã cho 2 FK import được

Import đổi **Mã → Id** dựa vào cột Mã (`Code_Field`) khai trong **LookupBox của form `TC_CONGTY`**. Đảm bảo 2 field sau
có `Code_Field = Ma`:

**ConfigStudio → Forms → mở form `TC_CONGTY` → chọn field → tab Control Props → mục "Nguồn dữ liệu FK" →
ô "Cột Mã (Code_Field) — cho Import / template":**

| Field | Source_Name | Value / Display | **Cột Mã (Code_Field)** | Ghi chú |
|---|---|---|---|---|
| `CapCongTy_Id` (Cấp công ty) | `TC_CapCongTy` | `Id` / `Ten` | **`Ma`** | nhập Mã cấp công ty |
| `CongTy_Cha_Id` (Công ty cha) | `TC_CongTy` | `Id` / `Ten` | **`Ma`** | self-ref; nhập Mã công ty cha (phải đã có) |

- Ô **"Cột Mã (Code_Field)"** nằm ngay dưới *Sắp xếp (ORDER BY)*, **luôn hiện** (không phụ thuộc chế độ EditBox).
- Thiếu `Code_Field` ⇒ khi mở trợ lý import, cột đó báo *"chưa cấu hình Mã tham chiếu"*.
- **Không cần** đặt `Code_Field` cho `PhuongXa_Id` / `ChiNhanhNganHang_Id` (không import ở v1).

> SQL tương đương (nếu chỉnh thẳng Config DB):
> ```sql
> UPDATE fl SET fl.Code_Field = N'Ma'
> FROM Ui_Field_Lookup fl JOIN Ui_Field fi ON fi.Field_Id = fl.Field_Id
> JOIN Ui_Form fo ON fo.Form_Id = fi.Form_Id
> WHERE fo.Form_Code = N'TC_CONGTY' AND fi.Field_Code IN (N'CapCongTy_Id', N'CongTy_Cha_Id');
> ```

---

## 2. Tùy chọn — Upsert theo Mã (cập nhật công ty đã có)

Công ty có `Ma` **unique** → nên dùng làm khóa upsert. Khai **`Ui_View.Import_Key_Fields = Ma`** cho lưới cây:
```sql
UPDATE Ui_View SET Import_Key_Fields = N'Ma' WHERE View_Code = N'Tree_TC_CongTy';
```
- Import lần sau: dòng có `Ma` đã tồn tại → **cập nhật** (không tạo trùng); chưa có → **thêm mới**.
- Không khai ⇒ chỉ **thêm mới** (trùng `Ma` sẽ lỗi do unique index).

*(ConfigStudio chưa có ô riêng cho `Import_Key_Fields` → tạm cấu hình bằng SQL trên Config DB, rồi đồng bộ xuống tenant.)*

---

## 3. Tùy chọn — Làm mờ cột nhạy cảm trong log

Ví dụ ẩn **Số tài khoản** trong log import (giữ 4 số cuối):
```sql
UPDATE sc SET sc.Is_Log_Masked = 1, sc.Log_Mask_Mode = N'Partial'
FROM Sys_Column sc JOIN Sys_Table st ON st.Table_Id = sc.Table_Id
WHERE st.Table_Code = N'TC_CongTy' AND sc.Column_Code = N'SoTaiKhoan';
```
Log sẽ ghi `****1234` thay vì số thật; **dữ liệu ghi vào bảng vẫn nguyên**. Xem [huong-dan-import-excel.md §5.3](huong-dan-import-excel.md).

---

## 4. Tùy chọn — Hook sau import

Ví dụ **tính lại thứ tự cây** sau khi import 1 mẻ công ty: viết logic trong `sp_AfterImport_TC_CongTy` (dùng `@RecordIdsJson`).
Sinh skeleton: ConfigStudio → **Sys_Table** → chọn `TC_CongTy` → nút **"⚙ Sinh store"** (tạo cả `sp_AfterImport_TC_CongTy`).
Hook mỗi dòng (`sp_AfterSave_Grid_TC_CongTy`) nhận `@Source='IMPORT'` để phân biệt import với nhập tay. Xem
[huong-dan-import-excel.md §5.4](huong-dan-import-excel.md).

---

## 5. Thao tác import (người dùng)

Mở màn Công ty → **⬆ Import Excel**:

1. **Tải template** → điền:
   - `Ma`, `Ten` (bắt buộc); các thông tin khác.
   - **Cấp công ty**: chọn **Mã** từ dropdown (sheet phụ).
   - **Công ty cha**: **Mã** công ty cha *(để trống nếu là công ty gốc; công ty cha phải đã có trong hệ thống)*.
   - **Để trống** *Phường/Xã* và *Chi nhánh ngân hàng* (điền sau — xem §0).
   - `TrangThai`: nhập đúng **giá trị** hệ dùng (vd mã trạng thái).
2. **Kiểm tra** → xem preview (Thêm mới / Cập nhật / Lỗi).
3. **Xác nhận ghi** → lưới cây tự nạp lại.

**Nhập cây nhiều cấp:** mẻ 1 = công ty gốc (cột Cha trống) → mẻ 2 = công ty con (cột Cha = Mã gốc) → mẻ 3 = cháu…

---

## 6. Triển khai

1. Chạy migration import (1 lần): `db/071` (Config), `db/072` (Data DB `ICare247_Solution`), `db/073` (Config seed i18n).
2. Cấu hình §1 (và §2/§3 nếu dùng) → **App web › Quản trị › Đồng bộ cấu hình › Áp dụng từ master** (đưa `Code_Field`,
   `Import_Key_Fields` xuống tenant).
3. Rebuild + restart API · rebuild web + hard-reload.
4. (Nếu dùng hook) chạy `db/procs/sp_AfterSave_Grid_TC_CongTy.sql` + `sp_AfterImport_TC_CongTy.sql` trên Data DB.

---

## 7. Checklist

- [ ] `CapCongTy_Id`, `CongTy_Cha_Id` có `Code_Field = Ma` (LookupBox form `TC_CONGTY`)
- [ ] (tùy) `Ui_View.Import_Key_Fields = Ma` cho `Tree_TC_CongTy` (upsert theo Mã)
- [ ] (tùy) `Sys_Column.Is_Log_Masked` cho `SoTaiKhoan` / cột nhạy cảm
- [ ] Đồng bộ cấu hình xuống tenant + restart/rebuild
- [ ] Test: tải template → điền (bỏ trống Phường/Xã, Chi nhánh) → preview → commit → kiểm cây + `Sys_Import_Log`
- [ ] Nhập cây: gốc trước → con sau (Mã cha)

---

## 8. Lỗi thường gặp (riêng màn Công ty)

| Hiện tượng | Nguyên nhân | Xử lý |
|---|---|---|
| Cột **Cấp / Cha** báo *"chưa cấu hình Mã tham chiếu"* | Thiếu `Code_Field=Ma` | Cấu hình §1 + đồng bộ. |
| **Công ty cha** báo *mã không tồn tại* | Cha chưa có trong DB (import cùng file) | Nhập gốc/cha ở **mẻ trước**, con ở mẻ sau. |
| Điền **Phường/Xã / Chi nhánh** → mọi dòng lỗi *mã không tồn tại* | Cột cascade lọc theo cha (§0.2) | Chi nhánh: bật `Import_Global_Code` nếu mã unique. Phường/Xã: để trống, nhập sau. |
| Cột báo lỗi *Mã bị trùng nhiều bản ghi* | Bật `Import_Global_Code` nhưng Mã con **không** unique toàn cục | Tắt cờ cho field đó; nhập cột đó sau bằng tay. |
| `TrangThai` báo sai định dạng | Nhập nhãn thay vì giá trị hệ dùng | Nhập đúng mã/giá trị `Sys_Lookup`. |
| Trùng `Ma` khi import lại | Chưa bật upsert | Khai `Import_Key_Fields = Ma` (§2). |

---

*Liên quan:* [cau-hinh-man-cong-ty.md](cau-hinh-man-cong-ty.md) · [huong-dan-import-excel.md](huong-dan-import-excel.md) · [cau-hinh-field-ao-cascade.md](cau-hinh-field-ao-cascade.md) · spec [25](../spec/25_FK_LOOKUP_SPEC.md) · ADR-034.
