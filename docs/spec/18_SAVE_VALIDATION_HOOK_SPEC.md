# Save Validation & After-Save Hook — Stored Procedure per màn (engine-driven)

**Spec:** 18_SAVE_VALIDATION_HOOK_SPEC
**Phiên bản:** 1.0
**Ngày:** 2026-06-22
**Liên quan:** ADR-029 · spec 10 (Resource Key) · spec 14 (View Config) · ADR-024 (engine-driven) · ADR-014 (ConfigCache)

---

## 1. Mục tiêu

Cho **mỗi màn nghiệp vụ engine-driven** (Grid/Form generic — vd Xã/Phường) thêm 2 điểm móc tùy chọn ở
pipeline lưu (`SaveMasterData`):

1. **`spc_Grid_<TableCode>`** — kiểm tra dữ liệu **TRƯỚC khi ghi** (lớp validation cuối ở DB).
   Nhận **toàn bộ field của màn** + **người thực hiện** + **khóa chính `Id`** (`Id = 0` ⇒ thêm mới).
   Trả danh sách lỗi; rỗng ⇒ hợp lệ.
2. **`sp_AfterSave_Grid_<TableCode>`** — hậu xử lý **SAU khi ghi** (cùng bộ tham số, `@Id` là id thật).

Hai store là **lớp bổ sung**, KHÔNG thay thế `ValidationEngine` + unique-check hiện có.

---

## 2. Bài toán "số lượng thông báo vô chừng" → dịch là HỮU HẠN

Số **instance** lỗi là vô hạn (`Mã 00433 đã tồn tại`, `Mã 00565 đã tồn tại`…) nhưng số **loại rule** hữu hạn.
Mỗi rule = **1 key i18n** + tham số.

> **Nguyên tắc cứng: store KHÔNG trả chuỗi tiếng Việt. Store trả `error_key` + `args`.**
> Handler resolve key→text qua `Sys_Resource` (server-side). Số bản dịch = số rule, không phải số lần lỗi.

Khớp convention spec 10: `sys.val.Required/Unique/Length/Compare…` (template `{0}/{1}`) + override per-field
`{table}.val.{field}.{rule}`.

---

## 3. Hợp đồng Stored Procedure (Data DB)

```sql
CREATE PROC dbo.spc_Grid_DM_PhuongXa
    @Id           BIGINT,            -- 0 = thêm mới, >0 = sửa
    @TenantId     INT,
    @NguoiDungID  BIGINT,            -- user id thao tác (claim sub) — token chuẩn, xem spec 19
    @LangCode     NVARCHAR(10),
    @PayloadJson  NVARCHAR(MAX)      -- toàn bộ field động của màn (JSON)
AS
-- SELECT error_key, args_json, field_name, severity   (RỖNG = hợp lệ)
```

`sp_AfterSave_Grid_DM_PhuongXa` — cùng tham số, `@Id` = id thật vừa ghi; thân chạy hậu xử lý (không bắt buộc trả gì).

### 3.1 Result set lỗi

| Cột | Kiểu | Ý nghĩa |
|---|---|---|
| `error_key` | NVARCHAR(200) | Key i18n (`sys.val.Unique`, `dm_phuongxa.val.maphuongxa.Unique`, `sys.val.Invalid`…) |
| `args_json` | NVARCHAR(MAX) | Mảng tham số JSON **theo vị trí token** — vd `["00433","Mã Xã/Phường"]` |
| `field_name` | NVARCHAR(128) | Field để UI tô đỏ; **NULL = thông báo cấp form** (banner/toast) |
| `severity` | NVARCHAR(20) | `error` / `warning` |

> **Quy ước token (đồng nhất `ResourceResolver.ApplyTokens` + db/053 + db/058):**
> `{0}` = giá trị người dùng nhập · `{1}` = nhãn field · `{2}/{3}` = tham số phụ (giới hạn).
> ⇒ `args_json` xếp theo đúng thứ tự đó: `[value, label, ...]`. Handler (SVHOOK-3) thay **mọi** token
> `{n}` theo vị trí mảng. Bộ key chung seed ở `db/058`: `sys.val.Invalid/Forbidden/Conflict/NotFound`
> (cấp form, không args), `sys.val.Integer/Numeric/Regex/Length/MinLength/Range/Compare`, `sys.msg.raw`.

