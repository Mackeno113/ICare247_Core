# FK Lookup Spec — Nguồn khóa ngoại dùng chung (lưới · form · import · template)

**Spec:** 25_FK_LOOKUP_SPEC
**Phiên bản:** 0.2 (mô hình chốt; Pha 2 Import thiết kế chốt 2026-07-07)
**Ngày:** 2026-06-28 · cập nhật 2026-07-07 (Pha 2 §11–§14)
**ADR liên quan:** ADR-033 + **ADR-034** (Import Pha 2) — nối ADR-024 (engine-driven + đọc qua SQL View + lọc phân quyền),
ADR-030 / spec 19 (token ngữ cảnh), ADR-029 / spec 18 (hook `sp_AfterSave_Grid_<T>`), ADR-014 (ConfigCache), ADR-015/14 (Ui_View), Migration 014 (`Ui_Field_Lookup.Code_Field`)
**Trạng thái:** Pha 1 ✅ xong · Pha 2 (Import) 📋 thiết kế chốt — chưa code · Pha 3 (Template) chưa làm. Phân pha ở §8.

---

## 1. Mục tiêu

Một **khóa ngoại** (vd `DM_ChiNhanhNganHang.NganHang_Id → DM_NganHang`) phải hiển thị/được dùng nhất quán ở **4 mặt**:

1. **Form Thêm/Sửa** — chọn ngân hàng bằng tên (ĐÃ CÓ: LookupBox/ComboBox).
2. **Lưới danh sách** — cột hiện **TÊN** ngân hàng (không phải `1`), **lọc/sắp xếp/xuất theo tên**.
3. **Import dữ liệu** — file nhập dùng **Mã** (`VCB`) → hệ resolve sang **Id**, chỉ chấp nhận mã trong tập **đã lọc phân quyền**.
4. **Xuất template import** — workbook gồm **sheet chính** (cột cần nhập) + **mỗi FK 1 sheet phụ** liệt kê `{Mã, Tên}` **đã lọc phân quyền/điều kiện**.

**Nguyên tắc cốt lõi (đã chốt với user):** **một định nghĩa FK duy nhất** — tái dùng `Ui_Field_Lookup` (FK của field trong Edit_Form) làm **nguồn sự thật**, cả 4 mặt cùng tham chiếu. Không khai trùng 4 nơi.

---

## 2. Vì sao tái dùng `Ui_Field_Lookup` (không tạo bảng FK mới)

`Ui_Field_Lookup` (FieldLookupConfig, Migration 014) **đã đủ** mọi thứ FK cần:

| Cột | Vai trò trong FK | Dùng cho mặt nào |
|---|---|---|
| `Source_Name` (table/view/tvf/custom_sql) | bảng đích (`DM_NganHang`) | tất cả |
| `Value_Column` | khóa lưu DB (`Id`) | form, import (đích), template |
| `Display_Column` | tên hiển thị (`Ten`, cho phép expression `CONCAT`) | form, lưới, template |
| **`Code_Field`** | **Mã** (`Ma`) — chính là cầu Mã↔Id | **import**, template |
| `Filter_Sql` | mệnh đề WHERE parameterized (`@TenantId`, token ngữ cảnh) | **lọc phân quyền cả 4 mặt** |
| `Order_By` | sắp xếp options | form, template |

→ Thêm bảng FK song song = **trùng lặp** + nguy cơ lệch. Quyết định: **dùng lại**, mở rộng cách *tham chiếu* tới nó.

---

## 3. Cách 3 mặt còn lại tham chiếu định nghĩa FK

`Ui_Field_Lookup` gắn theo `Field_Id` (field của một Edit_Form). Lưới/import/template **không** ở trong form đó, nên cần đường resolve:

### 3a. Resolve ngầm (mặc định — 0 cấu hình thêm)
`Ui_View` đã có `Edit_Form_Id`. Cột lưới đã có `Column_Id` (map `Sys_Column`). Vậy:

