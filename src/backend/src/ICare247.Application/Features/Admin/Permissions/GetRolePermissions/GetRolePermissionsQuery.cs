// File    : GetRolePermissionsQuery.cs
// Module  : Admin/Permissions
// Layer   : Application
// Purpose : Query cây chức năng + cờ quyền của 1 vai trò (ma trận màn Phân quyền).

using MediatR;

namespace ICare247.Application.Features.Admin.Permissions.GetRolePermissions;

public sealed record GetRolePermissionsQuery(long RoleId) : IRequest<IReadOnlyList<RolePermNodeDto>>;

public sealed class GetRolePermissionsQueryHandler
    : IRequestHandler<GetRolePermissionsQuery, IReadOnlyList<RolePermNodeDto>>
{
    private readonly Interfaces.IPermissionAdminRepository _repo;
    public GetRolePermissionsQueryHandler(Interfaces.IPermissionAdminRepository repo) => _repo = repo;

    /// <summary>Lấy ma trận quyền. Sự kiện theo sau: DxTreeList vẽ cây + tick theo cờ vai trò.</summary>
    public Task<IReadOnlyList<RolePermNodeDto>> Handle(GetRolePermissionsQuery r, CancellationToken ct)
        => _repo.GetRolePermissionsAsync(r.RoleId, ct);
}
