// File    : INavigationRepository.cs
// Module  : Navigation
// Layer   : Application
// Purpose : Đọc cây menu (HT_ChucNang) đã lọc theo quyền của 1 người dùng (Data DB tenant).

using ICare247.Application.Features.Navigation;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository menu động — đọc <c>HT_ChucNang</c> theo quyền <c>HT_VaiTro_Quyen</c>
/// của các vai trò người dùng. Connection lấy qua <see cref="IDataDbConnectionFactory"/>
/// (đã trỏ đúng Data DB của tenant hiện tại).
/// </summary>
public interface INavigationRepository
{
    /// <summary>
    /// Trả danh sách node user được thấy (Xem=1) + các node tổ tiên (giữ cây liền mạch),
    /// kèm cờ thao tác đã hợp (OR) qua nhiều vai trò. Sort theo ThuTu.
    /// </summary>
    Task<IReadOnlyList<MeNavNodeDto>> GetForUserAsync(long userId, CancellationToken ct = default);
}
