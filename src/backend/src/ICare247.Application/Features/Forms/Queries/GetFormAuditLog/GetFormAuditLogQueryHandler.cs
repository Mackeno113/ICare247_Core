// File    : GetFormAuditLogQueryHandler.cs
// Module  : Forms
// Layer   : Application
// Purpose : Handler cho GetFormAuditLogQuery — đọc Sys_Audit_Log.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Forms.Queries.GetFormAuditLog;

public sealed class GetFormAuditLogQueryHandler
    : IRequestHandler<GetFormAuditLogQuery, GetFormAuditLogResult>
{
    private readonly IAuditLogRepository _auditRepo;

    public GetFormAuditLogQueryHandler(IAuditLogRepository auditRepo)
    {
        _auditRepo = auditRepo;
    }

    public async Task<GetFormAuditLogResult> Handle(
        GetFormAuditLogQuery request, CancellationToken ct)
    {
        var (items, totalCount) = await _auditRepo.GetByObjectAsync(
            "Form", request.FormId, request.Page, request.PageSize, ct);

        return new GetFormAuditLogResult(items, totalCount, request.Page, request.PageSize);
    }
}
