// File    : RefreshTokenCommandHandler.cs
// Module  : Auth
// Layer   : Application
// Purpose : Xử lý làm mới token — verify refresh token, thu hồi token cũ, cấp cặp token mới.

using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Auth.Refresh;

/// <summary>
/// Handler cho <see cref="RefreshTokenCommand"/>. Áp dụng refresh-token rotation: mỗi lần
/// làm mới sẽ thu hồi token cũ và cấp token mới, hạn chế tái sử dụng token bị lộ.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    /// <summary>Hạn refresh token mới sau khi rotate (ngày).</summary>
    private const int RefreshDays = 7;

    private readonly IRefreshTokenRepository _refresh;
    private readonly IAuthRepository _auth;
    private readonly IJwtTokenService _jwt;
    private readonly IAuditWriter _audit;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refresh,
        IAuthRepository auth,
        IJwtTokenService jwt,
        IAuditWriter audit,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refresh = refresh;
        _auth = auth;
        _jwt = jwt;
        _audit = audit;
        _logger = logger;
    }

    /// <summary>
    /// Thực thi làm mới. Sự kiện theo sau: token cũ bị thu hồi, client nhận cặp token mới.
    /// </summary>
    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return AuthResult.Fail(AuthStatus.InvalidRefreshToken);

        var hash = _jwt.HashRefreshToken(request.RefreshToken);
        var record = await _refresh.GetByHashAsync(hash, ct);

        var now = DateTime.UtcNow;
        if (record is null || record.DaThuHoi || record.HetHanUtc <= now)
            return AuthResult.Fail(AuthStatus.InvalidRefreshToken);

        var user = await _auth.GetByIdAsync(record.NguoiDungId, ct);
        if (user is null || user.IsDeleted
            || !string.Equals(user.TrangThai, "HoatDong", StringComparison.OrdinalIgnoreCase))
        {
            // Tài khoản không còn hợp lệ → thu hồi token để dọn dẹp.
            await _refresh.RevokeAsync(record.Id, ct);
            return AuthResult.Fail(AuthStatus.InvalidRefreshToken);
        }

        // ── Rotation: thu hồi token cũ, cấp token mới ──
        await _refresh.RevokeAsync(record.Id, ct);

        var roles = await _auth.GetRoleCodesAsync(user.Id, ct);
        var subject = new TokenSubject(
            user.Id, user.TenDangNhap, request.TenantId, user.LaQuanTri, user.CongTyMacDinh_Id, roles);

        var access = _jwt.CreateAccessToken(subject);
        var newRefresh = _jwt.CreateRefreshToken();
        await _refresh.InsertAsync(user.Id, newRefresh.TokenHash, now.AddDays(RefreshDays),
            request.IpAddress, request.Device, ct);

        _logger.LogInformation("Làm mới token — UserId={UserId}", user.Id);
        _audit.Enqueue(new AuditEvent
        {
            TenantId = request.TenantId,
            Category = AuditCategory.Auth,
            Action = AuditAction.TokenRefresh,
            Result = "Success",
            UserId = user.Id,
            Username = user.TenDangNhap,
            IpAddress = request.IpAddress,
            Device = request.Device
        });

        var info = new AuthUserInfo(user.Id, user.TenDangNhap, user.LaQuanTri,
            user.CongTyMacDinh_Id, roles, user.DoiMatKhauLanSau);

        return new AuthResult(AuthStatus.Success, access.Token, newRefresh.Token,
            access.ExpiresInSeconds, info);
    }
}
