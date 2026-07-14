# Spec 28 — Doc Template (Xuất Word/PDF theo mẫu · mail-merge · ghép fragment)

> Phiên bản: 1.0 | Cập nhật: 2026-07-09
>
> Trạng thái: **CHỐT** — các điểm §13 A/B/C đã quyết; sẵn sàng GĐ1. (§13-D hoãn sau GĐ1.)

## 1. Mục tiêu & phạm vi

Cho phép người dùng **tự soạn mẫu Word (.docx)** ngay trong ứng dụng (Web + WPF ConfigStudio),
**kéo biến** vào mẫu, rồi hệ thống **bơm dữ liệu thật** để sinh văn bản (hợp đồng, biểu mẫu, quyết định…)
dạng `.docx`/`.pdf`, **in hàng loạt**.

Đặc thù đã chốt:

- **Mọi truy vấn dữ liệu đi qua Stored Procedure** — KHÔNG chấp nhận T-SQL trần trong cấu hình.
- **1 master + NHIỀU detail** (VD: 1 nhân viên → nhiều hợp đồng + nhiều diễn biến lương).
- **Master khổ A4 dọc, detail A4 ngang** — như nhiều mẫu tách biệt nhưng **in 1 lần** (1 file).
- **Template lưu tenant-local** — mỗi tenant tự cấu hình, KHÔNG đồng bộ ConfigSync.
- **DevExpress Office File API** cô lập vào **1 project backend duy nhất**.

Ngoài phạm vi GĐ1: master-detail lồng nhiều cấp (detail-của-detail), template dùng chung master→tenant,
ký số/đóng dấu, trộn ảnh động phức tạp.

## 2. Nguyên tắc thiết kế

### 2.1 Ghép fragment ("nhiều mẫu tách biệt, in 1 lần")

Một "bộ mẫu" = **1 fragment master** + **N fragment detail**, mỗi fragment là 1 file `.docx` độc lập,
có hướng giấy riêng, gắn **đúng 1 stored proc**. Khi sinh, hệ thống merge từng fragment rồi
**ghép nối** (`AppendDocumentContent`) theo thứ tự → 1 tài liệu duy nhất, giữ nguyên section/hướng giấy.

```
IN 1 LẦN (1 file .docx/.pdf)
├── Fragment MASTER   (A4 dọc)   ← proc master  → 1 dòng  → điền biến đơn (MERGEFIELD)
├── Fragment DETAIL 1 (A4 ngang) ← proc HĐ      → N dòng  → bảng lặp (merge region)
├── Fragment DETAIL 2 (A4 ngang) ← proc DBLuong → N dòng  → bảng lặp (merge region)
└── … (số detail tùy cấu hình, theo Thu_Tu)
```

> Lý do chọn ghép-fragment thay vì 1 template đa-section: RichEdit **không** hỗ trợ quan hệ
> master-detail kiểu DataSet-relation (feature đó thuộc sản phẩm Snap). Ghép fragment đơn giản,
> mỗi mảnh 1 nguồn dữ liệu độc lập, khớp mô tả nghiệp vụ, và xử lý hướng giấy tự nhiên.

### 2.2 Biến suy từ cột kết quả proc

Không gõ biến tay. Biến = **danh sách cột** mà stored proc trả ra, phát hiện qua
`sp_describe_first_result_set` (không chạy side-effect). Panel soạn thảo hiển thị đúng danh sách này.

### 2.3 Cô lập DevExpress

Nghiệp vụ render nằm sau interface `IDocTemplateRenderer` (tầng Application, **không** tham chiếu DevExpress).
Impl DevExpress nằm trong **`ICare247.Infrastructure.Documents`** — project duy nhất chạm `DevExpress.Docs`.
→ Chỉ 1 dev có license build project này (xem cân nhắc license ở §9.4).

## 3. Kiến trúc tổng thể

```
[Soạn]  Web DxRichEdit  /  WPF RichEditControl
           │ (lưu bytes .docx mỗi fragment + gắn proc)
           ▼
        Config store (tenant-local)  ── Doc_Template (+ Doc_Template_Detail)
           │
[Sinh]     ▼
        API  POST /api/v1/doc-templates/{id}/render?...params
           │
        IDocTemplateRenderer  (Application)
           │  impl
        ICare247.Infrastructure.Documents  (DevExpress)
           ├─ Data: gọi stored proc trên Data DB tenant (Dapper, CommandType.StoredProcedure)
           ├─ Merge: RichEditDocumentServer.MailMerge từng fragment
           └─ Ghép + xuất: AppendDocumentContent → SaveDocument(OpenXml) / ExportToPdf
```

