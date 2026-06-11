// File    : GetViewFilteredDataQueryHandler.cs
// Module  : Views
// Layer   : Application
// Purpose : Handler — nạp ViewMetadata (cache) rồi thực thi SP/SQL với tham số lọc whitelist.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Views.Queries.GetViewFilteredData;

/// <summary>
/// Lấy metadata View qua <see cref="IConfigCache"/> → thực thi nguồn SP/SQL với tham số panel lọc.
/// </summary>
public sealed class GetViewFilteredDataQueryHandler
    : IRequestHandler<GetViewFilteredDataQuery, ViewDataResult?>
{
    private readonly IConfigCache _configCache;
    private readonly IViewRepository _viewRepository;

    public GetViewFilteredDataQueryHandler(IConfigCache configCache, IViewRepository viewRepository)
    {
        _configCache = configCache;
        _viewRepository = viewRepository;
    }

    public async Task<ViewDataResult?> Handle(GetViewFilteredDataQuery request, CancellationToken ct)
    {
        var view = await _configCache.GetViewAsync(request.ViewCode, request.LangCode, request.TenantId, ct);
        if (view is null)
            return null;

        return await _viewRepository.GetFilteredDataAsync(view, request.FilterValues, ct);
    }
}
