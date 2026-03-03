// File    : UiDeltaResponse.cs
// Module  : Engine
// Layer   : Domain
// Purpose : Tổng hợp danh sách UI delta trả về từ EventEngine.

namespace ICare247.Domain.Engine.Models;

/// <summary>
/// Kết quả từ <see cref="IEventEngine.HandleEventAsync"/> — danh sách thay đổi UI.
/// Client áp dụng các delta theo thứ tự trong danh sách.
/// </summary>
/// <param name="Delta">
/// Danh sách thay đổi UI cần áp dụng. Rỗng nếu event không trigger action nào.
/// </param>
public sealed record UiDeltaResponse(IReadOnlyList<UiDelta> Delta)
{
    /// <summary>
    /// Response rỗng — event không trigger bất kỳ UI change nào.
    /// </summary>
    public static UiDeltaResponse Empty { get; } = new([]);
}
