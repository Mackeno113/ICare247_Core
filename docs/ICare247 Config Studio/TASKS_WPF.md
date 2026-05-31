# ConfigStudio WPF — Task Tracking

> Project: `ConfigStudio.WPF.UI` | Cập nhật: 2026-03-29
> Session trước (Wave C + ControlProps/I18n/Impact/SysLookup): tất cả done — xem Done log bên dưới

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

*(Không còn task in-progress — xem Backlog)*

---

## UI Optimization Roadmap (2026-05-31)

> Muc tieu: toi uu giao dien `src/frontend/ConfigStudio.WPF.UI/ConfigStudio.WPF.UI.slnx`
> theo huong design system hoa WPF, giam style lap lai, giu dung stack DevExpress WPF + Prism 9.

### Phase 0 — Baseline build
- [x] Chay build rieng module Grammar de xac nhan loi BAML/generated XAML.
- [x] Chay `dotnet clean` + `dotnet build` toan solution WPF.
- [x] Ket qua: `ConfigStudio.WPF.UI.slnx` build pass 0 error, 0 warning.
- [x] Ghi chu: loi cu `GrammarLibraryView.baml cannot be found` khong tai hien sau clean/build lai.

### Phase 1 — Shared UI resources
- [x] Tao resource dictionary chung cho design tokens: color, typography, spacing, shadow.
- [x] Tao resource dictionary chung cho controls: section card, field label, help text, icon button, toolbar separator, badge.
- [x] Merge resource dictionaries vao `App.xaml`/theme shell de cac module dung chung.
- [x] Migrate `SettingsView` lam man hinh mau: cards, inputs, password boxes, primary/outline buttons dung shared resources.
- [x] Build verify: `ConfigStudio.WPF.UI.slnx` pass 0 error, 0 warning.

### Phase 2 — Main workflow screens
- [x] Migrate `FormEditorView.xaml` sang shared resources, giu nguyen binding/ViewModel.
- [x] Migrate `FieldConfigView.xaml` sang shared resources, tach bot style cuc bo.
- [x] Migrate `ValidationRuleEditorView.xaml`, `EventEditorView.xaml`, `I18nManagerView.xaml` theo cung pattern.
- [x] Build verify: `ConfigStudio.WPF.UI.slnx` pass 0 error, 0 warning sau clean/build.
- [x] Fix encoding: restore cac XAML workflow bi mojibake ve UTF-8 dung va reapply style bang patch nho.
- [ ] Visual smoke test UI o kich thuoc toi thieu `1024x640`.

### Phase 3 — UX consistency
- [x] Chuan hoa shell theo admin tool: giam trang tri gradient/orb, tang do ro cua content area.
- [x] Chuan hoa command bar, dirty indicator, empty/loading/error states.
- [x] Chuan hoa DevExpress Grid density, column sizing, context menu va toolbar action.
- [x] Chuan hoa font: Segoe UI/system default; monospace chi dung cho code/id/json.
- [x] Build verify: `ConfigStudio.WPF.UI.slnx` pass 0 error, 0 warning sau Phase 3.

### Phase 4 — Verification and polish
- [x] Build WPF solution 0 error, 0 warning sau moi batch migrate.
- [x] Smoke test navigation: Dashboard -> FormManager -> FormEditor -> FieldConfig -> Rule/Event/I18n.
- [x] Kiem tra theme switch, breadcrumb/back-forward, status bar.
- [x] Cap nhat note/AI handoff neu co thay doi anh huong Claude Code.
- [x] Startup smoke: launch WPF app 6s, khong crash, stop process sach.
- [x] Polish: giu `formId/fieldCode/tableCode/sectionName` khi FieldConfig -> EventEditor va khi Rule/Event quay ve FieldConfig.
- [x] Encoding scan: touched XAML/CS khong co mojibake pattern.

---

### ✅ Bug Fix — ControlProps TextBox blank (2026-03-27)

