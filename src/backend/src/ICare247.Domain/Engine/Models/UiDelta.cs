// File    : UiDelta.cs
// Module  : Engine
// Layer   : Domain
// Purpose : Một thay đổi UI cần áp dụng sau khi EventEngine xử lý xong.

namespace ICare247.Domain.Engine.Models;

/// <summary>
/// Một delta thay đổi UI cho một field cụ thể.
/// Client (Blazor) nhận danh sách delta và áp dụng tuần tự.
/// </summary>
/// <param name="FieldCode">Field bị ảnh hưởng (Field_Code). Null nếu delta áp dụng cho toàn form.</param>
/// <param name="Action">
/// Loại thay đổi: 'SET_VALUE' | 'SET_VISIBLE' | 'SET_REQUIRED' | 'SET_READONLY' |
/// 'SET_ENABLED' | 'CLEAR_VALUE' | 'SHOW_MESSAGE' |
/// 'RELOAD_OPTIONS' | 'TRIGGER_VALIDATION'.
/// </param>
/// <param name="Data">
/// Payload kèm theo action — tuỳ loại action.
/// Ví dụ SET_VALUE: <c>{"value": 42}</c>.
/// Ví dụ SET_VISIBLE: <c>{"visible": false}</c>.
/// </param>
public sealed record UiDelta(
    string? FieldCode,
    string Action,
    IReadOnlyDictionary<string, object?>? Data);
