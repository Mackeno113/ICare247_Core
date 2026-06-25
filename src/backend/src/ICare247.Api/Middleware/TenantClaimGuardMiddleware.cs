// File    : TenantClaimGuardMiddleware.cs
// Module  : Multi-Tenant
// Layer   : Api
// Purpose : SEC1-3 (spec 20) — chống nhảy tenant. Với request ĐÃ xác thực, bắt buộc claim "tenant"
//           trong JWT phải khớp tenant đã phân giải (TenantContext, từ subdomain/header X-Tenant-Id).
//           Lệch → 403. Đặt SAU UseAuthentication (cần User claim) và SAU TenantMiddleware.

using ICare247.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ICare247.Api.Middleware;

/// <summary>
/// Bảo vệ cách ly tenant: token của tenant A KHÔNG được dùng để thao tác trên tenant B bằng cách
/// giả mạo header <c>X-Tenant-Id</c> hoặc subdomain. So claim <c>tenant</c> (do backend ký) với
/// <see cref="ITenantContext.TenantId"/> đã phân giải. Request ẩn danh (login/i18n) bỏ qua kiểm.
/// </summary>
public sealed class TenantClaimGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantClaimGuardMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TenantClaimGuardMiddleware(RequestDelegate next, ILogger<TenantClaimGuardMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// So khớp tenant-token vs tenant-request. Sự kiện theo sau: 403 nếu lệch hoặc token không có
    /// claim tenant hợp lệ; ngược lại đi tiếp pipeline.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // Chỉ kiểm khi đã xác thực — endpoint ẩn danh (login, i18n) không có token để so.
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var claim = context.User.FindFirst("tenant")?.Value;
            if (!int.TryParse(claim, out var tokenTenant) || tokenTenant <= 0)
            {
                _logger.LogWarning("Token thiếu claim 'tenant' hợp lệ — Path={Path}", context.Request.Path.Value);
                await WriteForbiddenAsync(context,
                    "Token không hợp lệ",
                    "Token không chứa thông tin tenant hợp lệ. Vui lòng đăng nhập lại.");
                return;
            }

            if (tokenTenant != tenantContext.TenantId)
            {
                _logger.LogWarning(
                    "Chặn nhảy tenant — tokenTenant={TokenTenant}, requestTenant={RequestTenant}, Path={Path}",
                    tokenTenant, tenantContext.TenantId, context.Request.Path.Value);
                await WriteForbiddenAsync(context,
                    "Sai tenant",
                    "Phiên đăng nhập không thuộc tenant của yêu cầu này.");
                return;
            }
        }

        await _next(context);
    }

    /// <summary>Ghi response ProblemDetails 403.</summary>
    private static async Task WriteForbiddenAsync(HttpContext context, string title, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails
        {
            Type = "https://icare247.vn/errors/tenant-mismatch",
            Title = title,
            Status = StatusCodes.Status403Forbidden,
            Detail = detail
        }, JsonOpts));
    }
}
