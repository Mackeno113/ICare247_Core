# Hướng dẫn cấu hình **LookupBox** — tham chiếu đầy đủ từng mục (ConfigStudio)

> **Đối tượng:** người cấu hình form trong ConfigStudio (WPF).
> **Phạm vi:** giải thích **chi tiết từng ô** trong tab **Control Props** khi `Editor_Type = LookupBox` (và biến thể `TreeLookupBox`), kèm **ví dụ minh họa theo từng trường hợp** (§6).
> **Liên quan:**
> - Tổng quan editor types → [09_FIELD_CONFIG_GUIDE.md §3.7](../spec/09_FIELD_CONFIG_GUIDE.md)
> - Cơ chế cascade Tỉnh→Xã → [12_CASCADE_LOOKUP_GUIDE.md](../spec/12_CASCADE_LOOKUP_GUIDE.md)
> - Thêm mới entity ngay trên control → [13_LOOKUP_ADD_NEW_GUIDE.md](../spec/13_LOOKUP_ADD_NEW_GUIDE.md)
> - Dựng lưới tham chiếu (FK) đầu-cuối → [cau-hinh-luoi-tham-chieu.md](cau-hinh-luoi-tham-chieu.md)

> **⚠ Cập nhật cơ chế reload (2026-07):** LookupBox chế độ **Bảng/View** nay **tự reload theo MỌI `@param` trong Filter SQL** — không cần khai reload thủ công. Ô multi *"🔄 Tự động reload"* đã **ẩn** (runtime không dùng); ô single *"Tự reload"* chuyển thành mục **Nâng cao** (chỉ cần cho TVF/Full SQL hoặc field ngoài Filter SQL). Xem §5.

---

## 0. LookupBox là gì

`LookupBox` là control chọn **khóa ngoại (FK)**: hiển thị tên dễ đọc cho người dùng nhưng **lưu xuống DB một giá trị `int` (Id)**. Dùng cho: Phòng ban, Nhà cung cấp, Khách hàng, Ngân hàng, Tỉnh/Xã… — bất kỳ cột FK trỏ tới một bảng nghiệp vụ.

- **Cột bind** phải là kiểu `int`/`bigint` (cột FK trong bảng đang cấu hình form).
- **Biến thể `TreeLookupBox`**: cùng cơ chế nhưng nguồn có cột cha/con → hiển thị dạng cây.
- **Nơi lưu cấu hình:** mỗi LookupBox sinh một bản ghi `Ui_Field_Lookup` (nguồn dữ liệu, popup, reload, thêm-mới) + phần `columns` trong `Control_Props_Json` của `Ui_Field`.

