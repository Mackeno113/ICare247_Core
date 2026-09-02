# Hướng dẫn Import dữ liệu từ Excel (theo cấu hình lưới)

> **Tài liệu này dành cho ai?** Người dùng cuối (người trực tiếp bấm nút Import và nhập file Excel) —
> **không cần biết lập trình**. Nếu bạn là người cấu hình hệ thống / lập trình viên / AI cần tra cứu
> nhanh, đi thẳng xuống [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật).
>
> **Bài này dùng để làm gì?** Hướng dẫn cách **nhập hàng loạt dữ liệu** vào 1 màn danh sách (ví dụ
> danh mục Ngân hàng, Tỉnh/Thành, Công ty…) từ 1 file Excel `.xlsx`, thay vì phải gõ tay từng dòng.
> Hỗ trợ: tải **file mẫu** sẵn định dạng đúng, **kiểm tra trước khi ghi** (xem trước), **thêm mới hoặc
> cập nhật cùng lúc**, và báo lỗi rõ ràng theo từng dòng để bạn sửa lại.
>
> Phạm vi bài này: **lưới phẳng** (danh sách dạng bảng thường). Màn dạng **cây** (ví dụ Công ty) có
> thêm vài lưu ý riêng — xem [import-man-cong-ty.md](import-man-cong-ty.md).
> Spec kỹ thuật: [`docs/spec/25_FK_LOOKUP_SPEC.md`](../spec/25_FK_LOOKUP_SPEC.md) §11–§14 · ADR-034.

---

## Vài thuật ngữ cần biết trước khi đọc

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **Template (file mẫu)** | File Excel hệ thống tạo sẵn đúng định dạng (đúng tên cột, đúng thứ tự) — bạn tải về, điền dữ liệu vào, rồi nộp lại cho hệ thống. |
| **Khóa ngoại** | 1 cột trong file mà giá trị phải **liên kết tới 1 danh mục khác** (ví dụ cột "Ngân hàng" phải khớp với 1 ngân hàng đã có sẵn trong hệ thống). Bạn nhập bằng **Mã** (ví dụ `VCB`), không cần biết số Id nội bộ. |
| **Xem trước (Preview)** | Bước hệ thống **đọc thử** file bạn nộp và báo trước dòng nào hợp lệ, dòng nào lỗi — **chưa ghi gì vào dữ liệu thật**. |
| **Xác nhận ghi (Commit)** | Bước bấm nút để hệ thống **ghi thật** các dòng hợp lệ vào dữ liệu — chỉ làm sau khi đã xem trước và thấy ổn. |
| **Thêm mới + Cập nhật (Upsert)** | Nhập 1 file mà **vừa tạo dòng mới, vừa sửa dòng đã có** cùng lúc — hệ tự nhận biết dựa vào cột khóa (thường là cột Mã). |
| **Khóa ghép** | Khi nhiều cột **gộp lại** mới xác định được 1 dòng dữ liệu là duy nhất (ví dụ phải khớp cả "Công ty" + "Mã" mới biết là dòng nào). Việc này do người quản trị bật sẵn cho từng màn. |
| **Nhật ký import (Log)** | Bản ghi lại ai đã import file gì, vào lúc nào, kết quả ra sao — dùng để tra cứu khi cần kiểm tra lại. |

---

## Phần A — Làm theo từng bước

### Trước khi bắt đầu

- Nút **⬆ Import Excel** nằm trên thanh công cụ của màn danh sách, cạnh nút **+ Thêm mới**. Nếu
  không thấy nút này, có thể màn đó chưa được bật tính năng import — báo người quản trị hệ thống.
- Bạn cần được **cấp quyền Thêm/Sửa dữ liệu** của màn đó thì mới import được (giống như quyền để
  bấm Thêm mới/Sửa bằng tay).
- Cột nào là **khóa ngoại** (liên kết tới danh mục khác, ví dụ Ngân hàng, Tỉnh/Thành) thì trong file
  bạn **nhập bằng Mã**, không cần biết số Id nội bộ.

---

### Bước 1 — Tải file mẫu (template) & điền dữ liệu

**Mục đích:** chuẩn bị đúng file Excel đúng định dạng hệ thống cần, tránh mất công sửa đi sửa lại vì
sai cột.