### 3.2 Quy ước truyền dữ liệu (đã chốt: JSON + OPENJSON)

- **Field động** của màn → `@PayloadJson` (parse bằng `JSON_VALUE`/`OPENJSON`).
- **Context cố định** (`@Id/@TenantId/@NguoiDungID/@LangCode`) → tham số rời, có kiểu rõ ràng. Tên `@NguoiDungID` đồng nhất với registry token (spec 19).
- `Id` của command: `null` (insert) **quy đổi `→ 0`** khi truyền vào store.

> Phương án thay thế cân nhắc & loại: **TVP** (chỉ đáng dùng khi save batch nhiều dòng — mất kiểu native);
> **XML** (verbose hơn JSON, không lợi thế); **dynamic param qua sp_executesql** (rủi ro injection — cấm).

---

## 4. i18n — show một thông báo bất kỳ

### 4.1 Thông báo chung biết trước (khuyến nghị)
Vẫn là 1 key, chỉ khác `field_name = NULL`:
```sql
-- Sys_Resource: sys.val.Invalid = "Dữ liệu của bạn không hợp lệ" / "Your data is invalid"
INSERT @err VALUES (N'sys.val.Invalid', NULL, NULL, N'error');
```
Nhóm chung gợi ý: `sys.val.Invalid`, `sys.val.Forbidden`, `sys.val.Conflict`, `sys.val.NotFound`.

### 4.2 Text tự do thật sự (escape hatch — mất đa ngôn ngữ)
```sql
-- Sys_Resource: sys.msg.raw = "{0}" (vi + en)
INSERT @err VALUES (N'sys.msg.raw', N'["Khách hàng đã bị khóa từ 01/06"]', NULL, N'error');
```
Chỉ dùng cho debug / triển khai 1 ngôn ngữ; lặp nhiều → nâng thành key thật ở §4.1.

### 4.3 Nơi resolve (đã chốt: **server-side**)
Handler resolve `error_key` + `args_json` → text qua `IConfigCache.ResolveKeyAsync` (giống code unique
hiện tại — `SaveMasterDataCommandHandler` dòng 77-81). DTO `MasterDataFieldError(FieldCode, Message)` giữ
nguyên (mang text đã dịch). `field_name = NULL` ⇒ trả `FieldCode = ""` → client gom vào banner.

---

## 5. Transaction (đã chốt: bọc chung)

```
── 1 transaction trên Data DB ──────────────────────────────
  a. IF OBJECT_ID('dbo.spc_Grid_<T>','P') IS NOT NULL → EXEC → có lỗi? ROLLBACK, trả 422
  b. INSERT/UPDATE động (tái dùng BuildColumnParams)
  c. IF OBJECT_ID('dbo.sp_AfterSave_Grid_<T>','P') IS NOT NULL → EXEC @Id thật
  d. COMMIT
```
After-save lỗi ⇒ rollback cả bản ghi (toàn vẹn dữ liệu). **DDL không nằm trong transaction này** (xem §6).

---

## 6. Store thiếu → opt-in + codegen (đã chốt)

- **Runtime:** `OBJECT_ID IS NULL` ⇒ **bỏ qua** bước store (màn chưa bật vẫn chạy như cũ — chỉ ValidationEngine
  + unique). App account **không cần quyền DDL**.
- **Tạo store:** **nút "Sinh store cho màn" trong ConfigStudio WPF** (cạnh nơi cấu hình form/table) → quét bảng
  chưa có store → ghi file `db/procs/spc_Grid_<Table>.sql` + `sp_AfterSave_Grid_<Table>.sql` skeleton
  **rỗng pass-through** để review rồi chạy tay.
- Skeleton dùng `IF OBJECT_ID(...) IS NULL CREATE` → **không bao giờ ghi đè** logic đã viết tay.

### 6.1 Skeleton mẫu (rỗng pass-through)

