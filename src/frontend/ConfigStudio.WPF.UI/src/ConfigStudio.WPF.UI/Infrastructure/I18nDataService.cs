// File    : I18nDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Dapper implementation cho II18nDataService — Sys_Resource + Sys_Language.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// CRUD i18n resources. Global data — không filter Tenant_Id.
/// </summary>
public sealed class I18nDataService : II18nDataService
{
    private readonly IAppConfigService _config;

    public I18nDataService(IAppConfigService config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<I18nRecord>> GetResourcesAsync(CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT r.Resource_Key AS ResourceKey,
                   MAX(CASE WHEN r.Lang_Code = 'vi' THEN r.Resource_Value END) AS ViVn,
                   MAX(CASE WHEN r.Lang_Code = 'en' THEN r.Resource_Value END) AS EnUs,
                   MAX(CASE WHEN r.Lang_Code = 'ja' THEN r.Resource_Value END) AS JaJp
            FROM   dbo.Sys_Resource r
            GROUP BY r.Resource_Key
            ORDER BY r.Resource_Key
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<I18nRecord>(
            new CommandDefinition(sql, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LanguageRecord>> GetLanguagesAsync(CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT Lang_Code  AS LangCode,
                   Lang_Name  AS LangName,
                   Is_Default AS IsDefault
            FROM   dbo.Sys_Language
            ORDER BY Lang_Code
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<LanguageRecord>(
            new CommandDefinition(sql, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<string?> ResolveKeyAsync(string resourceKey, string langCode, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return null;

        const string sql = """
            SELECT Resource_Value
            FROM   dbo.Sys_Resource
            WHERE  Resource_Key = @Key AND Lang_Code = @Lang
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        return await conn.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(sql, new { Key = resourceKey, Lang = langCode }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task SaveResourceAsync(string resourceKey, string langCode, string value, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        // MERGE: upsert vào Sys_Resource
        const string sql = """
            IF EXISTS (SELECT 1 FROM dbo.Sys_Resource WHERE Resource_Key = @Key AND Lang_Code = @Lang)
                UPDATE dbo.Sys_Resource
                SET    Resource_Value = @Value,
                       Version        = Version + 1,
                       Updated_At     = GETDATE()
                WHERE  Resource_Key = @Key AND Lang_Code = @Lang
            ELSE
                INSERT INTO dbo.Sys_Resource (Resource_Key, Lang_Code, Resource_Value, Version, Updated_At)
                VALUES (@Key, @Lang, @Value, 1, GETDATE())
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Key = resourceKey, Lang = langCode, Value = value }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteResourceAsync(string resourceKey, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        const string sql = "DELETE FROM dbo.Sys_Resource WHERE Resource_Key = @Key";

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Key = resourceKey }, cancellationToken: ct));
    }
}
