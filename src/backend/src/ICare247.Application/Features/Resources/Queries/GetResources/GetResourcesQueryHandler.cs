// File    : GetResourcesQueryHandler.cs
// Module  : Resources
// Layer   : Application
// Purpose : Handler — ủy quyền IResourceRepository.GetByKeysAsync (Sys_Resource).

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Resources.Queries.GetResources;

/// <summary>Batch-load resource theo key qua repository (không cache — tập key nhỏ, client cache lại).</summary>
public sealed class GetResourcesQueryHandler
    : IRequestHandler<GetResourcesQuery, IReadOnlyDictionary<string, string>>
{
    private readonly IResourceRepository _resources;

    public GetResourcesQueryHandler(IResourceRepository resources) => _resources = resources;

    public async Task<IReadOnlyDictionary<string, string>> Handle(GetResourcesQuery request, CancellationToken ct)
    {
        if (request.Keys.Count == 0) return new Dictionary<string, string>();
        return await _resources.GetByKeysAsync(request.Keys, request.LangCode, ct);
    }
}
