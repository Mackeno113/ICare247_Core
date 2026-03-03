// File    : IEventEngine.cs
// Module  : Engine
// Layer   : Domain
// Purpose : Interface xử lý form event và trả về danh sách UI delta để client áp dụng.

using ICare247.Domain.Engine.Models;

namespace ICare247.Domain.Engine;

/// <summary>
/// Engine xử lý event từ form (field changed, blur, load, submit).
/// <para>
/// Flow: Nhận FormEvent → lookup event handlers từ metadata →
/// evaluate điều kiện trigger (IAstEngine) → execute actions →
/// build UiDelta list → trả UiDeltaResponse.
/// </para>
/// </summary>
public interface IEventEngine
{
    /// <summary>
    /// Xử lý một event từ form và tạo danh sách thay đổi UI.
    /// </summary>
    /// <param name="formEvent">
    /// Event bao gồm: loại event, field nguồn, formId, tenantId, context hiện tại.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="UiDeltaResponse"/> chứa danh sách thay đổi UI client cần áp dụng.
    /// Trả <see cref="UiDeltaResponse.Empty"/> nếu event không trigger action nào.
    /// </returns>
    Task<UiDeltaResponse> HandleEventAsync(
        FormEvent formEvent,
        CancellationToken ct = default);
}
