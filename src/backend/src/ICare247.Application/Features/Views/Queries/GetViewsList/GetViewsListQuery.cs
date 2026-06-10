// File    : GetViewsListQuery.cs
// Module  : Views
// Layer   : Application
// Purpose : Query lấy danh sách View (header tóm tắt) có phân trang + filter — cho màn chọn View.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Views.Queries.GetViewsList;

/// <summary>
/// Query lấy danh sách View với phân trang, filter theo IsActive, search theo View_Code/Title.
/// </summary>
public sealed record GetViewsListQuery(
    int TenantId,
    string LangCode = "vi",
    bool? IsActive = null,
    string? SearchText = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<GetViewsListResult>;

/// <summary>Kết quả trả về gồm danh sách + tổng số record.</summary>
public sealed record GetViewsListResult(
    IReadOnlyList<ViewListItem> Items,
    int TotalCount,
    int Page,
    int PageSize
);
