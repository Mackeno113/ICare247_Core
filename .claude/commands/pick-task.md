User muốn chọn task tiếp theo. Thực hiện:

1. Đọc `TASKS.md` — phần 🟡 Todo
2. Đọc `.claude/memory/project_current_phase.md` để biết ưu tiên
3. Liệt kê top 5 task nên làm tiếp, sắp xếp theo:
   - Phase ưu tiên (Phase 1 trước Phase 2)
   - Dependency (task không có blocker trước)
   - Impact (task mở khóa nhiều task khác)
4. Với mỗi task, ghi rõ:
   - Tên task
   - Ước lượng thời gian (S/M/L)
   - Dependencies (nếu có)
   - Files cần đọc trước khi code
5. Hỏi user chọn task nào
6. Khi user chọn → move task sang 🔴 In Progress trong TASKS.md
7. Đọc `.claude-rules/` + `docs/spec/` liên quan
8. Bắt đầu code
