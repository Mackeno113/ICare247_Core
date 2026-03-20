// File    : TenantMiddleware.cs
// Module  : Multi-Tenant
// Layer   : Api
// Purpose : Extract X-Tenant-Id header → set ITenantContext cho request scope.

using ICare247.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ICare247.Api.Middleware;

/// <summary>
/// Middleware extract header <c>X-Tenant-Id</c> và set vào <see cref="ITenantContext"/>.
/// Trả 400 nếu header thiếu hoặc không hợp lệ (trừ endpoints được exclude).
/// </summary>
public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    /// <summary>JSON options cho error response.</summary>
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Danh sách path prefix không cần X-Tenant-Id.
    /// Ví dụ: health check, OpenAPI docs, Scalar UI.
    /// </summary>
    private static readonly string[] ExcludedPaths =
    [
        "/health",
        "/openapi",
        "/scalar"
    ];

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Bỏ qua endpoints không cần tenant
        if (IsExcluded(path))
        {
            await _next(context);
            return;
        }

        // Extract X-Tenant-Id header
        if (!context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValues)
            || !int.TryParse(headerValues.FirstOrDefault(), out var tenantId)
            || tenantId <= 0)
        {
            _logger.LogWarning(
                "Request thiếu X-Tenant-Id header — Path={Path}, Method={Method}",
                path, context.Request.Method);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails
            {
                Type = "https://icare247.vn/errors/missing-tenant",
                Title = "Thiếu thông tin tenant",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Header X-Tenant-Id là bắt buộc và phải là số nguyên dương."
            }, JsonOpts));
            return;
        }

        // Set tenant vào scoped context
        var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
        if (tenantContext is TenantContext mutable)
        {
            mutable.TenantId = tenantId;
        }

        _logger.LogDebug("Tenant resolved — TenantId={TenantId}, Path={Path}", tenantId, path);

        await _next(context);
    }

    /// <summary>Kiểm tra path có nằm trong danh sách exclude không.</summary>
    private static bool IsExcluded(string path)
    {
        foreach (var excluded in ExcludedPaths)
        {
            if (path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
