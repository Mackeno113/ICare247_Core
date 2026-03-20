// File    : GetFormsListQuery.cs
// Module  : Forms
// Layer   : Application
// Purpose : Query lấy danh sách form có phân trang và filter.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Forms.Queries.GetFormsList;

/// <summary>
/// Query lấy danh sách form với phân trang, filter theo Platform/Table/IsActive, search theo FormCode.
/// </summary>
public sealed record GetFormsListQuery(
    int TenantId,
    string? Platform = null,
    int? TableId = null,
    bool? IsActive = null,
    string? SearchText = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<GetFormsListResult>;

/// <summary>Kết quả trả về gồm danh sách + tổng số record.</summary>
public sealed record GetFormsListResult(
    IReadOnlyList<FormListItem> Items,
    int TotalCount,
    int Page,
    int PageSize
);
