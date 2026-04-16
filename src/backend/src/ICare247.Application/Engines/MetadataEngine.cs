// File    : MetadataEngine.cs
// Module  : Engines
// Layer   : Application
// Purpose : Implementation của IMetadataEngine — load FormMetadata + ResourceMap với hybrid cache.
//           L1 MemoryCache (5 phút) → L2 Redis (30 phút) → DB (FormRepository + ResourceRepository).
//           Sau khi load, resolve captionKey trong PopupColumnsJson → caption text theo langCode.

using System.Text.Json;
using System.Text.Json.Serialization;
using ICare247.Application.Constants;
using ICare247.Application.Interfaces;
using ICare247.Domain.Engine;
using ICare247.Domain.Entities.Form;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Engines;

/// <summary>
/// MetadataEngine: load <see cref="FormMetadata"/> đầy đủ bao gồm <c>ResourceMap</c> với L1+L2 cache.
/// <para>
/// Cache key: <c>icare:meta:rt:{tenantId}:{formCode}:lang:{langCode}</c>.
/// Invalidate bằng <see cref="InvalidateFormCacheAsync"/> sau khi admin cập nhật form/resource.
/// </para>
/// </summary>
public sealed class MetadataEngine : IMetadataEngine
{
    private readonly IFormRepository     _formRepo;
    private readonly IResourceRepository _resourceRepo;
    private readonly ICacheService       _cache;
    private readonly ILogger<MetadataEngine> _logger;

    // TTL cho RuntimeFormContext (FormMetadata + ResourceMap)
    private static readonly TimeSpan L1Ttl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan L2Ttl = TimeSpan.FromMinutes(30);

    // Ngôn ngữ mặc định khi invalidate (xóa cả các lang phổ biến)
    private static readonly string[] KnownLangCodes = ["vi", "en"];

    public MetadataEngine(
        IFormRepository     formRepo,
        IResourceRepository resourceRepo,
        ICacheService       cache,
        ILogger<MetadataEngine> logger)
    {
        _formRepo     = formRepo;
        _resourceRepo = resourceRepo;
        _cache        = cache;
        _logger       = logger;
    }

    /// <inheritdoc />
    public async Task<FormMetadata?> GetFormMetadataAsync(
        string formCode,
        string langCode,
        string platform,
        int tenantId,
        CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.RuntimeForm(formCode, langCode, tenantId);

        // ── L1/L2 cache lookup ────────────────────────────────────────────────
        var cached = await _cache.GetAsync<CachedFormMetadata>(cacheKey, ct);
        if (cached is not null)
            return cached.Metadata;

        // ── Cache miss: load từ DB ────────────────────────────────────────────
        _logger.LogDebug(
            "MetadataEngine cache miss — FormCode={FormCode}, Lang={Lang}, TenantId={TenantId}",
            formCode, langCode, tenantId);

        // Load FormMetadata (sections + fields) từ FormRepository
        var formMeta = await _formRepo.GetByCodeAsync(formCode, tenantId, langCode, ct);
        if (formMeta is null)
            return null;

        // Load ResourceMap (validation messages) từ ResourceRepository
        var resourceMap = await _resourceRepo.GetByFormAsync(formCode, langCode, tenantId, ct);

        // Resolve captionKey trong PopupColumnsJson của các LookupBox field → caption text theo langCode
        await ResolvePopupColumnCaptionsAsync(formMeta.Fields, langCode, ct);

        // Kết hợp: rebuild FormMetadata với ResourceMap đã load
        var enriched = new FormMetadata
        {
            FormId      = formMeta.FormId,
            TenantId    = formMeta.TenantId,
            FormCode    = formMeta.FormCode,
            FormName    = formMeta.FormName,
            Version     = formMeta.Version,
            Platform    = formMeta.Platform,
            Tabs        = formMeta.Tabs,
            Sections    = formMeta.Sections,
            Fields      = formMeta.Fields,
            ResourceMap = resourceMap
        };

        // ── Cache kết quả ─────────────────────────────────────────────────────
        await _cache.SetAsync(
            cacheKey,
            new CachedFormMetadata(enriched),
            memoryTtl: L1Ttl,
            redisTtl:  L2Ttl,
            ct: ct);

        return enriched;
    }

