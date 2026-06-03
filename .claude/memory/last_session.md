# Last Session Summary

> Cập nhật: 2026-06-04 (session 33 — Expression Builder fixes + TreeLookupBox)

## Trạng thái cuối session

- **Branch:** `master`
- **Commit cuối:** `f230f97` fix: null-check _autoSave StatusChanged lambda
- **Build:** Backend 0/0, WPF 0/0

## Đã làm trong session này

1. **Hướng dẫn cấu hình Events** (không code)
   - Giải thích Trigger/Condition/Actions, ví dụ cụ thể cascade Tỉnh→Xã

2. **Fix Expression Builder: FIELD context trống** (commit `89d368f`)
   - Root cause: EventEditorViewModel không truyền `"formFields"` khi mở dialog
   - Fix: inject IFormDetailDataService, load + cache fields, truyền List<ExpressionFieldInfo>
   - ExpressionFieldInfo.cs thêm vào Core.Data (dùng chung Events + Grammar)
   - FieldInfo.cs trong Grammar → global alias backward compat

3. **Fix Expression Builder: không nhập được Literal** (commit `89d368f`)
   - Root cause: TreeView chỉ có read-only TextBlock, không có UI nhập giá trị
   - Fix: Thêm Literal Editor panel vàng (TextBox + NetType ComboBox + Áp dụng)
   - Hiện tự động khi SelectedNode là Literal, sync giá trị 2 chiều

4. **Implement TreeLookupBox editor type** (commit `bd157b6`)
   - Migration 021: Parent_Column nvarchar(100) NULL vào Ui_Field_Lookup
   - Backend: IDynamicLookupRepository.QueryTreeAsync + BuildSafeSqlForTree
   - MediatR: QueryTreeLookupQuery + Handler
   - API: POST /api/v1/lookups/query-tree (LookupController)
   - Blazor: ILookupQueryService.QueryTreeAsync + LookupQueryService impl
   - Blazor: TreeLookupBoxRenderer.razor — flat→tree, expand/collapse, search, cascade
   - FieldRenderer: case "treelookup"
   - WPF: TreeLookupBox trong AvailableEditorTypes, IsTreeLookupEditor property
   - WPF: ParentColumn prop + save/load (FieldDataService + FieldConfigViewModel)
   - WPF: LookupBoxPropsPanel — panel xanh "Parent Column" khi IsTreeLookupEditor
   - WPF: BuildGuide card 🌳 cho TreeLookupBox

5. **Fix NullReferenceException FormEditorViewModel** (commit `f230f97`)
   - Root cause: _autoSave = null sau DisposeP0Services, StatusChanged lambda vẫn fire
   - Fix: capture local ref + null-check guard trong lambda

## DB cần chạy trước khi run app

- `db/017_lock_on_edit_replace_is_enabled.sql`
- `db/018_add_is_virtual_field.sql`
- `db/019_ui_field_column_id_nullable.sql`
- `db/020_ui_field_add_field_code.sql`
- `db/021_ui_field_lookup_add_parent_column.sql` ← MỚI session này

## Pending tiếp theo

| Task | Status |
|---|---|
| **BE-002** Integration tests ValidationEngine + EventEngine | ❌ Chưa làm |
| **BE-004** Apply Design System tokens Blazor | ❌ Chưa làm |
| **BE-003 / WPF-14** Manual E2E test | ⏳ Cần DB thật |
| Test TreeLookupBox end-to-end với DB thật | ⏳ Cần DB thật |
