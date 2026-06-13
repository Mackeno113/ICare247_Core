// File    : AuthController.cs
// Module  : Auth
// Layer   : Api
// Purpose : REST endpoint xác thực — đăng nhập, làm mới token, đăng xuất, quên/đặt lại mật khẩu.

using ICare247.Application.Features.Auth;
using ICare247.Application.Features.Auth.ForgotPassword;
using ICare247.Application.Features.Auth.Login;
using ICare247.Application.Features.Auth.Logout;
using ICare247.Application.Features.Auth.Refresh;
using ICare247.Application.Features.Auth.ResetPassword;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace ICare247.Api.Controllers;

/// <summary>
/// Xác thực người dùng. Tenant lấy qua TenantMiddleware (subdomain hoặc header X-Tenant-Id).
/// Tài khoản đọc từ HT_NguoiDung trong Data DB của tenant.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Đăng nhập bằng tên đăng nhập + mật khẩu.
    /// </summary>
    /// <remarks>
    /// POST /api/v1/auth/login (header X-Tenant-Id)
    /// Body: { "username": "admin", "password": "...", "rememberMe": true }
    /// 200: { accessToken, refreshToken, tokenType, expiresIn, user{...} }
    /// </remarks>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest body, CancellationToken ct)
    {
        var cmd = new LoginCommand(
            body.Username ?? "", body.Password ?? "", GetTenantId(),
            body.RememberMe, GetClientIp(), GetUserAgent());
        var result = await _mediator.Send(cmd, ct);
        return result.Status == AuthStatus.Success ? Ok(ToTokenResponse(result)) : MapFailure(result);
    }

    /// <summary>Làm mới cặp token từ refresh token còn hiệu lực (rotate).</summary>
    /// <remarks>POST /api/v1/auth/refresh — Body: { "refreshToken": "..." }</remarks>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest body, CancellationToken ct)
    {
        var cmd = new RefreshTokenCommand(body.RefreshToken ?? "", GetTenantId(), GetClientIp(), GetUserAgent());
        var result = await _mediator.Send(cmd, ct);
        return result.Status == AuthStatus.Success ? Ok(ToTokenResponse(result)) : MapFailure(result);
    }

    /// <summary>Đăng xuất — thu hồi refresh token của phiên.</summary>
    /// <remarks>POST /api/v1/auth/logout — Body: { "refreshToken": "..." }</remarks>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest body, CancellationToken ct)
    {
        await _mediator.Send(new LogoutCommand(body.RefreshToken, GetTenantId()), ct);
        return NoContent();
    }

    /// <summary>
    /// Yêu cầu đặt lại mật khẩu (STUB — chưa gửi email). Luôn trả 200 để chống dò tài khoản.
    /// </summary>
    /// <remarks>POST /api/v1/auth/forgot-password — Body: { "usernameOrEmail": "..." }</remarks>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest body, CancellationToken ct)
    {
        await _mediator.Send(new ForgotPasswordCommand(body.UsernameOrEmail ?? "", GetTenantId()), ct);
        return Ok(new { message = "Nếu tài khoản tồn tại, hướng dẫn đặt lại mật khẩu sẽ được gửi." });
    }

    /// <summary>Đặt lại mật khẩu bằng token (STUB — chưa hỗ trợ).</summary>
    /// <remarks>POST /api/v1/auth/reset-password — Body: { "token": "...", "newPassword": "..." }</remarks>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest body, CancellationToken ct)
    {
        var ok = await _mediator.Send(
            new ResetPasswordCommand(body.Token ?? "", body.NewPassword ?? "", GetTenantId()), ct);
        if (ok) return Ok(new { message = "Đặt lại mật khẩu thành công." });

        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Type = "https://icare247.vn/errors/not-implemented",
            Title = "Chưa hỗ trợ",
            Status = StatusCodes.Status501NotImplemented,
            Detail = "Tính năng đặt lại mật khẩu qua email đang được hoàn thiện."
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Map kết quả thất bại sang ProblemDetails với HTTP status phù hợp.</summary>
    private IActionResult MapFailure(AuthResult r) => r.Status switch
    {
        AuthStatus.Locked => Problem(StatusCodes.Status423Locked, "Tài khoản tạm khóa",
            r.LockUntilUtc is { } u
                ? $"Đăng nhập sai nhiều lần. Thử lại sau {Math.Max(1, (int)Math.Ceiling((u - DateTime.UtcNow).TotalMinutes))} phút."
                : "Tài khoản đang bị khóa tạm thời do đăng nhập sai nhiều lần."),
        AuthStatus.Disabled => Problem(StatusCodes.Status403Forbidden, "Tài khoản ngừng hoạt động",
            "Tài khoản của bạn đã ngừng hoạt động. Liên hệ quản trị viên."),
        AuthStatus.Expired => Problem(StatusCodes.Status403Forbidden, "Tài khoản hết hạn",
            "Tài khoản của bạn đã hết hạn sử dụng. Liên hệ quản trị viên."),
        AuthStatus.TwoFactorRequired => Problem(StatusCodes.Status401Unauthorized, "Cần xác thực 2 bước",
            "Tài khoản yêu cầu xác thực 2 bước — tính năng đang được hoàn thiện."),
        AuthStatus.InvalidRefreshToken => Problem(StatusCodes.Status401Unauthorized, "Phiên không hợp lệ",
            "Phiên đăng nhập đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại."),
        _ => Problem(StatusCodes.Status401Unauthorized, "Đăng nhập thất bại",
            "Tên đăng nhập hoặc mật khẩu không đúng.")
    };

    private ObjectResult Problem(int status, string title, string detail) => StatusCode(status, new ProblemDetails
    {
        Type = "https://icare247.vn/errors/auth",
        Title = title,
        Status = status,
        Detail = detail
    });

    /// <summary>Tạo payload token trả client (camelCase qua JSON options mặc định).</summary>
    private static object ToTokenResponse(AuthResult r) => new
    {
        accessToken = r.AccessToken,
        refreshToken = r.RefreshToken,
        tokenType = "Bearer",
        expiresIn = r.ExpiresInSeconds,
        user = r.User is null ? null : new
        {
            id = r.User.UserId,
            username = r.User.Username,
            isAdmin = r.User.IsAdmin,
            defaultCompanyId = r.User.DefaultCompanyId,
            roles = r.User.Roles,
            mustChangePassword = r.User.MustChangePassword
        }
    };

    private int GetTenantId()
        => HttpContext.RequestServices.GetRequiredService<ITenantContext>().TenantId;

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent()
    {
        var ua = Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(ua) ? null : (ua.Length > 300 ? ua[..300] : ua);
    }
}

/// <summary>Body POST /api/v1/auth/login.</summary>
public sealed class LoginRequest
{
    [JsonPropertyName("username")] public string? Username { get; init; }
    [JsonPropertyName("password")] public string? Password { get; init; }
    [JsonPropertyName("rememberMe")] public bool RememberMe { get; init; }
}

/// <summary>Body cho refresh / logout.</summary>
public sealed class RefreshRequest
{
    [JsonPropertyName("refreshToken")] public string? RefreshToken { get; init; }
}

/// <summary>Body POST /api/v1/auth/forgot-password.</summary>
public sealed class ForgotPasswordRequest
{
    [JsonPropertyName("usernameOrEmail")] public string? UsernameOrEmail { get; init; }
}

/// <summary>Body POST /api/v1/auth/reset-password.</summary>
public sealed class ResetPasswordRequest
{
    [JsonPropertyName("token")] public string? Token { get; init; }
    [JsonPropertyName("newPassword")] public string? NewPassword { get; init; }
}
