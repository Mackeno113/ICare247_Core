Bắt đầu session làm việc cho ICare247. Thực hiện đúng thứ tự:

1. Kiểm tra git sync:
   - Chạy `git fetch origin` để cập nhật remote refs
   - Chạy `git status` để kiểm tra ahead/behind
   - Nếu behind → hỏi user: "Remote có commits mới, git pull không?"
   - Nếu có uncommitted changes → cảnh báo user
2. Đọc `.claude/memory/last_session.md` để biết session trước làm gì
3. Đọc `.claude/memory/project_current_phase.md` để biết phase hiện tại
4. Đọc `TASKS.md` để xem task nào đang In Progress / Todo
5. Tóm tắt ngắn gọn cho user:
   - Session trước làm gì (máy nào, task gì)
   - Task đang dở (nếu có)
   - 3 task tiếp theo nên làm (theo thứ tự ưu tiên)
6. Hỏi user: "Hôm nay làm task nào?"
