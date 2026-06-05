# Last Session Summary

> Cập nhật: 2026-06-05 (session 36 — LookupBox "thêm mới entity" full-stack)

## Trạng thái cuối session

- **Branch:** `master`
- **Build:** API / Application / Infrastructure / Domain / RuntimeCheck / WPF Modules.Forms đều 0/0

## Session 36 — Thêm mới entity trên LookupBox (full 3 tầng + WPF)

Tính năng: dropdown LookupBox có nút "➕ Thêm mới" → mở dialog render Ui_Form (AddFormCode)
→ nhập liệu → POST insert vào bảng nguồn → auto-select bản ghi mới. Bật/tắt theo từng field.

**Cơ chế then chốt:**
1. Config 2 cột `Ui_Field_Lookup`: `Allow_Add_New` + `Add_Form_Code` (migration 022).
2. Backend đọc `Source_Name` theo `fieldId` (KHÔNG nhận bảng từ client) → verify tenant → insert parameterized + `OUTPUT INSERTED`.
3. `FieldCode = COALESCE(Field_Code, Column_Code)` → FieldCode = tên cột DB → map insert trực tiếp.
4. `LookupAddDialog` tái dùng `FieldRenderer` (như FormRunner) → tự hỗ trợ mọi control + cascade lồng.
5. `OnAddSaved`: reload + set Value → OnChange fire → cascade con reload theo ReloadTriggerField.

**File chính:** DynamicLookupRepository.InsertAsync, InsertLookupCommand/handler, LookupController POST insert,
LookupAddDialog.razor(+css), LookupBoxRenderer (CanAddNew/OpenAddDialog/OnAddSaved),
FieldDataService + FieldConfigViewModel + LookupBoxPropsPanel.xaml (WPF config).

**Setup để chạy:** (1) chạy db/022; (2) tạo Ui_Form bound bảng nguồn, field code = cột DB;
(3) ConfigStudio → bật "Cho phép thêm mới" + nhập Form Code.

**Caveat:** insert chưa chạy ValidationEngine server-side (chỉ check required client); field virtual bị loại;
chưa test runtime (cần DB + Ui_Form đích) — mới verify compile.

---

## Session 35 (trước đó)

- **Build:** RuntimeCheck 0/0, Infrastructure 0/0, API 0/0

## Đã làm trong session này

1. **Fix bug cascade lookup (root cause + giải pháp)**
   - Lỗi: `NotSupportedException: member NoiSinh_TinhThanhID of type JsonElement cannot be used as a parameter value`
   - Root cause: `QueryDynamicRequest.ContextValues` là `Dictionary<string,object?>` → System.Text.Json deserialize value thành `JsonElement`, truyền thẳng vào Dapper → nổ. Lần đầu (query không `@param`) chạy 200, cascade mới nổ.
   - Fix: helper `UnwrapParamValue()` trong `DynamicLookupRepository` — unwrap JsonElement → string/long/double/bool/null. Áp dụng `QueryAsync` + `QueryTreeAsync`.

2. **Cơ chế cascade runtime (đã verify trong mã)**
   - `@param` trong filterSql **phải trùng FieldCode** field cha — repo bind context key (= FieldCode) trực tiếp vào Dapper param cùng tên.
   - Reload do `ReloadTriggerField` (đơn, lưu `Ui_Field_Lookup`) — renderer đọc trường này.
   - `filterParams` (panel ⚡) + `reloadOnChange` (tag 🔄) trong Control_Props **KHÔNG** được RuntimeCheck renderer tiêu thụ → chỉ cần Filter SQL + ReloadTriggerField.

3. **Keyboard nav cho LookupBox + TreeLookupBox**
   - ↑/↓ di chuyển highlight, Enter chọn dòng highlight, Escape đóng.
   - `_highlightIndex` + class `.highlight` (viền trái màu primary). Gõ → highlight dòng 0.

4. **TreeLookupBox lọc trực tiếp trên control (mirror LookupBox)**
   - EditBox `<div>` → `<input>` gõ thẳng; bỏ thanh search riêng trong popup.
   - Node `@onclick` → `@onmousedown` (chạy trước blur); toggle ▸▾ `@onmousedown` + `preventDefault` để không làm input blur → popup giữ mở khi expand.
   - CSS: thêm `.lookupbox-search-input` + `:focus-within` vào tree css (CSS isolation), xóa `.popup-search`.

5. **Docs** — tạo `docs/spec/12_CASCADE_LOOKUP_GUIDE.md` (hướng dẫn cấu hình Tỉnh→Xã + 3 cấp + lỗi thường gặp).

## DB cần chạy trước khi run app

- `db/017_lock_on_edit_replace_is_enabled.sql`
- `db/018_add_is_virtual_field.sql`
- `db/019_ui_field_column_id_nullable.sql`
- `db/020_ui_field_add_field_code.sql`
- `db/021_ui_field_lookup_add_parent_column.sql`
- `db/022_ui_field_lookup_add_addnew.sql`

## Pending tiếp theo

| Task | Status |
|---|---|
| **BE-002** Integration tests ValidationEngine + EventEngine | ❌ Chưa làm |
| **BE-004** Apply Design System tokens Blazor | ❌ Chưa làm |
| **WPF-14** Test LookupBox end-to-end | ⏳ Cần DB thật |
| Test TreeLookupBox end-to-end với DB thật | ⏳ Cần DB thật |
| i18n captionKey: thêm `Sys_Resource` cho `ds_tinhthanh.col.ten_tinh` | ⏳ Cần chạy SQL |
| Cascade dropdown Tỉnh→Xã: backend JOIN + trả đủ hierarchy ID | ❌ Chưa implement |
