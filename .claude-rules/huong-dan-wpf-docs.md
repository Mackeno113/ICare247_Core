# Tài liệu hướng dẫn WPF (`docs/huong-dan-wpf/`) — Rule

## Quy tắc

Mọi file `.md` trong `docs/huong-dan-wpf/` (hướng dẫn cấu hình ConfigStudio) **bắt buộc** viết theo
khuôn 2 phần — không viết lẫn lộn ngôn ngữ kỹ thuật vào phần dành cho end-user:

- **Phần A — Làm theo từng bước**: dành cho Admin/Business Analyst **không biết lập trình**. Mỗi bước
  gồm **Mục đích** (vì sao làm) → **Làm gì** (thao tác cụ thể, đúng tên nút/ô thật trên UI) →
  **Bạn sẽ thấy gì** (kết quả mong đợi) → **Lỗi thường gặp**. Không nhắc ADR/số migration/tên bảng
  kỹ thuật nội bộ (`Sys_Table`, "engine", "tenant"...) trừ khi đã giải thích trong hộp thuật ngữ đầu
  bài. Có 1 ví dụ xuyên suốt cụ thể (1 màn/trường hợp thật), không nói chung chung.
- **Phần B — Tra cứu kỹ thuật**: giữ nguyên phong cách cũ (bảng dày đặc, ADR, quy ước, ghi chú triển
  khai) — dành cho người đã quen hoặc AI/dev cần tra nhanh. Không lặp lại giải thích đã có ở Phần A.

Xem [cau-hinh-man-danh-muc.md](../docs/huong-dan-wpf/cau-hinh-man-danh-muc.md) làm mẫu chuẩn.

## Sau khi tạo/sửa bất kỳ file `.md` nào trong thư mục này

**Bắt buộc** làm đủ 2 bước sau trước khi coi task xong (giống build-verify của code):

1. Nếu là bài **mới** — thêm 1 dòng bullet `- [ten-file.md](ten-file.md) — mô tả ngắn` vào đúng mục
   (`##`) trong [`docs/huong-dan-wpf/README.md`](../docs/huong-dan-wpf/README.md).
2. Chạy lại script sinh trang xem:
   ```bash
   node docs/huong-dan-wpf/build-docs-site.js
   ```

```
❌ SAI — sửa nội dung .md rồi dừng, không chạy lại script → site.html cũ, end-user đọc bản lỗi thời.
❌ SAI — sửa tay trực tiếp site.html → mất khi script chạy lại (site.html là file SINH RA, không phải nguồn).
✅ ĐÚNG — sửa .md → (nếu là bài mới) thêm bullet README.md → chạy build-docs-site.js → báo user.
```

## Vì sao

- `.md` là **nguồn dữ liệu duy nhất** — AI agent/dev đọc thẳng, dễ diff qua git. `site.html` chỉ là
  bản hiển thị sinh ra cho end-user (menu điều hướng + tìm kiếm, mở thẳng bằng trình duyệt, không cần
  cài Markdown viewer) — không giữ 2 bản nội dung song song.
- `build-docs-site.js` đọc mục lục **trực tiếp từ README.md** để dựng menu (không có danh sách trùng
  lặp trong code) — quên cập nhật README.md thì bài mới vẫn tự hiện ở nhóm "Khác (chưa gom nhóm)"
  cuối menu (không mất), nhưng vẫn nên cập nhật README.md để đúng nhóm ngay từ đầu.

## Implementation

- Script: `docs/huong-dan-wpf/build-docs-site.js` (Node thuần, không cần cài thư viện ngoài).
- Output: `docs/huong-dan-wpf/site.html` (tự sinh, đừng sửa tay).
- Chi tiết cách hoạt động → đọc chú thích đầu file script.
