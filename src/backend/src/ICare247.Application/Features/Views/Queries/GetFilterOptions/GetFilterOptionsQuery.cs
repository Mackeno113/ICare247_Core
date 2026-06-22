// File    : GetFilterOptionsQuery.cs
// Module  : Views
// Layer   : Application
// Purpose : Query nạp options cho 1 control lọc cascade (Combo/MultiSelect/Radio) — ADR-030.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Views.Queries.GetFilterOptions;

/// <summary>
/// Nạp options cho control lọc <paramref name="FilterCode"/> của View. static → Sys_Lookup;
/// dynamic → Lookup_Sql bind giá trị filter cha (<paramref name="ParentValues"/>, whitelist Depends_On)
/// + token ngữ cảnh. Trả <c>null</c> nếu View không tồn tại.
/// </summary>
public sealed record GetFilterOptionsQuery(
    string ViewCode,
    string FilterCode,
    int TenantId,
    IReadOnlyDictionary<string, string?> ParentValues,
    string LangCode = "vi"
) : IRequest<IReadOnlyList<FilterOption>?>;
