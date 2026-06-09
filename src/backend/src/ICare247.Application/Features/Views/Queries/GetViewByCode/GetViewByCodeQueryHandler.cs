// File    : GetViewByCodeQueryHandler.cs
// Module  : Views
// Layer   : Application
// Purpose : Handler cho GetViewByCodeQuery — ủy quyền facade IConfigCache (ADR-014), không chọc repo.

using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.View;
using MediatR;

namespace ICare247.Application.Features.Views.Queries.GetViewByCode;

/// <summary>
/// Lấy ViewMetadata qua facade <see cref="IConfigCache"/> (cache-aside L1+L2 bên trong facade).
/// </summary>
public sealed class GetViewByCodeQueryHandler
    : IRequestHandler<GetViewByCodeQuery, ViewMetadata?>
{
    private readonly IConfigCache _configCache;

    public GetViewByCodeQueryHandler(IConfigCache configCache)
        => _configCache = configCache;

    public Task<ViewMetadata?> Handle(GetViewByCodeQuery request, CancellationToken ct)
        => _configCache.GetViewAsync(request.ViewCode, request.LangCode, request.TenantId, ct);
}
