// File    : MetadataEngine.cs
// Module  : Engines
// Layer   : Application
// Purpose : Implementation của IMetadataEngine — load FormMetadata + ResourceMap với hybrid cache.
//           L1 MemoryCache (5 phút) → L2 Redis (30 phút) → DB (FormRepository + ResourceRepository).

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