```
Ui_View.Edit_Form_Id → Ui_Form
   ⨝ Ui_Field (fi.Form_Id = form, fi.Column_Id = ViewColumn.Column_Id)
      ⨝ Ui_Field_Lookup (fl.Field_Id = fi.Field_Id)
```

→ Cột FK của lưới **tự tìm** đúng định nghĩa lookup mà form sửa đang dùng. **Không thêm cấu hình.**

### 3b. Resolve tường minh (CHỐT — làm cùng v1)
Khi lưới không có Edit_Form, hoặc cột FK không map `Sys_Column`, hoặc muốn override:
`Ui_View_Column.Props_Json` chứa `{ "fkLookup": { "fieldId": <id> } }` (trỏ thẳng Field_Id mang định nghĩa).
Props_Json **chỉ ở server** (như `Lookup_Sql` — không serialize ra client).

> **Q1 đã chốt (user, 2026-06-28):** v1 làm **cả ngầm (3a) lẫn tường minh (3b)**. Thứ tự ưu tiên khi resolve:
> Props_Json.fkLookup.fieldId (tường minh) → nếu rỗng thì dò ngầm qua Edit_Form_Id + Column_Id.

---

## 4. Lọc phân quyền — tái dùng token ngữ cảnh (spec 19), KHÔNG cơ chế mới

Mọi mặt resolve FK đều chạy `Filter_Sql` **một lần khai** với token `@NguoiDungID`, `@CongTyID_Active`, `@TenantId`
(bind **server-side**, whitelist — spec 19 §2/§4). Hệ quả: **tập ngân hàng hợp lệ ở form = ở lưới = ở import = ở sheet template** — cùng một phạm vi quyền, không lệch.

Ví dụ `Filter_Sql` của FK ngân hàng (chỉ ngân hàng công ty đang chọn được giao):
```sql
IsDeleted = 0 AND (@CongTyID_Active = 0 OR CongTy_Id = @CongTyID_Active)
```

---

## 5. Mặt LƯỚI — hiển thị + lọc/sắp xếp/xuất theo TÊN (user chọn option 2)

**Vấn đề:** resolve FK→tên ở **client lúc render** (hướng B thuần) thì DxGrid **lọc/sort/xuất theo `Id` thô**, không theo tên — không đạt yêu cầu.

> **Cơ chế đã chốt (user, 2026-06-28): KHÔNG ép view tay cho mỗi FK.** Engine **tự sinh JOIN** theo cấu hình FK
> = mặc định (no-code); **SQL View JOIN tay = escape hatch** cho ca hiển thị phức tạp. Cả hai cùng hợp lệ (§5b).

### 5a. Mặc định — engine TỰ JOIN (no-code)

Cột FK ở `Ui_View_Column` khai `Props_Json = {"fkLookup":{"fieldId":<Field_Id của Edit_Form>}}`. Khi đọc dữ liệu,
`ViewRepository.GetDataAsync` resolve định nghĩa FK (Source_Name/Value_Column/Display_Column từ `Ui_Field_Lookup`)
rồi **sinh `LEFT JOIN` + đổi biểu thức SELECT của cột** thành tên (in-place — Model 2):

```sql
SELECT b.[Id] AS [Id],
       [_fk0].[Ten] AS [NganHang_Id],     -- cột FK hiện TÊN (alias = chính tên cột FK)
       b.[Ma] AS [Ma], b.[Ten] AS [Ten], b.[DiaChi] AS [DiaChi]
FROM   [dbo].[DM_ChiNhanhNganHang] AS b
LEFT JOIN [dbo].[DM_NganHang] AS [_fk0] ON [_fk0].[Id] = b.[NganHang_Id]
```

