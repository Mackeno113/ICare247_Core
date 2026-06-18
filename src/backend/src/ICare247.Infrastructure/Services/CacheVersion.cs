// File    : CacheVersion.cs
// Module  : Common
// Layer   : Infrastructure
// Purpose : Impl ICacheVersion bằng ConcurrentDictionary in-memory (1 instance — giống NavigationCache).
// Note    : Khi scale-out ≥2 instance (ADR-021) → thay bằng Redis INCR key cfgver:{tenantId}
//           để mọi instance dùng chung version (bump 1 nơi, mọi nơi miss).

using System.Collections.Concurrent;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Services;

/// <summary>Version-stamp cache theo tenant (in-memory). Bump = tăng số → key cache cũ vô hiệu.</summary>
public sealed class CacheVersion : ICacheVersion
{
    private readonly ConcurrentDictionary<int, int> _versions = new();

    /// <inheritdoc />
    public int Get(int tenantId) => _versions.TryGetValue(tenantId, out var v) ? v : 0;

    /// <inheritdoc />
    public void Bump(int tenantId) => _versions.AddOrUpdate(tenantId, 1, (_, cur) => cur + 1);
}
