# Hướng dẫn Import dữ liệu từ Excel (theo cấu hình lưới)

> Nhập hàng loạt dữ liệu vào bảng nghiệp vụ từ file Excel `.xlsx`, dựa trên cấu hình `Ui_View` của một màn lưới.
> Hỗ trợ: xuất **template mẫu** (ô khóa ngoại là dropdown chọn Mã), **kiểm tra trước khi ghi** (preview), **thêm mới hoặc cập nhật** (upsert theo khóa ghép), **ghi log** + **làm mờ cột nhạy cảm**, và **hook SQL** sau mỗi dòng / sau cả mẻ.
>
> Đối tượng đọc: **người dùng cuối** (phần 1–4) và **người cấu hình / quản trị** (phần 5 trở đi).
> Spec kỹ thuật: [`docs/spec/25_FK_LOOKUP_SPEC.md`](../spec/25_FK_LOOKUP_SPEC.md) §11–§14 · ADR-034. Phạm vi v1: **lưới phẳng** (TreeGrid làm sau).

---

## 1. Tổng quan

- Mỗi màn lưới (`/view/{ViewCode}`) có form Thêm/Sửa (`Edit_Form`) sẽ hiện nút **"⬆ Import Excel"** trên thanh công cụ (cạnh **+ Thêm mới**).
- Import ghi dữ liệu **qua đúng luồng lưu tay** (`SaveMasterDataCommand`) → nên **mọi ràng buộc, validation, audit** áp dụng y như khi nhập tay.
- Cột **khóa ngoại** (vd Ngân hàng, Tỉnh/Thành) trong file nhập bằng **Mã** (vd `VCB`), hệ tự đổi sang Id — người nhập **không cần biết Id**.

---

## 2. Các bước sử dụng (người dùng cuối)

Bấm **⬆ Import Excel** → mở trợ lý 3 bước:

### Bước 1 — Tải template & chọn file
1. Bấm **⬇ Tải template** → tải file `.xlsx` mẫu:
   - **Sheet chính**: mỗi cột là 1 trường cần nhập; tiêu đề in đậm, cột **bắt buộc** có dấu `*`; di chuột vào tiêu đề để xem ghi chú (kiểu dữ liệu / "Nhập Mã").
   - **Mỗi cột khóa ngoại có 1 sheet phụ** liệt kê `{Mã, Tên}` hợp lệ (đã lọc theo quyền của bạn); ô nhập cột đó là **dropdown** chọn Mã.
2. Điền dữ liệu vào sheet chính (bắt đầu từ **dòng 2**; dòng 1 là tiêu đề — **đừng xóa/sửa tiêu đề**).
3. Bấm **Chọn tệp** → chọn file đã điền.
4. Bấm **Kiểm tra**.

### Bước 2 — Xem trước (preview)
Hệ **chưa ghi gì**, chỉ phân tích và hiển thị:
- **Chip thống kê**: `Thêm mới: x` · `Cập nhật: y` · `Lỗi: z` · (Bỏ qua: dòng trống).
- **Bảng dòng lỗi**: số dòng Excel + mô tả lỗi (tô theo cột) để bạn sửa file.
- Nếu có **lỗi tệp** (thiếu cột bắt buộc, không đọc được…) → banner đỏ ở đầu.

Bạn có thể **Quay lại** để chọn/sửa file khác, hoặc bấm **Xác nhận ghi (n)** để ghi.

### Bước 3 — Kết quả
- Trạng thái: **Đã import thành công** / **Import một phần (có dòng lỗi)** / **Import thất bại**.
- Thống kê thực ghi + danh sách dòng lỗi (nếu còn).
- Lưới phía sau **tự nạp lại** để thấy dữ liệu mới. Bấm **Import tiếp** hoặc **Đóng**.

> **Ghi từng phần (partial commit):** các dòng hợp lệ **được ghi**, dòng lỗi **bị bỏ qua** và trả về để bạn sửa rồi import lại — không phải làm lại từ đầu.

---

## 3. Quy tắc dữ liệu trong file

