// File    : GetLookupByCodeQuery.cs
// Module  : Lookup
// Layer   : Application
// Purpose : Query lấy danh sách items của một Sys_Lookup code.

using ICare247.Domain.Entities.Lookup;
using MediatR;

namespace ICare247.Application.Features.Lookups.Queries.GetLookupByCode;

/// <summary>
/// Lấy danh sách lookup items theo code.
/// Kết quả đã resolve label theo <c>LangCode</c>.
/// </summary>
/// <param name="LookupCode">VD: 'GENDER', 'MARITAL_STATUS'</param>
/// <param name="TenantId">Tenant hiện tại. Fallback về global (0) nếu không có riêng.</param>
/// <param name="LangCode">Ngôn ngữ để resolve label. Mặc định 'vi'.</param>
public sealed record GetLookupByCodeQuery(
    string LookupCode,
    int    TenantId,
    string LangCode = "vi"
) : IRequest<IReadOnlyList<LookupItem>>;
