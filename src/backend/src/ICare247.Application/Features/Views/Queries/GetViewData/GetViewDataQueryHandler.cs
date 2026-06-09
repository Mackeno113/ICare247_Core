// File    : GetViewDataQueryHandler.cs
// Module  : Views
// Layer   : Application
// Purpose : Handler — nạp ViewMetadata qua facade rồi đọc dữ liệu qua IViewRepository.GetDataAsync.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Views.Queries.GetViewData;

/// <summary>
/// Lấy metadata View qua <see cref="IConfigCache"/> (cache) → truy dữ liệu bảng nguồn qua repository.
/// </summary>
public sealed class GetViewDataQueryHandler
    : IRequestHandler<GetViewDataQuery, ViewDataResult?>
{
    private readonly IConfigCache _configCache;
    private readonly IViewRepository _viewRepository;

    public GetViewDataQueryHandler(IConfigCache configCache, IViewRepository viewRepository)
    {
        _configCache = configCache;
        _viewRepository = viewRepository;
    }

    public async Task<ViewDataResult?> Handle(GetViewDataQuery request, CancellationToken ct)
    {
        var view = await _configCache.GetViewAsync(request.ViewCode, request.LangCode, request.TenantId, ct);
        if (view is null)
            return null;

        return await _viewRepository.GetDataAsync(view, request.Search, request.Page, request.PageSize, ct);
    }
}
