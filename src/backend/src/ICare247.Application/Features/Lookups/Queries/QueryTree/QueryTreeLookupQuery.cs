// File    : QueryTreeLookupQuery.cs
// Module  : Lookup
// Layer   : Application
// Purpose : Query lấy tree lookup rows — flat list có __parentId cho TreeLookupBox.

using MediatR;

namespace ICare247.Application.Features.Lookups.Queries.QueryTree;

/// <summary>
/// Truy vấn rows dạng cây cho TreeLookupBox.
/// Repository inject thêm key <c>__parentId</c> vào mỗi row.
/// </summary>
public sealed record QueryTreeLookupQuery(
    int                         FieldId,
    int                         TenantId,
    Dictionary<string, object?> ContextValues
) : IRequest<IReadOnlyList<IDictionary<string, object>>>;
