// File    : DependencyInjection.cs
// Module  : Infrastructure
// Layer   : Infrastructure
// Purpose : Đăng ký tất cả services của Infrastructure layer: DB, Cache, Repositories.

using ICare247.Application.Interfaces;
using ICare247.Infrastructure.Caching;
using ICare247.Infrastructure.Data;
using ICare247.Infrastructure.Repositories;
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
        var defaultConn = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(defaultConn))
        {
            services.AddSingleton<IDbConnectionFactory>(
                new SqlConnectionFactory(defaultConn));
        }

        // ── Cache ─────────────────────────────────────────────────────────────
        services.AddMemoryCache();

        // Redis — chỉ đăng ký khi có connection string
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddStackExchangeRedisCache(opts =>
                opts.Configuration = redisConn);
        }

        // HybridCacheService — L1 Memory + L2 Redis (optional)
        services.AddSingleton<ICacheService, HybridCacheService>();

        // ── Repositories ─────────────────────────────────────────────────────
        services.AddScoped<IFormRepository, FormRepository>();
        services.AddScoped<IFieldRepository, FieldRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IRuleRepository, RuleRepository>();
        services.AddScoped<IDependencyRepository, DependencyRepository>();

        // ── Logging / Telemetry ───────────────────────────────────────────────
        // TODO(phase5): Đăng ký OpenTelemetry TracerProvider

        return services;
    }
}
