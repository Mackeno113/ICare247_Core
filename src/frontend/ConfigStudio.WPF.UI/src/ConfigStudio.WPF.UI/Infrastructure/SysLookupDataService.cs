// File    : SysLookupDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Dapper implementation cho ISysLookupDataService — Sys_Lookup.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// Truy vấn <c>Sys_Lookup</c> kèm resolve label từ <c>Sys_Resource</c>.
/// Ưu tiên tenant riêng, fallback về global (Tenant_Id = 0).
/// </summary>
public sealed class SysLookupDataService : ISysLookupDataService
{
    private readonly IAppConfigService _config;

    public SysLookupDataService(IAppConfigService config) => _config = config;

    /// <inheritdoc />
    public async Task<IReadOnlyList<LookupItemRecord>> GetByCodeAsync(
        string lookupCode, string langCode = "vi",
        CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        // Ưu tiên tenant riêng, fallback global. Label từ Sys_Resource, fallback Label_Key.
        const string sql = """
            WITH ranked AS (
                SELECT l.Item_Code,
                       l.Label_Key,
                       COALESCE(r.Resource_Value, l.Label_Key) AS Label,
                       l.Sort_Order,
                       ROW_NUMBER() OVER (
                           PARTITION BY l.Item_Code
                           ORDER BY CASE WHEN l.Tenant_Id = @TenantId THEN 0 ELSE 1 END
                       ) AS rn
                FROM   dbo.Sys_Lookup l
                LEFT JOIN dbo.Sys_Resource r
                       ON r.Resource_Key = l.Label_Key
                      AND r.Lang_Code    = @LangCode
                WHERE  l.Lookup_Code = @LookupCode
                  AND  (l.Tenant_Id  = @TenantId OR l.Tenant_Id = 0)
                  AND  l.Is_Active   = 1
            )
            SELECT Item_Code  AS ItemCode,
                   Label_Key  AS LabelKey,
                   Label,
                   Sort_Order AS SortOrder
            FROM   ranked
            WHERE  rn = 1
            ORDER BY Sort_Order, Item_Code
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<LookupItemRecord>(
            new CommandDefinition(sql,
                new { LookupCode = lookupCode, TenantId = _config.TenantId, LangCode = langCode },
                cancellationToken: ct));

        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetAllCodesAsync(CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT DISTINCT Lookup_Code
            FROM   dbo.Sys_Lookup
            WHERE  (Tenant_Id = @TenantId OR Tenant_Id = 0)
              AND  Is_Active  = 1
            ORDER BY Lookup_Code
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var codes = await conn.QueryAsync<string>(
            new CommandDefinition(sql,
                new { TenantId = _config.TenantId },
                cancellationToken: ct));

        return codes.AsList();
    }
}