**Làm gì:**
1. Mở màn danh sách cần nhập dữ liệu → bấm **⬆ Import Excel**.
2. Ở màn hình hiện ra, bấm **⬇ Tải template** để tải file Excel mẫu về máy.
3. Mở file vừa tải:
   - **Sheet chính**: mỗi cột là 1 trường cần nhập; tiêu đề in đậm, cột **bắt buộc** có dấu `*`; di
     chuột vào tiêu đề để xem ghi chú (kiểu dữ liệu, hoặc "Nhập Mã").
   - Cột nào là **khóa ngoại** sẽ có sẵn **1 sheet phụ** liệt kê Mã + Tên hợp lệ, và ô nhập của cột
     đó là **dropdown** — bấm vào ô, chọn đúng Mã trong danh sách, không tự gõ Tên.
4. Điền dữ liệu bắt đầu từ **dòng 2** (dòng 1 là tiêu đề — **đừng xóa hoặc sửa tiêu đề**).
5. Nếu màn của bạn có ô **Chế độ import** (xem bảng bên dưới), chọn đúng chế độ trước khi qua bước
   sau — ô này chỉ xuất hiện khi người quản trị đã bật sẵn cho màn đó.
6. Quay lại màn hình web, bấm **Chọn tệp** → chọn đúng file vừa điền → bấm **Kiểm tra**.

**Bạn sẽ thấy gì:** màn hình chuyển sang bước xem trước (Bước 2), hiển thị thống kê số dòng sẽ được
xử lý.

**Lỗi thường gặp:**
- Bấm **Kiểm tra** báo không đọc được file → file không phải `.xlsx` (không dùng `.xls` cũ), hoặc
  file quá nặng (giới hạn 20MB).
- Cột khóa ngoại không hiện dropdown, hoặc chọn Mã nào cũng báo lỗi → báo người quản trị hệ thống
  kiểm tra lại cấu hình cột đó (chưa khai "Cột Mã cho Import").

**Chế độ import** (chỉ chọn được nếu người quản trị đã bật khóa cho màn này):

| Chế độ | Mã đã có trong hệ thống | Mã chưa có |
|---|---|---|
| **Thêm mới + cập nhật (upsert)** | Cập nhật | Thêm mới (cần đủ trường bắt buộc) |
| **Chỉ cập nhật** | Cập nhật | **Từ chối** (báo Mã không tồn tại) |
| **Chỉ thêm mới** | **Từ chối** (báo Mã đã tồn tại) | Thêm mới |

> **Mẹo — chỉ cần sửa vài cột, không cần đủ cả file:** nếu chỉ muốn cập nhật một vài trường cho các
> dòng đã có (ví dụ chỉ sửa "Thứ tự"), bạn có thể tạo file chỉ gồm cột **Mã** + các cột cần sửa,
> chọn chế độ **Chỉ cập nhật**. Cột nào **không có trong file** thì dữ liệu cũ được **giữ nguyên**
> (không bị xóa); còn ô để trống trong 1 cột **có trong file** thì sẽ bị **ghi đè thành rỗng** — cẩn
> thận đừng để trống nhầm.

---

### Bước 2 — Xem trước (Preview)

**Mục đích:** hệ thống chỉ **đọc thử** file, **chưa ghi gì vào dữ liệu thật** — để bạn phát hiện và
sửa lỗi trước khi ghi thật, tránh phải sửa lại sau khi đã lỡ ghi.

**Làm gì:**
- Đọc các ô thống kê: **Thêm mới: x** · **Cập nhật: y** · **Lỗi: z** (dòng trống trong file được bỏ
  qua, không tính là lỗi).
- Nếu có dòng lỗi, xem **bảng dòng lỗi** — ghi rõ số dòng trong Excel + mô tả lỗi ở cột nào.
- Nếu cần sửa file, bấm **Quay lại** để chọn file khác, sửa xong thì lặp lại Bước 1.

**Bạn sẽ thấy gì:** nếu ổn, bấm **Xác nhận ghi (n)** để chuyển sang Bước 3. Nếu file lỗi hoàn toàn
(ví dụ không đọc được, thiếu cột bắt buộc) sẽ có banner đỏ hiện ở đầu trang thay vì bảng thống kê.

**Lỗi thường gặp trong bảng dòng lỗi** (mỗi ô tự động cắt khoảng trắng đầu/cuối trước khi kiểm tra):

| Loại lỗi | Ý nghĩa | Cách sửa |
|---|---|---|
| "mã không tồn tại / ngoài phạm vi" | Cột khóa ngoại nhập sai Mã, hoặc Mã đó ngoài quyền của bạn | Chọn lại đúng Mã trong dropdown ở sheet phụ |
| "sai định dạng" | Cột số/ngày (`dd/MM/yyyy`)/đúng-sai (`1/0`) nhập sai kiểu | Sửa đúng định dạng theo ghi chú tiêu đề |
| "là bắt buộc" | Cột có dấu `*` bị để trống | Điền giá trị cho ô đó |
| Trùng khóa trong cùng file | Nếu màn bật khóa ghép: 2 dòng trong file cùng trùng giá trị khóa | Gộp lại còn 1 dòng, hoặc sửa cho không trùng |

