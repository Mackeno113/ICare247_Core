# 32 — Sinh mã tự động (`Ma`) theo quy tắc cấu hình

> **Trạng thái:** ✅ thiết kế CHỐT (2026-07-23, ADR-036) — DB + proc đã viết (`db/089`, `db/procs/*`),
> **chưa chạy lên DB**, chưa có phần C#/UI, chưa bật cho bảng nào.
> **Bối cảnh:** hầu hết danh mục có cột `Ma NVARCHAR(10..50) NOT NULL` + **filtered unique index**
> `WHERE IsDeleted = 0` (db/037). Hiện **người dùng phải tự gõ mã** — không có bất kỳ cơ chế sinh mã nào
> trong hệ thống (kiểm chứng 2026-07-23: `SinhMa|AutoCode|Sequence|NextCode` = 0 hit trong code).
> Cần một cơ chế **cấu hình được, dùng chung mọi bảng**, không viết tay cho từng màn.

---

## 1. Năm quyết định nền (user chốt 2026-07-23)

| Trục | Chốt |
|---|---|
| **Engine** | Stored proc **generic** `sp_SinhMa` — không hardcode mỗi bảng, không SQL `SEQUENCE` object |
| **Nơi cấu hình** | **Config DB** (`Sys_Ma_Rule` + `Sys_Ma_Rule_Segment`) — DEV khai bằng **ConfigStudio WPF**, đẩy xuống tenant qua ConfigSync (F1, spec 16) |
| **Nguồn của số** | ⛔ **KHÔNG lưu số lớn nhất ở bất cứ đâu.** Mỗi lần sinh phải **quét bảng đích** lấy MAX theo tiền tố |
| **Thời điểm cấp số** | **Hai bước**: xem trước (peek) lúc mở form + cấp thật (consume) lúc Lưu, trong transaction ghi |
| **Phạm vi đợt này** | Dựng **hạ tầng chung**, **chưa bật cho bảng nào** — quy tắc do DEV khai khi có nghiệp vụ thật |

---

## 2. Bất biến #1 — không có bộ đếm

**Sự thật duy nhất về "mã lớn nhất" là dữ liệu đang nằm trong bảng.** Mỗi lần sinh:

```sql
SELECT MAX(<phần số của Ma>) FROM <bảng đích> WHERE <cột mã> LIKE '<tiền tố>%'
```

Vì sao **cấm** bảng bộ đếm (đã từng thiết kế rồi bỏ): bộ đếm lưu sẵn **lệch thực tế** ngay khi có
import dữ liệu cũ · sửa mã bằng tay · xóa/khôi phục bản ghi · restore DB từng phần · chạy song song hai
đường ghi. Lệch một lần là **cấp trùng mã**, và không có cách nào tự phát hiện. Quét trực tiếp thì chậm
hơn một chút nhưng **không bao giờ sai**.

### 2.1. Hệ quả đẹp: phạm vi đánh số = tiền tố của mã

Không cần cột `Reset_Scope`, không cần `Scope_Column`, không cần job cuối năm — **mọi phạm vi tự suy ra
từ chính các đoạn đứng trước `SEQ`**:

| Mã sinh ra | Tiền tố quét MAX | Phạm vi đánh số (tự động) |
|---|---|---|
| `NV260007` | `NV26` | mỗi năm một dãy — sang 2027 tiền tố thành `NV27`, số về `0001` |
| `CT01-PHONG-007` | `CT01-PHONG-` | mỗi (công ty × cấp) một dãy |
| `CT-001` | `CT-` | một dãy duy nhất toàn bảng |

> **Giới hạn cần biết:** phạm vi đánh số **chỉ có thể theo những gì XUẤT HIỆN trong mã**. Muốn "mỗi công ty
> đánh số riêng" thì mã **bắt buộc** phải chứa mã công ty — nếu không, hai công ty sẽ ra cùng chuỗi mã và
> vỡ unique index toàn bảng trên `Ma`. Đây là ràng buộc **hợp lý**, không phải thiếu sót: mã trùng nhau
> giữa các công ty vốn đã là thiết kế sai trên một bảng dùng chung.

### 2.2. Cái giá phải trả

| | Xử lý |
|---|---|
| Quét MAX mỗi lần ghi | `LIKE '<tiền tố>%'` là **sargable** ⇒ index seek theo tiền tố, không quét toàn bảng. **Khi bật quy tắc cho bảng nào, phải bảo đảm bảng đó có index trên cột mã** — filtered unique index sẵn có (`WHERE IsDeleted = 0`) **không đủ** vì ta quét cả bản ghi đã xóa (§5.3) ⇒ thêm `CREATE INDEX IX_<Bang>_Ma ON <Bang>(Ma)` |
| Hai phiên cùng đọc MAX | Khóa phạm vi bằng `sp_getapplock` (§5.2) — thay cho vai trò "atomic" của bộ đếm |

