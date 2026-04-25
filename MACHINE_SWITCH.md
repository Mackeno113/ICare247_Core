# MACHINE_SWITCH.md — Protocol khi đổi máy tính

<!--
  FILE: MACHINE_SWITCH.md
  MỤC ĐÍCH: Quy trình bắt buộc khi chuyển làm việc giữa các máy tính.
  Áp dụng cho: cả Claude Code và Codex, cả 2 máy.
-->

## Khi nào cần đọc file này

- Bắt đầu làm việc trên máy khác (máy 1 → máy 2 hoặc ngược lại)
- Sau khi nghỉ dài và quay lại project
- Khi AI agent báo "branch behind origin"

---

## Checklist TRƯỚC khi bắt đầu session (máy mới)

```
[ ] 1. git pull origin master
[ ] 2. Đọc .claude/memory/last_session.md  (Claude Code làm gì lần cuối)
[ ] 3. Đọc .codex/memory/last_session.md   (Codex làm gì lần cuối)
[ ] 4. Đọc AI_HANDOFF.md                   (task nào đang dở, ai đang giữ)
[ ] 5. Kiểm tra có conflict git không → giải quyết trước khi code
```

---

## Checklist SAU khi kết thúc session (trước khi đóng máy)

```
[ ] 1. Commit tất cả code đã hoàn chỉnh
[ ] 2. Cập nhật memory file phù hợp:
       - Claude Code → .claude/memory/last_session.md
       - Codex       → .codex/memory/last_session.md
[ ] 3. Cập nhật AI_HANDOFF.md nếu có task đang dở hoặc cần bàn giao
[ ] 4. git push origin master
       ↳ BẮT BUỘC push memory files — đây là sync point duy nhất
```

---

## Quy tắc branch

| Loại thay đổi | Branch |
|---|---|
| Memory files (`.claude/memory/`, `.codex/memory/`) | Luôn commit thẳng `master` |
| Config files (`BRAIN.md`, `CLAUDE.md`, `AGENTS.md`) | Luôn commit thẳng `master` |
| Feature code | Branch `feature/...` → merge khi done |
| Bug fix | Branch `fix/...` hoặc thẳng `master` nếu nhỏ |

---

## Máy hiện tại

| ID | Mô tả | OS |
|---|---|---|
| Máy 1 | Máy chính (dev) | Windows 10 |
| Máy 2 | Máy phụ | Windows 10 |

> Cập nhật bảng này nếu thêm máy mới.

---

## Lưu ý quan trọng

- **Không bao giờ** bắt đầu code mà không `git pull` trước
- **Không bao giờ** đóng máy mà không `git push` memory files
- Nếu có conflict trong memory files → **giữ version mới hơn** (theo timestamp)
- Feature branch không cần push ngay, nhưng **memory phải sync**
