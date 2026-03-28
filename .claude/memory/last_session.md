# Last Session Summary

> Cập nhật: 2026-03-29 (session 12)

## Đã làm (session 29/03 — WPF Backlog clear)

### WPF-09: ColumnPickerDialog (Browse Column Popup)
- `ViewNames.ColumnPickerDialog` constant
- `ColumnPickerDialogViewModel.cs`: IDialogAware, search/filter realtime theo ColumnCode/DataType
- `ColumnPickerDialog.xaml`: ListBox với custom item style, double-click → select
- `FormsModule.cs`: RegisterDialog
- `FieldConfigViewModel.cs`: IDialogService? + ExecuteBrowseColumn() thật
- **Commit:** 9059747

### WPF-12: I18n Export/Import CSV/JSON
- `I18nManagerViewModel.cs`: ExecuteExportAsync (SaveFileDialog → CSV/JSON) + ExecuteImportAsync (OpenFileDialog → parse → merge → persist to DB)
- CSV: RFC 4180 parser/writer, Header Key,VI,EN,JA
- JSON: System.Text.Json, PropertyNameCaseInsensitive
- PersistImportedAsync: upsert 3 langs mỗi key
- **Commit:** 037bc34

### WPF-07: Clone Form Deep
- `IFormDataService`: thêm `CloneFormAsync`
- `FormDataService.cs`: CloneFormAsync trong transaction — CloneFormRowAsync (INSERT SELECT schema-aware) + CloneSectionsAsync (loop, oldId→newId map) + CloneFieldsAsync (loop với remapped Section_Id)
- `FormManagerViewModel.cs`: ExecuteDuplicateForm gọi service thật → reload
- **Commit:** 52ba4ce

### WPF-08: Form Preview Dialog
- `ViewNames.FormPreviewDialog` constant
- `FormPreviewModels.cs`: SectionPreviewModel + FieldPreviewModel (EditorTypeBg/Fg trả SolidColorBrush)
- `FormPreviewDialogViewModel.cs`: IDialogAware, load parallel sections+fields, group by SectionCode, orphan section
- `FormPreviewDialog.xaml`: WrapPanel field cards, EditorType badge, RO badge, opacity IsVisible=false
- `FormsModule.cs`: RegisterDialog
- `FormManagerViewModel.cs`: ExecutePreviewForm mở dialog
- **Commit:** 495a90e

---

## Trạng thái hiện tại

- Migration 014: **ĐÃ chạy** trên DB thật ✅
- WPF backlog (WPF-07, -08, -09, -10, -11, -12): **TẤT CẢ HOÀN THÀNH** ✅
- T4-T8 ComboBox/LookupBox panels: **ĐÃ HOÀN THÀNH** (session trước) ✅
- Build: **0 warnings, 0 errors** ✅

## Việc tiếp theo (ưu tiên)

1. **Backend** — MetadataEngine implement (Phase 6)
2. **T11 (Blazor)** — LookupComboBoxRenderer static (low priority)
3. **WPF** — T11 Blazor FormRunner ColSpan support nếu cần
4. Kiểm tra DB thật với các tính năng mới: Clone Form, Preview Dialog, Browse Column
