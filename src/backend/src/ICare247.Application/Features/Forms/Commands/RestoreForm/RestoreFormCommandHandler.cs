// File    : RestoreFormCommandHandler.cs
// Module  : Forms
// Layer   : Application
// Purpose : Handler cho RestoreFormCommand — set Is_Active=1, ghi audit log.

using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Forms.Commands.RestoreForm;

public sealed class RestoreFormCommandHandler : IRequestHandler<RestoreFormCommand>
{
    private readonly IFormRepository _formRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly ILogger<RestoreFormCommandHandler> _logger;

    public RestoreFormCommandHandler(
        IFormRepository formRepo,
        IAuditLogRepository auditRepo,
        ILogger<RestoreFormCommandHandler> logger)
    {
        _formRepo = formRepo;
        _auditRepo = auditRepo;
        _logger = logger;
    }

    public async Task Handle(RestoreFormCommand request, CancellationToken ct)
    {
        // Restore cần tìm cả form inactive → GetIdByCodeAsync (KHÔNG lọc Is_Active).
        // Trả về Form_Id để ghi audit đúng đối tượng (không còn ObjectId=0 như trước).
        var formId = await _formRepo.GetIdByCodeAsync(request.FormCode, request.TenantId, ct)
            ?? throw new KeyNotFoundException(
                $"Form '{request.FormCode}' không tồn tại trong tenant {request.TenantId}.");

        await _formRepo.SetActiveByCodeAsync(request.FormCode, true, request.TenantId, ct);

        // ── Audit log ───────────────────────────────────────────────────────
        await _auditRepo.InsertAsync(new AuditLogEntry
        {
            ObjectType = "Form",
            ObjectId = formId,
            Action = "RESTORE",
            ChangedBy = request.ChangedBy
        }, ct);

        _logger.LogInformation(
            "Form khôi phục — FormCode={FormCode}, TenantId={TenantId}",
            request.FormCode, request.TenantId);
    }
}
