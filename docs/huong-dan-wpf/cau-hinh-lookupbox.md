# Hướng dẫn cấu hình **LookupBox** — tham chiếu đầy đủ từng mục (ConfigStudio)

> **Đối tượng:** người cấu hình form trong ConfigStudio (WPF).
> **Phạm vi:** giải thích **chi tiết từng ô** trong tab **Control Props** khi `Editor_Type = LookupBox` (và biến thể `TreeLookupBox`).
> **Liên quan:**
> - Tổng quan editor types → [09_FIELD_CONFIG_GUIDE.md §3.7](../spec/09_FIELD_CONFIG_GUIDE.md)
> - Cascade Tỉnh→Xã (reload theo field cha) → [12_CASCADE_LOOKUP_GUIDE.md](../spec/12_CASCADE_LOOKUP_GUIDE.md)
> - Thêm mới entity ngay trên control → [13_LOOKUP_ADD_NEW_GUIDE.md](../spec/13_LOOKUP_ADD_NEW_GUIDE.md)
> - Dựng lưới tham chiếu (FK) đầu-cuối → [cau-hinh-luoi-tham-chieu.md](cau-hinh-luoi-tham-chieu.md)

---

## 0. LookupBox là gì

`LookupBox` là control chọn **khóa ngoại (FK)**: hiển thị tên dễ đọc cho người dùng nhưng **lưu xuống DB một giá trị `int` (Id)**. Dùng cho: Phòng ban, Nhà cung cấp, Khách hàng, Ngân hàng… — bất kỳ cột FK trỏ tới một bảng nghiệp vụ.

- **Cột bind** phải là kiểu `int` (cột FK trong bảng đang cấu hình form).
- **Biến thể `TreeLookupBox`**: cùng cơ chế nhưng nguồn có cột cha/con → hiển thị dạng cây.
- **Nơi lưu cấu hình:** mỗi LookupBox sinh một bản ghi `Ui_Field_Lookup` (nguồn dữ liệu, popup, reload, thêm-mới) + phần `columns`/`reloadOnChange`/`dataSourceConditions` trong `Control_Props_Json` của `Ui_Field`.

