// File    : AdminPermissionController.cs
// Module  : Admin/Permissions
// Layer   : Api
// Purpose : Endpoint cấu hình phân quyền (màn Phân quyền): danh sách vai trò + đọc/lưu ma trận
//           quyền theo vai trò (HT_VaiTro_Quyen). Bắt buộc JWT; userId suy từ claim sub.

using ICare247.Application.Features.Admin.Permissions;
using ICare247.Application.Features.Admin.Permissions.GetRolePermissions;
using ICare247.Application.Features.Admin.Permissions.GetRoles;
using ICare247.Application.Features.Admin.Permissions.SaveRolePermissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ICare247.Api.Controllers;

/// <summary>Cấu hình phân quyền theo vai trò (admin). TODO(AUTHZ-SEC): siết quyền truy cập chính màn này.</summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize]
public sealed class AdminPermissionController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminPermissionController(IMediator mediator) => _mediator = mediator;

    /// <summary>Danh sách vai trò để chọn.</summary>
    /// <remarks>GET /api/v1/admin/roles</remarks>
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetRolesQuery(), ct));

    /// <summary>Cây chức năng + cờ quyền hiện tại của 1 vai trò.</summary>
    /// <remarks>GET /api/v1/admin/roles/{roleId}/permissions</remarks>
    [HttpGet("roles/{roleId:long}/permissions")]
    public async Task<IActionResult> GetRolePermissions(long roleId, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetRolePermissionsQuery(roleId), ct));

    /// <summary>Lưu (upsert) quyền của vai trò.</summary>
    /// <remarks>PUT /api/v1/admin/roles/{roleId}/permissions</remarks>
    [HttpPut("roles/{roleId:long}/permissions")]
    public async Task<IActionResult> SaveRolePermissions(
        long roleId, [FromBody] SavePermissionsRequest body, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        await _mediator.Send(new SaveRolePermissionsCommand(roleId, body.Items ?? [], userId.Value), ct);
        return NoContent();
    }

    private long? GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return long.TryParse(raw, out var id) ? id : null;
    }
}

/// <summary>Body PUT lưu quyền vai trò.</summary>
public sealed class SavePermissionsRequest
{
    /// <summary>Trạng thái cờ từng node chức năng.</summary>
    public List<SavePermItem>? Items { get; set; }
}
