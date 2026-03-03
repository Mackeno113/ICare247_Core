// File    : DependencyInjection.cs
// Module  : Infrastructure
// Layer   : Infrastructure
// Purpose : Đăng ký tất cả services của Infrastructure layer: DB, Cache, Repositories.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ICare247.Infrastructure;

/// <summary>
/// Extension methods đăng ký Infrastructure layer vào IServiceCollection.
/// Gọi từ Program.cs: builder.Services.AddInfrastructure(configuration)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ─────────────────────────────────────────────────────────
        // TODO(phase1): Đăng ký IDbConnectionFactory → SqlConnectionFactory
        // services.AddSingleton<IDbConnectionFactory>(
        //     new SqlConnectionFactory(configuration.GetConnectionString("Default")!));

        // ── Cache ─────────────────────────────────────────────────────────────
        services.AddMemoryCache();
        // TODO(phase1): Đăng ký Redis nếu config có ConnectionStrings:Redis
        // services.AddStackExchangeRedisCache(opts =>
        //     opts.Configuration = configuration.GetConnectionString("Redis"));
        // TODO(phase1): Đăng ký IHybridCacheService → HybridCacheService

        // ── Repositories ─────────────────────────────────────────────────────
        // TODO(phase1): Đăng ký IFormRepository → FormRepository
        // TODO(phase1): Đăng ký IFieldRepository → FieldRepository

        // ── Logging / Telemetry ───────────────────────────────────────────────
        // TODO(phase5): Đăng ký OpenTelemetry TracerProvider

        return services;
    }
}
