// File    : GetFilterOptionsQueryHandler.cs
// Module  : Views
// Layer   : Application
// Purpose : Handler — nạp ViewMetadata (cache) rồi nạp options control lọc (static/dynamic + cascade).

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Views.Queries.GetFilterOptions;

/// <summary>Lấy metadata View (cache) → <see cref="IViewRepository.GetFilterOptionsAsync"/>.</summary>
public sealed class GetFilterOptionsQueryHandler
    : IRequestHandler<GetFilterOptionsQuery, IReadOnlyList<FilterOption>?>
{
    private readonly IConfigCache _configCache;
    private readonly IViewRepository _viewRepository;

    public GetFilterOptionsQueryHandler(IConfigCache configCache, IViewRepository viewRepository)
    {
        _configCache = configCache;
        _viewRepository = viewRepository;
    }

    public async Task<IReadOnlyList<FilterOption>?> Handle(GetFilterOptionsQuery request, CancellationToken ct)
    {
        var view = await _configCache.GetViewAsync(request.ViewCode, request.LangCode, request.TenantId, ct);
        if (view is null)
            return null;

        return await _viewRepository.GetFilterOptionsAsync(
            view, request.FilterCode, request.ParentValues, request.LangCode, ct);
    }
}
