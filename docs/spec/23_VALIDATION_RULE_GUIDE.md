# Hướng dẫn cấu hình Validation Rule

> **Đối tượng:** Admin/Developer cấu hình form trong ConfigStudio WPF  
> **Màn hình:** Validation Rule Editor (mở từ FieldConfig → tab Rules → [+ Thêm Rule])  
> **Cập nhật:** 2026-05-31

---

## Mục lục

1. [Tổng quan](#1-tổng-quan)
2. [Mở màn hình Rule Editor](#2-mở-màn-hình-rule-editor)
3. [Giao diện màn hình](#3-giao-diện-màn-hình)
4. [Các loại Rule](#4-các-loại-rule)
   - [Length — Độ dài ký tự](#41-length--độ-dài-ký-tự)
   - [Range — Khoảng giá trị](#42-range--khoảng-giá-trị)
   - [Regex — Biểu thức chính quy](#43-regex--biểu-thức-chính-quy)
   - [Compare — So sánh field](#44-compare--so-sánh-field)
   - [Numeric — Chỉ số](#45-numeric--chỉ-số)
   - [Custom — Điều kiện tự do](#46-custom--điều-kiện-tự-do)
5. [Mức độ nghiêm trọng (Severity)](#5-mức-độ-nghiêm-trọng-severity)
6. [Thông báo lỗi (Error Key)](#6-thông-báo-lỗi-error-key)
7. [Thứ tự thực thi](#7-thứ-tự-thực-thi)
8. [Kịch bản thực tế](#8-kịch-bản-thực-tế)
9. [Phím tắt](#9-phím-tắt)

---

## 1. Tổng quan

**Validation Rule** là điều kiện kiểm tra giá trị người dùng nhập vào một field trước khi submit form.

Mỗi rule gồm 4 thành phần:

| Thành phần | Mô tả | Ví dụ |
|---|---|---|
| **Loại Rule** | Kiểu kiểm tra | `Length`, `Range`, `Regex`... |
| **Tham số** | Giá trị cấu hình theo loại | min=6, max=100 |
| **Error Key** | Key i18n cho thông báo lỗi | `nhanvien.val.hoten.length` |
| **Severity** | Mức độ — có chặn submit không | `Error` / `Warning` / `Info` |

> **Lưu ý:** Trường "Bắt buộc nhập" (`Is_Required`) **không** cấu hình ở đây — dùng checkbox
> **Bắt buộc** trong tab **Behavior** của FieldConfig.

---

## 2. Mở màn hình Rule Editor

**Cách 1 — Từ FieldConfig:**
```
FormEditor → click [⚙] trên field → tab Rules → [+ Thêm Rule]
```

**Cách 2 — Từ sidebar:**
```
Menu trái → Validation Rules → Ctrl+N để thêm mới
```

Khi mở từ FieldConfig, màn hình tự load danh sách rule của field đó và hiển thị breadcrumb:
```
← Cấu hình Field  ›  [SectionName]  ›  [FieldCode]
```

---

## 3. Giao diện màn hình

```
┌─────────────────────────────────────────────────────────────────┐
│ ← Cấu hình Field › InfoCaNhan › HoTen          [+ Thêm Rule] ↑↓│
│ Validation Rules                                                 │
│ 2 rule(s) — Field Id: 12                                         │
├──────────────────────────────────┬──────────────────────────────┤
│ #  │ Loại Rule │ Điều kiện       │ Chi tiết Rule                │
│ 1  │ Length    │ HoTen >= 2 &&   │ Loại Rule: [Length      ▼]   │
│    │           │ HoTen <= 100    │ Error Key: nv.val.hoten...   │
│ 2  │ Regex     │ ^[a-zA-ZÀ-ỹ]+  │ Severity: [Error       ▼]   │
│    │           │                 │                              │
│                                  │ Độ dài ký tự                 │
│                                  │ Min [2  ] – Max [100 ]       │
│                                  │ ┌─────────────────────────┐  │
│                                  │ │ HoTen >= 2 && HoTen<=100│  │
│                                  │ └─────────────────────────┘  │
│                                  │         [Hủy] [Áp dụng & Lưu]│
└──────────────────────────────────┴──────────────────────────────┘
│ 🛡  2 rules · Field: HoTen · Section: InfoCaNhan                 │
└─────────────────────────────────────────────────────────────────┘
```

**Cột lưới:**
- `#` — Thứ tự thực thi (kéo ↑↓ để đổi)
- `Loại Rule` — badge xanh
- `Điều kiện` — expression preview (monospace)
- `Thông báo lỗi (key)` — i18n key
- `Mức độ` — Error (đỏ) / Warning (vàng) / Info (xanh)
- `Bật` — checkbox bật/tắt rule không cần xóa

**Edit Panel** (bên phải, mở khi click ✎ hoặc double-click dòng):
- Luôn hiển thị: Loại Rule + Error Key + Severity
- Hiển thị thêm theo loại: Range inputs / Regex pattern / Compare selector / Expression Builder

---

## 4. Các loại Rule

### 4.1 Length — Độ dài ký tự

**Dùng cho:** Trường văn bản (`nvarchar`, `varchar`, `char`)

**Tham số:**

| Tham số | Mô tả | Bắt buộc |
|---|---|---|
| Min | Số ký tự tối thiểu | Không (để trống = không giới hạn) |
| Max | Số ký tự tối đa | Không (để trống = không giới hạn) |

**Ví dụ cấu hình:**
```
Loại:  Length
Min:   2
Max:   100
```
→ Expression tự sinh: `HoTen >= 2 && HoTen <= 100`

**Ví dụ thực tế:**

| Field | Min | Max | Ý nghĩa |
|---|---|---|---|
| Họ tên | 2 | 100 | Tên không quá ngắn hoặc quá dài |
| Mật khẩu | 8 | 255 | Tối thiểu 8 ký tự |
| Mã nhân viên | 3 | 20 | Code định danh |
| Ghi chú | — | 2000 | Chỉ giới hạn tối đa |

> **Tip:** Để trống **Min** nếu không cần giới hạn tối thiểu (ví dụ: trường optional).

---

### 4.2 Range — Khoảng giá trị

**Dùng cho:** Số (`int`, `decimal`, `float`) và Ngày (`datetime`, `date`)

**Tham số:**

| Tham số | Mô tả | Định dạng ngày |
|---|---|---|
| Min | Giá trị / ngày nhỏ nhất | `yyyy-MM-dd` hoặc `dd/MM/yyyy` |
| Max | Giá trị / ngày lớn nhất | `yyyy-MM-dd` hoặc `dd/MM/yyyy` |

**Ví dụ cấu hình — Số lượng đặt hàng:**
```
Loại:  Range
Min:   1
Max:   9999
```
→ Expression: `SoLuong >= 1 && SoLuong <= 9999`

**Ví dụ cấu hình — Ngày hợp đồng:**
```
Loại:  Range
Min:   2020-01-01
Max:   2099-12-31
```
→ Expression: `NgayHopDong >= 2020-01-01 && NgayHopDong <= 2099-12-31`

**Ví dụ thực tế:**

| Field | Min | Max | Ý nghĩa |
|---|---|---|---|
| Số lượng | 1 | 9999 | Không cho nhập 0 hoặc âm |
| Tuổi | 18 | 65 | Giới hạn tuổi lao động |
| Đơn giá | 0 | — | Không cho số âm |
| Năm sinh | 1940 | 2010 | Khoảng năm hợp lệ |

---

### 4.3 Regex — Biểu thức chính quy

**Dùng cho:** Kiểm tra định dạng chuỗi (email, số điện thoại, mã định danh...)

**Tham số:**

| Tham số | Mô tả |
|---|---|
| Pattern | Biểu thức Regex (cú pháp .NET) |

**Template có sẵn** (click để áp dụng ngay):

| Template | Pattern | Ví dụ hợp lệ |
|---|---|---|
| Email | `^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$` | `user@company.vn` |
| Số điện thoại VN | `^(0\|\+84)[0-9]{9,10}$` | `0912345678` |
| Mật khẩu mạnh | `^(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#$%]).{8,}$` | `Abc@12345` |
| Mã định danh | `^[A-Z0-9_]{3,50}$` | `NV_001`, `PB001` |
| Chỉ số nguyên | `^[0-9]+$` | `12345` |
| Chỉ chữ cái | `^[a-zA-ZÀ-ỹ\s]+$` | `Nguyễn Văn A` |
| URL | `^https?://[^\s/$.?#].[^\s]*$` | `https://icare247.vn` |

**Ví dụ cấu hình — Email công ty:**
```
Loại:   Regex
Pattern: ^[a-zA-Z0-9._%+\-]+@company\.vn$
```

**Ví dụ cấu hình — Mã nhân viên:**
```
Loại:   Regex
Pattern: ^NV[0-9]{4,6}$
```
→ Hợp lệ: `NV0001`, `NV123456` | Không hợp lệ: `nv001`, `001NV`

> **Lưu ý cú pháp:** Regex dùng chuẩn .NET. Ký tự đặc biệt cần escape với `\`.
> Dùng [regex101.com](https://regex101.com) (chọn flavor `.NET`) để test pattern trước.

---

### 4.4 Compare — So sánh field

**Dùng cho:** Kiểm tra mối quan hệ giữa 2 field trong cùng form.

**Tham số:**

| Tham số | Mô tả |
|---|---|
| Field Code | Field cần so sánh (chọn từ dropdown) |
| Toán tử | `==`, `!=`, `>`, `>=`, `<`, `<=` |

**Ví dụ cấu hình — Ngày nghỉ việc phải sau ngày vào làm:**
```
Field hiện tại: NgayNghiViec
So sánh với:    NgayVaoLam
Toán tử:        >=
```
→ Expression: `NgayNghiViec >= NgayVaoLam`

**Ví dụ cấu hình — Giá bán không thấp hơn giá vốn:**
```
Field hiện tại: GiaBan
So sánh với:    GiaVon
Toán tử:        >=
```

**Bảng toán tử:**

| Toán tử | Ý nghĩa | Dùng khi |
|---|---|---|
| `==` | Bằng | 2 field phải cùng giá trị |
| `!=` | Khác | 2 field phải khác nhau |
| `>` | Lớn hơn (không bằng) | Field này phải lớn hơn field kia |
| `>=` | Lớn hơn hoặc bằng | Field này ≥ field kia |
| `<` | Nhỏ hơn | Field này phải nhỏ hơn field kia |
| `<=` | Nhỏ hơn hoặc bằng | Field này ≤ field kia |

> **Lưu ý:** Field Code phân biệt hoa/thường. `NgayVaoLam` ≠ `ngayvaoLam`.
> Dropdown tự động load danh sách field trong form — không cần nhớ code thủ công.

---

### 4.5 Numeric — Chỉ số

**Dùng cho:** Bắt buộc field chỉ chứa số (dùng khi EditorType là TextBox nhưng giá trị phải là số).

**Tham số:** Tương tự Range — Min/Max tùy chọn.

**Ví dụ:** Mã số thuế chỉ gồm chữ số, 10-13 ký tự:
```
Loại:  Numeric
Min:   10
Max:   13
```

> **Tip:** Nếu field đã dùng EditorType = `NumericBox`, không cần thêm rule Numeric vì
> control đã chặn nhập ký tự không phải số. Rule Numeric hữu ích khi dùng `TextBox` để nhập
> số dạng chuỗi (mã số thuế, số CMND...).

---

### 4.6 Custom — Điều kiện tự do

**Dùng cho:** Mọi logic kiểm tra không thuộc các loại trên.

**Cách cấu hình:**
1. Chọn `Custom`
2. Click **`{} Expression Builder`**
3. Dùng Expression Builder để kéo-thả / nhập điều kiện
4. Xác nhận → expression tự điền vào ô điều kiện

**Ví dụ expression Custom:**
```
// Ngày phải trong năm hiện tại
year(NgayChungTu) == year(today())

// Tỷ lệ chiết khấu không vượt 50%
TyLeChietKhau >= 0 && TyLeChietKhau <= 50

// Email hoặc để trống
len(Email) == 0 || regexMatch(Email, "^.+@.+\\..+$")
```

**Hàm có sẵn trong Expression:**

| Hàm | Mô tả | Ví dụ |
|---|---|---|
| `len(x)` | Độ dài chuỗi | `len(HoTen) >= 2` |
| `trim(x)` | Bỏ khoảng trắng đầu/cuối | `len(trim(MaCode)) > 0` |
| `today()` | Ngày hôm nay | `NgaySinh <= today()` |
| `year(x)` | Lấy năm | `year(NgayHopDong) >= 2024` |
| `month(x)` | Lấy tháng | `month(NgayHieuLuc) >= 1` |
| `iif(cond, t, f)` | Điều kiện inline | `iif(LoaiHD=="A", GiaTri>=1000, true)` |
| `regexMatch(x, p)` | Kiểm tra regex | `regexMatch(Email, "^.+@.+$")` |
| `toDate(s)` | Parse chuỗi → Date | `NgayBD <= toDate("2099-12-31")` |

---

## 5. Mức độ nghiêm trọng (Severity)

| Severity | Màu | Hành vi khi vi phạm |
|---|---|---|
| **Error** | Đỏ | **Chặn submit** — người dùng bắt buộc phải sửa |
| **Warning** | Vàng | Hiển thị cảnh báo nhưng **vẫn cho phép submit** |
| **Info** | Xanh | Chỉ hiện gợi ý, không ảnh hưởng submit |

**Khi nào dùng Warning thay Error:**
- Dữ liệu có vẻ sai nhưng có thể hợp lệ trong trường hợp đặc biệt
- Ví dụ: Số lượng đặt hàng > 1000 → cảnh báo xác nhận, không chặn
- Ví dụ: Ngày hợp đồng trong quá khứ → cảnh báo, vẫn cho lưu

---

## 6. Thông báo lỗi (Error Key)

**Error Key** là khóa i18n — văn bản thông báo thực tế được quản lý trong **I18n Manager**.

### Auto-generate

Hệ thống tự sinh Error Key theo pattern:
```
{tableCode}.val.{fieldCode}.{ruleType}
```

**Ví dụ:**
- Bảng `NhanVien`, field `HoTen`, rule `Length` → `nhanvien.val.hoten.length`
- Bảng `DonHang`, field `SoLuong`, rule `Range` → `donhang.val.soluong.range`

### Đặt nội dung thông báo

Sau khi lưu rule, vào **I18n Manager** để điền nội dung:

| Key | VI | EN |
|---|---|---|
| `nhanvien.val.hoten.length` | Họ tên phải từ 2 đến 100 ký tự | Name must be between 2 and 100 characters |
| `donhang.val.soluong.range` | Số lượng phải từ 1 đến 9999 | Quantity must be between 1 and 9999 |

> **Lưu ý:** Hệ thống tự INSERT key vào `Sys_Resource` khi lưu rule (nếu key chưa tồn tại),
> với giá trị mặc định = tên field. Vào I18n Manager để thay bằng thông báo thân thiện.

---

## 7. Thứ tự thực thi

Rules được thực thi theo cột `#` từ nhỏ đến lớn. **Tất cả rules đều được check** — không dừng ở rule lỗi đầu tiên.

**Điều chỉnh thứ tự:**
- Chọn rule → click `↑` `↓` trên toolbar
- Hoặc dùng phím `Alt+↑` / `Alt+↓`
- Hoặc right-click → "Di chuyển lên/xuống"

**Ảnh hưởng đến UX:** Rules có `OrderNo` nhỏ hơn hiển thị lỗi trước trong danh sách lỗi trên form.

**Khuyến nghị thứ tự:**
```
1 — Numeric / Length (kiểm tra cơ bản nhất)
2 — Range / Regex (kiểm tra định dạng)
3 — Compare (kiểm tra liên field)
4 — Custom (logic phức tạp)
```

---

## 8. Kịch bản thực tế

### Kịch bản 1: Field "Họ tên nhân viên"

| # | Loại | Cấu hình | Severity | Ý nghĩa |
|---|---|---|---|---|
| 1 | Length | Min=2, Max=100 | Error | Không cho nhập tên quá ngắn/dài |
| 2 | Regex | `^[a-zA-ZÀ-ỹ\s]+$` | Error | Chỉ cho ký tự chữ cái và khoảng trắng |

---

### Kịch bản 2: Field "Email"

| # | Loại | Cấu hình | Severity | Ý nghĩa |
|---|---|---|---|---|
| 1 | Length | Max=255 | Error | Giới hạn độ dài DB |
| 2 | Regex | Email template | Error | Kiểm tra định dạng email |

---

### Kịch bản 3: Field "Ngày nghỉ việc"

| # | Loại | Cấu hình | Severity | Ý nghĩa |
|---|---|---|---|---|
| 1 | Compare | `NgayNghiViec >= NgayVaoLam` | Error | Không cho nhập trước ngày vào làm |
| 2 | Compare | `NgayNghiViec <= today()` | Warning | Cảnh báo nếu ngày tương lai |

---

### Kịch bản 4: Field "Số lượng đặt hàng"

| # | Loại | Cấu hình | Severity | Ý nghĩa |
|---|---|---|---|---|
| 1 | Range | Min=1, Max=9999 | Error | Phải là số dương, không quá 9999 |
| 2 | Custom | `SoLuong <= TonKho` | Warning | Cảnh báo vượt tồn kho (không chặn) |

---

### Kịch bản 5: Field "Mật khẩu"

| # | Loại | Cấu hình | Severity | Ý nghĩa |
|---|---|---|---|---|
| 1 | Length | Min=8, Max=255 | Error | Tối thiểu 8 ký tự |
| 2 | Regex | Mật khẩu mạnh template | Error | Phải có chữ hoa, số, ký tự đặc biệt |

---

## 9. Phím tắt

| Phím | Hành động |
|---|---|
| `Ctrl+N` | Thêm rule mới |
| `Ctrl+S` | Lưu tất cả thay đổi |
| `Alt+↑` | Di chuyển rule lên |
| `Alt+↓` | Di chuyển rule xuống |
| `Enter` (chọn row) | Mở edit panel |
| `Delete` (chọn row) | Xóa rule (có xác nhận) |
| `Esc` | Đóng edit panel (hủy thay đổi chưa lưu) |

---

## Xem thêm

- [FieldConfig Guide](09_FIELD_CONFIG_GUIDE.md) — Cấu hình tổng thể field
- [Event Editor Guide](../form-runtime-flow.txt) — Cấu hình Event & Action
- [I18n Manager](../spec/08_CONVENTIONS.md) — Quản lý bản dịch thông báo lỗi
- [Grammar V1 Spec](03_GRAMMAR_V1_SPEC.md) — Cú pháp Expression đầy đủ cho Custom rule