| Quy tắc | Chi tiết |
|---|---|
| **Cắt khoảng trắng** | Mọi ô tự động **trim** đầu/cuối trước khi kiểm tra. `" VCB "` = `"VCB"`. |
| **Dòng trống** | Bỏ qua hoàn toàn (không tính lỗi). |
| **Cột khóa ngoại** | Nhập **Mã** (chọn từ dropdown). Mã không có trong danh sách (sai hoặc ngoài quyền) → lỗi *"mã không tồn tại / ngoài phạm vi"*. |
| **Kiểu dữ liệu** | Số / ngày (`dd/MM/yyyy`) / đúng-sai (`1/0`) sai định dạng → lỗi *"sai định dạng"*. |
| **Bắt buộc** | Cột `*` để trống → lỗi *"là bắt buộc"*. |
| **Trùng khóa** | Nếu màn bật **khóa ghép** (xem §5.2): trùng trong cùng file → lỗi; trùng với dữ liệu đã có → **cập nhật** (không tạo trùng). |

---

## 4. Hiểu 3 trạng thái dòng

| Trạng thái | Nghĩa |
|---|---|
| **Thêm mới (NEW)** | Chưa có bản ghi khớp khóa → sẽ **INSERT**. |
| **Cập nhật (UPDATE)** | Đã có bản ghi khớp khóa ghép → sẽ **UPDATE đè** (chỉ khi màn bật khóa ghép). |
| **Lỗi (ERROR)** | Có ít nhất 1 lỗi → **không ghi** dòng này. |

> Không bật khóa ghép ⇒ mọi dòng là **Thêm mới** (insert-only).

---

## 5. Cấu hình (người quản trị)

### 5.1 Bắt buộc — cầu Mã ↔ Id cho cột khóa ngoại
Để import/template biết cột nào dùng Mã, mỗi khóa ngoại phải khai **cột Mã** trong định nghĩa lookup (`Ui_Field_Lookup.Code_Field`).
- **ConfigStudio (WPF):** mở field LookupBox của **Edit_Form** → tab **Control Props** → mục **"Nguồn dữ liệu FK"** →
  ô **"Cột Mã (Code_Field) — cho Import / template"** → nhập `Ma` → **Lưu**. *(Ô này luôn hiện, không phụ thuộc chế độ EditBox.)*
- **Hoặc SQL trực tiếp** (ví dụ field Ngân hàng — Field 34): `UPDATE Ui_Field_Lookup SET Code_Field = N'Ma' WHERE Field_Id = 34;` (đã có trong `db/071`).
- Thiếu `Code_Field` ⇒ cột đó **không import/xuất template được** (báo lỗi cấu hình khi mở trợ lý).

### 5.2 Tùy chọn — Upsert theo KHÓA GHÉP
Khai **`Ui_View.Import_Key_Fields`** = danh sách field-code phân tách dấu phẩy (CSV), có thể gồm **cả cột khóa ngoại**:
```sql
UPDATE Ui_View SET Import_Key_Fields = N'CongTy_Id,Ma' WHERE View_Code = N'...';
```
- Hệ so khớp **sau khi đã đổi Mã→Id** (khóa FK so trên Id), chuẩn hóa **trim + không phân biệt hoa/thường**.
- Có bản ghi khớp → **UPDATE**; chưa có → **INSERT**.
- **Rỗng/không khai ⇒ chỉ thêm mới** (insert-only, an toàn nhất).

### 5.3 Tùy chọn — Làm mờ cột nhạy cảm trong log
Bật theo **cột** (dùng lại cho mọi màn). **ConfigStudio (WPF):** mở field map cột đó → tab **Cơ bản** →
thẻ **"🕶 Làm mờ trong log"** → bật + chọn **Kiểu** (Full/Partial/Hash) → **Lưu Field**. *(Thẻ chỉ hiện với field
map cột thật, không hiện với field ảo. Là thuộc tính cấp cột `Sys_Column` → áp cho mọi form/view dùng cột đó.)*

Hoặc **SQL**: `Sys_Column.Is_Log_Masked = 1` + `Log_Mask_Mode`:

