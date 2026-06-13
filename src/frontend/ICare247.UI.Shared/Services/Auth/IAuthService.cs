// File    : IAuthService.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Hợp đồng dịch vụ xác thực dùng chung — đăng nhập / đăng xuất / làm mới phiên.

namespace ICare247.UI.Shared.Services.Auth;

/// <summary>Kết quả đăng nhập trả cho màn Login.</summary>
/// <param name="Succeeded">True nếu đăng nhập thành công.</param>
/// <param name="ErrorMessage">Thông báo lỗi thân thiện (null khi thành công).</param>
public sealed record AuthLoginResult(bool Succeeded, string? ErrorMessage = null);

/// <summary>
/// Dịch vụ xác thực người dùng cho frontend: gọi API backend, lưu token, gắn header Bearer,
/// và đồng bộ trạng thái với <c>AuthenticationStateProvider</c>.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Nạp token đã lưu (nếu có) và gắn header Authorization cho HttpClient. Gọi lúc khởi động
    /// shell trước khi gọi các API cần xác thực.
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Đăng nhập bằng tài khoản/mật khẩu. Sự kiện theo sau: nếu thành công, token được lưu,
    /// header Bearer được gắn, trạng thái xác thực cập nhật.
    /// </summary>
    Task<AuthLoginResult> LoginAsync(string username, string password, bool rememberMe,
        CancellationToken ct = default);

    /// <summary>
    /// Đăng xuất phiên hiện tại. Sự kiện theo sau: thu hồi refresh token (best-effort), xóa
    /// token cục bộ, gỡ header, cập nhật trạng thái.
    /// </summary>
    Task LogoutAsync(CancellationToken ct = default);

    /// <summary>Yêu cầu gửi hướng dẫn đặt lại mật khẩu (luôn coi như thành công — chống dò).</summary>
    Task RequestPasswordResetAsync(string usernameOrEmail, CancellationToken ct = default);
}
