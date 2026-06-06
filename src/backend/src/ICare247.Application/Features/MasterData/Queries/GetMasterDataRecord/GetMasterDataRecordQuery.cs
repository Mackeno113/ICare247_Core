// File    : GetMasterDataRecordQuery.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Query lấy đầy đủ 1 bản ghi danh mục theo PK (cho form Sửa).

using MediatR;

namespace ICare247.Application.Features.MasterData.Queries.GetMasterDataRecord;

public sealed record GetMasterDataRecordQuery(string FormCode, int TenantId, object Id)
    : IRequest<IDictionary<string, object?>?>;
