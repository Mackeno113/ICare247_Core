// File    : GetMasterDataFormInfoQuery.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Query lấy thông tin bảng đích + cột của 1 form danh mục (cho lưới + form).

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.MasterData.Queries.GetMasterDataFormInfo;

/// <param name="FormCode">Mã form danh mục.</param>
/// <param name="TenantId">Tenant hiện tại.</param>
public sealed record GetMasterDataFormInfoQuery(string FormCode, int TenantId)
    : IRequest<MasterDataFormInfo?>;
