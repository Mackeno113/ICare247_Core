# Last Session Summary

> Cập nhật: 2026-05-31 (session 29 — Audit + Tracking cleanup + BE-005 design)

## Trạng thái cuối session

- **Branch:** `master`
- **Commit cuối:** `b028b5a` docs: thêm hướng dẫn cấu hình Validation Rule Editor (spec/10)
- **Build:** WPF 0 error/0 warning, Blazor 0 error/0 warning

## Đã làm trong session này

1. **Commit UI Optimization WPF** (`37ab5cf`) — DesignTokens.xaml + Controls.xaml + migrate 15 XAML sang shared resources (Phases 0-4)
2. **Audit tracking** — phát hiện BE-001/WPF-10/11/12/13 đã done từ trước, cập nhật TASKS.md + project_current_phase.md
3. **Verify renderers** — NumericBoxRenderer + DatePickerRenderer đã implement đầy đủ + wired vào FieldRenderer
4. **Docs** — tạo `docs/spec/10_VALIDATION_RULE_GUIDE.md` (445 dòng, hướng dẫn đầy đủ 6 loại rule)
5. **Thảo luận** — chuẩn hóa CSDL cho bài toán cascading Tỉnh/Xã, thiết kế Is_Virtual field concept
6. **Task BE-005** — tạo task Is_Virtual field với đầy đủ scope + files liên quan

## Task tiếp theo — BE-005 (ưu tiên cao)

**Implement `Is_Virtual` flag cho `Ui_Field`** — hỗ trợ field UI-only không lưu DB.

Use case: Form NhanVien có `NoiSinh_TinhThanh` (LookupComboBox, UI helper lọc tỉnh)
+ `NoiSinh_XaPhuong` (LookupBox, lưu DB). TinhThanh là field ảo — không cần cột trong DB.

**Thứ tự implement:**
1. `db/018_add_is_virtual_field.sql` — `ALTER TABLE Ui_Field ADD Is_Virtual BIT NOT NULL DEFAULT 0`
2. Backend: `FieldMetadata.IsVirtual` + `FieldRepository` + `FormRepository`
3. Blazor: `FieldState.IsVirtual` + `FormRunner` filter submit payload
4. WPF: `FieldConfigRecord` + `FieldDataService` + ViewModel + View (checkbox Behavior tab)
5. ADR-018 + docs

**Files cần đọc khi bắt đầu:**
- `src/backend/src/ICare247.Domain/Entities/Form/FieldMetadata.cs`
- `src/backend/src/ICare247.Infrastructure/Repositories/FieldRepository.cs`
- `src/backend/src/ICare247.Blazor.RuntimeCheck/Models/RuntimeModels.cs`
- `src/backend/src/ICare247.Blazor.RuntimeCheck/Pages/FormRunner.razor`
- `src/frontend/ConfigStudio.WPF.UI/src/ConfigStudio.WPF.UI.Core/Data/FieldConfigRecord.cs`

## Pending thực sự (sau audit)

| Task | Status |
|---|---|
| **BE-005** Is_Virtual field | 🟠 Pending — bắt đầu session sau |
| **BE-002** Integration tests | ❌ Chưa làm |
| **BE-004** Apply Design System tokens Blazor | ❌ Chưa làm |
| **BE-003 / WPF-14** Manual E2E test | ⏳ Cần DB thật |
| **Gap 2** Pre-populate tỉnh khi load edit | 🟠 Pending — sau BE-005 |
| `DefaultValueJson` orphan property | 🤔 Cần quyết định |

## DB cần chạy trước khi run app
- `db/017_lock_on_edit_replace_is_enabled.sql` (nếu chưa chạy)
- `db/018_add_is_virtual_field.sql` (sau khi implement BE-005)