- Tên là **cột thật** trong kết quả → **lọc/sort/xuất theo tên chạy tự nhiên**, **không** view tay, **không** sửa client.
- Cột giữ `Field_Name='NganHang_Id'` + caption "Ngân hàng"; người dùng thấy tên, payload sửa/xóa dùng `Key_Field=Id`.
- **An toàn injection:** mọi identifier (Source_Name/Value/Display + cột) whitelist qua `SafeIdentifierRegex` + `Bracket`
  (như `ViewRepository`/`DynamicLookupRepository` đã làm). Auto-JOIN **chỉ** nhận `Query_Mode='table'` + cột Value/Display
  **đơn** (identifier); Display là **expression** (CONCAT…) hoặc nguồn `custom_sql`/`tvf` → bỏ qua, dùng escape-hatch (§5b).
- **Tham chiếu định nghĩa FK:** tường minh `Props_Json.fkLookup.fieldId` (ưu tiên) → ngầm qua `Edit_Form_Id + Column_Id` (§3).

### 5b. Escape hatch — SQL View JOIN tay (ca phức tạp)

Khi cần hiển thị ghép nhiều bảng / multi-hop / biểu thức: tạo SQL View tay (mẫu `vw_DM_TinhThanhPho`, db/052) rồi
`Ui_View.Source_Type='View'`, `Source_Object='vw_...'`. Engine vẫn **chồng auto-JOIN lên view** nếu view còn cột FK thô.

> **Q1/Q2 đã chốt (user, 2026-06-28):** Q1 = tham chiếu **cả tường minh + ngầm**; Q2 = lưới v1 **chỉ JOIN tên**
> (auto-JOIN hiển thị **không** áp `Filter_Sql`/token — bản ghi đã tồn tại, chỉ tra tên); **phân quyền dòng để pha RLS sau**.
> `Filter_Sql`+token chỉ lọc **tập chọn/import** ở form/import/template (§4), không lọc dòng chính của lưới.

---

## 6. Mặt IMPORT — Mã → Id, validate theo tập đã lọc quyền (Pha 2)

1. File import: cột FK nhập bằng **Mã** (`VCB`). Cột tiêu đề ánh xạ field theo `Ui_Form`/`Ui_View`.
2. Engine đọc định nghĩa FK (§3) → chạy truy vấn `SELECT Code_Field AS code, Value_Column AS id FROM Source_Name WHERE Filter_Sql` (đã bind token) → map `Mã→Id`.
3. Mã không có trong map (sai hoặc ngoài quyền) → **dòng lỗi** với `error_key` (i18n, theo cơ chế ADR-029): `import.fk.code_not_found` (args: cột, mã).
4. Resolve xong → ghi qua `SaveMasterDataCommand` generic (tự audit) như nhập tay.

> Engine import (đọc workbook, map cột↔field, gom lỗi theo dòng) **chưa có trong repo** → là hạ tầng mới của Pha 2; spec con tách sau khi duyệt mô hình.

---

## 7. Mặt TEMPLATE — workbook nhiều sheet, sheet phụ lọc quyền (Pha 3)

1. **Sheet chính** = cột cần nhập (suy từ `Ui_Form`/`Ui_View`), tiêu đề i18n, ô FK ghi **Mã**.
2. **Mỗi cột FK → 1 sheet phụ** `{Mã, Tên}` lấy từ truy vấn lookup **đã lọc `Filter_Sql`+token** (đúng tập người nhập được phép).
3. (Tùy chọn) Excel **Data Validation** ô FK trỏ tới dải sheet phụ → người nhập chọn Mã hợp lệ, giảm sai.
4. Cùng định nghĩa FK với import → template và import **không lệch** danh sách.

---

## 8. Phân pha triển khai

