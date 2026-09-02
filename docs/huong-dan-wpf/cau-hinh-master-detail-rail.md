# Hướng dẫn cấu hình màn Master-Detail / Rail (hồ sơ) trong ConfigStudio

> **Tài liệu này dành cho ai?** Người cấu hình hệ thống (Admin, Business Analyst, IT triển khai) —
> **không cần biết lập trình**. Nếu bạn là lập trình viên/AI cần tra cứu nhanh, đi thẳng xuống
> [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật).
>
> **Bài này dùng để làm gì?** Dựng màn **hồ sơ** = 1 bản ghi chủ + nhiều bảng con (hồ sơ nhân viên,
> khách hàng, nhà cung cấp, tài sản…), bố cục **RAIL WORKSPACE**: thanh thông tin định danh dính trên
> đầu + rail điều hướng con bên trái + phần nội dung bên phải đổi theo pane đang chọn. Bạn **không cần
> viết code** — chỉ khai báo Form + các pane trong ConfigStudio, hệ thống tự vẽ ra màn hình thật.
>
> Ví dụ xuyên suốt: **Hồ sơ nhân viên `NS_NhanVien`** + 6 bảng con (Địa chỉ, Học vấn, Ngoại ngữ,
> Chứng chỉ, Thân nhân, Giấy tờ nước ngoài).

---

## Vài thuật ngữ cần biết trước khi đọc

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **Form** | Màn hình **nhập/sửa 1 bản ghi** — VD hồ sơ 1 nhân viên. |
| **View** | Màn hình **danh sách nhiều bản ghi** dạng lưới — nơi bấm vào 1 dòng để mở hồ sơ. |
| **Field** | 1 ô nhập liệu trên Form (ứng với 1 cột dữ liệu). |
| **Rail** | Thanh điều hướng dọc bên trái trang hồ sơ, liệt kê các mục con (VD "Học vấn", "Ngoại ngữ"…) để bấm chuyển qua lại. |
| **Pane** | 1 mục trên rail, ứng với **1 bảng con** — bấm vào pane sẽ hiện lưới dữ liệu của bảng con đó ở phần bên phải. |
| **Bản ghi chủ (master)** | Bản ghi chính đang xem/sửa (VD 1 nhân viên cụ thể) — các pane đều xoay quanh bản ghi này. |
| **Bảng con (detail)** | Bảng dữ liệu phụ, gắn với đúng 1 bản ghi chủ qua 1 cột khóa ngoại (VD mọi dòng "Học vấn" đều gắn với 1 `NhanVien_Id`). |
| **Khóa ngoại (FK)** | Cột trong bảng con dùng để "trỏ về" đúng bản ghi chủ nào — sai cột này thì lưới con hiện sai/rỗng dữ liệu. |

> Khác màn chứng từ (hóa đơn, phiếu…: lưới nằm ngay trong thân form, lưu gộp 1 lần): hồ sơ dùng
> **Rail** + mỗi dòng con **lưu ngay** khi bấm Lưu trên popup dòng đó. Đặc tả kỹ thuật đầy đủ:
> [33_RAIL_WORKSPACE_MASTER_DETAIL_SPEC.md](../spec/33_RAIL_WORKSPACE_MASTER_DETAIL_SPEC.md).

---

## Phần A — Làm theo từng bước

### Chuẩn bị trước khi bắt đầu

Trước khi mở ConfigStudio ra làm, cần chắc chắn các điều sau (thường IT kỹ thuật đã chuẩn bị sẵn,
bạn chỉ cần kiểm tra):

- **`db/106` đã chạy** trên Config DB (tạo bảng `Ui_Form_Detail` + cột `Ui_Form.Detail_Layout`).
  Chưa chạy → màn không có gì để cấu hình, và Web hiển thị form phẳng như cũ (không lỗi).
- **Form MASTER đã có** (Ui_Form + field). `NS_NhanVien` đã có (db/105).
- **Các form CON đã cấu hình xong** như màn danh mục thường (mỗi form con = 1 bảng con, có đủ field +
  cột lưới). Xem [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md). Mỗi bảng con **phải có cột FK**
  trỏ về master (vd `NhanVien_Id`).
- **Đã có MÀN LIST cho form master** để vào được bản ghi → rail. Cách chuẩn: tạo **Ui_View grid** +
  **menu** (như [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md)), đặt **Edit_Form = form master
  Rail**. Khi bấm Sửa/double-click 1 dòng, grid tự **điều hướng sang trang rail** (không mở popup) vì
  Edit_Form là `Detail_Layout='Rail'`. → Chưa có màn list = chưa vào được rail.