    /// <inheritdoc />
    public async Task InvalidateFormCacheAsync(string formCode, int tenantId)
    {
        // Xóa cache cho tất cả ngôn ngữ đã biết
        foreach (var lang in KnownLangCodes)
        {
            var key = CacheKeys.RuntimeForm(formCode, lang, tenantId);
            await _cache.RemoveAsync(key);
        }

        _logger.LogInformation(
            "MetadataEngine cache invalidated — FormCode={FormCode}, TenantId={TenantId}",
            formCode, tenantId);
    }

    // ── Resolve captionKey trong PopupColumnsJson ────────────────────────────

    /// <summary>
    /// Với mỗi field có LookupConfig.PopupColumnsJson, parse JSON, batch-load tất cả
    /// captionKey từ Sys_Resource theo langCode, rồi reserialize lại với "caption" đã resolved.
    /// <para>
    /// Backward compat: nếu entry chỉ có "caption" (plain text cũ), giữ nguyên.
    /// Nếu có "captionKey" nhưng không tìm thấy trong Sys_Resource, fallback = captionKey string.
    /// </para>
    /// </summary>
    private async Task ResolvePopupColumnCaptionsAsync(
        IReadOnlyList<FieldMetadata> fields,
        string langCode,
        CancellationToken ct)
    {
        // Thu thập tất cả captionKey từ tất cả LookupBox fields trong 1 pass
        var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var field in fields)
        {
            var json = field.LookupConfig?.PopupColumnsJson;
            if (string.IsNullOrWhiteSpace(json)) continue;

            try
            {
                var cols = JsonSerializer.Deserialize<List<PopupColRaw>>(json, jsonOpts);
                if (cols is null) continue;
                foreach (var col in cols)
                    if (!string.IsNullOrWhiteSpace(col.CaptionKey))
                        allKeys.Add(col.CaptionKey);
            }
            catch { /* JSON không hợp lệ — bỏ qua field này */ }
        }

        if (allKeys.Count == 0) return;

        // Batch-load tất cả keys trong 1 query duy nhất
        var resolved = await _resourceRepo.GetByKeysAsync(allKeys, langCode, ct);

        // Reserialize từng field's PopupColumnsJson với caption đã resolved
        foreach (var field in fields)
        {
            var json = field.LookupConfig?.PopupColumnsJson;
            if (string.IsNullOrWhiteSpace(json)) continue;

            try
            {
                var cols = JsonSerializer.Deserialize<List<PopupColRaw>>(json, jsonOpts);
                if (cols is null || cols.Count == 0) continue;

                // Build output: caption = resolved text (hoặc captionKey nếu không tìm thấy, hoặc caption cũ)
                var output = cols.Select(c =>
                {
                    string caption;
                    if (!string.IsNullOrWhiteSpace(c.CaptionKey))
                        caption = resolved.TryGetValue(c.CaptionKey, out var txt) ? txt : c.CaptionKey;
                    else
                        caption = c.Caption ?? "";   // backward compat: plain text cũ

                    return new
                    {
                        fieldName  = c.FieldName,
                        caption,                     // text đã resolved — Blazor đọc field này
                        captionKey = c.CaptionKey,   // giữ key để debug / re-resolve sau
                        width      = c.Width
                    };
                });

                field.LookupConfig!.PopupColumnsJson = JsonSerializer.Serialize(output);
            }
            catch { /* bỏ qua — Blazor sẽ fallback vào DisplayColumn */ }
        }
    }

    // ── Inner DTOs ────────────────────────────────────────────────────────────

    /// <summary>Raw entry khi parse PopupColumnsJson — hỗ trợ cả schema mới (captionKey) và cũ (caption).</summary>
    private sealed class PopupColRaw
    {
        [JsonPropertyName("fieldName")]  public string  FieldName  { get; init; } = "";
        [JsonPropertyName("captionKey")] public string? CaptionKey { get; init; }
        [JsonPropertyName("caption")]    public string? Caption    { get; init; }
        [JsonPropertyName("width")]      public int     Width      { get; init; }
    }

    // ── Private wrapper cho JSON serialization ────────────────────────────────

    /// <summary>
    /// Wrapper để cache service serialize/deserialize FormMetadata qua JSON.
    /// FormMetadata là record không có parameterless constructor nên cần wrapper.
    /// </summary>
    private sealed class CachedFormMetadata
    {
        public FormMetadata Metadata { get; init; }

        public CachedFormMetadata(FormMetadata metadata)
        {
            Metadata = metadata;
        }

        // Parameterless constructor cho JSON deserialization
        private CachedFormMetadata()
        {
            Metadata = null!;
        }
    }
}
