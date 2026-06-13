// File    : AuthResult.cs
// Module  : Auth
// Layer   : Application
// Purpose : Kết quả dùng chung cho luồng đăng nhập / làm mới token + thông tin người dùng.

namespace ICare247.Application.Features.Auth;

/// <summary>Trạng thái kết quả xác thực — controller map sang HTTP status + thông báo.</summary>
public enum AuthStatus
{
    /// <summary>Thành công — đã cấp token.</summary>
    Success = 0,

    /// <summary>Sai tên đăng nhập hoặc mật khẩu (gộp chung để chống dò tài khoản).</summary>
    InvalidCredentials = 1,

    /// <summary>Tài khoản đang bị khóa tạm thời do đăng nhập sai nhiều lần.</summary>
    Locked = 2,

    /// <summary>Tài khoản ngừng hoạt động.</summary>
    Disabled = 3,

    /// <summary>Tài khoản đã hết hạn sử dụng.</summary>
    Expired = 4,

    /// <summary>Yêu cầu nhập mã 2FA (chưa hỗ trợ ở pha này).</summary>
    TwoFactorRequired = 5,

    /// <summary>Refresh token không hợp lệ / đã thu hồi / hết hạn.</summary>
    InvalidRefreshToken = 6
}

/// <summary>Thông tin người dùng trả về client sau khi đăng nhập (không gồm dữ liệu nhạy cảm).</summary>
/// <param name="UserId">HT_NguoiDung.Id.</param>
/// <param name="Username">Tên đăng nhập.</param>
/// <param name="IsAdmin">Cờ quản trị.</param>
/// <param name="DefaultCompanyId">Công ty mặc định (phạm vi dữ liệu).</param>
/// <param name="Roles">Mã vai trò.</param>
/// <param name="MustChangePassword">Bắt buộc đổi mật khẩu ở phiên này.</param>
public sealed record AuthUserInfo(
    long UserId,
    string Username,
    bool IsAdmin,
    long? DefaultCompanyId,
    IReadOnlyList<string> Roles,
    bool MustChangePassword);

/// <summary>
/// Kết quả xác thực. Khi <see cref="Status"/> = Success: token + thông tin user có giá trị;
/// ngược lại chỉ <see cref="Status"/> (+ <see cref="LockUntilUtc"/> khi bị khóa) có nghĩa.
/// </summary>
/// <param name="Status">Trạng thái kết quả.</param>
/// <param name="AccessToken">JWT (null nếu thất bại).</param>
/// <param name="RefreshToken">Refresh token gốc (null nếu thất bại).</param>
/// <param name="ExpiresInSeconds">Số giây access token còn hiệu lực.</param>
/// <param name="User">Thông tin người dùng (null nếu thất bại).</param>
/// <param name="LockUntilUtc">Thời điểm hết khóa (UTC) khi Status = Locked.</param>
public sealed record AuthResult(
    AuthStatus Status,
    string? AccessToken = null,
    string? RefreshToken = null,
    int ExpiresInSeconds = 0,
    AuthUserInfo? User = null,
    DateTime? LockUntilUtc = null)
{
    /// <summary>Tạo kết quả thất bại chỉ gồm trạng thái.</summary>
    public static AuthResult Fail(AuthStatus status, DateTime? lockUntilUtc = null)
        => new(status, LockUntilUtc: lockUntilUtc);
}
