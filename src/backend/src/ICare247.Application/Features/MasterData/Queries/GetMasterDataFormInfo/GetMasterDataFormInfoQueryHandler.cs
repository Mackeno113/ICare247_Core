// File    : GetMasterDataFormInfoQueryHandler.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Handler — delegate sang IMasterDataRepository.GetFormInfoAsync.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.MasterData.Queries.GetMasterDataFormInfo;

public sealed class GetMasterDataFormInfoQueryHandler
    : IRequestHandler<GetMasterDataFormInfoQuery, MasterDataFormInfo?>
{
    private readonly IMasterDataRepository _repo;

    public GetMasterDataFormInfoQueryHandler(IMasterDataRepository repo) => _repo = repo;

    /// <summary>Đọc metadata bảng đích + cột. Sự kiện theo sau: API trả cho Blazor để render lưới/form.</summary>
    public Task<MasterDataFormInfo?> Handle(GetMasterDataFormInfoQuery request, CancellationToken ct) =>
        _repo.GetFormInfoAsync(request.FormCode, request.TenantId, ct);
}
