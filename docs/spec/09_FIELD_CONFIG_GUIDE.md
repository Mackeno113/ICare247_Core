# Hướng dẫn Cấu hình Field — ICare247 ConfigStudio

> **Đối tượng:** Admin / Người cấu hình form
> **Phiên bản:** 1.0 — 2026-03-26
> **Cập nhật:** Mỗi khi thay đổi schema hoặc engine

---

## Mục lục

1. [Tổng quan](#1-tổng-quan)
2. [Tab Cơ bản](#2-tab-cơ-bản)
3. [Tab Control Props](#3-tab-control-props)
4. [Tab Rules](#4-tab-rules)
5. [Tab Events](#5-tab-events)
6. [Ví dụ thực tế](#6-ví-dụ-thực-tế)

---

## 1. Tổng quan

Mỗi **Field** trong một Form được cấu hình qua 4 tab:

```
┌─────────────────────────────────────────────────────────┐
│  Cấu hình Field: sec_nhanvien_1 › MaNhanVien            │
├──────────┬─────────────────┬──────────┬─────────────────┤
│  Cơ bản  │  Control Props  │  Rules   │    Events       │
└──────────┴─────────────────┴──────────┴─────────────────┘
```

| Tab | Mục đích |
|-----|---------|
| **Cơ bản** | Bind cột DB, chọn loại control, behavior, thứ tự, i18n |
| **Control Props** | Tham số riêng của từng loại control (maxLength, format, lookup…) |
| **Rules** | Validation — bắt buộc, khoảng giá trị, regex, điều kiện tùy chỉnh |
| **Events** | Hành động động — ẩn/hiện, tính toán, reload dropdown khi giá trị thay đổi |

---

## 2. Tab Cơ bản

### 2.1 Thông tin cơ bản

| Trường | Mô tả | Ví dụ |
|--------|-------|-------|
| **Column (DB)** | Cột DB mà field này bind vào. Quyết định kiểu dữ liệu và constraint. | `MaNhanVien (nvarchar, NOT NULL)` |
| **Net Type** | Kiểu .NET tự động xác định từ cột DB — chỉ đọc. | `string`, `int`, `DateTime`, `bool` |
| **Editor Type** | Loại UI component hiển thị. Thay đổi sẽ cập nhật tab Control Props. | `TextBox` |

> **Lưu ý:** Sau khi chọn Column, Net Type được tự động điền. Hãy chọn Editor Type phù hợp với kiểu dữ liệu đó.

### 2.2 Editor Types — Bảng tra cứu nhanh

| Editor Type | Dùng khi | Column type khuyến nghị |
|-------------|----------|------------------------|
| `TextBox` | Văn bản ngắn: tên, mã số, email, địa chỉ | `nvarchar`, `varchar`, `char` |
| `TextArea` | Văn bản dài: ghi chú, mô tả, nội dung | `nvarchar(max)`, `text` |
| `NumericBox` | Số: số lượng, đơn giá, tuổi, phần trăm | `int`, `decimal`, `float` |
| `DatePicker` | Ngày / Ngày giờ | `datetime`, `date` |
| `CheckBox` | Có/Không — trạng thái nhỏ | `bit` |
| `ToggleSwitch` | Bật/Tắt — Active/Inactive | `bit` |
| `ComboBox` | Dropdown từ API động | Bất kỳ |
| `RadioGroup` | Danh mục tĩnh ≤ 5 lựa chọn (Giới tính, Trạng thái) | `nvarchar` |
| `LookupComboBox` | Danh mục tĩnh > 5 lựa chọn từ Sys_Lookup | `nvarchar` |
| `LookupBox` | FK tham chiếu bảng nghiệp vụ (Phòng ban, Khách hàng) | `int` (FK) |

### 2.3 Behavior

Ba thuộc tính tĩnh kiểm soát trạng thái của field — lưu thẳng vào bảng `Ui_Field`:

| Toggle | Cột DB | Mô tả | Mặc định |
|--------|--------|-------|----------|
| **Hiển thị** | `Is_Visible` | Field có hiển thị trên form không. Tắt để ẩn field (vẫn lưu DB). | ✅ Bật |
| **🔒 Chỉ đọc** | `Is_ReadOnly` | Hiển thị giá trị nhưng không cho chỉnh sửa. Giá trị vẫn được submit. | ❌ Tắt |
| **✱ Bắt buộc** | `Is_Required` | Không cho phép để trống khi submit form. | ❌ Tắt |

> **Phân biệt Chỉ đọc vs Ẩn:**
> - `Is_ReadOnly = true` → hiện giá trị, không sửa được, **vẫn lưu khi submit**
> - `Is_Visible = false` → ẩn hoàn toàn, không tương tác, **vẫn lưu khi submit**
>
> **Behavior động** (ẩn/hiện theo điều kiện, readonly theo trạng thái…) → cấu hình tại tab **Events**.

### 2.4 Layout

| Trường | Mô tả | Giá trị |
|--------|-------|---------|
| **Thứ tự hiển thị** | Số thứ tự trong section (Order_No). Field nhỏ hơn hiển thị trước. | Số nguyên ≥ 1 |
| **Độ rộng (Col Span)** | Chiếm bao nhiêu cột trong grid layout của section. | `1/3`, `2/3`, `Full` |

```
Section layout (3 cột):
┌───────┬───────┬───────┐
│  1/3  │  1/3  │  1/3  │   ← 3 field 1/3 mỗi field
└───────┴───────┴───────┘
┌───────────────┬───────┐
│     2/3       │  1/3  │   ← 1 field 2/3 + 1 field 1/3
└───────────────┴───────┘
┌───────────────────────┐
│         Full          │   ← 1 field chiếm toàn bộ hàng
└───────────────────────┘
```

### 2.5 Display (i18n)

| Trường | Mô tả | Ví dụ |
|--------|-------|-------|
| **Label Key** | Resource key cho nhãn field. Tra cứu trong `Sys_Resource`. | `nhanvien.field.manhanvien.label` |
| **Placeholder Key** | Resource key cho placeholder text trong ô nhập. | `nhanvien.field.manhanvien.placeholder` |
| **Tooltip Key** | Resource key cho tooltip khi hover. | `nhanvien.field.manhanvien.tooltip` |

**Cú pháp key chuẩn:** `{FormCode}.field.{FieldCode}.{qualifier}`

| Component | Ví dụ | Ghi chú |
|-----------|-------|---------|
| `FormCode` | `nhanvien` | `Ui_Form.Form_Code` viết thường |
| `field` | `field` | cố định, phân biệt namespace |
| `FieldCode` | `manhanvien` | `Ui_Field.Column_Code` viết thường |
| `qualifier` | `label`, `placeholder`, `tooltip` | loại nội dung |

**Ví dụ:** field `MaNhanVien` trong form `NhanVien`:
- Label: `nhanvien.field.manhanvien.label` → "Mã nhân viên"
- Placeholder: `nhanvien.field.manhanvien.placeholder` → "Nhập mã nhân viên..."
- Tooltip: `nhanvien.field.manhanvien.tooltip` → "Mã định danh duy nhất"

> **Tự động tạo key:** Nhấn **+ Tạo key** trong FieldConfigView để auto-generate theo cú pháp trên. Nếu key đã tồn tại → hệ thống cảnh báo và cho phép dùng tiếp hoặc hủy.
> **Quy ước tự động:** Khi nhập Label Key, Placeholder và Tooltip tự động theo theo. Bạn có thể tách biệt nếu cần nội dung khác nhau.
> **Manage i18n →** nhấn nút để mở màn hình quản lý bản dịch cho key này.

---

## 3. Tab Control Props

Tab này thay đổi nội dung theo **Editor Type** đang chọn.

### 3.1 TextBox — Văn bản ngắn

| Property | Kiểu | Mô tả | Mặc định |
|----------|------|-------|----------|
| `maxLength` | int | Số ký tự tối đa được nhập | 255 |
| `isMultiline` | bool | Cho phép xuống dòng trong ô nhập | false |
| `rows` | int | Số dòng hiển thị khi `isMultiline = true` | 3 |

**Ví dụ JSON:**
```json
{ "maxLength": 100, "isMultiline": false }
```

---

### 3.2 TextArea — Văn bản dài

| Property | Kiểu | Mô tả | Mặc định |
|----------|------|-------|----------|
| `maxLength` | int | Số ký tự tối đa | 4000 |
| `rows` | int | Số dòng hiển thị (khuyến nghị ≥ 3) | 5 |

---

### 3.3 NumericBox — Số

| Property | Kiểu | Mô tả | Mặc định |
|----------|------|-------|----------|
| `minValue` | decimal | Giá trị tối thiểu | 0 |
| `maxValue` | decimal | Giá trị tối đa | 999999 |
| `decimals` | int | Số chữ số thập phân (0 = số nguyên) | 0 |
| `spinStep` | decimal | Bước nhảy khi bấm mũi tên lên/xuống | 1 |
| `allowNull` | bool | Cho phép để trống | false |

**Ví dụ JSON:**
```json
{ "minValue": 0, "maxValue": 100, "decimals": 2, "spinStep": 0.01 }
```

---

### 3.4 DatePicker — Ngày / Ngày giờ

| Property | Kiểu | Mô tả | Mặc định |
|----------|------|-------|----------|
| `format` | string | Định dạng hiển thị | `dd/MM/yyyy` |
| `minDate` | string | Ngày tối thiểu được chọn (ISO 8601) | _(không giới hạn)_ |
| `maxDate` | string | Ngày tối đa được chọn (ISO 8601) | _(không giới hạn)_ |

**Các giá trị `format` hợp lệ:**

| Format | Hiển thị | Dùng cho |
|--------|----------|---------|
| `dd/MM/yyyy` | 26/03/2026 | Ngày sinh, ngày đặt hàng |
| `dd/MM/yyyy HH:mm` | 26/03/2026 14:30 | Timestamp sự kiện |
| `MM/yyyy` | 03/2026 | Tháng hiệu lực |
| `yyyy` | 2026 | Năm sản xuất |

---

### 3.5 CheckBox / ToggleSwitch — Bit

Không cần cấu hình thêm. Mapping trực tiếp vào cột `bit`:
- `true` → `1` trong DB
- `false` → `0` trong DB

---

### 3.6 RadioGroup / LookupComboBox — Danh mục tĩnh (Sys_Lookup)

| Property | Kiểu | Mô tả |
|----------|------|-------|
| `lookupCode` | string | Mã nhóm danh mục trong `Sys_Lookup`. VD: `GENDER`, `MARITAL_STATUS` |
| `layout` | string | `horizontal` hoặc `vertical` (chỉ dùng cho RadioGroup) |

**Ví dụ JSON:**
```json
{ "lookupCode": "GENDER", "layout": "horizontal" }
```

> **Tạo/sửa danh mục:** Vào **Quản lý Sys_Lookup** để thêm/sửa các item trong nhóm danh mục.

---

### 3.7 LookupBox — FK tham chiếu bảng nghiệp vụ

LookupBox có 3 chế độ truy vấn:

#### Chế độ: Bảng / View (khuyến nghị)

| Property | Mô tả | Ví dụ |
|----------|-------|-------|
| **Cột Value (lưu vào DB)** | Cột FK sẽ lưu vào DB. | `PhongBan_Id` |
| **Cột Display (hiển thị)** | Cột hiển thị trong ô và popup. | `Ten_PhongBan` |
| **Tên bảng hoặc View** | Tên bảng nguồn hoặc View. View có thể chứa JOIN sẵn. | `DM_PhongBan` hoặc `vw_PhongBan_Full` |
| **Filter SQL / WHERE** | Điều kiện lọc thêm. Dùng tham số hệ thống. | `Is_Active = 1 AND Tenant_Id = @TenantId` |
| **Sắp xếp (ORDER BY)** | Thứ tự hiển thị trong popup. | `Ten_PhongBan ASC` |

**Tham số hệ thống có thể dùng trong Filter SQL:**

| Tham số | Giá trị |
|---------|---------|
| `@TenantId` | ID tenant hiện tại |
| `@Today` | Ngày hiện tại (không có giờ) |
| `@CurrentUser` | Username người đang đăng nhập |

**Tham số từ field khác trong form:**

Dùng `@{FieldCode}` để tham chiếu giá trị field khác. Khi field nguồn thay đổi → lookup tự động reload.

Ví dụ: Filter phòng ban theo chi nhánh đang chọn:
```sql
Is_Active = 1 AND ChiNhanh_Id = @ChiNhanhId
```

| Property | Mô tả | Ví dụ |
|----------|-------|-------|
| **Cột hiển thị trong popup** | Danh sách cột trong bảng popup chọn. | `[{"column":"PhongBan_Id","title":"Mã","width":60}, {"column":"Ten_PhongBan","title":"Tên phòng ban","width":200}]` |
| **Tự động reload khi field thay đổi** | FieldCode trigger reload. Giá trị đang chọn bị xóa nếu không còn hợp lệ. | `ChiNhanhId` |

#### Chế độ: Function (TVF)

Dùng khi cần truyền nhiều tham số hoặc logic phức tạp hơn WHERE đơn giản.

| Property | Mô tả | Ví dụ |
|----------|-------|-------|
| **Tên Function** | Table-Valued Function trong DB. | `fn_GetPhongBanHieuLuc` |
| **Tham số Function** | Danh sách tham số theo thứ tự định nghĩa hàm. Nguồn: `field` (từ form) hoặc `system` (`@TenantId`, `@Today`, `@CurrentUser`). | |

#### Chế độ: SQL tùy chỉnh

Dùng khi cần JOIN phức tạp mà View không phù hợp.

| Property | Mô tả |
|----------|-------|
| **SELECT SQL** | Full SELECT statement. **Phải có alias khớp** với Cột Value và Cột Display đã khai báo. |

**Ví dụ:**
```sql
SELECT p.PhongBan_Id, p.Ten_PhongBan
FROM   DM_PhongBan p
JOIN   DM_ChiNhanh c ON c.ChiNhanh_Id = p.ChiNhanh_Id
WHERE  p.Is_Active = 1
  AND  p.Tenant_Id = @TenantId
ORDER BY p.Ten_PhongBan
```

---

## 4. Tab Rules

Rules là các **điều kiện validate tĩnh** áp dụng cho field khi user submit form.

> **Validate động** (validate khi điều kiện X thỏa) → dùng `Condition_Expr` trong rule hoặc action `TRIGGER_VALIDATION` trong tab Events.

### 4.1 Cấu trúc một Rule

| Trường | Mô tả |
|--------|-------|
| **Loại Rule** | Kiểu validate: `Required`, `Length`, `Range`, `Regex`, `Compare`, `Custom` |
| **Mức độ nghiêm trọng** | `Error` = chặn submit / `Warning` = cảnh báo nhưng cho submit / `Info` = chỉ gợi ý |
| **Error Key (auto)** | Resource key thông báo lỗi, tự động sinh theo pattern `{table}.val.{field}.{type}` |
| **Điều kiện áp dụng** | _(tùy chọn)_ Rule chỉ áp dụng khi điều kiện này đúng. Dùng Expression Builder. |

**Thứ tự đánh giá:** Rules được evaluate theo số **#** — nhỏ trước. Dùng ↑↓ để sắp xếp.

---

### 4.2 Loại Rule — Chi tiết

#### Required — Bắt buộc nhập

Không cần thiết lập thêm. Field không được để trống khi submit.

> **Lưu ý:** Nếu đã bật **✱ Bắt buộc** trong tab Cơ bản, không cần thêm rule `Required` — chúng tương đương nhau. Dùng rule khi cần `Warning` thay vì `Error`, hoặc khi cần điều kiện áp dụng.

---

#### Length — Độ dài văn bản

Kiểm tra số ký tự của field `nvarchar`/`varchar`.

| Trường | Mô tả | Ví dụ |
|--------|-------|-------|
| **Min** | Tối thiểu bao nhiêu ký tự | `6` |
| **Max** | Tối đa bao nhiêu ký tự | `50` |

Để trống một đầu = không giới hạn đầu đó.

**Preview expression (tự sinh):**
```
len(MatKhau) >= 6 && len(MatKhau) <= 50
```

---

#### Range — Khoảng giá trị

Kiểm tra giá trị số hoặc ngày nằm trong khoảng cho phép.

| Trường | Mô tả | Ví dụ số | Ví dụ ngày |
|--------|-------|----------|-----------|
| **Min** | Giá trị tối thiểu | `1` | `2020-01-01` |
| **Max** | Giá trị tối đa | `9999` | `2099-12-31` |

**Preview expression (tự sinh):**
```
SoLuong >= 1 && SoLuong <= 9999
```

---

#### Regex — Biểu thức chính quy

Kiểm tra định dạng văn bản theo pattern regex.

| Trường | Mô tả |
|--------|-------|
| **Pattern** | Regular expression. Nhập thủ công hoặc chọn template có sẵn. |

**Template có sẵn:**

| Template | Pattern | Dùng cho |
|----------|---------|---------|
| Mật khẩu mạnh | `^(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#$%]).{8,}$` | Ít nhất 8 ký tự, có hoa, số, ký tự đặc biệt |
| Email | `^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$` | Địa chỉ email |
| Số điện thoại VN | `^(0\|\+84)[0-9]{9,10}$` | SĐT Việt Nam |
| Chỉ số nguyên | `^[0-9]+$` | Chỉ chứa chữ số |
| Chỉ chữ cái | `^[a-zA-ZÀ-ỹ\s]+$` | Chỉ chứa chữ cái và khoảng trắng |
| Mã định danh | `^[A-Z0-9_]{3,50}$` | Mã hệ thống: chữ hoa, số, dấu gạch dưới |
| URL | `^https?://[^\s/$.?#].[^\s]*$` | Đường dẫn web |

---

#### Compare — So sánh với field khác

Kiểm tra mối quan hệ giữa field này với một field khác trong cùng form.

| Trường | Mô tả | Ví dụ |
|--------|-------|-------|
| **Field so sánh** | FieldCode của field kia | `NgayVaoLam` |
| **Toán tử** | `==`, `!=`, `>`, `>=`, `<`, `<=` | `>=` |

**Ví dụ:** `NgayNghiViec >= NgayVaoLam`

---

#### Custom — Điều kiện tùy chỉnh

Viết điều kiện AST bất kỳ bằng **Expression Builder**.

Điều kiện phải trả về `true` (hợp lệ) hoặc `false` (lỗi).

**Ví dụ một số điều kiện custom:**
```
# Tổng tiền phải bằng đơn giá × số lượng
TongTien == DonGia * SoLuong

# Ngày hết hạn phải sau ngày hiện tại
NgayHetHan > today()

# Nếu loại hợp đồng là "XD" thì thời hạn phải > 12 tháng
iif(LoaiHopDong == "XD", ThoiHan > 12, true)
```

---

### 4.3 Mức độ nghiêm trọng (Severity)

| Mức | Màu | Hành vi khi vi phạm |
|-----|-----|---------------------|
| `Error` | 🔴 Đỏ | **Chặn submit** — user bắt buộc phải sửa |
| `Warning` | 🟡 Vàng | **Hiện cảnh báo** nhưng vẫn cho phép submit |
| `Info` | 🔵 Xanh | **Hiện gợi ý** nhẹ — không ảnh hưởng submit |

---

### 4.4 Thông báo lỗi đa ngôn ngữ

**Error Key** được tự động sinh theo pattern:
```
{table_code}.val.{field_code}.{rule_type}
```

Ví dụ: `nhanvien.val.MatKhau.length`

Khi lưu rule, hệ thống **tự động tạo bản dịch mặc định** trong `Sys_Resource` (vi + en). Để tùy chỉnh nội dung thông báo:
1. Vào **Quản lý i18n**
2. Tìm theo Error Key
3. Chỉnh sửa nội dung vi/en

---

## 5. Tab Events

Events là hành động **động** xảy ra khi có sự kiện nhất định (user nhập liệu, form load…).

### 5.1 Cấu trúc một Event

```
Trigger (Khi nào)
    └── Condition (Nếu điều kiện đúng)   ← tùy chọn
        └── Actions (Thực hiện)
            ├── Action 1
            ├── Action 2
            └── ...
```

### 5.2 Triggers — Khi nào kích hoạt

| Trigger | Kích hoạt khi | Dùng cho |
|---------|--------------|---------|
| `OnChange` | User thay đổi giá trị field | Tính toán, reload dropdown phụ thuộc |
| `OnBlur` | User rời khỏi field (mất focus) | Validate, format lại giá trị |
| `OnLoad` | Form vừa load xong | Set giá trị mặc định, ẩn/hiện theo quyền |
| `OnSubmit` | User bấm nút Submit | Validate phức tạp, tính tổng cuối |
| `OnSectionToggle` | User mở/đóng một section | Lazy load dữ liệu khi cần |

---

### 5.3 Actions — Hành động thực hiện

#### SET_VALUE — Gán giá trị

Gán giá trị cho một field khác (hoặc chính field này).

| Tham số | Mô tả | Ví dụ |
|---------|-------|-------|
| `targetField` | FieldCode của field nhận giá trị | `TongTien` |
| `valueExpression` | Expression tính giá trị (AST) | `SoLuong * DonGia` |

**Ví dụ thực tế:**
- Field `SoLuong` có trigger `OnChange` → SET_VALUE `TongTien = SoLuong * DonGia`
- Field `NgayVaoLam` có trigger `OnChange` → SET_VALUE `NamKinhNghiem = year(today()) - year(NgayVaoLam)`

---

#### SET_VISIBLE — Ẩn / Hiện field hoặc section

| Tham số | Mô tả | Ví dụ |
|---------|-------|-------|
| `targetField` | FieldCode hoặc SectionCode | `SoTheDuong` |
| `conditionExpression` | Điều kiện để hiện (true = hiện, false = ẩn) | `LoaiHopDong == "CO_DINH"` |

**Ví dụ thực tế:**
- Chỉ hiện field `NgayHetHan` khi `LoaiHopDong != "KHONG_THOI_HAN"`
- Ẩn section `ThongTinBaoHiem` khi nhân viên là thử việc

---

#### SET_READONLY — Khóa / Mở chỉnh sửa

| Tham số | Mô tả | Ví dụ |
|---------|-------|-------|
| `targetField` | FieldCode cần khóa/mở | `MaHopDong` |
| `conditionExpression` | True = chỉ đọc, False = cho sửa | `TrangThai == "DUYET"` |

**Ví dụ thực tế:**
- Khóa toàn bộ form khi trạng thái = "Đã duyệt"
- Khóa `DonGia` khi sản phẩm đã có hóa đơn

---

#### SET_REQUIRED — Bắt buộc động

| Tham số | Mô tả | Ví dụ |
|---------|-------|-------|
| `targetField` | FieldCode cần bắt buộc/bỏ bắt buộc | `SoGiayPhep` |
| `conditionExpression` | True = bắt buộc, False = không bắt buộc | `LoaiXe == "TAI"` |

**Ví dụ thực tế:**
- `SoGiayPhep` chỉ bắt buộc khi `LoaiXe == "TAI"` (xe tải cần giấy phép đặc biệt)

---

#### SET_ENABLED — Kích hoạt / Vô hiệu hóa

Khác với SET_READONLY: field bị disabled sẽ **grayout hoàn toàn** và **không được tính vào submit**.

| Tham số | Mô tả | Ví dụ |
|---------|-------|-------|
| `targetField` | FieldCode cần kích hoạt/vô hiệu | `TongTienThuCong` |
| `conditionExpression` | True = enabled, False = disabled | `IsAutoCalc == false` |

**Ví dụ thực tế:**
- Disable `TongTien` khi `IsAutoCalc = true` (hệ thống tự tính, không cho nhập tay)

---

#### CLEAR_VALUE — Xóa giá trị

Xóa giá trị của field khi điều kiện thay đổi.

| Tham số | Mô tả | Ví dụ |
|---------|-------|-------|
| `targetField` | FieldCode cần xóa giá trị | `PhongBanId` |

**Ví dụ thực tế:**
- Khi `ChiNhanhId` thay đổi → CLEAR_VALUE `PhongBanId` (phòng ban cũ không còn thuộc chi nhánh mới)

---

#### RELOAD_OPTIONS — Reload danh sách dropdown

Reload danh sách options của một LookupBox/ComboBox phụ thuộc.

| Tham số | Mô tả |
|---------|-------|
| `targetField` | FieldCode của LookupBox cần reload |
| `dependsOn` | FieldCode nguồn đang thay đổi (dùng làm tham số filter) |

**Ví dụ thực tế:**
- Khi `TinhId` thay đổi → RELOAD_OPTIONS `HuyenId` (load huyện theo tỉnh mới)
- Khi `LoaiSanPham` thay đổi → RELOAD_OPTIONS `MaSanPham` (chỉ hiện SP thuộc loại đó)

---

#### TRIGGER_VALIDATION — Kích hoạt validate

Validate thủ công một hoặc nhiều field.

| Tham số | Mô tả |
|---------|-------|
| `targetFields` | Mảng FieldCode cần validate | `["NgayKetThuc", "SoNgay"]` |

---

#### SHOW_MESSAGE — Hiển thị thông báo

Hiển thị popup hoặc toast thông báo cho user.

| Tham số | Mô tả | Ví dụ |
|---------|-------|-------|
| `messageKey` | Resource key nội dung thông báo | `donhang.warn.so_luong_lon` |
| `severity` | `info`, `warn`, `error` | `warn` |

---

### 5.4 Ví dụ Event hoàn chỉnh

**Yêu cầu:** Khi `SoLuong` thay đổi, tự tính `TongTien`. Nếu `TongTien > 100,000,000` thì hiện cảnh báo.

```
Trigger:   OnChange (field: SoLuong)
Condition: ─ (không có — luôn chạy)
Actions:
  1. SET_VALUE     → TongTien = SoLuong * DonGia
  2. SHOW_MESSAGE  → condition: TongTien > 100000000
                    messageKey: donhang.warn.tong_tien_lon
                    severity: warn
```

---

## 6. Ví dụ thực tế

### 6.1 Form Nhân viên — Field Mật khẩu

| Cài đặt | Giá trị |
|---------|---------|
| Column | `MatKhau (nvarchar, NOT NULL)` |
| Editor Type | `TextBox` |
| Hiển thị | ✅ |
| Chỉ đọc | ❌ |
| Bắt buộc | ✅ |
| maxLength | `100` |
| **Rule 1** | Length: min=8, max=100, Error — "Mật khẩu tối thiểu 8 ký tự" |
| **Rule 2** | Regex: `^(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#$%]).{8,}$`, Warning — "Nên dùng mật khẩu mạnh" |

---

### 6.2 Form Đơn hàng — Field Phòng ban (FK Lookup)

| Cài đặt | Giá trị |
|---------|---------|
| Column | `PhongBan_Id (int)` |
| Editor Type | `LookupBox` |
| Bắt buộc | ✅ |
| Query Mode | `table` |
| Source Table | `DM_PhongBan` |
| Value Field | `PhongBan_Id` |
| Display Field | `Ten_PhongBan` |
| Filter SQL | `Is_Active = 1 AND Tenant_Id = @TenantId` |
| Order By | `Ten_PhongBan ASC` |
| Popup Columns | Mã phòng ban (80px), Tên phòng ban (200px) |

---

### 6.3 Form Hợp đồng — Ẩn field theo loại hợp đồng

**Yêu cầu:** Field `NgayHetHan` chỉ hiện khi `LoaiHopDong != "KHONG_THOI_HAN"`.

| Event | |
|-------|--|
| Trigger | `OnChange` (field: `LoaiHopDong`) |
| Action | `SET_VISIBLE` → target: `NgayHetHan`, condition: `LoaiHopDong != "KHONG_THOI_HAN"` |

Thêm trigger `OnLoad` với cùng action để đảm bảo trạng thái đúng khi mở form edit.

---

*Tài liệu này được cập nhật cùng với spec kỹ thuật tại `docs/spec/`. Mọi thay đổi schema cần cập nhật cả hai.*