---

### Bước 3 — Xác nhận ghi & xem kết quả

**Mục đích:** ghi chính thức các dòng hợp lệ vào dữ liệu thật. Dòng lỗi **không bị mất** — chỉ bị bỏ
qua và trả lại để bạn sửa rồi import lại, không phải làm lại từ đầu.

**Làm gì:** bấm **Xác nhận ghi (n)**.

**Bạn sẽ thấy gì:**
- 1 trong 3 trạng thái: **Đã import thành công** / **Import một phần** (có dòng lỗi) / **Import
  thất bại**.
- Thống kê số dòng đã ghi thật + danh sách dòng lỗi còn lại (nếu có).
- Lưới danh sách phía sau **tự nạp lại** để bạn thấy ngay dữ liệu mới.
- Bấm **Import tiếp** nếu còn file khác cần nhập, hoặc **Đóng** để kết thúc.

**Lỗi thường gặp:** thấy **Import một phần** → không cần lo, các dòng đúng **đã được ghi**; chỉ cần
sửa lại đúng các dòng báo lỗi trong bảng, rồi nộp lại (có thể tạo file mới chỉ gồm các dòng đó) và
lặp lại từ Bước 1.

---

### Hiểu kết quả: 3 trạng thái của 1 dòng dữ liệu

| Trạng thái | Nghĩa |
|---|---|
| **Thêm mới** | Chưa có bản ghi nào khớp khóa → hệ sẽ tạo dòng mới. |
| **Cập nhật** | Đã có bản ghi khớp khóa (thường là khớp Mã) → hệ sẽ ghi đè dữ liệu cũ bằng dữ liệu mới (chỉ xảy ra khi màn có bật khóa ghép). |
| **Lỗi** | Dòng có ít nhất 1 lỗi → **không ghi** dòng này, phải sửa rồi import lại. |

> Nếu màn của bạn **không có** ô "Chế độ import" ở Bước 1 (nghĩa là quản trị chưa bật khóa ghép),
> mọi dòng hợp lệ đều được xử lý như **Thêm mới** — không thể dùng để cập nhật dữ liệu đã có.

---

## Phần B — Tra cứu kỹ thuật

> Dành cho người đã quen cách làm ở Phần A, hoặc lập trình viên/AI cần tra nhanh tên trường/cấu hình
> kỹ thuật. Nội dung dưới đây là bản kỹ thuật đầy đủ — không giải thích lại từ đầu.

### 1. Tổng quan

- Mỗi màn lưới (`/view/{ViewCode}`) có form Thêm/Sửa (`Edit_Form`) sẽ hiện nút **"⬆ Import Excel"**
  trên thanh công cụ (cạnh **+ Thêm mới**).
- Import ghi dữ liệu **qua đúng luồng lưu tay** (`SaveMasterDataCommand`) → nên **mọi ràng buộc,
  validation, audit** áp dụng y như khi nhập tay.
- Cột **khóa ngoại** (vd Ngân hàng, Tỉnh/Thành) trong file nhập bằng **Mã** (vd `VCB`), hệ tự đổi
  sang Id — người nhập **không cần biết Id**.

### 2. Cấu hình (người quản trị)

#### 2.1 Bắt buộc — cầu Mã ↔ Id cho cột khóa ngoại
Để import/template biết cột nào dùng Mã, mỗi khóa ngoại phải khai **cột Mã** trong định nghĩa lookup
(`Ui_Field_Lookup.Code_Field`).
- **ConfigStudio (WPF):** mở field LookupBox của **Edit_Form** → tab **Control Props** → mục
  **"Nguồn dữ liệu FK"** → ô **"Cột Mã (Code_Field) — cho Import / template"** → nhập `Ma` →
  **Lưu**. *(Ô này luôn hiện, không phụ thuộc chế độ EditBox.)*
- **Hoặc SQL trực tiếp** (ví dụ field Ngân hàng — Field 34):
  `UPDATE Ui_Field_Lookup SET Code_Field = N'Ma' WHERE Field_Id = 34;` (đã có trong `db/071`).
- Thiếu `Code_Field` ⇒ cột đó **không import/xuất template được** (báo lỗi cấu hình khi mở trợ lý).

#### 2.2 Tùy chọn — Upsert theo KHÓA GHÉP
**ConfigStudio → Quản lý View → mở View → tab Cột → tick cột "Khóa trùng (import)"** cho các cột làm
khóa (tick **nhiều cột = khóa ghép**, vd `Ma`; hoặc `CongTy_Id` + `Ma`) → **Lưu**.

