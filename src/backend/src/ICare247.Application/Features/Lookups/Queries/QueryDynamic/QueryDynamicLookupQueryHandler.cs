// File    : QueryDynamicLookupQueryHandler.cs
// Module  : Lookup
// Layer   : Application
// Purpose : Handler cho QueryDynamicLookupQuery — delegate sang IDynamicLookupRepository.
//           Không cache vì dynamic data thay đổi theo context (cascading).

using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Lookups.Queries.QueryDynamic;

public sealed class QueryDynamicLookupQueryHandler
    : IRequestHandler<QueryDynamicLookupQuery, IReadOnlyList<IDictionary<string, object>>>
{
    private readonly IDynamicLookupRepository                    _repo;
    private readonly ILogger<QueryDynamicLookupQueryHandler>     _logger;

    public QueryDynamicLookupQueryHandler(
        IDynamicLookupRepository repo,
        ILogger<QueryDynamicLookupQueryHandler> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IDictionary<string, object>>> Handle(
        QueryDynamicLookupQuery request, CancellationToken ct)
    {
        _logger.LogDebug(
            "QueryDynamicLookup — FieldId={FieldId} TenantId={TenantId} ContextKeys=[{Keys}]",
            request.FieldId,
            request.TenantId,
            string.Join(", ", request.ContextValues.Keys));

        var rows = await _repo.QueryAsync(
            request.FieldId, request.TenantId, request.ContextValues, ct);

        _logger.LogDebug(
            "QueryDynamicLookup OK — FieldId={FieldId} → {Count} rows",
            request.FieldId, rows.Count);

        return rows;
    }
}
