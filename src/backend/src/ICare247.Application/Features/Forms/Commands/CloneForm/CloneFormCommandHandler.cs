// File    : CloneFormCommandHandler.cs
// Module  : Forms
// Layer   : Application
// Purpose : Handler cho CloneFormCommand — validate unique, clone form, ghi audit log.

using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Forms.Commands.CloneForm;

public sealed class CloneFormCommandHandler : IRequestHandler<CloneFormCommand, int>
{
    private readonly IFormRepository _formRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly ILogger<CloneFormCommandHandler> _logger;

    public CloneFormCommandHandler(
        IFormRepository formRepo,
        IAuditLogRepository auditRepo,
        ILogger<CloneFormCommandHandler> logger)
    {
        _formRepo = formRepo;
        _auditRepo = auditRepo;
        _logger = logger;
    }

    public async Task<int> Handle(CloneFormCommand request, CancellationToken ct)
    {
        // ── Validate source tồn tại ─────────────────────────────────────────
        var sourceForm = await _formRepo.GetByCodeAsync(
            request.SourceFormCode, request.TenantId, ct)
            ?? throw new KeyNotFoundException(
                $"Form nguồn '{request.SourceFormCode}' không tồn tại trong tenant {request.TenantId}.");

        // ── Validate new code unique ─────────────────────────────────────────
        var exists = await _formRepo.ExistsCodeAsync(request.NewFormCode, request.TenantId, ct);
        if (exists)
        {
            throw new InvalidOperationException(
                $"Form_Code '{request.NewFormCode}' đã tồn tại trong tenant {request.TenantId}.");
        }

        // ── Clone ───────────────────────────────────────────────────────────
        var newFormId = await _formRepo.CloneAsync(
            sourceForm.FormId, request.NewFormCode, request.TenantId, ct);

        // ── Audit log ───────────────────────────────────────────────────────
        await _auditRepo.InsertAsync(new AuditLogEntry
        {
            ObjectType = "Form",
            ObjectId = newFormId,
            Action = "CLONE",
            ChangedBy = request.CreatedBy,
            NewValueJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                SourceFormCode = request.SourceFormCode,
                NewFormCode = request.NewFormCode
            })
        }, ct);

        _logger.LogInformation(
            "Form nhân bản — Source={SourceCode}, New={NewCode}, NewFormId={NewFormId}",
            request.SourceFormCode, request.NewFormCode, newFormId);

        return newFormId;
    }
}
