# Last Session Summary

> Cập nhật: 2026-03-19

## Đã làm (tổng hợp từ 17/03 → 19/03)

### ConfigStudio.WPF.UI — Form Editor Refactor (19/03)

#### Gộp FormEditDialog vào FormEditorView
- **Xóa** `FormEditDialogView.xaml` (645 dòng) + `FormEditDialogViewModel.cs` (665 dòng)
- Merge logic vào **FormEditorViewModel** — tạo mới và sửa dùng chung View
- Phân biệt mode bằng `IsNewForm` flag, không mở Dialog riêng
- `FormManagerViewModel` bỏ `IDialogService`, navigate thẳng tới FormEditor

#### Redesign FormEditorView UI
- Card-based layout với shadow, Tailwind CSS color palette
- Intro panel mô tả ý nghĩa màn hình + từng field (hướng dẫn enduser)
- Header bar: accent bar + platform badge + dirty indicator
- Form field pattern: Label + Input + Help text 3 phần

#### Business Table luôn lấy từ DB
- Xóa `LoadTableOptions()` mock data hardcode
- Cả create/edit mode gọi `IFormDataService.GetTablesByTenantAsync()` từ DB

#### Design Guidelines
- Thêm 10 nguyên tắc thiết kế UI vào `.claude-rules/wpf-configstudio.md`
- Áp dụng cho toàn bộ project ConfigStudio WPF

### Branches — Đã dọn
- 3 branches (`strange-cray`, `tender-goldwasser`, `zen-bhabha`) đã merge hết vào master
- Worktrees bị process lock → cần xóa manual khi chuyển máy:
  ```bash
  cd D:\ICare247_Core
  git worktree remove --force .claude/worktrees/tender-goldwasser
  git worktree remove --force .claude/worktrees/zen-bhabha
  git branch -d claude/strange-cray claude/tender-goldwasser claude/zen-bhabha
  ```

## Đang làm
- Không có task dở dang

## Task tiếp theo (gợi ý)
- **ConfigStudio:** FieldConfigView/ViewModel đầy đủ (hiện chỉ là stub), ValidationRuleEditor, EventEditor
- **ConfigStudio:** LoadMockData() trong FormEditorViewModel vẫn dùng data giả cho sections/events/permissions → cần chuyển sang DB
- **Backend:** Application interfaces (IFormRepository, IFieldRepository, IDbConnectionFactory, ICacheService)
- **Backend:** CacheKeys.cs
