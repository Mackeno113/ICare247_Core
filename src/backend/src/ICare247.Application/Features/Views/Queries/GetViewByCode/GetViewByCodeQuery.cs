// File    : GetViewByCodeQuery.cs
// Module  : Views
// Layer   : Application
// Purpose : Query lấy metadata View theo View_Code — dùng cho runtime render lưới/cây.
//           Trả ViewInfoResponse (đã loại Lookup_Sql) chứ KHÔNG trả entity → không lộ SQL ra client.

using MediatR;

namespace ICare247.Application.Features.Views.Queries.GetViewByCode;

/// <summary>
/// Query lấy metadata View (header + cột + action) theo View_Code, trả <see cref="ViewInfoResponse"/>
/// (đã loại <c>Lookup_Sql</c>). Cache-aside (L1 → L2) bên trong facade → miss thì đọc DB.
/// </summary>
public sealed record GetViewByCodeQuery(
    string ViewCode,
    int TenantId,
    string LangCode = "vi"
) : IRequest<ViewInfoResponse?>;
