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
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace ICare247.Api.Controllers;

/// <summary>
/// Xác thực người dùng. Tenant lấy qua TenantMiddleware (subdomain hoặc header X-Tenant-Id).
/// Tài khoản đọc từ HT_NguoiDung trong Data DB của tenant.
/// SEC2-1: refresh token đặt trong cookie HttpOnly (không trả JSON, JS không đọc được).
/// [AllowAnonymous] gắn TỪNG endpoint công khai — riêng logout-all yêu cầu đăng nhập (FallbackPolicy).
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")] // SEC3-3: chặt cho /auth/* — chống brute-force/credential-stuffing
public sealed class AuthController : ControllerBase
{
    /// <summary>Tên cookie chứa refresh token.</summary>
    private const string RefreshCookieName = "ic247.rt";

    /// <summary>Path giới hạn cookie — chỉ gửi cho các endpoint /api/v1/auth.</summary>
    private const string RefreshCookiePath = "/api/v1/auth";

    private readonly IMediator _mediator;

    /// <summary>Domain cookie refresh — rỗng/null = host-only (đúng cho localhost + prod 1-host API). Cấu hình prod qua Auth:RefreshCookie:Domain.</summary>
    private readonly string? _refreshCookieDomain;

    public AuthController(IMediator mediator, IConfiguration config)
    {
        _mediator = mediator;
        _refreshCookieDomain = config["Auth:RefreshCookie:Domain"];
    }

    /// <summary>
    /// Đăng nhập bằng tên đăng nhập + mật khẩu.
    /// </summary>
    /// <remarks>
    /// POST /api/v1/auth/login (header X-Tenant-Id)
    /// Body: { "username": "admin", "password": "...", "rememberMe": true }
    /// 200: { accessToken, tokenType, expiresIn, user{...} } — refresh token đặt trong cookie HttpOnly.
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest body, CancellationToken ct)
    {
        var cmd = new LoginCommand(
            body.Username ?? "", body.Password ?? "", GetTenantId(),
            body.RememberMe, GetClientIp(), GetUserAgent());
        var result = await _mediator.Send(cmd, ct);
        if (result.Status != AuthStatus.Success) return MapFailure(result);

        SetRefreshCookie(result.RefreshToken!, result.RefreshExpiresAtUtc);
        return Ok(ToTokenResponse(result));
    }

    /// <summary>Làm mới cặp token từ refresh token trong cookie HttpOnly (rotate).</summary>
    /// <remarks>POST /api/v1/auth/refresh — refresh token đọc TỪ COOKIE (không nhận body).</remarks>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var rt = Request.Cookies[RefreshCookieName] ?? "";
        var cmd = new RefreshTokenCommand(rt, GetTenantId(), GetClientIp(), GetUserAgent());
        var result = await _mediator.Send(cmd, ct);
        if (result.Status != AuthStatus.Success)
        {
            ClearRefreshCookie(); // refresh hỏng/hết hạn/đã thu hồi → dọn cookie
            return MapFailure(result);
        }

        SetRefreshCookie(result.RefreshToken!, result.RefreshExpiresAtUtc);
        return Ok(ToTokenResponse(result));
    }

    /// <summary>Đăng xuất phiên hiện tại — thu hồi refresh token (đọc từ cookie) + xóa cookie.</summary>
    /// <remarks>POST /api/v1/auth/logout — refresh token đọc TỪ COOKIE.</remarks>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var rt = Request.Cookies[RefreshCookieName];
        await _mediator.Send(new LogoutCommand(rt, GetTenantId()), ct);
        ClearRefreshCookie();
        return NoContent();
    }

    /// <summary>SEC2-3: Đăng xuất MỌI thiết bị — thu hồi toàn bộ refresh token của user hiện tại + xóa cookie.</summary>
    /// <remarks>POST /api/v1/auth/logout-all — YÊU CẦU đăng nhập (lấy user từ token).</remarks>
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        await _mediator.Send(new LogoutAllCommand(userId.Value, GetTenantId()), ct);
        ClearRefreshCookie();
        return NoContent();
    }

    /// <summary>
    /// Yêu cầu đặt lại mật khẩu (STUB — chưa gửi email). Luôn trả 200 để chống dò tài khoản.
    /// </summary>
    /// <remarks>POST /api/v1/auth/forgot-password — Body: { "usernameOrEmail": "..." }</remarks>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest body, CancellationToken ct)
    {
        await _mediator.Send(new ForgotPasswordCommand(body.UsernameOrEmail ?? "", GetTenantId()), ct);
        return Ok(new { message = "Nếu tài khoản tồn tại, hướng dẫn đặt lại mật khẩu sẽ được gửi." });
    }

    /// <summary>Đặt lại mật khẩu bằng token (STUB — chưa hỗ trợ).</summary>
    /// <remarks>POST /api/v1/auth/reset-password — Body: { "token": "...", "newPassword": "..." }</remarks>
    [HttpPost("reset-password")]
    [AllowAnonymous]
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

    // ── Refresh cookie (SEC2-1) ────────────────────────────────────────────────

    /// <summary>Đặt refresh token vào cookie HttpOnly. Sự kiện theo sau: trình duyệt giữ cookie, JS không đọc được.</summary>
    private void SetRefreshCookie(string refreshToken, DateTime? expiresAtUtc)
        => Response.Cookies.Append(RefreshCookieName, refreshToken, BuildCookieOptions(expiresAtUtc));

    /// <summary>Xóa refresh cookie (đăng xuất). Options phải khớp lúc set để trình duyệt xóa đúng.</summary>
    private void ClearRefreshCookie()
        => Response.Cookies.Delete(RefreshCookieName, BuildCookieOptions(expiresAtUtc: null));

    /// <summary>
    /// Options cookie refresh: HttpOnly (chặn JS) + Secure (chỉ HTTPS) + SameSite=Lax (app &amp; API
    /// cùng site: subdomain hoặc khác port localhost) + Path giới hạn /api/v1/auth.
    /// </summary>
    private CookieOptions BuildCookieOptions(DateTime? expiresAtUtc) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = RefreshCookiePath,
        Domain = string.IsNullOrWhiteSpace(_refreshCookieDomain) ? null : _refreshCookieDomain,
        Expires = expiresAtUtc is { } e ? new DateTimeOffset(e, TimeSpan.Zero) : null
    };

    /// <summary>UserId từ claim (sub / NameIdentifier) — null nếu chưa xác thực.</summary>
    private long? GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return long.TryParse(raw, out var id) ? id : null;
    }

    /// <summary>Tạo payload token trả client. SEC2-1: KHÔNG trả refreshToken (đã đặt trong cookie HttpOnly).</summary>
    private static object ToTokenResponse(AuthResult r) => new
    {
        accessToken = r.AccessToken,
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