| Pha | Phạm vi | Điểm chạm chính | Quy mô |
|---|---|---|---|
| **0** | Chốt mô hình (spec này) + ADR-033 | docs/spec/25, architecture_decisions.md | ✅ xong |
| **1** | **Lưới**: view JOIN `vw_DM_ChiNhanhNganHang` + cấu hình `Ui_View` (Source_Type=View, cột `TenNganHang`, ẩn `NganHang_Id`, caption i18n) | `db/064` (Data DB view) + `db/065` (Config DB cấu hình) — ĐÃ áp dụng live (Tenant 1) | ✅ xong |
| **2** | **Import** (📋 thiết kế chốt 2026-07-07 — §11–§14): workbook + map cột↔field + resolve Mã→Id (lọc quyền) + composite-key upsert + partial commit + 2 hook proc + log/masking | App/Infra mới (ImportEngine/Resolver) + 3 endpoint + Blazor Wizard + migration | Lớn |
| **3** | **Template**: sinh workbook (sheet chính + sheet phụ FK lọc quyền) + Data Validation | App/Infra (TemplateExport) + endpoint + nút tải template | Lớn |
| **Bắc cầu** | Hợp nhất: cùng truy vấn lookup (Source_Name/Code_Field/Value_Column/Filter_Sql+token) cho Pha 2+3 | `IFkLookupResolver` dùng chung (App/Infra) | — |

**Ownership (theo tiền lệ ADR):** `db/*` + cấu hình ConfigStudio = Codex/Claude theo phân công; Domain/Application/Infrastructure/Api/Blazor = Claude.

---

## 9. Quyết định — trạng thái

- **Q1 — Tham chiếu định nghĩa FK:** ✅ CHỐT — làm **cả ngầm (§3a) lẫn tường minh Props_Json (§3b)**, tường minh ưu tiên.
- **Q2 — Phân quyền dòng ở lưới:** ✅ CHỐT — v1 view JOIN **chỉ hiển thị tên**; phân quyền dòng để **pha RLS sau** (ADR-024).
- **Q3 — Thư viện Excel cho Pha 2/3:** ✅ CHỐT (2026-07-07) — **ClosedXML** (MIT, hợp SaaS thương mại; Data Validation dropdown tốt; không cần đọc `.xls` cũ). EPPlus loại (Polyform Noncommercial — phải mua license); OpenXML SDK loại (low-level, viết Data Validation/style dài dòng).
- **Q4 — `DM_ChiNhanhNganHang` thiếu PK trong metadata** (ADR-032) → Pha 1 khai `Key_Field=Id` cho View để Sửa/Xóa chạy. ✅ làm trong Pha 1.

---

## 10. Liên quan

- **ADR-024** — màn engine-driven, đọc qua SQL View, lọc phân quyền (nền của §5).
- **ADR-030 / spec 19** — token ngữ cảnh `@NguoiDungID`/`@CongTyID_Active` (nền của §4).
- **Migration 014** — `Ui_Field_Lookup.Code_Field` (cầu Mã↔Id của §6/§7).
- **ADR-029 / spec 18** — cơ chế `error_key` + i18n (dùng cho lỗi import §6).
- **db/052** — `vw_DM_TinhThanhPho`/`vw_DM_PhuongXa` (mẫu view JOIN cho §5).
- **spec 18 / ADR-029** — `sp_AfterSave_Grid_<T>` (nền của hook mỗi dòng §12).

---

## 11. Pha 2 — IMPORT chi tiết (chốt 2026-07-07)

> Phạm vi v1 = **Grid phẳng** (TreeGrid pha sau). Thư viện = **ClosedXML**.

### 11.1 Ba endpoint (dry-run bắt buộc)
```
GET  /api/v1/views/{code}/import/template   → ExportImportTemplateQuery  (tải workbook, §7)
POST /api/v1/views/{code}/import/validate    → ValidateImportFileCommand  (upload → preview + lỗi/dòng)
POST /api/v1/views/{code}/import/commit       → CommitImportCommand        (ghi dòng hợp lệ theo importSessionId)
```
Commit **chỉ nhận `importSessionId`** đã validate — không ghi file chưa qua kiểm.

