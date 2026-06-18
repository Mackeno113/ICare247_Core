// File    : ConfigCache.cs
// Module  : Engines
// Layer   : Application
// Purpose : Implementation IConfigCache (ADR-014) — facade đọc config qua cache-aside L1+L2.
//           Bọc MetadataEngine (form metadata) + IResourceRepository (i18n) + ILookupRepository
//           (Sys_Lookup options). Key gắn slot :v{version} sẵn cho version-stamp (CC-4a).

using System.Collections.Concurrent;
using ICare247.Application.Constants;
using ICare247.Application.Interfaces;
using ICare247.Domain.Engine;
using ICare247.Domain.Entities.Form;
using ICare247.Domain.Entities.Lookup;
using ICare247.Domain.Entities.Permission;
using ICare247.Domain.Entities.View;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Engines;

/// <summary>
/// Facade đọc config qua cache cache-aside (L1 Memory + L2 Redis) — xem ADR-014.
/// <para>
/// Form metadata: ủy quyền cho <see cref="IMetadataEngine"/> (đã có cache riêng) — KHÔNG cache lại
/// (tránh double-cache + double invalidation). i18n resource map + lookup options: facade tự
/// cache-aside qua <see cref="ICacheService"/> với key có slot <c>:v{version}</c>.
/// </para>
/// </summary>
/// <remarks>
/// version hiện cố định <c>0</c> (1 instance, dựa event-remove của MetadataEngine). Khi scale-out
/// (CC-4a) đổi <see cref="CurrentVersion"/> thành đọc từ Redis <c>cfgver:*</c>. Stampede lock +
/// negative cache sẽ bổ sung ở CC-0c. Permission đợi repo ở CC-3 — tạm trả <c>null</c>.
/// </remarks>
public sealed class ConfigCache : IConfigCache
{
    private readonly IMetadataEngine     _metadataEngine;
    private readonly IResourceRepository _resourceRepo;
    private readonly ILookupRepository   _lookupRepo;
    private readonly IViewRepository     _viewRepo;
    private readonly ICacheService       _cache;
    private readonly ICacheVersion       _version;
    private readonly ILogger<ConfigCache> _logger;

    // Config đổi hiếm → TTL dài hơn data; vẫn là lưới an toàn dưới version-stamp/event-remove.
    private static readonly TimeSpan L1Ttl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan L2Ttl = TimeSpan.FromMinutes(30);

    // Negative cache: kết quả rỗng (key/lookup chưa tồn tại) cache TTL ngắn để config mới
    // xuất hiện sớm (cửa sổ stale ~30s) thay vì đợi hết TTL dài.
    private static readonly TimeSpan NegTtl = TimeSpan.FromSeconds(30);

    // Version-stamp theo tenant (CC-4a) — "cưỡng chế làm mới" bump version → mọi key cũ vô hiệu.
    private int CurrentVersion(int tenantId) => _version.Get(tenantId);

    // Ngôn ngữ phổ biến — xóa cache cho tất cả khi invalidate (giống MetadataEngine).
    private static readonly string[] KnownLangCodes = ["vi", "en"];