> Mọi nhãn cột trong popup phải qua **Resource Key i18n** (không gõ chữ cứng) — xem mục [Popup grid](#3-popup-grid).

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
| **Filter SQL / WHERE** *(tùy chọn)* | Điều kiện lọc bổ sung. Được ghép vào WHERE (parameterized — **không nối chuỗi tay**). | `Is_Active = 1 AND Tenant_Id = @TenantId` |
| **Sắp xếp (ORDER BY)** | Thứ tự hiển thị trong popup. | `Ten_PhongBan ASC` |

**Tham số dùng được trong Filter SQL:**

| Loại | Token | Giá trị runtime |
|---|---|---|
| Hệ thống | `@TenantId` | Tenant hiện tại |
| Hệ thống | `@Today` | Ngày hiện tại (không giờ) |
| Hệ thống | `@CurrentUser` | Username đang đăng nhập |
| **Từ field khác** | `@{FieldCode}` | Giá trị field cùng form. **Khi field đó đổi → lookup tự reload** (cascade). |

> Ví dụ cascade — phòng ban lọc theo chi nhánh đang chọn:
> ```sql
> Is_Active = 1 AND ChiNhanh_Id = @ChiNhanhId
> ```
> Chi tiết cơ chế cascade → [12_CASCADE_LOOKUP_GUIDE.md](../spec/12_CASCADE_LOOKUP_GUIDE.md).

### 1.4. Chế độ **Function (TVF)**

| Ô | Ý nghĩa | Ví dụ |
|---|---|---|
| **Tên Function (TVF)** | Table-Valued Function trong DB. | `fn_GetPhongBanHieuLuc` |
| **Tham số hàm (theo thứ tự)** | Danh sách tham số — **thứ tự phải khớp định nghĩa hàm**. Mỗi dòng: `@Tên` · **Nguồn** (`field` = lấy từ field trong form / `system` = `@TenantId`…) · `Field/System key`. | `@NgayHieuLuc` · `field` · `NgayVaoLam` |

Bấm **+ Thêm** để thêm dòng tham số; **✕** để xoá.

### 1.5. Chế độ **SQL tùy chỉnh**

| Ô | Ý nghĩa |
|---|---|
| **SELECT SQL** | Câu `SELECT` đầy đủ. **Bắt buộc** có alias **khớp** với *Cột Value* và *Cột Display* đã khai báo. Hỗ trợ tham số hệ thống `@TenantId @Today @CurrentUser`. |

```sql
SELECT p.PhongBan_Id, p.Ten_PhongBan
FROM   DM_PhongBan p
JOIN   DM_ChiNhanh c ON c.ChiNhanh_Id = p.ChiNhanh_Id
WHERE  p.Is_Active = 1 AND p.Tenant_Id = @TenantId
ORDER BY p.Ten_PhongBan
```

### 1.6. **Cho phép tìm kiếm**

Checkbox bật ô tìm kiếm tăng dần (incremental search) trong popup. Mặc định **bật**. Tắt khi danh sách rất ngắn.

---

## 2. EditBox hiển thị

Quy định **hiển thị thế nào trong ô input sau khi đã chọn** một bản ghi.

| Chế độ (`editBoxMode`) | Hiển thị | Ô phụ |
|---|---|---|
| **TextOnly** *(mặc định)* | Chỉ cột Display. | — |
| **CodeAndName** | Mã ngắn + tên (vd `PB01 · Phòng Kế toán`). | hiện thêm **Cột mã code (CodeField)** — vd `PhongBan_Code` |
| **Custom** | Template hiển thị riêng. | — |

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
| **Resource Key (i18n)** | Khóa i18n cho tiêu đề cột — **bắt buộc qua i18n, không gõ chữ cứng** (vd `phongban.col.ma_phong_ban`). |
| **Rộng (px)** | Bề rộng cột (40–400). |
| **✕** | Xoá cột. |

Bấm **+ Thêm cột** để thêm dòng.

### 3.3. Tự reload khi field thay đổi *(1 field — đơn giản)*

Ô **`ReloadTriggerField`**: nhập **một** FieldCode. Khi field đó đổi giá trị → LookupBox **xoá lựa chọn hiện tại + nạp lại data source**. Để trống = không cascading.

> Đây là bản **1-field** lưu ở `Ui_Field_Lookup.Reload_Trigger_Field`. Bản **nhiều-field** ở mục [§5](#5-tự-động-reload-khi-field-thay-đổi-nhiều-field). Nếu Filter SQL đã dùng `@{FieldCode}` thì reload đã tự kích hoạt, **không cần khai lại** ở đây.

### 3.4. *(TreeLookupBox)* Cột cha — Parent Column

Chỉ hiện khi `Editor_Type = TreeLookupBox`. Ô **`ParentColumn`** *(bắt buộc)*: tên cột chứa Id cha trong bảng nguồn (vd `Parent_Id`) để dựng cây cha/con.

---

## 4. Thêm mới entity

Cho phép tạo bản ghi nguồn ngay trên control mà không rời form.

| Ô | Ý nghĩa |
|---|---|
| **Cho phép thêm mới (➕)** | Bật nút "Thêm mới" trong dropdown LookupBox. Lưu `Ui_Field_Lookup.Allow_Add_New`. |
| **Form Code dialog thêm mới** *(bắt buộc khi bật)* | `Form_Code` của `Ui_Form` bound đúng bảng nguồn → mở làm dialog nhập liệu. Lưu `Ui_Field_Lookup.Add_Form_Code`. |

Chi tiết luồng + giới hạn → [13_LOOKUP_ADD_NEW_GUIDE.md](../spec/13_LOOKUP_ADD_NEW_GUIDE.md).

---

## 5. Tự động reload khi field thay đổi *(nhiều field)*

Phiên bản **danh sách** của §3.3: khai **nhiều** FieldCode; bất kỳ field nào trong danh sách đổi giá trị → lookup reload (giá trị đang chọn bị xoá nếu không còn hợp lệ). Nhập FieldCode rồi **Enter** hoặc bấm **+ Thêm** (hiển thị dạng tag, bấm **×** để xoá). Lưu vào `reloadOnChange` trong `Control_Props_Json`.

> **Ba cách kích hoạt reload — chọn 1 cho rõ ràng:** `@{FieldCode}` trong Filter SQL (1.3) · `ReloadTriggerField` (3.3, 1 field) · danh sách `reloadOnChange` (mục này, nhiều field).

---

## 6. Điều kiện đổi bảng nguồn

Đổi **bảng nguồn** theo giá trị field khác trong form (data source động). Bấm **+ Thêm điều kiện**; mỗi điều kiện:

| Phần | Ô | Ý nghĩa |
|---|---|---|
| **Nếu** | **Field** | FieldCode trong form (vd `LoaiNhanVien`). |
| | **Phép so sánh** | `eq · neq · gt · gte · lt · lte · contains · startsWith`. |
| | **Giá trị so sánh** | Giá trị đối chiếu (vd `THUE_NGOAI`). |
| **→ Thì dùng** | **Bảng nguồn thay thế** | Bảng khác (vd `DM_DonViThueNgoai`). |
| | **Cột hiển thị** | Cột Display của bảng thay thế (vd `Ten_Don_Vi`). |
| | **Filter SQL** *(tùy chọn)* | Điều kiện riêng cho bảng thay thế. |

**Ưu tiên từ trên xuống**: điều kiện đầu tiên khớp sẽ thắng; không điều kiện nào khớp → dùng bảng mặc định ở §1.

---

## 7. Kiểm tra & Diễn giải cấu hình

Nút **▶ Diễn giải**: sinh **bản mô tả tiếng Việt** toàn bộ cấu hình hiện tại (từ `Control_Props_Json`) để rà soát trước khi **Lưu Field**. Không sửa dữ liệu — chỉ để kiểm tra logic (mode, bảng, cột, filter, reload…).

---

## 8. Nơi lưu & tham chiếu mã nguồn

| Thành phần | Nơi lưu |
|---|---|
| Nguồn dữ liệu, popup size, EditBox mode, reload-1-field, parent column, thêm-mới | bảng **`Ui_Field_Lookup`** |
| `columns`, `reloadOnChange`, `dataSourceConditions`, `queryMode`… | **`Ui_Field.Control_Props_Json`** |

- UI panel: `src/frontend/ConfigStudio.WPF.UI/.../Views/Panels/ControlProps/LookupBoxPropsPanel.xaml`
- 2 mục reload-nhiều-field + đổi-bảng-nguồn: `Views/FieldConfigView.xaml` (tab Control Props)
- ViewModel + ý nghĩa từng property: `ViewModels/FieldConfigViewModel.cs`
- Runtime render (Blazor): xem [24_BLAZOR_CONTROL_RENDERER_SPEC.md](../spec/24_BLAZOR_CONTROL_RENDERER_SPEC.md)

---

## 9. Checklist nhanh trước khi Lưu

- [ ] Cột bind là `int` (FK)?
- [ ] **Cột Value** = cột Id; **Cột Display** = cột tên?
- [ ] Bảng/View (hoặc Function/SQL) đã đúng nguồn?
- [ ] Filter SQL parameterized (`@TenantId`, `@{FieldCode}`), không nối chuỗi tay?
- [ ] Cột popup đều có **Resource Key i18n** (không chữ cứng)?
- [ ] Reload khai đúng **một** trong 3 cách (không trùng)?
- [ ] Nếu bật Thêm mới → đã chọn **Form Code** đúng bảng nguồn?
- [ ] Bấm **Diễn giải** rà soát lần cuối.
