// File    : SysLookupDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Dapper implementation cho ISysLookupDataService — CRUD Sys_Lookup + Sys_Resource.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// Truy vấn và quản lý <c>Sys_Lookup</c> kèm resolve label từ <c>Sys_Resource</c>.
/// Ưu tiên tenant riêng, fallback về global (Tenant_Id = 0).
/// </summary>
public sealed class SysLookupDataService : ISysLookupDataService
{
    private readonly IAppConfigService _config;

    public SysLookupDataService(IAppConfigService config) => _config = config;

    // ── Đọc ──────────────────────────────────────────────────

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
            ORDER BY Lookup_Code
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var codes = await conn.QueryAsync<string>(
            new CommandDefinition(sql,
                new { TenantId = _config.TenantId },
                cancellationToken: ct));

        return codes.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LookupItemEditRecord>> GetItemsForEditAsync(
        string lookupCode, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        // Lấy đầy đủ cả inactive, join Sys_Resource để lấy label vi + en.
        const string sql = """
            SELECT l.Lookup_Id   AS LookupId,
                   l.Tenant_Id   AS TenantId,
                   l.Lookup_Code AS LookupCode,
                   l.Item_Code   AS ItemCode,
                   l.Label_Key   AS LabelKey,
                   COALESCE(rvi.Resource_Value, N'') AS LabelVi,
                   COALESCE(ren.Resource_Value, N'') AS LabelEn,
                   l.Sort_Order  AS SortOrder,
                   l.Is_Active   AS IsActive
            FROM   dbo.Sys_Lookup l
            LEFT JOIN dbo.Sys_Resource rvi
                   ON rvi.Resource_Key = l.Label_Key AND rvi.Lang_Code = N'vi'
            LEFT JOIN dbo.Sys_Resource ren
                   ON ren.Resource_Key = l.Label_Key AND ren.Lang_Code = N'en'
            WHERE  l.Lookup_Code = @LookupCode
              AND  (l.Tenant_Id  = @TenantId OR l.Tenant_Id = 0)
            ORDER BY l.Sort_Order, l.Item_Code
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<LookupItemEditRecord>(
            new CommandDefinition(sql,
                new { LookupCode = lookupCode, TenantId = _config.TenantId },
                cancellationToken: ct));

        return items.AsList();
    }

    // ── Ghi ──────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<int> AddItemAsync(LookupItemEditRecord item, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return 0;

        // Label_Key tự sinh theo pattern: {lookup_code_lower}.{item_code_lower}
        var labelKey = $"{item.LookupCode.ToLower()}.{item.ItemCode.ToLower()}";
        item.LabelKey = labelKey;

        const string insertLookup = """
            INSERT INTO dbo.Sys_Lookup (Tenant_Id, Lookup_Code, Item_Code, Label_Key, Sort_Order, Is_Active)
            OUTPUT INSERTED.Lookup_Id
            VALUES (@TenantId, @LookupCode, @ItemCode, @LabelKey, @SortOrder, @IsActive)
            """;

        // Upsert Sys_Resource vi + en
        const string upsertResource = """
            IF EXISTS (SELECT 1 FROM dbo.Sys_Resource WHERE Resource_Key = @Key AND Lang_Code = @Lang)
                UPDATE dbo.Sys_Resource
                SET    Resource_Value = @Val, Version = Version + 1
                WHERE  Resource_Key = @Key AND Lang_Code = @Lang
            ELSE
                INSERT INTO dbo.Sys_Resource (Resource_Key, Lang_Code, Resource_Value, Version)
                VALUES (@Key, @Lang, @Val, 1)
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            var newId = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(insertLookup,
                    new { TenantId = _config.TenantId, item.LookupCode, item.ItemCode,
                          item.LabelKey, item.SortOrder, item.IsActive },
                    transaction: tx, cancellationToken: ct));

            // Lưu label vi
            if (!string.IsNullOrWhiteSpace(item.LabelVi))
                await conn.ExecuteAsync(
                    new CommandDefinition(upsertResource,
                        new { Key = labelKey, Lang = "vi", Val = item.LabelVi },
                        transaction: tx, cancellationToken: ct));

            // Lưu label en
            if (!string.IsNullOrWhiteSpace(item.LabelEn))
                await conn.ExecuteAsync(
                    new CommandDefinition(upsertResource,
                        new { Key = labelKey, Lang = "en", Val = item.LabelEn },
                        transaction: tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
            return newId;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateItemAsync(LookupItemEditRecord item, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        const string updateLookup = """
            UPDATE dbo.Sys_Lookup
            SET    Item_Code  = @ItemCode,
                   Label_Key  = @LabelKey,
                   Sort_Order = @SortOrder,
                   Is_Active  = @IsActive
            WHERE  Lookup_Id  = @LookupId
            """;

        const string upsertResource = """
            IF EXISTS (SELECT 1 FROM dbo.Sys_Resource WHERE Resource_Key = @Key AND Lang_Code = @Lang)
                UPDATE dbo.Sys_Resource
                SET    Resource_Value = @Val, Version = Version + 1
                WHERE  Resource_Key = @Key AND Lang_Code = @Lang
            ELSE
                INSERT INTO dbo.Sys_Resource (Resource_Key, Lang_Code, Resource_Value, Version)
                VALUES (@Key, @Lang, @Val, 1)
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            await conn.ExecuteAsync(
                new CommandDefinition(updateLookup,
                    new { item.ItemCode, item.LabelKey, item.SortOrder, item.IsActive, item.LookupId },
                    transaction: tx, cancellationToken: ct));

            // Cập nhật Sys_Resource vi + en
            if (!string.IsNullOrWhiteSpace(item.LabelVi))
                await conn.ExecuteAsync(
                    new CommandDefinition(upsertResource,
                        new { Key = item.LabelKey, Lang = "vi", Val = item.LabelVi },
                        transaction: tx, cancellationToken: ct));

            if (!string.IsNullOrWhiteSpace(item.LabelEn))
                await conn.ExecuteAsync(
                    new CommandDefinition(upsertResource,
                        new { Key = item.LabelKey, Lang = "en", Val = item.LabelEn },
                        transaction: tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(int lookupId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        const string sql = "DELETE FROM dbo.Sys_Lookup WHERE Lookup_Id = @LookupId";

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { LookupId = lookupId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> AddLookupCodeAsync(string lookupCode, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return false;

        // Kiểm tra code chưa tồn tại
        const string checkSql = """
            SELECT COUNT(1) FROM dbo.Sys_Lookup
            WHERE Lookup_Code = @LookupCode
              AND (Tenant_Id = @TenantId OR Tenant_Id = 0)
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var exists = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(checkSql,
                new { LookupCode = lookupCode, TenantId = _config.TenantId },
                cancellationToken: ct));

        // Code đã tồn tại — không thêm placeholder, coi như thành công
        return exists == 0 || true;
    }

    /// <inheritdoc />
    public async Task<bool> ItemCodeExistsAsync(
        string lookupCode, string itemCode,
        int excludeLookupId = 0,
        CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return false;

        const string sql = """
            SELECT COUNT(1) FROM dbo.Sys_Lookup
            WHERE  Lookup_Code = @LookupCode
              AND  Item_Code   = @ItemCode
              AND  (Tenant_Id  = @TenantId OR Tenant_Id = 0)
              AND  Lookup_Id  <> @ExcludeId
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql,
                new { LookupCode = lookupCode, ItemCode = itemCode,
                      TenantId = _config.TenantId, ExcludeId = excludeLookupId },
                cancellationToken: ct));

        return count > 0;
    }
}
