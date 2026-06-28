# /generate-crud — Sinh CRUD Dapper (CQRS)

**Mục đích:** Sinh repository Dapper + Query/Command + Handler cho 1 bảng.
**Input:** `$ARGUMENTS` = tên bảng (+ cột/khóa nếu có). Trống → hỏi user bảng nào.
**Output:** `I{Entity}Repository`/`{Entity}Repository` + GetById/GetList/Insert/Update/Deactivate + Handler.
**Agent gọi:** `backend-dapper-expert`.

Thực hiện:
1. Nếu thiếu bảng → hỏi user. Đọc schema thật của bảng (live DB hoặc `docs/spec/02`).
2. Gọi `Agent` với `subagent_type: backend-dapper-expert`, yêu cầu sinh CRUD theo bảng + ràng buộc (Dapper, Tenant_Id, Is_Active, ct, naming CQRS).
3. Báo file đã tạo; gợi ý chạy `/generate-tests` + `/review-changes`. KHÔNG tự commit.
