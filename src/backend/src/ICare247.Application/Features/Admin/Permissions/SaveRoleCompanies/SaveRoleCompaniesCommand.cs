// File    : SaveRoleCompaniesCommand.cs
// Module  : Admin/Permissions
// Layer   : Application
// Purpose : Command ghi lại tập công ty của 1 vai trò (HT_VaiTro_CongTy). Kế thừa ĐỘNG:
//           mọi user thuộc vai trò thấy thay đổi ngay ở lần tải switcher kế — không copy, không rác.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Permissions.SaveRoleCompanies;

/// <param name="RoleId">Vai trò được cấu hình.</param>
/// <param name="CongTyIds">Toàn bộ công ty thuộc phạm vi vai trò sau khi lưu (WYSIWYG từ cây).</param>
/// <param name="UserId">Người thao tác (claim sub).</param>
public sealed record SaveRoleCompaniesCommand(
    long RoleId, IReadOnlyList<long> CongTyIds, long UserId) : IRequest<Unit>;

public sealed class SaveRoleCompaniesCommandHandler : IRequestHandler<SaveRoleCompaniesCommand, Unit>
{
    private readonly IPermissionAdminRepository _repo;

    public SaveRoleCompaniesCommandHandler(IPermissionAdminRepository repo) => _repo = repo;

    /// <summary>Ghi diff tập công ty vai trò. Sự kiện theo sau: switcher các user thuộc vai trò áp ngay.</summary>
    public async Task<Unit> Handle(SaveRoleCompaniesCommand r, CancellationToken ct)
    {
        await _repo.SaveRoleCompaniesAsync(r.RoleId, r.CongTyIds, r.UserId, ct);
        return Unit.Value;
    }
}
