# Hướng dẫn cấu hình màn engine-driven (no-code) trong ConfigStudio

> **Tài liệu này dành cho ai?** Người cấu hình hệ thống (Admin, Business Analyst, IT triển khai) —
> **không cần biết lập trình**. Nếu bạn là lập trình viên/AI cần tra cứu nhanh tên trường kỹ thuật,
> đi thẳng xuống [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật).
>
> **Bài này dùng để làm gì?** Tạo ra 1 "màn danh mục" mới (ví dụ: danh sách Cấp công ty, Đơn vị tính,
> Ngân hàng...) **mà không cần viết code** — chỉ khai báo trong ConfigStudio, hệ thống tự vẽ ra màn
> hình thật. Đây là cách làm dùng cho **mọi màn danh mục đơn giản** trong toàn bộ ICare247.
>
> Ví dụ xuyên suốt cả bài: **Cấp công ty (`TC_CapCongTy`)** — danh mục đơn giản nhất (không có quan hệ
> với bảng khác), phù hợp làm ví dụ đầu tiên.

---

## Vài thuật ngữ cần biết trước khi đọc

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **Bảng dữ liệu** | Nơi lưu dữ liệu thật trong cơ sở dữ liệu (giống 1 sheet Excel có cột cố định). |
| **Form** | Màn hình **nhập/sửa 1 bản ghi** — ví dụ ô nhập "Mã", "Tên" khi bấm Thêm mới. |
| **View** | Màn hình **danh sách nhiều bản ghi** dạng lưới (bảng) — nơi bạn xem/tìm/lọc dữ liệu. |
| **Field** | 1 ô nhập liệu trên Form (ứng với 1 cột trong bảng dữ liệu). |
| **ConfigStudio** | Ứng dụng desktop bạn đang dùng để khai báo cấu hình (khác với web nghiệp vụ mà nhân viên dùng hằng ngày). |
| **Đồng bộ cấu hình** | Thao tác đưa cấu hình bạn vừa khai báo từ ConfigStudio ra **hệ thống thật** (nơi nhân viên đăng nhập dùng). Khai báo xong mà chưa đồng bộ thì chưa ai thấy được. |
| **No-code** | Bạn không cần viết 1 dòng code nào — chỉ điền form, chọn dropdown, tick chọn. |

---

## Phần A — Làm theo từng bước

### Chuẩn bị trước khi bắt đầu

Trước khi mở ConfigStudio ra làm, cần chắc chắn 2 điều sau (thường IT kỹ thuật đã chuẩn bị sẵn,
bạn chỉ cần kiểm tra):

1. **Bảng dữ liệu đã tồn tại** — ví dụ bảng `TC_CapCongTy` (Cấp công ty) đã được tạo sẵn trong cơ sở
   dữ liệu. Nếu bảng chưa có, báo IT kỹ thuật tạo bảng trước — ConfigStudio không tự tạo bảng mới.
2. **ConfigStudio đang trỏ đúng nơi làm việc** — vào **Settings → Target DB**, kiểm tra đang chọn
   đúng cơ sở dữ liệu chứa bảng ở bước 1 (không phải cơ sở dữ liệu cấu hình nội bộ). Nếu không chắc,
   hỏi người quản trị hệ thống của bạn.

