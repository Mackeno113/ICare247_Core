// File    : DependencyInjection.cs
// Module  : Infrastructure
// Layer   : Infrastructure
// Purpose : Đăng ký tất cả services của Infrastructure layer: DB, Cache, Repositories, Telemetry.

using ICare247.Application.Interfaces;
using ICare247.Infrastructure.Audit;
using ICare247.Infrastructure.Auth;
using ICare247.Infrastructure.Caching;
using ICare247.Infrastructure.Data;
using ICare247.Infrastructure.MultiTenancy;
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
        // ── Database — factory TENANT-AWARE (scoped) — ADR-018 ───────────────
        //
        //   Connection string KHÔNG còn cố định lúc đăng ký. Mỗi request:
        //   TenantMiddleware phân giải tenant (subdomain/header) → ITenantConnectionResolver
        //   → set ConfigConnectionString/DataConnectionString vào TenantContext (scoped).
        //   Factory đọc từ TenantContext để mở đúng cặp DB của tenant.
        //
        //   Chế độ fallback (chưa cấu hình ConnectionStrings:Catalog): resolver trả về
        //   Config/Data cố định trong config → hành vi như cũ (1 tenant / dev).
        //
        //   Config DB → IDbConnectionFactory ; Data DB → IDataDbConnectionFactory.
        services.AddScoped<IDbConnectionFactory, ConfigDbConnectionFactory>();
        services.AddScoped<IDataDbConnectionFactory, DataDbConnectionFactory>();

        // ── Multi-tenant connection resolver (ADR-018) ───────────────────────
        // Phân giải tenant → cặp connection string từ Catalog DB (cache in-memory).
        // Chưa cấu hình ConnectionStrings:Catalog → fallback dùng Config/Data cố định ở trên
        // (chế độ 1 tenant / dev — KHÔNG đổi hành vi hiện tại). Bước flip factory scoped làm sau.
        services.AddSingleton<ITenantConnectionResolver, TenantConnectionResolver>();

        // ── Cache ─────────────────────────────────────────────────────────────
        // Cờ bật/tắt cache toàn cục (Cache:Enabled). Mặc định true; đặt false (dev) để test cấu hình.
        services.Configure<Caching.CacheSettings>(configuration.GetSection("Cache"));

        services.AddMemoryCache();

        // Redis — chỉ đăng ký khi có connection string.
        // Nếu không có Redis, fallback về DistributedMemoryCache để IDistributedCache
        // luôn được resolve (HybridCacheService constructor cần nó dù là nullable).
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddStackExchangeRedisCache(opts =>
                opts.Configuration = redisConn);

            // Multiplexer dùng cho Redis Stream (audit log). Chỉ đăng ký khi có Redis →
            // AuditBackgroundService tự fallback ghi thẳng DB khi service này vắng mặt.
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
                _ => StackExchange.Redis.ConnectionMultiplexer.Connect(redisConn));
        }
        else
        {
            // NOTE: DistributedMemoryCache chỉ là placeholder local — không share giữa instances.
            // HybridCacheService sẽ nhận instance này nhưng thực tế chỉ dùng L1 MemoryCache.
            services.AddDistributedMemoryCache();
        }

        // HybridCacheService — L1 Memory + L2 Redis (optional)
        services.AddSingleton<ICacheService, HybridCacheService>();

        // Version-stamp cache theo tenant (singleton — chia sẻ mọi request). Bump = vô hiệu cache config tenant.
        services.AddSingleton<ICacheVersion, Services.CacheVersion>();

        // ── Repositories ─────────────────────────────────────────────────────
        services.AddScoped<IFormRepository, FormRepository>();
        services.AddScoped<IFieldRepository, FieldRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IRuleRepository, RuleRepository>();
        services.AddScoped<IDependencyRepository, DependencyRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<IDynamicLookupRepository, DynamicLookupRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<IMasterDataRepository, MasterDataRepository>();
        // Catalog tồn tại hook store (cache-aside) — save path đọc cache thay vì query OBJECT_ID (ADR-029).
        services.AddScoped<IHookStoreCatalog, Services.HookStoreCatalog>();
        services.AddScoped<IUserGridLayoutRepository, UserGridLayoutRepository>();
        services.AddScoped<IReferenceCheckService, ReferenceCheckService>();
        services.AddScoped<IViewRepository, ViewRepository>();
        // Token ngữ cảnh (Sys_Context_Param) — registry + resolver (Claim/Header/ActiveScope). Spec 19, ADR-030.
        services.AddScoped<IContextParamRepository, ContextParamRepository>();
        services.AddScoped<IContextParamResolver, ContextParamResolver>();

        // ── Config Sync (F1 — đồng bộ config master→tenant, spec 16) ─────────
        // Scoped: dùng IDbConnectionFactory tenant-aware (đích) + đọc master từ ConnectionStrings:Config.
        services.AddScoped<IConfigSyncService, ConfigSync.ConfigSyncService>();

        // ── Auth (đăng nhập / JWT) ────────────────────────────────────────────
        // Repo đọc HT_NguoiDung/HT_RefreshToken từ Data DB tenant (scoped — tenant-aware).
        // Hasher + JwtTokenService stateless → singleton (JwtTokenService đọc Jwt:* lúc khởi tạo).
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<INavigationRepository, NavigationRepository>();
        services.AddScoped<IPermissionAdminRepository, PermissionAdminRepository>();
        services.AddScoped<IMenuAdminRepository, MenuAdminRepository>();
        services.AddScoped<IPermissionService, Services.PermissionService>();
        services.AddSingleton<INavigationCache, Services.NavigationCache>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // ── Audit log (nhật ký hoạt động — non-blocking) ─────────────────────
        // Hàng đợi + bộ ghi singleton; tiến trình nền tiêu thụ và ghi NK_ (Redis Stream hoặc
        // fallback ghi thẳng DB). IAuditWriter (enqueue) đăng ký ở tầng Api (cần HttpContext).
        services.AddSingleton<IAuditQueue>(_ => new AuditQueue(capacity: 20_000));
        services.AddSingleton<AuditNkWriter>();
        services.AddHostedService<AuditBackgroundService>();

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
