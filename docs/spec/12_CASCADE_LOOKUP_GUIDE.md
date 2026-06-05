# Hướng dẫn cấu hình control Tỉnh/Thành → Xã/Phường (Cascade LookupBox)

> Mục tiêu: cấu hình 2 control liên động — chọn **Tỉnh/Thành** sẽ tự động load lại
> danh sách **Xã/Phường** tương ứng. Thực hiện hoàn toàn trong **ConfigStudio WPF**
> (màn hình *Form Editor → Field Config*), không cần viết code.

---

## 1. Cơ chế cascade — đúng theo runtime

Field **con** (Xã/Phường) cần đúng **2 thứ**:

| Thành phần | Lưu ở | Vai trò |
|---|---|---|
| **Filter SQL** có `@<FieldCodeCha>` | `Ui_Field_Lookup.Filter_Sql` | Câu WHERE lọc Xã theo Tỉnh đang chọn |
| **Tự reload khi field thay đổi** (`ReloadTriggerField`) | `Ui_Field_Lookup` | Khi field Tỉnh đổi giá trị → xoá lựa chọn cũ + load lại Xã |

**⚠️ Hai quy tắc cốt lõi (đã kiểm chứng trong mã runtime):**

1. **Tên `@param` PHẢI trùng FieldCode của field cha.**
   `DynamicLookupRepository` bind thẳng mọi giá trị field trên form (key = FieldCode)
   vào tham số Dapper cùng tên. Vì vậy `@TinhId` chỉ resolve được khi field Tỉnh có
   `FieldCode = TinhId`. Không có bảng ánh xạ trung gian.
   → Xem [DynamicLookupRepository.cs:90-97](../../src/backend/src/ICare247.Infrastructure/Repositories/DynamicLookupRepository.cs).

2. **Reload do `ReloadTriggerField` quyết định** (ô *"Tự reload khi field thay đổi"*),
   **không** phải danh sách tag `reloadOnChange`.
   → Xem [LookupBoxRenderer.razor:141-156](../../src/backend/src/ICare247.Blazor.RuntimeCheck/Components/FieldRenderers/LookupBoxRenderer.razor).

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

### 4.2. Tự động reload (ô *"Tự reload khi field thay đổi"*)

Đây là **bước bắt buộc** để cascade chạy. Trong **Section 3 — Popup grid** (hoặc khung
*reload* tuỳ layout), nhập vào ô **Tự reload khi field thay đổi**:

```
TinhId
```

(chính là FieldCode của field Tỉnh). Để trống = không cascade — Xã chỉ lọc đúng lần
đầu mở form và không phản ứng khi đổi tỉnh.

> Trường này lưu vào `Ui_Field_Lookup.ReloadTriggerField` và là thứ runtime renderer
> thật sự đọc để biết khi nào reload.

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

- **Quận/Huyện** (`HuyenId`):
  - Filter SQL: `TinhId = @TinhId AND Is_Active = 1`
  - Tự reload khi field thay đổi: `TinhId`
- **Xã/Phường** (`XaId`):
  - Filter SQL: `HuyenId = @HuyenId AND Is_Active = 1`
  - Tự reload khi field thay đổi: `HuyenId`

> Đổi Tỉnh → Huyện reload (giá trị Huyện bị xoá) → vì Huyện đổi → Xã reload theo.
> Cascade lan truyền tự động qua chuỗi trigger.

---

## 7. Lỗi thường gặp

| Triệu chứng | Nguyên nhân | Cách sửa |
|---|---|---|
| Đổi Tỉnh nhưng Xã **không** load lại | Quên điền **ReloadTriggerField** (ô *Tự reload*) | Mục 4.2 |
| Xã luôn rỗng | `@param` trong Filter SQL **không trùng FieldCode** field cha | Đổi `@TinhId` cho khớp `FieldCode = TinhId` |
| Xã hiện đủ mọi tỉnh | Filter SQL để trống hoặc sai tên cột FK | Kiểm tra cột `TinhId` trong `DM_XaPhuong` |
| Lỗi "cấu hình không hợp lệ" | Filter SQL chứa keyword bị chặn (`;`, `--`, `EXEC`…) | Chỉ dùng điều kiện WHERE thuần |
| Lần đầu mở form Xã đã sai data | Tỉnh chưa chọn → `@TinhId` null | Nên để Xã `disabled` đến khi có Tỉnh (rule/validation) |

---

## 8. Lưu ý về panel "Tham số từ field khác" & tag "reloadOnChange"

Trên WPF còn 2 vùng cũ: khung xanh **⚡ Tham số từ field khác** (`filterParams`) và
danh sách tag **🔄 reloadOnChange**. Hai vùng này được serialize vào `Control_Props`
JSON nhưng **runtime renderer RuntimeCheck hiện không tiêu thụ chúng** — nó bind
`@param` theo FieldCode và reload theo `ReloadTriggerField` đơn.

→ Để cascade hoạt động chắc chắn, dùng đúng **2 mục ở Mục 4** (Filter SQL với
`@FieldCodeCha` + ReloadTriggerField). Có thể bỏ qua 2 vùng cũ.

---

## 9. Tham chiếu mã nguồn

- Build SQL + bind context: [DynamicLookupRepository.cs](../../src/backend/src/ICare247.Infrastructure/Repositories/DynamicLookupRepository.cs)
- Reload trigger runtime: [LookupBoxRenderer.razor](../../src/backend/src/ICare247.Blazor.RuntimeCheck/Components/FieldRenderers/LookupBoxRenderer.razor) (dòng 137–176)
- Lưu cấu hình: [FieldConfigViewModel.cs](../../src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Modules.Forms/ViewModels/FieldConfigViewModel.cs) (dòng ~2381)
- DTO cấu hình: [FieldLookupConfigRecord.cs](../../src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Core/Data/FieldLookupConfigRecord.cs)
- Dữ liệu phân cấp **cùng 1 bảng** (cha/con) → cân nhắc `TreeLookupBox` (`parentColumn`), xem [11_BLAZOR_CONTROL_RENDERER_SPEC.md](11_BLAZOR_CONTROL_RENDERER_SPEC.md).
