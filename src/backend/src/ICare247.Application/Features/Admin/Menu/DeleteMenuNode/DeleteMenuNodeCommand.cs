// File    : DeleteMenuNodeCommand.cs
// Module  : Admin/Menu
// Layer   : Application
// Purpose : Soft-delete 1 node menu. Chặn xóa node base (LaHeThong=1) và node đang có con.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Menu.DeleteMenuNode;

/// <param name="Id">Node cần xóa.</param>
/// <param name="UserId">Người thao tác.</param>
public sealed record DeleteMenuNodeCommand(long Id, long UserId) : IRequest<Unit>;

public sealed class DeleteMenuNodeCommandHandler : IRequestHandler<DeleteMenuNodeCommand, Unit>
{
    private readonly IMenuAdminRepository _repo;
    private readonly INavigationCache _navCache;
    private readonly ITenantContext _tenant;

    public DeleteMenuNodeCommandHandler(
        IMenuAdminRepository repo, INavigationCache navCache, ITenantContext tenant)
    {
        _repo = repo;
        _navCache = navCache;
        _tenant = tenant;
    }

    public async Task<Unit> Handle(DeleteMenuNodeCommand r, CancellationToken ct)
    {
        var isSystem = await _repo.IsSystemNodeAsync(r.Id, ct);
        if (isSystem is null)
            throw new InvalidOperationException($"Không tìm thấy node #{r.Id}.");
        if (isSystem.Value)
            throw new InvalidOperationException("Không thể xóa node hệ thống (LaHeThong=1). Có thể tắt KichHoat.");

        if (await _repo.CountActiveChildrenAsync(r.Id, ct) > 0)
            throw new InvalidOperationException("Node đang có node con — di chuyển/xóa node con trước.");

        await _repo.SoftDeleteAsync(r.Id, r.UserId, ct);
        _navCache.InvalidateTenant(_tenant.TenantId);
        return Unit.Value;
    }
}