> Cấu hình rail nhập **qua ConfigStudio**, KHÔNG seed bằng SQL (quy tắc dự án).
>
> ℹ️ Qua đường **View grid**, `Display_Mode` của form master **không quan trọng** (grid mở thẳng trang
> rail). `Display_Mode='Tab'` chỉ cần nếu vào rail qua màn generic `/master/{FormCode}` (MasterDataList).

---

### Mở màn

**Mục đích:** biết vào đúng màn cấu hình rail và nhận diện bố cục màn hình trước khi thao tác.

**Làm gì:** menu trái, chọn **Forms › Master-Detail / Rail** (icon ▤ — "Cấu hình Master-Detail / Rail").

**Bạn sẽ thấy gì:** màn chia 4 vùng — trên cùng chọn **Form master + Bố cục**, bên trái là **lưới các
pane** đã tạo, bên phải là **Editor** để soạn 1 pane, dưới cùng là thanh **＋ Tạo / 💾 Lưu / 🗑 Ẩn /
⟳ Tải lại**.

---

### Bước 1 — Đặt bố cục Rail cho form master

**Mục đích:** báo cho hệ thống biết form này sẽ hiển thị theo kiểu **hồ sơ có rail**, thay vì form
phẳng bình thường.

**Làm gì:**
1. **Form master:** chọn form chủ (vd `NS_NhanVien`).
2. **Bố cục chi tiết:** chọn **`Rail`**.
3. Bấm **💾 Lưu bố cục**.

**Bạn sẽ thấy gì:** form master được đánh dấu dùng bố cục Rail — chưa có pane nào nên rail còn trống,
việc thêm pane làm ở Bước 2. (`Inline` = lưới nằm trong thân form kiểu chứng từ, là bố cục mặc định
khi chưa đổi.)

**Lỗi thường gặp:** quên bấm **💾 Lưu bố cục** sau khi chọn `Rail` → đổi bố cục không được ghi lại.

---

### Bước 2 — Tạo từng pane (mỗi bảng con = 1 pane)

**Mục đích:** thêm 1 mục vào rail cho từng bảng con (VD "Học vấn", "Ngoại ngữ"…), để người dùng bấm
vào là thấy đúng lưới dữ liệu của bảng con đó.

**Làm gì:** với mỗi bảng con, lặp lại:
1. Bấm **＋ Tạo pane** (`Ctrl+N`).
2. Điền Editor bên phải (chi tiết từng ô ở bảng dưới).
3. Bấm **💾 Lưu pane** (`Ctrl+S`).

Chọn 1 dòng trong lưới trái để sửa lại · **🗑 Ẩn** = soft-delete (bỏ `Is_Active`) · **⟳ Tải lại** (`F5`).

> **KHÔNG** tạo pane cho "Thông tin chung" — mục này (các field vô hướng của form master) là **tự động**,
> luôn nằm đầu rail.

**Bạn sẽ thấy gì:** mỗi pane vừa Lưu xuất hiện thêm 1 dòng trong lưới bên trái, theo đúng **Order_No**
đã đặt.

