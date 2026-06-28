# /review-security — Soát bảo mật cấp ứng dụng

**Mục đích:** Soát SQLi, rò tenant, JWT/RBAC, secret, AST-safety, audit-log trên diff/code.
**Input:** `$ARGUMENTS` = phạm vi (file/thư mục/feature). Trống → soát `git diff` hiện tại.
**Output:** danh sách lỗ hổng theo mức Critical/High/Medium/Low + cách fix.
**Agent gọi:** `security-reviewer` (read-only).

Thực hiện:
1. Xác định phạm vi: nếu trống, lấy `git diff` (staged + unstaged).
2. Gọi `Agent` với `subagent_type: security-reviewer`, truyền phạm vi/diff.
3. Trình bày báo cáo; kết luận ✅ merge được / ❌ phải sửa (liệt kê mục chặn).
