// File    : HttpRequestContextAccessor.cs
// Module  : Context
// Layer   : Api
// Purpose : Cài IRequestContextAccessor từ HttpContext — claim JWT + HTTP header cho ContextParamResolver.

using System.Security.Claims;
using ICare247.Application.Interfaces;

namespace ICare247.Api.Context;

/// <summary>
/// Lấy giá trị request thô cho resolver token ngữ cảnh (spec 19): UserId/claim từ JWT, header từ request.
/// </summary>
public sealed class HttpRequestContextAccessor : IRequestContextAccessor
{
    private readonly IHttpContextAccessor _http;

    public HttpRequestContextAccessor(IHttpContextAccessor http) => _http = http;

    /// <inheritdoc />
    public long UserId
    {
        get
        {
            var user = _http.HttpContext?.User;
            var raw = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("sub")?.Value;
            return long.TryParse(raw, out var id) ? id : 0;
        }
    }

    /// <inheritdoc />
    public string? GetClaim(string name)
    {
        var user = _http.HttpContext?.User;
        if (user is null) return null;
        // 'sub' hay ánh xạ sang NameIdentifier → thử cả hai.
        return user.FindFirst(name)?.Value
            ?? (name == "sub" ? user.FindFirst(ClaimTypes.NameIdentifier)?.Value : null);
    }

    /// <inheritdoc />
    public string? GetHeader(string name)
    {
        var headers = _http.HttpContext?.Request.Headers;
        if (headers is null) return null;
        return headers.TryGetValue(name, out var v) ? v.ToString() : null;
    }
}
