// File    : DependencyInjection.cs
// Module  : Infrastructure
// Layer   : Infrastructure
// Purpose : Đăng ký tất cả services của Infrastructure layer: DB, Cache, Repositories, Telemetry.

using ICare247.Application.Interfaces;
using ICare247.Infrastructure.Caching;
using ICare247.Infrastructure.Data;
using ICare247.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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
        // ── Database — 2 connection string riêng biệt ────────────────────────
        //
        //   Config DB ("Config"): ICare247_Config — metadata form engine
        //     Ui_Form, Ui_Field, Sys_*, Val_*, Evt_*, Gram_*
        //     → IDbConnectionFactory
        //
        //   Data DB  ("Data"):   DB nghiệp vụ thực tế — bệnh nhân, hồ sơ,...
        //     → IDataDbConnectionFactory
        //
        //   Cả 2 được cấu hình trong:
        //     %APPDATA%\ICare247\Api\appsettings.local.json
        //     mục ConnectionStrings: { "Config": "...", "Data": "..." }
        //
        // ── Config DB (IDbConnectionFactory) ─────────────────────────────────
        var configConn = configuration.GetConnectionString("Config")
                      ?? configuration.GetConnectionString("Default"); // backward compat
        if (!string.IsNullOrWhiteSpace(configConn))
        {
            services.AddSingleton<IDbConnectionFactory>(
                new SqlConnectionFactory(configConn));
        }

        // ── Data DB (IDataDbConnectionFactory) ───────────────────────────────
        // Nếu chưa cấu hình Data DB → fallback về Config DB (môi trường dev đơn giản).
        var dataConn = configuration.GetConnectionString("Data");
        if (!string.IsNullOrWhiteSpace(dataConn))
        {
            services.AddSingleton<IDataDbConnectionFactory>(
                new SqlConnectionFactory(dataConn));
        }
        else if (!string.IsNullOrWhiteSpace(configConn))
        {
            // Dev fallback: chưa có Data DB riêng → dùng chung Config DB
            services.AddSingleton<IDataDbConnectionFactory>(
                new SqlConnectionFactory(configConn));
        }

        // ── Cache ─────────────────────────────────────────────────────────────
        services.AddMemoryCache();

        // Redis — chỉ đăng ký khi có connection string.
        // Nếu không có Redis, fallback về DistributedMemoryCache để IDistributedCache
        // luôn được resolve (HybridCacheService constructor cần nó dù là nullable).
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddStackExchangeRedisCache(opts =>
                opts.Configuration = redisConn);
        }
        else
        {
            // NOTE: DistributedMemoryCache chỉ là placeholder local — không share giữa instances.
            // HybridCacheService sẽ nhận instance này nhưng thực tế chỉ dùng L1 MemoryCache.
            services.AddDistributedMemoryCache();
        }

        // HybridCacheService — L1 Memory + L2 Redis (optional)
        services.AddSingleton<ICacheService, HybridCacheService>();

        // ── Repositories ─────────────────────────────────────────────────────
        services.AddScoped<IFormRepository, FormRepository>();
        services.AddScoped<IFieldRepository, FieldRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IRuleRepository, RuleRepository>();
        services.AddScoped<IDependencyRepository, DependencyRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ILookupRepository, LookupRepository>();

        // ── OpenTelemetry ─────────────────────────────────────────────────────
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: "ICare247.Api",
                    serviceVersion: "1.0.0"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("ICare247.*"));

        return services;
    }
}
