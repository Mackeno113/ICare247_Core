// File    : I18nDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Dapper implementation cho II18nDataService — Sys_Resource + Sys_Language.
//           Có cache in-memory toàn bộ Sys_Resource (nạp 1 lần) để ResolveKeyAsync
//           không tạo N+1 query khi mở form (mỗi section/field 1 round-trip).

using System.Collections.Concurrent;
using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// CRUD i18n resources. Global data — không filter Tenant_Id.
/// Đăng ký SINGLETON (App.xaml.cs) để cache sống xuyên suốt phiên làm việc.
/// </summary>
public sealed class I18nDataService : II18nDataService
{
    private readonly IAppConfigService _config;

    // ── Cache toàn bộ Sys_Resource ────────────────────────────
    // Key cache = "{resourceKey}|{langCode}" → Resource_Value.
    // Separator '|' không xuất hiện trong resource key (chữ thường + dấu chấm) lẫn lang code.
    private readonly ConcurrentDictionary<string, string?> _cache = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    // Connection string đã dùng để nạp cache — đổi (qua Settings) → nạp lại.
    private string? _loadedForConn;

    public I18nDataService(IAppConfigService config)
    {
        _config = config;
    }

    /// <summary>Ghép khóa cache từ resourceKey + langCode (có separator tránh va chạm).</summary>
    private static string CacheKey(string resourceKey, string langCode) => $"{resourceKey}|{langCode}";

    /// <summary>
    /// Đảm bảo cache đã nạp toàn bộ Sys_Resource cho connection string hiện tại.
    /// Nạp 1 query duy nhất; lần đổi connection string → xóa cache + nạp lại.
    /// Sự kiện theo sau: <see cref="ResolveKeyAsync"/> đọc thẳng từ cache (RAM).
    /// </summary>
    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (!_config.IsConfigured) return;

        var conn = _config.ConnectionString;
        if (_loadedForConn == conn) return;

        await _loadLock.WaitAsync(ct);
        try
        {
            // Double-check sau khi giành được lock
            if (_loadedForConn == conn) return;

            const string sql = """
                SELECT Resource_Key   AS ResourceKey,
                       Lang_Code      AS LangCode,
                       Resource_Value AS ResourceValue
                FROM   dbo.Sys_Resource
                """;

            await using var sqlConn = new SqlConnection(conn);
            var rows = await sqlConn.QueryAsync<ResourceRow>(
                new CommandDefinition(sql, cancellationToken: ct));

            _cache.Clear();
            foreach (var r in rows)
                _cache[CacheKey(r.ResourceKey, r.LangCode)] = r.ResourceValue;

            _loadedForConn = conn;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>Dòng thô đọc từ Sys_Resource khi nạp cache.</summary>
    private sealed record ResourceRow(string ResourceKey, string LangCode, string? ResourceValue);

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        // Buộc nạp lại lần kế tiếp — dùng sau khi sửa hàng loạt resource ở I18nManager.
        _loadedForConn = null;
        await EnsureLoadedAsync(ct);
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

        // Nạp cache lần đầu (1 query cho toàn bộ resource) rồi đọc từ RAM.
        await EnsureLoadedAsync(ct);
        return _cache.TryGetValue(CacheKey(resourceKey, langCode), out var value) ? value : null;
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

        // Write-through cache — không phải nạp lại toàn bộ.
        _cache[CacheKey(resourceKey, langCode)] = value;
    }

    /// <inheritdoc />
    public async Task InitResourceIfMissingAsync(
        string resourceKey, string langCode, string defaultValue, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        // Đã có trong cache → coi như tồn tại, bỏ qua cả round-trip DB.
        await EnsureLoadedAsync(ct);
        if (_cache.ContainsKey(CacheKey(resourceKey, langCode))) return;

        // Chỉ INSERT nếu chưa có — không ghi đè bản dịch người dùng đã sửa
        const string sql = """
            IF NOT EXISTS (
                SELECT 1 FROM dbo.Sys_Resource
                WHERE  Resource_Key = @Key AND Lang_Code = @Lang
            )
                INSERT INTO dbo.Sys_Resource (Resource_Key, Lang_Code, Resource_Value, Version, Updated_At)
                VALUES (@Key, @Lang, @Value, 1, GETDATE())
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(
            new CommandDefinition(sql,
                new { Key = resourceKey, Lang = langCode, Value = defaultValue },
                cancellationToken: ct));

        // Phản ánh vào cache (TryAdd để không đè nếu thread khác vừa set).
        _cache.TryAdd(CacheKey(resourceKey, langCode), defaultValue);
    }

    /// <inheritdoc />
    public async Task DeleteResourceAsync(string resourceKey, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        const string sql = "DELETE FROM dbo.Sys_Resource WHERE Resource_Key = @Key";

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Key = resourceKey }, cancellationToken: ct));

        // Xóa mọi ngôn ngữ của key khỏi cache (prefix = "{key}|").
        var prefix = resourceKey + "|";
        foreach (var k in _cache.Keys)
            if (k.StartsWith(prefix, StringComparison.Ordinal))
                _cache.TryRemove(k, out _);
    }
}