| `Log_Mask_Mode` | Kết quả trong log | Dùng khi |
|---|---|---|
| `Full` (mặc định) | `***` | Ẩn hoàn toàn (tiền lương) |
| `Partial` | `****1234` (giữ 4 ký tự cuối) | Số tài khoản |
| `Hash` | `sha256:9f2a…` | So trùng mà không lộ giá trị |

```sql
UPDATE Sys_Column SET Is_Log_Masked = 1, Log_Mask_Mode = N'Full'
WHERE Table_Id = <id> AND Column_Code = N'TienLuong';
```
- Làm mờ **trước khi ghi log** — giá trị thật **không bao giờ** vào bảng `Sys_Import_Log_Detail`.
- **Không ảnh hưởng dữ liệu thật** ghi vào bảng đích, cũng **không ảnh hưởng hook** (hook nhận giá trị thật).

### 5.4 Tùy chọn — Hook SQL
Xem [`docs/spec/18_SAVE_VALIDATION_HOOK_SPEC.md`](../spec/18_SAVE_VALIDATION_HOOK_SPEC.md) + spec 25 §12.

**a) Hook mỗi dòng — `sp_AfterSave_Grid_<Table>`** (đã có sẵn từ save hook)
- Vì import ghi qua `SaveMasterDataCommand`, proc này **tự chạy cho từng dòng import**, trong cùng transaction (lỗi → **rollback dòng đó**).
- Nhận: `@PayloadJson` (toàn bộ dữ liệu dòng) · `@NguoiDungID` (ai) · `@Id` (`0`=thêm mới, `>0`=cập nhật) · **`@Source`** (`'IMPORT'`/`'MANUAL'`) · **`@ImportSessionId'`** (phiên import).
- Ví dụ chỉ chạy khi import: `IF @Source = N'IMPORT' BEGIN ... END`.

**b) Hook sau cả mẻ — `sp_AfterImport_<Table>`** (mới)
- Chạy **1 lần** sau khi các dòng đã ghi xong. Nhận thống kê mẻ + `@RecordIdsJson` (mảng Id đã ghi) + `@ImportSessionId`.
- Lỗi ở đây **không** rollback dữ liệu đã ghi (chỉ ghi cảnh báo).

**Sinh skeleton hook:** ConfigStudio → màn **Quản lý bảng (Sys_Table)** → nút **"⚙ Sinh store"** → tạo cả 3 proc (`spc_`, `sp_AfterSave_`, `sp_AfterImport_`) dạng rỗng, **không đè** proc đã sửa tay. Viết logic bằng `ALTER PROCEDURE` trực tiếp trên Data DB.

> **Lưu ý contract v2:** với bảng bật import, proc `sp_AfterSave_` cần khai thêm `@Source` + `@ImportSessionId` (có DEFAULT). Save tay **không** truyền 2 tham số này nên proc cũ **không vỡ**; chỉ cần cập nhật proc cho bảng có import (dùng nút "Sinh store" hoặc regen).

---

### 5.5 Tùy chọn — Import khóa ngoại **cascade** (resolve Mã toàn cục)

FK **lọc theo field cha** (cascade — vd Phường/Xã theo Tỉnh, Chi nhánh theo Ngân hàng) **mặc định không import được**:
import không có bước "chọn cha" nên danh sách con rỗng ([xem giải thích](huong-dan-import-excel.md)). Bật cờ
**`Import_Global_Code`** để khi import **bỏ lọc cha → tra Mã con trên toàn bảng**.

**ConfigStudio (WPF):** mở field FK → tab **Control Props** → mục **Nguồn dữ liệu FK** → tick
**"Import: resolve Mã toàn cục (bỏ lọc cha)"** (ngay dưới ô Cột Mã) → **Lưu Field**.

- **CHỈ bật khi Mã con DUY NHẤT toàn cục** (vd mã chi nhánh). Nếu Mã con **trùng** (nhiều Id) → engine **từ chối cả file**
  với lỗi `import.fk.ambiguous_code` (không đoán bừa). VD **Phường/Xã** thường trùng mã giữa các tỉnh ⇒ **không** hợp cờ này.
- Vẫn cần `Code_Field = Ma` (§5.1). Cần **db/074** đã chạy.

## 6. Log import

