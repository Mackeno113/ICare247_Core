# Comment Rules — ICare247 (Tiếng Việt)

## File Header — Bắt buộc cho mọi file .cs

```csharp
// File    : {FileName}.cs
// Module  : {ModuleName}
// Layer   : {Domain | Application | Infrastructure | Api}
// Purpose : {Mô tả ngắn bằng tiếng Việt}
```

## Class / Interface

```csharp
/// <summary>
/// Repository truy vấn metadata form từ bảng <c>Ui_Form</c> qua Dapper.
/// Tất cả query phải parameterized, filter <c>Is_Active = 1</c>.
/// </summary>
public class FormRepository : IFormRepository
```

## Public Method — Bắt buộc bằng Tiếng Việt

Mỗi hàm C# **phải có** XML doc comment gồm đủ 4 phần:

1. `<summary>` — Ý nghĩa của hàm (làm gì, tại sao tồn tại)
2. `<param>` — Giải thích từng tham số
3. `<returns>` — Giá trị trả về và các trường hợp (null, empty, v.v.)
4. `<remarks>` (**bắt buộc nếu có side-effect**) — Sự kiện/hành động xảy ra SAU khi hàm chạy:
   - Raise event nào? (`IsDirty = true`, `PropertyChanged`, domain event...)
   - Trigger gì? (reload UI, gọi command, publish message...)
   - Thay đổi state nào? (cache bị xóa, flag bị set...)

```csharp
/// <summary>
/// Load metadata form theo Form_Code. Trả về <c>null</c> nếu không tìm thấy.
/// </summary>
/// <param name="formCode">Ui_Form.Form_Code — unique identifier của form.</param>
/// <param name="ct">Cancellation token để hủy query nếu request bị cancel.</param>
/// <returns><see cref="FormMetadata"/> nếu tìm thấy; <c>null</c> nếu không tồn tại.</returns>
/// <remarks>
/// Sau khi load thành công: set <c>_cache[formCode]</c> → các lần gọi tiếp theo lấy từ cache.
/// Không raise event; caller tự quyết định notify UI.
/// </remarks>
/// <exception cref="SqlException">Throw khi DB lỗi — không swallow.</exception>
public async Task<FormMetadata?> GetByCodeAsync(string formCode, CancellationToken ct = default)
```

### Ví dụ hàm có side-effect / trigger

```csharp
/// <summary>
/// Gán EditorType mới cho field đang chọn.
/// </summary>
/// <param name="value">Tên editor type mới (vd: "LookupBox", "TextBox").</param>
/// <remarks>
/// Sự kiện theo sau:
/// - Gọi <see cref="LoadControlPropSchema"/> → reload tab Control Props.
/// - Set <c>IsDirty = true</c> → kích hoạt nút Save.
/// - Nếu đổi từ LookupBox sang loại khác: hiện dialog xác nhận → xóa FK config nếu đồng ý.
/// </remarks>
public string SelectedEditorType { get => ...; set => ... }
```

### Ví dụ hàm không có side-effect

```csharp
/// <summary>
/// Tính tổng số field đang active trong section.
/// </summary>
/// <param name="sectionId">ID của section cần đếm.</param>
/// <returns>Số lượng field có <c>Is_Active = 1</c>.</returns>
/// <remarks>Không có side-effect. Không thay đổi state.</remarks>
public int CountActiveFields(int sectionId)
```

## Logic Block trong Method

```csharp
// ── 1. Check cache ───────────────────────────────────────
// ── 2. Load from DB ─────────────────────────────────────
// ── 3. Build response ────────────────────────────────────
```

## Edge Case / Null Check

```csharp
// NULL-SAFE: Identifier không tồn tại trong context → trả null, không throw.
// Lý do: form có thể chưa có giá trị khi mới load → không phải lỗi.
if (!context.TryGetValue(node.Name, out var value))
    return null;
```

## TODO Tags

```csharp
// TODO(phase2): Hỗ trợ array index trong dot-notation path
// FIXME: Race condition nếu 2 request cùng compile cùng 1 expression
// NOTE: Dùng OrdinalIgnoreCase vì Column_Code là technical name
// HACK: Workaround tạm thời, cần refactor
```
