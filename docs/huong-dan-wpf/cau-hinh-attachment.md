# Hướng dẫn cấu hình **AttachmentBox** — đính kèm / upload tệp (ConfigStudio)

> **Tài liệu này dành cho ai?** Người cấu hình form trong ConfigStudio (Admin, Business Analyst, IT
> triển khai) — không cần biết lập trình. Nếu bạn là lập trình viên/AI cần tra cứu nhanh, đi thẳng
> xuống [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật).
>
> **Bài này dùng để làm gì?** Bật control **đính kèm tệp** (ảnh, PDF, Office…) cho 1 field trên
> form — người dùng chọn tệp, xem tiến trình tải lên, xem thumbnail ảnh, tải về, xóa. Control **tự
> chuyển giữa 2 chế độ** tùy cách bạn khai field, bạn không cần chọn tay chế độ nào.
>
> Ví dụ xuyên suốt cả bài: field **"Tài liệu hợp đồng"** (cần nhiều tệp) và field **"Logo công ty"**
> (chỉ cần đúng 1 tệp).

---

## Vài thuật ngữ cần biết trước khi đọc

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **AttachmentBox** | Tên control (kiểu ô nhập liệu) cho phép đính kèm/tải tệp lên, gắn với 1 field trên form. |
| **Field ảo (IsVirtual)** | Field chỉ tồn tại trên giao diện, **KHÔNG** gắn với 1 cột cụ thể trong bảng dữ liệu — dùng khi 1 field cần chứa **nhiều** giá trị (ở đây là nhiều tệp) mà 1 cột không chứa đủ. |
| **Editor Type** | Kiểu ô nhập liệu hiển thị cho field (VD TextBox, AttachmentBox…) — chọn trong ConfigStudio, không cần biết lập trình. |
| **Control Props** | Vài tùy chọn riêng của từng loại control (VD phân loại tệp) — phần lớn trường hợp để mặc định là dùng được ngay. |
| **Publish** | Thao tác lưu + đưa cấu hình field/form ra để hệ thống thật áp dụng. |

---

## Phần A — Làm theo từng bước

### Chuẩn bị trước khi bắt đầu

Trước khi cấu hình, cần biết (thường IT/vận hành đã chuẩn bị sẵn 1 lần cho toàn hệ thống, không lặp
lại mỗi form):

1. Đã chạy cập nhật cấu trúc cơ sở dữ liệu để tạo nơi lưu tệp trên Data DB của đơn vị bạn.
2. Đã cấu hình nơi lưu tệp (dung lượng tối đa, loại tệp cho phép…) — mặc định hệ thống đã chạy được
   ngay, không cần chỉnh gì thêm.

