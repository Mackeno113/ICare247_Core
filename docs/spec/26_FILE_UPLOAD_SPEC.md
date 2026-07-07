# 26 — Đặc tả tính năng Upload / Đính kèm tệp

> **Trạng thái:** đã hiện thực (backend + frontend + tích hợp WPF), chưa kiểm thử trình duyệt end-to-end.
> **Đối tượng:** dev backend/frontend.
> **Hướng dẫn cấu hình no-code (ConfigStudio)** → [../huong-dan-wpf/cau-hinh-attachment.md](../huong-dan-wpf/cau-hinh-attachment.md).

Hệ đính kèm tệp tổng quát: mọi loại tệp (ảnh/PDF/Office…), gắn vào field của Form Engine, với 4 trục:
**tối ưu dung lượng ảnh · UX file lớn (streaming + progress) · bảo mật (allowlist + magic-byte + chặn mã thực thi) · lưu trữ linh hoạt (di dời gốc, đa node)**.

---

## 1. Mô hình dữ liệu — tách Blob ⟂ Attachment

Migration: [`db/070_alter_tt_tep_blob_attachment.sql`](../../db/070_alter_tt_tep_blob_attachment.sql). Bổ sung (additive) trên nền `db/063` (đã có `TT_TepDinhKem` cho logo).

```
TT_TepBlob            — NỘI DUNG VẬT LÝ (đơn vị dedup theo Checksum)
  Id, Checksum(unique filtered IsDeleted=0), ContentType, KichThuoc,
  Storage_Kind(Db|FileSystem|Object), NoiDung(varbinary khi Db) | Storage_Key(khi FS/Object),
  RefCount, + khối audit (ADR-022)

TT_TepDinhKem         — BẢN GHI ĐÍNH KÈM (mỗi lần dùng 1 dòng, trỏ Blob)
  Id, Blob_Id → TT_TepBlob, ThumbBlob_Id → TT_TepBlob,
  Owner_Table, Owner_Id, Field_Ma,     -- gắn record/field (chế độ đa tệp)
  TenFile, ContentType, KichThuoc, Loai, Checksum, + audit
```

- **Dedup:** nhiều bản ghi đính kèm cùng nội dung → 1 dòng `TT_TepBlob` (RefCount đếm tham chiếu). Xóa hết tham chiếu (RefCount=0) → dọn nội dung vật lý.
- **Tương thích:** `TC_CongTy.Logo_Id` vẫn trỏ `TT_TepDinhKem.Id` như cũ.

---

## 2. Trừu tượng lưu trữ (`IFileStore`)

3 backend đứng sau một interface, chọn qua config `FileStorage:Provider`:

| Provider | Nội dung | Node-safe sau LB |
|---|---|---|
| `Db` | bytes trong cột `TT_TepBlob.NoiDung` | ✅ (mọi node chung Data DB) |
| `FileSystem` | file trên đĩa/shared mount, `Storage_Key` = path **tương đối** | ✅ nếu `BaseRoot` là mount dùng chung |
| `Object` | MinIO/S3/Azure Blob (**stub**, chờ cắm SDK) | ✅ bản chất |

**Định tuyến:** [`IFileStoreSelector`](../../src/backend/src/ICare247.Application/Interfaces/IFileStoreSelector.cs) — ghi: tệp ≤ `DbThresholdBytes` → `Db`, lớn hơn → provider cấu hình; đọc/xóa: theo `Storage_Kind` đã lưu.

**Di dời gốc chứa (D: → NAS → cloud):** `Storage_Key` trong DB **luôn tương đối** (`[siteKey/]{tenantId}/{yyyy}/{MM}/{loai}/{shard}/{sha}.{ext}`), gốc thật = `FileStorage:BaseRoot` (config theo deployment). Di dời = copy cây thư mục + đổi 1 giá trị config, **0 dòng SQL**.

