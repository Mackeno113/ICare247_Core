// File    : IAuthService.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Hợp đồng dịch vụ xác thực dùng chung — đăng nhập / đăng xuất / trạng
//           thái phiên. Module Auth và Administration đều phụ thuộc abstraction này.

namespace ICare247.UI.Shared.Services.Auth;

/// <summary>
/// Dịch vụ xác thực người dùng cho frontend. Các màn login/logout và shell layout
/// dựa vào abstraction này để biết người dùng đã đăng nhập hay chưa.
/// </summary>
public interface IAuthService
{
    /// <summary>True nếu phiên hiện tại đã đăng nhập (có token hợp lệ).</summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Đăng nhập bằng tài khoản/mật khẩu. Sự kiện theo sau: nếu thành công,
    /// <see cref="IsAuthenticated"/> = true và token được lưu.
    /// </summary>
    /// <returns>True nếu đăng nhập thành công.</returns>
    Task<bool> LoginAsync(string username, string password, CancellationToken ct = default);

    /// <summary>
    /// Đăng xuất phiên hiện tại. Sự kiện theo sau: xóa token,
    /// <see cref="IsAuthenticated"/> = false.
    /// </summary>
    Task LogoutAsync(CancellationToken ct = default);
}
