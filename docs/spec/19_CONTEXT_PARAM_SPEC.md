# Context Param Spec — `Sys_Context_Param` (registry tham số ngữ cảnh)

**Spec:** 19_CONTEXT_PARAM_SPEC
**Phiên bản:** 1.0
**Ngày:** 2026-06-22
**ADR liên quan:** ADR-030 (architecture_decisions.md) — nối ADR-016 (Ui_View_Filter), ADR-029 (hook store)
**Migration:** `db/060_create_sys_context_param.sql`

---

## 1. Mục tiêu

Một **danh mục token ngữ cảnh** (no-code) mà engine bind **server-side** cho mọi SQL **admin tự viết**:
`Lookup_Sql` của bộ lọc (spec 14 §10), Source SP/SQL của View (spec 14 §9), và — sau khi đồng nhất —
hook store (spec 18). Thêm token mới = thêm 1 dòng cấu hình, **không sửa code**.

### Vì sao tách registry

- Trước đây mỗi token ("người dùng hiện tại") bị hardcode rải rác + **lệch tên**: `@NguoiThucHien`
  (hook store), `@__CreatedBy` (nội bộ engine). Registry quy về **một bộ tên chuẩn dùng chung**.
- Phân quyền dữ liệu (chỉ thấy đơn vị được giao) cần `@NguoiDungID` trong SQL → token phải an toàn
  (không cho client giả mạo) và mở rộng được (thêm `@ChiNhanhID_Active`… không phải build lại).

---

## 2. Quy ước tên (BẮT BUỘC)

| Tiền tố / hậu tố | Nghĩa | Admin được dùng trong SQL config? |
|---|---|---|
| `@__xxx` | tham số **nội bộ engine** (Dapper, SQL máy sinh — `@__CreatedBy`, `@__UpdatedBy`, `@__Id`) | ❌ CẤM |
| `@<tên registry>` | token ngữ cảnh công khai (khai ở `Sys_Context_Param`) | ✅ |
| hậu tố `_Active` | phạm vi do **UI chọn** (company-switcher…), server **validate theo quyền** | ✅ |

Token định danh người dùng chuẩn = **`@NguoiDungID`** (thay `@NguoiThucHien` cũ của hook store — xem §6).

---

## 3. Schema `Sys_Context_Param` (Config DB)

| Cột | Kiểu | Mô tả |
|---|---|---|
| `Param_Id` | int IDENTITY PK | |
| `Param_Name` | nvarchar(100), **unique** | token KHÔNG có `@`, vd `CongTyID_Active` |
| `Sql_Type` | nvarchar(20) | `bigint`\|`int`\|`string`\|`decimal`\|`date`\|`bool` |
| `Source_Kind` | nvarchar(20) | `Claim`\|`Header`\|`ActiveScope` |
| `Source_Key` | nvarchar(100) | tên claim / tên header để đọc |
| `Validate_Sql` | nvarchar(max) NULL | **chỉ** `ActiveScope` — trả 1/0 (bind `@NguoiDungID`, `@val`) |
| `Default_Value` | nvarchar(255) NULL | giá trị khi rỗng/không hợp lệ |
| `Description` | nvarchar(300) NULL | |
| `Is_System` | bit | token lõi nền tảng (đồng bộ master→tenant) |
| `Is_Active` | bit | bật/tắt |

Ràng buộc: `CHK Source_Kind`, `CHK Sql_Type`, `CHK ActiveScope ⇒ Validate_Sql NOT NULL`.

---

## 4. Resolve giá trị (server-side)

| `Source_Kind` | Nguồn | Tin cậy |
|---|---|---|
| `Claim` | JWT claim tên `Source_Key` (vd `sub`, `tenant`) | **Bất biến** — client không sửa được |
| `Header` | HTTP header tên `Source_Key` (vd `X-Lang`) | Đọc thô; rỗng → `Default_Value` |
| `ActiveScope` | HTTP header `Source_Key` (vd `X-Active-CongTy`) → chạy `Validate_Sql(@NguoiDungID,@val)` | **Có kiểm**: 1 = nhận `@val`; rỗng/0 → ép `Default_Value` |

**Bảo mật cốt lõi:** `@NguoiDungID` (Claim) là **ranh giới cứng** — SQL JOIN bảng quyền theo token này.
`*_Active` chỉ **thu hẹp mềm** trong ranh giới đó: client chọn công ty, nhưng `Validate_Sql` đảm bảo
công ty đó nằm trong tập user được giao; sai → 0 = "mọi công ty được phân quyền".

### Cách client gửi `*_Active`

HTTP header `X-Active-<Scope>` (vd `X-Active-CongTy`) đặt bởi company-switcher, gắn vào **mọi** request
View/Lookup. Server đọc + validate; **không** tin giá trị client mà chưa qua `Validate_Sql`.

---

## 5. Bind & whitelist khi chạy

Engine, với một `Lookup_Sql` / Source SQL:
1. Quét các `@param` SQL tham chiếu.
2. Mỗi param:
   - ∈ `Sys_Context_Param` (Is_Active) → bind **giá trị ngữ cảnh** (server resolve §4). KHÔNG lấy từ client.
   - ∈ param filter của View (`Ui_View_Filter`, gồm cha trong `Depends_On`) → bind **giá trị filter** (client, parameterized).
   - còn lại → **không bind → chặn** (lỗi cấu hình / nghi injection).
3. Toàn bộ parameterized qua Dapper (không nối chuỗi).

→ Whitelist = (registry) ∪ (filter params). Đây là mở rộng cơ chế whitelist của spec 14 §9.4.3.

---

## 6. Đồng nhất với hook store (spec 18)

Hook store `spc_Grid_<T>` / `sp_AfterSave_Grid_<T>` trước dùng `@NguoiThucHien`. Để **một bộ tên chuẩn**:
đổi `@NguoiThucHien` → **`@NguoiDungID`** ở: signature 2 proc + `MasterDataRepository.SaveWithHooksAsync`
(EXEC) + `HookStoreTemplate` + spec 18. `@TenantId`/`@LangCode` giữ nguyên (đã trùng registry).

> ⚠️ Đổi **đồng loạt**: sau khi sửa repo/template phải **chạy lại** 2 file proc trên Data DB, nếu không
> proc cũ (`@NguoiThucHien`) sẽ lệch tham số khi EXEC → lỗi lưu.

---

## 7. Cấu hình (ConfigStudio WPF)

Màn **"Tham số ngữ cảnh"** — CRUD `Sys_Context_Param` (chưa code trong đợt thiết kế; seed SQL đủ chạy ban đầu).
Tab "Bộ lọc" (ViewManager): ô `Lookup_Sql` kèm **gợi ý token** liệt kê từ `Sys_Context_Param` Is_Active.

---

## 8. Trạng thái

- **Thiết kế chốt 2026-06-22** (ADR-030). Migration `db/060` + cột cascade `db/059` đã viết.
- **CHƯA code runtime** (resolver server + bind + load options Combo) — xem roadmap `CTXPARAM-*`/`VFILTER-*` trong TASKS.md.
- **Mở:** chốt bảng phân công user↔công ty thật (Validate_Sql `CongTyID_Active` đang là MẪU).
