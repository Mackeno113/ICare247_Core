// File    : IEventActionHandler.cs
// Module  : Engines/EventActions
// Layer   : Application
// Purpose : Strategy cho 1 loại Event action (AUDIT-5). Thêm action mới = thêm 1 handler + đăng ký DI,
//           KHÔNG sửa EventEngine (OCP). EventEngine dispatch theo ActionCode qua registry.

using ICare247.Domain.Engine.Models;

namespace ICare247.Application.Engines.EventActions;

/// <summary>
/// Xử lý một loại action (theo <see cref="ActionCode"/>) → sinh danh sách <see cref="UiDelta"/>.
/// </summary>
public interface IEventActionHandler
{
    /// <summary>Mã action nhận diện (canonical, VD "SET_VALUE"). Khớp Action_Code không phân biệt hoa/thường.</summary>
    string ActionCode { get; }

    /// <summary>Thực thi action; trả list delta (rỗng nếu không tạo delta / param thiếu).</summary>
    Task<IReadOnlyList<UiDelta>> ExecuteAsync(EventActionContext context, CancellationToken ct);
}
