// File    : GetViewByCodeQuery.cs
// Module  : Views
// Layer   : Application
// Purpose : Query lấy ViewMetadata đầy đủ theo View_Code — dùng cho runtime render lưới/cây.

using ICare247.Domain.Entities.View;
using MediatR;

namespace ICare247.Application.Features.Views.Queries.GetViewByCode;

/// <summary>
/// Query lấy <see cref="ViewMetadata"/> (header + cột + action) theo View_Code.
/// Cache-aside (L1 → L2) → miss thì đọc DB qua <c>IViewRepository</c>.
/// </summary>
public sealed record GetViewByCodeQuery(
    string ViewCode,
    int TenantId,
    string LangCode = "vi"
) : IRequest<ViewMetadata?>;