    // Stampede lock per-key (in-process): khi nhiều request cùng miss 1 key, chỉ 1 request
    // chạm DB, số còn lại chờ rồi đọc lại cache. Key space hữu hạn (tenant×scope×lang) nên
    // không xóa entry — SemaphoreSlim không cấp OS handle nếu không truy cập AvailableWaitHandle.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    public ConfigCache(
        IMetadataEngine     metadataEngine,
        IResourceRepository resourceRepo,
        ILookupRepository   lookupRepo,
        IViewRepository     viewRepo,
        ICacheService       cache,
        ICacheVersion       version,
        ILogger<ConfigCache> logger)
    {
        _metadataEngine = metadataEngine;
        _resourceRepo   = resourceRepo;
        _lookupRepo     = lookupRepo;
        _viewRepo       = viewRepo;
        _cache          = cache;
        _version        = version;
        _logger         = logger;
    }

    /// <inheritdoc />
    public Task<FormMetadata?> GetFormMetadataAsync(
        string formCode, string langCode, string platform, int tenantId,
        CancellationToken ct = default)
        // MetadataEngine đã cache + invalidate — facade chỉ ủy quyền, không cache lại.
        => _metadataEngine.GetFormMetadataAsync(formCode, langCode, platform, tenantId, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> GetResourceMapAsync(
        string scope, string langCode, int tenantId, CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.ConfigResourceMap(scope, langCode, tenantId, CurrentVersion(tenantId));

        return await GetOrLoadAsync(
            cacheKey,
            label: $"resource Scope={scope}, Lang={langCode}, TenantId={tenantId}",
            // GetByFormAsync nạp cả {scope}.* lẫn global sys.* trong 1 query.
            // Sao chép sang Dictionary cụ thể (OrdinalIgnoreCase) để serialize qua cache.
            loader: async token => new Dictionary<string, string>(
                await _resourceRepo.GetByFormAsync(scope, langCode, tenantId, token),
                StringComparer.OrdinalIgnoreCase),
            isEmpty: m => m.Count == 0,
            ct: ct);
    }

    /// <inheritdoc />
    public async Task<string?> ResolveKeyAsync(
        string key, string langCode, int tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        // Scope = đoạn đầu trước dấu '.' — theo convention key {table|sys}.val.{column}.unique.
        // GetResourceMapAsync(scope) nạp luôn cả global sys.* nên 1 lần đủ cho cả key chính + fallback.
        var scope = key.Split('.', 2)[0];
        var map = await GetResourceMapAsync(scope, langCode, tenantId, ct);

        return map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LookupItem>> GetLookupOptionsAsync(
        string lookupCode, string langCode, int tenantId, CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.ConfigLookup(lookupCode, langCode, tenantId, CurrentVersion(tenantId));

        return await GetOrLoadAsync(
            cacheKey,
            label: $"lookup Code={lookupCode}, Lang={langCode}, TenantId={tenantId}",
            loader: async token =>
                (await _lookupRepo.GetByCodeAsync(lookupCode, tenantId, langCode, token)).ToList(),
            isEmpty: list => list.Count == 0,
            ct: ct);
    }

    /// <inheritdoc />
    public async Task InvalidateLookupAsync(string lookupCode, int tenantId)
    {
        var version = CurrentVersion(tenantId);
        foreach (var lang in KnownLangCodes)
        {
            var key = CacheKeys.ConfigLookup(lookupCode, lang, tenantId, version);
            await _cache.RemoveAsync(key);
        }

        _logger.LogInformation(
            "ConfigCache invalidated (lookup) — Code={Code}, TenantId={TenantId}",
            lookupCode, tenantId);
    }

    /// <inheritdoc />
    public Task<FormPermission?> GetFormPermissionsAsync(
        int formId, int tenantId, CancellationToken ct = default)
    {
        // TODO (CC-3): inject permission repo + cache theo ConfigPermission key.
        // Hiện chưa có repo Sys_Permission → trả null; caller xử lý deny-by-default.
        return Task.FromResult<FormPermission?>(null);
    }

    /// <inheritdoc />
    public async Task<ViewMetadata?> GetViewAsync(
        string viewCode, string langCode, int tenantId, CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.View(viewCode, CurrentVersion(tenantId), langCode, tenantId);

        var cached = await _cache.GetAsync<ViewMetadata>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var view = await _viewRepo.GetByCodeAsync(viewCode, tenantId, langCode, ct);

        // Không cache kết quả null (View không tồn tại) — tránh giữ negative cho aggregate lớn;
        // mã sai là trường hợp hiếm, để miss thẳng DB.
        if (view is null)
            return null;

        await _cache.SetAsync(cacheKey, view, memoryTtl: L1Ttl, redisTtl: L2Ttl, ct: ct);
        return view;
    }

    /// <inheritdoc />
    public async Task InvalidateViewAsync(string viewCode, int tenantId)
    {
        var version = CurrentVersion(tenantId);
        foreach (var lang in KnownLangCodes)
        {
            var key = CacheKeys.View(viewCode, version, lang, tenantId);
            await _cache.RemoveAsync(key);
        }

        _logger.LogInformation(
            "ConfigCache invalidated (view) — Code={Code}, TenantId={TenantId}",
            viewCode, tenantId);
    }

    // ── Cache-aside dùng chung: stampede lock per-key + negative cache ──────────

    /// <summary>
    /// Cache-aside có chống stampede: check cache → miss thì giành lock per-key → double-check →
    /// load từ source → cache (TTL ngắn nếu rỗng, TTL dài nếu có dữ liệu).
    /// </summary>
    /// <typeparam name="T">Kiểu giá trị cache (class — Dictionary, List...).</typeparam>
    /// <param name="cacheKey">Key đã build (gồm tenant/lang/version).</param>
    /// <param name="label">Mô tả ngắn cho log khi miss.</param>
    /// <param name="loader">Hàm load từ repository khi cache miss.</param>
    /// <param name="isEmpty">Đánh giá kết quả rỗng → dùng negative TTL.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task<T> GetOrLoadAsync<T>(
        string cacheKey,
        string label,
        Func<CancellationToken, Task<T>> loader,
        Func<T, bool> isEmpty,
        CancellationToken ct) where T : class
    {
        var cached = await _cache.GetAsync<T>(cacheKey, ct);
        if (cached is not null)
            return cached;

        // Stampede lock: chỉ 1 request load 1 key tại 1 thời điểm trong process này.
        var gate = Locks.GetOrAdd(cacheKey, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            // Double-check: request khác có thể đã nạp xong trong lúc ta chờ lock.
            cached = await _cache.GetAsync<T>(cacheKey, ct);
            if (cached is not null)
                return cached;

            _logger.LogDebug("ConfigCache miss ({Label})", label);

            var loaded = await loader(ct);

            // Rỗng → negative cache TTL ngắn; có dữ liệu → TTL config dài.
            var ttl = isEmpty(loaded) ? NegTtl : L1Ttl;
            var redisTtl = isEmpty(loaded) ? NegTtl : L2Ttl;
            await _cache.SetAsync(cacheKey, loaded, memoryTtl: ttl, redisTtl: redisTtl, ct: ct);

            return loaded;
        }
        finally
        {
            gate.Release();
        }
    }
}
