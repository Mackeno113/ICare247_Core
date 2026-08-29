// File    : ContextParamRepository.cs
// Module  : Context
// Layer   : Infrastructure
// Purpose : Dapper đọc Sys_Context_Param (Config DB) — registry token ngữ cảnh đang bật, QUA CACHE
//           (L1/L2, cùng khuôn CodeRuleCatalog/HookStoreCatalog): bảng bé, đọc lại ở MỌI lần resolve
//           token (View/Lookup/DocTemplate) nhưng gần như không đổi.

using Dapper;
using ICare247.Application.Constants;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Context;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Đọc <c>Sys_Context_Param</c> (Config DB) — token <c>Is_Active=1</c>, cache-aside theo tenant.</summary>
public sealed class ContextParamRepository : IContextParamRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ICacheService _cache;
    private readonly ICacheVersion _version;
    private readonly ITenantContext _tenant;

    // Registry hiếm đổi (chỉnh qua SQL/ConfigStudio, không phải per-request) → cache lâu; bump version
    // (nút "Cưỡng chế làm mới cache", ADR-014/CC-4a) vô hiệu ngay khi cần — cùng TTL CodeRuleCatalog.
    private static readonly TimeSpan MemTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RedisTtl = TimeSpan.FromMinutes(60);

    public ContextParamRepository(
        IDbConnectionFactory db, ICacheService cache, ICacheVersion version, ITenantContext tenant)
    {
        _db = db;
        _cache = cache;
        _version = version;
        _tenant = tenant;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContextParam>> GetActiveAsync(CancellationToken ct = default)
    {
        var tenantId = _tenant.TenantId;
        var key = CacheKeys.ContextParamRegistry(tenantId, _version.Get(tenantId));

        var cached = await _cache.GetAsync<List<ContextParam>>(key, ct);
        if (cached is not null)
            return cached;

        var rows = await LoadAsync(ct);
        await _cache.SetAsync(key, rows, MemTtl, RedisTtl, ct);
        return rows;
    }

    /// <summary>Đọc thẳng Config DB (bỏ qua cache) — chỉ gọi khi cache miss.</summary>
    private async Task<List<ContextParam>> LoadAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT Param_Id      AS ParamId,
                   Param_Name    AS ParamName,
                   Sql_Type      AS SqlType,
                   Source_Kind   AS SourceKind,
                   Source_Key    AS SourceKey,
                   Validate_Sql  AS ValidateSql,
                   Default_Value AS DefaultValue,
                   Description,
                   Is_System     AS IsSystem
            FROM   dbo.Sys_Context_Param
            WHERE  Is_Active = 1
            """;

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<ContextParam>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }
}
