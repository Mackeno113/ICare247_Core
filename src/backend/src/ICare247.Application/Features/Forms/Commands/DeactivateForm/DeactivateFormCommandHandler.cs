// File    : DeactivateFormCommandHandler.cs
// Module  : Forms
// Layer   : Application
// Purpose : Handler cho DeactivateFormCommand — set Is_Active=0, invalidate cache, audit log.

using ICare247.Application.Constants;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Forms.Commands.DeactivateForm;

public sealed class DeactivateFormCommandHandler : IRequestHandler<DeactivateFormCommand>
{
    private readonly IFormRepository _formRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly ICacheService _cache;
    private readonly ILogger<DeactivateFormCommandHandler> _logger;

    public DeactivateFormCommandHandler(
        IFormRepository formRepo,
        IAuditLogRepository auditRepo,
        ICacheService cache,
        ILogger<DeactivateFormCommandHandler> logger)
    {
        _formRepo = formRepo;
        _auditRepo = auditRepo;
        _cache = cache;
        _logger = logger;
    }

    public async Task Handle(DeactivateFormCommand request, CancellationToken ct)
    {
        var form = await _formRepo.GetByCodeAsync(request.FormCode, request.TenantId, ct: ct)
            ?? throw new KeyNotFoundException(
                $"Form '{request.FormCode}' không tồn tại trong tenant {request.TenantId}.");

        // ── Set Is_Active = 0 ───────────────────────────────────────────────
        await _formRepo.SetActiveAsync(form.FormId, false, request.TenantId, ct);

        // ── Invalidate cache ────────────────────────────────────────────────
        var cacheKey = CacheKeys.Form(request.FormCode, 0, "vi", form.Platform, request.TenantId);
        await _cache.RemoveAsync(cacheKey, ct);

        // ── Audit log ───────────────────────────────────────────────────────
        await _auditRepo.InsertAsync(new AuditLogEntry
        {
            ObjectType = "Form",
            ObjectId = form.FormId,
            Action = "DEACTIVATE",
            ChangedBy = request.ChangedBy
        }, ct);

        _logger.LogInformation(
            "Form vô hiệu hóa — FormCode={FormCode}, TenantId={TenantId}",
            request.FormCode, request.TenantId);
    }
}
