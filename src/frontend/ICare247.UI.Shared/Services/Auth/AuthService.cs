// File    : AuthService.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Cài đặt IAuthService — gọi /api/v1/auth/*, lưu token, gắn header Bearer, đồng bộ
//           trạng thái xác thực với JwtAuthenticationStateProvider.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

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
    private readonly ICare247.UI.Shared.Services.I18n.LocalizationService _loc;

    public AuthService(HttpClient http, TokenStore store, JwtAuthenticationStateProvider authState,
        ICare247.UI.Shared.Services.I18n.LocalizationService loc)
    {
        _http = http;
        _store = store;
        _authState = authState;
        _loc = loc;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _store.EnsureLoadedAsync();
        ApplyAuthHeader(_store.AccessToken);
    }

    /// <inheritdoc />
    public async Task<AuthLoginResult> LoginAsync(string username, string password, bool rememberMe,
        CancellationToken ct = default)
    {
        HttpResponseMessage resp;
        try
        {
            resp = await _http.PostAsJsonAsync("api/v1/auth/login",
                new { username, password, rememberMe }, ct);
        }
        catch (Exception ex)
        {
            return new AuthLoginResult(false, _loc.L("auth.error.noserver", "Không kết nối được máy chủ: {0}", ex.Message));
        }

        if (resp.IsSuccessStatusCode)
        {
            var data = await resp.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
            if (data is null || string.IsNullOrEmpty(data.AccessToken) || string.IsNullOrEmpty(data.RefreshToken))
                return new AuthLoginResult(false, _loc.L("auth.error.badresponse", "Phản hồi đăng nhập không hợp lệ."));

            await _store.SetAsync(data.AccessToken, data.RefreshToken);
            ApplyAuthHeader(data.AccessToken);
            _authState.NotifyAuthenticationChanged();
            return new AuthLoginResult(true);
        }

        return new AuthLoginResult(false, await ExtractErrorAsync(resp, ct));
    }

    /// <inheritdoc />
    public async Task LogoutAsync(CancellationToken ct = default)
    {
        await _store.EnsureLoadedAsync();
        var refresh = _store.RefreshToken;
        if (!string.IsNullOrEmpty(refresh))
        {
            try
            {
                await _http.PostAsJsonAsync("api/v1/auth/logout", new { refreshToken = refresh }, ct);
            }
            catch
            {
                // Đăng xuất phía server thất bại không chặn việc xóa phiên cục bộ.
            }
        }

        await _store.ClearAsync();
        ApplyAuthHeader(null);
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

    /// <summary>Gắn/gỡ header Authorization trên HttpClient dùng chung.</summary>
    private void ApplyAuthHeader(string? accessToken)
        => _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(accessToken) ? null : new AuthenticationHeaderValue("Bearer", accessToken);

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
        [property: JsonPropertyName("refreshToken")] string? RefreshToken,
        [property: JsonPropertyName("expiresIn")] int ExpiresIn);

    private sealed record ProblemDetailsLite(
        [property: JsonPropertyName("detail")] string? Detail);
}
