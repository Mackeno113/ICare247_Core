# Hướng dẫn cấu hình control Tỉnh/Thành → Xã/Phường (Cascade LookupBox)

> Mục tiêu: cấu hình 2 control liên động — chọn **Tỉnh/Thành** sẽ tự động load lại
> danh sách **Xã/Phường** tương ứng. Thực hiện hoàn toàn trong **ConfigStudio WPF**
> (màn hình *Form Editor → Field Config*), không cần viết code.

---

## 1. Cơ chế cascade — đúng theo runtime

Field **con** (Xã/Phường) chỉ cần **1 thứ**: **Filter SQL** có `@<FieldCodeCha>`.

| Thành phần | Lưu ở | Vai trò |
|---|---|---|
| **Filter SQL** có `@<FieldCodeCha>` | `Ui_Field_Lookup.Filter_Sql` | Vừa lọc Xã theo Tỉnh, **vừa** là tín hiệu để tự reload khi Tỉnh đổi |

**⚠️ Hai quy tắc cốt lõi (đã kiểm chứng trong mã runtime):**

1. **Tên `@param` PHẢI trùng FieldCode của field cha.**
   `DynamicLookupRepository` bind thẳng mọi giá trị field trên form (key = FieldCode)
   vào tham số Dapper cùng tên. Vì vậy `@TinhId` chỉ resolve được khi field Tỉnh có
   `FieldCode = TinhId`. Không có bảng ánh xạ trung gian.
   → Xem [DynamicLookupRepository.cs](../../src/backend/src/ICare247.Infrastructure/Repositories/DynamicLookupRepository.cs).

2. **Reload TỰ ĐỘNG theo MỌI `@param` trong Filter SQL** (chế độ Bảng/View).
   Renderer theo dõi tất cả `@param` field-cha trong Filter SQL; đổi **bất kỳ** cha nào →
   xoá lựa chọn cũ + nạp lại. **Không cần khai ô "Tự reload"** cho trường hợp table-mode.
   → Xem [LookupBoxRenderer.razor `OnParametersSetAsync`](../../src/frontend/ICare247_UI/Components/FieldRenderers/LookupBoxRenderer.razor).

**Luồng runtime:**
1. User chọn Tỉnh → context có `TinhId = <giá trị mới>`.
2. Field Xã có `ReloadTriggerField = TinhId` → renderer phát hiện trigger đổi →
   xoá giá trị Xã đang chọn + gọi lại API `query-dynamic`.
3. API chạy `SELECT ... FROM DM_XaPhuong WHERE TinhId = @TinhId ...`,
   `@TinhId` lấy từ context (`= TinhId` field cha).
4. Danh sách Xã chỉ còn bản ghi thuộc tỉnh đã chọn.

> ⚠️ Chỉ dùng `@Param` — tuyệt đối không nối chuỗi. Repository chặn keyword DDL/DML và
> bind tham số qua Dapper để chống SQL injection.

---

## 2. Chuẩn bị dữ liệu (DB nghiệp vụ)

```
DM_TinhThanh ( TinhId INT PK, Ten_Tinh NVARCHAR, Ma_Tinh NVARCHAR, Is_Active BIT )
DM_XaPhuong  ( XaId   INT PK, Ten_Xa   NVARCHAR, Ma_Xa  NVARCHAR, TinhId INT FK, Is_Active BIT )
```

Bảng con (`DM_XaPhuong`) **bắt buộc** có cột khoá ngoại trỏ về tỉnh (`TinhId`).

---

## 3. Field 1 — Tỉnh/Thành (field cha)

Form Editor → chọn field → panel **Field Config**:

| Mục | Giá trị |
|---|---|
| **FieldCode** | `TinhId` ← *tên này sẽ thành `@TinhId` ở field con — ghi nhớ chính xác* |
| **Editor Type** | `LookupBox` |
| **Chế độ truy vấn** | Bảng / View |
| **Tên bảng** | `DM_TinhThanh` |
| **Cột Value (FK lưu DB)** | `TinhId` |
| **Cột Display** | `Ten_Tinh` |
| **Filter SQL** | `Is_Active = 1 AND Tenant_Id = @TenantId` |
| **ORDER BY** | `Ten_Tinh ASC` |
| **Cho phép tìm kiếm** | ✓ |

> Field cha không cần cấu hình cascade. `@TenantId`, `@Today`, `@CurrentUser` là tham số
> hệ thống tự bơm vào.

---

## 4. Field 2 — Xã/Phường (field con — phần quan trọng)

### 4.1. Nguồn dữ liệu FK

| Mục | Giá trị |
|---|---|
| **FieldCode** | `XaId` |
| **Editor Type** | `LookupBox` |
| **Chế độ truy vấn** | Bảng / View |
| **Tên bảng** | `DM_XaPhuong` |
| **Cột Value** | `XaId` |
| **Cột Display** | `Ten_Xa` |
| **Filter SQL** | `TinhId = @TinhId AND Is_Active = 1` |
| **ORDER BY** | `Ten_Xa ASC` |

> 🔑 `@TinhId` trong Filter SQL **phải trùng FieldCode `TinhId`** của field Tỉnh ở mục 3.
> Tên cột DB bên trái (`TinhId =`) là cột FK trong `DM_XaPhuong`.
> Nếu cột FK trong bảng tên khác (VD `Tinh_Id`) thì viết: `Tinh_Id = @TinhId`
> (trái = tên cột DB, phải = `@` + FieldCode field cha).

