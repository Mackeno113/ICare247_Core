# AI Handoff Log — ICare247 Core

Ghi lại mỗi khi bàn giao task giữa Claude Code và Codex.
**Newest first.** Cập nhật ngay khi bắt đầu hoặc hoàn thành một task.

---

## Template

```
### [YYYY-MM-DD] <TASK-ID> — <from> → <to>
- Status: in_progress | blocked | done
- Files: <danh sách file chính đã/sẽ sửa>
- Cần biết: <thông tin quan trọng cho agent nhận>
- Bước tiếp theo: <1 hành động cụ thể>
```

---

## Entries

### [2026-04-25] GOV-001 — claude → both

- Status: done
- Files: `BRAIN.md`, `CLAUDE.md`, `AGENTS.md`, `.codex/memory/*`, `MACHINE_SWITCH.md`, `AI_TASKS.yaml`, `AI_DECISIONS.md`, `AI_HANDOFF.md`
- Cần biết: Toàn bộ AI config đã được rebuild. BRAIN.md là single source of truth. Codex giờ có `.codex/memory/` riêng. Đọc MACHINE_SWITCH.md trước khi đổi máy.
- Bước tiếp theo: Codex bắt đầu WPF-10 (Compare rule dropdown) hoặc WPF-13 (pass tableCode).

---

### [2026-04-17] Wave 10 — claude → codex

- Status: done
- Files: `FieldLookupConfig.cs`, `MetadataEngine.cs`, `I18nManagerViewModel.cs`, `FieldConfigViewModel.cs`, `MainWindow.xaml.cs`, `LookupBoxPropsPanel.xaml`
- Cần biết: i18n captionKey hoàn chỉnh. MetadataEngine resolve popup column captions từ Sys_Resource. WPF: SpinEdit race condition fix, SysLookupManager fix, MainWindow fullscreen fix, popup columns UX (▲▼ + ✕).
- Bước tiếp theo: Test LookupBox end-to-end với GioiTinh + PhongBanID.

---

### [2026-03-03] GOV-001 — codex → claude

- Status: done
- Files: `AI_PROJECT_BRIEF.md` (đã xóa), `AI_TASKS.yaml`, `AI_HANDOFF.md`, `AI_DECISIONS.md`
- Cần biết: Governance files ban đầu được tạo.
- Bước tiếp theo: Confirm owners cho CORE-001, APP-001, INF-001, API-001.
