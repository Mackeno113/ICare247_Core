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
    string? Search      = null,
    bool?   ActiveOnly  = null,
    int     Page        = 1,
    int     PageSize    = 50,
    // Lọc lưới con master-detail (rail workspace): chỉ lấy dòng có [ParentKey] = ParentValue.
    // ParentKey được whitelist theo Sys_Column của bảng ở repository (chống SQL injection).
    string? ParentKey   = null,
    object? ParentValue = null) : IRequest<MasterDataListResult>;
