# Hướng dẫn sử dụng màn **Quản lý menu**

> **Tài liệu này dành cho ai?** Người cấu hình hệ thống (Admin, Business Analyst, IT triển khai) —
> **không cần biết lập trình**. Nếu bạn là lập trình viên/AI cần tra cứu nhanh, đi thẳng xuống
> [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật).
>
> Màn này giúp bạn **đưa các màn (View) đã cấu hình lên menu bên trái** bằng cách **chọn**, không phải gõ
> đường dẫn hay viết SQL. Vào: **Quản trị hệ thống → Quản lý menu**.
>
> Ví dụ xuyên suốt cả bài: dựng cụm menu **"Danh mục nền tảng"** chứa mục con **"Khách hàng"** —
> mục con này mở đúng màn danh sách khách hàng đã cấu hình sẵn.

---

## Vài thuật ngữ cần biết trước khi đọc

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **Node** | 1 dòng/1 mục trên cây menu — có thể là 1 **Nhóm** hoặc 1 mục **Mở View**. |
| **Nhóm** | Node chỉ để gom các mục con lại; bấm vào **không mở màn nào**, chỉ xổ ra mục con. |
| **Mở View** | Node bấm vào sẽ **mở 1 màn danh sách** (View) đã cấu hình sẵn. |
| **Node cha** | Node đứng ngay bên trên node hiện tại trong cây — quyết định node nằm trong nhóm nào. |
| **Kích hoạt** | Công tắc bật/tắt hiển thị — tắt thì mục **biến mất khỏi menu** nhưng vẫn còn để bật lại sau. |
| **Vai trò** | Nhóm quyền gán cho người dùng (VD Nhân viên, Quản lý...) — quyết định ai **thấy** mục menu nào. |

---

## Phần A — Làm theo từng bước

### Bước 1 — Tạo nhóm menu

**Mục đích:** dựng 1 cụm để gom các mục menu liên quan lại với nhau — ví dụ cụm "Danh mục nền tảng"
chứa các danh mục dùng chung.

**Làm gì:**
1. Vào **Quản trị hệ thống → Quản lý menu**.
2. Bấm **+ Thêm node**.
3. **Loại node** → chọn **Nhóm (không mở màn)**.
4. **Tên hiển thị**: gõ `Danh mục nền tảng`.
5. **Node cha**: chọn `— Gốc —` (vì đây là cụm cấp cao nhất).
6. (Tùy chọn) chọn **Icon**, gõ **Thứ tự** (số nhỏ hiện trước).
7. Để ô **Kích hoạt** được tích.
8. Bấm **Lưu**.

**Bạn sẽ thấy gì:** cụm "Danh mục nền tảng" xuất hiện trên cây menu bên trái màn Quản lý menu —
chưa có mục con nên bấm vào chưa xổ ra gì, việc đó làm ở Bước 2.

**Lỗi thường gặp:** quên đổi **Loại node** sang "Nhóm" (để mặc định "Mở View") → hệ thống đòi bạn
phải chọn 1 View mới cho Lưu.

---

### Bước 2 — Tạo mục mở một màn danh sách (View)

**Mục đích:** đưa 1 màn danh sách (VD danh sách Khách hàng) đã cấu hình sẵn lên menu, để người dùng
bấm vào là mở được màn đó — không cần gõ đường dẫn tay.

**Làm gì:**
1. Bấm **+ Thêm node**.
2. **Loại node** → chọn **Mở View**.
3. **View**: bấm dropdown → chọn màn cần đưa lên menu (VD *Khách hàng*) — danh sách này lấy thẳng từ
   cấu hình, không phải gõ tay.
   - Ô **Tên hiển thị** tự điền theo tên View — sửa lại nếu muốn đổi chữ hiển thị.
   - Dòng `→ /view/...` chỉ để xem trước đường dẫn (hệ tự tạo, không cần đụng vào).
4. **Node cha**: chọn nhóm "Danh mục nền tảng" vừa tạo ở Bước 1 — để mục này nằm bên trong nhóm đó.
5. (Tùy chọn) gõ **Thứ tự** để sắp vị trí trong nhóm.
6. Bấm **Lưu**.

**Bạn sẽ thấy gì:** mục "Khách hàng" xuất hiện bên trong cụm "Danh mục nền tảng" trên cây menu.

**Lỗi thường gặp:** bấm **Lưu** không được → do **chưa chọn View** trong dropdown, hoặc ô **Tên hiển
thị** đang để trống.

---

### Bước 3 — Sắp xếp lại, sửa, ẩn hoặc xóa

**Mục đích:** chỉnh lại menu sau khi đã tạo — đổi thứ tự trước/sau, sửa tên, tạm ẩn không cho ai
thấy, hoặc xóa hẳn mục không dùng nữa.