**An toàn:**
- [`StorageKeyBuilder`](../../src/backend/src/ICare247.Infrastructure/Files/StorageKeyBuilder.cs): key sinh 100% ở server, làm sạch mọi thành phần.
- [`FileSystemFileStore`](../../src/backend/src/ICare247.Infrastructure/Files/FileSystemFileStore.cs): guard **path-traversal** (resolved phải nằm trong BaseRoot) + ghi tệp tạm → rename (atomic) + dedup bỏ ghi lại.
- [`FileStorageStartupCheck`](../../src/backend/src/ICare247.Infrastructure/Files/FileStorageStartupCheck.cs): probe backend lúc khởi động; `Provider != Db` mà không đọc/ghi được → **dừng app (fail-fast)** — tránh node âm thầm ghi local gây 404 sau load-balancer.

---

## 3. Luồng upload (orchestration)

[`UploadAttachmentCommandHandler`](../../src/backend/src/ICare247.Application/Features/Files/UploadAttachment/UploadAttachmentCommandHandler.cs):

```
Controller: đọc multipart → SPOOL ra tệp tạm seekable (FileOptions.DeleteOnClose) → không nạp full RAM
      │
Handler:
  1. Đọc 8KB header + kích thước
  2. Validate (allowlist đuôi + magic-byte + sniff mã thực thi)      → 400 nếu fail
  3. Nếu là ảnh: SkiaSharp resize/nén + sinh thumbnail (thay nội dung)
  4. Tính SHA256 (của nội dung đã tối ưu)
  5. Chọn store theo kích thước → dựng key tương đối → SaveAsync
  6. UpsertAsync TT_TepBlob (MERGE HOLDLOCK: dedup + RefCount++)      ← race-safe
  7. Lặp (5–6) cho thumbnail
  8. Insert TT_TepDinhKem (Blob_Id, ThumbBlob_Id, Owner/Field, TenFile…)
```

---

## 4. Bảo mật ([`FileValidator`](../../src/backend/src/ICare247.Infrastructure/Files/FileValidator.cs))

4 lớp, theo thứ tự:

1. **Kích thước:** `> 0` và `≤ MaxBytes`.
2. **Đuôi + double-extension:** mọi đoạn đuôi (kể cả `.php.pdf`) không được nằm trong **blocklist nguy hiểm** (`exe dll bat ps1 js vbs php asp html svg …`); đuôi cuối phải thuộc **allowlist**.
3. **Sniff nội dung:** chặn magic thực thi (`MZ`, ELF, Mach-O, shebang) và marker script/HTML (`<script`, `<?php`, `<svg`, `onerror=`…) trong phần đầu.
4. **Magic-byte khớp đuôi:** PNG/JPEG/GIF/WebP/PDF/ZIP(docx…)/OLE(doc…). Text (txt/csv) không bắt buộc magic.

Serve tệp ([`AttachmentsController`](../../src/backend/src/ICare247.Api/Controllers/AttachmentsController.cs)): thêm `X-Content-Type-Options: nosniff`; endpoint tải nội dung chính dùng `Content-Disposition: attachment` (không cho trình duyệt render inline). ETag theo checksum + 304.

---

## 5. Tối ưu ảnh

- **Server (nguồn chuẩn):** [`SkiaImageOptimizer`](../../src/backend/src/ICare247.Infrastructure/Files/SkiaImageOptimizer.cs) (SkiaSharp, MIT) — resize theo `MaxDimension`, nén `Quality`, sinh thumbnail. Ảnh có alpha → giữ PNG; còn lại → JPEG. Lỗi giải mã → dùng ảnh gốc (không chặn upload).
- **Client (giảm băng thông):** [`attachment-upload.js`](../../src/frontend/ICare247_UI/wwwroot/js/attachment-upload.js) nén qua canvas trước khi gửi. Server vẫn chuẩn hóa lại.

---

## 6. API

| Method | Route | Mô tả |
|---|---|---|
| POST | `/api/v1/attachments` | Upload (multipart: `file` + `loai/ownerTable/ownerId/fieldMa` tùy chọn). 400 nếu tệp không hợp lệ. |
| GET | `/api/v1/attachments/{id}` | Tải nội dung chính (attachment + nosniff + ETag). |
| GET | `/api/v1/attachments/{id}/thumbnail` | Thumbnail (inline cho `<img>`). |
| GET | `/api/v1/attachments/{id}/info` | Metadata 1 tệp (chế độ 1-tệp/cột). |
| GET | `/api/v1/attachments?ownerTable&ownerId&fieldMa` | Liệt kê đính kèm theo record. |
| POST | `/api/v1/attachments/link` | Gắn loạt tệp "treo" (Owner_Id NULL) vào record vừa tạo — đa-tệp-khi-thêm-mới. |
| DELETE | `/api/v1/attachments/{id}` | Xóa (soft-delete + giảm RefCount blob; =0 → dọn vật lý). |

