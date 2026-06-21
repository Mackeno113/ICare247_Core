// File    : NavigationCache.cs
// Module  : Navigation
// Layer   : Infrastructure
// Purpose : Impl INavigationCache bằng IMemoryCache + token hủy theo tenant. Mỗi tenant 1
//           CancellationTokenSource — Invalidate = cancel → mọi entry menu của tenant bị xóa cùng lúc.
// Note    : In-memory (1 instance). Khi scale-out ≥2 instance (ADR-021) → thay token bằng Redis pub/sub.

using System.Collections.Concurrent;
using ICare247.Application.Features.Navigation;
using ICare247.Application.Interfaces;
using ICare247.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ICare247.Infrastructure.Services;

/// <summary>Cache menu theo (tenant,user); invalidate cả tenant qua token hủy dùng chung.</summary>
public sealed class NavigationCache : INavigationCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    private readonly IMemoryCache _cache;
    private readonly bool _enabled;
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _tenantTokens = new();

    public NavigationCache(IMemoryCache cache, IOptions<CacheSettings> cacheOptions)
    {
        _cache = cache;
        _enabled = cacheOptions.Value.Enabled;
    }

    /// <inheritdoc />
    public async Task<MeNavigationDto> GetOrLoadAsync(int tenantId, long userId, Func<Task<MeNavigationDto>> load)
    {
        // Cache tắt (Cache:Enabled=false) → luôn nạp mới, không lưu (test menu).
        if (!_enabled) return await load();

        var key = $"nav:{tenantId}:{userId}";
        if (_cache.TryGetValue(key, out MeNavigationDto? cached) && cached is not null)
            return cached;

        var data = await load();

        var cts = _tenantTokens.GetOrAdd(tenantId, _ => new CancellationTokenSource());
        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl }
            .AddExpirationToken(new CancellationChangeToken(cts.Token));
        _cache.Set(key, data, options);
        return data;
    }

    /// <inheritdoc />
    public void InvalidateTenant(int tenantId)
    {
        if (_tenantTokens.TryRemove(tenantId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}
