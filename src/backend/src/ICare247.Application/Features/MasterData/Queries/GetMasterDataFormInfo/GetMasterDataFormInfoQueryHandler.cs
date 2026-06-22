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
    private readonly IHookStoreCatalog     _hookCatalog;

    public GetMasterDataFormInfoQueryHandler(IMasterDataRepository repo, IHookStoreCatalog hookCatalog)
    {
        _repo        = repo;
        _hookCatalog = hookCatalog;
    }

    /// <summary>Đọc metadata bảng đích + cột. Sự kiện theo sau: API trả cho Blazor để render lưới/form.</summary>
    public async Task<MasterDataFormInfo?> Handle(GetMasterDataFormInfoQuery request, CancellationToken ct)
    {
        var info = await _repo.GetFormInfoAsync(request.FormCode, request.TenantId, ct);

        // Nạp sẵn cache hook store NGAY khi mở list/form → save sau đó không phải query OBJECT_ID (ADR-029).
        if (info is not null)
            await _hookCatalog.GetAsync(info.TableName, request.TenantId, ct);

        return info;
    }
}
