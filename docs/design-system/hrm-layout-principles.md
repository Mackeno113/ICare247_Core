# ICare247 — Nguyên tắc bố cục UI HRM đa công ty

> Nguyên tắc **bố cục/cấu trúc** cho phân hệ HRM đa công ty (tập đoàn, multi-tenant) trên DevExpress Blazor.
> **Phạm vi:** CHỈ layout/cấu trúc. GIỮ NGUYÊN theme đã chốt (Fluent Light + `tokens.css` hiện tại) —
> KHÔNG refactor token, KHÔNG accent-màu-theo-tenant.

---

## Quy tắc số 1 — phân loại dữ liệu TRƯỚC khi chọn UI

Xác định mỗi nhóm dữ liệu thuộc loại nào; loại dữ liệu quyết định kiểu giao diện.

| Loại | Định nghĩa | Ví dụ | Giao diện đúng |
|---|---|---|---|
| **1:1** | Mỗi NV đúng 1 giá trị | Thông tin cá nhân, liên hệ, vị trí hiện tại | **Form Card ≤ 2 cột** |
| **1:N** | Một NV nhiều bản ghi | Hợp đồng, quá trình công tác, đào tạo, khen thưởng, người phụ thuộc | **DxGrid con + `DxGridCommandColumn`** |

> Sai lầm tránh: ép mọi "tab" thành form field. Phần lớn hồ sơ NS là 1:N — *danh sách có Thêm/Sửa/Xóa*.

## Điều hướng hồ sơ nhân sự

- **> 6 mục → nav dọc gom nhóm** (kiểu trang Settings), KHÔNG tab ngang (tab ngang chỉ hợp ≤ 5–6 mục).
- Gom nhóm: **Nhân thân** (Thông tin chung · Gia đình) · **Công việc** (Hợp đồng · Quá trình · Đào tạo) · **Đãi ngộ** (Lương & phụ cấp · Bảo hiểm) · **Hồ sơ** (Tài liệu đính kèm).
- Mỗi mục 1:N hiển thị **badge số bản ghi**.
- Bố cục trang chi tiết:
  ```
  [ Header: ảnh + tên + mã NV + công ty + trạng thái ]
  [ Nav dọc gom nhóm (~180px) | Vùng nội dung mục đang chọn ]
  ```

## Mẫu 1:1 — Form

- Tối đa **2 cột** desktop; gom field theo Card/section có tiêu đề.
- Label **bên trái** (desktop, mật độ cao) → **phía trên** (mobile).
- Validate tại field (`ValidationMessage` / `ValidationSummary`).

## Mẫu 1:N — Danh sách + CRUD

- Mỗi mục 1:N = **một DxGrid con**; khai báo `DxGridCommandColumn` (New/Edit/Delete sẵn).
- CRUD: ghi DB trong `EditModelSaving` (submit + validate đạt) và `DataItemDeleting` (sau xác nhận xóa).
- **EditMode theo độ phức tạp:**

  | Tình huống | EditMode |
  |---|---|
  | Nhiều trường + validate (hợp đồng, lương) | `PopupEditForm` |
  | Ít cột, đơn giản (người phụ thuộc, chứng chỉ) | `EditRow` / `EditCell` |
  | Nhập hàng loạt (import) | `BatchEdit` |

- Toolbar grid: tìm nhanh · lọc nâng cao · sort · export Excel · **saved views** · **tùy chỉnh cột**.

## Đa công ty — phần bố cục (KHÔNG phải màu)

- **Company switcher** luôn hiển thị (header góc trên trái, cạnh logo); có chế độ "Tất cả công ty" + từng công ty.
- **Ẩn/disable nút lệnh** (New/Edit/Delete) theo quyền phạm vi công ty ngay trên UI — không chỉ chặn backend.
- **Cây tổ chức** Tập đoàn → Công ty → Khối/Phòng ban → Nhân viên bằng **TreeList**; lọc NV theo node.
- Kiểm quyền tenant trong `EditModelSaving` / `DataItemDeleting`; ở chế độ "Tất cả công ty" nên xác nhận công ty đích để tránh nhập nhầm.

> Lưu ý: tài liệu gốc có đề "accent color theo công ty" — phần đó **không áp** (thuộc theme đã chốt).

## Responsive & Accessibility

- Desktop 2 cột / Tablet 1–2 / Mobile 1 cột; mobile label chuyển lên trên, nav dọc thu thành menu trượt.
- Keyboard nav đầy đủ (chuyển mục, mở popup, lưu, xóa) + contrast đạt chuẩn → đưa vào checklist nghiệm thu.
