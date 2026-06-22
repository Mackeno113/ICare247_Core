// File    : HookStoreCatalog.cs
// Module  : MasterData
// Layer   : Infrastructure
// Purpose : Impl IHookStoreCatalog — tra tồn tại spc_Grid_/sp_AfterSave_Grid_ của bảng QUA CACHE
//           (L1/L2). Cold-miss → 1 query gộp 2 OBJECT_ID trên Data DB rồi cache. Save path đọc
//           cache → 0 query khi lưu (ADR-029).

using System.Text.RegularExpressions;
using Dapper;
using ICare247.Application.Constants;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Services;

/// <summary>Cache-aside catalog cho cờ tồn tại hook store theo (tenant, bảng).</summary>
public sealed partial class HookStoreCatalog : IHookStoreCatalog
{
    private readonly IDataDbConnectionFactory _dataDb;
    private readonly ICacheService _cache;
    private readonly ICacheVersion _version;

    // TTL: store hiếm thay đổi → cache khá lâu; flush cache (bump version) vô hiệu ngay khi tạo store mới.
    private static readonly TimeSpan MemTtl   = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RedisTtl = TimeSpan.FromMinutes(60);

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex SafeIdentifierRegex();

    public HookStoreCatalog(IDataDbConnectionFactory dataDb, ICacheService cache, ICacheVersion version)
    {
        _dataDb  = dataDb;
        _cache   = cache;
        _version = version;
    }

    /// <inheritdoc />
    public async Task<HookStoreFlags> GetAsync(string tableName, int tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tableName) || !SafeIdentifierRegex().IsMatch(tableName))
            return HookStoreFlags.None;

        var key = CacheKeys.HookStore(tableName, tenantId, _version.Get(tenantId));

        var cached = await _cache.GetAsync<HookStoreFlags>(key, ct);
        if (cached is not null) return cached;

        // Cold-miss: 1 query gộp tồn tại 2 store (dbo) trên Data DB.
        const string sql = """
            SELECT
                CASE WHEN OBJECT_ID(@V, 'P') IS NOT NULL THEN 1 ELSE 0 END AS HasValidate,
                CASE WHEN OBJECT_ID(@A, 'P') IS NOT NULL THEN 1 ELSE 0 END AS HasAfterSave
            """;
        using var conn = _dataDb.CreateConnection();
        var row = await conn.QueryFirstAsync<ExistsRow>(new CommandDefinition(
            sql,
            new { V = $"dbo.spc_Grid_{tableName}", A = $"dbo.sp_AfterSave_Grid_{tableName}" },
            cancellationToken: ct));

        var flags = new HookStoreFlags { HasValidate = row.HasValidate, HasAfterSave = row.HasAfterSave };
        await _cache.SetAsync(key, flags, MemTtl, RedisTtl, ct);
        return flags;
    }

    /// <summary>Dapper map cho result set tồn tại store.</summary>
    private sealed class ExistsRow
    {
        public bool HasValidate  { get; init; }
        public bool HasAfterSave { get; init; }
    }
}
