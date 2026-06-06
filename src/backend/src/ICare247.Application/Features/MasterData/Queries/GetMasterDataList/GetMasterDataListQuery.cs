// File    : GetMasterDataListQuery.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Query list bản ghi danh mục có search + active filter + paging.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.MasterData.Queries.GetMasterDataList;

public sealed record GetMasterDataListQuery(
    string FormCode,
    int    TenantId,
    string? Search    = null,
    bool?   ActiveOnly = null,
    int     Page      = 1,
    int     PageSize  = 50) : IRequest<MasterDataListResult>;