Nếu đây là lần đầu đơn vị bạn dùng tính năng đính kèm tệp, hỏi IT xác nhận đã làm 2 việc trên chưa
(chi tiết kỹ thuật xem [Phần B §5](#5-dieu-kien-he-thong-mot-lan-do-dev-van-hanh)).

---

### Bước 1 — Quyết định: field này cần nhiều tệp hay chỉ 1 tệp?

**Mục đích:** AttachmentBox tự đổi cách hoạt động dựa theo bạn khai field kiểu nào — chọn đúng ngay
từ đầu để đỡ phải sửa lại.

**Làm gì:**
- Nếu field cần chứa **nhiều tệp** (VD "Tài liệu hợp đồng", "Ảnh sản phẩm") → làm theo **Bước 2**
  (chế độ Đa tệp).
- Nếu field chỉ cần **đúng 1 tệp** và bảng dữ liệu đã có sẵn 1 cột kiểu số (`int`/`bigint`) để lưu
  (VD `Logo_Id`) → làm theo **Bước 3** (chế độ Một tệp).

**Bạn sẽ thấy gì:** xác định được 1 trong 2 hướng cấu hình ở dưới.

**Lỗi thường gặp:** chọn nhầm chế độ Một tệp cho field cần nhiều tệp → hệ thống chỉ giữ được **tệp
cuối cùng** upload, các tệp trước tự mất — xem Bước 2 để làm đúng chế độ Đa tệp.

---

### Bước 2 — Cấu hình chế độ Đa tệp (VD field "Tài liệu hợp đồng")

**Mục đích:** cho phép người dùng đính kèm **nhiều tệp** vào field này (hợp đồng, ảnh sản phẩm…).

**Làm gì:**
1. Mở form → **thêm field mới**.
2. Bật **IsVirtual = true** (field ảo, không map cột DB).
3. Đặt **Field Code** (VD `TaiLieu`).
4. Chọn **Editor Type = `AttachmentBox`**.
5. (Tùy chọn) đặt **Loại tệp** qua Control Props nếu muốn phân loại — xem
   [Phần B §3](#3-control-props-đều-tùy-chọn); phần lớn trường hợp bỏ qua, để mặc định.
6. **Lưu form** → Publish.

**Bạn sẽ thấy gì:** field hiện ra dạng khung đính kèm, cho chọn nhiều tệp, xem tiến trình upload,
xem thumbnail ảnh, tải về, xóa từng tệp.

**Lỗi thường gặp:** bấm Thêm mới (bản ghi **chưa lưu**) mà vẫn upload được tệp — đây là đúng hành
vi: tệp ở trạng thái "chờ gắn" và **tự gắn vào bản ghi** sau khi bạn bấm Lưu; nếu bấm Hủy (không
lưu), tệp treo sẽ tự được job dọn xử lý, không phải lỗi.

---

### Bước 3 — Cấu hình chế độ Một tệp (VD field "Logo công ty")

**Mục đích:** lưu đúng **1 tệp** thẳng vào **1 cột** của bảng (giống `Logo_Id`) — dùng cho logo,
avatar, ảnh đại diện.

**Chuẩn bị cột:** bảng phải có sẵn **1 cột kiểu `int`/`bigint`** để chứa Id tệp (VD `Logo_Id`,
`Avatar_Id`). Nếu chưa có, báo IT tạo cột trước.

**Làm gì:**
1. Mở form → thêm field **map vào đúng cột int** đó (IsVirtual = **TẮT**).
2. Chọn **Editor Type = `AttachmentBox`**.
3. **Lưu form** → Publish.

**Bạn sẽ thấy gì:** field hiện khung đính kèm chỉ cho chọn **1 tệp**; chọn tệp mới sẽ tự thay tệp cũ
(tệp cũ tự bị xóa).

**Lỗi thường gặp:** chọn nhiều tệp nhưng chỉ giữ lại 1 tệp cuối → **đúng hành vi** của chế độ này
(field đang map cột int, chỉ chứa được 1 tham chiếu); muốn nhiều tệp thì quay lại Bước 2.

---

### Bước 4 — Chạy thử

**Mục đích:** xác nhận upload/xem/tải/xóa tệp hoạt động đúng trước khi bàn giao.

**Làm gì:** mở web, vào form vừa cấu hình, thử:
1. Chọn 1 tệp hợp lệ (VD ảnh `.jpg` hoặc `.pdf`) để upload.
2. Xem thumbnail (nếu là ảnh), tải tệp về, rồi xóa thử.
3. Với chế độ **Đa tệp**: thử thêm với bản ghi **mới chưa lưu**, để kiểm tra tệp "chờ gắn" tự gắn
   sau khi bấm Lưu.

**Bạn sẽ thấy gì:** tệp lên/xuống bình thường; nếu chọn tệp không hợp lệ (đuôi tệp không nằm trong
danh sách cho phép, hoặc tệp có dấu hiệu không an toàn) → hệ thống **tự chặn** và báo rõ lý do — đây
là hành vi bảo mật mặc định, không phải lỗi cấu hình.

**Lỗi thường gặp:** control không hiện / field vẫn ra ô nhập chữ bình thường → chưa **Publish** sau
khi đổi Editor Type, hoặc hệ thống chưa đủ điều kiện ở mục Chuẩn bị. Xem đầy đủ ở bảng
[Lỗi thường gặp](#6-lỗi-thường-gặp) trong Phần B.

---

## Phần B — Tra cứu kỹ thuật

> Dành cho người đã quen cách làm ở Phần A, hoặc lập trình viên/AI cần tra nhanh tên trường kỹ thuật.
> Nội dung dưới đây là bản kỹ thuật đầy đủ — không giải thích lại từ đầu.
>
> **Liên quan:**
> - Đặc tả kỹ thuật đầy đủ (backend/frontend/DB/bảo mật) → [26_FILE_UPLOAD_SPEC.md](../spec/26_FILE_UPLOAD_SPEC.md)
> - Tổng quan editor types → [09_FIELD_CONFIG_GUIDE.md](../spec/09_FIELD_CONFIG_GUIDE.md)
> - Field ảo là gì → [cau-hinh-field-ao-cascade.md](cau-hinh-field-ao-cascade.md)

## 0. AttachmentBox là gì

`AttachmentBox` là control cho phép **tải tệp lên** (ảnh, PDF, Office…) gắn với bản ghi đang mở: chọn tệp, xem tiến trình, xem preview thumbnail (với ảnh), tải về, xóa. Ảnh được **nén tự động** (cả ở trình duyệt lẫn server) và server **kiểm tra an toàn** trước khi nhận.

Control có **2 chế độ, tự chọn theo cờ `IsVirtual` của field** — bạn không phải cấu hình gì thêm để chuyển chế độ:

| Chế độ | Bật khi | Số tệp | Nơi lưu liên kết | Dùng cho |
|---|---|---|---|---|
| **Đa tệp** | `IsVirtual = BẬT` (field ảo, không map cột) | Nhiều | Bảng phụ `TT_TepDinhKem` theo `(bảng, Id record, field)` | Hồ sơ, hợp đồng, ảnh sản phẩm |
| **Một tệp** | `IsVirtual = TẮT`, field map **cột `int`** | Đúng 1 | Id tệp lưu **thẳng vào cột** (kiểu `Logo_Id`) | Logo, avatar, ảnh đại diện |

> **Vì sao 2 chế độ?** Một cột DB chỉ chứa được **1 tham chiếu** (1 tệp). Muốn **nhiều tệp** cho 1 field thì không nhét vào cột được → phải để field **ảo** và lưu liên kết ở bảng phụ. Còn khi chỉ cần **1 tệp** thì lưu Id tệp vào 1 cột `int` là gọn nhất (join SQL trực tiếp được).

---

## 1. Chế độ **Đa tệp** (field ảo)

Dùng khi 1 field cần chứa **nhiều tệp** (VD: "Tài liệu hợp đồng", "Ảnh sản phẩm").

**Các bước trong ConfigStudio:**

1. Mở form → **thêm field mới**.
2. Bật **IsVirtual = true** (field UI-only, không map cột DB).
3. Đặt **Field Code** (VD: `TaiLieu`) — dùng làm `Field_Ma` để gom nhóm tệp.
4. Chọn **Editor Type = `AttachmentBox`**.
5. (Tùy chọn) đặt **Loại tệp** qua Control Props — xem §3.
6. **Lưu form** → Publish.

**Cơ chế lấy tệp (không cần cột):** control hỏi bảng phụ `TT_TepDinhKem`: *"cho mọi tệp của bảng `<form>`, record `<Id đang mở>`, field `<Field Code>`"*. Bảng chủ + Id record được form **tự cung cấp** — bạn không phải gõ.

> ✅ **Đa tệp chạy được cả khi thêm mới.** Với record **mới chưa lưu**, tệp được upload ngay (trạng thái "treo") và **tự gắn vào bản ghi sau khi bạn bấm Lưu**. Nếu bấm Hủy (không lưu), tệp treo sẽ được job dọn xử lý.

---

## 2. Chế độ **Một tệp** (field map cột)

Dùng khi field chứa **đúng 1 tệp** và muốn lưu tham chiếu **thẳng vào cột** của bản ghi (giống `TC_CongTy.Logo_Id`).

**Chuẩn bị cột:** bảng phải có **1 cột kiểu `int`/`bigint`** để chứa Id tệp (VD `Logo_Id`, `Avatar_Id`).

**Các bước trong ConfigStudio:**

1. Mở form → thêm field **map vào cột int** đó (IsVirtual = **TẮT**).
2. Chọn **Editor Type = `AttachmentBox`**.
3. **Lưu form** → Publish.

**Cơ chế:** khi upload, control ghi **Id tệp vào giá trị field** → form lưu Id đó xuống cột như mọi field bình thường. Khi mở lại, control đọc Id từ cột → hiển thị tệp. Thay tệp = upload tệp mới (tệp cũ tự bị xóa).

> ✅ **Chế độ 1 tệp chạy được cả khi tạo mới** — không cần lưu record trước, vì Id tệp được lưu **cùng lúc** với bản ghi.

---

## 3. Control Props (đều tùy chọn)

Control đọc cấu hình từ `Control_Props_Json`. **Lưu ý:** ô `Control_Props_Json` trong ConfigStudio hiện **chỉ hiển thị (không gõ tay)** — nên các giá trị dưới đây đều có **mặc định hợp lý**, phần lớn trường hợp **không cần đặt gì**.

| Khóa | Áp cho | Ý nghĩa | Mặc định |
|---|---|---|---|
| `loai` | cả 2 | Nhãn phân loại tệp (VD `HopDong`, `Anh`). Chỉ để gom/nhận diện. | không phân loại |
| `ownerTable` | đa tệp | Bảng chủ tệp gắn vào. | **tự suy từ form** |
| `ownerIdField` | đa tệp | Tên khóa trong context để lấy Id record. | `Id` (host tự bơm `__ownerId`) |
| `maxDimension` | ảnh | Cạnh dài tối đa (px) khi nén ở trình duyệt. | `2000` |
| `quality` | ảnh | Chất lượng nén client (0–1). | `0.85` |

> Nếu cần đặt các khóa này (VD đính kèm sang **bảng khác** bảng đích của form), hiện phải nhập `Control_Props_Json` qua đường khác (seed DB / công cụ) — panel nhập trực tiếp trong WPF là hạng mục nâng cấp sau. Với dùng thông thường thì **không cần**.

---

## 4. Bảo mật & tối ưu — server tự lo

Bạn **không phải cấu hình** những mục sau, server tự áp cho mọi tệp:

- **Kiểm tra hợp lệ:** allowlist đuôi tệp + **magic-byte** (chống đổi đuôi) + **chặn mã thực thi/script/HTML-SVG** + chặn double-extension.
- **Giới hạn kích thước:** theo `FileStorage:MaxBytes` (mặc định 50MB).
- **Tối ưu ảnh:** resize theo cạnh dài tối đa + nén + **sinh thumbnail** (SkiaSharp).
- **Chống trùng:** tệp trùng nội dung (checksum) chỉ lưu **1 bản** vật lý (dedup).

Danh sách đuôi cho phép mặc định: ảnh (`png jpg jpeg webp gif`), tài liệu (`pdf doc docx xls xlsx ppt pptx`), text (`csv txt`), nén (`zip`). Muốn đổi → sửa `FileStorage:Validation:AllowedExtensions` (cấu hình hệ thống, không phải trong ConfigStudio).

---

## 5. Điều kiện hệ thống (một lần, do dev/vận hành)

Để control chạy, hệ thống cần:

1. **Đã chạy migration `db/070_alter_tt_tep_blob_attachment.sql`** trên Data DB của tenant (tạo bảng `TT_TepBlob` + cột mới).
2. **Cấu hình `FileStorage`** trong `appsettings.local.json` (nơi lưu tệp lớn) — mặc định `Provider=Db` chạy được ngay. Chi tiết: [26_FILE_UPLOAD_SPEC.md §7](../spec/26_FILE_UPLOAD_SPEC.md).

---

## 6. Lỗi thường gặp

| Hiện tượng | Nguyên nhân | Cách xử lý |
|---|---|---|
| Hiện *"Tệp sẽ được gắn sau khi Lưu"* (đa tệp, thêm mới) | Bình thường — tệp đã upload, đang chờ gắn | Bấm **Lưu** để gắn tệp vào bản ghi. |
| Upload báo *"Định dạng không nằm trong danh sách cho phép"* | Đuôi tệp ngoài allowlist | Đổi loại tệp, hoặc thêm đuôi vào `FileStorage:Validation:AllowedExtensions`. |
| Upload báo *"chứa dấu hiệu mã thực thi"* | Nội dung tệp có script/HTML/exe (kể cả SVG) | Đúng theo thiết kế — tệp bị chặn vì lý do an toàn. |
| Control không hiện / field ra ô text | Chưa Publish sau khi đổi Editor Type; hoặc chưa chạy migration 070 | Publish lại form; kiểm tra migration. |
| Chọn nhiều tệp nhưng chỉ giữ 1 | Field đang ở **chế độ 1 tệp** (IsVirtual=TẮT) | Bật IsVirtual nếu muốn đa tệp. |
