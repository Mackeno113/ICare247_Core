Lưu thông tin quan trọng vào memory để nhớ qua sessions và đồng bộ qua nhiều máy.

Thực hiện:

1. Hỏi user muốn lưu gì (nếu chưa rõ từ context)
2. Xác định loại memory:
   - **Architecture decision** → ghi vào `.claude/memory/architecture_decisions.md`
   - **Coding style feedback** → ghi vào `.claude/memory/coding_style_feedback.md`
   - **Phase/progress update** → ghi vào `.claude/memory/project_current_phase.md`
   - **User preference** → ghi vào `.claude/memory/user_profile.md`
3. Append nội dung mới (không xóa cũ), ghi kèm ngày
4. Confirm với user: "Đã lưu [nội dung] vào [file]"
5. Nhắc user: "Commit + push để đồng bộ sang máy khác"

**Lưu ý:** Memory PHẢI nằm trong repo (`.claude/memory/`) để git sync.
KHÔNG ghi vào `~/.claude/projects/` — file đó là local, không sync.
