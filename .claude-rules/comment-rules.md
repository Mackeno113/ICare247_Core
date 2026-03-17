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

## Public Method

```csharp
/// <summary>
/// Load metadata form theo Form_Code. Trả về <c>null</c> nếu không tìm thấy.
/// </summary>
/// <param name="formCode">Ui_Form.Form_Code — unique identifier của form.</param>
/// <param name="ct">Cancellation token để hủy query nếu request bị cancel.</param>
/// <returns><see cref="FormMetadata"/> nếu tìm thấy; <c>null</c> nếu không tồn tại.</returns>
/// <exception cref="SqlException">Throw khi DB lỗi — không swallow.</exception>
public async Task<FormMetadata?> GetByCodeAsync(string formCode, CancellationToken ct = default)
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
