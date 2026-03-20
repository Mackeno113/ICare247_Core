// File    : HybridCacheService.cs
// Module  : Caching
// Layer   : Infrastructure
// Purpose : L1 MemoryCache + L2 Redis — implement ICacheService.

using System.Text.Json;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Caching;

/// <summary>
/// Hybrid cache: L1 (MemoryCache, 5 phút) → L2 (Redis, 30 phút).
/// Get: L1 hit → return. L1 miss → L2 hit → promote L1 → return. All miss → null.
/// Set: ghi cả L1 + L2.
/// Remove: xóa cả L1 + L2.
/// </summary>
public sealed class HybridCacheService : ICacheService
{
    /// <summary>TTL mặc định cho L1 MemoryCache.</summary>
    private static readonly TimeSpan DefaultMemoryTtl = TimeSpan.FromMinutes(5);

    /// <summary>TTL mặc định cho L2 Redis.</summary>
    private static readonly TimeSpan DefaultRedisTtl = TimeSpan.FromMinutes(30);

    private readonly IMemoryCache _memory;
    private readonly IDistributedCache? _redis;
    private readonly ILogger<HybridCacheService> _logger;

    /// <summary>JSON options dùng chung — camelCase, ignore null.</summary>
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public HybridCacheService(
        IMemoryCache memory,
        IDistributedCache? redis,
        ILogger<HybridCacheService> logger)
    {
        _memory = memory;
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        // ── L1: MemoryCache ─────────────────────────────────────────────────
        if (_memory.TryGetValue(key, out T? cached) && cached is not null)
        {
            _logger.LogDebug("Cache L1 hit — Key={Key}", key);
            return cached;
        }

        // ── L2: Redis ───────────────────────────────────────────────────────
        if (_redis is null) return null;

        try
        {
            var bytes = await _redis.GetAsync(key, ct);
            if (bytes is null) return null;

            var value = JsonSerializer.Deserialize<T>(bytes, JsonOpts);
            if (value is null) return null;

            // Promote lên L1
            _memory.Set(key, value, DefaultMemoryTtl);
            _logger.LogDebug("Cache L2 hit → promote L1 — Key={Key}", key);

            return value;
        }
        catch (Exception ex)
        {
            // Redis lỗi → fallback, không crash app
            _logger.LogWarning(ex, "Redis GetAsync lỗi — Key={Key}, tiếp tục không cache", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value,
        TimeSpan? memoryTtl = null,
        TimeSpan? redisTtl = null,
        CancellationToken ct = default) where T : class
    {
        // ── L1: MemoryCache ─────────────────────────────────────────────────
        _memory.Set(key, value, memoryTtl ?? DefaultMemoryTtl);

        // ── L2: Redis ───────────────────────────────────────────────────────
        if (_redis is null) return;

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOpts);
            await _redis.SetAsync(key, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = redisTtl ?? DefaultRedisTtl
            }, ct);
        }
        catch (Exception ex)
        {
            // Redis lỗi → L1 vẫn có data, không crash
            _logger.LogWarning(ex, "Redis SetAsync lỗi — Key={Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        // ── L1 ──────────────────────────────────────────────────────────────
        _memory.Remove(key);

        // ── L2 ──────────────────────────────────────────────────────────────
        if (_redis is null) return;

        try
        {
            await _redis.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis RemoveAsync lỗi — Key={Key}", key);
        }
    }
}
