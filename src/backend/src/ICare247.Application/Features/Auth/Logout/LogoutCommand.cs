// File    : LogoutCommand.cs
// Module  : Auth
// Layer   : Application
// Purpose : Command đăng xuất — thu hồi refresh token của phiên hiện tại.

using MediatR;

namespace ICare247.Application.Features.Auth.Logout;

/// <summary>
/// Đăng xuất: thu hồi refresh token đang dùng. Access token (JWT) tự hết hạn theo thời gian.
/// </summary>
/// <param name="RefreshToken">Refresh token của phiên cần đăng xuất.</param>
/// <param name="TenantId">Tenant hiện tại.</param>
public sealed record LogoutCommand(string? RefreshToken, int TenantId) : IRequest<Unit>;