**File:** `FieldConfigViewModel.cs`

**Vấn đề:** Tab "Control Props" không hiển thị input fields khi mở field có EditorType = TextBox (default).

**Root cause (2 bugs):**
1. `_selectedEditorType` default = `"TextBox"` → `SetProperty` không detect change → `LoadControlPropSchema()` không được gọi → `ControlProps` rỗng
2. Ngay cả khi gọi được, giá trị từ DB không được restore (dùng `oldValues` từ `ControlProps` rỗng → dùng default)

**Fix:**
- Reset `_selectedEditorType = ""` trước khi gán `field.EditorType` → force SetProperty detect change
- Set `_controlPropsJson` (backing field) TRƯỚC khi `SelectedEditorType` được gán
- `LoadControlPropSchema()`: khi `ControlProps.Count == 0` → parse `_controlPropsJson` để restore saved values
- Thêm `ParseControlPropsJson()` + `ConvertJsonPropValue()` helpers

---

## ⬜ Backlog

### Priority 1 — Bug / Thiếu logic trong màn hình cấu hình field

~~WPF-01, WPF-02 — moved to Done (2026-03-26)~~

---

### Priority 2 — FormEditor TODOs (phase 2)

#### ~~WPF-03~~: ✅ FormEditorViewModel.ExecuteSaveAsync — DONE
- `ExecuteSaveAsync()` gọi `UpdateFormMetadataAsync` với optimistic concurrency (Version)
- `ExecuteSaveSectionAsync()` gọi `UpsertSectionAsync` + `SaveResourceAsync` vi/en
- Đã implement từ trước — verified 2026-03-29

~~WPF-04, WPF-05, WPF-06 — moved to Done~~

---

### Priority 3 — FormManager TODOs (phase 2)

#### ~~WPF-07~~: ✅ FormManager Clone Form — DONE
- `FormDataService.CloneFormAsync`: deep clone Ui_Form + Ui_Section + Ui_Field trong transaction, schema-aware
- `FormManagerViewModel.ExecuteDuplicateForm`: gọi service, reload list sau khi clone
- `CloneFormDialog` + `CloneFormDialogViewModel`: validate unique code
- Verified 2026-03-29

#### ~~WPF-08~~: ✅ Form Preview Dialog — DONE
- `FormPreviewDialog` + `FormPreviewDialogViewModel` (186 dòng) + `FormPreviewModels.cs` — đầy đủ
- `FormManagerViewModel.ExecutePreviewForm` gọi dialog với params ✅
- `FormDetailViewModel.ExecutePreview` — fix wire dialog (commit session 2026-03-29) ✅

---

### Priority 4 — Các màn hình khác

