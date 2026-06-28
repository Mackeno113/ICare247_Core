# /optimize-sql — Tối ưu truy vấn MS SQL Server

**Mục đích:** Phân tích query chậm, đề xuất bản viết lại (CTE/CROSS APPLY) + index.
**Input:** `$ARGUMENTS` = câu SQL, hoặc đường dẫn file/repo chứa query.
**Output:** chẩn đoán + SQL viết lại + `CREATE INDEX` đề xuất (advisory, không tự áp).
**Agent gọi:** `sql-server-optimizer` (read-only).

Thực hiện:
1. Nếu `$ARGUMENTS` trỏ file → đọc query liên quan; nếu là SQL thô → dùng trực tiếp.
2. Gọi `Agent` với `subagent_type: sql-server-optimizer`, truyền query + bối cảnh bảng/schema.
3. Trình bày kết quả; **không sửa file** trừ khi user yêu cầu rõ (khi đó chuyển `backend-dapper-expert`).
