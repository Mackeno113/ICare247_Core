# Hướng dẫn cấu hình **Bộ lọc liên kết (cascade) + lọc theo tài khoản + đổ giá trị Thêm mới**

> **Tài liệu này dành cho ai?** Người cấu hình hệ thống (Admin, Business Analyst, IT triển khai) —
> không cần biết lập trình, ngoại trừ việc **dán 1 câu SQL có sẵn** (thường do IT kỹ thuật soạn) vào
> 1 ô cấu hình. Nếu bạn là lập trình viên/AI cần tra cứu nhanh, đi thẳng xuống
> [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật).
>
> **Bài này dùng để làm gì?** Làm cho panel lọc bên trái 1 màn danh sách "thông minh" hơn, theo 3
> nhu cầu thường gặp:
> 1. Chọn ô lọc này thì ô lọc khác **tự nạp lại theo** (VD chọn Công ty → Phòng ban tự đổi theo đúng
>    công ty đó) — gọi là **cascade**.
> 2. Ô lọc **chỉ hiện dữ liệu** mà người đang đăng nhập được phép xem — không cần bạn tự lọc tay.
> 3. Khi bấm **Thêm mới**, form mới **tự điền sẵn** giá trị theo đúng bộ lọc đang chọn.
>
> Ví dụ xuyên suốt cả bài: màn **Danh sách nhân viên** — chọn **Công ty** (chỉ hiện công ty được phân
> quyền) → **Phòng ban** tự nạp theo Công ty → nhập **Năm** → xem lưới; bấm Thêm mới thì Công ty/Phòng
> ban đã điền sẵn.

---

## Vài thuật ngữ cần biết trước khi đọc

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **Panel lọc** | Khung các ô lọc nằm bên trái 1 màn danh sách (lưới), dùng để thu hẹp dữ liệu hiển thị. |
| **Cascade (lọc liên kết cha–con)** | Chọn giá trị ở ô lọc "cha" thì ô lọc "con" tự nạp lại danh sách phù hợp, đồng thời tự xóa giá trị con đang chọn. |
| **Token ngữ cảnh** | Giá trị hệ thống **tự điền** vào câu SQL (VD người đang đăng nhập là ai) — không lấy từ dữ liệu người dùng gõ, nên không giả mạo được. |
| **Đổ giá trị (Prefill)** | Khi bấm Thêm mới, 1 ô trên form mới **tự điền sẵn** theo giá trị đang lọc, đỡ phải chọn lại. |
| **Khóa** | Sau khi đổ giá trị sẵn, ô đó bị khóa lại (không cho sửa) hoặc vẫn cho sửa lại — tùy bạn chọn. |
| **Lookup_Sql** | Câu lệnh SQL trả về danh sách để đổ vào 1 ô dropdown lọc — thường IT kỹ thuật soạn sẵn, bạn chỉ dán vào. |
| **Param_Name** | Tên tham số kỹ thuật của 1 ô lọc (VD `@CongTyId`) — dùng để nối ô lọc này với câu SQL nguồn hoặc với ô lọc con. |

---

## Phần A — Làm theo từng bước

### Chuẩn bị trước khi bắt đầu

Trước khi cấu hình, cần biết 2 điều sau (thường IT kỹ thuật đã chuẩn bị sẵn, bạn chỉ cần hỏi lại
nếu là lần đầu đơn vị bạn dùng tính năng này):

1. **2 bước cập nhật cấu trúc cơ sở dữ liệu đã được IT chạy 1 lần** cho toàn hệ thống (không lặp lại
   mỗi màn hình). Nếu đây là lần đầu tiên dùng tính năng lọc liên kết, hỏi IT xác nhận đã làm chưa.
2. **Màn danh sách (View) đang cấu hình phải là "lưới nâng cao"** và đã có panel lọc bên trái. Nếu
   màn của bạn chưa có panel lọc, làm theo bài [cau-hinh-man-quan-ly-view.md](cau-hinh-man-quan-ly-view.md)
   trước, rồi quay lại đây.

---