#### ~~WPF-09~~: ✅ FieldConfigViewModel — Browse Column Popup — DONE
- `ColumnPickerDialog` + `ColumnPickerDialogViewModel` registered, `ExecuteBrowseColumn` gọi dialog ✅
- Verified 2026-03-29
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
| FieldConfig RequiredErrorKey | bd6f765 | Is_Required inline error key + RequiredErrorKey dedicated column + auto-suggest + fix key syntax dùng TableCode (không phải FormCode) |
| FieldConfig Redesign | 4d0081d | Left Panel Navigator + header badges (EditorType/Required/DataType) + breadcrumb + i18n key gen + LostFocus preview |
| WPF-08 | 495a90e | Form Preview Dialog — section/field card view với EditorType badges |
| WPF-07 | 52ba4ce | Clone Form deep — CloneFormAsync (Ui_Form + Ui_Section + Ui_Field) trong transaction |
| WPF-12 | 037bc34 | I18n Export/Import CSV/JSON — RFC 4180 + System.Text.Json |
| WPF-09 | 9059747 | ColumnPickerDialog — Browse Column popup với search/filter |
| WPF-10 | 044219e | ValidationRuleEditor Compare field dropdown — IFormDetailDataService |
| WPF-10 follow-up | 2026-05-31 | Chuan hoa Compare field dropdown sang `IRuleDataService.GetFieldCodesInFormAsync(formId)` theo AI_TASKS |
| WPF-11 | 044219e | FormSummaryDto EventCount — subquery COUNT Evt_Definition |
| T4-T8 (Wave CB) | (session) | ComboBoxPropsPanel + LookupBoxPropsPanel hoàn chỉnh + tích hợp FieldConfigView |
| Bug: ControlProps TextBox blank | 2026-03-27 | Fix `LoadControlPropSchema` không được gọi khi field là TextBox (default) + restore values từ JSON |
| Wave C | 707c882 | FieldConfig Behavior 2×2 + ValidationRule Length/Compare + Event SET_ENABLED/CLEAR_VALUE/SHOW_MESSAGE |
| Wave D | 8add8ba | Spec docs: DB schema + engine spec + action/rule param schema |
| ADR-013 | 932e879 | ColSpan 3-col → 4-col: Migration + Blazor CSS fix (ColSpan từng bị ignore) + WPF RadioButton |
| WPF-03 | 932e879 | FormEditorViewModel.ExecuteSaveAsync: IFormDataService.UpdateFormMetadataAsync + FormDataService implement + auto-save delegate |
| WPF-04 | session 2026-03-26 | ExecuteDeleteNode: confirm MessageBox trước khi xóa section (dùng DisplayName + fieldCount) |
| WPF-05 | session 2026-03-26 | ExecuteBackToList: check IsDirty → YesNoCancel dialog → Save/Discard/Cancel |
| WPF-06 | session 2026-03-26 | LoadPermissionsAsync: GetRolesAsync từ Sys_Role (global + per-tenant) + fallback hardcoded |
| WPF-01 | session 2026-03-26 | ExecuteDeleteEvent: confirm dialog + gọi DeleteEventAsync DB |
| WPF-02 | session 2026-03-26 | ExecuteDeleteRuleAsync: đã gọi DB + confirm — verified done |

---

## Decisions Log

| Date | Decision | Lý do |
|---|---|---|
| 2026-03-29 | i18n key syntax đổi sang `{TableCode}.field.{FieldCode}.{qualifier}` (dùng TableCode, không phải FormCode) | TableCode là stable identifier, FormCode có thể thay đổi |
| 2026-03-29 | `Required_Error_Key` dùng dedicated column trong `Ui_Field` (không lưu trong Control_Props_Json) | Nhất quán với LabelKey/PlaceholderKey/TooltipKey pattern |
| 2026-03-29 | `Is_Required = true` → auto-suggest `RequiredErrorKey` nếu chưa có giá trị; null out khi `Is_Required = false` | Giữ DB sạch, tránh orphan keys |
| 2026-03-29 | i18n key syntax: `{FormCode}.field.{FieldCode}.{qualifier}` (qualifier = label/placeholder/tooltip) | User xác nhận — cập nhật vào 09_FIELD_CONFIG_GUIDE.md |
| 2026-03-29 | Preview i18n khi `LostFocus` (không phải mỗi keystroke) | Tránh spam DB/cache mỗi lần gõ ký tự |
| 2026-03-29 | Warn nếu key đã tồn tại, không silent skip | User cần biết key đã có bản dịch để tránh ghi đè nhầm |
| 2026-03-26 | `Is_Required` / `Is_Enabled` = static columns trong `Ui_Field` (không phải Val_Rule) | ADR-010: required/enabled là state tĩnh, không cần điều kiện động |
| 2026-03-26 | `Required` rule type bị xóa khỏi ValidationRuleEditor | Thay bởi `Ui_Field.Is_Required` — tránh conflict giữa 2 cơ chế |
| 2026-03-26 | Tab Events trong FieldConfigView: chỉ có OpenEvent, không có DeleteEvent | Design intent ban đầu — nhưng cần bổ sung (WPF-01) |