> Danh mục **đơn giản** (không liên quan tới bảng khác — như Cấp công ty, Đơn vị tính, Ngân hàng,
> Quốc gia) làm đúng theo bài này. Danh mục có **liên kết tới bảng khác** (ví dụ Tỉnh/Thành phải chọn
> thuộc Quốc gia nào, Phường/Xã phải chọn thuộc Tỉnh nào) phức tạp hơn 1 chút — xem mục
> [Biến thể cho các danh mục khác](#bien-the-cho-cac-danh-muc-man-khac) ở cuối bài.

---

### Bước 1 — Khai báo bảng dữ liệu vào hệ thống

**Mục đích:** cho ConfigStudio biết "có 1 bảng dữ liệu tên là gì, nằm ở đâu" — đây là bước bắt buộc
đầu tiên trước khi tạo bất kỳ màn hình nào dùng bảng đó.

**Làm gì:**
1. Ở menu bên trái, chọn **Forms › Sys Table**.
2. Ở khung "Tạo mới", tìm ô dropdown ghi **"Chọn bảng / view có sẵn (Target DB)"** — bấm vào và gõ
   `TC_CapCongTy` để lọc nhanh, rồi chọn đúng bảng đó.
3. Hệ thống sẽ **tự điền sẵn** 2 ô mã kỹ thuật (Table_Code, Schema_Name) — bạn không cần gõ tay.
4. (Không bắt buộc) Sửa ô **tên hiển thị** thành `Cấp công ty` — đây là tên bạn sẽ nhìn thấy trong
   danh sách quản lý, không ảnh hưởng dữ liệu.
5. Bấm **Lưu**.

**Bạn sẽ thấy gì sau bước này:** bảng `TC_CapCongTy` xuất hiện trong danh sách bên trái màn hình Sys
Table — nghĩa là hệ thống đã "biết" tới bảng này.

**Lỗi thường gặp:** không tìm thấy bảng trong dropdown → thường do đang trỏ sai "Target DB" (xem lại
mục Chuẩn bị) hoặc bảng thật sự chưa được IT tạo.

---

### Bước 2 — Tạo màn nhập liệu (Form)

**Mục đích:** dựng ra màn hình nhỏ (popup) để người dùng **thêm mới** hoặc **sửa** 1 dòng dữ liệu —
ví dụ ô nhập "Mã", "Tên" khi ai đó bấm nút Thêm mới trên danh sách Cấp công ty.

**Làm gì:**
1. Menu: **Forms › New Form**.
2. Ở tab **Thông tin**, điền:
   - **Form Code**: gõ `TC_CAPCONGTY` (chỉ dùng CHỮ HOA, số và dấu gạch dưới `_` — hệ thống sẽ báo
     lỗi nếu gõ sai định dạng). Đây chỉ là **mã định danh nội bộ**, không cần trùng với bất cứ thứ gì
     khác trên web.
   - **Bảng nguồn dữ liệu**: chọn `TC_CapCongTy` từ dropdown (chính là bảng vừa khai báo ở Bước 1).
   - **Display Mode** (kiểu hiển thị): chọn **Popup** (màn nhỏ hiện lên khi Thêm/Sửa, không chiếm cả
     trang — phù hợp cho danh mục đơn giản).
3. Bấm **Tạo Form** để lưu lần đầu (bắt buộc phải lưu 1 lần trước khi khai báo được các ô nhập liệu
   bên dưới).
4. Bấm nút **Auto-generate fields** ("Tạo fields tự động từ cấu trúc cột") — hệ thống sẽ đọc cấu trúc
   bảng và liệt kê sẵn toàn bộ cột cho bạn chọn:
   - **Tick chọn** 3 cột: `Ma`, `Ten`, `ThuTu` — đây là 3 cột người dùng thật sự cần nhập tay.
   - **Không tick** cột `Id` và các cột "kỹ thuật hệ thống" (`CreatedBy`, `CreatedAt`, `UpdatedBy`,
     `UpdatedAt`, `IsDeleted`, `Ver`) — những cột này hệ thống tự động điền, người dùng không cần và
     không nên thấy.
   - Bấm **Generate** — 3 ô nhập liệu xuất hiện trên danh sách field.
5. Với từng ô vừa tạo, bấm vào để chỉnh chi tiết (kiểu hiển thị + nhãn tiếng Việt, có nút **🌐 Dịch**
   để thêm ngôn ngữ khác):
   - `Ma` → kiểu **TextBox** (ô nhập chữ thường), nhãn hiển thị **"Mã"**, đánh dấu **bắt buộc nhập**.
   - `Ten` → kiểu **TextBox**, nhãn **"Tên"**, đánh dấu **bắt buộc nhập**.
   - `ThuTu` → kiểu **NumericBox** (ô nhập số), nhãn **"Thứ tự"**.
6. Bấm **Lưu thay đổi**.

**Bạn sẽ thấy gì sau bước này:** Form `TC_CAPCONGTY` có đủ 3 ô nhập liệu, sẵn sàng dùng — nhưng
**chưa ai vào được** vì chưa có màn danh sách trỏ tới nó (làm ở Bước 3).

**Lỗi thường gặp:** quên bấm "Lưu thay đổi" sau khi sửa nhãn từng field → nhãn không được ghi lại.

---

### Bước 3 — Tạo màn danh sách (View dạng lưới)

**Mục đích:** dựng màn hình **danh sách** (dạng bảng/lưới) để người dùng xem toàn bộ dữ liệu, tìm
kiếm, và bấm vào 1 dòng để mở Form Sửa (Bước 2) hoặc bấm nút Thêm mới để mở Form Thêm.

> 📖 Màn "Views" có rất nhiều tùy chọn (7 tab: Cơ bản / Hành vi / Export-Print / Cây / Cột / Actions /
> Bộ lọc). Bài này chỉ hướng dẫn phần tối thiểu cần có; muốn hiểu hết từng ô, xem
> [cau-hinh-man-quan-ly-view.md](cau-hinh-man-quan-ly-view.md).

**Làm gì:**
1. Menu: **Forms › Views (Grid/Tree)**.
2. Tạo mới, điền:
   - **View_Type** (kiểu lưới): chọn **Grid** (lưới phẳng bình thường — khác **Tree** dùng cho dữ
     liệu có quan hệ cha-con như sơ đồ tổ chức).
   - **View_Code**: gõ `TC_CapCongTy` — hệ thống tự ghép thành `Grid_TC_CapCongTy`. **Lưu ý:** mã này
     phải khớp đúng với đường dẫn (route) đã khai báo ở màn Menu, nếu không màn hình sẽ không mở
     được từ menu — nếu chưa rõ phần menu, hỏi người phụ trách cấu hình menu.
   - **Bảng nguồn**: chọn `TC_CapCongTy` (vì đây là danh mục đơn giản, dùng thẳng bảng gốc, không
     cần tạo view SQL riêng).
3. Ở tab **Cột**: thêm 3 cột `Ma`, `Ten`, `ThuTu` — bấm nút **🌐** cạnh mỗi cột để đặt tiêu đề hiển
   thị: Mã / Tên / Thứ tự.
4. Ở ô **Edit_Form**: chọn form `TC_CAPCONGTY` (Form đã tạo ở Bước 2) — để khi người dùng bấm Thêm
   mới hoặc double-click 1 dòng, đúng Form popup đó sẽ hiện lên.
5. Bấm **Lưu**.

**Bạn sẽ thấy gì sau bước này:** cấu hình đầy đủ 1 màn danh mục — nhưng vẫn cần Bước 4 để đưa ra
môi trường thật.

---

### Bước 4 — Đưa cấu hình ra hệ thống thật (Đồng bộ)

**Mục đích:** những gì bạn khai báo ở Bước 1-3 hiện chỉ nằm trong ConfigStudio. Bước này đẩy cấu
hình đó xuống môi trường mà nhân viên thật sự dùng hằng ngày.

**Làm gì:**
- Mở ứng dụng web (không phải ConfigStudio) → vào **Quản trị › Đồng bộ cấu hình**.
- Bấm **Xem trước** để kiểm tra danh sách thay đổi sắp áp dụng.
- Nếu đúng, bấm **Áp dụng từ master** để chính thức đưa cấu hình xuống.

> Nếu bạn đang làm trên môi trường phát triển/thử nghiệm (dùng chung 1 nơi cấu hình và 1 nơi chạy
> thật), bước này có thể không cần thiết — hỏi người phụ trách kỹ thuật của bạn nếu không chắc.

---

### Bước 5 — Kiểm tra kết quả

- Mở ứng dụng web → vào **Danh mục › Cấp công ty**.
- Bạn sẽ thấy lưới danh sách hiện ra (có thể còn trống nếu chưa có dữ liệu).
- Bấm **Thêm mới** hoặc double-click 1 dòng → màn popup hiện ra đúng 3 ô: Mã, Tên, Thứ tự. ✅

Nếu không thấy đúng như trên, xem lại lỗi thường gặp ở từng bước phía trên, hoặc phần
[Quy ước nhanh](#quy-ước-nhanh) bên dưới.

---

## Biến thể cho các danh mục/màn khác

Cách làm ở trên áp dụng gần như y hệt cho các danh mục đơn giản khác — chỉ đổi tên bảng. Với danh
mục có liên kết tới bảng khác (ví dụ chọn Tỉnh/Thành thuộc Quốc gia nào), cần thêm bước cấu hình
liên kết — xem tài liệu riêng bên dưới, đừng tự suy luận theo bài này.

| Loại | Khác biệt |
|---|---|
| **ĐVT / Ngân hàng / Cấp phòng ban / Quốc gia** (đơn giản) | Làm y hệt ví dụ trên. Chỉ đổi tên bảng + mã màn danh sách tương ứng: `Grid_DM_DonViTinh` / `Grid_DM_NganHang` / `Grid_DM_CapPhongBan` / `Grid_DM_QuocGia`. |
| **Tỉnh/TP, Phường/Xã** (có liên kết tới bảng khác) | Hướng dẫn riêng đầy đủ: [cau-hinh-luoi-tham-chieu.md](cau-hinh-luoi-tham-chieu.md). |
| **Công ty** (có cấu trúc cây cha-con) | Hướng dẫn riêng đầy đủ: [cau-hinh-man-cong-ty.md](cau-hinh-man-cong-ty.md). |
| **Hồ sơ nhiều bảng con** (ví dụ hồ sơ nhân viên) | Không dùng cách này — xem [cau-hinh-master-detail-rail.md](cau-hinh-master-detail-rail.md). |

---

## Phần B — Tra cứu kỹ thuật

> Dành cho người đã quen cách làm ở Phần A, hoặc lập trình viên/AI cần tra nhanh tên trường kỹ thuật.
> Nội dung dưới đây là bản rút gọn — không giải thích lại từ đầu.

**Triết lý:** KHÔNG code màn, chỉ cấu hình metadata (`Sys_Table` → `Ui_Form`/`Ui_Field` → `Ui_View`)
trong ConfigStudio rồi đồng bộ xuống tenant — engine generic tự render. (ADR-024)

### Quy ước nhanh

- **Form_Code**: CHỮ HOA + số + `_` (vd `TC_CAPCONGTY`). Không cần khớp route.
- **View_Code**: `{View_Type}_{Bảng}` — `Grid_*` cho lưới phẳng, `Tree_*` cho lưới cây. **PHẢI khớp** route `/view/{View_Code}` khai trong menu (`AppNav.NavScreen.Route` hoặc `HT_ChucNang.DuongDan`).
- **Thứ tự cấu hình theo phụ thuộc**: danh mục (Quốc gia → Tỉnh → Phường/Xã; Cấp công ty…) **TRƯỚC** → rồi màn tham chiếu (Công ty…), để đủ nguồn lookup.
- **Không cấu hình cột audit** (Id/CreatedBy/At/UpdatedBy/At/IsDeleted/Ver) vào form — engine tự xử lý.
- **Chọn editor cho field danh mục/FK:** **LookupBox** = khóa ngoại (`int`) sang bảng nghiệp vụ (vd `QuocGia_Id → DM_QuocGia`) · **LookupComboBox** = mã chuỗi từ `Sys_Lookup` (vd `LoaiHinh`) · **ComboBox** = API động. Bảng so sánh + cây quyết định: xem [09_FIELD_CONFIG_GUIDE.md §2.2.1](../spec/09_FIELD_CONFIG_GUIDE.md).

### Ghi chú triển khai

- Bảng `TC_CapCongTy` đã có trong **Data DB `ICare247_Solution`** (db/037 — đã chạy).
- ConfigStudio **Settings → Target DB** trỏ vào **`ICare247_Solution`** (KHÔNG phải `QLNS_Demo`/Config).
- Danh mục **có FK** (Tỉnh/TP → Quốc gia, Phường/Xã → Tỉnh): cần view `db/051`/`db/052`.
- Bước 4 (đồng bộ) trên môi trường dev: master = tenant nên cấu hình đã ở đúng DB; bước này chủ yếu set cờ `Is_System`.