> Mọi nhãn cột trong popup phải qua **Resource Key i18n** (không gõ chữ cứng) — xem [Popup grid](#3-popup-grid).

---

## 1. Nguồn dữ liệu FK

Phần khai báo **lấy danh sách lựa chọn ở đâu**.

### 1.1. Chế độ truy vấn (Query Mode)

| Chế độ | Khi nào dùng | Lưu (`queryMode`) |
|---|---|---|
| **Bảng / View** *(khuyến nghị)* | Lấy thẳng từ 1 bảng hoặc View (View có thể JOIN sẵn). 90% trường hợp. | `table` |
| **Function (TVF)** | Cần truyền nhiều tham số / logic động mà WHERE đơn giản không đủ. | `function` |
| **SQL tùy chỉnh** | JOIN phức tạp, không tiện tạo View. | `custom_sql` |

Đổi chế độ → panel bên dưới đổi theo. Hai ô **Cột Value** và **Cột Display** dùng chung cho cả 3 chế độ.

### 1.2. Hai ô dùng chung

| Ô | Ý nghĩa | Ví dụ |
|---|---|---|
| **Cột Value — FK lưu vào DB** | Cột Id sẽ **ghi xuống DB** (chính là giá trị field). | `Id`, `PhongBan_Id` |
| **Cột Display (hiển thị)** | Cột hiển thị trong ô input và (mặc định) trong popup. | `Ten`, `Ten_PhongBan` |

### 1.3. Chế độ **Bảng / View**

| Ô | Ý nghĩa | Ví dụ |
|---|---|---|
| **Tên bảng hoặc View** | Bảng nguồn hoặc View. View nên chứa sẵn JOIN nếu cần tên đẹp. | `DM_NganHang`, `vw_PhongBan_Full` |
| **Filter SQL / WHERE** *(tùy chọn)* | Điều kiện lọc bổ sung. Ghép vào WHERE (parameterized — **không nối chuỗi tay**). | `Is_Active = 1 AND Tenant_Id = @TenantId` |
| **Sắp xếp (ORDER BY)** | Thứ tự hiển thị trong popup. | `Ten_PhongBan ASC` |

**Tham số dùng được trong Filter SQL:**

| Loại | Token | Giá trị runtime |
|---|---|---|
| Hệ thống | `@TenantId` | Tenant hiện tại (server tự bơm) |
| Hệ thống | `@Today` | Ngày hiện tại (không giờ) |
| Hệ thống | `@CurrentUser` | Username đang đăng nhập |
| **Từ field khác** | `@{FieldCode}` | Giá trị field cùng form. **Tên sau `@` phải TRÙNG ĐÚNG `Field Code` của field cha** (phân biệt hoa/thường). Khi field đó đổi → lookup **tự lọc lại + reload** (cascade). |

> **Quy tắc vàng của cascade:** `@Ten` trong Filter SQL không có bảng ánh xạ — nó bind thẳng theo `Field Code`. Đặt `@TinhThanhPho_Id` thì phải có field cha `Field Code = TinhThanhPho_Id`. Chi tiết → [§5](#5-cascade--reload-theo-field-cha) và [§6](#6-ví-dụ-minh-họa-theo-từng-trường-hợp).

### 1.4. Chế độ **Function (TVF)**

| Ô | Ý nghĩa | Ví dụ |
|---|---|---|
| **Tên Function (TVF)** | Table-Valued Function trong DB. | `fn_GetPhongBanHieuLuc` |
| **Tham số hàm (theo thứ tự)** | Danh sách tham số — **thứ tự phải khớp định nghĩa hàm**. Mỗi dòng: `@Tên` · **Nguồn** (`field`/`system`) · `Field/System key`. | `@NgayHieuLuc` · `field` · `NgayVaoLam` |

Bấm **+ Thêm** để thêm dòng tham số; **✕** để xoá.

### 1.5. Chế độ **SQL tùy chỉnh**

| Ô | Ý nghĩa |
|---|---|
| **SELECT SQL** | Câu `SELECT` đầy đủ. **Bắt buộc** có alias **khớp** *Cột Value* và *Cột Display*. Hỗ trợ `@TenantId @Today @CurrentUser`. |

```sql
SELECT p.PhongBan_Id, p.Ten_PhongBan
FROM   DM_PhongBan p
JOIN   DM_ChiNhanh c ON c.ChiNhanh_Id = p.ChiNhanh_Id
WHERE  p.Is_Active = 1 AND p.Tenant_Id = @TenantId
ORDER BY p.Ten_PhongBan
```

### 1.6. **Cho phép tìm kiếm**

Checkbox bật ô tìm kiếm tăng dần trong popup. Mặc định **bật**. Tắt khi danh sách rất ngắn.

---

## 2. EditBox hiển thị

Quy định **hiển thị thế nào trong ô input sau khi đã chọn** một bản ghi.

| Chế độ (`editBoxMode`) | Hiển thị | Ô phụ |
|---|---|---|
| **TextOnly** *(mặc định)* | Chỉ cột Display. | — |
| **CodeAndName** | Mã ngắn + tên (vd `PB01 · Phòng Kế toán`). | hiện thêm **Cột mã code (CodeField)** — vd `PhongBan_Code` |
| **Custom** | Template hiển thị riêng. | *(chi tiết để sau — §7)* |

---

## 3. Popup grid

Cấu hình **bảng chọn** bật lên khi người dùng mở LookupBox.

### 3.1. Kích thước

| Ô | Ý nghĩa | Mặc định / khoảng |
|---|---|---|
| **Chiều rộng popup (px)** | Bề ngang dropdown grid. | 600 (200–1200) |
| **Chiều cao popup (px)** | Bề cao dropdown grid. | 400 (150–800) |

### 3.2. Cột hiển thị trong popup

Danh sách cột trong bảng popup. **Để trống = chỉ hiển thị cột Display đơn giản** (không bật grid). Mỗi dòng:

| Thành phần | Ý nghĩa |
|---|---|
| **▲ / ▼** | Đổi thứ tự cột. |
| **Tên cột DB** | Tên cột nguồn (vd `PhongBan_Code`). |
| **Resource Key (i18n)** | Khóa i18n cho tiêu đề cột — **bắt buộc qua i18n** (vd `phongban.col.ma_phong_ban`). |
| **Rộng (px)** | Bề rộng cột (40–400). |
| **✕** | Xoá cột. |

Bấm **+ Thêm cột** để thêm dòng.

### 3.3. *(TreeLookupBox)* Cột cha — Parent Column

Chỉ hiện khi `Editor_Type = TreeLookupBox`. Ô **`ParentColumn`** *(bắt buộc)*: tên cột chứa Id cha trong bảng nguồn (vd `Parent_Id`) để dựng cây cha/con.

---

## 4. Thêm mới entity

Cho phép tạo bản ghi nguồn ngay trên control mà không rời form.

| Ô | Ý nghĩa |
|---|---|
| **Cho phép thêm mới (➕)** | Bật nút "Thêm mới" trong dropdown. Lưu `Ui_Field_Lookup.Allow_Add_New`. |
| **Form Code dialog thêm mới** *(bắt buộc khi bật)* | `Form_Code` của `Ui_Form` bound đúng bảng nguồn → mở làm dialog nhập liệu. Lưu `Ui_Field_Lookup.Add_Form_Code`. |

Chi tiết luồng + giới hạn → [13_LOOKUP_ADD_NEW_GUIDE.md](../spec/13_LOOKUP_ADD_NEW_GUIDE.md).

---

## 5. Cascade / reload theo field cha

**Nguyên tắc (đã kiểm chứng trong mã runtime):**

1. **Lọc theo field cha** = viết `@{FieldCodeCha}` trong Filter SQL. Runtime truyền **toàn bộ giá trị field của form** vào câu SQL nên **bao nhiêu `@param` cũng bind đúng** — lọc theo **nhiều cha** hoạt động ngay.
2. **Reload (chế độ Bảng/View)** = **TỰ ĐỘNG**. Renderer theo dõi **mọi `@param` trong Filter SQL**; đổi **bất kỳ** field cha nào → xoá lựa chọn cũ + nạp lại danh sách. **Không cần khai gì thêm.**

| Cách khai | Trạng thái | Khi nào dùng |
|---|---|---|
| `@{FieldCode}` trong **Filter SQL** | ✅ **Chính** — vừa lọc vừa tự reload (mọi @param) | Mọi cascade chế độ Bảng/View (1 hoặc nhiều cha) |
| Ô multi **"🔄 Tự reload theo nhiều field cha (Multi-Trigger)"** (`Reload_Trigger_Fields`, Migration 068) | ✅ Runtime **CÓ** dùng — hợp với @param Filter SQL | TVF/Full SQL, hoặc field cha **không** có trong Filter SQL, cần reload theo nhiều cha |
| Ô **"Nâng cao — Tự reload thủ công theo 1 field"** (`ReloadTriggerField`) | ✅ Tùy chọn — 1 field | Trường hợp đơn giản 1 cha ngoài Filter SQL |

> **Tương thích ngược:** nếu app runtime **chưa được cập nhật** bản auto-reload-theo-Filter-SQL, hãy điền tạm ô **Nâng cao** = `FieldCode` field cha để cascade chạy ngay. Sau khi cập nhật, ô này thành tùy chọn.

---

## 6. Ví dụ minh họa theo từng trường hợp

> Ký hiệu: **[Cơ bản]** = tab Cơ bản; **[CP]** = tab Control Props. Cột trái Filter SQL = **cột thật trong bảng nguồn**, `@` phải = **Field Code field cha**.

### VD1 — LookupBox cơ bản (không cascade): chọn Ngân hàng
- [Cơ bản] Editor Type: `LookupBox`; cột bind: `NganHang_Id` (int).
- [CP] Query Mode: **Bảng/View** · Tên bảng: `DM_NganHang` · Value: `Id` · Display: `Ten` · Filter SQL: `Is_Active = 1 AND Tenant_Id = @TenantId` · ORDER BY: `Ten ASC`.

### VD2 — Cascade **1 cha**: Tỉnh/Thành → Xã/Phường *(đúng case đang làm)*
- Field cha **Tỉnh** (`TinhThanhPho_Id`): LookupBox nguồn `DM_TinhThanhPho`, Value `Id`, Display `Ten`. Ghi nhớ **Field Code = `TinhThanhPho_Id`**.
- Field con **Xã** (`PhuongXa_Id`):
  - Tên bảng: `DM_PhuongXa` · Value `Id` · Display `Ten`.
  - **Filter SQL:** `TinhThanhPho_Id = @TinhThanhPho_Id`
    *(vế trái = cột FK **thật** trong `DM_PhuongXa`; nếu tên khác, VD `Tinh_Id`, thì `Tinh_Id = @TinhThanhPho_Id`)*.
  - Reload: **không cần** (table-mode tự reload theo `@TinhThanhPho_Id`). *(Runtime cũ chưa cập nhật → điền ô Nâng cao = `TinhThanhPho_Id`.)*

### VD3 — Cascade **nhiều cha**: Phòng ban + Cấp bậc + Biểu thuế → danh sách
- 3 field cha có Field Code: `PhongBan_Id`, `CapBac_Id`, `BieuThue_Id`.
- Field list con — **Filter SQL:**
  ```sql
  PhongBan_Id = @PhongBan_Id AND CapBac_Id = @CapBac_Id AND BieuThue_Id = @BieuThue_Id
  ```
- Đổi **bất kỳ** field nào trong 3 → list tự reload. Không cần điền ô Nâng cao.

### VD4 — Function (TVF): phòng ban còn hiệu lực theo ngày
- Query Mode: **Function** · Tên TVF: `fn_GetPhongBanHieuLuc` · Value `PhongBan_Id` · Display `Ten_PhongBan`.
- Tham số: `@NgayHieuLuc` · `field` · `NgayVaoLam`.
- ⚠ Reload TVF **không** tự dò được (auto-reload chỉ đọc Filter SQL) → điền ô **Nâng cao** = `NgayVaoLam`.

### VD5 — SQL tùy chỉnh: phòng ban JOIN chi nhánh
- Query Mode: **SQL tùy chỉnh** · SELECT như [§1.5]. Value `PhongBan_Id`, Display `Ten_PhongBan`.
- Reload theo field cha (nếu có) → điền ô **Nâng cao** (auto-reload không dò trong SELECT tự viết).

### VD6 — TreeLookupBox: phòng ban phân cấp
- Editor Type: `TreeLookupBox` · nguồn `DM_PhongBan` (có cột `Parent_Id`).
- [CP] **Parent Column** = `Parent_Id` · Value `PhongBan_Id` · Display `Ten_PhongBan`.

### VD7 — EditBox CodeAndName: hiện "mã · tên"
- EditBox mode: **CodeAndName** · **CodeField** = `PhongBan_Code` → ô input hiện `PB01 · Phòng Kế toán`.

### VD8 — Thêm mới entity ngay trên control
- Bật **Cho phép thêm mới** · **Form Code** = form nhập liệu của bảng nguồn (VD `NGANHANG_FORM`). Xem [13_LOOKUP_ADD_NEW_GUIDE.md](../spec/13_LOOKUP_ADD_NEW_GUIDE.md).

---

## 7. Trường hợp chưa chắc chắn — liệt kê (chi tiết để sau)

> Các mục dưới đây **chưa xác minh chạy đầy đủ ở runtime** hoặc còn hở — ghi lại để hoàn thiện sau, chưa hướng dẫn chi tiết.

1. **"Điều kiện đổi bảng nguồn" (`dataSourceConditions`)** — panel *"⊠ Điều kiện đổi bảng nguồn"* cho phép đổi bảng nguồn theo giá trị field. **Backend `DynamicLookupRepository` hiện KHÔNG đọc cấu hình này** → **chưa hoạt động lúc chạy**. Chưa dùng cho đến khi runtime hỗ trợ.
2. **Cascade cho chế độ TVF / SQL tùy chỉnh** — auto-reload **chỉ dò `@param` trong Filter SQL**, chưa dò tham số trong tên hàm / câu SELECT. Tạm thời **bắt buộc** dùng ô *Nâng cao — Tự reload* cho 2 chế độ này. Cần verify end-to-end việc bind lại tham số khi reload.
3. **Panel cũ "⚡ Tham số từ field khác" (`filterParams`)** *(nếu còn hiển thị ở layout nào)* — runtime **không tiêu thụ**; chỉ dùng `@param` trong Filter SQL. Sẽ gỡ/ẩn.
4. **EditBox `Custom` (template riêng)** — cú pháp template & phạm vi hỗ trợ chưa chốt.
5. **Đồng bộ tài liệu:** [12_CASCADE_LOOKUP_GUIDE.md §8](../spec/12_CASCADE_LOOKUP_GUIDE.md) còn mô tả reload theo **1** `ReloadTriggerField` — cần cập nhật theo cơ chế auto-reload đa-`@param` ở §5.

---

## 8. Kiểm tra & Diễn giải cấu hình

Nút **▶ Diễn giải**: sinh **bản mô tả tiếng Việt** toàn bộ cấu hình hiện tại để rà soát trước khi **Lưu Field**. Không sửa dữ liệu — chỉ kiểm tra logic (mode, bảng, cột, filter, reload…). Có thể **Thu gọn/Mở rộng** khối kết quả.

**Cảnh báo cascade khi soát:**
- **P2** — `@param` trong Filter SQL **không khớp** Field Code field nào → danh sách con sẽ **RỖNG**. Sửa tên `@param` hoặc đặt đúng Field Code field cha.
- *(P3 "chưa đặt reload" đã bỏ — table-mode tự reload theo `@param`.)*

---

## 9. Nơi lưu & tham chiếu mã nguồn

| Thành phần | Nơi lưu |
|---|---|
| Nguồn dữ liệu, popup size, EditBox mode, reload-1-field (Nâng cao), parent column, thêm-mới | bảng **`Ui_Field_Lookup`** |
| `columns`, `queryMode`… | **`Ui_Field.Control_Props_Json`** |
| `Reload_Trigger_Fields` (Multi-Trigger, Migration 068) | cột **`Ui_Field_Lookup`** — runtime hợp với @param Filter SQL để reload |
| `dataSourceConditions` | vẫn ghi trong JSON nhưng **runtime KHÔNG dùng** (deprecated) |

- UI panel: `src/frontend/ConfigStudio.WPF.UI/.../Views/Panels/ControlProps/LookupBoxPropsPanel.xaml`
- Renderer runtime (Blazor): `src/frontend/ICare247_UI/Components/FieldRenderers/LookupBoxRenderer.razor` — auto-reload theo `@param` Filter SQL.
- Build SQL + bind context: `src/backend/src/ICare247.Infrastructure/Repositories/DynamicLookupRepository.cs`
- ViewModel + ý nghĩa property: `ViewModels/FieldConfigViewModel.cs`

---

## 10. Checklist nhanh trước khi Lưu

- [ ] Cột bind là `int`/`bigint` (FK)?
- [ ] **Cột Value** = cột Id; **Cột Display** = cột tên?
- [ ] Bảng/View (hoặc Function/SQL) đã đúng nguồn?
- [ ] Filter SQL parameterized (`@TenantId`, `@{FieldCode}`), không nối chuỗi tay?
- [ ] Cascade: `@param` **trùng đúng Field Code** field cha? cột trái = cột FK **thật** trong bảng nguồn?
- [ ] Cột popup đều có **Resource Key i18n** (không chữ cứng)?
- [ ] TVF/Full SQL cần reload → đã điền ô **Nâng cao — Tự reload**?
- [ ] Nếu bật Thêm mới → đã chọn **Form Code** đúng bảng nguồn?
- [ ] Bấm **Diễn giải** rà soát lần cuối (không còn cảnh báo P2).