### 4.2. Tự động reload — **KHÔNG cần khai (table-mode)**

Chế độ Bảng/View: chỉ cần Filter SQL có `@TinhId` là cascade **tự chạy** — renderer tự
reload khi `TinhId` đổi. **Không phải điền gì thêm.**

Ô **"Nâng cao — Tự reload thủ công theo 1 field"** (`ReloadTriggerField`) chỉ dùng cho:
- Chế độ **TVF / Full SQL** (tham số không nằm trong Filter SQL → auto-reload không dò được), hoặc
- Muốn reload theo field **không** xuất hiện trong Filter SQL.

> **Tương thích ngược:** nếu runtime chưa cập nhật bản auto-reload, tạm điền ô Nâng cao =
> `TinhId` để cascade chạy; sau khi cập nhật, ô này thành tùy chọn.

### 4.3. Kiểm tra

Bấm **▶ Diễn giải** để xem bản tóm tắt cấu hình bằng tiếng Việt trước khi lưu.

---

## 5. SQL sinh ra lúc runtime

Với cấu hình trên, repository build:

```sql
SELECT XaId, Ten_Xa
FROM   DM_XaPhuong
WHERE  TinhId = @TinhId AND Is_Active = 1
ORDER BY Ten_Xa ASC
```

`@TinhId` = giá trị field `TinhId` hiện tại trên form, `@TenantId` được bơm sẵn.

---

## 6. Mở rộng: 3 cấp Tỉnh → Quận/Huyện → Xã/Phường

Lặp lại pattern theo chuỗi — mỗi field con trỏ về **field cha trực tiếp** của nó:

- **Quận/Huyện** (`HuyenId`): Filter SQL `TinhId = @TinhId AND Is_Active = 1`
- **Xã/Phường** (`XaId`): Filter SQL `HuyenId = @HuyenId AND Is_Active = 1`

> Chỉ cần Filter SQL — reload tự động theo `@param`. Đổi Tỉnh → Huyện reload (Huyện bị xoá)
> → vì Huyện đổi → Xã reload theo. Cascade lan truyền tự động qua chuỗi.

---

## 7. Lỗi thường gặp

| Triệu chứng | Nguyên nhân | Cách sửa |
|---|---|---|
| Đổi Tỉnh nhưng Xã **không** load lại | Runtime **chưa cập nhật** bản auto-reload; hoặc TVF/Full SQL không dò được `@param` | Cập nhật runtime; hoặc điền ô *Nâng cao — Tự reload* = `TinhId` (mục 4.2) |
| Xã luôn rỗng | `@param` trong Filter SQL **không trùng FieldCode** field cha | Đổi `@TinhId` cho khớp `FieldCode = TinhId` |
| Xã hiện đủ mọi tỉnh | Filter SQL để trống hoặc sai tên cột FK | Kiểm tra cột `TinhId` trong `DM_XaPhuong` |
| Lỗi "cấu hình không hợp lệ" | Filter SQL chứa keyword bị chặn (`;`, `--`, `EXEC`…) | Chỉ dùng điều kiện WHERE thuần |
| Lần đầu mở form Xã đã sai data | Tỉnh chưa chọn → `@TinhId` null | Nên để Xã `disabled` đến khi có Tỉnh (rule/validation) |

---

## 8. Cơ chế reload (cập nhật) & các vùng cũ

**Reload đa-`@param` (chế độ Bảng/View):** renderer production (`ICare247_UI`) theo dõi
**mọi `@param` field-cha trong Filter SQL** và tự reload khi bất kỳ cha nào đổi — bao cả
cascade **nhiều cha** (VD `WHERE A=@A AND B=@B`). Đây là cơ chế chính, thay cho `ReloadTriggerField` đơn.

**Các vùng cũ trên WPF — KHÔNG dùng:**
- Tag **🔄 reloadOnChange** (`reloadOnChange`) → **đã ẩn**; runtime không tiêu thụ.
- Khung **⚡ Tham số từ field khác** (`filterParams`) → runtime không tiêu thụ.
- Ô **"Tự reload" (single `ReloadTriggerField`)** → chuyển sang mục **Nâng cao**; chỉ cần cho
  TVF/Full SQL hoặc field ngoài Filter SQL.

→ Cascade chuẩn = **chỉ viết Filter SQL** với `@FieldCodeCha`. Xem thêm
[cau-hinh-lookupbox.md §5–§6](../huong-dan-wpf/cau-hinh-lookupbox.md).

---

## 9. Tham chiếu mã nguồn

- Build SQL + bind context: [DynamicLookupRepository.cs](../../src/backend/src/ICare247.Infrastructure/Repositories/DynamicLookupRepository.cs)
- Reload runtime (auto theo `@param`): [LookupBoxRenderer.razor `OnParametersSetAsync`](../../src/frontend/ICare247_UI/Components/FieldRenderers/LookupBoxRenderer.razor)
- Lưu cấu hình: [FieldConfigViewModel.cs](../../src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Modules.Forms/ViewModels/FieldConfigViewModel.cs)
- DTO cấu hình: [FieldLookupConfigRecord.cs](../../src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Core/Data/FieldLookupConfigRecord.cs)
- Dữ liệu phân cấp **cùng 1 bảng** (cha/con) → cân nhắc `TreeLookupBox` (`parentColumn`), xem [24_BLAZOR_CONTROL_RENDERER_SPEC.md](24_BLAZOR_CONTROL_RENDERER_SPEC.md).
