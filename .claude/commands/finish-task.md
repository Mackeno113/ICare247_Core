Hoàn thành task hiện tại. Thực hiện đúng thứ tự:

1. Chạy `dotnet build ICare247.slnx` để verify build thành công
   - Nếu fail → sửa lỗi trước, KHÔNG tiếp tục
2. Cập nhật `TASKS.md`:
   - Move task từ 🔴 In Progress → ✅ Done
   - Ghi note nếu có quyết định quan trọng vào Decisions Log
3. Cập nhật `.claude/memory/last_session.md`:
   - Ghi task vừa hoàn thành vào "Đã làm"
   - Ghi ngày + task tiếp theo gợi ý
4. Cập nhật `.claude/memory/project_current_phase.md` nếu phase thay đổi
5. Stage tất cả file liên quan (code + TASKS.md + memory)
6. Hỏi user: "Commit với message gì?" (đề xuất message phù hợp)
7. Sau khi user confirm → commit
8. Hỏi user: "Push luôn không?" (quan trọng nếu user chuyển máy)
