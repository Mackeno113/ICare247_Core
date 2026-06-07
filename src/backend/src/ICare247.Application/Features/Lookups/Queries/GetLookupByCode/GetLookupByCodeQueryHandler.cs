// File    : GetLookupByCodeQueryHandler.cs
// Module  : Lookup
// Layer   : Application
// Purpose : Handler GetLookupByCodeQuery — đọc Sys_Lookup options qua IConfigCache (ADR-014).

using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Lookup;
using MediatR;

namespace ICare247.Application.Features.Lookups.Queries.GetLookupByCode;

public sealed class GetLookupByCodeQueryHandler
    : IRequestHandler<GetLookupByCodeQuery, IReadOnlyList<LookupItem>>
{
    private readonly IConfigCache _config;

    public GetLookupByCodeQueryHandler(IConfigCache config) => _config = config;

    /// <summary>
    /// Trả options của một lookup code — caching (L1+L2, stampede, negative) nằm trong facade.
    /// Sự kiện theo sau: UI render ComboBox/RadioGroup từ danh sách trả về.
    /// </summary>
    public Task<IReadOnlyList<LookupItem>> Handle(
        GetLookupByCodeQuery request, CancellationToken ct)
        => _config.GetLookupOptionsAsync(
            request.LookupCode, request.LangCode, request.TenantId, ct);
}
