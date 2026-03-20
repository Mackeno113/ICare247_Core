// File    : GetFormByCodeQuery.cs
// Module  : Forms
// Layer   : Application
// Purpose : Query lấy FormMetadata đầy đủ theo Form_Code — dùng cho runtime render.

using ICare247.Domain.Entities.Form;
using MediatR;

namespace ICare247.Application.Features.Forms.Queries.GetFormByCode;

/// <summary>
/// Query lấy <see cref="FormMetadata"/> (aggregate root) theo Form_Code.
/// Kiểm tra cache trước (L1 → L2) → miss thì đọc DB.
/// </summary>
public sealed record GetFormByCodeQuery(
    string FormCode,
    int TenantId,
    string LangCode = "vi",
    string Platform = "web"
) : IRequest<FormMetadata?>;
