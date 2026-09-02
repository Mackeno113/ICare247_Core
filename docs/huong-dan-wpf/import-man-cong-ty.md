# Hướng dẫn cấu hình **Import Excel cho màn Công ty** (Tree_TC_CongTy)

> **Tài liệu này dành cho ai?** Người dùng cuối nhập dữ liệu Công ty hàng loạt (phần A) — **không
> cần biết lập trình**. Nếu bạn là người cấu hình hệ thống / lập trình viên / AI cần tra cứu kỹ
> thuật, đi thẳng xuống [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật).
>
> **Bài này dùng để làm gì?** Nhập hàng loạt dữ liệu **Công ty** (`/view/Tree_TC_CongTy`) từ file
> Excel, thay vì gõ tay từng công ty. Đây là **bổ sung riêng cho màn Công ty** trên cơ chế import
> chung — nếu bạn chưa đọc cơ chế chung, xem [huong-dan-import-excel.md](huong-dan-import-excel.md)
> trước (giải thích thế nào là template, xem trước, xác nhận ghi…). Màn Công ty đã được dựng theo
> [cau-hinh-man-cong-ty.md](cau-hinh-man-cong-ty.md).
> Spec kỹ thuật: [25](../spec/25_FK_LOOKUP_SPEC.md) §11–§14 · ADR-034.

---

## Vài thuật ngữ cần biết trước khi đọc

