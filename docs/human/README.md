# docs/human — Tài liệu cho NGƯỜI đọc (agent KHÔNG tự nạp)

> Thư mục này **nằm ngoài Session Protocol** — Claude/Codex không đọc tự động mỗi phiên.
> Nguồn context bắt buộc cho agent là `BRAIN.md` + `.claude/memory/` + `TASKS.md` (xem `CLAUDE.md`).
> Đặt ở đây các cẩm nang vận hành/onboarding để giảm token nạp mỗi session mà không mất tài liệu.

## Đang dùng

| File | Nội dung | Canonical cho |
|---|---|---|
| [AI_WORKFLOW_GUIDE.md](AI_WORKFLOW_GUIDE.md) | Hệ thống AI-assisted workflow vận hành **thế nào** — kiến trúc tài liệu, cấu trúc thư mục, workflow hàng ngày, đồng bộ multi-machine, cách mở rộng (rule/command/memory/spec). | "Quy trình làm việc" |
| [AI_AGENT_CONFIG_GUIDE.md](AI_AGENT_CONFIG_GUIDE.md) | **Mỗi file cấu hình là gì** — `CLAUDE.md`, `AGENTS.md`, `.cursorrules`, `.github/copilot-instructions.md`, `.editorconfig`, `.gitignore` + cách deploy bộ config. | "Tham chiếu file config" |

> Hai file bổ trợ nhau (một mô tả *quy trình*, một mô tả *từng file config*). Nguồn sự thật cho **hard
> constraint + ownership + tech stack** vẫn là [`BRAIN.md`](../../BRAIN.md) — hai guide này chỉ diễn giải cho người.

## Lưu trữ (`archive/`) — lịch sử, không còn hiệu lực vận hành

| File | Vì sao lưu trữ |
|---|---|
| [archive/AI_WORKFLOW_UPGRADE_PROPOSAL.md](archive/AI_WORKFLOW_UPGRADE_PROPOSAL.md) | Bản đề xuất nâng cấp workflow — **đã triển khai xong** (memory/hooks/skills đã có, CLAUDE.md đã tách gọn). Giữ làm hồ sơ thiết kế. |
| [archive/AI_ONBOARDING_GAPS.md](archive/AI_ONBOARDING_GAPS.md) | Audit onboarding/khoảng trống tại một thời điểm — snapshot, dễ lỗi thời. Giữ để tra cứu bối cảnh. |