### 11.2 Pipeline kiểm hợp lệ (mỗi ô/dòng, theo thứ tự)
1. **Trim** đầu/cuối mọi ô (chuẩn hoá trước khi validate).
2. Bỏ dòng rỗng hoàn toàn.
3. **Kiểu/định dạng** (số/ngày/bool/độ dài/regex) theo `Sys_Column` + Val_Rule → `import.format.invalid` (cột, giá trị).
4. **Bắt buộc** field required rỗng → `import.required.missing`.
5. **FK resolve Mã→Id** (dùng `IFkLookupResolver` §Bắc-cầu): Mã ngoài tập đã lọc quyền → `import.fk.code_not_found` (cột, mã).
6. **Trùng khoá** (trong file + đụng DB theo mode §11.3) → `import.duplicate.key`.
7. **Validation Engine** (AST Grammar V1) như nhập tay → key rule tương ứng.

Lỗi dùng `error_key` + args (ADR-029), resolve i18n server-side. FK resolve **1 lần/cột** (nạp toàn bộ `{Mã→Id}` vào dictionary) → tránh N+1.

> **Nguồn định nghĩa FK cho import/template (đã code, khác mặt LƯỚI):** lấy **trực tiếp từ `Ui_Field_Lookup` của field
> Edit_Form** (`FieldMetadata.LookupConfig`) trong `IImportMetadataProvider` — KHÔNG dò qua cột `Ui_View` như mặt lưới (§3).
> Nhờ vậy import/template chạy đúng cho **cả màn đọc SQL View JOIN tay** (vd `vw_TC_CongTy` — cột là tên ghép sẵn,
> `Column_Id=NULL`, không mang `fkLookup`): FK vẫn resolve theo cấu hình LookupBox của form. `IFkLookupResolver` chỉ còn lo
> khâu chạy truy vấn Mã↔Id (Data DB, lọc token).
>
> **FK cascade khi import (Phương án B, đã code):** field con lọc theo field cha (vd `PhuongXa_Id` lọc theo Tỉnh) không
> resolve được vì import không có ngữ cảnh chọn cha. Cờ **`Ui_Field_Lookup.Import_Global_Code=1`** (db/074) → import **bỏ
> `Filter_Sql`** → tra Mã con trên toàn bảng; chỉ hợp FK có **Mã con duy nhất toàn cục**. Mã trùng ⇒ `FkCodeMap.HasAmbiguousCode`
> → engine từ chối cả file (`import.fk.ambiguous_code`). Đọc cờ phòng thủ trong `IImportMetadataProvider` (không đụng SELECT
> load form cốt lõi). Set bằng SQL (ConfigStudio chưa có ô).

### 11.3 Upsert theo KHOÁ GHÉP (đã chốt)
- Khoá ghép = các cột tick **`Ui_View_Column.Is_Import_Key=1`** (db/075) — checkbox mỗi cột ở tab Cột (ConfigStudio),
  tick nhiều cột = khoá ghép; **được phép gồm cột FK**. *(Thay `Ui_View.Import_Key_Fields` CSV db/071 — cột đó bỏ, không dùng.)*
- Không tick cột nào ⇒ fallback **insert-only** (an toàn).
- **So khớp SAU khi resolve FK** (khoá FK so trên Id, không so Mã):
  1. Dựng composite key mỗi dòng từ **giá trị đã resolve**, chuẩn hoá **trim + culture-invariant** (để `"CN01"` = `" cn01 "`).
  2. Nạp 1 lần tập hiện có trong phạm vi quyền: `SELECT Id, {keyCols} FROM Source WHERE {FilterSql + token}` → `Dictionary<compositeKey, Id>`.
  3. Trùng key **trong file** → `import.duplicate.key`; key **có trong DB** → UPDATE (`SaveMasterDataCommand(id)`); chưa có → INSERT (`id=null`).
- Ghi qua `SaveMasterDataCommand` ⇒ audit `CreatedBy/At` vs `UpdatedBy/At` tự đúng theo insert/update.

