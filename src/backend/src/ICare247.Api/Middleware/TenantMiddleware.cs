// File    : TenantMiddleware.cs
// Module  : Multi-Tenant
// Layer   : Api
// Purpose : Phân giải tenant cho mỗi request → set Tenant_Id + cặp connection string vào
//           TenantContext. Ưu tiên SUBDOMAIN (khi đã cấu hình Catalog), fallback header
//           X-Tenant-Id (dev / 1 tenant). Xem ADR-018.

using ICare247.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ICare247.Api.Middleware;

/// <summary>
/// Middleware phân giải tenant: suy từ subdomain của Host (đa tenant qua Catalog) hoặc
/// header <c>X-Tenant-Id</c> (fallback). Set <see cref="TenantContext"/> (Tenant_Id +
/// connection string Config/Data) cho request scope. Trả 400 nếu không xác định được tenant.
/// </summary>
public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;
    private readonly ITenantConnectionResolver _resolver;
    private readonly string? _baseDomain;

    /// <summary>JSON options cho error response.</summary>
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Danh sách path prefix không cần tenant (health check, OpenAPI docs, Scalar UI).
    /// </summary>
    private static readonly string[] ExcludedPaths =
    [
        "/health",
        "/openapi",
        "/scalar"
    ];

    public TenantMiddleware(
        RequestDelegate next,
        ILogger<TenantMiddleware> logger,
        ITenantConnectionResolver resolver,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _resolver = resolver;
        _baseDomain = configuration["Catalog:BaseDomain"]; // vd "icare247.vn"
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (IsExcluded(path))
        {
            await _next(context);
            return;
        }

        TenantConnections? tenant;
        try
        {
            tenant = await ResolveTenantAsync(context);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Phân giải tenant thất bại — Path={Path}", path);
            await WriteProblemAsync(context, "Tenant không hợp lệ", ex.Message);
            return;
        }

        if (tenant is null)
        {
            _logger.LogWarning("Không xác định được tenant — Path={Path}, Host={Host}",
                path, context.Request.Host.Value);
            await WriteProblemAsync(context,
                "Thiếu thông tin tenant",
                "Không xác định được tenant từ subdomain hoặc header X-Tenant-Id.");
            return;
        }

        // Set vào scoped context — factory sẽ đọc connection string từ đây.
        if (context.RequestServices.GetRequiredService<ITenantContext>() is TenantContext ctx)
        {
            ctx.TenantId = tenant.TenantId;
            ctx.ConfigConnectionString = tenant.ConfigConnectionString;
            ctx.DataConnectionString = tenant.DataConnectionString;
        }

        _logger.LogDebug("Tenant resolved — TenantId={TenantId}, Path={Path}", tenant.TenantId, path);
        await _next(context);
    }

    /// <summary>
    /// Phân giải tenant: subdomain trước (khi Catalog bật), rồi header X-Tenant-Id.
    /// </summary>
    /// <param name="context">HttpContext của request.</param>
    /// <returns>Cặp connection của tenant, hoặc null nếu không xác định.</returns>
    private async Task<TenantConnections?> ResolveTenantAsync(HttpContext context)
    {
        // ① Subdomain (chỉ khi đã cấu hình Catalog DB)
        if (_resolver.IsCatalogConfigured)
        {
            var subdomain = ExtractSubdomain(context.Request.Host.Host, _baseDomain);
            if (subdomain is not null)
            {
                var bySub = await _resolver.ResolveBySubdomainAsync(subdomain, context.RequestAborted);
                if (bySub is not null) return bySub;
                _logger.LogWarning("Subdomain '{Sub}' không khớp tenant nào trong catalog.", subdomain);
            }
        }

        // ② Header X-Tenant-Id (fallback: dev / 1 tenant / công cụ test)
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValues)
            && int.TryParse(headerValues.FirstOrDefault(), out var tenantId)
            && tenantId > 0)
        {
            return await _resolver.ResolveByIdAsync(tenantId, context.RequestAborted);
        }

        return null;
    }

    /// <summary>
    /// Tách nhãn subdomain từ Host theo base domain. <c>congtyA.icare247.vn</c> + base
    /// <c>icare247.vn</c> → <c>congtyA</c>. Bỏ qua 'www'. Trả null nếu không khớp.
    /// </summary>
    /// <param name="host">Host (không gồm port).</param>
    /// <param name="baseDomain">Domain gốc cấu hình (Catalog:BaseDomain).</param>
    /// <returns>Nhãn subdomain hoặc null.</returns>
    private static string? ExtractSubdomain(string host, string? baseDomain)
    {
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(baseDomain))
            return null;
        if (host.Equals(baseDomain, StringComparison.OrdinalIgnoreCase))
            return null;
        if (!host.EndsWith("." + baseDomain, StringComparison.OrdinalIgnoreCase))
            return null;

        var label = host[..^(baseDomain.Length + 1)].Split('.', 2)[0];
        if (string.IsNullOrWhiteSpace(label)
            || label.Equals("www", StringComparison.OrdinalIgnoreCase))
            return null;
        return label;
    }

    /// <summary>Ghi response ProblemDetails 400.</summary>
    /// <param name="context">HttpContext.</param>
    /// <param name="title">Tiêu đề lỗi.</param>
    /// <param name="detail">Mô tả lỗi.</param>
    private static async Task WriteProblemAsync(HttpContext context, string title, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails
        {
            Type = "https://icare247.vn/errors/missing-tenant",
            Title = title,
            Status = StatusCodes.Status400BadRequest,
            Detail = detail
        }, JsonOpts));
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