## 4. Mô hình dữ liệu

**Vị trí DB (chốt §13-A):** đặt trong **Config DB** (`ICare247_Config`) — truy cập qua
`IDbConnectionFactory` (nhóm `Ui_*`/`Sys_*`), **KHÔNG** ở Data DB ("Data DB không chứa cấu hình"),
**KHÔNG** đưa vào descriptor ConfigSync (tenant-local). Mô hình **mỗi tenant 1 DB** → ranh giới tenant là
chính DB, nên **KHÔNG có cột `Tenant_Id`** (ADR-035 — cô lập ở tầng connection, không ở tầng cột).

Stored proc dữ liệu (§5) chạy trên **Data DB** qua `IDataDbConnectionFactory` — tách hẳn Config DB.

Áp **khối cột auto chuẩn** (Spec 11 §0.1) cho mọi bảng: `Id, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, IsDeleted, Ver`.

### 4.1 `Doc_Template` — bộ mẫu (master)

| Cột | Kiểu | Ràng buộc | Ý nghĩa |
|---|---|---|---|
| Id | bigint | PK IDENTITY | |
| Ma | nvarchar(50) | filtered UNIQUE `WHERE IsDeleted=0` | mã bộ mẫu |
| Ten | nvarchar(200) | NOT NULL | tên hiển thị |
| Master_Proc | nvarchar(128) | NOT NULL | tên stored proc master (trả **1 dòng**) |
| Master_Docx | varbinary(max) | NULL | bytes fragment master (A4 dọc) |
| Mo_Ta | nvarchar(500) | NULL | ghi chú |
| Is_Active | bit | NOT NULL DEFAULT 1 | bật/tắt |
| *(+ khối audit)* | | | |

### 4.2 `Doc_Template_Detail` — mảnh chi tiết (1 master : N detail)

| Cột | Kiểu | Ràng buộc | Ý nghĩa |
|---|---|---|---|
| Id | bigint | PK IDENTITY | |
| Template_Id | bigint | NOT NULL FK→Doc_Template | thuộc bộ mẫu nào |
| Ma | nvarchar(50) | NOT NULL | mã mảnh (unique trong 1 Template_Id) |
| Ten | nvarchar(200) | NOT NULL | VD "Danh sách hợp đồng" |
| Detail_Proc | nvarchar(128) | NOT NULL | stored proc detail (trả **N dòng**) |
| Detail_Docx | varbinary(max) | NULL | bytes fragment detail (thường A4 ngang, có vùng lặp) |
| Thu_Tu | int | NOT NULL DEFAULT 0 | thứ tự ghép/in (ADR-027 dùng chung nếu cần ▲▼) |
| Is_Active | bit | NOT NULL DEFAULT 1 | |
| *(+ khối audit)* | | | |

**Constraint:** UNIQUE `(Template_Id, Ma)` WHERE `IsDeleted=0`.

### 4.3 `Doc_Proc_Registry` — đăng ký proc hợp lệ (whitelist, chốt §13-B)

Chỉ proc có trong bảng này mới được render/khám phá biến gọi (chặn gọi proc tùy tiện).

| Cột | Kiểu | Ràng buộc | Ý nghĩa |
|---|---|---|---|
| Id | bigint | PK IDENTITY | |
| Proc_Name | nvarchar(128) | filtered UNIQUE `WHERE IsDeleted=0` | tên proc (validate regex `^[A-Za-z_][A-Za-z0-9_]*$`) |
| Loai | nvarchar(20) | NOT NULL | `master` \| `detail` (gợi ý vai trò) |
| Mo_Ta | nvarchar(500) | NULL | mô tả proc |
| Is_Active | bit | NOT NULL DEFAULT 1 | |
| *(+ khối audit)* | | | |

> `Master_Proc`/`Detail_Proc` phải trỏ tới `Proc_Name` đang `Is_Active=1` trong registry — nếu không, từ chối.