### 11.4 Partial commit (đã chốt)
- Dòng lỗi bị **loại trước khi mở transaction** → không rollback lẫn nhau; dòng hợp lệ vẫn ghi.
- Preview gắn nhãn mỗi dòng: **NEW** / **UPDATE** (kèm Id đang trùng) / **ERROR** (lý do) → người dùng thấy "40 thêm, 12 cập nhật, 3 lỗi" trước khi commit.
- Trả danh sách dòng lỗi + **workbook có thêm cột "Lỗi"** (dựng lại từ log §13, không cần giữ file gốc).

---

## 12. Hai HOOK SQL cho Import (đã chốt 2026-07-07 — hướng ④: tái dùng proc spec 18)

> Không dùng "field SQL gõ thẳng trên Ui_View", không dùng Event Engine (engine đó sinh UI delta cho form client, không tham gia transaction ghi + nuốt lỗi). Tái dùng cơ chế proc spec 18.

### 12.1 Hook MỖI DÒNG = `sp_AfterSave_Grid_<Table>` (spec 18) — đã có sẵn
Import ghi mỗi dòng **qua chính `SaveMasterDataCommand`** ⇒ proc after-save **tự nổ cho từng dòng**, đã cung cấp:
- **Toàn bộ dữ liệu dòng** → `@PayloadJson` (OPENJSON).
- **Ai import** → `@NguoiDungID`.
- **Thao tác** → `@Id = 0` (thêm mới) / `> 0` (update).
- **Chạy trong cùng transaction; hook lỗi → rollback dòng đó** (spec 18 §5) = đúng chốt "rollback dòng".

**Mở rộng hợp đồng proc** thêm ngữ cảnh import (để proc xử lý khác nhập tay nếu muốn):
- `@Source` NVARCHAR(20) = `N'MANUAL'` (DEFAULT) — `'IMPORT'` khi import.
- `@ImportSessionId` UNIQUEIDENTIFIER = NULL (DEFAULT) — phiên import.

> **Zero-regression (đã code):** engine **chỉ thêm** `@Source`/`@ImportSessionId` vào câu EXEC **khi import**
> (`importSessionId != null`). Save tay giữ EXEC cũ (5 tham số) ⇒ proc after-save **chưa nâng cấp v2 không bị
> "too many arguments"**. Chỉ bảng có bật import mới cần regen after-save proc (v2). Codegen ConfigStudio
> (`HookStoreTemplate`) sinh sẵn param v2 + proc `sp_AfterImport_<T>`.

### 12.2 Hook SAU IMPORT = `sp_AfterImport_<Table>` (proc MỚI) — chạy 1 lần
Gọi cuối `CommitImportCommand`, transaction riêng sau khi các dòng đã commit:
```sql
CREATE PROC dbo.sp_AfterImport_<TableCode>
    @ImportSessionId UNIQUEIDENTIFIER,
    @NguoiDungID     BIGINT,
    @TenantId        INT,
    @InsertedCount   INT,
    @UpdatedCount    INT,
    @ErrorCount      INT,
    @RecordIdsJson   NVARCHAR(MAX),   -- mảng Id các dòng đã ghi
    @ImportedAt      DATETIME
AS ...
```
- Lỗi hook sau-import **không** rollback dữ liệu đã ghi (không thể) → ghi lỗi vào `Sys_Import_Log` + cảnh báo người dùng "đã import nhưng post-SQL lỗi".
- **Opt-in `OBJECT_ID`** (không có proc → bỏ qua; app account không cần DDL), **codegen skeleton từ ConfigStudio** như spec 18 §6, dùng lại `IHookStoreCatalog`.

---

## 13. LOG IMPORT + LÀM MỜ (masking) (đã chốt 2026-07-07)

> Vị trí lưu = **Data DB per-tenant** (như `Sys_Audit_Log`). Mỗi bản ghi mang `Correlation_Id` nối sang `logs/icare247-*.log`.

