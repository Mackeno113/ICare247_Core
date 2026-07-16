// File    : GetRoleCompaniesQuery.cs
// Module  : Admin/Permissions
// Layer   : Application
// Purpose : Query cây công ty + cờ đã gán của 1 vai trò (phần "Phạm vi công ty" màn Phân quyền).

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Permissions.GetRoleCompanies;

/// <param name="RoleId">Vai trò cần xem phạm vi công ty.</param>
public sealed record GetRoleCompaniesQuery(long RoleId) : IRequest<IReadOnlyList<RoleCompanyNodeDto>>;

public sealed class GetRoleCompaniesQueryHandler
    : IRequestHandler<GetRoleCompaniesQuery, IReadOnlyList<RoleCompanyNodeDto>>
{
    private readonly IPermissionAdminRepository _repo;

    public GetRoleCompaniesQueryHandler(IPermissionAdminRepository repo) => _repo = repo;

    /// <summary>Đọc cây công ty + cờ gán. Sự kiện theo sau: FE render cây checkbox WYSIWYG.</summary>
    public Task<IReadOnlyList<RoleCompanyNodeDto>> Handle(GetRoleCompaniesQuery r, CancellationToken ct)
        => _repo.GetRoleCompaniesAsync(r.RoleId, ct);
}
