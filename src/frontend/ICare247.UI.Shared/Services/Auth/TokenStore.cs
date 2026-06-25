// File    : TokenStore.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Giữ access token CHỈ TRONG RAM (SEC2-2). Không persist localStorage → XSS khó lấy hơn,
//           đóng trình duyệt là mất phiên RAM (khôi phục qua silent refresh + cookie HttpOnly).
//           Refresh token do server quản qua cookie HttpOnly — client không giữ.

using Microsoft.JSInterop;

namespace ICare247.UI.Shared.Services.Auth;

/// <summary>
/// Kho access token RAM-only. Khôi phục phiên sau reload do <see cref="TokenRefresher"/> đảm nhiệm
/// (gọi /auth/refresh bằng cookie). Scoped — 1 instance/phiên người dùng.
/// </summary>
public sealed class TokenStore
{
    // Key cũ từng dùng localStorage (Step 1 + trước SEC2) — chủ động xóa để không còn token sót.
    private const string LegacyAccessKey = "ic247.accessToken";
    private const string LegacyRefreshKey = "ic247.refreshToken";

    private readonly IJSRuntime _js;
    private bool _purged;

    public TokenStore(IJSRuntime js) => _js = js;

    /// <summary>Access token (JWT) hiện tại — null nếu chưa đăng nhập / chưa silent-refresh.</summary>
    public string? AccessToken { get; private set; }

    /// <summary>Đã có access token trong bộ nhớ.</summary>
    public bool HasToken => !string.IsNullOrEmpty(AccessToken);

    /// <summary>
    /// Dọn token cũ còn sót trong localStorage (1 lần). Access token nay CHỈ ở RAM nên không nạp gì.
    /// </summary>
    public async Task EnsureLoadedAsync()
    {
        if (_purged) return;
        _purged = true;
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", LegacyAccessKey);
            await _js.InvokeVoidAsync("localStorage.removeItem", LegacyRefreshKey);
        }
        catch
        {
            // JS chưa sẵn sàng (prerender) — bỏ qua, lần sau gọi lại.
            _purged = false;
        }
    }

    /// <summary>Đặt access token mới (RAM). Sự kiện theo sau: HasToken = true.</summary>
    public Task SetAsync(string accessToken)
    {
        AccessToken = accessToken;
        return Task.CompletedTask;
    }

    /// <summary>Xóa access token (đăng xuất). Sự kiện theo sau: HasToken = false.</summary>
    public Task ClearAsync()
    {
        AccessToken = null;
        return Task.CompletedTask;
    }
}
