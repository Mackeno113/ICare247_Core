// File    : DeleteCompany.cs
// Module  : Organization / Companies
// Layer   : Application
// Purpose : Command + Handler — xóa mềm công ty. Chặn khi còn công ty con hoặc phòng ban
//           đang gắn (trả lý do dạng KEY i18n). Enqueue nhật ký khi xóa thành công.

using ICare247.Application.Features.Organization.Companies.Models;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Organization.Companies.Commands;

/// <param name="Id">Công ty cần xóa.</param>
/// <param name="TenantId">Tenant (cho nhật ký).</param>
/// <param name="UserId">Người thao tác.</param>
public sealed record DeleteCompanyCommand(long Id, int TenantId, long? UserId) : IRequest<DeleteCompanyResult>;

public sealed class DeleteCompanyCommandHandler
    : IRequestHandler<DeleteCompanyCommand, DeleteCompanyResult>
{
    private readonly ICongTyRepository _repo;
    private readonly IAuditWriter _audit;

    public DeleteCompanyCommandHandler(ICongTyRepository repo, IAuditWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<DeleteCompanyResult> Handle(DeleteCompanyCommand r, CancellationToken ct)
    {
        var (children, departments) = await _repo.CountDependentsAsync(r.Id, ct);
        if (children > 0)
            return new DeleteCompanyResult(false, "organization.company.error.hasChildren");
        if (departments > 0)
            return new DeleteCompanyResult(false, "organization.company.error.hasDepartments");

        await _repo.DeleteAsync(r.Id, r.UserId, ct);

        _audit.Enqueue(new AuditEvent
        {
            TenantId   = r.TenantId,
            Category   = AuditCategory.MasterData,
            Action     = AuditAction.DataDelete,
            Result     = "Success",
            ObjectType = "TC_CongTy",
            ObjectId   = r.Id.ToString()
        });

        return new DeleteCompanyResult(true, null);
    }
}
