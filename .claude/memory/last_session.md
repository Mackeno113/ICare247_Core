# Last Session Summary

> Cập nhật: 2026-05-31 (session 30 — BE-005 Is_Virtual field)

## Trạng thái cuối session

- **Branch:** `master`
- **Commit cuối:** `b028b5a` docs: thêm hướng dẫn cấu hình Validation Rule Editor (spec/10)
- **Build:** WPF 0 error/0 warning, Blazor 0 error/0 warning

## Đã làm trong session này

1. **BE-005 Is_Virtual field** (commit `49f9daf`) — toàn bộ stack:
   - `db/018_add_is_virtual_field.sql` — ALTER TABLE Ui_Field ADD Is_Virtual BIT NOT NULL DEFAULT 0
   - Domain: `FieldMetadata.IsVirtual`
   - Infrastructure: `FieldRepository` (3 queries + 2 new initializers) + `FormRepository` (1 query + 1 initializer)
   - Blazor: `FieldMetadataDto.IsVirtual` + `FieldState.IsVirtual` + `FormRunner` mapping
   - WPF: `FieldConfigRecord.IsVirtual` + `FieldDataService` (SELECT/INSERT/UPDATE/BuildFieldParam) + `FieldConfigViewModel` (property + load + save) + `FieldConfigView.xaml` (Behavior tab, row 4 "Field ảo")
   - Build: 0 error / 0 warning (backend + WPF)

## Pending tiếp theo

| Task | Status |
|---|---|
| **BE-002** Integration tests | ❌ Chưa làm |
| **BE-004** Apply Design System tokens Blazor | ❌ Chưa làm |
| **BE-003 / WPF-14** Manual E2E test | ⏳ Cần DB thật |
| **Gap 2** Pre-populate tỉnh khi load edit | 🟠 Pending |
| `DefaultValueJson` orphan property | 🤔 Cần quyết định |

## DB cần chạy trước khi run app
- `db/017_lock_on_edit_replace_is_enabled.sql` (nếu chưa chạy)
- `db/018_add_is_virtual_field.sql` (BE-005 — chạy sau khi pull)
