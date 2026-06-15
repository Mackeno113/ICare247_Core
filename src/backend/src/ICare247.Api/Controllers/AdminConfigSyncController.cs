// File    : AdminConfigSyncController.cs
// Module  : Admin/ConfigSync
// Layer   : Api
// Purpose : Action super admin "Cập nhật cấu hình từ master" (CFGSYNC-3, spec 16 §9). Đồng bộ config
//           master → Config DB tenant hiện tại. Gate [RequirePermission] — SUPERADMIN tự được bỏ qua.

using ICare247.Api.Authorization;
using ICare247.Application.Features.Admin.ConfigSync;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ICare247.Api.Controllers;

/// <summary>
/// Đồng bộ cấu hình từ master cho tenant hiện tại. Chức năng enforce: "administration.config-sync"
/// (chỉ super admin; mọi role khác cần được cấp quyền tường minh). Một chiều master → tenant.
/// </summary>
[ApiController]
[Route("api/v1/admin/config-sync")]
[Authorize]
public sealed class AdminConfigSyncController : ControllerBase
{
    /// <summary>Mã chức năng dùng cho gate quyền (SUPERADMIN bỏ qua kiểm).</summary>
    private const string FuncCode = "administration.config-sync";

    private readonly IMediator _mediator;

    public AdminConfigSyncController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Xem trước (dry-run) diff đồng bộ — KHÔNG ghi xuống tenant. Trả số dòng I/U/deactivate/skip theo bảng.
    /// </summary>
    /// <remarks>POST /api/v1/admin/config-sync/preview</remarks>
    [HttpPost("preview")]
    [RequirePermission(FuncCode, PermissionOp.Xem)]
    public async Task<IActionResult> Preview(CancellationToken ct = default)
        => Ok(await _mediator.Send(new SyncConfigCommand(DryRun: true, TriggeredBy: CurrentUser()), ct));

    /// <summary>Áp đồng bộ thật (transaction/tenant). Trả tổng hợp kết quả + chi tiết từng bảng.</summary>
    /// <remarks>POST /api/v1/admin/config-sync</remarks>
    [HttpPost("")]
    [RequirePermission(FuncCode, PermissionOp.Sua)]
    public async Task<IActionResult> Run(CancellationToken ct = default)
        => Ok(await _mediator.Send(new SyncConfigCommand(DryRun: false, TriggeredBy: CurrentUser()), ct));

    /// <summary>Tên người dùng hiện tại (cho log sync) — ưu tiên unique_name/name, fallback claim sub.</summary>
    private string? CurrentUser()
        => User.FindFirst("unique_name")?.Value
           ?? User.Identity?.Name
           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
           ?? User.FindFirst("sub")?.Value;
}
