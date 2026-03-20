// File    : GetFormAuditLogQuery.cs
// Module  : Forms
// Layer   : Application
// Purpose : Query lấy audit log của một form theo Form_Id.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Forms.Queries.GetFormAuditLog;

/// <summary>
/// Query lấy audit log của form từ Sys_Audit_Log WHERE Object_Type='Form'.
/// </summary>
public sealed record GetFormAuditLogQuery(
    int FormId,
    int TenantId,
    int Page = 1,
    int PageSize = 20
) : IRequest<GetFormAuditLogResult>;

/// <summary>Kết quả audit log có phân trang.</summary>
public sealed record GetFormAuditLogResult(
    IReadOnlyList<AuditLogItem> Items,
    int TotalCount,
    int Page,
    int PageSize
);
