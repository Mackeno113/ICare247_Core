// File    : SaveCompany.cs
// Module  : Organization / Companies
// Layer   : Application
// Purpose : Command + Handler — thêm/sửa công ty. Kiểm bắt buộc + trùng Mã + vòng lặp cây,
//           ghi DB (CreatedBy/UpdatedBy theo userId), enqueue nhật ký (non-blocking).
//           Lỗi trả về dạng KEY i18n để UI dịch.

using ICare247.Application.Features.Organization.Companies.Models;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Organization.Companies.Commands;

/// <param name="Id">Null = thêm mới; có giá trị = cập nhật.</param>
/// <param name="Input">Giá trị form.</param>
/// <param name="TenantId">Tenant (cho nhật ký).</param>
/// <param name="UserId">Người thao tác (claim sub) — bơm CreatedBy/UpdatedBy.</param>
public sealed record SaveCompanyCommand(long? Id, CompanyInput Input, int TenantId, long? UserId)
    : IRequest<SaveCompanyResult>;

public sealed class SaveCompanyCommandHandler
    : IRequestHandler<SaveCompanyCommand, SaveCompanyResult>
{
    private readonly ICongTyRepository _repo;
    private readonly IAuditWriter _audit;

    public SaveCompanyCommandHandler(ICongTyRepository repo, IAuditWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<SaveCompanyResult> Handle(SaveCompanyCommand r, CancellationToken ct)
    {
        var i = r.Input;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(i.Ma))  errors.Add("organization.company.error.maRequired");
        if (string.IsNullOrWhiteSpace(i.Ten)) errors.Add("organization.company.error.tenRequired");
        if (i.CapCongTyId <= 0)               errors.Add("organization.company.error.capRequired");

        if (errors.Count == 0)
        {
            if (await _repo.ExistsMaAsync(i.Ma, r.Id, ct))
                errors.Add("organization.company.error.maDuplicate");

            // Chống vòng lặp cây: không cho chọn cha là chính mình hoặc con/cháu.
            if (r.Id is not null && i.CongTyChaId is not null)
            {
                if (i.CongTyChaId == r.Id || await _repo.WouldCreateCycleAsync(r.Id.Value, i.CongTyChaId, ct))
                    errors.Add("organization.company.error.parentCycle");
            }
        }

        if (errors.Count > 0)
            return new SaveCompanyResult(false, null, errors);

        if (r.Id is null)
        {
            var newId = await _repo.InsertAsync(i, r.UserId, ct);
            Audit(r, AuditAction.DataCreate, newId);
            return new SaveCompanyResult(true, newId, []);
        }

        await _repo.UpdateAsync(r.Id.Value, i, r.UserId, ct);
        Audit(r, AuditAction.DataUpdate, r.Id.Value);
        return new SaveCompanyResult(true, r.Id, []);
    }

    /// <summary>Ghi nhật ký 1 thao tác ghi công ty (non-blocking). NewValueJson = giá trị form.</summary>
    private void Audit(SaveCompanyCommand r, string action, long id)
        => _audit.Enqueue(new AuditEvent
        {
            TenantId   = r.TenantId,
            Category   = AuditCategory.MasterData,
            Action     = action,
            Result     = "Success",
            ObjectType = "TC_CongTy",
            ObjectId   = id.ToString(),
            NewValueJson = System.Text.Json.JsonSerializer.Serialize(r.Input)
        });
}