### 4.4 `Doc_Template_Param` — ánh xạ tham số proc (bảng phụ, chốt §13-C)

Khai báo tham số mỗi proc cần + nguồn lấy giá trị lúc render.

| Cột | Kiểu | Ràng buộc | Ý nghĩa |
|---|---|---|---|
| Id | bigint | PK IDENTITY | |
| Template_Id | bigint | NOT NULL FK→Doc_Template | thuộc bộ mẫu |
| Detail_Id | bigint | NULL FK→Doc_Template_Detail | NULL = tham số cho proc master; có = cho proc detail cụ thể |
| Param_Name | nvarchar(64) | NOT NULL | tên tham số proc, VD `@NhanVien_Id` |
| Nguon | nvarchar(20) | NOT NULL | `key` (từ keyParams render) \| `context` (NguoiDungId) \| `const` |
| Nguon_Key | nvarchar(64) | NULL | khóa trong keyParams / tên context / giá trị const |
| Kieu | nvarchar(20) | NOT NULL DEFAULT 'string' | ép kiểu tham số |
| Thu_Tu | int | NOT NULL DEFAULT 0 | thứ tự (nếu cần) |
| *(+ khối audit)* | | | |

**Constraint:** UNIQUE `(Template_Id, Detail_Id, Param_Name)` WHERE `IsDeleted=0`.

## 5. Hợp đồng Stored Procedure

### 5.1 Vai trò

- **Proc master** — trả về **đúng 1 dòng**; mỗi cột = 1 biến đơn (`MERGEFIELD`).
- **Proc detail** — trả về **0..N dòng**; mỗi cột = 1 biến trong vùng lặp (region) của fragment detail.

### 5.2 Tham số chuẩn

Mọi proc nhận cùng bộ tham số ngữ cảnh (đặt tên thống nhất), tối thiểu:

| Tham số | Nguồn | Bắt buộc |
|---|---|---|
| `@KhoaChinh` (hoặc theo nghiệp vụ, VD `@NhanVien_Id`) | tham số render truyền vào | ✔ |
| `@NguoiDungId` | ngữ cảnh phiên (lọc quyền) | tùy proc |

> ADR-035: **không** truyền `@Tenant_Id` — proc chạy trên Data DB đã thuộc đúng tenant (connection tự trỏ).

> Ánh xạ tên tham số khai báo ở **bảng phụ `Doc_Template_Param`** (§4.4) — chốt §13-C.
> Gọi qua Dapper `CommandType.StoredProcedure`, **tham số hóa 100%**, không nối chuỗi.

### 5.3 Whitelist proc (bảo mật) — chốt §13-B = **bảng đăng ký**

Chỉ proc có trong **`Doc_Proc_Registry`** (§4.3, `Is_Active=1`) mới được gọi. Trước khi thực thi:
1. Tên proc phải khớp `Doc_Proc_Registry` (registry nằm trong Config DB của chính tenant).
2. Validate lại tên bằng regex `^[A-Za-z_][A-Za-z0-9_]*$` (phòng thủ 2 lớp).
3. Chạy trên **Data DB tenant** dưới principal **read-only**; proc chỉ `SELECT` (không DML/DDL).

### 5.4 Khám phá biến (variable discovery)

Khi soạn: gọi `sys.sp_describe_first_result_set @tsql = N'EXEC <proc> ...'` (hoặc `SET FMTONLY`)
→ lấy danh sách `(name, system_type_name, ...)` → panel biến. Không thực thi thân proc gây side-effect.

## 6. Cơ chế template & merge (DevExpress — đã kiểm chứng §14)

### 6.1 Biến đơn
Chèn `MERGEFIELD TenCot` — tên field = tên cột proc trả về.

### 6.2 Bảng lặp detail (nạp nguyên bảng nhiều dòng)
Trong fragment detail: chèn 1 bảng, hàng dữ liệu chứa các `MERGEFIELD`, **bọc vùng lặp** bằng
`MailMergeOptions.RegionStartTag` / `RegionEndTag` → mỗi dòng proc → 1 hàng bảng.

### 6.3 Hướng giấy theo section
`SectionPage.Landscape = true` + `PaperKind = A4`. Fragment master để dọc, fragment detail để ngang;
khi ghép, mỗi mảnh giữ page-setup riêng (Word section).

