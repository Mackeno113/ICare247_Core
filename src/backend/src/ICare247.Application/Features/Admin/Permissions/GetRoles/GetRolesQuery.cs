// File    : GetRolesQuery.cs
// Module  : Admin/Permissions
// Layer   : Application
// Purpose : Query danh sách vai trò cho màn Phân quyền.

using MediatR;

namespace ICare247.Application.Features.Admin.Permissions.GetRoles;

public sealed record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>;

public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly Interfaces.IPermissionAdminRepository _repo;
    public GetRolesQueryHandler(Interfaces.IPermissionAdminRepository repo) => _repo = repo;

    /// <summary>Lấy vai trò. Sự kiện theo sau: đổ DxComboBox chọn vai trò ở màn Phân quyền.</summary>
    public Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery r, CancellationToken ct) => _repo.GetRolesAsync(ct);
}
