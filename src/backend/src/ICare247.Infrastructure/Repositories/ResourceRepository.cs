// File    : ResourceRepository.cs
// Module  : Resource
// Layer   : Infrastructure
// Purpose : Dapper implementation của IResourceRepository — đọc Sys_Resource theo form scope.
//           Load toàn bộ keys của form + global sys.val.* trong 1 query.

using Dapper;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Repository đọc <c>Sys_Resource</c> để build ResourceMap cho ValidationEngine.
/// <para>
/// Query lấy: key của form cụ thể (<c>{formCode}.%</c>) + global sys keys (<c>sys.val.%</c>).
/// Sys_Resource không có Tenant_Id — tenant isolation qua formCode convention.
/// </para>
/// </summary>
public sealed class ResourceRepository : IResourceRepository
{
    private readonly IDbConnectionFactory _db;

    public ResourceRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> GetByFormAsync(
        string formCode,
        string langCode,
        int tenantId,
        CancellationToken ct = default)
    {
        // Load keys của form cụ thể + global sys.val/sys.hint templates trong 1 query.
        const string sql = """
            SELECT r.Resource_Key   AS ResourceKey,
                   r.Resource_Value AS ResourceValue
            FROM   dbo.Sys_Resource r
            WHERE  r.Lang_Code = @LangCode
              AND  (r.Resource_Key LIKE @FormPrefix
                    OR r.Resource_Key LIKE 'sys.val.%'
                    OR r.Resource_Key LIKE 'sys.hint.%')
            """;

        using var conn = _db.CreateConnection();
        var cmd = new CommandDefinition(
            sql,
            new
            {
                LangCode   = langCode,
                FormPrefix = formCode + ".%"
            },
            cancellationToken: ct);

        var rows = await conn.QueryAsync<ResourceRow>(cmd);

        // Build dictionary — OrdinalIgnoreCase để lookup không phân biệt hoa thường
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.ResourceKey))
                map[row.ResourceKey] = row.ResourceValue ?? string.Empty;
        }

        return map;
    }

    // ── Private DTO ──────────────────────────────────────────────────────
    private sealed class ResourceRow
    {
        public string ResourceKey   { get; init; } = string.Empty;
        public string? ResourceValue { get; init; }
    }
}
