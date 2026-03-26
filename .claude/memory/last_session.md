# Last Session Summary

> Cập nhật: 2026-03-26 (session 8)

## Đã làm (session 26/03 — session 8)

### Field Config Schema Fix — Wave C + D

**Bối cảnh:** ADR-010/011/012 — `Is_Required` + `Is_Enabled` thành cột tĩnh trong `Ui_Field`; thêm rule types `Length`/`Compare`; thêm action types `SET_ENABLED`/`CLEAR_VALUE`/`SHOW_MESSAGE`.

#### Wave C — ConfigStudio WPF (commit 707c882)

| File | Thay đổi |
|---|---|
| `FieldConfigRecord.cs` | Thêm `IsRequired`, `IsEnabled` |
| `FieldDataService.cs` | SELECT/INSERT/UPDATE thêm `Is_Required`, `Is_Enabled` |
| `FieldConfigViewModel.cs` | Thêm `IsEnabled`; xóa `ToggleRequiredRule` (IsRequired giờ là cột DB) |
| `FieldConfigView.xaml` | Behavior card 2×2: IsVisible + IsReadOnly / IsRequired + IsEnabled |
| `ValidationRuleEditorViewModel.cs` | Xóa `Required` option; thêm `Length` + `Compare`; properties: IsCompareType, EditCompareField, EditCompareOp, CompareOpOptions, CompareExpressionPreview, HasComparePreview |
| `ValidationRuleEditorView.xaml` | Thêm Compare section + preview Border (dùng HasComparePreview + BoolToVis) |
| `EventEditorViewModel.cs` | ActionTypeOptions: thêm SET_ENABLED, CLEAR_VALUE, SHOW_MESSAGE |

Build WPF: **0 errors, 0 warnings** ✅

#### Wave D — Spec docs (uncommitted — staged cho commit này)

| File | Thay đổi |
|---|---|
| `docs/spec/02_DATABASE_SCHEMA.md` | Thêm `Is_Required`/`Is_Enabled` vào `Ui_Field`; xóa note lỗi thời; cập nhật Val_Rule_Type + Evt_Action_Type |
| `docs/spec/04_ENGINE_SPEC.md` | ValidationEngine: Field_Id trực tiếp (không còn Val_Rule_Field junction); thêm Length/Compare rule types; EventEngine: thêm 3 action types |
| `docs/spec/05_ACTION_RULE_PARAM_SCHEMA.md` | `Required` deprecated; JSON schema đầy đủ cho Length, Compare, SET_ENABLED, CLEAR_VALUE, SHOW_MESSAGE |

Build backend: **0 errors, 0 warnings** ✅
Build WPF: **0 errors, 0 warnings** ✅

---

## Trạng thái hiện tại

- Migration SQL 010–012: **file đã tạo, CHƯA chạy trên DB thật** ⚠️
- Wave C: **done + committed** ✅
- Wave D: **done + committed** ✅

## Việc tiếp theo (ưu tiên)

1. **Wave A** — Chạy migrations 010–012 trên DB thật (`db/migrations/`)
2. **Wave B** — Backend: `FieldMetadata.cs`, `FieldRepository.cs`, `ValidationEngine.cs` (Length/Compare), `EventEngine.cs` (SET_ENABLED/CLEAR_VALUE/SHOW_MESSAGE), `UiDelta.cs`
3. **MetadataEngine** — implement IMetadataEngine (Phase 6 còn lại)
4. **Integration tests** — backend
