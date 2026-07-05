// File    : LookupCacheVersion.cs
// Module  : Common
// Layer   : Infrastructure
// Purpose : Impl ILookupCacheVersion bằng ConcurrentDictionary in-memory (giống CacheVersion).
// Note    : Khi scale-out ≥2 instance → thay bằng Redis INCR key lkupver:{tenant}:{table}.

using System.Collections.Concurrent;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Services;

/// <summary>Version-stamp cache dữ liệu lookup theo (tenant, bảng nguồn) — in-memory.</summary>
public sealed class LookupCacheVersion : ILookupCacheVersion
{
    private readonly ConcurrentDictionary<string, int> _versions = new();

    private static string Key(int tenantId, string sourceTable)
        => $"{tenantId}:{sourceTable.Trim().ToLowerInvariant()}";

    /// <inheritdoc />
    public int Get(int tenantId, string sourceTable)
        => _versions.TryGetValue(Key(tenantId, sourceTable), out var v) ? v : 0;

    /// <inheritdoc />
    public void Bump(int tenantId, string sourceTable)
        => _versions.AddOrUpdate(Key(tenantId, sourceTable), 1, (_, cur) => cur + 1);
}