> Các thuật ngữ chung (template, xem trước, xác nhận ghi, upsert, khóa ghép…) đã giải thích ở
> [huong-dan-import-excel.md § Vài thuật ngữ cần biết](huong-dan-import-excel.md#vài-thuật-ngữ-cần-biết-trước-khi-đọc).
> Dưới đây chỉ thêm các thuật ngữ **riêng cho màn Công ty**:

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **Công ty gốc** | Công ty **không có công ty cha** — đứng đầu 1 nhánh trong sơ đồ tổ chức (cây). |
| **Công ty cha / công ty con** | Quan hệ cấp trên–cấp dưới giữa các công ty, hiển thị dạng **cây** trên màn hình (giống sơ đồ tổ chức). |
| **Nhập theo mẻ** | Vì công ty cha phải **có sẵn trong hệ thống trước** thì mới nhập được công ty con, nên với dữ liệu nhiều cấp bạn cần **nộp file nhiều lần** (mẻ 1: công ty gốc, mẻ 2: công ty con, mẻ 3: công ty cháu…), không nhập được hết trong 1 file duy nhất. |
| **Cột phụ thuộc theo cha** | 1 số cột (ví dụ *Phường/Xã*, *Chi nhánh ngân hàng*) chỉ hiện danh sách chọn được **sau khi** đã biết công ty thuộc Tỉnh/Thành hay Ngân hàng nào — khi nhập bằng Excel hệ **chưa biết "cha"** của các cột này nên **để trống**, bổ sung sau bằng cách sửa tay trên màn hình. |

---

## Phần A — Làm theo từng bước

### Trước khi bắt đầu

Màn Công ty hiển thị dạng **cây** (công ty cha – công ty con), nên có 2 điều cần lưu ý trước khi
nhập file, khác với các danh mục đơn giản khác:

1. **Công ty cha phải đã có trong hệ thống trước khi nhập công ty con.** Hệ thống **chưa** tự sắp
   xếp thứ tự cha-con trong 1 file — vì vậy:
   - Nhập **công ty gốc trước** (để trống cột *Công ty cha*).
   - Sau khi công ty gốc đã có trong hệ thống, mới nhập tiếp **công ty con** ở **file/mẻ sau**
     (điền đúng Mã công ty cha).
2. **2 cột sau đây để trống khi import, bổ sung sau bằng tay:**
   - **Phường/Xã**
   - **Chi nhánh ngân hàng**

   Lý do: 2 cột này chỉ hiện danh sách chọn được sau khi đã biết công ty thuộc Tỉnh/Thành hay Ngân
   hàng nào — mà file Excel không có cách khai báo "cha" đó, nên nếu điền sẽ luôn báo lỗi *"mã không
   tồn tại"*. Sau khi import xong, mở lại từng công ty trên màn hình để chọn 2 cột này bằng tay.

> Các cột còn lại nhập bình thường như hướng dẫn chung: `Ma`, `Ten`, `TenVietTat`, `MaSoThue`,
> `DiaChi`, `DienThoai`, `Email`, `Website`, `NguoiDaiDien`, `GiamDoc`, `KeToanTruong`,
> `SoTaiKhoan`, `TrangThai`, **Cấp công ty** (chọn Mã), **Công ty cha** (chọn Mã).

---

### Bước 1 — Tải mẫu & điền dữ liệu Công ty

**Mục đích:** chuẩn bị đúng file Excel cho riêng dữ liệu Công ty, tránh bị lỗi do điền nhầm 2 cột
cần để trống.

**Làm gì:**
1. Mở màn **Công ty** → bấm **⬆ Import Excel**.
2. Bấm **Tải template**, mở file vừa tải và điền:
   - `Ma`, `Ten` — **bắt buộc**.
   - **Cấp công ty**: chọn **Mã** từ dropdown (sheet phụ).
   - **Công ty cha**: nhập **Mã** công ty cha *(để trống nếu đây là công ty gốc; công ty cha phải
     đã có sẵn trong hệ thống — xem mục "Trước khi bắt đầu")*.
   - **Để trống** cột *Phường/Xã* và *Chi nhánh ngân hàng* (bổ sung sau bằng tay).
   - `TrangThai`: nhập đúng **giá trị** hệ thống đang dùng (không phải nhãn hiển thị) — nếu không
     chắc giá trị nào, hỏi người quản trị hệ thống.
   - Các cột thông tin khác điền bình thường.
3. Chọn tệp vừa điền → bấm **Kiểm tra**.

**Bạn sẽ thấy gì:** chuyển sang bước xem trước (Bước 2) với thống kê Thêm mới/Cập nhật/Lỗi.

**Lỗi thường gặp:**
- **Công ty cha** báo *"mã không tồn tại"* → công ty cha chưa có trong hệ thống; nhập công ty gốc
  hoặc cha ở **mẻ trước**, công ty con nhập ở mẻ sau.
- Điền **Phường/Xã** hoặc **Chi nhánh ngân hàng** → mọi dòng báo lỗi *"mã không tồn tại"* → để
  trống 2 cột này, bổ sung sau bằng tay trên màn hình (xem "Trước khi bắt đầu").
- `TrangThai` báo sai định dạng → đang nhập nhãn hiển thị thay vì đúng giá trị hệ dùng; hỏi người
  quản trị giá trị đúng.

---

### Bước 2 — Xem trước (Preview)

**Mục đích:** kiểm tra trước khi ghi thật — xem cách làm chung tại
[huong-dan-import-excel.md § Bước 2](huong-dan-import-excel.md#bước-2--xem-trước-preview).

**Làm gì:** đọc thống kê **Thêm mới / Cập nhật / Lỗi**; nếu có dòng lỗi, sửa lại file theo gợi ý ở
Bước 1 rồi quay lại chọn file.

**Bạn sẽ thấy gì:** nếu ổn, bấm **Xác nhận ghi** để qua Bước 3.

---

### Bước 3 — Xác nhận ghi

**Mục đích:** ghi chính thức các công ty hợp lệ vào hệ thống.

**Làm gì:** bấm **Xác nhận ghi**.

**Bạn sẽ thấy gì:** lưới cây Công ty **tự nạp lại**, hiển thị các công ty vừa nhập đúng vị trí
cha-con.

---

### Nhập cây nhiều cấp (nhiều mẻ)

Vì công ty cha phải có trước, dữ liệu công ty nhiều cấp cần nộp **nhiều lần**:

- **Mẻ 1** = công ty **gốc** (cột *Công ty cha* để trống).
- **Mẻ 2** = công ty **con** (cột *Công ty cha* = Mã công ty gốc vừa nhập ở mẻ 1).
- **Mẻ 3** = công ty **cháu** (cột *Công ty cha* = Mã công ty con ở mẻ 2)… cứ thế cho các cấp sâu
  hơn.

Sau khi hoàn tất tất cả các mẻ, mở lại từng công ty để bổ sung bằng tay **Phường/Xã** và **Chi
nhánh ngân hàng** nếu cần.

---

## Phần B — Tra cứu kỹ thuật

> Dành cho người đã quen cách làm ở Phần A, hoặc lập trình viên/AI cần tra nhanh cấu hình kỹ thuật.
> Nội dung dưới đây là bản kỹ thuật đầy đủ — không giải thích lại từ đầu.

### 1. Điều kiện & giới hạn kỹ thuật

Màn Công ty là **lưới CÂY** (`TreeList`) đọc **view JOIN tay** (`vw_TC_CongTy`) — nên có 2 giới hạn
ở **v1 (lưới phẳng)**:

1. **Import phẳng — công ty cha phải tồn tại TRƯỚC.**
   Cột *Công ty cha* nhập bằng **Mã** của công ty đã có trong DB. Hệ **chưa** sắp xếp cây trong 1
   file (topo-sort để sau).
   → Nhập **công ty gốc trước** (để trống cột Cha), rồi mới nhập công ty con ở **mẻ sau** (điền Mã
   cha).

2. **Cột cascade (`Phường/Xã`, `Chi nhánh ngân hàng`) — mặc định KHÔNG import được.**
   Hai cột này lọc theo **field cha ảo** (Tỉnh/Thành, Ngân hàng); import **không có ngữ cảnh chọn
   cha** nên danh sách con rỗng ⇒ điền Mã sẽ **báo lỗi**. Hai lựa chọn:
   - **`Chi nhánh ngân hàng`:** nếu **mã chi nhánh duy nhất toàn cục** → bật
     **`Import_Global_Code=1`** cho field `ChiNhanhNganHang_Id` (tra Mã toàn bảng, bỏ lọc ngân hàng)
     + đặt `Code_Field=Ma` → **import được**. Xem
     [huong-dan-import-excel.md §2.5](huong-dan-import-excel.md).
   - **`Phường/Xã`:** mã xã thường **trùng giữa các tỉnh** ⇒ **không** bật global được (engine từ
     chối nếu trùng). **Để trống** khi import, cập nhật sau bằng nhập tay.

   *(Field ảo Tỉnh/Thành, Ngân hàng không lưu DB nên **không** xuất hiện trong template.)*

> Các cột **import bình thường**: `Ma`, `Ten`, `TenVietTat`, `MaSoThue`, `DiaChi`, `DienThoai`,
> `Email`, `Website`, `NguoiDaiDien`, `GiamDoc`, `KeToanTruong`, `SoTaiKhoan`, `TrangThai`,
> **`Cấp công ty` (Mã)**, **`Công ty cha` (Mã)**.

### 2. Cấu hình bắt buộc — cầu Mã cho 2 FK import được

Import đổi **Mã → Id** dựa vào cột Mã (`Code_Field`) khai trong **LookupBox của form `TC_CONGTY`**.
Đảm bảo 2 field sau có `Code_Field = Ma`:

**ConfigStudio → Forms → mở form `TC_CONGTY` → chọn field → tab Control Props → mục "Nguồn dữ liệu
FK" → ô "Cột Mã (Code_Field) — cho Import / template":**

| Field | Source_Name | Value / Display | **Cột Mã (Code_Field)** | Ghi chú |
|---|---|---|---|---|
| `CapCongTy_Id` (Cấp công ty) | `TC_CapCongTy` | `Id` / `Ten` | **`Ma`** | nhập Mã cấp công ty |
| `CongTy_Cha_Id` (Công ty cha) | `TC_CongTy` | `Id` / `Ten` | **`Ma`** | self-ref; nhập Mã công ty cha (phải đã có) |

- Ô **"Cột Mã (Code_Field)"** nằm ngay dưới *Sắp xếp (ORDER BY)*, **luôn hiện** (không phụ thuộc
  chế độ EditBox).
- Thiếu `Code_Field` ⇒ khi mở trợ lý import, cột đó báo *"chưa cấu hình Mã tham chiếu"*.
- **Không cần** đặt `Code_Field` cho `PhuongXa_Id` / `ChiNhanhNganHang_Id` (không import ở v1).

> SQL tương đương (nếu chỉnh thẳng Config DB):
> ```sql
> UPDATE fl SET fl.Code_Field = N'Ma'
> FROM Ui_Field_Lookup fl JOIN Ui_Field fi ON fi.Field_Id = fl.Field_Id
> JOIN Ui_Form fo ON fo.Form_Id = fi.Form_Id
> WHERE fo.Form_Code = N'TC_CONGTY' AND fi.Field_Code IN (N'CapCongTy_Id', N'CongTy_Cha_Id');
> ```

### 3. Tùy chọn — Upsert theo Mã (cập nhật công ty đã có)

Công ty có `Ma` **unique** → nên dùng làm khóa upsert.
**ConfigStudio → Quản lý View → mở `Tree_TC_CongTy` → tab Cột → tick "Khóa trùng (import)" ở cột
`Ma`** → **Lưu**.

- Import lần sau: dòng có `Ma` đã tồn tại → **cập nhật** (không tạo trùng); chưa có → **thêm mới**.
- Không tick ⇒ chỉ **thêm mới** (trùng `Ma` sẽ lỗi do unique index).
- Muốn khóa ghép (vd theo công ty + mã) → tick nhiều cột.

### 4. Tùy chọn — Làm mờ cột nhạy cảm trong log

Ví dụ ẩn **Số tài khoản** trong log import (giữ 4 số cuối):
```sql
UPDATE sc SET sc.Is_Log_Masked = 1, sc.Log_Mask_Mode = N'Partial'
FROM Sys_Column sc JOIN Sys_Table st ON st.Table_Id = sc.Table_Id
WHERE st.Table_Code = N'TC_CongTy' AND sc.Column_Code = N'SoTaiKhoan';
```
Log sẽ ghi `****1234` thay vì số thật; **dữ liệu ghi vào bảng vẫn nguyên**. Xem
[huong-dan-import-excel.md §2.3](huong-dan-import-excel.md).

### 5. Tùy chọn — Hook sau import

Ví dụ **tính lại thứ tự cây** sau khi import 1 mẻ công ty: viết logic trong
`sp_AfterImport_TC_CongTy` (dùng `@RecordIdsJson`).
Sinh skeleton: ConfigStudio → **Sys_Table** → chọn `TC_CongTy` → nút **"⚙ Sinh store"** (tạo cả
`sp_AfterImport_TC_CongTy`).
Hook mỗi dòng (`sp_AfterSave_Grid_TC_CongTy`) nhận `@Source='IMPORT'` để phân biệt import với nhập
tay. Xem [huong-dan-import-excel.md §2.4](huong-dan-import-excel.md).

### 6. Triển khai

1. Chạy migration import (1 lần): `db/071` (Config), `db/072` (Data DB `ICare247_Solution`),
   `db/073` (Config seed i18n).
2. Cấu hình §2 (và §3/§4 nếu dùng) → **App web › Quản trị › Đồng bộ cấu hình › Áp dụng từ master**
   (đưa `Code_Field`, `Import_Key_Fields` xuống tenant).
3. Rebuild + restart API · rebuild web + hard-reload.
4. (Nếu dùng hook) chạy `db/procs/sp_AfterSave_Grid_TC_CongTy.sql` + `sp_AfterImport_TC_CongTy.sql`
   trên Data DB.

### 7. Checklist

- [ ] `CapCongTy_Id`, `CongTy_Cha_Id` có `Code_Field = Ma` (LookupBox form `TC_CONGTY`)
- [ ] (tùy) tick "Khóa trùng (import)" cột `Ma` (tab Cột) cho `Tree_TC_CongTy` (upsert theo Mã)
- [ ] (tùy) `Sys_Column.Is_Log_Masked` cho `SoTaiKhoan` / cột nhạy cảm
- [ ] Đồng bộ cấu hình xuống tenant + restart/rebuild
- [ ] Test: tải template → điền (bỏ trống Phường/Xã, Chi nhánh) → preview → commit → kiểm cây +
      `Sys_Import_Log`
- [ ] Nhập cây: gốc trước → con sau (Mã cha)

### 8. Lỗi thường gặp (riêng màn Công ty)

| Hiện tượng | Nguyên nhân | Xử lý |
|---|---|---|
| Cột **Cấp / Cha** báo *"chưa cấu hình Mã tham chiếu"* | Thiếu `Code_Field=Ma` | Cấu hình §2 + đồng bộ. |
| **Công ty cha** báo *mã không tồn tại* | Cha chưa có trong DB (import cùng file) | Nhập gốc/cha ở **mẻ trước**, con ở mẻ sau. |
| Điền **Phường/Xã / Chi nhánh** → mọi dòng lỗi *mã không tồn tại* | Cột cascade lọc theo cha (§1) | Chi nhánh: bật `Import_Global_Code` nếu mã unique. Phường/Xã: để trống, nhập sau. |
| Cột báo lỗi *Mã bị trùng nhiều bản ghi* | Bật `Import_Global_Code` nhưng Mã con **không** unique toàn cục | Tắt cờ cho field đó; nhập cột đó sau bằng tay. |
| `TrangThai` báo sai định dạng | Nhập nhãn thay vì giá trị hệ dùng | Nhập đúng mã/giá trị `Sys_Lookup`. |
| Trùng `Ma` khi import lại | Chưa bật upsert | Tick "Khóa trùng (import)" cột `Ma` (§3). |

---

*Liên quan:* [cau-hinh-man-cong-ty.md](cau-hinh-man-cong-ty.md) ·
[huong-dan-import-excel.md](huong-dan-import-excel.md) ·
[cau-hinh-field-ao-cascade.md](cau-hinh-field-ao-cascade.md) ·
spec [25](../spec/25_FK_LOOKUP_SPEC.md) · ADR-034.
