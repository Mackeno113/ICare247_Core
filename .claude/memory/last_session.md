# Last Session Summary

> Cập nhật: 2026-03-25 (session 6)

## Đã làm (session 25/03 — session 6)

### Phase 10 — Schema Extension: Tab + Lookup

Toàn bộ session là **thảo luận thiết kế + viết migration SQL + cập nhật spec**.
Không có code C# nào thay đổi — build vẫn 0 errors, 0 warnings.

#### Quyết định thiết kế chốt

| Chủ đề | Quyết định |
|---|---|
| Multi-tab form | Thêm bảng `Ui_Tab` riêng (Option A). 0-1 tab = render phẳng, backward compat |
| Col_Span | Column riêng `tinyint DEFAULT 1` (1/2/3) trên `Ui_Field`, KHÔNG để trong JSON |
| FK Lookup config | Bảng `Ui_Field_Lookup` 1-1 với `Ui_Field`, Query_Mode/Source/Filter/Popup_Columns_Json |
| Lookup phân loại | `Ui_Field.Lookup_Source`: NULL / 'static' (Sys_Lookup) / 'dynamic' (Ui_Field_Lookup) |
| Popup columns | JSON array trong `Ui_Field_Lookup.Popup_Columns_Json` (không sub-table) |
| Sys_Lookup Tenant | Đổi `DEFAULT 0` → `NULL FK→Sys_Tenant` cho nhất quán toàn hệ thống |

#### Files đã tạo/sửa

| File | Loại |
|---|---|
| `docs/migrations/005_add_ui_tab.sql` | Migration mới |
| `docs/migrations/006_alter_ui_section_add_tab.sql` | Migration mới |
| `docs/migrations/007_alter_ui_field_add_cols.sql` | Migration mới |
| `docs/migrations/008_add_ui_field_lookup.sql` | Migration mới |
| `docs/migrations/009_fix_sys_lookup_tenant.sql` | Migration mới |
| `docs/spec/02_DATABASE_SCHEMA.md` | Cập nhật: Ui_Tab, Ui_Field_Lookup, cols mới, sơ đồ, 33 bảng |
| `TASKS.md` | Cập nhật: Phase 10 done, Decisions Log |

---

## Trạng thái

- Build backend: **0 errors, 0 warnings** ✅
- Build WPF: **0 errors, 0 warnings** ✅
- Migration SQL 005-009: **viết xong, CHƯA chạy trên DB thật** ⚠️

## Việc tiếp theo (ưu tiên — máy khác)

1. **Chạy migrations theo thứ tự**: `003 → 004 → 005 → 006 → 007 → 008 → 009` trên DB thật
2. **Cập nhật Domain entities**: `SectionMetadata` (+ TabId), `FieldMetadata` (+ ColSpan, LookupSource, LookupCode)
3. **Cập nhật Repositories**: `FormRepository`, `FieldRepository` — query mới JOIN Ui_Tab, đọc Col_Span + Lookup fields
4. **Cập nhật ConfigStudio**: `IFieldDataService` + `FieldConfigViewModel` — tab "FK Lookup" config
5. Blazor: support FieldType `select` (LookupBox — gọi Sys_Lookup API)
6. MetadataEngine (IMetadataEngine) — backend
