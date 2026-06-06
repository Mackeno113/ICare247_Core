// File    : GetMasterDataRecordQueryHandler.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Handler — delegate sang IMasterDataRepository.GetByIdAsync.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.MasterData.Queries.GetMasterDataRecord;

public sealed class GetMasterDataRecordQueryHandler
    : IRequestHandler<GetMasterDataRecordQuery, IDictionary<string, object?>?>
{
    private readonly IMasterDataRepository _repo;

    public GetMasterDataRecordQueryHandler(IMasterDataRepository repo) => _repo = repo;

    /// <summary>Lấy 1 bản ghi. Sự kiện theo sau: form Sửa load sẵn dữ liệu.</summary>
    public Task<IDictionary<string, object?>?> Handle(GetMasterDataRecordQuery r, CancellationToken ct) =>
        _repo.GetByIdAsync(r.FormCode, r.TenantId, r.Id, ct);
}