### 13.1 `Sys_Import_Log` (cấp mẻ — 1 dòng / 1 lần import)
`Id`, `ImportSessionId` (GUID), `View_Code`/`Table_Name`, `File_Name`/`File_Size`/`File_Hash`, `Mode` (`insert`/`upsert`), `Total_Rows`/`Inserted`/`Updated`/`Error_Count`/`Skipped`, `Status` (`Validating`/`Committed`/`PartialSuccess`/`Failed`), `Started_At`/`Finished_At`/`Duration_Ms`, `Correlation_Id`, `CreatedBy`/`CreatedAt` (= ai + khi nào import; audit tường minh ADR-022, không dựa DEFAULT).

### 13.2 `Sys_Import_Log_Detail` (**chỉ dòng lỗi** — đã chốt)
`Id`, `Import_Log_Id` (FK), `Row_Number` (số dòng Excel), `Operation` (`INSERT`/`UPDATE`/`ERROR`/`SKIP`), `Record_Id` (null nếu lỗi), `Error_Key`/`Error_Args_Json`/`Field_Name` (ADR-029), **`Row_Json`** (đủ mọi cột của dòng, **đã làm mờ** §13.3).

- Chỉ ghi dòng **ERROR/SKIP** (dòng thành công đã có audit-log JSON-diff riêng — không trùng lặp).
- File "kết quả lỗi" tải về = dựng lại từ bảng này.

### 13.3 Làm mờ log (masking) — bật/tắt theo CỘT
- Cờ **`Sys_Column.Is_Log_Masked`** (bit, default 0) + **`Sys_Column.Log_Mask_Mode`**. Đặt ở **cấp cột** (nhạy cảm ở mọi màn → khai 1 lần; dùng lại cho audit-log sau). "Bật tính năng" = set `Is_Log_Masked=1` cho cột (vd `TienLuong`).
- `Log_Mask_Mode`: `Full` (`"***"`, mặc định) · `Partial` (`"****1234"`, giữ 4 cuối) · `Hash` (`"sha256:9f2a…"`).
- **Nguyên tắc cứng:**
  - Làm mờ **TRƯỚC khi ghi DB** — giá trị nhạy cảm KHÔNG bao giờ chạm bảng log (không phải che lúc hiển thị).
  - Áp cho **cả `Row_Json` lẫn `Error_Args_Json`** (lỗi rơi vào cột nhạy cảm cũng phải mờ).
  - **Không ảnh hưởng ghi dữ liệu thật** — masking chỉ tác động bản sao đưa vào log; bảng đích + hook proc vẫn nhận giá trị thật.

---

## 14. Điểm chạm code Pha 2 (khi triển khai)

**Application/Infrastructure (mới):**
- `IFkLookupResolver` — cầu chung template+import (Mã↔Id, lọc `Filter_Sql`+token). ← "Bắc cầu" §8.
- `IImportTemplateBuilder` (ClosedXML) — sheet chính + sheet phụ FK + Data Validation dropdown.
- `IImportEngine` (ClosedXML) — parse + trim + validate + composite-key upsert plan + masking log.

**Api/Application:**
- `ImportController` + `ExportImportTemplateQuery` / `ValidateImportFileCommand` / `CommitImportCommand`.
- `CommitImportCommand` ghi mỗi dòng qua `SaveMasterDataCommand` (proc `sp_AfterSave_` tự nổ) → cuối mẻ gọi `sp_AfterImport_<T>` + ghi `Sys_Import_Log(_Detail)`.

**Blazor:** `ImportWizard.razor` — upload → preview (NEW/UPDATE/ERROR) → commit → tải file lỗi.

**DB (migration):**
- `Ui_View.Import_Key_Fields` (CSV field-code).
- `Sys_Import_Log` + `Sys_Import_Log_Detail` (Data DB tenant).
- `Sys_Column.Is_Log_Masked` + `Log_Mask_Mode`.
- Mở rộng hợp đồng `sp_AfterSave_Grid_<T>` (thêm `@Source` + `@ImportSessionId`) + skeleton `sp_AfterImport_<T>`.

**Ownership:** db + ConfigStudio = Codex/Claude theo phân công; Domain/App/Infra/Api/Blazor = Claude.
