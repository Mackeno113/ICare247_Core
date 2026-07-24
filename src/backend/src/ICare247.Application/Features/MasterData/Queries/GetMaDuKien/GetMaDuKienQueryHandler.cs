// File    : GetMaDuKienQueryHandler.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Handler — delegate sang IMasterDataRepository.GetPreviewCodeAsync.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.MasterData.Queries.GetMaDuKien;

public sealed class GetMaDuKienQueryHandler
    : IRequestHandler<GetMaDuKienQuery, MaPreviewResult?>
{
    private readonly IMasterDataRepository _repo;

    public GetMaDuKienQueryHandler(IMasterDataRepository repo) => _repo = repo;

    /// <summary>
    /// Trả mã dự kiến + metadata quy tắc. Sự kiện theo sau: Blazor điền vào ô mã (read-only nếu
    /// <c>AllowManual = false</c>) và ghi nhớ <c>SourceFields</c> để xin lại mã khi field nguồn đổi.
    /// </summary>
    public Task<MaPreviewResult?> Handle(GetMaDuKienQuery r, CancellationToken ct)
        => _repo.GetPreviewCodeAsync(r.FormCode, r.TenantId, r.Values, ct);
}
