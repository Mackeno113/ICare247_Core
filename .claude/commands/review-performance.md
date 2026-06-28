# /review-performance — Soát hiệu năng backend/DB/cache

**Mục đích:** Bắt N+1 Dapper, thiếu cache, SQL nặng, scalar UDF, async sai trên diff/code.
**Input:** `$ARGUMENTS` = phạm vi (file/thư mục/query). Trống → soát `git diff` hiện tại.
**Output:** danh sách nút thắt theo tác động Cao/Vừa/Thấp + đề xuất fix.
**Agent gọi:** `performance-reviewer` (read-only).

Thực hiện:
1. Xác định phạm vi: nếu trống, lấy `git diff`.
2. Gọi `Agent` với `subagent_type: performance-reviewer`, truyền phạm vi/diff.
3. Trình bày báo cáo; nêu điểm nóng cần xử lý trước khi merge.
