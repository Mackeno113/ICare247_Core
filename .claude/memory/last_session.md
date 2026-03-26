# Last Session Summary

> Cập nhật: 2026-03-27 (session 10)

## Đã làm (session 27/03 — session 10)

### Bug Fix: ControlProps TextBox blank (FieldConfigViewModel.cs)

**Vấn đề:** Tab "Control Props" không hiển thị gì khi mở field có EditorType = "TextBox".

**Root cause (2 bugs):**
1. `_selectedEditorType` default = `"TextBox"` → `SetProperty` không detect change khi load field TextBox → `LoadControlPropSchema()` không được gọi → `ControlProps` rỗng
2. Kể cả khi load được, `oldValues` lấy từ `ControlProps` rỗng → không restore values từ DB

**Fix trong `LoadFromDatabaseAsync()`:**
- Set `_controlPropsJson = field.ControlPropsJson` (backing field) trước khi SelectedEditorType thay đổi
- Reset `_selectedEditorType = ""` → force SetProperty detect change → `LoadControlPropSchema()` luôn chạy
- Xóa dòng `ControlPropsJson = field.ControlPropsJson` dư (rebuild tự động sau)

**Fix trong `LoadControlPropSchema()`:**
- Khi `ControlProps.Count == 0` → parse `_controlPropsJson` thay vì dùng empty dict
- Thêm `ParseControlPropsJson()` + `ConvertJsonPropValue()` helpers (xử lý JsonElement → typed value)

Build WPF: **0 errors, 0 warnings** ✅
Build backend: **0 errors, 0 warnings** ✅

---

## Trạng thái hiện tại

- Migration 010–013: **ĐÃ chạy trên DB thật** ✅
- Bug ControlProps blank: **fixed + committed** ✅

## Việc tiếp theo (ưu tiên)

1. **WPF-09** — Browse Column Popup (ColumnPickerDialog)
2. **WPF-10** — ValidationRuleEditor Compare rule: TextBox → dropdown field list
3. **WPF-07** — FormManager Clone Form implement thật
4. **WPF-11** — FormSummaryDto EventCount
5. **Backend** — MetadataEngine implement (Phase 6 còn lại)