---

## 3. Bất biến #2 — quy tắc là các ĐOẠN, không phải một chuỗi mẫu

Bảng đơn giản chỉ cần vài đoạn; bảng phức tạp ghép mã từ **3 nguồn thông tin trở lên**, và nguồn thường
phải **tra sang bảng khác** (payload có `CongTy_Id = 5`, mã cần chữ `CT01`). Một chuỗi token kiểu
`{F:CongTy_Id}` không diễn tả nổi việc đó, và cũng không dựng được UI cấu hình.

⇒ Quy tắc = **1 dòng cha + N dòng đoạn**, mỗi đoạn một dòng có thứ tự.

### 3.1. `Sys_Ma_Rule` — quy tắc (Config DB, master → tenant)

```sql
CREATE TABLE dbo.Sys_Ma_Rule
(
    Rule_Id       INT IDENTITY(1,1) NOT NULL,
    Table_Code    NVARCHAR(128)  NOT NULL,   -- 'TC_PhongBan' (khớp Sys_Table.Table_Code)
    Column_Code   NVARCHAR(128)  NOT NULL,   -- 'Ma'
    Step          INT            NOT NULL DEFAULT 1,
    Allow_Manual  BIT            NOT NULL DEFAULT 0,   -- 1 = user gõ đè được
    Is_Active     BIT            NOT NULL DEFAULT 1,
    Description   NVARCHAR(500)  NULL,
    -- + 4 cờ ConfigSync: Is_System / Is_Customized / Synced_At / Source_Ver (db/050)
    CONSTRAINT PK_Sys_Ma_Rule PRIMARY KEY (Rule_Id),
    CONSTRAINT CHK_Sys_Ma_Rule_Step CHECK (Step >= 1)
);
CREATE UNIQUE INDEX UQ_Sys_Ma_Rule_Target ON dbo.Sys_Ma_Rule (Table_Code, Column_Code);
```

Bảng Config DB → cột **tiếng Anh**, tiền tố `Sys_`, **không** khối cột auto ADR-022 (khối đó thuộc Data DB —
`db/061` chỉ quét prefix `DM_/TC_/HT_/…`), đúng như `Sys_Context_Param` (db/060) và `Ui_Lookup_Template` (db/083).

### 3.2. `Sys_Ma_Rule_Segment` — các đoạn ghép nên mã

| Cột | Ý nghĩa |
|---|---|
| `Order_No` | thứ tự ghép, 1..n |
| `Segment_Type` | `LITERAL` · `DATE` · `FIELD` · `LOOKUP` · `SEQ` |
| `Text_Value` | `LITERAL`: chữ cố định (`CT`, `-`) — `DATE`: định dạng (`yyyy`, `yy`, `MM`, `dd`, `yyyyMM`) |
| `Field_Code` | `FIELD`/`LOOKUP`: mã cột trong payload đang lưu |
| `Lookup_Table` / `Lookup_Key_Col` / `Lookup_Val_Col` | `LOOKUP`: `SELECT [Val] FROM [Table] WHERE [Key] = <giá trị field>` |
| `Substring_Start` / `Length` / `Pad_Char` / `Pad_Side` / `Text_Transform` | chuẩn hóa giá trị đoạn (cắt · độ rộng cố định · đệm trái/phải · HOA/thường) |

**Ví dụ "3 bộ thông tin ra mã"** — mã phòng ban `CT01-PHONG-007`:

| # | Loại | Cấu hình | Ra |
|---|---|---|---|
| 1 | `LOOKUP` | `CongTy_Id` → `TC_CongTy.Ma` | `CT01` |
| 2 | `LITERAL` | `-` | `-` |
| 3 | `LOOKUP` | `Cap_Id` → `DM_CapPhongBan.Ma` | `PHONG` |
| 4 | `LITERAL` | `-` | `-` |
| 5 | `SEQ` | `Length = 3`, `Pad = '0'` | `007` |

Số `007` đếm riêng cho **từng cặp (công ty, cấp)** — vì tiền tố quét MAX là `CT01-PHONG-`. Không phải khai
thêm gì.

### 3.3. Hai ràng buộc bắt buộc

