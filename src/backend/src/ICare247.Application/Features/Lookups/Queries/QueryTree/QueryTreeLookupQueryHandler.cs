// File    : QueryTreeLookupQueryHandler.cs
// Module  : Lookup
// Layer   : Application
// Purpose : Handler cho QueryTreeLookupQuery — delegate sang IDynamicLookupRepository.QueryTreeAsync.

using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Lookups.Queries.QueryTree;

public sealed class QueryTreeLookupQueryHandler
    : IRequestHandler<QueryTreeLookupQuery, IReadOnlyList<IDictionary<string, object>>>
{
    private readonly IDynamicLookupRepository                 _repo;
    private readonly ILogger<QueryTreeLookupQueryHandler>     _logger;

    public QueryTreeLookupQueryHandler(
        IDynamicLookupRepository repo,
        ILogger<QueryTreeLookupQueryHandler> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IDictionary<string, object>>> Handle(
        QueryTreeLookupQuery request, CancellationToken ct)
    {
        _logger.LogDebug(
            "QueryTreeLookup — FieldId={FieldId} TenantId={TenantId}",
            request.FieldId, request.TenantId);

        var rows = await _repo.QueryTreeAsync(
            request.FieldId, request.TenantId, request.ContextValues, ct);

        _logger.LogDebug(
            "QueryTreeLookup OK — FieldId={FieldId} → {Count} rows",
            request.FieldId, rows.Count);

        return rows;
    }
}
