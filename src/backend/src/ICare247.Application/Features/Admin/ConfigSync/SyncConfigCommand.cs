// File    : SyncConfigCommand.cs
// Module  : Admin/ConfigSync
// Layer   : Application
// Purpose : Command kích hoạt đồng bộ config master → tenant (CFGSYNC-3). Gọi IConfigSyncService
//           rồi invalidate cache điều hướng (menu có thể đổi sau khi cấu hình thay đổi).

using ICare247.Application.ConfigSync;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.ConfigSync;

/// <param name="DryRun">true = chỉ xem trước diff, không ghi xuống tenant.</param>
/// <param name="TriggeredBy">Người kích hoạt (username super admin) — ghi vào log sync.</param>
public sealed record SyncConfigCommand(bool DryRun, string? TriggeredBy) : IRequest<ConfigSyncResult>;

/// <summary>Thực thi đồng bộ; áp thật thì xóa cache menu tenant để lần điều hướng kế nạp lại.</summary>
public sealed class SyncConfigCommandHandler : IRequestHandler<SyncConfigCommand, ConfigSyncResult>
{
    private readonly IConfigSyncService _sync;
    private readonly INavigationCache _navCache;
    private readonly ITenantContext _tenant;

    public SyncConfigCommandHandler(
        IConfigSyncService sync, INavigationCache navCache, ITenantContext tenant)
    {
        _sync = sync;
        _navCache = navCache;
        _tenant = tenant;
    }

    /// <summary>
    /// Chạy <see cref="IConfigSyncService.SyncAsync"/>. Sự kiện theo sau: nếu áp thật thành công →
    /// invalidate menu tenant. (Invalidate ConfigCache theo version-stamp toàn tenant = CC-4, làm sau.)
    /// </summary>
    public async Task<ConfigSyncResult> Handle(SyncConfigCommand r, CancellationToken ct)
    {
        var result = await _sync.SyncAsync(
            new ConfigSyncOptions { DryRun = r.DryRun, TriggeredBy = r.TriggeredBy }, ct);

        if (!r.DryRun && result.Status == "Success")
            _navCache.InvalidateTenant(_tenant.TenantId);

        return result;
    }
}
