# Hướng dẫn cấu hình **Bộ lọc liên kết (cascade) + lọc theo tài khoản + đổ giá trị Thêm mới**

> Tính năng mở rộng "panel lọc trái" của lưới nâng cao (`Ui_View_Filter`) cho 3 nhu cầu:
> 1. **Cascade** — các control lọc phụ thuộc nhau (chọn Công ty → nạp Phòng ban → chọn Năm → nạp Nhân viên).
> 2. **Lọc theo tài khoản đăng nhập** — chỉ hiện đơn vị user được phân quyền (token `@NguoiDungID`).
> 3. **Đổ giá trị filter sang form Thêm mới** — bản ghi mới mặc định theo bộ lọc đang chọn, cho sửa lại hoặc khóa.
>
> Cấu hình ở **ConfigStudio › Quản Lý View › tab Bộ lọc**. Không viết code — chỉ điền cấu hình + câu SQL nguồn.
> Tham chiếu: spec `14_VIEW_CONFIG_SPEC.md` §10 · `19_CONTEXT_PARAM_SPEC.md` · ADR-030.

---

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
