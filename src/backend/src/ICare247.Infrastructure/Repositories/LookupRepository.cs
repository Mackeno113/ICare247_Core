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
/// Cô lập tenant ở tầng connection (1 Config DB = 1 tenant, ADR-035) — KHÔNG lọc theo cột.
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
        // JOIN Sys_Resource để lấy label đã dịch; fallback về Label_Key nếu chưa có bản dịch.
        const string sql = """
            SELECT l.Lookup_Id                             AS LookupId,
                   l.Lookup_Code                           AS LookupCode,
                   l.Item_Code                             AS ItemCode,
                   l.Label_Key                             AS LabelKey,
                   COALESCE(r.Resource_Value, l.Label_Key) AS Label,
                   l.Sort_Order                            AS SortOrder,
                   l.Is_Active                             AS IsActive
            FROM   dbo.Sys_Lookup l
            LEFT JOIN dbo.Sys_Resource r
                   ON r.Resource_Key = l.Label_Key
                  AND r.Lang_Code    = @LangCode
            WHERE  l.Lookup_Code = @LookupCode
              AND  l.Is_Active   = 1
            ORDER BY l.Sort_Order, l.Item_Code
            """;

        using var conn = _db.CreateConnection();
        var cmd = new CommandDefinition(
            sql,
            new { LookupCode = lookupCode, LangCode = langCode },
            cancellationToken: ct);

        var results = await conn.QueryAsync<LookupItem>(cmd);
        return results.ToList();
    }
}
