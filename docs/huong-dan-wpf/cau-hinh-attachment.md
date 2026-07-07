# Hướng dẫn cấu hình **AttachmentBox** — đính kèm / upload tệp (ConfigStudio)

> **Đối tượng:** người cấu hình form trong ConfigStudio (WPF).
> **Phạm vi:** cách bật control đính kèm tệp cho một field, chọn **chế độ đa tệp / một tệp**, và các ô Control Props liên quan.
> **Liên quan:**
> - Đặc tả kỹ thuật đầy đủ (backend/frontend/DB/bảo mật) → [26_FILE_UPLOAD_SPEC.md](../spec/26_FILE_UPLOAD_SPEC.md)
> - Tổng quan editor types → [09_FIELD_CONFIG_GUIDE.md](../spec/09_FIELD_CONFIG_GUIDE.md)
> - Field ảo là gì → [cau-hinh-field-ao-cascade.md](cau-hinh-field-ao-cascade.md)

---

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

**Chuẩn bị cột:** bảng phải có **1 cột kiểu `int`/`bigint`** để chứa Id tệp (VD: `Logo_Id`, `Avatar_Id`).

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
