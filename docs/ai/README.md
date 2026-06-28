# docs/ai — Mục lục hạ tầng AI cho ICare247

> **Cửa vào duy nhất.** Quên ở đâu? Mở file này. Hoặc bảo Claude: *"liệt kê tooling AI"*.
> Nhanh nhất đầu buổi: chạy **`/start-session`** — Claude tự tóm tắt session trước + việc cần làm.

---

## 📂 Tài liệu governance (đọc khi nhập template ngoài)
| File | Dùng khi |
|---|---|
| [AI_TEMPLATE_INTEGRATION_PLAN.md](AI_TEMPLATE_INTEGRATION_PLAN.md) | Kế hoạch tổng: phân mức template, bộ agent, command, 5 phase, prompt mẫu, rủi ro |
| [TEMPLATE_INTAKE.md](TEMPLATE_INTAKE.md) | Checklist **bắt buộc** trước khi nhập 1 skill/agent/command từ aitmpl.com |
| `BRAIN.md §11` | Luật gốc: không copy nguyên bản, không sinh SSOT thứ 2, thứ tự ưu tiên xung đột |

## 🤖 Agent (14) — gõ qua `Agent` tool hoặc Claude tự gọi
**Review (read-only):** `security-reviewer` · `performance-reviewer` · `sql-server-optimizer`
**Sinh code (.NET/Dapper):** `backend-dapper-expert` · `test-generator`
**Engine ICare247 (tự viết):** `metadata-engine` · `form-engine` · `validation-engine` · `event-engine` · `rbac` · `cache-redis` · `observability`
**Thiết kế/phân tích (có sẵn):** `design-agent` · `product-analyst`

> Nguồn + mức customize từng agent: xem header trong file `.claude/agents/*.md` và bảng log [TEMPLATE_INTAKE.md §F](TEMPLATE_INTAKE.md).

## ⌨️ Slash command (gõ `/` để Claude Code tự gợi ý)
**Review:** `/review-security` · `/review-performance` · `/optimize-sql` · `/review-architecture` · `/review-db-schema` · `/review-changes`
**Sinh code:** `/generate-crud` · `/generate-api` · `/generate-tests` · `/generate-devexpress-form` · `/generate-validation-rule` · `/generate-event-action` · `/generate-docs`
**Engine:** `/analyze-metadata` · `/build-dependency-graph`
**Quy trình:** `/start-session` · `/pick-task` · `/finish-task` · `/save-memory` · `/design`

## 🧭 Khi nào dùng gì (tra nhanh)
| Tôi muốn… | Dùng |
|---|---|
| Bắt đầu buổi làm, không nhớ đang dở gì | `/start-session` |
| Soát 1 thay đổi trước khi commit | `/review-security` + `/review-performance` (+ `/review-changes`) |
| Thêm bảng → tầng data/API | `/generate-crud` → `/generate-api` → `/generate-tests` |
| Query chậm | `/optimize-sql` |
| Dựng form nghiệp vụ | `/generate-devexpress-form` |
| Thêm rule/validate | `/generate-validation-rule` |
| Nhập 1 template mới từ aitmpl | Đọc [TEMPLATE_INTAKE.md](TEMPLATE_INTAKE.md) TRƯỚC |

---
*Cập nhật khi thêm/bớt agent hoặc command. Tạo: session 69 (2026-06-28).*
