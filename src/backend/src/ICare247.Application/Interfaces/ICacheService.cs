// File    : ICacheService.cs
// Module  : Common
// Layer   : Application
// Purpose : Hybrid cache service (L1 Memory + L2 Redis) — Application layer chỉ gọi Get/Set/Remove.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Cache service hỗ trợ L1 (MemoryCache) + L2 (Redis).
/// Khi Get: kiểm tra L1 trước → miss thì kiểm tra L2 → miss thì return null.
/// Khi Set: ghi cả L1 và L2.
/// Khi Remove: xóa cả L1 và L2.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Lấy giá trị từ cache. Kiểm tra L1 → L2 → null.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Ghi giá trị vào cache (L1 + L2).
    /// </summary>
    /// <param name="key">Cache key — lấy từ <c>CacheKeys</c>.</param>
    /// <param name="value">Giá trị cần cache.</param>
    /// <param name="memoryTtl">TTL cho L1 MemoryCache (mặc định 5 phút).</param>
    /// <param name="redisTtl">TTL cho L2 Redis (mặc định 30 phút).</param>
    Task SetAsync<T>(string key, T value,
        TimeSpan? memoryTtl = null,
        TimeSpan? redisTtl = null,
        CancellationToken ct = default) where T : class;

    /// <summary>
    /// Xóa key khỏi cả L1 và L2.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken ct = default);
}