**Lỗi thường gặp:** xem [Bảng lỗi thường gặp](#bảng-lỗi-thường-gặp-chống-cấu-hình-sai) bên dưới —
đa số lỗi nằm ở gõ sai **Parent_Key_Column** hoặc **Icon**.

#### Tham chiếu từng ô Editor

| Ô | Bắt buộc | Giá trị / ví dụ | Ghi chú |
|---|---|---|---|
| **Detail_Code** | ✅ | `HocVan` | Mã pane, **unique trong form**. Viết liền không dấu. Là key rail, thành phần key i18n tự sinh, + nhãn dự phòng khi chưa nhập Tiêu đề. |
| **Pane_Type** | ✅ | `Grid` | `Grid` = lưới CRUD bảng con. `Timeline` = dòng thời gian — **hoãn** (hiện chỉ ra placeholder). |
| **Order_No** | — | `1`, `2`, `3`… | Thứ tự pane trên rail (tăng dần). |
| **Detail_Form** | ✅ (Grid) | `NS_NhanVien_HocVan` | Form CON định nghĩa cột lưới + CRUD. Chọn từ dropdown. |
| **Parent_Key_Column** | ✅ (Grid) | `NhanVien_Id` | Cột FK **thật** trên bảng con trỏ về master. Gõ SAI → lưới rỗng (xem bảng lỗi). |
| **Save_Mode** | — | `Immediate` | Hồ sơ → **Immediate** (mỗi dòng lưu ngay). `WithMaster` là của chứng từ. |
| **Cho thêm** | — | tick | Ẩn/hiện nút **+ Thêm mới** (còn cần quyền Thêm trên form con). |
| **Cho xóa** | — | tick | Ẩn/hiện nút **🗑** (còn cần quyền Xóa trên form con). |
| **Tiêu đề pane (tiếng Việt)** | — | `Học vấn` | **Gõ THẲNG nhãn tiếng Việt** (không gõ key). Key i18n **tự sinh** theo cấu trúc `{formcode}.detail.{detailcode}.title` (hiện read-only dưới ô). Nút **🌐 Dịch** mở dialog nhập ngôn ngữ khác. Lưu vào `Sys_Resource` khi **💾 Lưu pane**. Trống → fallback `Detail_Code`. |
| **Icon** | — | `list` | **Tên icon** (không phải emoji) — xem danh sách hợp lệ bên dưới. Tên lạ → chấm tròn. |
| **Group_Key** | — | `RELATED` | **Mã** nhóm (code) gom pane cùng nhóm. Nhãn hiển thị nhập ở ô "Nhãn nhóm (tiếng Việt)" + 🌐 (key tự sinh, chia sẻ theo nhóm). |
| **Is_Active** | — | tick | Bỏ tick = ẩn pane. |
| Edit_Mode / Kéo sắp / Min dòng | — | — | **Chưa dùng ở Rail** (để dành cho chứng từ/phase sau) — cứ để mặc định. |

---

### Bước 3 — Đẩy cấu hình xuống tenant & kiểm tra

**Mục đích:** đưa cấu hình vừa khai báo ra môi trường thật và xác nhận rail hoạt động đúng.

**Làm gì:**
1. *(Nếu master khác tenant)* App web → **Quản trị › Đồng bộ cấu hình** → **Xem trước** → **Áp dụng**.
   *(Dev: master = tenant nên đã ở đúng DB, có thể bỏ qua bước này.)*
2. Web → **Danh mục › (màn của form master)** → **Sửa 1 bản ghi** → rail hiện ra.
3. Nếu Web chưa đổi sau khi sửa cấu hình: bấm **Xóa cache** ở màn danh sách rồi mở lại.

**Bạn sẽ thấy gì:** trang rail hiện "Thông tin chung" + các pane vừa tạo. Chọn 1 pane → thêm/sửa/xóa
được dòng con. ✅

**Lỗi thường gặp:** xem [Bảng lỗi thường gặp](#bảng-lỗi-thường-gặp-chống-cấu-hình-sai) bên dưới —
các lỗi phổ biến nhất ở bước này là "vẫn ra popup phẳng" và "chỉ thấy Thông tin chung".

---

## Ví dụ hoàn chỉnh — `NS_NhanVien` (6 pane)

Sáu form con (db/105) đều có khóa cha `NhanVien_Id`, `Pane_Type=Grid`, `Save_Mode=Immediate`,
`Group_Key=RELATED`:

| Order_No | Detail_Code | Detail_Form | Parent_Key_Column | Icon |
|---|---|---|---|---|
| 1 | `DiaChi` | `NS_NhanVien_DiaChi` | `NhanVien_Id` | `building` |
| 2 | `HocVan` | `NS_NhanVien_HocVan` | `NhanVien_Id` | `list` |
| 3 | `NgoaiNgu` | `NS_NhanVien_NgoaiNgu` | `NhanVien_Id` | `languages` |
| 4 | `ChungChi` | `NS_NhanVien_ChungChi` | `NhanVien_Id` | `package` |
| 5 | `ThanNhan` | `NS_NhanVien_ThanNhan` | `NhanVien_Id` | `users` |
| 6 | `GiayToNuocNgoai` | `NS_NhanVien_GiayToNuocNgoai` | `NhanVien_Id` | `credit-card` |

---

## Bảng lỗi thường gặp (chống cấu hình sai)

| Triệu chứng | Nguyên nhân & cách sửa |
|---|---|
| **Chưa vào được** hồ sơ nhân viên | Chưa tạo màn list: tạo **Ui_View grid + menu** cho form master, đặt **Edit_Form = form master Rail**. |
| Sửa 1 dòng vẫn ra **popup phẳng**, không phải trang rail | (a) `Detail_Layout` chưa = `Rail` (Bước 1) · (b) 0 pane `Is_Active` · (c) **Edit_Form** của View không trỏ đúng form master Rail · (d) tenant chưa chạy `db/106`. (Grid chỉ điều hướng trang rail khi Edit_Form là Rail.) |
| Vào trang rail nhưng chỉ có "Thông tin chung" | Đang **Thêm mới** (rail cần Id — chỉ ở chế độ Sửa). Lưu xong mở Sửa mới thấy đủ pane. |
| Rail hiện nhưng **lưới con rỗng** dù có dữ liệu | `Parent_Key_Column` gõ SAI (không phải cột thật của bảng con). Engine chủ động trả rỗng (không dám hiện dòng của cha khác). Kiểm đúng tên cột FK. |
| Mục rail hiện **chấm tròn** thay vì icon | `Icon` không thuộc bộ đã đăng ký, hoặc gõ emoji. Dùng tên trong danh sách ở [Phần B](#phần-b--tra-cứu-kỹ-thuật). |
| Nhãn rail hiện **mã** (vd `HocVan`) | Chưa nhập "Tiêu đề pane (tiếng Việt)" khi lưu pane → chưa có resource i18n → fallback `Detail_Code`. |
| Thêm dòng con báo **lỗi FK / thiếu master** | `Parent_Key_Column` sai → engine không gán được khóa cha. Kiểm tên cột. |
| Đổi cấu hình xong Web chưa đổi | Bấm **Xóa cache** ở màn danh sách rồi mở lại. |

---

## Phần B — Tra cứu kỹ thuật

> Dành cho người đã quen cách làm ở Phần A, hoặc lập trình viên/AI cần tra nhanh tên trường kỹ thuật.

### Icon hợp lệ

Ô **Icon** nhận **tên** (không phải emoji). Tên lạ/emoji → engine vẽ **chấm tròn** (không vỡ layout).
Danh sách đăng ký hiện có:

```
layout-grid  building  users     clock          package        credit-card
bar-chart    sliders   wrench    chevron-right  chevrons-left  dot
languages    plus      search    x              pencil         trash
refresh-cw   upload    save      list           alert-triangle
```

Gợi ý hồ sơ: `users` (thân nhân), `building` (địa chỉ), `languages` (ngoại ngữ), `package`/`list`
(chứng chỉ/học vấn), `credit-card` (giấy tờ).

---

### Nhãn i18n (rail đẹp tiếng Việt)

Nhãn cấu-hình-được của rail đi theo **Hệ 1 (metadata-driven, `Sys_Resource`)** — **KHÔNG** phải JSON
overlay client, và **không ai gõ key tay vào `{lang}.json`** (xem [HUONG_DAN_I18N.md](../HUONG_DAN_I18N.md)).

- **Nhãn pane:** **gõ thẳng nhãn tiếng Việt** vào ô "Tiêu đề pane" (như ô Nhãn field). Key i18n
  **tự sinh** theo cấu trúc `{formcode}.detail.{detailcode}.title` (hiện read-only dưới ô, **không gõ tay**).
  Nút **🌐 Dịch** mở dialog "Dịch đa ngôn ngữ" để nhập en/… Bản dịch ghi vào `Sys_Resource` khi Lưu pane;
  backend resolve theo `Lang_Code`. Trống/chưa dịch → rail fallback `Detail_Code`.
- **Nhãn nhóm:** `Group_Key` là **mã nhóm** (code, vd `RELATED`) để gom pane. Nhãn hiển thị nhập ở ô
  **"Nhãn nhóm (tiếng Việt)"** — gõ thẳng (vd `Liên quan`), key i18n tự sinh
  `{formcode}.railgroup.{groupkey}.title`, nút **🌐 Dịch** cho ngôn ngữ khác. Mọi pane cùng `Group_Key`
  chia sẻ 1 nhãn. Trống → rail hiện thô `Group_Key`.

> Chuỗi giao diện cố định của rail ("Thông tin chung", "Dòng thời gian sẽ có ở phiên bản sau"…) do
> **code** đảm nhiệm qua `Loc.L("key","base vi")` (Hệ 2) — không liên quan cấu hình, không cần bạn làm gì.

---

### Giới hạn hiện tại (Pha 2)

- **Pane Timeline** ("Quá trình công tác" đọc từ biến động) — chỉ hiện placeholder, làm ở phase sau.
- **Thanh % hoàn thiện hồ sơ** + **command palette ⌘K** — chưa có.
- `Edit_Mode` (CellInline/RowPopup), `Kéo sắp thứ tự`, `Min dòng`, footer tổng — có ô nhưng **chưa
  áp dụng cho Rail** (thêm/sửa dòng con luôn qua popup dùng chung).

---

### Liên kết

- [33_RAIL_WORKSPACE_MASTER_DETAIL_SPEC.md](../spec/33_RAIL_WORKSPACE_MASTER_DETAIL_SPEC.md) — đặc tả kỹ thuật (schema, runtime, cơ chế lọc, bản đồ mã).
- [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md) — cấu hình form con (bảng con) như danh mục thường.
- [30_FORM_CHUNG_TU_SPEC.md](../spec/30_FORM_CHUNG_TU_SPEC.md) — biến thể chứng từ (`Inline`, lưu gộp transaction).