Ghi ở **Data DB** của tenant:
- **`Sys_Import_Log`** (mỗi mẻ): ai import, tên file + hash, chế độ (insert/upsert), số thêm/sửa/lỗi, trạng thái, thời lượng, `Correlation_Id` (truy log server).
- **`Sys_Import_Log_Detail`** (chỉ **dòng lỗi**): số dòng, loại thao tác, `Error_Key`/args, tên cột lỗi, và `Row_Json` (đã làm mờ) — dùng để tra và sửa.

Dòng **thành công** không ghi vào Detail (đã có audit-log JSON-diff riêng của hệ).

---

## 7. Triển khai (checklist deploy)

1. Chạy migration:
   - `db/071_import_config_key_masking.sql` (Config DB) — cột `Import_Key_Fields`, `Is_Log_Masked`/`Log_Mask_Mode`, set `Code_Field` field khóa ngoại.
   - `db/072_create_sys_import_log.sql` (Data DB) — 2 bảng log.
   - `db/073_seed_import_messages.sql` (Config DB) — thông báo lỗi `import.*` (vi/en).
2. (Nếu dùng hook) chạy lại `db/procs/sp_AfterSave_Grid_<Table>.sql` (contract v2) + `db/procs/sp_AfterImport_<Table>.sql` trên Data DB.
3. Rebuild + **restart API**; rebuild web + **hard-reload** trình duyệt; rebuild ConfigStudio (nếu dùng nút Sinh store).
4. (Tùy chọn) cấu hình `Import_Key_Fields` (upsert) và `Is_Log_Masked` (làm mờ).

---

## 8. Lỗi thường gặp

| Hiện tượng | Nguyên nhân | Cách xử lý |
|---|---|---|
| Không thấy nút **Import Excel** | Màn không có `Edit_Form` hoặc tắt Thêm | Gắn Edit_Form cho View; bật `Allow_Add`. |
| *"Màn này chưa có form Thêm/Sửa để import"* | `Ui_View.Edit_Form_Id` rỗng | Cấu hình Edit_Form cho View. |
| Cột khóa ngoại báo *"chưa cấu hình Mã tham chiếu"* | Thiếu `Ui_Field_Lookup.Code_Field` | Khai cột Mã (§5.1). |
| Mọi dòng khóa ngoại **lỗi mã không tồn tại** | Nhập **Tên** thay vì **Mã**, hoặc Mã ngoài phạm vi quyền | Dùng dropdown trong template (nhập Mã); kiểm quyền/`Filter_Sql`. |
| Muốn cập nhật nhưng luôn ra **Thêm mới** | Chưa khai `Import_Key_Fields` | Khai khóa ghép (§5.2). |
| *"Bạn không có quyền thêm/cập nhật dữ liệu"* | Thiếu quyền Form.Thêm / Form.Sửa | Cấp quyền cho vai trò. |
| Import xong nhưng **hook không chạy** | Proc chưa tồn tại trên Data DB | Sinh + deploy proc (§5.4). |
| Save tay lỗi *"too many arguments"* sau khi bật import | Proc `sp_AfterSave_` chưa nâng v2 nhưng bị gọi kèm `@Source` | Không xảy ra với save tay (engine không truyền); nếu gặp khi import → regen proc v2. |

---

## 9. Giới hạn v1

- Chỉ **lưới phẳng** (chưa hỗ trợ TreeGrid — nhập cây theo Mã cha sẽ làm sau).
- File `.xlsx` (không đọc `.xls` cũ); giới hạn 20MB.
- Preview & commit **tải file 2 lần** (dry-run + ghi) — chấp nhận cho dữ liệu danh mục.
- Khóa ngoại resolve theo **cột trùng tên** giữa View và Edit_Form (trường hợp FK in-place phổ biến).

---

*Liên quan:* [cau-hinh-luoi-tham-chieu.md](cau-hinh-luoi-tham-chieu.md) (FK auto-JOIN) · [cau-hinh-lookupbox.md](cau-hinh-lookupbox.md) · spec [25](../spec/25_FK_LOOKUP_SPEC.md)/[18](../spec/18_SAVE_VALIDATION_HOOK_SPEC.md) · ADR-034/033/029.
