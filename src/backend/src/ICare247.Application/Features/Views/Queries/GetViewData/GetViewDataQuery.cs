// File    : GetViewDataQuery.cs
// Module  : Views
// Layer   : Application
// Purpose : Query lấy trang dữ liệu cho một View (Source_Type='Table') — search + paging.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Views.Queries.GetViewData;

/// <summary>
/// Lấy dữ liệu lưới của View theo View_Code: nạp metadata (cache) rồi SELECT cột Data từ bảng nguồn.
/// Trả <c>null</c> nếu View không tồn tại.
/// </summary>
public sealed record GetViewDataQuery(
    string ViewCode,
    int TenantId,
    string LangCode = "vi",
    string? Search = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<ViewDataResult?>;
