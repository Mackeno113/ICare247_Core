# Last Session Summary

> Cập nhật: 2026-03-29 (session 14)

## Đã làm (session 29/03 — FieldConfig RequiredErrorKey + key syntax fix)

### FieldConfig RequiredErrorKey (commit bd6f765)

**Implement `Is_Required` inline error key + fix key generation:**

1. **`FieldConfigRecord.cs`** — thêm `RequiredErrorKey` property
2. **`FieldDataService.cs`** — thêm `Required_Error_Key` vào SELECT/INSERT/UPDATE/BuildFieldParam
3. **`FieldConfigViewModel.cs`**:
   - `RequiredErrorKey` property (LostFocus preview)
   - `RequiredErrorKeyPreview` + `HasRequiredErrorKeyPreview` computed
   - `IsRequiredExpanded => _isRequired` (section visible khi Is_Required = true)
   - `IsRequired` setter: auto-suggest RequiredErrorKey nếu empty
   - `AutoSuggestRequiredErrorKeyAsync()`: `{tableCode}.val.{columnCode}.required`
   - `GenerateRequiredErrorKeyCommand`: warn nếu key đã tồn tại trong DB
   - **Fix key syntax**: `ExecuteGenerateKeyAsync` dùng `TableCode` (không phải `FormCode`)
4. **`FieldConfigView.xaml`** — thêm inline RequiredErrorKey section (BoolToVis trên IsRequiredExpanded):
   - TextEdit (LostFocus), "+ Tạo key" button, preview VI text
5. **`015_ui_field_add_required_error_key.sql`** — migration đã được confirmed chạy trên DB thật
6. **`docs/spec/09_FIELD_CONFIG_GUIDE.md`** — cập nhật key convention sang TableCode

**Key naming convention (final):**
- Label/Placeholder/Tooltip: `{tableCode}.field.{fieldCode}.label` (dùng TableCode!)
- Required error: `{tableCode}.val.{fieldCode}.required`

---

### Bug Fix — PlaceholderKey/TooltipKey hiển thị sai (commit b400802)

- Load từ DB: `PlaceholderKey`/`TooltipKey` null → fallback về `LabelKey` → hiển thị `.label` suffix
- `SyncKeyIfLinked`: khi generate LabelKey → kéo theo Placeholder/Tooltip
- **Fix:** bỏ fallback + xóa `SyncKeyIfLinked` — mỗi key độc lập

---

## Trạng thái hiện tại

- Build: **0 warnings, 0 errors** ✅
- FieldConfig RequiredErrorKey + bug fix keys: **HOÀN THÀNH** ✅
- Commits pushed: **bd6f765, c64aa4a, b400802**

## Việc tiếp theo (ưu tiên)

1. **Run migration 010** nếu chưa có `Is_Required` trong `Ui_Field` (user hỏi)
2. **Control Props tab JSON preview sidebar** — principle 3 từ HTML design (right column hiển thị ControlPropsJson realtime)
3. **Backend** — MetadataEngine implement (Phase 6)
