// File    : LoginCommand.cs
// Module  : Auth
// Layer   : Application
// Purpose : Command đăng nhập bằng tên đăng nhập + mật khẩu → cấp access token + refresh token.

using MediatR;

namespace ICare247.Application.Features.Auth.Login;

/// <summary>
/// Đăng nhập tài khoản nội bộ (LoaiTaiKhoan = Local) của tenant hiện tại.
/// Handler verify mật khẩu PBKDF2, kiểm trạng thái/khóa/hết hạn, cấp JWT + refresh token.
/// </summary>
/// <param name="Username">Tên đăng nhập.</param>
/// <param name="Password">Mật khẩu thô.</param>
/// <param name="TenantId">Tenant hiện tại (TenantMiddleware phân giải).</param>
/// <param name="RememberMe">Ghi nhớ đăng nhập — kéo dài hạn refresh token.</param>
/// <param name="IpAddress">IP client (audit phiên).</param>
/// <param name="Device">User-Agent / mô tả thiết bị (audit phiên).</param>
public sealed record LoginCommand(
    string Username,
    string Password,
    int TenantId,
    bool RememberMe,
    string? IpAddress,
    string? Device
) : IRequest<AuthResult>;
