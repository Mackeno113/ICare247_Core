// File    : GetViewsListQueryHandler.cs
// Module  : Views
// Layer   : Application
// Purpose : Handler cho GetViewsListQuery — delegate xuống IViewRepository (không cache, list đổi thường xuyên).

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Views.Queries.GetViewsList;

/// <summary>
/// Lấy danh sách View từ DB qua repository. Không cache vì danh sách thay đổi thường xuyên.
/// </summary>
public sealed class GetViewsListQueryHandler
    : IRequestHandler<GetViewsListQuery, GetViewsListResult>
{
    private readonly IViewRepository _viewRepository;

    public GetViewsListQueryHandler(IViewRepository viewRepository)
        => _viewRepository = viewRepository;

    public async Task<GetViewsListResult> Handle(GetViewsListQuery request, CancellationToken ct)
    {
        var (items, totalCount) = await _viewRepository.GetListAsync(
            request.TenantId, request.LangCode, request.IsActive,
            request.SearchText, request.Page, request.PageSize, ct);

        return new GetViewsListResult(items, totalCount, request.Page, request.PageSize);
    }
}
