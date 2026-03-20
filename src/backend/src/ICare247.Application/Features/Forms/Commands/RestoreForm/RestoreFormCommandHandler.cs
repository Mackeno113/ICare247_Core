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
        // Restore cần tìm cả form inactive → không dùng GetByCodeAsync (chỉ tìm active)
        // Tạm dùng ExistsCodeAsync để verify tồn tại, sau đó gọi SetActive
        var exists = await _formRepo.ExistsCodeAsync(request.FormCode, request.TenantId, ct);
        if (!exists)
        {
            throw new KeyNotFoundException(
                $"Form '{request.FormCode}' không tồn tại trong tenant {request.TenantId}.");
        }

        // Cần lấy FormId — mở rộng repository nếu cần, tạm dùng GetByCode không filter active
        // TODO: Thêm GetByCodeIncludeInactiveAsync nếu cần
        // Hiện tại dùng workaround: GetByCodeAsync chỉ lấy active → cần sửa SetActive nhận FormCode

        await _formRepo.SetActiveByCodeAsync(request.FormCode, true, request.TenantId, ct);

        // ── Audit log ───────────────────────────────────────────────────────
        await _auditRepo.InsertAsync(new AuditLogEntry
        {
            ObjectType = "Form",
            ObjectId = 0, // Sẽ cập nhật khi có method lấy FormId từ inactive form
            Action = "RESTORE",
            ChangedBy = request.ChangedBy
        }, ct);

        _logger.LogInformation(
            "Form khôi phục — FormCode={FormCode}, TenantId={TenantId}",
            request.FormCode, request.TenantId);
    }
}