1. **Đúng 1 đoạn `SEQ`** mỗi quy tắc. 0 đoạn → mọi bản ghi ra cùng một mã (vỡ unique ngay dòng thứ hai);
   2 đoạn → không xác định được đoạn nào mang số khi quét MAX. *(Filtered unique index chặn trường hợp 2;
   trường hợp 0 do WPF + C# chặn — DB không diễn tả được "phải có ít nhất 1 dòng con" bằng constraint.)*
2. **Mọi đoạn đứng TRƯỚC `SEQ` phải có độ dài xác định** — hoặc `LITERAL`, hoặc `Length > 0`. Nếu độ dài
   thay đổi theo bản ghi thì vị trí chữ số đầu tiên không cố định ⇒ không cắt được phần số ra để so MAX.
   *(Kiểm ở WPF + C# lúc lưu quy tắc, kèm thông báo rõ ràng.)*

---

## 4. Hai bước tách bạch — peek vs. consume

**Ô mã không được trống lúc mở form**, nhưng **không được giữ chỗ số**.

| | `sp_XemTruocMa` (peek) | `sp_SinhMa` (consume) |
|---|---|---|
| Gọi khi | Mở form **Thêm mới** | Ngay trước `INSERT`, **trong transaction lưu** |
| Bảng đích | Đọc MAX, **không khóa** | Đọc MAX **có khóa phạm vi** |
| Ghi gì | **Không ghi gì cả** | Không ghi gì ngoài chính bản ghi |
| Kết quả | Mã **dự kiến** (hiển thị) | Mã **chính thức** |
| Nếu hủy form | Không dấu vết — **không thủng số** | — |

**Hệ quả phải chấp nhận:** hai người mở form cùng lúc **thấy cùng một mã dự kiến**; người Lưu sau nhận mã
kế tiếp. Mã hiển thị là *dự kiến*, không phải cam kết — UI phải nói rõ (§7). Đổi lại: **không bao giờ trùng**
và **không thủng số** khi mở-rồi-hủy.

> Vì không có bộ đếm, mã dự kiến ở đây **luôn khớp thực tế trong bảng** tại thời điểm xem — kể cả sau khi
> ai đó vừa import, sửa tay hay xóa bản ghi. Bộ đếm lưu sẵn thì không làm được điều này.

---

## 5. Thuật toán cấp số (`sp_SinhMa`)

**Ba đối tượng DB** (đều ở Data DB, `db/procs/`):

| Đối tượng | Vai trò |
|---|---|
| `fn_GhepMa` | Ghép `tiền tố + số (đệm 0) + hậu tố`. **Dùng chung** 2 proc ⇒ peek và consume không thể ghép lệch |
| `sp_SinhMa` | Cấp thật — khóa phạm vi → quét MAX → ghép → chốt trùng |
| `sp_XemTruocMa` | Xem trước — quét MAX → ghép (không khóa) |

### 5.1. Phân vai C# ↔ SQL

C# dựng sẵn **`@TienTo` / `@HauTo` / `@DoRongSeq`** từ các đoạn rồi truyền xuống; proc chỉ lo phần số.
Lý do proc **không** tự đọc quy tắc: `Sys_Ma_Rule` nằm ở **Config DB**, proc nằm ở **Data DB** — truy vấn
3 phần tên (`[TenConfigDb].dbo.Sys_Ma_Rule`) sẽ **đóng cứng tên database** vào proc, vỡ mô hình
1-DB-1-tenant (ADR-018/025). Thêm nữa, đoạn `FIELD`/`LOOKUP`/`DATE` cần **payload đang lưu** và **giờ địa
phương** — hai thứ proc không có.

> **`DATE` phải dùng giờ ĐỊA PHƯƠNG, không phải UTC.** VN = UTC+7 ⇒ 06:00 ngày 01/01 giờ VN là 23:00 ngày
> 31/12 theo UTC: dùng UTC sẽ cấp mã mang **năm cũ** suốt 7 tiếng đầu mỗi năm. Cột audit vẫn UTC như cũ —
> chỉ đoạn `DATE` của mã là giờ địa phương.

### 5.2. Khóa phạm vi thay cho "atomic" của bộ đếm

```sql
EXEC sp_getapplock @Resource = 'SinhMa|dbo|TC_PhongBan|Ma|CT01-PHONG-',
                   @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 5000;
```

- Chỉ nối tiếp các lần sinh mã **cùng tiền tố** (cùng công ty / cùng năm…), **không chặn toàn bảng**.
- Nhả **tự động** khi `COMMIT`/`ROLLBACK` — không có đường rò khóa.
- **`@@TRANCOUNT = 0` → proc `RAISERROR` và dừng.** Không có transaction thì khóa nhả ngay khi proc kết
  thúc, hai phiên sẽ cùng đọc một MAX ⇒ cấp trùng. Caller **bắt buộc** mở transaction.
  ⚠️ **Việc cho MA-3:** `MasterDataRepository.InsertAsync` hiện gọi `InsertCoreAsync(tx: null)` — đường này
  **chưa có transaction**. Phải mở transaction ở đó, hoặc chỉ cho sinh mã đi qua `SaveWithHooksAsync`.
- ⚠️ **`SET XACT_ABORT OFF` trong `sp_SinhMa`** (khác mọi proc còn lại, vốn `ON`): proc chạy **bên trong
  transaction của engine**; với `XACT_ABORT ON`, bất kỳ lỗi nào cũng đẩy transaction ngoài sang trạng thái
  **uncommittable** (`XACT_STATE = -1`). Đặt trong proc chỉ ảnh hưởng phạm vi proc.

### 5.3. Quét MAX

```sql
SELECT @So = ISNULL(MAX(TRY_CONVERT(BIGINT, SUBSTRING([Ma], @Start, 4000))), 0)
FROM   [dbo].[TC_PhongBan] WITH (UPDLOCK, HOLDLOCK)
WHERE  [Ma] LIKE @Mask ESCAPE '\';       -- @Mask = 'CT01-PHONG-[0-9]%'
```

- **Số nằm cuối mã** (đại đa số): lấy hết phần đuôi rồi `TRY_CONVERT`. Mã có đuôi không phải số
  (`CT01-PHONG-ABC` nhập tay) → `NULL` → `MAX` tự bỏ qua. Cách này **còn xử lý đúng mã đã tràn độ rộng**
  (`...-1000` khi `Length = 3`) — thứ mà mặt nạ cố định số chữ số sẽ bỏ sót ⇒ cấp trùng.
- **Số nằm giữa mã** (có hậu tố): bắt buộc `Length > 0`, mặt nạ dùng `[0-9]` đúng số chữ số.
- **KHÔNG lọc `IsDeleted`** — mã của bản ghi đã xóa mềm **vẫn tính vào MAX** ⇒ không tái dùng mã cũ.
  Tái dùng mã của bản ghi đã xóa là thứ kế toán/kiểm toán không chấp nhận.
- Tiền tố/hậu tố được **escape** ký tự `LIKE` (`\ % _ [`) trước khi ghép mặt nạ; chỉ đi qua tham số
  `sp_executesql`, không nối chuỗi.
- `@Bang`/`@Cot` **có** ghép vào dynamic SQL ⇒ bắt buộc kiểm tra tồn tại thật trong `sys.tables`/`sys.columns`
  rồi `QUOTENAME` (cùng cách `sp_RecomputeTreeOrder`, db/085).

### 5.4. Chốt cuối — mã vừa dựng đã bị chiếm chưa

MAX chỉ bảo đảm **số** chưa dùng, không bảo đảm **mã** chưa bị chiếm: dữ liệu nhập tay có thể đệm số khác
quy tắc (`CT-7` vs `CT-007`) nên **không lọt mặt nạ** nhưng lại đụng đúng chuỗi mã sắp ghi. Proc kiểm
`EXISTS` và nhảy tiếp tới khi trống, **chặn trên 100 vòng** để cấu hình sai không thành vòng lặp vô tận.

### 5.5. Bốn lớp chống trùng (defense-in-depth, `.claude-rules/debugging.md`)

1. Khóa phạm vi `sp_getapplock` — hai phiên cùng tiền tố không thể chen ngang nhau.
2. Quét MAX trên **dữ liệu thật**, gồm cả bản ghi đã xóa mềm.
3. Chốt `EXISTS` trước khi trả mã (§5.4).
4. **Giữ nguyên filtered unique index** trên `Ma` — không vì có engine mà bỏ ràng buộc DB.

---

## 6. Điểm chạm backend — 4 chỗ

| # | Nơi | Việc |
|---|---|---|
| 1 | [`MasterDataRepository.InsertCoreAsync`](../../src/backend/src/ICare247.Infrastructure/Repositories/MasterDataRepository.cs) | Trước `BuildColumnParams`: bảng có quy tắc **và** `values[Cot]` trống → dựng `@TienTo`/`@HauTo`/`@DoRongSeq` từ các đoạn → `EXEC sp_SinhMa` trên **cùng `data`/`tx`** → gán vào `values`. **Phải bảo đảm có transaction** (§5.2) |
| 2 | `ICodeRuleCatalog` (mới) | Cache quy tắc + đoạn theo tenant — **khuôn `IHookStoreCatalog`** (ADR-029): không query Config DB mỗi lần lưu |
| 3 | `POST /api/v1/master-data/{formCode}/ma-du-kien` (mới) | Endpoint riêng cho peek. **Không** nhét vào `GetMasterDataFormInfo`: form-info là config được cache, mã dự kiến là giá trị động — trộn vào sẽ đóng băng mã theo cache. **POST chứ không GET** (khác bản spec đầu): cần giá trị field hiện có trên form cho đoạn FIELD/LOOKUP, mà nhét dữ liệu người dùng vào query string là rò rỉ qua log/lịch sử trình duyệt. Trả **204** khi bảng không có quy tắc |
| 4 | `SaveMasterDataCommandHandler` | Vòng check `IsUnique`: cột tự sinh + giá trị trống đã `continue` sẵn — **kiểm chứng lại khi code**, không sửa mù |

**Đoạn `LOOKUP` tra ở đâu:** C# truy vấn **Data DB** (`SELECT TOP (1) [Val] FROM [Table] WHERE [Key] = @v`)
trong **cùng connection/transaction** đang lưu — thấy được cả dữ liệu vừa ghi trong transaction đó.
Identifier validate bằng whitelist regex + bọc `[]`; giá trị luôn qua tham số Dapper.

**Bộ sinh là service DÙNG CHUNG, không nằm trong repository:** `MaCodeGenerator` (Infrastructure) nhận
`(rule, connection, transaction, values, schema)`. Lý do: có **nhiều hơn một đường ghi** (§9) — nếu nhét
logic ghép mã vào `MasterDataRepository` thì đường thứ hai sẽ phải chép lại và sớm muộn lệch nhau.

**`ICodeRuleCatalog` nuốt lỗi có chủ đích:** tenant chưa chạy `db/089` (thiếu bảng) → log warning và trả
`null` = "không có quy tắc", **không ném**. Ném ở đây sẽ chặn toàn bộ đường lưu danh mục của tenant chưa
migrate. Ngược lại, lỗi **trong lúc sinh mã** (đã có quy tắc) thì **ném ra** cho transaction rollback: ghi
bản ghi thiếu mã hoặc sai quy tắc tệ hơn là báo lỗi cho người dùng.

---

## 7. UI — Web (Blazor)

| Trường hợp | Hành vi ô `Ma` |
|---|---|
| Thêm mới, `Allow_Manual = 0` | **Read-only**, chữ mờ, hiện mã dự kiến + chú thích *"mã dự kiến — cấp chính thức khi lưu"* |
| Thêm mới, `Allow_Manual = 1` | Cho gõ; điền sẵn mã dự kiến; user xóa/gõ đè thì tôn trọng giá trị user |
| Sửa | **Khóa** (cơ chế `LockOnEdit` sẵn có) — đổi mã bản ghi đã phát sinh giao dịch là sai nghiệp vụ |
| Bảng **không có** quy tắc | Giữ nguyên hành vi hiện tại (user tự gõ) — **không hồi quy** |

Đoạn `LOOKUP`/`FIELD` phụ thuộc field khác trên form (vd `CongTy_Id`) ⇒ **đổi field nguồn thì mã dự kiến
phải tính lại** — cùng cơ chế `Reload_Trigger_Fields` của cascade lookup (spec 12). Chưa chọn đủ nguồn thì
ô mã để trống kèm chú thích, **không hiện mã sai**. Hook vào **thay đổi giá trị field nguồn**, không vào
"event đã chạy" — xem §10.3 (nguồn gây đổi là người dùng hay event `SET_VALUE` đều kích hoạt như nhau).

Chú thích và nhãn đều là **key i18n** (spec 10), không hardcode text.

---

## 8. ConfigStudio WPF — màn "Quy tắc sinh mã"

CRUD `Sys_Ma_Rule` + lưới đoạn con, theo khuôn màn `Ui_Lookup_Template` (WPF-15) — mọi cấu hình phải có ô
trên ConfigStudio, **không cấu hình bằng SQL tay** (feedback `config-via-wpf`):

- Chọn **bảng** (từ `Sys_Table`) + **cột** (từ `Sys_Column`, mặc định `Ma`).
- **Lưới đoạn** kéo-thả đổi thứ tự; mỗi dòng chọn loại đoạn → panel thuộc tính đổi theo loại.
- **Ô Preview** dựng mã mẫu ngay khi sửa (client-side, không đụng DB).
- Chặn tại chỗ 2 ràng buộc §3.3 (đúng 1 `SEQ`; mọi đoạn trước `SEQ` có độ dài xác định) + **advisory tĩnh**
  nhắc bảng đích phải có index thường trên cột mã (§2.2). *(Kiểm index sống qua `TargetConnectionString`
  của ConfigStudio là refinement sau — đợt này để nhắc tĩnh.)*
- **Cảnh báo chồng cơ chế (§10.1):** khi lưu, nếu cột mã đã là đích của một event `SET_VALUE`/`CLEAR_VALUE`
  trên bảng này → hiện cảnh báo (không chặn) rằng quy tắc chỉ chạy khi ô để trống.
- Đọc/ghi cột mới bọc try/catch phòng thủ (tenant chưa chạy migration vẫn mở được màn).

**ConfigSync:** thêm descriptor vào thứ tự sync (spec 16 §2) — `Sys_Ma_Rule` UPSERT theo
`Table_Code + Column_Code`, `Sys_Ma_Rule_Segment` là **bảng con theo cha** (xóa-ghi lại theo `Rule_Id` đã
re-link), tôn trọng `Is_System` / `Is_Customized`.

---

## 9. Các đường ghi khác

| Đường ghi | Hành vi |
|---|---|
| **Import Excel** (spec 25) | Đi qua đúng `SaveMasterDataCommandHandler` ⇒ hưởng sẵn. **Quy ước: file có giá trị `Ma` → tôn trọng; ô trống → sinh mới.** Cho phép import dữ liệu cũ giữ nguyên mã — và vì không có bộ đếm, mã sinh sau đó **tự nhảy qua** vùng số mà file vừa chiếm |
| **Lookup "Thêm nhanh"** (AddNew từ dropdown) | ⚠️ **ĐƯỜNG GHI RIÊNG** — `DynamicLookupRepository.InsertAsync`, KHÔNG đi qua `SaveMasterDataCommandHandler` (kiểm chứng 2026-07-24). **Đã nối sinh mã (MA-3b):** bọc transaction (trước đó ghi không transaction) + gọi **cùng** `MaCodeGenerator`. Gotcha: `Source_Name` có thể qualify schema (`dbo.TC_CongTy`) còn `Table_Code` lưu tên trần ⇒ phải tách trước khi tra quy tắc |
| **`spc_Grid_<T>`** (validate) | Chạy **trước** insert ⇒ **chưa thấy mã**. Hook cần kiểm mã thì phải chuyển sang after-save |
| **`sp_AfterSave_Grid_<T>`** | Chạy sau insert, cùng transaction ⇒ thấy mã đã cấp |
| **Batch / proc thuần T-SQL** | Gọi thẳng `sp_SinhMa` được, miễn tự dựng `@TienTo`/`@HauTo` và **mở transaction** |

---

## 10. Tương tác với Event Engine (form events)

Form cho phép control gọi **sự kiện** (`Evt_*`, spec 04/05): `OnChange` / `OnBlur` / `OnLoad` / `OnSubmit`
sinh ra các **action** (`SET_VALUE`, `SET_VISIBLE`, `SET_READONLY`, `CLEAR_VALUE`, `RELOAD_OPTIONS`…).
Cả hai cơ chế đều có thể nhắm vào ô `Ma` ⇒ phải định nghĩa rõ ranh giới.

### 10.0. Không có xung đột GHI — hai cơ chế ở hai tầng khác nhau

| | Event Engine | Sinh mã |
|---|---|---|
| Chạy ở | **Client** (trả `UiDelta` về trình duyệt) | **Server**, tại `InsertCoreAsync` |
| Thời điểm | Khi tương tác form (chưa lưu) | **Đúng lúc ghi DB**, trong transaction |
| Có ghi DB không | **KHÔNG** — mọi action chỉ là delta UI | Có — cấp số + INSERT |

⇒ Không đường nào ghi `Ma` hai lần. Đây cũng là lý do **KHÔNG gộp sinh mã thành một action của Event
Engine**: action chỉ trả delta, không ghi DB; còn mã thật phải cấp **tại thời điểm ghi, trong transaction,
có khóa phạm vi** (§5.2). Biến nó thành action ghi-DB sẽ phá mô hình "delta thuần" và mở lại đúng cái race
mà thiết kế này đang tránh. **Sinh mã bắt buộc ở save-path.**

### 10.1. Event `SET_VALUE` nhắm vào `Ma` — luật nhường

Một form có thể cấu hình `OnChange` → `SET_VALUE` tính `Ma` bằng công thức AST. Nếu bảng đó **cũng** có
quy tắc sinh mã thì hai nguồn cùng muốn quyết `Ma`. Giải quyết bằng **đúng luật đã có** (§6, §9):

> **`values["Ma"]` tới lúc save mà KHÁC RỖNG → tôn trọng; RỖNG → sinh.**

Event chạy trước (client) điền `Ma` ⇒ lúc save code-gen thấy không rỗng nên **nhường**. Hành vi nhất quán,
nhưng **ngầm** ⇒ dễ gây bất ngờ khi cấu hình. Do đó:

- **WPF cảnh báo (MA-6):** khi lưu quy tắc cho một bảng mà bảng đó đã có event `SET_VALUE` (hoặc `CLEAR_VALUE`)
  nhắm vào cùng cột mã → hiện cảnh báo *"Cột này đang được một sự kiện gán giá trị; quy tắc sinh mã sẽ chỉ
  chạy khi ô để trống."* Không chặn cứng (có thể là chủ đích), chỉ báo để người cấu hình biết mình đang chồng 2 cơ chế.

### 10.2. `Ma` là đích của `SET_READONLY` / `SET_ENABLED` — chỉ thẩm mỹ

Sinh mã đã bắt ô `Ma` read-only khi Thêm mới (§7). Event đụng `read-only`/`enabled` của `Ma` chỉ đổi **hiển
thị**, không đụng dữ liệu. Ưu tiên: delta của event thắng (client là nơi cuối cùng). Chấp nhận được — không
có hệ quả về tính đúng của mã.

### 10.3. Preview phải TÍNH LẠI khi event đổi field nguồn (điểm thật)

Đoạn `FIELD`/`LOOKUP` lấy giá trị từ field khác (vd `CongTy_Id`). Khi field nguồn đổi — **dù do người dùng
gõ, dù do một event `SET_VALUE` gán** — mã dự kiến hiện tại **thành sai** (vẫn `CT01-…` trong khi đã sang
công ty khác).

**Cơ chế: `ma-du-kien` gọi ĐỘC LẬP, không nhét vào round-trip của Event Engine.**

- Client giữ **tập field nguồn** của quy tắc (suy từ các đoạn `FIELD`/`LOOKUP` — server trả kèm khi mở form).
- **Bất kỳ thay đổi nào** khiến một field trong tập đó đổi giá trị → client gọi lại `GET …/ma-du-kien`
  (có debounce, cùng tinh thần `Reload_Trigger_Fields` của cascade lookup — spec 12).
- Điều then chốt: hook vào **thay đổi GIÁ TRỊ field**, không hook vào "event đã chạy". Nhờ vậy nguồn gây đổi
  là gì (người dùng, event `SET_VALUE`, prefill Thêm mới) đều kích hoạt tính lại như nhau — không phải liệt
  kê từng loại trigger.

> Vì sao tách khỏi Event Engine: giữ 2 cơ chế độc lập, không đưa code-gen (server, chạm DB) vào EventEngine
> (thuần delta). Đánh đổi: thêm một round-trip `ma-du-kien` khi field nguồn đổi — rẻ (chỉ `SELECT MAX` +
> vài `LOOKUP`), và chỉ xảy ra ở form **Thêm mới** của bảng **có** quy tắc + có đoạn phụ thuộc field.

### 10.4. Thứ tự lúc mở form Thêm mới (`OnLoad` × preview)

Khi mở form mới: vừa gọi `ma-du-kien` để điền `Ma`, vừa có thể có event `OnLoad`. Quy ước để không giẫm chân:

1. Chạy `OnLoad` (prefill/ẩn hiện field) **trước** — nó có thể set chính các field nguồn.
2. **Sau khi** trạng thái form ổn định, mới gọi `ma-du-kien` (đọc field nguồn ở giá trị sau `OnLoad`).
3. Nếu `OnLoad` có `SET_VALUE`/`CLEAR_VALUE` nhắm `Ma`: áp luật §10.1 — giá trị event đặt được coi là "đã có",
   preview **không đè**. Ô để đúng thứ event muốn.

---

## 11. Bảng nào KHÔNG bật

**Mã chuẩn quốc tế/ngành thì tuyệt đối không tự sinh** — sinh ra sẽ phá liên thông dữ liệu:

| Bảng | Mã thực tế | Kết luận |
|---|---|---|
| `DM_QuocGia` | `VN`, `US` (ISO alpha-2/3) | ❌ không bật |
| `DM_DonViTinh` | `KG`, `CAI`, `THUNG` | ❌ không bật |
| `DM_NganHang` | `VCB`, `BIDV` | ❌ không bật |
| `DM_TinhThanhPho` / `DM_PhuongXa` | mã hành chính nhà nước | ❌ không bật |
| `TC_CapCongTy` / `DM_CapPhongBan` | `TONGCT`, `KHOI`, `PHONG` — mã ngữ nghĩa cố định | ❌ không bật |
| `TC_CongTy`, `TC_PhongBan`, `HT_NguoiDung` | mã nội bộ do khách tự đặt | ✅ **ứng viên** (chưa bật đợt này) |

---

## 12. Rủi ro & đánh đổi đã chấp nhận

| Rủi ro | Xử lý |
|---|---|
| Mã dự kiến ≠ mã thật khi có người lưu chen ngang | Chấp nhận — UI ghi rõ "dự kiến". Đổi lại: không thủng số |
| Quét MAX mỗi lần ghi tốn hơn đọc bộ đếm | `LIKE 'tiền tố%'` seek theo index; **bắt buộc có index trên cột mã** khi bật quy tắc (§2.2) |
| Khóa phạm vi giữ tới commit ⇒ ghi tuần tự **cùng tiền tố** | Chấp nhận ở quy mô danh mục; các tiền tố khác nhau vẫn ghi song song |
| Dữ liệu cũ đệm số khác quy tắc (`CT-7` vs `CT-007`) | Chốt `EXISTS` + nhảy số (§5.4) |
| Mã tràn độ rộng (`{SEQ:3}` tới 1000) | Trả **số đầy đủ**, không cắt (cắt = sinh mã trùng). Quét MAX kiểu "lấy hết đuôi" vẫn đọc đúng mã đã tràn |
| Đổi cấu hình đoạn giữa chừng | Tiền tố đổi ⇒ **dãy số mới bắt đầu lại từ 1**. Đây là hành vi đúng (dãy cũ vẫn tra được), nhưng phải nói rõ cho người cấu hình |
| Tenant chưa chạy migration | `ICodeRuleCatalog` bắt lỗi thiếu bảng → coi như "không có quy tắc" ⇒ hành vi cũ, **không vỡ màn** |

---

## 13. Việc phải làm (theo dõi ở `TASKS.md`)

1. ✅ **MA-1** — `db/089_create_sys_ma_rule.sql` (Config DB: `Sys_Ma_Rule` + `Sys_Ma_Rule_Segment`).
2. ✅ **MA-2** — `db/procs/fn_GhepMa.sql` + `db/procs/sp_SinhMa.sql` + `db/procs/sp_XemTruocMa.sql`.
   *(⏳ 4 script CHƯA chạy lên DB — user tự chạy bằng SSMS.)*
3. ✅ **MA-3** — `ICodeRuleCatalog` + `CodeRuleCatalog` (cache) + `MaCodeGenerator` (service dùng chung)
   + điểm chạm `InsertCoreAsync`; `InsertAsync` nay tự mở transaction.
4. ✅ **MA-3b** — nối đường ghi thứ hai: "Thêm nhanh" lookup (`DynamicLookupRepository`) — bọc transaction
   + dùng **cùng** `MaCodeGenerator`.
5. ✅ **MA-4** — `POST …/ma-du-kien` (204 khi không có quy tắc; trả kèm `SourceFields`).
6. ✅ **MA-5** — Blazor: ô `Ma` read-only + mã dự kiến + tính lại khi field nguồn đổi + **gỡ mã dự kiến khỏi
   payload lúc Lưu** (nếu gửi đi, server sẽ tôn trọng nó và không cấp mã thật).
7. ✅ **MA-6** — ConfigStudio WPF: màn "Quy tắc sinh mã" + lưới đoạn + panel theo loại + preview.
8. ✅ **MA-7** — ConfigSync: descriptor `Sys_Ma_Rule` (khóa ghép) + `Sys_Ma_Rule_Segment` (con theo `Order_No`).
9. ✅ **MA-8** — spec 02 (module `Sys_Ma_Rule*`) + spec 16 §2 (thứ tự sync 15/16).

> ⏳ **CHƯA có gì được build hay chạy** — 4 script SQL chưa lên DB; backend/WPF/Blazor chưa compile.
> **Không** bật quy tắc cho bảng nào ở đợt hạ tầng này (user chốt) — bật khi có nghiệp vụ thật yêu cầu.