### 6.4 Ghép fragment
`RichEditDocumentServer` gốc load/merge master → với mỗi detail: merge ra sub-doc →
`document.AppendDocumentContent(stream, DocumentFormat.OpenXml)` theo `Thu_Tu`.

## 7. Backend

### 7.1 Interface (tầng Application — không DevExpress)

```csharp
public interface IDocTemplateRenderer
{
    /// Sinh 1 tài liệu từ bộ mẫu + tham số khóa. Trả bytes theo định dạng yêu cầu.
    Task<DocRenderResult> RenderAsync(
        long templateId, IReadOnlyDictionary<string, object?> keyParams,
        DocOutputFormat format, CancellationToken ct = default);

    /// Khám phá biến của 1 proc (cho màn soạn).
    Task<IReadOnlyList<DocVariable>> DescribeVariablesAsync(
        string procName, CancellationToken ct = default);
}
// DocOutputFormat: Docx | Pdf ;  DocRenderResult(byte[] Bytes, string ContentType, string FileName)
// DocVariable(string Name, string DbType)
```

### 7.2 Impl `ICare247.Infrastructure.Documents`
Tham chiếu `DevExpress.Docs`. Luồng `RenderAsync`:
1. Nạp `Doc_Template` + `Doc_Template_Detail` (Is_Active, sắp theo Thu_Tu).
2. Validate proc theo whitelist (§5.3).
3. Gọi proc master → DataTable (1 dòng) → merge `Master_Docx`.
4. Với mỗi detail: gọi proc → DataTable (N dòng) → merge `Detail_Docx` (region) → `AppendDocumentContent`.
5. `SaveDocument(OpenXml)` hoặc `ExportToPdf` → `DocRenderResult`.

### 7.3 API (RFC 7807 — Spec 07)
| Method | Path | Mô tả |
|---|---|---|
| GET | `/api/v1/doc-templates` | danh sách |
| GET/PUT/POST/DELETE | `/api/v1/doc-templates/{id}` | CRUD cấu hình |
| GET | `/api/v1/doc-templates/describe?proc=...` | khám phá biến |
| POST | `/api/v1/doc-templates/{id}/render?format=pdf` | sinh theo Id (body = keyParams) |
| POST | `/api/v1/doc-templates/by-code/{code}/render?format=pdf` | sinh theo **Ma** (dùng khi gắn màn qua `Ui_View_Action.Target`) |

## 7.4 Gắn mẫu vào màn hình (binding) — tái dùng `Ui_View_Action`

**Không thêm bảng binding mới.** Một bộ mẫu gắn vào **màn lưới** bằng đúng cơ chế nút hành động của lưới
(`Ui_View_Action`, Spec 14 §2.3): thêm 1 dòng action với

- `Action_Type = 'Export'` (hoặc `'Print'`), `Export_Format = 'docx'|'pdf'`, `Export_Engine = 'Server'`,
- **`Target = Doc_Template.Ma`** ← đây là liên kết "màn này ↔ mẫu này",
- `Scope = 'Toolbar'|'Row'|'Both'`, `Require_Selection = 1`.

**Runtime:** web (`DataView`) nhận diện action Export/Print + `Engine='Server'` + có `Target` → gom **dòng đang chọn**
(đầy đủ cột) làm `keyParams` → gọi `POST /doc-templates/by-code/{Target}/render` → tải file. `Doc_Template_Param`
(`Nguon='key'`, `Nguon_Key = <tên cột lưới>`) ánh xạ cột dòng sang tham số proc.

**Authoring:** ConfigStudio màn *Quản lý View* → tab *Actions* có combo **"Bộ mẫu (Xuất tài liệu)"** liệt kê
`Doc_Template` (Config DB) → chọn mẫu tự điền `Target = Ma` + `Engine='Server'`.

> **Màn form chi tiết** (mở 1 bản ghi) chưa có cơ chế action tương đương (`Ui_Form_Action` chưa tồn tại) → binding
> hiện chỉ áp cho **lưới/danh sách**. Nút xuất trên form là pha sau.
> Hướng dẫn deployer đầy đủ: `docs/huong-dan-wpf/cau-hinh-xuat-tai-lieu.md`.

## 8. Màn soạn template (authoring)

