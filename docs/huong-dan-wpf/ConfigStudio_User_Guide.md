# ICare247 ConfigStudio — Hướng Dẫn Sử Dụng

> Phiên bản: 1.0 | Cập nhật: 2026-03-20
> Đối tượng: Admin hệ thống, Business Analyst, IT cấu hình form

> **Tài liệu này dành cho ai?** Người cấu hình hệ thống (Admin, Business Analyst, IT triển khai) —
> **không cần biết lập trình**. Nếu bạn là lập trình viên/AI cần tra cứu nhanh 1 màn hình cụ thể, đi
> thẳng xuống [Phần B — Tra cứu từng màn hình](#phần-b--tra-cứu-từng-màn-hình).
>
> **Bài này dùng để làm gì?** Đây là tài liệu **tổng quan toàn bộ ConfigStudio** — app desktop dùng để
> khai báo màn hình/form **không cần viết code** (khai báo xong → đồng bộ ra hệ thống thật). Khác các
> bài "1 tác vụ" khác trong `docs/huong-dan-wpf/`, bài này là **sổ tay tra cứu mọi màn hình** của app —
> đọc Phần A để nắm nhanh 1 lượt "làm 1 form từ đầu đến cuối", rồi quay lại Phần B khi cần tra chi tiết
> từng màn cụ thể.

---

## Vài thuật ngữ cần biết trước khi đọc

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **Form** | Màn hình **nhập/sửa 1 bản ghi** trên ứng dụng web — gồm nhiều Section, mỗi Section nhiều Field. |
| **Section** | 1 nhóm field trên form (vd nhóm "Thông tin cơ bản"). |
| **Field** | 1 ô nhập liệu trên form (ứng với 1 cột dữ liệu). |
| **Validation Rule** | Điều kiện kiểm tra dữ liệu hợp lệ (vd "Số lượng phải từ 1 đến 9999"). |
| **Event** | Hành động tự động xảy ra khi field thay đổi (vd chọn xong "Tỉnh" thì tự lọc lại "Phường/Xã"). |
| **Expression (biểu thức)** | Công thức logic dùng cho Rule/Event, dựng bằng cách kéo-thả (không gõ code). |
| **Publish** | Bước xuất bản form ra để người dùng thật sử dụng — trước đó phải qua kiểm tra (Publish Checklist). |
| **i18n** | Viết tắt "đa ngôn ngữ" — quản lý bản dịch label/thông báo cho nhiều ngôn ngữ. |

---

## Phần A — Bắt đầu nhanh (dựng 1 form từ đầu đến cuối)

> Mục tiêu: đi hết 1 lượt vòng đời của 1 form đơn giản, để hình dung tổng thể trước khi đọc sâu từng
> màn ở Phần B. Đúng theo luồng làm việc chuẩn của ConfigStudio:
>
> ```
> Tạo Form --> Thêm Section/Field --> Cấu hình Field --> Đặt Rule/Event --> Kiểm tra --> Publish
> ```

### Bước 1 — Mở ConfigStudio và tạo Form mới

**Mục đích:** khởi tạo 1 form rỗng để bắt đầu khai báo.

**Làm gì:** chạy `ConfigStudio.WPF.UI.exe` → menu trái chọn **Forms › New Form** → điền **Form_Code**
(chỉ chữ HOA/số/gạch dưới, vd `PO_ORDER`), **Form_Name**, chọn **Business Table** (bảng dữ liệu),
**Platform** (web/mobile/wpf) → bấm **[Tạo Form Mới]**.

**Bạn sẽ thấy gì:** chuyển sang màn hình Form Editor (chế độ chỉnh sửa), form đã tồn tại nhưng chưa có
Section/Field nào.

**Xem chi tiết:** [Quản lý Form (Form Manager)](#4-quan-ly-form-form-manager) · [Chỉnh sửa Form (Form Editor)](#6-chinh-sua-form-form-editor).

### Bước 2 — Thêm Section và Field

**Mục đích:** dựng khung nhập liệu — nhóm các ô lại theo Section, mỗi Section chứa các Field cần nhập.

**Làm gì:** ở cột trái (cây cấu trúc), bấm **[+ Section]** để thêm nhóm, đổi tên; sau đó bấm **[+
Field]** trong section đó để thêm từng ô nhập liệu.

**Bạn sẽ thấy gì:** cây cấu trúc hiện Section (nút gốc) → Field (nút con) đúng thứ tự vừa thêm.

**Xem chi tiết:** [Chỉnh sửa Form (Form Editor)](#6-chinh-sua-form-form-editor) — mục "Thêm Section"/"Thêm Field".

### Bước 3 — Cấu hình từng Field

**Mục đích:** khai chi tiết 1 field — nó nối với cột dữ liệu nào, hiển thị kiểu gì (ô chữ/ô số/dropdown...),
nhãn tiếng Việt là gì, có bắt buộc nhập không.

**Làm gì:** click 1 field → bấm **[⚙]** để mở **Field Config** → chọn **Column**, **Editor Type**, nhập
**Label Key**, bật/tắt **Visible/ReadOnly/Required** → **[Lưu Field]**.

**Bạn sẽ thấy gì:** field hiện đúng nhãn tiếng Việt đã đặt; nếu bật Required, hệ thống tự tạo sẵn 1
validation rule "bắt buộc nhập".

**Xem chi tiết:** [Cấu hình Field (Field Config)](#7-cau-hinh-field-field-config).

### Bước 4 — Đặt Rule/Event nếu cần

**Mục đích:** thêm điều kiện kiểm tra dữ liệu (vd số lượng phải trong khoảng) hoặc hành vi tự động (vd
chọn field này thì tự tính field khác).

**Làm gì:** trong Field Config, tab **Rules** → **[+ Thêm Rule]** để thêm validation; tab **Events** →
**[+ Thêm Event]** để thêm hành vi tự động. Biểu thức logic dựng bằng **Expression Builder** (kéo-thả,
không gõ code).

**Bạn sẽ thấy gì:** badge số lượng rule/event trên field tăng lên; **Natural Language** trong Expression
Builder diễn giải lại bằng câu tiếng Việt để bạn xác nhận đúng ý.

**Xem chi tiết:** [Validation Rule Editor](#8-validation-rule-editor) · [Event Editor](#9-event-editor) ·
[Expression Builder](#10-expression-builder). *(Bước này có thể bỏ qua nếu form đơn giản không cần kiểm
tra/hành vi tự động gì thêm.)*

### Bước 5 — Kiểm tra bằng Publish Checklist

**Mục đích:** đảm bảo form không có lỗi cấu hình trước khi cho người dùng thật sử dụng.

**Làm gì:** ở Form Editor, bấm **[Publish]** → màn Publish Checklist bấm **[Run All Checks]**.

**Bạn sẽ thấy gì:** 11 mục kiểm tra chạy lần lượt, mỗi mục ra **Passed/Failed/Warning**. Nếu có Failed,
bấm **[Jump To]** để nhảy thẳng đến màn cần sửa.

**Lỗi thường gặp:** xem [bảng "Lỗi thường gặp"](#loi-thuong-gap) ở Phụ lục (Phần B) — các lỗi phổ biến
nhất là thiếu bản dịch (Error_Key) và trùng Form_Code.

**Xem chi tiết:** [Publish Checklist](#14-publish-checklist).

### Bước 6 — Publish

**Mục đích:** xuất bản form ra để dùng thật.

**Làm gì:** khi tất cả mục kiểm tra đều **Passed** → bấm **[Publish]**.

**Bạn sẽ thấy gì:** form chuyển trạng thái đã xuất bản, người dùng cuối có thể thấy và dùng form trên
ứng dụng thật.

---

## Phần B — Tra cứu từng màn hình

> Dành cho người đã quen luồng làm việc ở Phần A, hoặc cần tra chi tiết 1 màn hình cụ thể — mỗi mục
> dưới đây là 1 màn hình riêng trong ConfigStudio, đọc độc lập không cần theo thứ tự.

### Mục lục

1. [Tổng quan](#1-tong-quan)
2. [Khởi động ứng dụng](#2-khoi-dong-ung-dung)
3. [Giao diện chính (Shell)](#3-giao-dien-chinh)
4. [Quản lý Form (Form Manager)](#4-quan-ly-form-form-manager)
5. [Xem chi tiết Form (Form Detail)](#5-xem-chi-tiet-form-form-detail)
6. [Chỉnh sửa Form (Form Editor)](#6-chinh-sua-form-form-editor)
7. [Cấu hình Field (Field Config)](#7-cau-hinh-field-field-config)
8. [Validation Rule Editor](#8-validation-rule-editor)
9. [Event Editor](#9-event-editor)
10. [Expression Builder](#10-expression-builder)
11. [Dependency Viewer](#11-dependency-viewer)
12. [Grammar Library](#12-grammar-library)
13. [i18n Manager](#13-i18n-manager)
14. [Publish Checklist](#14-publish-checklist)
15. [Cấu hình ứng dụng (Settings)](#15-cau-hinh-ung-dung-settings)

---

### 1. Tổng quan

ConfigStudio là công cụ cấu hình form metadata cho nền tảng ICare247. Cho phép:

- **Tạo và quản lý form** theo metadata-driven (không cần code)
- **Cấu hình field** với editor type, validation rules, events
- **Xây dựng biểu thức** (expression) bằng giao diện kéo-thả
- **Quản lý đa ngôn ngữ** (i18n) cho label, placeholder, error message
- **Kiểm tra trước khi publish** đảm bảo form hợp lệ

#### Luồng làm việc chính

```
Tạo Form --> Thêm Section/Field --> Cấu hình Field --> Đặt Rule/Event --> Kiểm tra --> Publish
```

---

### 2. Khởi động ứng dụng

1. Chạy file `ConfigStudio.WPF.UI.exe`
2. Ứng dụng hiển thị giao diện chính với sidebar bên trái
3. Thanh trạng thái góc trên phải hiển thị:
   - **Tenant**: tên đối tượng đang làm việc (vd: DEMO)
   - **User**: tài khoản đăng nhập (vd: admin)
   - **Connection**: trạng thái kết nối DB
   - **Cache**: trạng thái Redis
   - **Version**: phiên bản ứng dụng

---

### 3. Giao diện chính

#### Sidebar (menu trái)

| Menu | Màn hình | Mô tả |
|------|----------|-------|
| Dashboard | Trang chủ | Tổng quan hệ thống |
| **Forms** | | Nhóm quản lý form |
| - Sys Table | Quản lý bảng DB | Xem danh sách business table |
| - Form List | Danh sách form | Xem/tìm/lọc/thêm/sửa/xóa form |
| - New Form | Tạo form mới | Mở Form Editor chế độ tạo mới |
| Validation Rules | Quản lý rule | Xem/sửa validation rule |
| Events | Quản lý event | Xem/sửa event trigger + action |
| **Grammar** | | Nhóm grammar |
| - Functions | Thư viện hàm | Danh sách hàm cho phép trong expression |
| - Operators | Thư viện toán tử | Danh sách toán tử cho phép |
| i18n Keys | Quản lý đa ngôn ngữ | Quản lý key dịch và bản dịch |
| Settings | Cấu hình | Cấu hình kết nối, giao diện |

#### Thao tác

- **Click menu** để chuyển màn hình
- **Click mũi tên** để mở/đóng nhóm menu con (Forms, Grammar)
- **Click nút thu gọn** (góc trái sidebar) để ẩn/hiện sidebar
- **Đổi giao diện**: chọn giữa "Light Ocean" và "Slate Professional" trong Settings

---

### 4. Quản lý Form (Form Manager)

> Màn hình chính để xem và quản lý tất cả form trong hệ thống.

#### Giao diện

- **Thanh tiêu đề**: "Quản Lý Form" + nút [+ Tạo Form Mới] + nút [Làm mới]
- **Thanh lọc**:
  - Ô tìm kiếm (theo mã hoặc tên form)
  - Lọc Platform (Tất cả / web / mobile / wpf)
  - Lọc Business Table
  - Checkbox "Hiện form đã ẩn"
- **Bảng dữ liệu**: danh sách form với các cột:
  - Mã Form (click để xem chi tiết)
  - Tên Form, Table, Platform (badge màu), Version, Sections, Fields
  - Trạng thái (Active / Inactive)
  - Cập nhật (ngày giờ)
  - Hành động: [Sửa thông tin] [Mở Editor] [Xem trước] [Nhân bản] [Vô hiệu hóa/Khôi phục]
- **Thanh thống kê**: Hiển thị / Tổng / Active / Inactive

#### Thao tác từng bước

##### Tìm kiếm và lọc form
1. Gõ từ khóa vào ô tìm kiếm → danh sách lọc tự động
2. Chọn Platform từ dropdown để lọc theo nền tảng
3. Chọn Business Table để lọc theo bảng dữ liệu
4. Bật "Hiện form đã ẩn" để thấy form Inactive

##### Tạo form mới
1. Click **[+ Tạo Form Mới]**
2. Chuyển sang màn hình Form Editor chế độ tạo mới

##### Nhân bản form (Clone)
1. Click nút **[Nhân bản]** (biểu tượng ⧉) trên dòng form cần clone
2. Hộp thoại **Nhân bản Form** hiện lên:
   - Hiện tên form nguồn
   - Nhập **Form_Code mới** (chỉ dùng chữ HOA, số, dấu gạch dưới)
   - Hệ thống kiểm tra trùng lặp tự động khi gõ
   - Thông báo lỗi đỏ nếu Form_Code không hợp lệ hoặc đã tồn tại
3. Click **[Clone]** để xác nhận → form mới được tạo với Version = 1

##### Vô hiệu hóa form (Deactivate)
1. Click nút **[Vô hiệu hóa]** (biểu tượng khóa) trên dòng form
2. Hộp thoại **Vô hiệu hóa Form** hiện lên:
   - Hiện tên form và mã form
   - Hiện số lượng ảnh hưởng: X section, Y field, Z event
   - Cảnh báo: form sẽ bị ẩn khỏi runtime, dữ liệu không bị xóa
3. Click **[Vô hiệu hóa]** để xác nhận → form chuyển sang trạng thái Inactive

##### Khôi phục form
1. Bật "Hiện form đã ẩn" để thấy form Inactive
2. Click nút **[Khôi phục]** (biểu tượng mở khóa) → form chuyển lại Active

---

### 5. Xem chi tiết Form (Form Detail)

> Màn hình xem readonly toàn bộ thông tin form.

#### Cách mở
- Click vào **Mã Form** (link xanh gạch chân) trong Form Manager

#### Giao diện
- **Header**: Mã Form, Tên, Table, Platform, Layout Engine, Version, Checksum, trạng thái
- **5 tab**:
  - **Sections**: danh sách section với thứ tự, mã, tiêu đề, số field
  - **Fields**: tất cả field với column, section, editor type, visibility, readonly, rule count
  - **Events**: event với trigger, field target, điều kiện, số action
  - **Rules**: validation rule với loại, biểu thức, error key
  - **Audit Log**: lịch sử thay đổi (thời gian, hành động, người thực hiện)

#### Thao tác
- **[← Back]**: quay lại Form Manager
- **[Edit]**: chuyển sang Form Editor để chỉnh sửa
- **[Deactivate/Restore]**: đổi trạng thái form

---

### 6. Chỉnh sửa Form (Form Editor)

> Màn hình chính để thiết kế cấu trúc form.

#### Cách mở
- Click **[+ Tạo Form Mới]** trong Form Manager (chế độ tạo mới)
- Click **[Sửa thông tin]** hoặc **[Mở Editor]** trên dòng form (chế độ chỉnh sửa)

#### Giao diện — Chế độ tạo mới

- Header: "Tạo Form Mới" + nút [← Back]
- Card nhập thông tin:
  - **Form_Code**: chỉ cho phép A-Z, 0-9, _ (tự động viết hoa)
  - **Form_Name**: tên hiển thị của form
  - **Business Table**: chọn bảng dữ liệu từ danh sách DB
  - **Platform**: web / mobile / wpf
  - **Layout Engine**: Grid / Flex / Tab
  - **Mô tả**: ghi chú tùy chọn
- Nút **[Tạo Form Mới]** (chỉ bật khi nhập đủ và hợp lệ)

#### Giao diện — Chế độ chỉnh sửa

**Cột trái — Cây cấu trúc:**
- TreeView hiển thị cấu trúc: Section (nút gốc) → Field (nút con)
- Thanh công cụ: [+ Section] [+ Field] [Expand All] [Collapse All]
- Ô tìm kiếm để lọc node

**Cột phải — Tab chi tiết:**
- **Tab Form Info**: metadata form (code, name, platform, table, version...) gồm:
  - **Số cột** (combo 1..4 → `Ui_Form.Form_Columns`): số cột lưới nền của form. Chọn **1** = mỗi field 1 dòng độc;
    áp cho cả form full-page lẫn **popup Thêm/Sửa**. Sau khi lưu → bấm **Xóa cache** form để runtime nạp lại.
  - **Chế độ mở form** (combo Popup / Tab → `Ui_Form.Display_Mode`): popup inline hay điều hướng tab.
  - **Bề rộng tối đa** (`Ui_Form.Max_Width`, px): null = mặc định (Blazor 880px).
- **Tab Sections & Fields**: thuộc tính node đang chọn trong cây
- **Tab Events**: DataGrid event với [+ Thêm Event] [Sửa] [Xóa]
- **Tab Permissions**: bảng quyền theo role (Read / Write / Submit)

**Header actions:**
- [← Back]: quay lại Form Manager
- [Save]: lưu thay đổi (chỉ bật khi có thay đổi)
- [Publish]: chuyển sang Publish Checklist
- [View Dependencies]: mở Dependency Viewer

#### Thao tác từng bước

##### Thêm Section
1. Click **[+ Section]** trên thanh công cụ cây
2. Section mới xuất hiện cuối cây với tên mặc định
3. Click vào section để đổi tên và thuộc tính ở panel bên phải

##### Thêm Field
1. Chọn section trong cây (hoặc click [+ Field])
2. Field mới xuất hiện dưới section đã chọn
3. Click vào field → panel bên phải hiện thuộc tính cơ bản
4. Click **[⚙]** để mở **Field Config** cấu hình chi tiết

##### Di chuyển node
- Chọn node → click **[↑ Move Up]** hoặc **[↓ Move Down]**
- Hoặc kéo-thả trong cây (nếu hỗ trợ)

##### Thêm/Sửa Event
- Chuyển sang tab **Events**
- Click **[+ Thêm Event]** hoặc click **[⚙]** trên dòng event
- Chuyển sang màn hình Event Editor

##### Lưu form
- Click **[Save]** (nút chỉ bật khi có thay đổi — dấu ● vàng trên header báo hiệu)
- Hệ thống lưu và cập nhật Version++

---

### 7. Cấu hình Field (Field Config)

> Màn hình cấu hình chi tiết 1 field: column, editor type, display, behavior, rules, events.

#### Cách mở
- Click nút **[⚙]** trên field trong Form Editor

#### Giao diện

**Header**: nút [← Back], tiêu đề "Cấu hình Field", breadcrumb (Form > Section > Field), dấu ● khi chưa lưu, nút [Lưu Field] [Hủy]

**4 tab:**

##### Tab 1 — Cơ bản

3 card section:

**Card "Thông Tin Cơ Bản":**
- **Column (DB)**: dropdown chọn cột từ Sys_Column (hiển thị dạng "SoLuong (Int32, NOT NULL)")
- **Net Type**: tự động hiện kiểu .NET của column đã chọn (readonly)
- **Editor Type**: dropdown chọn loại component (TextBox, NumericBox, ComboBox, DatePicker, LookupBox, TextArea, CheckBox, ToggleSwitch)
- **Thứ tự hiển thị**: số thứ tự sắp xếp field trong section

**Card "Display (i18n)":**
- **Label Key**: nhập key (vd: `lbl.soluong`) → preview hiện bản dịch bên cạnh
- **Placeholder Key**: nhập key → preview
- **Tooltip Key**: nhập key → preview
- Link **[Manage i18n →]** để chuyển sang I18n Manager

**Card "Behavior":**
- **Visible**: bật/tắt hiển thị field trên form
- **ReadOnly**: bật/tắt chỉ xem không sửa
- **Required**: bật/tắt bắt buộc nhập → tự động tạo/xóa Required rule

##### Tab 2 — Control Props

- Hiển thị thuộc tính động dựa trên Editor Type đã chọn
- Mỗi Editor Type có bộ props riêng, ví dụ:
  - **NumericBox**: giá trị tối thiểu, tối đa, số thập phân, bước nhảy, cho phép rỗng
  - **TextBox**: độ dài tối đa, nhiều dòng, số dòng
  - **ComboBox**: API datasource, value field, display field, cho phép rỗng
  - **DatePicker**: định dạng ngày, ngày min/max
- **Raw JSON Preview**: xem JSON sẽ lưu vào DB (có thể mở rộng)

##### Tab 3 — Rules

- Hiện badge số lượng rule
- DataGrid: #, Rule Type, Expression, Error Key, Active
- Nút **[+ Thêm Rule]** → chuyển sang Validation Rule Editor
- Nút **[⚙]** trên dòng → mở rule trong Rule Editor
- Nút **[Xóa]** trên dòng → xóa rule (chưa lưu DB)

##### Tab 4 — Events

- Hiện badge số lượng event
- DataGrid: ID, Trigger, Condition, Actions count, Active
- Nút **[+ Thêm Event]** → chuyển sang Event Editor
- Nút **[⚙]** trên dòng → mở event trong Event Editor

#### Thao tác từng bước

##### Cấu hình 1 field đầy đủ
1. Chọn **Column** từ dropdown → Net Type tự động cập nhật
2. Chọn **Editor Type** phù hợp (vd: NumericBox cho số)
3. Nhập **Label Key** → kiểm tra preview bản dịch
4. Bật/tắt **Visible**, **ReadOnly**, **Required** theo yêu cầu
5. Chuyển sang tab **Control Props** → điều chỉnh thuộc tính riêng của component
6. Chuyển sang tab **Rules** → thêm validation rule nếu cần
7. Chuyển sang tab **Events** → thêm event nếu cần
8. Click **[Lưu Field]**

---

### 8. Validation Rule Editor

> Màn hình quản lý các validation rule gắn vào 1 field.

#### Cách mở
- Click **[+ Thêm Rule]** hoặc **[⚙]** trong tab Rules của Field Config

#### Giao diện
- **Header**: tên field đang cấu hình
- **Lọc**: dropdown Rule Type (Tất cả, Required, Numeric, Range, Regex, Custom)
- **DataGrid**: Rule ID, Rule Type, Expression Preview, Error Key, Severity (Error/Warning/Info), Active, Order
- **Thanh công cụ**: [+ Add Rule] [Edit] [Delete] [Move Up] [Move Down] [Open Expression Builder] [Save All] [Back]

#### Thao tác từng bước

##### Thêm rule mới
1. Click **[+ Add Rule]**
2. Panel chỉnh sửa hiện lên:
   - Chọn **Rule Type** (Required, Numeric, Range, Regex, Custom)
   - Nhập **Error Key** (vd: `err.soluong.range`)
   - Chọn **Severity** (Error / Warning / Info)
3. Click **[Open Expression Builder]** để xây dựng biểu thức
4. Lưu rule

##### Sửa biểu thức rule
1. Chọn rule trong DataGrid
2. Click **[Open Expression Builder]**
3. Xây dựng hoặc chỉnh sửa biểu thức (xem mục 10)
4. Click **[Apply]** trong dialog → biểu thức cập nhật vào rule

##### Sắp xếp thứ tự rule
- Chọn rule → click **[Move Up]** hoặc **[Move Down]**
- Thứ tự ảnh hưởng đến trình tự kiểm tra validation

---

### 9. Event Editor

> Màn hình quản lý event và action gắn vào 1 field.

#### Cách mở
- Click **[+ Thêm Event]** hoặc **[⚙]** trong tab Events của Field Config

#### Giao diện
- **Cột trái — Events DataGrid**: Event ID, Trigger, Condition Preview, Action Count, Active
- **Cột phải — Actions DataGrid**: chi tiết action của event đang chọn
  - Action ID, Action Type, Target Field, Param JSON, Order

#### Các loại Trigger

| Trigger | Khi nào kích hoạt |
|---------|-------------------|
| OnChange | Khi giá trị field thay đổi |
| OnBlur | Khi field mất focus |
| OnFocus | Khi field được focus |
| OnLoad | Khi form được load |
| OnSubmit | Khi form được submit |

#### Các loại Action

| Action Type | Mô tả |
|-------------|-------|
| SetValue | Gán giá trị cho field khác |
| SetVisible | Ẩn/hiện field khác |
| SetReadOnly | Khóa/mở field khác |
| SetRequired | Bật/tắt bắt buộc cho field khác |
| Recalculate | Tính lại giá trị field khác |
| ShowMessage | Hiện thông báo cho người dùng |
| Navigate | Chuyển trang |

#### Thao tác từng bước

##### Thêm event mới
1. Click **[+ Add Event]**
2. Event mới xuất hiện với trigger mặc định OnChange
3. Chọn trigger phù hợp từ dropdown

##### Đặt điều kiện (Condition)
1. Chọn event trong DataGrid
2. Click **[Edit Condition]**
3. Expression Builder mở lên → xây dựng điều kiện
4. Click **[Apply]** → điều kiện cập nhật

##### Thêm action
1. Chọn event (bên trái)
2. Click **[+ Add Action]** (bên phải)
3. Chọn Action Type, Target Field, nhập tham số

---

### 10. Expression Builder

> Dialog xây dựng biểu thức logic bằng giao diện trực quan (không cần viết code).

#### Cách mở
- Click **[Open Expression Builder]** từ Rule Editor hoặc Event Editor

#### Giao diện 3 cột

**Cột trái — Palette:**
- **Operators**: danh sách toán tử (+, -, ==, !=, &&, ||, ...)
- **Functions**: danh sách hàm (len, trim, iif, today, ...)
- **Fields**: danh sách field của form (SoLuong, DonGia, ...)
- Ô tìm kiếm để lọc

**Cột giữa — AST Tree:**
- Cây biểu thức hiển thị cấu trúc logic
- Chọn node để xem chi tiết
- Xóa node đã chọn

**Cột phải — Preview & Validation:**
- **Natural Language**: biểu thức được diễn giải bằng ngôn ngữ tự nhiên
- **JSON Output**: biểu thức dạng JSON (để lưu vào DB)
- **Validation Status**: kiểm tra biểu thức hợp lệ (kiểu trả về, lỗi cú pháp...)

#### Thao tác

1. Click **Operator/Function/Field** từ Palette → thêm vào AST Tree
2. Sắp xếp các node để tạo biểu thức mong muốn
3. Kiểm tra **Validation Status** — đảm bảo không có lỗi
4. Xem **Natural Language** để hiểu biểu thức vừa tạo
5. Click **[Apply]** để xác nhận → biểu thức trả về màn hình gọi

#### Ví dụ biểu thức

| Biểu thức | Diễn giải |
|-----------|-----------|
| `SoLuong >= 1 && SoLuong <= 9999` | Số lượng phải từ 1 đến 9999 |
| `TrangThai == "TuChoi"` | Khi trạng thái là "Từ Chối" |
| `iif(SoLuong > 0, SoLuong * DonGia, 0)` | Tính thành tiền, trả về 0 nếu số lượng = 0 |
| `len(trim(MaDonHang)) > 0` | Mã đơn hàng không được rỗng |

---

### 11. Dependency Viewer

> Màn hình hiển thị đồ thị phụ thuộc giữa Field, Rule và Event.

#### Cách mở
- Click **[View Dependencies]** từ Form Editor

#### Giao diện

**Cột trái — Bộ lọc:**
- Checkbox **Show Rules**: ẩn/hiện node Rule
- Checkbox **Show Events**: ẩn/hiện node Event
- Dropdown **Filter by Field**: lọc theo 1 field cụ thể
- Nút **[Auto Layout]**: tự động sắp xếp đồ thị
- Nút **[Regenerate]**: tải lại toàn bộ đồ thị

**Giữa — Canvas đồ thị:**
- **Node xanh dương** (cột trái): Field — tên field và kiểu dữ liệu
- **Node cam** (cột giữa): Rule — loại rule và error key
- **Node xanh lá** (cột phải): Event — trigger và field target
- **Mũi tên**: quan hệ phụ thuộc giữa các node

**Cột phải — Chi tiết node:**
- Khi click 1 node: hiện loại, tên, thông tin phụ
- Nút **[Open Node Editor]**: nhảy đến màn hình chỉnh sửa tương ứng

#### Thao tác
1. Xem tổng quan phụ thuộc giữa các thành phần
2. Lọc để tập trung vào 1 field cụ thể
3. Click node → xem chi tiết → click [Open Node Editor] để chuyển đến màn hình chỉnh sửa
4. Kiểm tra **Circular Dependency** (cảnh báo vòng lặp) → sửa nếu có

---

### 12. Grammar Library

> Thư viện hàm và toán tử được phép sử dụng trong biểu thức.

#### Cách mở
- Click **Grammar > Functions** hoặc **Grammar > Operators** trong sidebar

#### Tab Functions

| Cột | Mô tả |
|-----|-------|
| Function Name | Tên hàm (vd: `len`, `iif`, `today`) |
| Category | Nhóm: String, Math, Date, Logic, Conversion |
| Param Count | Số tham số |
| Return Type | Kiểu trả về |
| Description | Mô tả chức năng |
| Example | Ví dụ sử dụng |

- **Tìm kiếm**: gõ tên hàm hoặc mô tả để lọc
- **Lọc danh mục**: chọn String, Math, Date, Logic, Conversion
- **[+ Add Function]**: thêm hàm mới vào whitelist
- **[Delete]**: xóa hàm khỏi whitelist

#### Tab Operators

| Cột | Mô tả |
|-----|-------|
| Symbol | Ký hiệu (+, -, ==, &&, ...) |
| Name | Tên toán tử |
| Category | Nhóm: Arithmetic, Comparison, Logical |
| Precedence | Độ ưu tiên (số nhỏ = ưu tiên cao) |
| Description | Mô tả |

> **Lưu ý**: Chỉ những hàm/toán tử trong whitelist mới được phép dùng trong Expression Builder. Publish Checklist sẽ báo lỗi nếu biểu thức dùng hàm/toán tử không có trong whitelist.

---

### 13. i18n Manager

> Quản lý các resource key và bản dịch đa ngôn ngữ.

#### Cách mở
- Click **i18n Keys** trong sidebar
- Hoặc click **[Manage i18n →]** từ Field Config

#### Giao diện
- **Bộ lọc**: ô tìm kiếm, lọc Module (Form/Field/Rule/Event/System), checkbox "Chỉ hiện thiếu bản dịch"
- **Thống kê**: Tổng entry / Hiển thị / Thiếu bản dịch
- **DataGrid**: Resource Key, Module, Vi-Vn, En-Us, Ja-Jp, trạng thái thiếu

#### Thao tác từng bước

##### Thêm key mới
1. Click **[+ Add Entry]**
2. Dòng mới xuất hiện cuối danh sách
3. Nhập Resource Key (vd: `lbl.newfield`)
4. Chọn Module (Field)
5. Nhập bản dịch cho từng ngôn ngữ

##### Tìm key thiếu bản dịch
1. Bật checkbox **"Chỉ hiện thiếu bản dịch"**
2. Danh sách lọc chỉ còn các key chưa dịch đầy đủ
3. Nhập bản dịch trực tiếp vào ô trống trong DataGrid

##### Sửa bản dịch
- Click trực tiếp vào ô ngôn ngữ trong DataGrid → nhập/sửa bản dịch

> **Lưu ý**: Publish Checklist sẽ kiểm tra tất cả Error_Key đã được dịch đầy đủ. Nếu thiếu sẽ báo lỗi và có link nhảy đến đây.

---

### 14. Publish Checklist

> Màn hình kiểm tra trước khi publish form ra production.

#### Cách mở
- Click **[Publish]** từ Form Editor

#### 11 mục kiểm tra

| # | Kiểm tra | Mô tả |
|---|---------|-------|
| 1 | Label_Key hợp lệ | Tất cả field phải có Label_Key |
| 2 | Expression parse OK | Tất cả biểu thức JSON phải parse thành công |
| 3 | Hàm trong whitelist | Tất cả hàm dùng trong biểu thức phải có trong Grammar Library |
| 4 | Toán tử trong whitelist | Tất cả toán tử phải có trong Grammar Library |
| 5 | Rule trả về Boolean | Biểu thức validation rule phải trả về kiểu Boolean |
| 6 | Kiểu calculate tương thích | Biểu thức tính toán phải tương thích kiểu dữ liệu field |
| 7 | Không vòng lặp phụ thuộc | Không có circular dependency giữa các field |
| 8 | Độ sâu AST hợp lệ | Cây biểu thức không quá 20 tầng |
| 9 | Error_Key đã dịch | Tất cả error message đã dịch đầy đủ các ngôn ngữ |
| 10 | URL API hợp lệ | Tất cả URL trong CallAPI action phải đúng định dạng |
| 11 | Sys_Dependency đầy đủ | Bảng phụ thuộc chéo field phải được khai báo |

#### Trạng thái mỗi mục

| Trạng thái | Ý nghĩa |
|------------|---------|
| Pending | Chưa chạy |
| Running | Đang kiểm tra |
| Passed | Đạt |
| Failed | Lỗi — cần sửa |
| Warning | Cảnh báo — nên xem xét |

#### Thao tác

1. Click **[Run All Checks]** → hệ thống chạy 11 mục kiểm tra
2. Xem kết quả:
   - **Tất cả Passed**: nút [Publish] được bật
   - **Có Failed**: xem chi tiết lỗi, click **[Jump To]** để nhảy đến màn hình liên quan để sửa
3. Sửa lỗi → quay lại → chạy lại kiểm tra
4. Khi tất cả Passed → click **[Publish]** → form được xuất bản

---

### 15. Cấu hình ứng dụng (Settings)

#### Cách mở
- Click **Settings** trong sidebar

#### Các cấu hình

- **Kết nối DB**: connection string đến SQL Server
- **Tenant ID**: mã đối tượng làm việc
- **Redis**: cấu hình cache (host, port)
- **Giao diện**: chọn theme (Light Ocean / Slate Professional)

---

### Phụ lục

#### Phím tắt

| Phím | Chức năng |
|------|-----------|
| Ctrl+S | Lưu (trong Form Editor, Field Config) |
| Escape | Hủy / Đóng dialog |
| F5 | Làm mới dữ liệu |

#### Quy ước tên

| Quy ước | Ví dụ | Mô tả |
|---------|-------|-------|
| Form_Code | `PO_ORDER` | Chỉ chữ HOA, số, dấu gạch dưới |
| Label_Key | `lbl.soluong` | Tiền tố `lbl.` + tên field viết thường |
| Placeholder_Key | `ph.soluong` | Tiền tố `ph.` |
| Error_Key | `err.sl.range` | Tiền tố `err.` + viết tắt |

#### Lỗi thường gặp

| Lỗi | Nguyên nhân | Cách xử lý |
|-----|------------|------------|
| "Form_Code đã tồn tại" | Trùng mã khi tạo/clone | Đổi tên Form_Code khác |
| Publish thất bại: "Error_Key thiếu bản dịch" | Chưa dịch message sang tất cả ngôn ngữ | Mở i18n Manager → dịch các key còn thiếu |
| Publish thất bại: "Circular dependency" | 2 field phụ thuộc vòng lặp | Mở Dependency Viewer → phá vòng lặp |
| Expression không hợp lệ | Dùng hàm/toán tử không có trong whitelist | Mở Grammar Library → thêm vào whitelist |