- Hệ so khớp **sau khi đã đổi Mã→Id** (khóa FK so trên Id), chuẩn hóa **trim + không phân biệt
  hoa/thường**.
- Có bản ghi khớp → **UPDATE**; chưa có → **INSERT**.
- **Không tick cột nào ⇒ chỉ thêm mới** (insert-only, an toàn nhất).
- Cột khóa phải là **cột nhập được** (trùng tên field của form sửa); cột hiển thị thuần (vd tên đã
  JOIN) không tính.

#### 2.3 Tùy chọn — Làm mờ cột nhạy cảm trong log
Bật theo **cột** (dùng lại cho mọi màn). **ConfigStudio (WPF):** mở field map cột đó → tab **Cơ
bản** → thẻ **"🕶 Làm mờ trong log"** → bật + chọn **Kiểu** (Full/Partial/Hash) → **Lưu Field**.
*(Thẻ chỉ hiện với field map cột thật, không hiện với field ảo. Là thuộc tính cấp cột `Sys_Column`
→ áp cho mọi form/view dùng cột đó.)*

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
- **Không ảnh hưởng dữ liệu thật** ghi vào bảng đích, cũng **không ảnh hưởng hook** (hook nhận giá
  trị thật).

#### 2.4 Tùy chọn — Hook SQL
Xem [`docs/spec/18_SAVE_VALIDATION_HOOK_SPEC.md`](../spec/18_SAVE_VALIDATION_HOOK_SPEC.md) + spec
25 §12.

**a) Hook mỗi dòng — `sp_AfterSave_Grid_<Table>`** (đã có sẵn từ save hook)
- Vì import ghi qua `SaveMasterDataCommand`, proc này **tự chạy cho từng dòng import**, trong cùng
  transaction (lỗi → **rollback dòng đó**).
- Nhận: `@PayloadJson` (toàn bộ dữ liệu dòng) · `@NguoiDungID` (ai) · `@Id` (`0`=thêm mới,
  `>0`=cập nhật) · **`@Source`** (`'IMPORT'`/`'MANUAL'`) · **`@ImportSessionId'`** (phiên import).
- Ví dụ chỉ chạy khi import: `IF @Source = N'IMPORT' BEGIN ... END`.

**b) Hook sau cả mẻ — `sp_AfterImport_<Table>`** (mới)
- Chạy **1 lần** sau khi các dòng đã ghi xong. Nhận thống kê mẻ + `@RecordIdsJson` (mảng Id đã
  ghi) + `@ImportSessionId`.
- Lỗi ở đây **không** rollback dữ liệu đã ghi (chỉ ghi cảnh báo).

**Sinh skeleton hook:** ConfigStudio → màn **Quản lý bảng (Sys_Table)** → nút **"⚙ Sinh store"** →
tạo cả 3 proc (`spc_`, `sp_AfterSave_`, `sp_AfterImport_`) dạng rỗng, **không đè** proc đã sửa tay.
Viết logic bằng `ALTER PROCEDURE` trực tiếp trên Data DB.

> **Lưu ý contract v2:** với bảng bật import, proc `sp_AfterSave_` cần khai thêm `@Source` +
> `@ImportSessionId` (có DEFAULT). Save tay **không** truyền 2 tham số này nên proc cũ **không vỡ**;
> chỉ cần cập nhật proc cho bảng có import (dùng nút "Sinh store" hoặc regen).

#### 2.5 Tùy chọn — Import khóa ngoại **cascade** (resolve Mã toàn cục)

FK **lọc theo field cha** (cascade — vd Phường/Xã theo Tỉnh, Chi nhánh theo Ngân hàng) **mặc định
không import được**: import không có bước "chọn cha" nên danh sách con rỗng. Bật cờ
**`Import_Global_Code`** để khi import **bỏ lọc cha → tra Mã con trên toàn bảng**.

**ConfigStudio (WPF):** mở field FK → tab **Control Props** → mục **Nguồn dữ liệu FK** → tick
**"Import: resolve Mã toàn cục (bỏ lọc cha)"** (ngay dưới ô Cột Mã) → **Lưu Field**.

- **CHỈ bật khi Mã con DUY NHẤT toàn cục** (vd mã chi nhánh). Nếu Mã con **trùng** (nhiều Id) →
  engine **từ chối cả file** với lỗi `import.fk.ambiguous_code` (không đoán bừa). VD **Phường/Xã**
  thường trùng mã giữa các tỉnh ⇒ **không** hợp cờ này.
