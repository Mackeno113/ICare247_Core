// File    : GetViewFilteredDataQuery.cs
// Module  : Views
// Layer   : Application
// Purpose : Query thực thi lưới nâng cao (Source_Type='Sp'/'Sql') với tham số từ panel lọc trái.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Views.Queries.GetViewFilteredData;

/// <summary>
/// Thực thi View nguồn SP/SQL với bộ giá trị lọc người dùng nhập (key = Filter_Code).
/// Nạp metadata (cache) rồi bind whitelist tham số → gọi SP/SQL. Trả <c>null</c> nếu View không tồn tại.
/// </summary>
public sealed record GetViewFilteredDataQuery(
    string ViewCode,
    int TenantId,
    IReadOnlyDictionary<string, string?> FilterValues,
    string LangCode = "vi"
) : IRequest<ViewDataResult?>;
