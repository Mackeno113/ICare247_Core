// File    : SaveUserRolesCommand.cs
// Module  : Admin/Users
// Layer   : Application
// Purpose : Command ghi lại danh sách vai trò của 1 user (tab Vai trò màn Người dùng).
//           Vai trò quyết định cả quyền chức năng (HT_VaiTro_Quyen) lẫn phạm vi công ty kế thừa
//           (HT_VaiTro_CongTy) — cùng kế thừa động nên chỉ cần ghi map, không copy gì thêm.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Users.SaveUserRoles;

/// <param name="Id">User được gán.</param>
/// <param name="VaiTroIds">Toàn bộ vai trò user thuộc về sau khi lưu (thêm thiếu, xóa mềm thừa).</param>
/// <param name="ActorId">Người thao tác (claim sub).</param>
public sealed record SaveUserRolesCommand(
    long Id, IReadOnlyList<long> VaiTroIds, long ActorId) : IRequest<Unit>;

public sealed class SaveUserRolesCommandHandler : IRequestHandler<SaveUserRolesCommand, Unit>
{
    private readonly IUserAdminRepository _repo;
    private readonly INavigationCache _navCache;
    private readonly ITenantContext _tenant;

    public SaveUserRolesCommandHandler(
        IUserAdminRepository repo, INavigationCache navCache, ITenantContext tenant)
    {
        _repo = repo;
        _navCache = navCache;
        _tenant = tenant;
    }

    /// <summary>Ghi map vai trò rồi xóa cache menu tenant (menu user đổi theo vai trò).
    /// Sự kiện theo sau: lần điều hướng kế của user nạp menu + quyền mới.</summary>
    public async Task<Unit> Handle(SaveUserRolesCommand r, CancellationToken ct)
    {
        await _repo.SaveUserRolesAsync(r.Id, r.VaiTroIds, r.ActorId, ct);
        _navCache.InvalidateTenant(_tenant.TenantId);
        return Unit.Value;
    }
}