### Bước 1 — Mở màn cấu hình View, vào tab Bộ lọc

**Mục đích:** đây là nơi khai báo tất cả ô lọc sẽ hiển thị ở panel bên trái màn danh sách.

**Làm gì:**
1. Menu: **Forms › Views (Grid/Tree)**.
2. Mở View cần cấu hình (VD **Danh sách nhân viên**).
3. Chọn tab **Bộ lọc**.

**Bạn sẽ thấy gì:** 1 lưới (có thể đang trống, hoặc đã có sẵn vài dòng) — mỗi dòng trong lưới này
là 1 ô lọc trên panel.

**Lỗi thường gặp:** không thấy tab Bộ lọc, hoặc panel lọc không hiện trên web → View đang ở kiểu
nguồn dữ liệu đơn giản (không phải Sql/Sp), hoặc chưa bật ô **Filter_Panel_Enabled** ở tab Cơ bản.

---

### Bước 2 — Thêm ô lọc "Công ty" (control cha, chỉ hiện đơn vị được phân quyền)

**Mục đích:** cho người dùng chọn Công ty để lọc danh sách nhân viên, nhưng **chỉ liệt kê công ty
mà chính người đó được phép xem** — không hiện toàn bộ công ty trong hệ thống.

**Làm gì:** thêm 1 dòng mới trong tab Bộ lọc:
- **Control**: chọn **Combo**.
- **Param_Name**: gõ `@CongTyId` — tên tham số dùng lại ở câu SQL nguồn của View.
- **LookupSrc**: chọn **dynamic**.
- **Lookup_Sql**: dán câu SQL lấy danh sách công ty (thường do IT kỹ thuật soạn sẵn — xem mẫu câu ở
  [Phần B §3](#3-viết-lookup_sql-nguồn-options-động)). Câu này dùng token `@NguoiDungID` để tự lọc
  theo người đăng nhập, bạn không cần tính toán gì thêm.

**Bạn sẽ thấy gì:** sau khi Lưu, mở lại màn danh sách trên web sẽ thấy ô "Công ty" dạng dropdown,
chỉ liệt kê công ty mà người dùng hiện tại được giao.

**Lỗi thường gặp:** dropdown **rỗng** dù có dữ liệu → **LookupSrc** chưa để `dynamic`, hoặc
**Lookup_Sql** đang trống/sai.

---

### Bước 3 — Thêm ô lọc "Phòng ban" (con, tự nạp theo Công ty — cascade)

**Mục đích:** khi người dùng chọn Công ty ở Bước 2, ô Phòng ban **tự nạp lại** đúng phòng ban của
công ty đó — không bắt người dùng lọc thủ công qua danh sách dài toàn bộ phòng ban.

**Làm gì:** thêm 1 dòng mới:
- **Control**: **Combo**.
- **Param_Name**: `@PhongBanId`.
- **LookupSrc**: **dynamic**.
- **Phụ thuộc**: gõ đúng mã dòng "Công ty" ở Bước 2 (VD `cong_ty`).
- **Lookup_Sql**: dán câu SQL lấy phòng ban theo công ty cha (IT soạn sẵn), có dùng `@CongTyId`
  (đúng tên **Param_Name** của dòng cha).

**Bạn sẽ thấy gì:** ô "Phòng ban" ban đầu trống/khóa; sau khi chọn Công ty, tự nạp danh sách phòng
ban đúng công ty đó. Đổi sang Công ty khác → Phòng ban tự **xóa** lựa chọn cũ và nạp lại danh sách
mới.

**Lỗi thường gặp:** chọn Công ty rồi mà Phòng ban **không nạp** → cột **Phụ thuộc** gõ sai mã dòng
cha, hoặc **Lookup_Sql** của Phòng ban dùng sai tên tham số (không khớp Param_Name của Công ty).

---

### Bước 4 — Thêm ô lọc "Năm" (nhập tay, không liên kết)

**Mục đích:** cho người dùng gõ 1 năm để lọc nhân viên vào làm trong năm đó — không cần dropdown.

**Làm gì:** thêm 1 dòng: **Control** chọn **Number**, **Param_Name** gõ `@Nam`. Không cần điền
LookupSrc/Phụ thuộc.

**Bạn sẽ thấy gì:** ô nhập số "Năm" xuất hiện trên panel lọc, cạnh 2 ô Combo ở trên.

---

### Bước 5 — Cấu hình đổ giá trị sang form Thêm mới

**Mục đích:** khi bấm **+ Thêm mới** ngay từ màn danh sách đang lọc theo Công ty + Phòng ban, form
mới hiện ra không bắt người dùng chọn lại từ đầu — Công ty và Phòng ban đã được điền sẵn theo đúng
bộ lọc đang xem.

**Làm gì:**
- Ở dòng "Công ty" (Bước 2): điền ô **Đổ vào field** = `CongTy_Id` (tên field tương ứng trên form
  Thêm/Sửa nhân viên), bật ô **Khóa** (để người dùng không sửa lại Công ty đã lọc).
- Ở dòng "Phòng ban" (Bước 3): điền **Đổ vào field** = `PhongBan_Id`, **KHÔNG** bật Khóa (để người
  dùng vẫn sửa lại Phòng ban nếu cần).

**Bạn sẽ thấy gì:** bấm **+ Thêm mới** trên màn danh sách đang lọc → form hiện ra với ô Công ty đã
điền sẵn và khóa (xám, không sửa được), ô Phòng ban điền sẵn nhưng vẫn chọn lại được.

**Lỗi thường gặp:** Thêm mới mà form **không tự điền** → chưa khai **Đổ vào field**, hoặc gõ sai tên
field so với form Thêm/Sửa, hoặc đang chưa chọn gì ở bộ lọc lúc bấm Thêm mới.

---

### Bước 6 — Lưu và chạy thử

**Mục đích:** xác nhận toàn bộ 3 ô lọc + cơ chế cascade + prefill hoạt động đúng trước khi bàn giao.

**Làm gì:**
1. Bấm **Lưu** ở màn Views.
2. Nếu vừa đổi cấu hình, bấm nút **↻ Xóa cache** trên màn hình đang cấu hình (hoặc nhờ IT restart
   API) để hệ thống đọc cấu hình mới.
3. Mở lại màn danh sách nhân viên trên web: chọn Công ty → xem Phòng ban tự nạp → nhập Năm → bấm
   **Tìm** → xem lưới kết quả; bấm **+ Thêm mới** → kiểm tra Công ty/Phòng ban đã điền sẵn đúng như
   Bước 5.

**Bạn sẽ thấy gì:** đúng như mô tả ở mục Làm gì.

**Lỗi thường gặp:** đổi cấu hình mà màn **không cập nhật** → quên bấm ↻ Xóa cache / chưa restart API.
Nếu vẫn lỗi sau khi làm đủ các bước trên, xem thêm bảng [Khắc phục sự cố](#6-khắc-phục-sự-cố) ở
Phần B.

---

## Phần B — Tra cứu kỹ thuật

> Dành cho người đã quen cách làm ở Phần A, hoặc lập trình viên/AI cần tra nhanh tên trường kỹ thuật.
> Nội dung dưới đây là bản kỹ thuật đầy đủ — không giải thích lại từ đầu.
>
> Tham chiếu: spec `14_VIEW_CONFIG_SPEC.md` §10 · `19_CONTEXT_PARAM_SPEC.md` · ADR-030.

## ⚠️ Yêu cầu trước (1 lần)

Chạy 2 migration trên **Config DB (`ICare247_Config`)** — idempotent, chạy lại an toàn:

```
db/059_alter_ui_view_filter_cascade.sql   → thêm 3 cột Depends_On / Default_To_Field / Default_Lock vào Ui_View_Filter
db/060_create_sys_context_param.sql       → tạo bảng Sys_Context_Param + seed 4 token lõi
```

Sau khi chạy: **flush cache View** (nút ↻ Xóa cache trên màn `/view/...`, hoặc restart API) để metadata nạp cột mới.

> Nền tảng: View phải là **lưới nâng cao** (`Source_Type = Sp` hoặc `Sql`, `Filter_Panel_Enabled = 1`).
> Xem `cau-hinh-man-quan-ly-view.md` cho cách bật panel lọc + khai control lọc cơ bản.

---

## 1. Khái niệm cốt lõi

### 1.1 Token ngữ cảnh — giá trị server tự điền vào câu SQL

Trong câu SQL bạn viết (`Lookup_Sql` của control, hoặc SP/SQL nguồn lưới), có thể dùng **token** mà **server tự
thay giá trị** — KHÔNG lấy từ client (an toàn, không giả mạo được). Bảng token sẵn có (`Sys_Context_Param`):

| Token | Kiểu | Nguồn | Ý nghĩa |
|---|---|---|---|
| `@NguoiDungID` | bigint | JWT claim `sub` | `NguoiDung_Id` user đăng nhập — **ranh giới bảo mật cứng**, JOIN bảng quyền theo token này |
| `@TenantId` | int | JWT claim `tenant` | Tenant hiện tại |
| `@LangCode` | string | header `X-Lang` | Ngôn ngữ giao diện (mặc định `vi`) |
| `@CongTyID_Active` | bigint | header `X-Active-CongTy` | Công ty đang chọn ở switcher; `0` = mọi công ty được phân quyền |

**Quy ước tên (quan trọng):**
- `@NguoiDungID`, `@TenantId`, `@LangCode` — dùng được trong **mọi SQL bạn viết** (kể cả hook store `spc_Grid_*`).
- Hậu tố `_Active` = phạm vi do UI chọn, server **validate theo quyền** trước khi dùng.
- `@__xxx` (2 gạch dưới) = **nội bộ engine** — **CẤM** dùng trong SQL cấu hình.

### 1.2 Cascade — quan hệ cha → con

Control con khai **`Phụ thuộc`** (cột `Depends_On`) = mã của control cha. Khi cha đổi giá trị → engine **nạp lại
options con** (truyền giá trị cha vào `Lookup_Sql` con) và **xóa giá trị con** đang chọn. Con để trống/khóa cho tới
khi cha có giá trị.

### 1.3 Prefill — đổ giá trị sang form Thêm mới

Control khai **`Đổ vào field`** (cột `Default_To_Field`) = `Field_Code` trên form Thêm/Sửa của View. Khi bấm
**+ Thêm mới**, giá trị filter đang chọn được đổ sẵn vào field đó. **`Khóa`** (`Default_Lock`) bật = field read-only
(không cho sửa); tắt = đổ sẵn nhưng cho sửa lại.

---

## 2. Cấu hình control lọc — tab **Bộ lọc**

Mỗi dòng trong lưới tab Bộ lọc = 1 control = 1 tham số. Các cột liên quan tính năng này:

| Cột | Khi nào điền | Ghi chú |
|---|---|---|
| **Control** | luôn | `Combo` / `MultiSelect` / `Radio` để có dropdown nạp options; `Text`/`Number`/`Date`/`Checkbox` = nhập tay |
| **Param_Name** | luôn | tên tham số trong SQL nguồn lưới, vd `@CongTyId`. Là khóa whitelist (chống injection) |
| **LookupSrc** | Combo/Radio/MultiSelect | `static` (đọc `Sys_Lookup`) \| `dynamic` (chạy `Lookup_Sql`). Cascade/scope **luôn dùng `dynamic`** |
| **Lookup_Sql** | khi `dynamic` | câu `SELECT value, display ...` — xem §3 |
| **Phụ thuộc** (`Depends_On`) | control con | CSV `Filter_Code` cha. VD `cong_ty`. Để trống = độc lập |
| **Đổ vào field** (`Default_To_Field`) | muốn prefill | `Field_Code` trên form Thêm/Sửa. VD `CongTy_Id` |
| **Khóa** (`Default_Lock`) | khi có prefill | bật = read-only · tắt = cho sửa lại |

> **Khoảng giá trị** (từ–đến) vẫn tách **2 dòng** (vd `tu_ngay` Operator `>=` + `den_ngay` Operator `<=`).

---

## 3. Viết `Lookup_Sql` (nguồn options động)

Quy tắc: **`SELECT` trả 2 cột — cột đầu = `value` (gửi lên khi chọn), cột sau = `display` (hiển thị)**. Đặt tên
cột `value`/`display` cho rõ, hoặc cứ để 2 cột đầu theo thứ tự.

```sql
-- Control "Công ty" — chỉ công ty user được phân quyền (scope theo tài khoản)
SELECT c.Id AS value, c.Ten AS display
FROM   dbo.TC_CongTy c
JOIN   dbo.HT_NguoiDung_CongTy q ON q.CongTy_Id = c.Id      -- bảng phân công user↔công ty (đổi theo schema thật)
WHERE  q.NguoiDung_Id = @NguoiDungID                        -- token ngữ cảnh — chỉ đơn vị được giao
  AND  c.IsDeleted = 0
ORDER BY c.Ten;
```

```sql
-- Control "Phòng ban" — phụ thuộc Công ty (cascade) + vẫn scope theo quyền
SELECT p.Id AS value, p.Ten AS display
FROM   dbo.TC_PhongBan p
WHERE  p.CongTy_Id = @CongTyId                              -- @CongTyId = Param_Name của control cha "Công ty"
  AND  p.IsDeleted = 0
ORDER BY p.Ten;
```

**Engine chỉ bind** các tham số: (a) token đăng ký ở `Sys_Context_Param`, (b) `Param_Name` của control **cha** đã
khai ở cột `Phụ thuộc`. Tham số khác → bị chặn. Giá trị luôn parameterized.

> Dùng `@CongTyID_Active` để giới hạn thêm theo công ty đang chọn (khi đã có company-switcher):
> `AND (@CongTyID_Active = 0 OR c.Id = @CongTyID_Active)`.

---

## 4. Ví dụ đầy đủ — View "Danh sách nhân viên"

**Mục tiêu:** chọn Công ty (được phân quyền) → Phòng ban (theo công ty) → Năm → lưới nhân viên vào làm trong năm.
Thêm mới nhân viên thì mặc định Công ty (khóa) + Phòng ban (cho sửa) theo bộ lọc.

### Bước 1 — View nguồn (tab Cơ bản)
- `Source_Type = Sql` (hoặc `Sp`), `Source_Object` = câu SQL/SP nhận `@CongTyId, @PhongBanId, @Nam`:

```sql
SELECT nv.Id, nv.Ma, nv.HoTen, nv.NgayBatDau, pb.Ten AS PhongBan
FROM   dbo.NS_NhanVien nv
JOIN   dbo.TC_PhongBan pb ON pb.Id = nv.PhongBan_Id
WHERE  (@CongTyId   IS NULL OR pb.CongTy_Id   = @CongTyId)
  AND  (@PhongBanId IS NULL OR nv.PhongBan_Id = @PhongBanId)
  AND  (@Nam        IS NULL OR YEAR(nv.NgayBatDau) = @Nam)
  AND  nv.IsDeleted = 0;
```
- Bật `Filter_Panel_Enabled`. `Edit_Form` = form Thêm/Sửa nhân viên (để có nút Thêm mới + prefill).

### Bước 2 — 3 control lọc (tab Bộ lọc)

| Filter_Code | Control | Param_Name | LookupSrc | Phụ thuộc | Lookup_Sql | Đổ vào field | Khóa |
|---|---|---|---|---|---|---|:---:|
| `cong_ty` | Combo | `@CongTyId` | dynamic | — | (SQL công ty ở §3) | `CongTy_Id` | ✓ |
| `phong_ban` | Combo | `@PhongBanId` | dynamic | `cong_ty` | (SQL phòng ban ở §3) | `PhongBan_Id` | ✗ |
| `nam` | Number | `@Nam` | — | — | — | — | — |

> Nhãn mỗi control là **i18n** — bấm 🌐 đặt `Label_Key` (vd `nhan_vien.view.filter.cong_ty.label`).

### Bước 3 — chạy thử
1. Lưu View → flush cache → mở `/view/<View_Code>`.
2. Panel trái: "Công ty" chỉ liệt kê công ty user được giao. Chọn → "Phòng ban" tự nạp theo công ty. Đổi công ty →
   phòng ban tự xóa + nạp lại. Nhập Năm → bấm **Tìm** → lưới nhân viên.
3. Bấm **+ Thêm mới** → form mở với Công ty đổ sẵn **(khóa)** + Phòng ban đổ sẵn **(cho sửa)**.

---

## 5. Thêm token ngữ cảnh mới (no-code)

Cần token mới (vd `@ChiNhanhID_Active`)? Thêm **1 dòng** vào `Sys_Context_Param` (Config DB) — chưa có màn WPV
riêng nên tạm chạy SQL:

```sql
INSERT INTO dbo.Sys_Context_Param (Param_Name, Sql_Type, Source_Kind, Source_Key, Validate_Sql, Default_Value, Description, Is_System)
VALUES (N'ChiNhanhID_Active', N'bigint', N'ActiveScope', N'X-Active-ChiNhanh',
        N'SELECT 1 FROM dbo.HT_NguoiDung_ChiNhanh WHERE NguoiDung_Id=@NguoiDungID AND ChiNhanh_Id=@val AND IsDeleted=0',
        N'0', N'Chi nhánh đang chọn', 1);
```

| Cột | Ý nghĩa |
|---|---|
| `Source_Kind` | `Claim` (đọc JWT) · `Header` (đọc HTTP header) · `ActiveScope` (header + validate theo quyền) |
| `Source_Key` | tên claim / tên header |
| `Validate_Sql` | **bắt buộc với ActiveScope** — trả 1/0; sai → ép `Default_Value`. Bind sẵn `@NguoiDungID` + `@val` |

Sau khi thêm → flush cache. Token tự dùng được trong mọi `Lookup_Sql`/SQL nguồn.

---

## 6. Khắc phục sự cố

| Hiện tượng | Nguyên nhân / cách xử lý |
|---|---|
| Combo **rỗng** dù có dữ liệu | `LookupSrc` chưa đặt `dynamic`, hoặc `Lookup_Sql` trống / sai cột. Kiểm tra SELECT trả ≥1 cột |
| Combo con **không nạp** khi chọn cha | Cột **Phụ thuộc** chưa khai mã cha đúng, hoặc `Lookup_Sql` con tham chiếu `@param` không phải Param_Name của cha |
| "chọn mục phụ thuộc trước" | Đúng hành vi — control con chờ cha có giá trị |
| Công ty hiện **hết** (không lọc quyền) | `Lookup_Sql` thiếu JOIN bảng quyền theo `@NguoiDungID`, hoặc bảng phân công user↔công ty chưa đúng tên |
| Thêm mới **không đổ giá trị** | Chưa khai **Đổ vào field**, hoặc `Field_Code` không khớp field trên `Edit_Form`, hoặc filter đang trống |
| Đổi cấu hình mà màn **không cập nhật** | Chưa flush cache View (nút ↻) / chưa restart API |

---

## 7. Giới hạn hiện tại

- **Company-switcher chưa có UI** → header `X-Active-CongTy` chưa được gửi, nên `@CongTyID_Active` luôn = `0`
  (mọi công ty được phân quyền). Lọc theo **người dùng** (`@NguoiDungID`) vẫn áp đầy đủ. Khi có switcher, chỉ cần
  gửi header — không phải sửa cấu hình.
- **Validate_Sql của `CongTyID_Active`** trong seed (`db/060`) đang là **MẪU** tham chiếu `HT_NguoiDung_CongTy` —
  cần đổi đúng bảng/cột phân công user↔công ty thật của hệ thống.
- **Màn quản lý `Sys_Context_Param` trên ConfigStudio** chưa làm — tạm thêm token bằng SQL (§5).
- `MultiSelect → IN`: control nạp options đa chọn đã chạy; SP/SQL nguồn cần `Operator = IN` ở dòng filter tương ứng.