Tất cả `[Authorize]`. `FilesController` cũ (logo) giữ nguyên, không đụng.

---

## 7. Cấu hình (`appsettings` → section `FileStorage`)

```jsonc
"FileStorage": {
  "Provider": "Db",              // Db | FileSystem | Object
  "BaseRoot": "",                // \\nas01\icare247 (FileSystem) — dùng chung mọi node sau LB
  "SiteKey": "",                 // đoạn cô lập vật lý tùy chọn (nhiều site chung 1 gốc)
  "DbThresholdBytes": 262144,    // ≤256KB → Db
  "MaxBytes": 52428800,          // 50MB
  "Object": { "Endpoint": "", "Bucket": "", "AccessKey": "", "SecretKey": "" },
  "Validation": { "AllowedExtensions": [ /* rỗng = dùng default */ ] },
  "Image": { "Enabled": true, "MaxDimension": 2000, "Quality": 82,
             "ThumbnailDimension": 256, "ThumbnailQuality": 72 }
}
```

- **1 node / dev:** `Provider=Db` — chạy ngay, không cần gì thêm.
- **Cụm nhiều node (LB):** `Provider=FileSystem` + `BaseRoot` là **shared mount** mọi node cùng thấy; hoặc `Provider=Object`; hoặc để `Db` (nặng DB nhưng luôn đúng).
- Đặt `BaseRoot`/`Object` thật qua `appsettings.local.json` (không vào git).

---

## 8. Frontend

**Control** [`AttachmentRenderer.razor`](../../src/frontend/ICare247_UI/Components/FieldRenderers/AttachmentRenderer.razor) — **2 chế độ tự chọn theo `FieldState.IsVirtual`:**

| Chế độ | Điều kiện | Giá trị field | Lấy tệp |
|---|---|---|---|
| Đa tệp | `IsVirtual = true` | = số lượng tệp (record cũ) / **List Id treo** (thêm mới) | `ListByOwner(bảng, Id, field)` |
| 1 tệp | `IsVirtual = false` (map cột) | = Id tệp (hoặc null) | `GetInfo(Id trong cột)` |

**Đa-tệp-khi-thêm-mới (attach-then-link):** record mới chưa có Id → tệp upload ngay (Owner_Id **treo** NULL), renderer giữ danh sách Id ở `State.Value`. Sau khi [`MasterDataForm`](../../src/frontend/ICare247_UI/Components/MasterData/MasterDataForm.razor) lưu record có Id → gọi `POST /attachments/link` gắn loạt Id đó (chỉ dòng `Owner_Id IS NULL` + đúng `CreatedBy` — an toàn). Hủy không lưu → tệp treo được job dọn xử lý.

- **Dispatch:** [`FieldRenderer.razor`](../../src/frontend/ICare247_UI/Components/FieldRenderer.razor) `case "attachment"`.
- **Chuẩn hóa type:** [`FormRunner.razor`](../../src/frontend/ICare247_UI/Pages/FormRunner.razor) `NormalizeFieldType`: `attachmentbox → attachment`.
- **Tự suy owner (đa tệp):** [`MasterDataForm.razor`](../../src/frontend/ICare247_UI/Components/MasterData/MasterDataForm.razor) bơm `__ownerTable`(=FormCode) + `__ownerId`(=Id record) vào Context; renderer đọc.
- **Upload JS:** XHR (để có % progress — HttpClient WASM không báo upload progress) + nén ảnh canvas + Bearer thủ công; tải về = fetch kèm token → blob (endpoint cần JWT nên không mở URL trực tiếp).
- **Service:** [`AttachmentApiService`](../../src/frontend/ICare247_UI/Services/AttachmentApiService.cs) (list/delete/info/thumbnail-dataURL/upload-options).

---

