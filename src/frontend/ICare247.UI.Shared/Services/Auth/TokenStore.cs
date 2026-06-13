// File    : TokenStore.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Lưu access/refresh token trong bộ nhớ + localStorage (giữ phiên qua reload trang).

using Microsoft.JSInterop;

namespace ICare247.UI.Shared.Services.Auth;

/// <summary>
/// Kho token phía client. Giữ token trong RAM để truy cập đồng bộ, đồng thời lưu localStorage
/// để phiên đăng nhập sống sót khi tải lại trang. Scoped — 1 instance/phiên người dùng.
/// </summary>
public sealed class TokenStore
{
    private const string AccessKey = "ic247.accessToken";
    private const string RefreshKey = "ic247.refreshToken";

    private readonly IJSRuntime _js;
    private bool _loaded;

    public TokenStore(IJSRuntime js) => _js = js;

    /// <summary>Access token (JWT) hiện tại — null nếu chưa đăng nhập.</summary>
    public string? AccessToken { get; private set; }

    /// <summary>Refresh token hiện tại — null nếu chưa đăng nhập.</summary>
    public string? RefreshToken { get; private set; }

    /// <summary>Đã có access token trong bộ nhớ.</summary>
    public bool HasToken => !string.IsNullOrEmpty(AccessToken);

    /// <summary>
    /// Nạp token từ localStorage (1 lần). Sự kiện theo sau: AccessToken/RefreshToken có giá trị
    /// nếu phiên trước còn lưu.
    /// </summary>
    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        AccessToken = await _js.InvokeAsync<string?>("localStorage.getItem", AccessKey);
        RefreshToken = await _js.InvokeAsync<string?>("localStorage.getItem", RefreshKey);
        _loaded = true;
    }

    /// <summary>Lưu cặp token mới (RAM + localStorage). Sự kiện theo sau: phiên được ghi nhớ.</summary>
    public async Task SetAsync(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        _loaded = true;
        await _js.InvokeVoidAsync("localStorage.setItem", AccessKey, accessToken);
        await _js.InvokeVoidAsync("localStorage.setItem", RefreshKey, refreshToken);
    }

    /// <summary>Xóa token (đăng xuất). Sự kiện theo sau: HasToken = false.</summary>
    public async Task ClearAsync()
    {
        AccessToken = null;
        RefreshToken = null;
        _loaded = true;
        await _js.InvokeVoidAsync("localStorage.removeItem", AccessKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", RefreshKey);
    }
}
