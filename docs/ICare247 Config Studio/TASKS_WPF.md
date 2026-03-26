# ConfigStudio WPF — Task Tracking

> Project: `ConfigStudio.WPF.UI` | Cập nhật: 2026-03-26
> Session trước (Wave C): FieldConfigRecord IsRequired/IsEnabled + ValidationRuleEditor Length/Compare + EventEditor SET_ENABLED/CLEAR_VALUE/SHOW_MESSAGE

---

## Trạng thái màn hình Field Config (phân tích session 2026-03-26)

### ✅ Đã hoàn thành (Wave C — commit 707c882)

| File | Thay đổi |
|---|---|
| `FieldConfigRecord.cs` | `IsRequired`, `IsEnabled` thêm vào model |
| `FieldDataService.cs` | SELECT/INSERT/UPDATE có `Is_Required`, `Is_Enabled` |
| `FieldConfigViewModel.cs` | `IsEnabled`; xóa `ToggleRequiredRule`; Behavior 2×2; LinkedRules + LinkedEvents load từ DB |
| `FieldConfigView.xaml` | 4 tabs đầy đủ (Cơ bản / Control Props / Rules / Events) |
| `ValidationRuleEditorViewModel.cs` | Xóa `Required`; thêm `Length` + `Compare`; `EditCompareField/Op/Preview` |
| `ValidationRuleEditorView.xaml` | Compare section + preview Border |
| `EventEditorViewModel.cs` | `ActionTypeOptions`: `SET_ENABLED`, `CLEAR_VALUE`, `SHOW_MESSAGE` |

---

## 🔴 In Progress

_(trống — chọn task từ danh sách bên dưới)_

---

## ⬜ Backlog

### Priority 1 — Bug / Thiếu logic trong màn hình cấu hình field

#### WPF-01: Thêm DeleteEventCommand trong FieldConfigView Tab Events
- **File:** `FieldConfigViewModel.cs` + `FieldConfigView.xaml` (Tab 4)
- **Vấn đề:** Tab 4 Events chỉ có nút `⚙ OpenEvent`, không có nút Delete. Tab 3 Rules đã có `DeleteRuleCommand` + nút 🗑.
- **Việc cần làm:**
  1. Thêm `DelegateCommand<EventSummaryDto> DeleteEventCommand` vào ViewModel
  2. Implement `ExecuteDeleteEvent(EventSummaryDto?)`: remove từ `LinkedEvents`, reindex, `IsDirty = true`
  3. Thêm nút 🗑 `InlineDeleteBtnStyle` vào CellTemplate column Actions trong Events grid (xaml)
- **Ước lượng:** ~30 phút

#### WPF-02: ExecuteDeleteRule không gọi DB
- **File:** `FieldConfigViewModel.cs` line 1503
- **Vấn đề:** `ExecuteDeleteRule` chỉ xóa khỏi `LinkedRules` local, không gọi `_ruleService.DeleteRuleAsync()` → rule vẫn tồn tại trong DB sau khi save field.
- **Việc cần làm:**
  1. Thêm `IRuleDataService.DeleteRuleAsync(ruleId, ct)` nếu chưa có
  2. Trong `ExecuteDeleteRule`: gọi `_ruleService?.DeleteRuleAsync(rule.RuleId, _cts.Token)` (fire-and-forget hoặc await)
  3. Thêm confirmation dialog trước khi xóa ("Xóa rule này?")
- **Ước lượng:** ~45 phút

---

### Priority 2 — FormEditor TODOs (phase 2)

#### WPF-03: FormEditorViewModel.ExecuteSaveAsync — gọi data service thật
- **File:** `FormEditorViewModel.cs` line 1593, 1741
- **Vấn đề:** `ExecuteSaveAsync()` và `ExecuteSaveSectionAsync()` đều là TODO — chưa gọi `IFormDataService`.
- **Việc cần làm:**
  1. Implement `IFormDataService.SaveFormAsync(form, sections, ct)` nếu chưa có
  2. Trong `ExecuteSaveAsync`: build `FormConfigRecord` từ ViewModel state, gọi service
  3. Handle version conflict (optimistic concurrency)
- **Ước lượng:** ~2 giờ

#### WPF-04: FormEditorViewModel — Confirm dialog xóa Section
- **File:** `FormEditorViewModel.cs` line 1332
- **Vấn đề:** `ExecuteDeleteSection` không có confirm dialog — xóa ngay lập tức cả section + tất cả fields.
- **Việc cần làm:**
  1. Inject `IDialogService` vào FormEditorViewModel
  2. Hiện `ConfirmDialog("Xóa section '{name}' và tất cả {count} fields? Không thể hoàn tác.")`
  3. Chỉ xóa nếu user confirm
- **Ước lượng:** ~30 phút

#### WPF-05: FormEditorViewModel — Confirm IsDirty khi navigate back
- **File:** `FormEditorViewModel.cs` line 1612
- **Vấn đề:** `ExecuteBack()` navigate ngay, không kiểm tra `IsDirty` → mất unsaved changes.
- **Việc cần làm:**
  1. Nếu `IsDirty`, hiện dialog "Có thay đổi chưa lưu. Lưu trước khi thoát?"
  2. Options: Lưu và thoát / Thoát không lưu / Hủy
