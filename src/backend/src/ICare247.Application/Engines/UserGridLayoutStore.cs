// File    : UserGridLayoutStore.cs
// Module  : Engines
// Layer   : Application
// Purpose : Impl IUserGridLayoutStore — cache-aside L1+L2 trên IUserGridLayoutRepository (Data DB).
//           Single-writer (chỉ chủ sở hữu ghi) → write-through, không invalidate chéo, TTL dài an toàn.

using ICare247.Application.Constants;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Engines;

/// <inheritdoc cref="IUserGridLayoutStore" />
public sealed class UserGridLayoutStore : IUserGridLayoutStore
{
    private readonly IUserGridLayoutRepository _repo;
    private readonly ICacheService _cache;
    private readonly ILogger<UserGridLayoutStore> _logger;

    // Layout đổi hiếm + single-writer → TTL dài (write-through giữ luôn mới).
    private static readonly TimeSpan L1Ttl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan L2Ttl = TimeSpan.FromHours(4);

    public UserGridLayoutStore(
        IUserGridLayoutRepository repo, ICacheService cache, ILogger<UserGridLayoutStore> logger)
    {
        _repo   = repo;
        _cache  = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(
        long userId, string viewCode, string platform, int tenantId, CancellationToken ct = default)
    {
        var key = CacheKeys.UserGridLayout(tenantId, userId, viewCode, platform);

        // CachedLayout bọc string vì ICacheService yêu cầu T : class và cần phân biệt
        // "đã cache = rỗng" với "chưa cache" (tránh đọc DB lặp cho user không có layout).
        var cached = await _cache.GetAsync<CachedLayout>(key, ct);
        if (cached is not null)
            return cached.Json;

        var json = await _repo.GetAsync(userId, viewCode, platform, ct);
        await _cache.SetAsync(key, new CachedLayout(json), memoryTtl: L1Ttl, redisTtl: L2Ttl, ct: ct);
        return json;
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        long userId, string viewCode, string platform, int tenantId, string layoutJson, CancellationToken ct = default)
    {
        await _repo.UpsertAsync(userId, viewCode, platform, layoutJson, ct);

        // Write-through: cập nhật cache ngay → lần đọc sau (kể cả máy khác) hit cache, không chạm DB.
        var key = CacheKeys.UserGridLayout(tenantId, userId, viewCode, platform);
        await _cache.SetAsync(key, new CachedLayout(layoutJson), memoryTtl: L1Ttl, redisTtl: L2Ttl, ct: ct);

        _logger.LogDebug("UserGridLayout saved — User={User}, View={View}, Tenant={Tenant}",
            userId, viewCode, tenantId);
    }

    /// <inheritdoc />
    public async Task ResetAsync(
        long userId, string viewCode, string platform, int tenantId, CancellationToken ct = default)
    {
        await _repo.DeleteAsync(userId, viewCode, platform, ct);
        await _cache.RemoveAsync(CacheKeys.UserGridLayout(tenantId, userId, viewCode, platform), ct);
    }

    /// <summary>Wrapper cache: phân biệt "đã cache nhưng rỗng" (Json=null) với "chưa cache" (cache miss).</summary>
    private sealed class CachedLayout
    {
        public string? Json { get; init; }
        public CachedLayout() { }
        public CachedLayout(string? json) => Json = json;
    }
}