**Làm gì:**
- **Đổi thứ tự trong cùng cấp:** bấm nút **▲ / ▼** ở cột **Thứ tự** (hoặc sửa trực tiếp số Thứ tự
  trong popup Sửa — số nhỏ hiện trước).
- **Chuyển mục sang nhóm khác:** mở **Sửa** → đổi **Node cha**.
- **Sửa tên/icon/thứ tự:** bấm **Sửa** ở dòng cần sửa (hoặc chọn dòng rồi bấm **Sửa** ở khung chi
  tiết bên phải) → chỉnh trong popup → **Lưu**.
- **Tạm ẩn không xóa:** bỏ tích ô **Kích hoạt**.
- **Xóa hẳn:** chọn dòng → bấm **Xóa** → xác nhận trong hộp thoại hiện ra.

**Bạn sẽ thấy gì:** cây menu cập nhật ngay theo thay đổi.

**Lỗi thường gặp:**
- **Không xóa được** → node là **mục hệ thống** (mặc định của phần mềm, chỉ ẩn được bằng Kích hoạt),
  hoặc node **đang có mục con bên trong** (phải xóa/chuyển mục con trước).
- Đặt **Node cha** thành chính nhánh con của node đó → hệ thống chặn (tránh vòng lặp cây), báo lỗi.

---

### Bước 4 — Cho vai trò khác nhìn thấy mục menu vừa tạo

**Mục đích:** mục menu mới tạo mặc định **chỉ quản trị cấp cao thấy**. Bước này cấp quyền để đúng
nhóm nhân viên liên quan (VD toàn bộ nhân viên) thấy được mục đó.

**Làm gì:**
1. Vào **Quản trị → Phân quyền**.
2. Chọn **vai trò** cần cấp (VD "Nhân viên").
3. Tìm mục menu vừa tạo trong danh sách → tích ô **Xem**.
4. Bấm **Lưu**.

**Bạn sẽ thấy gì:** người dùng thuộc vai trò đó sẽ thấy mục menu ở **lần chuyển trang kế tiếp** —
không cần khởi động lại hệ thống.

**Lỗi thường gặp:** đã tích **Xem** và Lưu nhưng người dùng vẫn chưa thấy → nhắc họ chuyển sang
trang khác hoặc tải lại trình duyệt; menu không tự cập nhật khi đang đứng nguyên 1 trang.

---

> Gặp trục trặc khác không nằm ở trên? Xem bảng lỗi đầy đủ ở mục 6 trong
> [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật) bên dưới.

---

## Phần B — Tra cứu kỹ thuật

> Dành cho người đã quen cách làm ở Phần A, hoặc lập trình viên/AI cần tra nhanh tên trường kỹ thuật.

## 1. Hiểu nhanh: menu gồm 2 kiểu "node"

| Kiểu node | Là gì | Bấm vào |
|---|---|---|
| **Nhóm** | Cụm gom các mục con (vd "Danh mục nền tảng") | Không mở màn, chỉ xổ ra mục con |
| **Mở View** | Một mục mở 1 màn danh sách đã cấu hình | Mở màn đó (vd `/view/Grid_KhachHang`) |

Ví dụ cây menu bạn có thể dựng:

```
Danh mục nền tảng        ← Nhóm
 ├─ Khách hàng           ← Mở View (Grid_KhachHang)
 ├─ Nhà cung cấp         ← Mở View (Grid_NhaCungCap)
 └─ Đơn vị tính          ← Mở View (Grid_DonViTinh)
```

> Mẹo: tạo **Nhóm trước**, rồi tạo các mục **Mở View** và đặt **node cha** là nhóm đó.

---

## 2. Tạo một nhóm

1. Bấm **+ Thêm node**.
2. **Loại node** → chọn **Nhóm (không mở màn)**.
3. **Tên hiển thị**: gõ tên cụm, vd `Danh mục nền tảng`.
4. **Node cha**: chọn `— Gốc —` (nếu là cụm cấp cao nhất) hoặc một nhóm khác.
5. (Tùy) **Icon**, **Thứ tự** (số nhỏ hiện trước).
6. Để ô **Kích hoạt** được tích.
7. Bấm **Lưu**.

---

## 3. Tạo mục mở một View

1. Bấm **+ Thêm node**.
2. **Loại node** → chọn **Mở View**.
3. **View**: bấm dropdown → chọn màn cần đưa lên menu (danh sách này lấy thẳng từ cấu hình).
   - Ô **Tên hiển thị** sẽ **tự điền** theo tên View — sửa lại nếu muốn.
   - Dòng `→ /view/...` cho bạn xem trước đường dẫn (hệ tự tạo, bạn không phải gõ).
4. **Node cha**: chọn nhóm muốn đặt mục này vào.
5. (Tùy) **Thứ tự** để sắp vị trí trong nhóm.
6. Bấm **Lưu**.

> Bạn **không cần** nhập đường dẫn, mã màn hay quyền — chọn View là đủ, phần còn lại hệ thống tự điền.

