// File    : GetViewByCodeQueryHandler.cs
// Module  : Views
// Layer   : Application
// Purpose : Handler cho GetViewByCodeQuery — cache-aside (L1 → L2 → DB), mirror GetFormByCode.

using ICare247.Application.Constants;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.View;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Views.Queries.GetViewByCode;

/// <summary>
/// Cache-aside: kiểm tra cache trước → miss thì gọi repository → ghi cache → trả kết quả.
/// </summary>
public sealed class GetViewByCodeQueryHandler
    : IRequestHandler<GetViewByCodeQuery, ViewMetadata?>
{
    private readonly IViewRepository _viewRepository;
    private readonly ICacheService _cache;
    private readonly ILogger<GetViewByCodeQueryHandler> _logger;

    public GetViewByCodeQueryHandler(
        IViewRepository viewRepository,
        ICacheService cache,
        ILogger<GetViewByCodeQueryHandler> logger)
    {
        _viewRepository = viewRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ViewMetadata?> Handle(GetViewByCodeQuery request, CancellationToken ct)
    {
        // ── Bước 1: Thử lấy từ cache (version=0 = "latest", event-remove invalidation) ──
        var cacheKey = CacheKeys.View(request.ViewCode, 0, request.LangCode, request.TenantId);

        var cached = await _cache.GetAsync<ViewMetadata>(cacheKey, ct);
        if (cached is not null)
        {
            _logger.LogDebug(
                "Cache hit — ViewCode={ViewCode}, TenantId={TenantId}",
                request.ViewCode, request.TenantId);
            return cached;
        }

        // ── Bước 2: Cache miss → đọc DB ────────────────────────────────────
        var view = await _viewRepository.GetByCodeAsync(
            request.ViewCode, request.TenantId, request.LangCode, ct);

        if (view is null)
            return null;

        // ── Bước 3: Ghi cache ──────────────────────────────────────────────
        await _cache.SetAsync(cacheKey, view, ct: ct);

        return view;
    }
}
