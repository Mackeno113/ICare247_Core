# Last Session Summary

> Cập nhật: 2026-06-01 (session 32 — Bug fixes: AddField flow + Is_Virtual full stack)

## Trạng thái cuối session

- **Branch:** `master`
- **Commit cuối:** `913b117` ux: chuyen Field Code vao tab Behavior - inline duoi toggle Field ao
- **Build:** Backend 0/0, WPF Release 0/0

## Đã làm trong session này

1. **Fix: Đồng bộ Schema không lưu DB** (commit `ba7e2ac`)
   - Root cause: `ExecuteSyncSchemaAsync` chỉ update tree in-memory
   - Fix: `DeleteFieldAsync` + `PersistSyncSchemaAsync` async sau ShowDialog

2. **Fix: + Field không lưu / FK violation / không đổi tên** (commits `d93539e`, `2d244d1`, `dcc42f6`)
   - Temp Id âm + mode:"new" + auto-open FieldConfigView
   - Migration 019: `Column_Id INT NULL` (virtual field không cần cột)
   - `BuildFieldParam`: ColumnId=0 → NULL

3. **Fix: field mới không hiển thị sau lưu** (commit `b7f4b3b`)
   - Sau save mode:"new" → auto-navigate về FormEditor với `selectedFieldId=savedId`

4. **Fix: Sys_Column rỗng → không chọn được cột** (commit `25071c5`)
   - TextEdit gõ tay ColumnCode khi list rỗng
   - Auto-create Sys_Column khi save (`EnsureColumnExistsAsync`)

5. **Fix: INNER JOIN Sys_Column sau migration 019** (commits `d5a1a2f`, `baf5455`)
   - `FormDetailDataService.GetFieldsByFormAsync`: JOIN → LEFT JOIN
   - `FieldDataService.GetFieldDetailAsync`: JOIN → LEFT JOIN

6. **Cải thiện FieldConfigView UX** (commit `b9360a6`)
   - Thêm Section picker (ComboBox) — có thể thay đổi section
   - Column: ComboBox + TextEdit luôn hiện (không chỉ khi list rỗng)

7. **feat: Field_Code cho virtual field** (commit `0790370`)
   - Migration 020: `ALTER TABLE Ui_Field ADD Field_Code NVARCHAR(100) NULL`
   - Backend: `COALESCE(fi.Field_Code, sc.Column_Code) AS FieldCode`
   - WPF full stack: FieldConfigRecord, FieldDataService, ViewModel, View

8. **UX: Field Code inline trong tab Behavior** (commit `913b117`)
   - Bật "Field ảo" → Field Code TextEdit xuất hiện ngay bên dưới (không cần đổi tab)

9. **Rule mới** (commit `8248a60`)
   - Không tự ý sửa code — phải trình bày nguyên nhân/cách xử lý/các bước và chờ user chốt

## DB cần chạy trước khi run app

- `db/017_lock_on_edit_replace_is_enabled.sql`
- `db/018_add_is_virtual_field.sql`
- `db/019_ui_field_column_id_nullable.sql` ← Column_Id nullable
- `db/020_ui_field_add_field_code.sql` ← Field_Code mới

## Pending tiếp theo

| Task | Status |
|---|---|
| **BE-002** Integration tests | ❌ Chưa làm |
| **BE-004** Apply Design System tokens Blazor | ❌ Chưa làm |
| **BE-003 / WPF-14** Manual E2E test | ⏳ Cần DB thật |
| `DefaultValueJson` orphan property | 🤔 Cần quyết định |
| Verify toàn bộ flow AddField end-to-end | ⏳ Cần test thực tế |
