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

### [2026-05-31] WPF-10/WPF-13 - codex -> claude

- Status: done
- Files: `AI_TASKS.yaml`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Core/Interfaces/IRuleDataService.cs`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI/Infrastructure/RuleDataService.cs`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Modules.Rules/ViewModels/ValidationRuleEditorViewModel.cs`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Modules.Rules/Views/ValidationRuleEditorView.xaml`
- Can biet: WPF-13 marked done. WPF-10 now matches AI_TASKS acceptance: Compare field dropdown loads field codes from `IRuleDataService.GetFieldCodesInFormAsync(formId)` and the UI no longer allows free-text field entry.
- Buoc tiep theo: Reconcile old AI_TASKS WPF-11/WPF-12 status against TASKS_WPF done log before choosing the next WPF item.

---

### [2026-05-31] WPF-UI-PHASE4 - codex -> claude

- Status: done
- Files: `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Modules.Forms/ViewModels/FieldConfigViewModel.cs`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Modules.Events/ViewModels/EventEditorViewModel.cs`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Modules.Rules/ViewModels/ValidationRuleEditorViewModel.cs`, `docs/ICare247 Config Studio/TASKS_WPF.md`
- Can biet: Phase 4 verification passed. Navigation registrations match all shell/sidebar/requested routes. Startup smoke launched the WPF app for 6s without crash. Polished Rule/Event navigation so `formId`, `fieldCode`, `tableCode`, and `sectionName` are preserved when returning to FieldConfig.
- Buoc tiep theo: Manual visual pass on a real desktop session if pixel-level layout review is required.

---

### [2026-05-31] WPF-UI-PHASE3 - codex -> claude

- Status: done
- Files: `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI/MainWindow.xaml`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI/Themes/Shell.xaml`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI/Themes/Shell.SlateProfessional.xaml`, `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI/Themes/Controls.xaml`, workflow XAML views, `docs/ICare247 Config Studio/TASKS_WPF.md`
- Can biet: Phase 3 UI consistency done for WPF shell/theme/workflow screens. Shell uses Segoe UI and solid admin-tool surfaces; shared command bar, dirty indicator, state/error banner, and DevExpress grid density styles are in `Controls.xaml`.
- Buoc tiep theo: Phase 4 visual smoke/navigation smoke when app can be opened interactively.

---

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
