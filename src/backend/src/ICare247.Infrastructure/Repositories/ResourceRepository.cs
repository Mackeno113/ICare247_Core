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

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> GetByKeysAsync(
        IEnumerable<string> keys,
        string langCode,
        CancellationToken ct = default)
    {
        // Loại bỏ key rỗng, tránh query vô nghĩa
        var keyList = keys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
        if (keyList.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        const string sql = """
            SELECT r.Resource_Key   AS ResourceKey,
                   r.Resource_Value AS ResourceValue
            FROM   dbo.Sys_Resource r
            WHERE  r.Lang_Code = @LangCode
              AND  r.Resource_Key IN @Keys
            """;

        using var conn = _db.CreateConnection();
        var cmd = new CommandDefinition(
            sql,
            new { LangCode = langCode, Keys = keyList },
            cancellationToken: ct);

        var rows = await conn.QueryAsync<ResourceRow>(cmd);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.ResourceKey))
                map[row.ResourceKey] = row.ResourceValue ?? string.Empty;
        }

        return map;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> GetOverlayAsync(
        string langCode,
        string? keyPrefix = null,
        CancellationToken ct = default)
    {
        // Lấy toàn bộ key của ngôn ngữ; lọc theo prefix nếu có (vd "nav.").
        const string sql = """
            SELECT r.Resource_Key   AS ResourceKey,
                   r.Resource_Value AS ResourceValue
            FROM   dbo.Sys_Resource r
            WHERE  r.Lang_Code = @LangCode
              AND  (@Prefix IS NULL OR r.Resource_Key LIKE @PrefixLike)
            """;

        using var conn = _db.CreateConnection();
        var cmd = new CommandDefinition(
            sql,
            new
            {
                LangCode   = langCode,
                Prefix     = keyPrefix,
                PrefixLike = keyPrefix + "%"
            },
            cancellationToken: ct);

        var rows = await conn.QueryAsync<ResourceRow>(cmd);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.ResourceKey))
                map[row.ResourceKey] = row.ResourceValue ?? string.Empty;
        }
        return map;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> GetByKeyAllLangsAsync(
        string key,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT r.Lang_Code      AS ResourceKey,
                   r.Resource_Value AS ResourceValue
            FROM   dbo.Sys_Resource r
            WHERE  r.Resource_Key = @Key
            """;

        using var conn = _db.CreateConnection();
        var cmd = new CommandDefinition(sql, new { Key = key }, cancellationToken: ct);
        var rows = await conn.QueryAsync<ResourceRow>(cmd);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.ResourceKey))
                map[row.ResourceKey] = row.ResourceValue ?? string.Empty;
        }
        return map;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(
        string key,
        string langCode,
        string value,
        CancellationToken ct = default)
    {
        // MERGE theo PK (Resource_Key, Lang_Code). Tăng Version khi cập nhật.
        const string sql = """
            MERGE dbo.Sys_Resource AS t
            USING (SELECT @Key AS Resource_Key, @Lang AS Lang_Code) AS s
            ON (t.Resource_Key = s.Resource_Key AND t.Lang_Code = s.Lang_Code)
            WHEN MATCHED THEN
                UPDATE SET Resource_Value = @Value, Version = t.Version + 1, Updated_At = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT (Resource_Key, Lang_Code, Resource_Value, Version, Updated_At)
                VALUES (@Key, @Lang, @Value, 1, GETDATE());
            """;

        using var conn = _db.CreateConnection();
        var cmd = new CommandDefinition(
            sql,
            new { Key = key, Lang = langCode, Value = value },
            cancellationToken: ct);
        await conn.ExecuteAsync(cmd);
    }

    // ── Private DTO ──────────────────────────────────────────────────────
    private sealed class ResourceRow
    {
        public string ResourceKey   { get; init; } = string.Empty;
        public string? ResourceValue { get; init; }
    }
}