## 9. Tích hợp ConfigStudio (WPF)

- `Ui_Field.Editor_Type = "AttachmentBox"` → backend trả `FieldType` thẳng → `NormalizeFieldType` → renderer.
- [`FieldConfigViewModel`](../../src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Modules.Forms/ViewModels/FieldConfigViewModel.cs): thêm `AttachmentBox` vào `AvailableEditorTypes` + guide inline.
- Chọn chế độ = bật/tắt **IsVirtual** (đã có sẵn trong Field Config). Xem hướng dẫn: [cau-hinh-attachment.md](../huong-dan-wpf/cau-hinh-attachment.md).

---

## 10. Danh sách file

**Backend** — `Application`: `Files/{FileStorageOptions,StoredContent}.cs`, `Interfaces/{IFileStore,IStorageKeyBuilder,IFileStoreSelector,IFileValidator,IImageOptimizer,ITepBlobRepository,IAttachmentRepository}.cs`, `Features/Files/{UploadAttachment,GetAttachment,ListAttachments,DeleteAttachment}/*`. `Infrastructure`: `Files/{StorageKeyBuilder,DbFileStore,FileSystemFileStore,ObjectFileStore,FileStoreSelector,FileStorageStartupCheck,FileValidator,SkiaImageOptimizer}.cs`, `Repositories/{TepBlobRepository,AttachmentRepository}.cs`. `Api`: `Controllers/AttachmentsController.cs`. DI: `Infrastructure/DependencyInjection.cs`. Package: `SkiaSharp` (+ `NativeAssets.Linux.NoDependencies`).

**Frontend** — `Services/AttachmentApiService.cs`, `Components/FieldRenderers/AttachmentRenderer.razor(.css)`, `wwwroot/js/attachment-upload.js`, sửa `FieldRenderer.razor`/`FormRunner.razor`/`MasterDataForm.razor`/`Program.cs`.

**DB** — `db/070_alter_tt_tep_blob_attachment.sql`.

---

## 11. Việc cần làm sau (follow-ups)

1. **Chạy migration `070`** trên Data DB mỗi tenant.
2. **`ObjectFileStore`**: cắm SDK MinIO/S3/Azure (đang stub; startup-check chặn nếu chọn `Object` mà chưa cắm).
3. **Job dọn tệp mồ côi — HOÃN (thiết kế riêng sau).** 3 nguồn mồ côi: (a) tệp treo `Owner_Id NULL`
   bị bỏ khi Hủy form thêm mới; (b) tệp 1-tệp upload rồi Hủy (không cột nào trỏ tới); (c) blob
   `RefCount=0` sót do lỗi/crash giữa chừng khi xóa. Cần **thời gian ân hạn** (VD 24h) để không xóa
   nhầm tệp đang điền dở. Đã bàn 3 cách chạy (endpoint admin / hosted-service duyệt tenant / SQL Agent);
   quyết định: **gộp vào một tính năng "quản lý tiến trình nền" chung có UI** — phân tích & thiết kế
   trong giai đoạn riêng, KHÔNG làm ở phase này.
   - **Tác động tạm thời:** tệp mồ côi chỉ **tốn dung lượng**, KHÔNG ảnh hưởng đúng/sai nghiệp vụ
     (không dòng nào tham chiếu tới chúng). Dọn tay bằng SQL khi cần cho tới khi có job.
4. ~~Đính-kèm-khi-tạo-mới (orphan→link) cho chế độ đa tệp~~ — **ĐÃ LÀM** (upload treo → `POST /link` sau khi lưu).
5. **Panel Control Props trong WPF** để nhập `ownerTable/loai` (ô hiện chỉ-đọc) — nếu cần đính kèm sang bảng khác bảng đích.
6. **SkiaSharp trên Linux**: đã thêm `NativeAssets.Linux.NoDependencies` — verify khi deploy.
7. **Kiểm thử trình duyệt end-to-end** (chưa chạy — mới verify bằng compile).
8. **RBAC theo record** cho endpoint đính kèm (hiện chỉ `[Authorize]`).
9. Cân nhắc `ownerTable` = tên bảng vật lý (thay vì FormCode) nếu cần nhiều form chung 1 bảng thấy chung tệp.
