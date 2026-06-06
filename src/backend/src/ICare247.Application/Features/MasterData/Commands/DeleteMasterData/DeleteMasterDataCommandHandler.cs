// File    : DeleteMasterDataCommandHandler.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Handler xóa cứng — soft-check tham chiếu trước; chỉ DELETE khi sạch.

using ICare247.Application.Features.MasterData.Models;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.MasterData.Commands.DeleteMasterData;

public sealed class DeleteMasterDataCommandHandler
    : IRequestHandler<DeleteMasterDataCommand, MasterDataDeleteResult>
{
    private readonly IMasterDataRepository  _repo;
    private readonly IReferenceCheckService _refCheck;
    private readonly ILogger<DeleteMasterDataCommandHandler> _logger;

    public DeleteMasterDataCommandHandler(
        IMasterDataRepository repo,
        IReferenceCheckService refCheck,
        ILogger<DeleteMasterDataCommandHandler> logger)
    {
        _repo     = repo;
        _refCheck = refCheck;
        _logger   = logger;
    }

    /// <summary>
    /// Soft-check tham chiếu (lớp enforce server-side, phòng race) → DELETE nếu sạch.
    /// Sự kiện theo sau: API trả 409 + danh sách nơi dùng nếu bị chặn; 200 nếu xóa xong.
    /// </summary>
    public async Task<MasterDataDeleteResult> Handle(DeleteMasterDataCommand r, CancellationToken ct)
    {
        var info = await _repo.GetFormInfoAsync(r.FormCode, r.TenantId, ct)
                   ?? throw new InvalidOperationException($"MasterData: form '{r.FormCode}' không tồn tại.");

        var usages = await _refCheck.CheckUsageAsync(info.TableId, r.Id, ct);
        if (usages.Count > 0)
        {
            _logger.LogWarning("DeleteMasterData CHẶN '{Form}' Id={Id} — {Places} nơi tham chiếu.",
                r.FormCode, r.Id, usages.Count);
            return new MasterDataDeleteResult(Success: false, BlockedBy: usages);
        }

        await _repo.DeleteAsync(r.FormCode, r.TenantId, r.Id, ct);
        _logger.LogInformation("DeleteMasterData OK '{Form}' Id={Id}.", r.FormCode, r.Id);
        return new MasterDataDeleteResult(Success: true, BlockedBy: []);
    }
}
