# Last Session Summary

> Cập nhật: 2026-03-30 (session 16)

## Đã làm (session 30/03 — FormRunner MemoRenderer + CheckBoxRenderer)

### 1. Tách TextBoxRenderer — DxTextBox only (design correction)

- **Quyết định thiết kế:** DxTextBox và DxMemo là 2 control hoàn toàn khác nhau
- User tự chọn EditorType phù hợp trong WPF ConfigStudio — không có auto-switch
- `TextBoxRenderer.razor` chỉ render DxTextBox (loại bỏ `isMultiline` logic)
- Full spec: `BindValueMode`, `InputDelay`, `ClearButton` (Auto/Never only — không có Always), `AutoComplete`, `Password`, `MaxLength`
- Blur qua `@onfocusout` wrapper div (DxTextBox `LostFocus` không đáng tin)

### 2. MemoRenderer.razor (NEW)

- EditorType "TextArea" → `MemoRenderer` → `DxMemo`
- Props: `maxLength=4000`, `rows=4`, `bindValueMode="OnLostFocus"`, `inputDelay=300`
- `NormalizeFieldType`: "textarea" / "memo" → "textarea"
- `FieldRenderer.razor`: case "textarea" → `<MemoRenderer>`

### 3. CheckBoxRenderer.razor (NEW)

- EditorType "CheckBox" → `CheckBoxRenderer(IsSwitch=false)` → `DxCheckBox CheckType.Checkbox`
- EditorType "ToggleSwitch" → `CheckBoxRenderer(IsSwitch=true)` → `DxCheckBox CheckType.Switch`
- **Bug fix quan trọng:** `CheckType.CheckBox` không tồn tại trong DX v25.2.3 → đúng là `CheckType.Checkbox` (chữ 'b' thường)
- **Bug fix:** `CheckedChanged` không phải generic callback → dùng `@bind-Checked` với backing property pattern:
  ```csharp
  private bool BoundValue { get => _localValue; set => _ = HandleCheckedChangedAsync(value); }
  ```
- Props: `allowIndeterminate`, `labelPosition`, `labelWrapMode`
- AllowIndeterminate: 3 trạng thái `true/false/null`, click order: Indeterminate→Checked→Unchecked
- `NormalizeFieldType`: "toggleswitch" / "toggle" → "switch"

### 4. WPF FieldConfigViewModel schema updates

- **TextBox** (6 props): maxLength, isPassword, autoComplete, bindValueMode, inputDelay, clearButtonMode
- **TextArea** (4 props — EditorType mới): maxLength, rows, bindValueMode, inputDelay
- **CheckBox** (3 props): allowIndeterminate, labelPosition, labelWrapMode
- **ToggleSwitch** (1 prop): labelPosition
- Removed: `isMultiline`, `rows` (từ TextBox), `nullText` (trùng với i18n Placeholder Key)

### 5. Spec 11 — Blazor Control Renderer

- Tạo `docs/spec/11_BLAZOR_CONTROL_RENDERER_SPEC.md`
- EditorType→Renderer mapping table đầy đủ (10 controls)
- DxTextBox / DxMemo / DxCheckBox full spec với ControlPropsJson schema
- Common renderer pattern: parameters, code template, Props class inner type
- FieldRenderer routing table + NormalizeFieldType mapping

---

## Trạng thái hiện tại

- Build: **0 errors** ✅ (3 warnings: 2 DX license + 1 đã fix CS8602)
- Unit tests: **145 passed** ✅
- Renderers done: TextBox ✅ | Memo ✅ | CheckBox ✅ | ComboBox ✅ | LookupBox ✅ | Select ✅
- Renderers pending: **NumericBox** (DxSpinEdit) | **DatePicker** (DxDateEdit)

## Việc tiếp theo (ưu tiên)

1. **NumericBox renderer** — `NumericBoxRenderer.razor` (DxSpinEdit) + WPF NumericBox props schema
2. **DatePicker renderer** — `DatePickerRenderer.razor` (DxDateEdit) + WPF DatePicker props schema
3. **Test FormRunner end-to-end** — cần API + DB đang chạy, form có CheckBox/TextBox fields
4. **T11** — `LookupComboBoxRenderer.razor` (low priority)

## Quyết định quan trọng session này

- **DxTextBox ≠ DxMemo:** 2 EditorType riêng biệt, user chọn trong ConfigStudio
- **CheckType.Checkbox:** DX v25 enum value là `Checkbox` (không phải `CheckBox`)
- **@bind-Checked pattern:** DxCheckBox dùng backing property để fire async event
- **NullText:** KHÔNG lưu trong ControlPropsJson — dùng `State.Label + "..."` (fallback), đúng cách là i18n PlaceholderKey
