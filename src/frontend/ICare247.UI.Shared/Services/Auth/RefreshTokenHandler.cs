// File    : RefreshTokenHandler.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : SEC2-2 (spec 20) — DelegatingHandler gắn Bearer (từ TokenStore) cho mọi request;
//           khi gặp 401 (trừ endpoint /auth/*) → silent refresh qua TokenRefresher → retry 1 lần.

using System.Net;
using System.Net.Http.Headers;

namespace ICare247.UI.Shared.Services.Auth;

/// <summary>
/// Tự đính access token + xử lý 401 trong suốt. Nguồn access token = <see cref="TokenStore"/> (RAM),
/// nên sau khi refresh cập nhật store thì request kế tiếp tự có token mới. Bỏ qua refresh cho chính
/// các endpoint /api/v1/auth/* (login/refresh/logout) để tránh vòng lặp.
/// </summary>
public sealed class RefreshTokenHandler : DelegatingHandler
{
    private const string AuthPathFragment = "/api/v1/auth/";

    private readonly TokenStore _store;
    private readonly TokenRefresher _refresher;

    public RefreshTokenHandler(TokenStore store, TokenRefresher refresher)
    {
        _store = store;
        _refresher = refresher;
    }

    /// <summary>Gắn Bearer; nếu 401 (không phải endpoint auth) thì refresh + retry 1 lần.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var isAuthEndpoint = request.RequestUri?.AbsolutePath.Contains(
            AuthPathFragment, StringComparison.OrdinalIgnoreCase) ?? false;

        var token = _store.AccessToken;
        if (!string.IsNullOrEmpty(token) && request.Headers.Authorization is null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await base.SendAsync(request, ct);

        // Chỉ thử refresh khi: 401 + không phải endpoint auth + đang có token (đã đăng nhập).
        if (resp.StatusCode != HttpStatusCode.Unauthorized || isAuthEndpoint || string.IsNullOrEmpty(token))
            return resp;

        var refreshed = await _refresher.RefreshAsync(token, ct);
        if (!refreshed) return resp; // refresh thất bại → trả 401 gốc (sẽ điều hướng login)

        // Refresh OK → dựng lại request với token mới, gửi lại 1 lần.
        HttpRequestMessage retry;
        try
        {
            retry = await CloneAsync(request);
        }
        catch
        {
            return resp; // body đã tiêu thụ, không clone được → trả 401 gốc
        }

        resp.Dispose();
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _store.AccessToken);
        return await base.SendAsync(retry, ct);
    }

    /// <summary>Sao chép request (method/uri/headers/content) để gửi lại sau khi refresh.</summary>
    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);

        if (req.Content is not null)
        {
            var bytes = await req.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var h in req.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        foreach (var h in req.Headers)
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);

        // Giữ các option (gồm cờ browser credentials nếu có) để hành vi gửi lại nhất quán.
        foreach (var opt in req.Options)
            ((IDictionary<string, object?>)clone.Options)[opt.Key] = opt.Value;

        return clone;
    }
}
