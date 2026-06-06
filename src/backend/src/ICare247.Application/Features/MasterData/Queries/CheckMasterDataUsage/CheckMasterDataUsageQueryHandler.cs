// File    : CheckMasterDataUsageQueryHandler.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Handler — resolve Table_Id từ form rồi gọi IReferenceCheckService.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.MasterData.Queries.CheckMasterDataUsage;

public sealed class CheckMasterDataUsageQueryHandler
    : IRequestHandler<CheckMasterDataUsageQuery, IReadOnlyList<ReferenceUsage>>
{
    private readonly IMasterDataRepository  _repo;
    private readonly IReferenceCheckService _refCheck;

    public CheckMasterDataUsageQueryHandler(
        IMasterDataRepository repo, IReferenceCheckService refCheck)
    {
        _repo     = repo;
        _refCheck = refCheck;
    }

    /// <summary>
    /// Soft-check tham chiếu. Sự kiện theo sau: dialog xóa hiện cảnh báo + ẩn nút Xóa nếu bị dùng.
    /// </summary>
    public async Task<IReadOnlyList<ReferenceUsage>> Handle(
        CheckMasterDataUsageQuery r, CancellationToken ct)
    {
        var info = await _repo.GetFormInfoAsync(r.FormCode, r.TenantId, ct);
        if (info is null) return [];
        return await _refCheck.CheckUsageAsync(info.TableId, r.Id, ct);
    }
}
