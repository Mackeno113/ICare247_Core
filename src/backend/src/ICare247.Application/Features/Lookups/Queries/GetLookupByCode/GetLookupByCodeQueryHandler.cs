// File    : GetLookupByCodeQueryHandler.cs
// Module  : Lookup
// Layer   : Application
// Purpose : Handler — cache-aside cho GetLookupByCodeQuery.

using ICare247.Application.Constants;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Lookup;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Lookups.Queries.GetLookupByCode;

public sealed class GetLookupByCodeQueryHandler
    : IRequestHandler<GetLookupByCodeQuery, IReadOnlyList<LookupItem>>
{
    private readonly ILookupRepository _repo;
    private readonly ICacheService     _cache;
    private readonly ILogger<GetLookupByCodeQueryHandler> _logger;

    public GetLookupByCodeQueryHandler(
        ILookupRepository repo,
        ICacheService     cache,
        ILogger<GetLookupByCodeQueryHandler> logger)
    {
        _repo   = repo;
        _cache  = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LookupItem>> Handle(
        GetLookupByCodeQuery request, CancellationToken ct)
    {
        // Lookup data ít thay đổi → cache TTL dài (L1 10 phút)
        var key = CacheKeys.Lookup(request.LookupCode, request.TenantId, request.LangCode);

        var cached = await _cache.GetAsync<List<LookupItem>>(key, ct);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit lookup {Code} tenant {TenantId}", request.LookupCode, request.TenantId);
            return cached;
        }

        var items = await _repo.GetByCodeAsync(
            request.LookupCode, request.TenantId, request.LangCode, ct);

        if (items.Count > 0)
            await _cache.SetAsync(key, items.ToList(), ct: ct);

        return items;
    }
}
