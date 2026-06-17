# Hướng dẫn sử dụng màn **Quản lý menu**

> Màn này giúp bạn **đưa các màn (View) đã cấu hình lên menu bên trái** bằng cách **chọn**, không phải gõ
> đường dẫn hay viết SQL. Vào: **Quản trị hệ thống → Quản lý menu**.

---

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

**Sắp xếp thứ tự:** sửa số ở ô **Thứ tự** của từng node (nhỏ → hiện trước trong cùng cấp).

**Đổi vị trí trong cây:** sửa **Node cha** của node. (Hệ chặn việc đặt một node vào chính nhánh con của nó.)

**Sửa:** bấm **Sửa** ở dòng tương ứng trong cây → chỉnh trong form bên phải → **Lưu**.

**Ẩn tạm (không xóa):** bỏ tích **Kích hoạt** → mục biến mất khỏi menu nhưng vẫn còn để bật lại sau.

**Xóa hẳn:** bấm **Xóa**. Lưu ý — **không xóa được** khi:
- Node là **mục hệ thống** (mặc định của phần mềm) → chỉ ẩn được bằng Kích hoạt.
- Node **đang có mục con** → di chuyển/xóa các mục con trước.

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