---

## 4. Sắp xếp, sửa, xóa

**Sắp xếp thứ tự:** bấm nút **▲ / ▼** ở cột **Thứ tự** để đẩy node lên/xuống trong cùng cấp (hoặc sửa
trực tiếp số **Thứ tự** trong popup — nhỏ → hiện trước).

**Đổi vị trí trong cây:** sửa **Node cha** của node. (Hệ chặn việc đặt một node vào chính nhánh con của nó.)

**Sửa:** bấm **Sửa** ở dòng (hoặc chọn dòng rồi bấm **Sửa** ở khung chi tiết bên phải) → chỉnh trong
**popup** → **Lưu**. Bấm một dòng bất kỳ để xem nhanh thông tin (chỉ đọc) ở khung bên phải.

**Ẩn tạm (không xóa):** bỏ tích **Kích hoạt** → mục biến mất khỏi menu nhưng vẫn còn để bật lại sau.

**Xóa hẳn:** bấm **Xóa**. Lưu ý — **không xóa được** khi:
- Node là **mục hệ thống** (mặc định của phần mềm) → chỉ ẩn được bằng Kích hoạt.
- Node **đang có mục con** → di chuyển/xóa các mục con trước.

---

## 4b. Chọn Icon

Ở popup thêm/sửa node, mục **Icon** hiện **bộ icon dùng chung** để bấm chọn (không gõ tay). Nút ✕
đầu hàng = bỏ icon.

> **Muốn thêm icon mới?** Bộ icon nằm ở file `src/frontend/ICare247.UI.Shared/Components/Icon.razor`
> (bộ Lucide — xem tên & hình tại [lucide.dev](https://lucide.dev)). Kỹ thuật thêm icon bằng 2 bước
> trong file đó: (1) dán `<path>` của icon thành 1 `case "<tên>":` trong khối `<svg>`; (2) thêm đúng
> `"<tên>"` vào mảng `RegisteredNames` ở khối `@code`. Sau khi build lại, icon mới tự xuất hiện trong
> bộ chọn ở màn này.

---

## 4c. Dịch tên menu sang ngôn ngữ khác (i18n)

Ô **Tên hiển thị** là **bản gốc tiếng Việt** — đây cũng là chữ hiển thị khi *chưa* có bản dịch.

Khi sửa một node, popup hiện thêm **Khóa i18n** (vd `nav.screen.organization.title`). Tên trên menu
được dịch **theo khóa này**: nếu từ điển ngôn ngữ đang chọn có giá trị cho khóa → menu hiện bản dịch;
nếu không → hiện bản gốc tiếng Việt.

> Khóa được **suy tự động** từ vị trí node trong cây (nhóm → phân hệ → màn), khớp đúng cách menu thật
> tra cứu. Bạn dùng khóa này để cấu hình bản dịch trong công cụ i18n.

---

## 5. Để người khác nhìn thấy mục menu vừa tạo

Mục mới mặc định **chỉ quản trị cấp cao thấy**. Muốn vai trò khác thấy:

1. Vào **Quản trị → Phân quyền**.
2. Chọn **vai trò**.
3. Tìm mục menu vừa tạo → tích **Xem**.
4. **Lưu**.

> Menu chỉ hiện những mục mà vai trò của người dùng được **Xem**. Sau khi lưu, menu cập nhật ở **lần
> chuyển trang kế tiếp** (không cần khởi động lại).

---

## 6. Gặp trục trặc? (thường gặp)

| Hiện tượng | Nguyên nhân & cách xử lý |
|---|---|
| Tạo xong nhưng **không thấy trên menu** | Chưa cấp **Xem** cho vai trò (mục 5), hoặc node bị **tắt Kích hoạt**. |
| Menu **chưa đổi** ngay | Chuyển sang trang khác / tải lại — menu nạp lại theo phiên điều hướng. |
| **Không bấm Lưu được** mục Mở View | Chưa **chọn View** trong dropdown, hoặc **Tên** đang trống. |
| **Không xóa được** node | Node hệ thống (chỉ ẩn được) hoặc node **còn mục con**. |
| Dropdown View **trống** | Chưa cấu hình View nào, hoặc View đang ở trạng thái ẩn (Is_Active = 0). |

---

## 7. Cài đặt (1 lần, do kỹ thuật làm)

Chạy `db/054_seed_ht_chucnang_menu_admin.sql` trên Data DB của tenant để mục **Quản lý menu** xuất hiện
trong Quản trị. Sau đó đăng nhập tài khoản quản trị là dùng được.

---

*Liên quan: cấu hình màn danh sách (View) xem [cau-hinh-man-quan-ly-view.md](cau-hinh-man-quan-ly-view.md).
Phần kiến trúc/kỹ thuật & đường nâng cấp thêm "node mở Form": xem ADR-026 trong `.claude/memory/architecture_decisions.md`.*
