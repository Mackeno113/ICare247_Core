// File    : CreateFormCommandHandler.cs
// Module  : Forms
// Layer   : Application
// Purpose : Handler cho CreateFormCommand — validate unique, insert form, ghi audit log.

using System.Text.Json;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Forms.Commands.CreateForm;

public sealed class CreateFormCommandHandler : IRequestHandler<CreateFormCommand, int>
{
    private readonly IFormRepository _formRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly ILogger<CreateFormCommandHandler> _logger;

    public CreateFormCommandHandler(
        IFormRepository formRepo,
        IAuditLogRepository auditRepo,
        ILogger<CreateFormCommandHandler> logger)
    {
        _formRepo = formRepo;
        _auditRepo = auditRepo;
        _logger = logger;
    }

    public async Task<int> Handle(CreateFormCommand request, CancellationToken ct)
    {
        // ── Kiểm tra Form_Code unique ───────────────────────────────────────
        var exists = await _formRepo.ExistsCodeAsync(request.FormCode, request.TenantId, ct);
        if (exists)
        {
            throw new InvalidOperationException(
                $"Form_Code '{request.FormCode}' đã tồn tại trong tenant {request.TenantId}.");
        }

        // ── Insert form ─────────────────────────────────────────────────────
        var formId = await _formRepo.CreateAsync(new FormCreateParams
        {
            FormCode = request.FormCode,
            TableId = request.TableId,
            Platform = request.Platform,
            LayoutEngine = request.LayoutEngine,
            Description = request.Description,
            CreatedBy = request.CreatedBy
        }, request.TenantId, ct);

        // ── Ghi audit log ───────────────────────────────────────────────────
        await _auditRepo.InsertAsync(new AuditLogEntry
        {
            ObjectType = "Form",
            ObjectId = formId,
            Action = "INSERT",
            ChangedBy = request.CreatedBy,
            NewValueJson = JsonSerializer.Serialize(new
            {
                request.FormCode,
                request.TableId,
                request.Platform,
                request.LayoutEngine,
                Version = 1
            })
        }, ct);

        _logger.LogInformation(
            "Form tạo mới thành công — FormId={FormId}, FormCode={FormCode}",
            formId, request.FormCode);

        return formId;
    }
}
