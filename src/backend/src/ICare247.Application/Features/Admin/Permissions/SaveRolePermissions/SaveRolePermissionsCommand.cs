// File    : SaveRolePermissionsCommand.cs
// Module  : Admin/Permissions
// Layer   : Application
// Purpose : Command lưu (upsert) quyền của 1 vai trò từ màn Phân quyền.

using MediatR;

namespace ICare247.Application.Features.Admin.Permissions.SaveRolePermissions;

/// <param name="RoleId">Vai trò được cấu hình.</param>
/// <param name="Items">Trạng thái cờ từng node (gửi từ TreeList).</param>
/// <param name="UserId">Người thao tác (claim sub) — ghi CreatedBy/UpdatedBy.</param>
public sealed record SaveRolePermissionsCommand(
    long RoleId, IReadOnlyList<SavePermItem> Items, long UserId) : IRequest<Unit>;

public sealed class SaveRolePermissionsCommandHandler : IRequestHandler<SaveRolePermissionsCommand, Unit>
{
    private readonly Interfaces.IPermissionAdminRepository _repo;
    private readonly Interfaces.INavigationCache _navCache;
    private readonly Interfaces.ITenantContext _tenant;

    public SaveRolePermissionsCommandHandler(
        Interfaces.IPermissionAdminRepository repo,
        Interfaces.INavigationCache navCache,
        Interfaces.ITenantContext tenant)
    {
        _repo = repo;
        _navCache = navCache;
        _tenant = tenant;
    }

    /// <summary>Lưu quyền rồi xóa cache menu tenant. Sự kiện theo sau: lần điều hướng kế nạp menu mới.</summary>
    public async Task<Unit> Handle(SaveRolePermissionsCommand r, CancellationToken ct)
    {
        await _repo.SaveRolePermissionsAsync(r.RoleId, r.Items, r.UserId, ct);
        _navCache.InvalidateTenant(_tenant.TenantId);
        return Unit.Value;
    }
}