- **Ước lượng:** ~30 phút

#### WPF-06: FormEditorViewModel — Load Permissions từ Sys_Role
- **File:** `FormEditorViewModel.cs` line 830, 879
- **Vấn đề:** Tab Permissions dùng hardcoded roles, chưa load từ DB.
- **Việc cần làm:**
  1. Implement `IFormDataService.GetRolesAsync(tenantId, ct)` query `Sys_Role`
  2. Populate `AvailableRoles` collection trong ViewModel
  3. UI: Checkbox list roles với quyền View/Edit/Delete per role
- **Ước lượng:** ~1.5 giờ

---

### Priority 3 — FormManager TODOs (phase 2)

#### WPF-07: FormManager Clone Form — implement thật
- **File:** `FormManagerViewModel.cs` line 436
- **Vấn đề:** Clone chỉ tạo object local với Form_Code mới, không persist vào DB.
- **Việc cần làm:**
  1. Implement `IFormDataService.CloneFormAsync(sourceFormId, newCode, tenantId, ct)`
  2. SQL: Copy Ui_Form + Ui_Section + Ui_Field (deep clone, new PKs)
  3. Sau clone: navigate sang FormEditor với form mới
- **Ước lượng:** ~2 giờ

#### WPF-08: Form Preview Dialog
- **File:** `FormManagerViewModel.cs` line 365, `FormDetailViewModel.cs` line 370
- **Vấn đề:** "Preview form" là TODO — chưa có dialog render metadata thành form visual.
- **Việc cần làm:**
  1. Tạo `FormPreviewDialog` — render Ui_Field list theo EditorType → DevExpress controls
  2. Read-only preview (không save data)
  3. Hiển thị các field theo ColSpan grid layout
- **Ước lượng:** ~4 giờ (task lớn)

---

### Priority 4 — Các màn hình khác

#### WPF-09: FieldConfigViewModel — Browse Column Popup
- **File:** `FieldConfigViewModel.cs` line 1466 (TODO phase2)
- **Vấn đề:** Nút 🔍 Browse column hiện chỉ là placeholder, không mở popup.
- **Việc cần làm:**
  1. Implement `ExecuteBrowseColumn()`: mở dialog `ColumnPickerDialog`
  2. Dialog list các column từ `AvailableColumns` với search/filter
  3. Chọn column → set `SelectedColumn`
- **Ước lượng:** ~1 giờ

#### WPF-10: ValidationRuleEditor — Compare rule field list dropdown
- **File:** `ValidationRuleEditorViewModel.cs`, `ValidationRuleEditorView.xaml`
- **Vấn đề:** `EditCompareField` là TextBox thủ công — user phải nhớ/gõ field code. Nên là dropdown danh sách fields trong form.
- **Việc cần làm:**
  1. Thêm `AvailableFieldCodes : List<string>` — load từ `IRuleDataService.GetFieldCodesInFormAsync(formId)`
  2. Đổi TextEdit → ComboBoxEdit với `ItemsSource=AvailableFieldCodes` trong Compare section
- **Ước lượng:** ~45 phút

#### WPF-11: FormSummaryDto EventCount
- **File:** `FormManagerViewModel.cs` line 383
- **Vấn đề:** `eventCount` truyền hardcoded = 0, FormSummaryDto không có field này.
- **Việc cần làm:**
  1. Thêm `EventCount : int` vào `FormSummaryDto`
  2. SQL trong `GetFormsAsync`: thêm subquery `COUNT(*)` từ `Ui_Field_Event`
  3. Hiển thị trong FormManager grid
- **Ước lượng:** ~30 phút

#### WPF-12: I18n Manager — Export/Import CSV/JSON
- **File:** `I18nManagerViewModel.cs` line 373-374
- **Vấn đề:** `ExecuteExport()` và `ExecuteImport()` là TODO empty methods.
- **Việc cần làm:**
  1. Export: Serialize `Entries` → CSV (columns: Key, VI, EN, ...) → SaveFileDialog
  2. Import: OpenFileDialog → parse CSV/JSON → merge vào `Entries` → confirm overwrite
- **Ước lượng:** ~1.5 giờ

---

## ✅ Done

| Task | Commit | Mô tả |
|---|---|---|
| Wave C | 707c882 | FieldConfig Behavior 2×2 + ValidationRule Length/Compare + Event SET_ENABLED/CLEAR_VALUE/SHOW_MESSAGE |
| Wave D | 8add8ba | Spec docs: DB schema + engine spec + action/rule param schema |

---

## Decisions Log

| Date | Decision | Lý do |
|---|---|---|
| 2026-03-26 | `Is_Required` / `Is_Enabled` = static columns trong `Ui_Field` (không phải Val_Rule) | ADR-010: required/enabled là state tĩnh, không cần điều kiện động |
| 2026-03-26 | `Required` rule type bị xóa khỏi ValidationRuleEditor | Thay bởi `Ui_Field.Is_Required` — tránh conflict giữa 2 cơ chế |
| 2026-03-26 | Tab Events trong FieldConfigView: chỉ có OpenEvent, không có DeleteEvent | Design intent ban đầu — nhưng cần bổ sung (WPF-01) |
