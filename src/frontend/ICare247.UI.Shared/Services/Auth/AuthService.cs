// File    : AuthService.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Cài đặt IAuthService — gọi /api/v1/auth/*, lưu token, gắn header Bearer, đồng bộ
//           trạng thái xác thực với JwtAuthenticationStateProvider.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace ICare247.UI.Shared.Services.Auth;

/// <summary>
/// Dịch vụ xác thực thật (thay stub cũ). Dùng chung HttpClient của host (đã gắn X-Tenant-Id);
/// sau khi đăng nhập sẽ gắn thêm header Authorization: Bearer cho mọi request module.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly TokenStore _store;
    private readonly JwtAuthenticationStateProvider _authState;
    private readonly TokenRefresher _refresher;
    private readonly ICare247.UI.Shared.Services.I18n.LocalizationService _loc;

    public AuthService(HttpClient http, TokenStore store, JwtAuthenticationStateProvider authState,
        TokenRefresher refresher, ICare247.UI.Shared.Services.I18n.LocalizationService loc)
    {
        _http = http;
        _store = store;
        _authState = authState;
        _refresher = refresher;
        _loc = loc;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _store.EnsureLoadedAsync();   // dọn token cũ trong localStorage (di sản)
        // SEC2-2: access token chỉ ở RAM → khôi phục phiên qua silent refresh (cookie HttpOnly).
        // Thành công: TokenRefresher tự set access + notify. Thất bại: ẩn danh → MainLayout về /login.
        await _refresher.RefreshAsync(staleToken: null, ct);
    }

    /// <inheritdoc />
    public async Task<AuthLoginResult> LoginAsync(string username, string password, bool rememberMe,
        CancellationToken ct = default)
    {
        HttpResponseMessage resp;
        try
        {
            // SEC2-1: credentials Include để trình duyệt LƯU cookie refresh (HttpOnly) từ response.
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/login")
            {
                Content = JsonContent.Create(new { username, password, rememberMe })
            };
            req.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            resp = await _http.SendAsync(req, ct);
        }
        catch (Exception ex)
        {
            return new AuthLoginResult(false, _loc.L("auth.error.noserver", "Không kết nối được máy chủ: {0}", ex.Message));
        }

        if (resp.IsSuccessStatusCode)
        {
            var data = await resp.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
            if (data is null || string.IsNullOrEmpty(data.AccessToken))
                return new AuthLoginResult(false, _loc.L("auth.error.badresponse", "Phản hồi đăng nhập không hợp lệ."));

            // Refresh token nằm trong cookie HttpOnly (server set) — client chỉ giữ access token (RAM).
            // Bearer do RefreshTokenHandler tự đính từ TokenStore cho mọi request → không set DefaultRequestHeaders.
            await _store.SetAsync(data.AccessToken);
            _authState.NotifyAuthenticationChanged();
            return new AuthLoginResult(true);
        }

        return new AuthLoginResult(false, await ExtractErrorAsync(resp, ct));
    }

    /// <inheritdoc />
    public async Task LogoutAsync(CancellationToken ct = default)
    {
        try
        {
            // SEC2-1: refresh token nằm trong cookie HttpOnly → gửi credentials, không cần body.
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/logout");
            req.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            await _http.SendAsync(req, ct);
        }
        catch
        {
            // Đăng xuất phía server thất bại không chặn việc xóa phiên cục bộ.
        }

        await _store.ClearAsync();
        _authState.NotifyAuthenticationChanged();
    }

    /// <inheritdoc />
    public async Task RequestPasswordResetAsync(string usernameOrEmail, CancellationToken ct = default)
    {
        try
        {
            await _http.PostAsJsonAsync("api/v1/auth/forgot-password",
                new { usernameOrEmail }, ct);
        }
        catch
        {
            // Chống dò tài khoản: không lộ lỗi ra UI.
        }
    }

    /// <summary>Trích thông báo lỗi thân thiện từ ProblemDetails (hoặc fallback theo status).</summary>
    private async Task<string> ExtractErrorAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetailsLite>(cancellationToken: ct);
            if (!string.IsNullOrWhiteSpace(problem?.Detail)) return problem!.Detail!;
        }
        catch
        {
            // Body không phải ProblemDetails — dùng fallback bên dưới.
        }

        return resp.StatusCode == HttpStatusCode.Unauthorized
            ? _loc.L("auth.error.invalidcredentials", "Tên đăng nhập hoặc mật khẩu không đúng.")
            : _loc.L("auth.error.loginfailed", "Đăng nhập thất bại. Vui lòng thử lại.");
    }

    // ── DTO nội bộ ────────────────────────────────────────────────────────────

    private sealed record LoginResponse(
        [property: JsonPropertyName("accessToken")] string? AccessToken,
        [property: JsonPropertyName("expiresIn")] int ExpiresIn);

    private sealed record ProblemDetailsLite(
        [property: JsonPropertyName("detail")] string? Detail);
}