- Vẫn cần `Code_Field = Ma` (§2.1). Cần **db/074** đã chạy.

### 3. Log import

Ghi ở **Data DB** của tenant:
- **`Sys_Import_Log`** (mỗi mẻ): ai import, tên file + hash, chế độ (insert/upsert), số
  thêm/sửa/lỗi, trạng thái, thời lượng, `Correlation_Id` (truy log server).
- **`Sys_Import_Log_Detail`** (chỉ **dòng lỗi**): số dòng, loại thao tác, `Error_Key`/args, tên cột
  lỗi, và `Row_Json` (đã làm mờ) — dùng để tra và sửa.

Dòng **thành công** không ghi vào Detail (đã có audit-log JSON-diff riêng của hệ).

### 4. Triển khai (checklist deploy)

1. Chạy migration (Config DB trừ 072):
   - `db/071` — masking (`Is_Log_Masked`/`Log_Mask_Mode`) + set `Code_Field`. `db/072` (Data DB) —
     2 bảng log.
   - `db/073` — thông báo lỗi `import.*` (vi/en). `db/074` — `Import_Global_Code`. `db/075` —
     `Ui_View_Column.Is_Import_Key` (khóa ghép). `db/076` — thông báo chế độ import.
2. (Nếu dùng hook) chạy lại `db/procs/sp_AfterSave_Grid_<Table>.sql` (contract v2) +
   `db/procs/sp_AfterImport_<Table>.sql` trên Data DB.
3. Rebuild + **restart API**; rebuild web + **hard-reload** trình duyệt; rebuild ConfigStudio.
4. Cấu hình trên ConfigStudio: `Code_Field`, tick "Khóa trùng (import)" (upsert), "Làm mờ trong
   log", "resolve Mã toàn cục".

### 5. Lỗi thường gặp

| Hiện tượng | Nguyên nhân | Cách xử lý |
|---|---|---|
| Không thấy nút **Import Excel** | Màn không có `Edit_Form` hoặc tắt Thêm | Gắn Edit_Form cho View; bật `Allow_Add`. |
| *"Màn này chưa có form Thêm/Sửa để import"* | `Ui_View.Edit_Form_Id` rỗng | Cấu hình Edit_Form cho View. |
| Cột khóa ngoại báo *"chưa cấu hình Mã tham chiếu"* | Thiếu `Ui_Field_Lookup.Code_Field` | Khai cột Mã (§2.1). |
| Mọi dòng khóa ngoại **lỗi mã không tồn tại** | Nhập **Tên** thay vì **Mã**, hoặc Mã ngoài phạm vi quyền | Dùng dropdown trong template (nhập Mã); kiểm quyền/`Filter_Sql`. |
| Muốn cập nhật nhưng luôn ra **Thêm mới** | Chưa tick cột "Khóa trùng (import)" | Tick cột khóa ở tab Cột (§2.2). |
| *"Bạn không có quyền thêm/cập nhật dữ liệu"* | Thiếu quyền Form.Thêm / Form.Sửa | Cấp quyền cho vai trò. |
| Import xong nhưng **hook không chạy** | Proc chưa tồn tại trên Data DB | Sinh + deploy proc (§2.4). |
| Save tay lỗi *"too many arguments"* sau khi bật import | Proc `sp_AfterSave_` chưa nâng v2 nhưng bị gọi kèm `@Source` | Không xảy ra với save tay (engine không truyền); nếu gặp khi import → regen proc v2. |

### 6. Giới hạn v1

- Chỉ **lưới phẳng** (chưa hỗ trợ TreeGrid — nhập cây theo Mã cha sẽ làm sau).
- **Cập nhật một phần cột / chế độ import cần bật khóa** (cột "Khóa trùng (import)"); không bật
  khóa ⇒ chỉ thêm mới, phải đủ cột.
- File `.xlsx` (không đọc `.xls` cũ); giới hạn 20MB.
- Preview & commit **tải file 2 lần** (dry-run + ghi) — chấp nhận cho dữ liệu danh mục.
- Khóa ngoại resolve theo **cột trùng tên** giữa View và Edit_Form (trường hợp FK in-place phổ
  biến).

---

*Liên quan:* [cau-hinh-luoi-tham-chieu.md](cau-hinh-luoi-tham-chieu.md) (FK auto-JOIN) ·
[cau-hinh-lookupbox.md](cau-hinh-lookupbox.md) ·
spec [25](../spec/25_FK_LOOKUP_SPEC.md)/[18](../spec/18_SAVE_VALIDATION_HOOK_SPEC.md) ·
ADR-034/033/029.
