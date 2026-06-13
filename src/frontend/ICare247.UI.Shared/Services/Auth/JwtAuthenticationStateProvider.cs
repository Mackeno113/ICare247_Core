// File    : JwtAuthenticationStateProvider.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : AuthenticationStateProvider dựng ClaimsPrincipal từ access token trong TokenStore.

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ICare247.UI.Shared.Services.Auth;

/// <summary>
/// Cung cấp trạng thái xác thực cho Blazor (AuthorizeView/&lt;CascadingAuthenticationState&gt;).
/// Đọc claim từ access token đã lưu; token hết hạn/không có → người dùng ẩn danh.
/// </summary>
public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string AuthType = "jwt";
    private const string NameClaim = "unique_name";
    private const string RoleClaim = "role";

    private readonly TokenStore _store;

    public JwtAuthenticationStateProvider(TokenStore store) => _store = store;

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await _store.EnsureLoadedAsync();
        return new AuthenticationState(BuildPrincipal(_store.AccessToken));
    }

    /// <summary>Thông báo trạng thái xác thực thay đổi (sau login/logout) để UI render lại.</summary>
    public void NotifyAuthenticationChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    /// <summary>Dựng ClaimsPrincipal từ token; ẩn danh nếu token rỗng/hết hạn.</summary>
    private static ClaimsPrincipal BuildPrincipal(string? token)
    {
        var claims = JwtParser.ParseClaims(token);
        if (claims.Count == 0 || JwtParser.IsExpired(claims))
            return new ClaimsPrincipal(new ClaimsIdentity());

        var identity = new ClaimsIdentity(claims, AuthType, NameClaim, RoleClaim);
        return new ClaimsPrincipal(identity);
    }
}
