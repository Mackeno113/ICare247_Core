// File    : RefreshTokenCommand.cs
// Module  : Auth
// Layer   : Application
// Purpose : Command làm mới cặp token từ refresh token còn hiệu lực (rotate token).

using MediatR;
using ICare247.Application.Features.Auth;

namespace ICare247.Application.Features.Auth.Refresh;

/// <summary>
/// Đổi 1 refresh token còn hiệu lực lấy cặp token mới. Token cũ bị thu hồi (rotation).
/// </summary>
/// <param name="RefreshToken">Refresh token gốc client đang giữ.</param>
/// <param name="TenantId">Tenant hiện tại.</param>
/// <param name="IpAddress">IP client (audit phiên mới).</param>
/// <param name="Device">Mô tả thiết bị (audit phiên mới).</param>
public sealed record RefreshTokenCommand(
    string RefreshToken,
    int TenantId,
    string? IpAddress,
    string? Device
) : IRequest<AuthResult>;
