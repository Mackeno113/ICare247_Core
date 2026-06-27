// File    : ActiveScopeHandler.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : VFILTER-ACTIVE (ADR-030) — DelegatingHandler gắn header X-Active-CongTy cho mọi request
//           theo công ty đang chọn (AppState). Đặt NGOÀI RefreshTokenHandler để header có sẵn cả khi
//           retry sau refresh (CloneAsync copy nguyên headers). null/0 → bỏ header (server dùng default 0).

using System.Globalization;
using ICare247.UI.Shared.State;

namespace ICare247.UI.Shared.Services.Auth;

/// <summary>Đính X-Active-CongTy = công ty đang chọn (token @CongTyID_Active). Server validate lại theo quyền.</summary>
public sealed class ActiveScopeHandler : DelegatingHandler
{
    private const string HeaderName = "X-Active-CongTy";

    private readonly AppState _appState;

    public ActiveScopeHandler(AppState appState) => _appState = appState;

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var id = _appState.ActiveCompanyId;
        if (id is > 0 && !request.Headers.Contains(HeaderName))
            request.Headers.TryAddWithoutValidation(
                HeaderName, id.Value.ToString(CultureInfo.InvariantCulture));

        return base.SendAsync(request, ct);
    }
}
