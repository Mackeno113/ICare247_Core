// File    : LoginCommandHandler.cs
// Module  : Auth
// Layer   : Application
// Purpose : Xử lý đăng nhập — verify mật khẩu PBKDF2, kiểm trạng thái/khóa/hết hạn, lockout,
//           cấp access token (JWT) + refresh token, ghi nhận phiên.

using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Auth.Login;

/// <summary>
/// Handler cho <see cref="LoginCommand"/>. Chống dò tài khoản: mọi sai sót về tài khoản/mật
/// khẩu đều trả <see cref="AuthStatus.InvalidCredentials"/>. Khóa tạm sau nhiều lần sai.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    /// <summary>Số lần sai liên tiếp tối đa trước khi khóa tạm.</summary>
    private const int MaxFailedAttempts = 5;

    /// <summary>Thời gian khóa tạm (phút) khi vượt ngưỡng.</summary>
    private const int LockoutMinutes = 15;

    /// <summary>Hạn refresh token mặc định (ngày).</summary>
    private const int RefreshDaysDefault = 7;

    /// <summary>Hạn refresh token khi "Ghi nhớ đăng nhập" (ngày).</summary>
    private const int RefreshDaysRemember = 30;

    private readonly IAuthRepository _auth;
    private readonly IRefreshTokenRepository _refresh;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IAuditWriter _audit;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IAuthRepository auth,
        IRefreshTokenRepository refresh,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        IAuditWriter audit,
        ILogger<LoginCommandHandler> logger)
    {
        _auth = auth;
        _refresh = refresh;
        _hasher = hasher;
        _jwt = jwt;
        _audit = audit;
        _logger = logger;
    }

    /// <summary>
    /// Thực thi đăng nhập. Sự kiện theo sau: nếu Success → client nhận access + refresh token,
    /// bản ghi refresh token được lưu, LanDangNhapCuoi cập nhật.
    /// </summary>
    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _auth.GetByUsernameAsync(request.Username, ct);

        // Không có user / không phải tài khoản nội bộ / chưa đặt mật khẩu → coi như sai thông tin.
        if (user is null
            || !string.Equals(user.LoaiTaiKhoan, "Local", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(user.MatKhauHash))
        {
            _logger.LogInformation("Đăng nhập thất bại (không tồn tại/không hợp lệ) — Username={Username}", request.Username);
            Audit(request, AuditAction.LoginFailed, "Failed", user?.Id);
            return AuthResult.Fail(AuthStatus.InvalidCredentials);
        }

        // ── Đang bị khóa tạm? ──
        var now = DateTime.UtcNow;
        if (user.KhoaDenKhi is { } lockUntil && lockUntil > now)
        {
            _logger.LogWarning("Đăng nhập bị chặn (đang khóa) — UserId={UserId}, đến {LockUntil}", user.Id, lockUntil);
            Audit(request, AuditAction.LoginLocked, "Failed", user.Id);
            return AuthResult.Fail(AuthStatus.Locked, lockUntil);
        }

        // ── Trạng thái tài khoản ──
        if (!string.Equals(user.TrangThai, "HoatDong", StringComparison.OrdinalIgnoreCase))
        {
            Audit(request, AuditAction.LoginFailed, "Failed", user.Id);
            return AuthResult.Fail(AuthStatus.Disabled);
        }

        if (user.HetHanTaiKhoan is { } expiry && expiry < now)
        {
            Audit(request, AuditAction.LoginFailed, "Failed", user.Id);
            return AuthResult.Fail(AuthStatus.Expired);
        }

        // ── Verify mật khẩu ──
        if (!_hasher.Verify(user.MatKhauHash!, request.Password))
        {
            var newCount = user.SoLanDangNhapSai + 1;
            DateTime? lockTo = newCount >= MaxFailedAttempts ? now.AddMinutes(LockoutMinutes) : null;
            await _auth.RecordLoginFailureAsync(user.Id, newCount, lockTo, ct);

            _logger.LogWarning("Đăng nhập sai mật khẩu — UserId={UserId}, lần sai {Count}", user.Id, newCount);
            Audit(request, lockTo is not null ? AuditAction.LoginLocked : AuditAction.LoginFailed, "Failed", user.Id);
            return lockTo is not null
                ? AuthResult.Fail(AuthStatus.Locked, lockTo)
                : AuthResult.Fail(AuthStatus.InvalidCredentials);
        }

        // ── 2FA (chưa hỗ trợ ký token bước 2 ở pha này) ──
        if (!string.Equals(user.HinhThuc2FA, "None", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Tài khoản yêu cầu 2FA — UserId={UserId} (chưa hỗ trợ).", user.Id);
            return AuthResult.Fail(AuthStatus.TwoFactorRequired);
        }

        // ── Thành công ──
        await _auth.RecordLoginSuccessAsync(user.Id, ct);

        var roles = await _auth.GetRoleCodesAsync(user.Id, ct);
        var subject = new TokenSubject(
            user.Id, user.TenDangNhap, request.TenantId, user.LaQuanTri, user.CongTyMacDinh_Id, roles);

        var access = _jwt.CreateAccessToken(subject);
        var refresh = _jwt.CreateRefreshToken();
        var refreshExpiry = now.AddDays(request.RememberMe ? RefreshDaysRemember : RefreshDaysDefault);

        await _refresh.InsertAsync(user.Id, refresh.TokenHash, refreshExpiry,
            request.IpAddress, request.Device, ct);

        _logger.LogInformation("Đăng nhập thành công — UserId={UserId}, TenantId={TenantId}", user.Id, request.TenantId);
        Audit(request, AuditAction.LoginSuccess, "Success", user.Id);

        var info = new AuthUserInfo(user.Id, user.TenDangNhap, user.LaQuanTri,
            user.CongTyMacDinh_Id, roles, user.DoiMatKhauLanSau);

        return new AuthResult(AuthStatus.Success, access.Token, refresh.Token,
            access.ExpiresInSeconds, info);
    }

    /// <summary>Ghi nhật ký 1 lần đăng nhập (non-blocking). Username luôn lấy từ input để truy vết.</summary>
    private void Audit(LoginCommand req, string action, string result, long? userId)
        => _audit.Enqueue(new AuditEvent
        {
            TenantId = req.TenantId,
            Category = AuditCategory.Auth,
            Action = action,
            Result = result,
            UserId = userId,
            Username = req.Username,
            IpAddress = req.IpAddress,
            Device = req.Device
        });
}
