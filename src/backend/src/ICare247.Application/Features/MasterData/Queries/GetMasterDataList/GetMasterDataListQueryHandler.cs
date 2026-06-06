// File    : GetMasterDataListQueryHandler.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Handler — delegate sang IMasterDataRepository.GetListAsync.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.MasterData.Queries.GetMasterDataList;

public sealed class GetMasterDataListQueryHandler
    : IRequestHandler<GetMasterDataListQuery, MasterDataListResult>
{
    private readonly IMasterDataRepository _repo;

    public GetMasterDataListQueryHandler(IMasterDataRepository repo) => _repo = repo;

    /// <summary>Lấy trang dữ liệu. Sự kiện theo sau: API trả lưới danh sách cho Blazor.</summary>
    public Task<MasterDataListResult> Handle(GetMasterDataListQuery r, CancellationToken ct) =>
        _repo.GetListAsync(r.FormCode, r.TenantId, r.Search, r.ActiveOnly, r.Page, r.PageSize, ct);
}
