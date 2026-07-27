// File    : GetRoleDepartmentsQuery.cs
// Module  : Admin/Permissions
// Layer   : Application
// Purpose : Query cây phòng ban + cờ đã gán của 1 vai trò (phần "Phạm vi phòng ban" màn Phân quyền).

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Permissions.GetRoleDepartments;

/// <param name="RoleId">Vai trò cần xem phạm vi phòng ban.</param>
public sealed record GetRoleDepartmentsQuery(long RoleId) : IRequest<IReadOnlyList<RoleDepartmentNodeDto>>;

public sealed class GetRoleDepartmentsQueryHandler
    : IRequestHandler<GetRoleDepartmentsQuery, IReadOnlyList<RoleDepartmentNodeDto>>
{
    private readonly IPermissionAdminRepository _repo;

    public GetRoleDepartmentsQueryHandler(IPermissionAdminRepository repo) => _repo = repo;

    /// <summary>Đọc cây phòng ban + cờ gán. Sự kiện theo sau: FE render cây checkbox (không cascade).</summary>
    public Task<IReadOnlyList<RoleDepartmentNodeDto>> Handle(GetRoleDepartmentsQuery r, CancellationToken ct)
        => _repo.GetRoleDepartmentsAsync(r.RoleId, ct);
}