Chung: khung 2 phần — **editor** (DevExpress RichEdit) + **panel biến** (list cột từ proc, nhóm master/detail).
Thao tác: đặt con trỏ → click/kéo biến → chèn `MERGEFIELD`; nút "Chèn bảng lặp" bọc region cho detail.
Template lưu chung format `.docx (OpenXml)` → **soạn WPF mở được ở Web và ngược lại**.

### 8.1 Web — Blazor `DxRichEdit` (`DevExpress.Blazor.RichEdit`)
### 8.2 WPF ConfigStudio — `RichEditControl` (`DevExpress.Xpf.RichEdit`)

> Cả hai đều dùng DevExpress đã có sẵn ở frontend → không phát sinh ràng buộc license mới.
> Logic panel biến + chèn field phải viết 2 lần (2 nền tảng); format template + backend dùng chung.

## 9. Bảo mật

1. **Chỉ stored proc** (không T-SQL trần) + whitelist (§5.3).
2. **Read-only + SELECT-only** trên Data DB tenant; tham số hóa 100%.
3. Cấu hình template chỉ dành **admin/dev qua WPF/Web quản trị** (nguyên tắc "config qua WPF").
4. **License DevExpress**: build ở máy có license; runtime royalty-free (deploy/tenant không tính thêm).

## 10. Sinh hàng loạt & in
- 1 lần render = 1 bộ mẫu cho 1 khóa master. Hàng loạt = lặp danh sách khóa (ghép thành 1 file nhiều bản
  hoặc nhiều file .pdf) — cơ chế cụ thể chốt sau GĐ1.
- `MailMergeOptions.FirstRecordIndex/LastRecordIndex`, `MergeMode` (NewSection) dùng khi cần lặp bản ghi.

## 11. i18n
Mọi label màn soạn + nhãn panel biến theo Spec 10 (key suy từ cấu trúc). Tên biến (cột proc) là kỹ thuật,
hiển thị kèm chú thích i18n nếu có bảng ánh xạ (tương lai).

## 12. Phân giai đoạn
- **GĐ1 (backend + PoC):** 2 bảng + migration + `IDocTemplateRenderer` + impl DevExpress + API render;
  PoC ghép master (A4 dọc) + 1 detail (A4 ngang) → PDF từ template dựng tay.
- **GĐ2 (soạn Web):** trang Blazor DxRichEdit + panel biến + CRUD template.
- **GĐ3 (soạn WPF):** module ConfigStudio RichEditControl + panel biến.

## 13. Quyết định (đã chốt)
- **A. ✅** `Doc_Template*` đặt ở **Config DB** (`IDbConnectionFactory`), per-tenant DB, ngoài ConfigSync; **không có cột `Tenant_Id`** (ADR-035 — cô lập ở tầng connection). Proc chạy trên Data DB (`IDataDbConnectionFactory`).
- **B. ✅** Whitelist = **bảng đăng ký `Doc_Proc_Registry`** (§4.3) + regex phòng thủ.
- **C. ✅** Ánh xạ tham số proc = **bảng phụ `Doc_Template_Param`** (§4.4).
- **D. ⏳** Sinh hàng loạt (1 file nhiều bản vs nhiều file) — **hoãn quyết sau GĐ1**.

## 14. API DevExpress đã kiểm chứng (reflection 25.2)
- `DevExpress.XtraRichEdit.RichEditDocumentServer` (asm `DevExpress.RichEdit.v25.2.Core`).
- `LoadDocument(byte[], DocumentFormat)`, `SaveDocument`, `ExportToPdf(Stream[, PdfExportOptions])`.
- `MailMerge(MailMergeOptions, Stream, DocumentFormat)` + overloads (Document/String/IRichEditDocumentServer).
- `MailMergeOptions`: `DataSource`, `DataMember`, `MergeMode` (NewParagraph|NewSection|JoinTables),
  `RegionStartTag`/`RegionEndTag`, `FirstRecordIndex`/`LastRecordIndex`.
- `SubDocument.AppendDocumentContent(Stream, DocumentFormat[, ...])` / `InsertDocumentContent(...)`.
- `SectionPage`: `Landscape (bool)`, `PaperKind (DXPaperKind)`, `Width/Height`.
- `DocumentFormat`: OpenXml (=.docx), Pdf export riêng qua `ExportToPdf`.
