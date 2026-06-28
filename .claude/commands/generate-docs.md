# /generate-docs — Cập nhật tài liệu/spec

**Mục đích:** Sinh/cập nhật doc tiếng Việt cho thay đổi (spec, guide, ADR).
**Input:** `$ARGUMENTS` = phạm vi/feature. Trống → dựa `git diff` + quyết định trong session.
**Output:** doc cập nhật trong `docs/spec` hoặc `docs/guide`, tiếng Việt, bám cấu trúc hiện có.
**Agent gọi:** _(chưa có Technical Writer agent — xử lý inline; có thể nhập sau)._

Thực hiện:
1. Xác định doc liên quan trong `docs/spec` (đánh số tiếp theo nếu spec mới — tránh trùng số).
2. Viết tiếng Việt, giữ technical term; bám format file cùng loại; thêm link chéo `[[...]]`/markdown.
3. Nếu là quyết định kiến trúc → ghi `AI_DECISIONS.md`. KHÔNG tự commit.
