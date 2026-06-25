// File    : TokenRefresher.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : SEC2-2 (spec 20) — gọi /auth/refresh (cookie HttpOnly) để lấy access token mới vào RAM.
//           Dùng HttpClient RIÊNG (không qua RefreshTokenHandler) → tránh đệ quy 401. Single-flight.

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace ICare247.UI.Shared.Services.Auth;

/// <summary>
/// Làm mới access token từ refresh token trong cookie HttpOnly. Dùng cho: (a) silent refresh lúc
/// khởi động app, (b) RefreshTokenHandler khi gặp 401. Single-flight: nhiều request 401 cùng lúc
/// chỉ refresh 1 lần; request nào thấy token đã đổi thì dùng luôn token mới.
/// </summary>
public sealed class TokenRefresher
{
    private readonly HttpClient _http;   // client RIÊNG, KHÔNG gắn RefreshTokenHandler
    private readonly TokenStore _store;
    private readonly JwtAuthenticationStateProvider _authState;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public TokenRefresher(HttpClient http, TokenStore store, JwtAuthenticationStateProvider authState)
    {
        _http = http;
        _store = store;
        _authState = authState;
    }

    /// <summary>
    /// Làm mới access token. <paramref name="staleToken"/> = token mà caller vừa thấy bị 401
    /// (null khi gọi lúc boot). Nếu token hiện tại đã KHÁC staleToken → request khác đã refresh xong,
    /// trả true ngay. Sự kiện theo sau: thành công → TokenStore có access mới + UI cập nhật auth.
    /// </summary>
    public async Task<bool> RefreshAsync(string? staleToken, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            // Dedup: request khác vừa refresh xong (token đã đổi) → khỏi gọi lại.
            if (!string.IsNullOrEmpty(_store.AccessToken) && _store.AccessToken != staleToken)
                return true;

            return await DoRefreshAsync(ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Gọi POST /auth/refresh với cookie (credentials Include). Trả true nếu lấy được access mới.</summary>
    private async Task<bool> DoRefreshAsync(CancellationToken ct)
    {
        HttpResponseMessage resp;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/refresh");
            req.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            resp = await _http.SendAsync(req, ct);
        }
        catch
        {
            return false; // mất kết nối → coi như chưa đăng nhập
        }

        if (!resp.IsSuccessStatusCode) return false;

        var data = await resp.Content.ReadFromJsonAsync<RefreshResponse>(cancellationToken: ct);
        if (data is null || string.IsNullOrEmpty(data.AccessToken)) return false;

        await _store.SetAsync(data.AccessToken);
        _authState.NotifyAuthenticationChanged();
        return true;
    }

    private sealed record RefreshResponse(
        [property: JsonPropertyName("accessToken")] string? AccessToken,
        [property: JsonPropertyName("expiresIn")] int ExpiresIn);
}
