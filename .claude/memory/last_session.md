# Last Session Summary

> Cập nhật: 2026-06-01 (session 31 — Bug fixes: Sync Schema + AddField + Is_Virtual)

## Trạng thái cuối session

- **Branch:** `master`
- **Commit cuối:** `dcc42f6` fix: + Field tự động mở Chi tiết để cấu hình và lưu
- **Build:** Backend 0/0, WPF Release 0/0

## Đã làm trong session này

1. **BE-005 Is_Virtual** (commit `49f9daf`) — toàn bộ stack (DB migration 018, Domain, Repositories, Blazor, WPF)

2. **Fix: Đồng bộ Schema không lưu DB** (commit `ba7e2ac`)
   - Root cause: `ExecuteSyncSchemaAsync` chỉ update tree in-memory, không gọi `SaveFieldAsync`/`DeleteFieldAsync`
   - Fix: thêm `DeleteFieldAsync` + `PersistSyncSchemaAsync` async sau khi `ShowDialog` trả về

3. **Fix: + Field không lưu được / FK violation / không đổi tên được** (commits `d93539e`, `2d244d1`, `dcc42f6`)
   - Bug 1: `ExecuteAddField` dùng fake positive Id → UPDATE row không tồn tại → silent fail
   - Fix: dùng negative temp Id + detect `Id <= 0` → `mode:"new"` + `fieldId:0` → INSERT
   - Bug 2: `FieldConfigViewModel.ExecuteSaveAsync` không capture return value → `FieldId` vẫn 0 sau save
   - Fix: `savedId = await SaveFieldAsync(...)` → update `FieldId` + switch mode `"edit"`
   - Bug 3: `Column_Id NOT NULL` → FK violation khi virtual field không chọn cột
   - Fix: migration 019 (Column_Id nullable) + `BuildFieldParam` gửi null khi `ColumnId = 0`
   - Bug 4: QPB read-only → user không thể sửa gì
   - Fix: `ExecuteAddField` tự động mở `FieldConfigView` mode:new ngay khi thêm field

4. **FormTreeNode.IsVirtual** + WPF plumbing: QPB display, hydrate, quick-save

## DB cần chạy trước khi run app

- `db/017_lock_on_edit_replace_is_enabled.sql` (nếu chưa chạy)
- `db/018_add_is_virtual_field.sql` (BE-005)
- `db/019_ui_field_column_id_nullable.sql` (Column_Id nullable cho virtual field)

## Pending tiếp theo

| Task | Status |
|---|---|
| **BE-002** Integration tests | ❌ Chưa làm |
| **BE-004** Apply Design System tokens Blazor | ❌ Chưa làm |
| **BE-003 / WPF-14** Manual E2E test | ⏳ Cần DB thật |
| `DefaultValueJson` orphan property | 🤔 Cần quyết định |