```sql
-- db/procs/spc_Grid_DM_PhuongXa.sql
IF OBJECT_ID('dbo.spc_Grid_DM_PhuongXa','P') IS NULL
EXEC('
CREATE PROC dbo.spc_Grid_DM_PhuongXa
    @Id BIGINT, @TenantId INT, @NguoiDungID BIGINT,
    @LangCode NVARCHAR(10), @PayloadJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    -- Tách field: SELECT @x = JSON_VALUE(@PayloadJson, ''$.MaPhuongXa'') ...
    -- Validate → SELECT error_key/args_json/field_name/severity. Hiện tại: pass-through.
    SELECT TOP 0
        CAST(NULL AS NVARCHAR(200)) AS error_key,
        CAST(NULL AS NVARCHAR(MAX)) AS args_json,
        CAST(NULL AS NVARCHAR(128)) AS field_name,
        CAST(NULL AS NVARCHAR(20))  AS severity;
END');
```
`sp_AfterSave_Grid_DM_PhuongXa` tương tự, thân `SET NOCOUNT ON; RETURN;`.

---

## 7. Điểm chạm code (khi triển khai)

**Backend**
- `IMasterDataRepository` / `MasterDataRepository` — thêm `ProcError` + method gói `validate → ghi → after-save`
  trong 1 transaction trên `_dataDb` (opt-in `OBJECT_ID`).
- `SaveMasterDataCommandHandler` (dòng 84) — sau unique-check, gọi proc-validate, merge `errors`, resolve
  key→text; phần ghi DB chuyển sang method transaction mới.
- DTO `MasterDataFieldError` giữ nguyên.

**Frontend**
- `MasterDataForm.razor` (dòng 170-175) — gom lỗi `FieldCode` rỗng vào banner `_formError` (đừng để rơi).

**Codegen**
- ConfigStudio WPF (Modules.Forms) — nút sinh `db/procs/*.sql` skeleton từ `Sys_Table`.

**i18n seed**
- Migration seed `sys.val.*` + `sys.val.Invalid` + `sys.msg.raw` (vi + en) vào `Sys_Resource`.

---

## 9. Cache tồn tại store — KHÔNG query khi lưu

Mỗi lần lưu **không** chạy `OBJECT_ID`. Cờ tồn tại 2 store đọc qua **`IHookStoreCatalog`** (cache-aside
L1 mem + L2 redis, gắn version-stamp tenant `:v{n}`). Cold-miss = **1 query gộp** 2 `OBJECT_ID` rồi cache
(mem 10′, redis 60′).

- **Nạp sẵn lúc mở list/form:** `GetMasterDataFormInfoQueryHandler` gọi `GetAsync(tableName,…)` → cache ấm trước khi lưu.
- **Khi lưu:** `SaveMasterDataCommandHandler` đọc cờ từ catalog → truyền `hasValidate/hasAfterSave` vào
  `SaveWithHooksAsync` → 0 query khi cache ấm.
- **Tạo store mới:** "Cưỡng chế làm mới cache" (bump `ICacheVersion`) đổi version trong key → mở list lần sau nạp lại.
- **Màn View** (ViewController): chưa pre-warm ở list-open (key store = bảng của **edit form**, không phải view nguồn)
  → cache **tự nạp** ở lần lưu đầu (1 query gộp), các lần sau 0 query.

## 8. Quyết định đã chốt (tóm tắt)

| Hạng mục | Chốt |
|---|---|
| Cơ chế lỗi | Result set `error_key, args_json, field_name, severity` (rỗng = hợp lệ) |
| Truyền data | JSON + `OPENJSON`; context là param rời |
| i18n | Key ở `Sys_Resource`; **server resolve** key→text |
| Transaction | Bọc `spc_ → ghi → sp_AfterSave_` chung 1 transaction Data DB |
| Store thiếu | Runtime bỏ qua (opt-in `OBJECT_ID`); tạo qua **codegen ConfigStudio WPF**, skeleton **rỗng pass-through** |
| Id | `null` (insert) → `0` khi vào store |
| Naming | Trước: `spc_Grid_<TableCode>` · Sau: `sp_AfterSave_Grid_<TableCode>` |
