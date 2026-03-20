// File    : UpdateFormCommandHandler.cs
// Module  : Forms
// Layer   : Application
// Purpose : Handler cho UpdateFormCommand — update form, invalidate cache, ghi audit log.

using System.Text.Json;
using ICare247.Application.Constants;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICare247.Application.Features.Forms.Commands.UpdateForm;

public sealed class UpdateFormCommandHandler : IRequestHandler<UpdateFormCommand>
{
    private readonly IFormRepository _formRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly ICacheService _cache;
    private readonly ILogger<UpdateFormCommandHandler> _logger;

    public UpdateFormCommandHandler(
        IFormRepository formRepo,
        IAuditLogRepository auditRepo,
        ICacheService cache,
        ILogger<UpdateFormCommandHandler> logger)
    {
        _formRepo = formRepo;
        _auditRepo = auditRepo;
        _cache = cache;
        _logger = logger;
    }

    public async Task Handle(UpdateFormCommand request, CancellationToken ct)
    {
        // ── Lấy form hiện tại ───────────────────────────────────────────────
        var form = await _formRepo.GetByCodeAsync(request.FormCode, request.TenantId, ct)
            ?? throw new KeyNotFoundException(
                $"Form '{request.FormCode}' không tồn tại trong tenant {request.TenantId}.");

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            form.FormCode, form.Version, form.Platform
        });

        // ── Update ──────────────────────────────────────────────────────────
        await _formRepo.UpdateAsync(new FormUpdateParams
        {
            FormId = form.FormId,
            TableId = request.TableId,
            Platform = request.Platform,
            LayoutEngine = request.LayoutEngine,
            Description = request.Description,
            UpdatedBy = request.UpdatedBy
        }, request.TenantId, ct);

        // ── Invalidate cache ────────────────────────────────────────────────
        var cacheKey = CacheKeys.Form(request.FormCode, 0, "vi", request.Platform, request.TenantId);
        await _cache.RemoveAsync(cacheKey, ct);

        // ── Ghi audit log ───────────────────────────────────────────────────
        await _auditRepo.InsertAsync(new AuditLogEntry
        {
            ObjectType = "Form",
            ObjectId = form.FormId,
            Action = "UPDATE",
            ChangedBy = request.UpdatedBy,
            OldValueJson = oldSnapshot,
            NewValueJson = JsonSerializer.Serialize(new
            {
                request.FormCode,
                request.TableId,
                request.Platform,
                request.LayoutEngine
            })
        }, ct);

        _logger.LogInformation(
            "Form cập nhật — FormCode={FormCode}, TenantId={TenantId}",
            request.FormCode, request.TenantId);
    }
}
