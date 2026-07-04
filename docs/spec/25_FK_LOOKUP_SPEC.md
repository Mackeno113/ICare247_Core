# FK Lookup Spec — Nguồn khóa ngoại dùng chung (lưới · form · import · template)

**Spec:** 25_FK_LOOKUP_SPEC
**Phiên bản:** 0.1 (DRAFT — chờ duyệt mô hình)
**Ngày:** 2026-06-28
**ADR liên quan:** ADR-033 (mới) — nối ADR-024 (engine-driven + đọc qua SQL View + lọc phân quyền),
ADR-030 / spec 19 (token ngữ cảnh), ADR-014 (ConfigCache), ADR-015/14 (Ui_View), Migration 014 (`Ui_Field_Lookup.Code_Field`)
**Trạng thái:** 📋 thiết kế — chưa code. Phân pha ở §8.

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
| **2** | **Import**: hạ tầng đọc workbook + map cột↔field + resolve Mã→Id (lọc quyền) + lỗi theo dòng | App/Infra mới (ImportEngine) + endpoint + Blazor upload + dùng `Ui_Field_Lookup` | Lớn |
| **3** | **Template**: sinh workbook (sheet chính + sheet phụ FK lọc quyền) + Data Validation | App/Infra (TemplateExport) + endpoint + nút tải template | Lớn |
| **Bắc cầu** | Hợp nhất: cùng truy vấn lookup (Source_Name/Code_Field/Value_Column/Filter_Sql+token) cho Pha 2+3 | `IFkLookupResolver` dùng chung (App/Infra) | — |

**Ownership (theo tiền lệ ADR):** `db/*` + cấu hình ConfigStudio = Codex/Claude theo phân công; Domain/Application/Infrastructure/Api/Blazor = Claude.

---

## 9. Quyết định — trạng thái

- **Q1 — Tham chiếu định nghĩa FK:** ✅ CHỐT — làm **cả ngầm (§3a) lẫn tường minh Props_Json (§3b)**, tường minh ưu tiên.
- **Q2 — Phân quyền dòng ở lưới:** ✅ CHỐT — v1 view JOIN **chỉ hiển thị tên**; phân quyền dòng để **pha RLS sau** (ADR-024).
- **Q3 — Thư viện Excel cho Pha 2/3:** ⏳ MỞ — ClosedXML / EPPlus / OpenXML SDK (license + API). Chốt khi tới Pha 2.
- **Q4 — `DM_ChiNhanhNganHang` thiếu PK trong metadata** (ADR-032) → Pha 1 khai `Key_Field=Id` cho View để Sửa/Xóa chạy. ✅ làm trong Pha 1.

---

## 10. Liên quan

- **ADR-024** — màn engine-driven, đọc qua SQL View, lọc phân quyền (nền của §5).
- **ADR-030 / spec 19** — token ngữ cảnh `@NguoiDungID`/`@CongTyID_Active` (nền của §4).
- **Migration 014** — `Ui_Field_Lookup.Code_Field` (cầu Mã↔Id của §6/§7).
- **ADR-029 / spec 18** — cơ chế `error_key` + i18n (dùng cho lỗi import §6).
- **db/052** — `vw_DM_TinhThanhPho`/`vw_DM_PhuongXa` (mẫu view JOIN cho §5).
