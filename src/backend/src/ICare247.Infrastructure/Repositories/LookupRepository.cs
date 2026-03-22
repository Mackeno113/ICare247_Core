// File    : LookupRepository.cs
// Module  : Lookup
// Layer   : Infrastructure
// Purpose : Dapper implementation của ILookupRepository — đọc Sys_Lookup + resolve label.

using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Lookup;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Repository cho <c>Sys_Lookup</c>.
/// Ưu tiên record có <c>Tenant_Id = @TenantId</c>, fallback về global (<c>Tenant_Id = 0</c>).
/// Label resolve từ <c>Sys_Resource</c> theo <c>LangCode</c>, fallback 'vi' nếu không tìm thấy.
/// </summary>
public sealed class LookupRepository : ILookupRepository
{
    private readonly IDbConnectionFactory _db;

    public LookupRepository(IDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<LookupItem>> GetByCodeAsync(
        string lookupCode, int tenantId, string langCode = "vi",
        CancellationToken ct = default)
    {
        // Ưu tiên tenant riêng, fallback global (Tenant_Id=0).
        // JOIN Sys_Resource để lấy label đã dịch; fallback về Label_Key nếu chưa có bản dịch.
        const string sql = """
            WITH ranked AS (
                SELECT l.Lookup_Id,
                       l.Tenant_Id,
                       l.Lookup_Code,
                       l.Item_Code,
                       l.Label_Key,
                       COALESCE(r.Resource_Value, l.Label_Key) AS Label,
                       l.Sort_Order,
                       l.Is_Active,
                       -- Tenant riêng được ưu tiên (rank=1) trước global (rank=2)
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
            SELECT Lookup_Id  AS LookupId,
                   Tenant_Id  AS TenantId,
                   Lookup_Code AS LookupCode,
                   Item_Code  AS ItemCode,
                   Label_Key  AS LabelKey,
                   Label,
                   Sort_Order AS SortOrder,
                   Is_Active  AS IsActive
            FROM   ranked
            WHERE  rn = 1
            ORDER BY Sort_Order, Item_Code
            """;

        using var conn = _db.CreateConnection();
        var cmd = new CommandDefinition(
            sql,
            new { LookupCode = lookupCode, TenantId = tenantId, LangCode = langCode },
            cancellationToken: ct);

        var results = await conn.QueryAsync<LookupItem>(cmd);
        return results.ToList();
    }
}
