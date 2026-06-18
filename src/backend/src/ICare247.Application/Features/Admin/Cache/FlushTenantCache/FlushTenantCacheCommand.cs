// File    : FlushTenantCacheCommand.cs
// Module  : Admin/Cache
// Layer   : Application
// Purpose : "Cưỡng chế làm mới" — vô hiệu TOÀN BỘ cache của tenant hiện tại trong 1 thao tác:
//           (1) cache DÙNG CHUNG (config: form/view/lookup/resource) qua bump version-stamp;
//           (2) cache RIÊNG TỪNG USER trên server (menu điều hướng) qua InvalidateTenant.
//           Cache riêng từng user phía trình duyệt do client tự xóa khi tải lại cưỡng chế.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Cache.FlushTenantCache;

/// <summary>Xóa toàn bộ cache của tenant hiện tại (dùng chung + per-user trên server).</summary>
public sealed record FlushTenantCacheCommand() : IRequest;

public sealed class FlushTenantCacheCommandHandler : IRequestHandler<FlushTenantCacheCommand>
{
    private readonly ICacheVersion    _version;
    private readonly INavigationCache _navCache;
    private readonly ITenantContext   _tenant;

    public FlushTenantCacheCommandHandler(
        ICacheVersion version, INavigationCache navCache, ITenantContext tenant)
    {
        _version  = version;
        _navCache = navCache;
        _tenant   = tenant;
    }

    public Task Handle(FlushTenantCacheCommand request, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        _version.Bump(tenantId);            // cache dùng chung (config/form/view/lookup/resource)
        _navCache.InvalidateTenant(tenantId); // cache menu per-user trên server
        return Task.CompletedTask;
    }
}
