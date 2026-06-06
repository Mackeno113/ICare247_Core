// File    : CheckMasterDataUsageQuery.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Query soft-check — bản ghi danh mục có đang bị tham chiếu ở đâu không (cho dialog xóa).

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.MasterData.Queries.CheckMasterDataUsage;

/// <param name="FormCode">Mã form danh mục (để resolve Table_Id).</param>
/// <param name="TenantId">Tenant hiện tại.</param>
/// <param name="Id">Giá trị PK của bản ghi cần kiểm tra.</param>
public sealed record CheckMasterDataUsageQuery(string FormCode, int TenantId, object Id)
    : IRequest<IReadOnlyList<ReferenceUsage>>;
