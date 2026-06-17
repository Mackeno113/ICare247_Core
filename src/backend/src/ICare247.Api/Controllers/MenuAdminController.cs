// File    : MenuAdminController.cs
// Module  : Admin/Menu
// Layer   : Api
// Purpose : Endpoint Menu Builder — đọc cây + thêm/sửa/xóa node menu (HT_ChucNang, Data DB tenant).
//           Ghi đơn-DB; picker View/Form đọc Config DB qua endpoint riêng (/api/v1/views, /config/forms).
//           Enforce quyền chức năng "administration.menu". userId suy từ claim sub.

using ICare247.Api.Authorization;
using ICare247.Application.Features.Admin.Menu.DeleteMenuNode;
using ICare247.Application.Features.Admin.Menu.GetMenuTree;
using ICare247.Application.Features.Admin.Menu.UpsertMenuNode;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ICare247.Api.Controllers;

/// <summary>Cấu hình cây menu (admin). Enforce chức năng "administration.menu".</summary>
[ApiController]
[Route("api/v1/admin/menu")]
[Authorize]
public sealed class MenuAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public MenuAdminController(IMediator mediator) => _mediator = mediator;

    /// <summary>Toàn bộ cây menu của tenant (gồm node ẩn) để dựng cây + chọn cha.</summary>
    /// <remarks>GET /api/v1/admin/menu/tree</remarks>
    [HttpGet("tree")]
    [RequirePermission("administration.menu", PermissionOp.Xem)]
    public async Task<IActionResult> GetTree(CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetMenuTreeQuery(), ct));

    /// <summary>Thêm node menu mới.</summary>
    /// <remarks>POST /api/v1/admin/menu</remarks>
    [HttpPost]
    [RequirePermission("administration.menu", PermissionOp.Them)]
    public async Task<IActionResult> Create([FromBody] MenuNodeRequest body, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var id = await _mediator.Send(body.ToCommand(null, userId.Value), ct);
        return Ok(new { id });
    }

    /// <summary>Sửa node menu.</summary>
    /// <remarks>PUT /api/v1/admin/menu/{id}</remarks>
    [HttpPut("{id:long}")]
    [RequirePermission("administration.menu", PermissionOp.Sua)]
    public async Task<IActionResult> Update(long id, [FromBody] MenuNodeRequest body, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        await _mediator.Send(body.ToCommand(id, userId.Value), ct);
        return NoContent();
    }

    /// <summary>Xóa (soft-delete) node menu.</summary>
    /// <remarks>DELETE /api/v1/admin/menu/{id}</remarks>
    [HttpDelete("{id:long}")]
    [RequirePermission("administration.menu", PermissionOp.Xoa)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        await _mediator.Send(new DeleteMenuNodeCommand(id, userId.Value), ct);
        return NoContent();
    }

    private long? GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return long.TryParse(raw, out var id) ? id : null;
    }
}

/// <summary>Body thêm/sửa node menu (NodeKind: Group | View | Form).</summary>
public sealed class MenuNodeRequest
{
    /// <summary>Group | View | Form — quyết định route + đối tượng quyền.</summary>
    public string NodeKind { get; set; } = "Group";

    /// <summary>Tên hiển thị (bắt buộc).</summary>
    public string Ten { get; set; } = "";

    /// <summary>Node cha (null = gốc).</summary>
    public long? ParentId { get; set; }

    /// <summary>ViewCode (View) / FormCode (Form). Null với Group.</summary>
    public string? ObjectCode { get; set; }

    /// <summary>Mã phân hệ (tùy chọn).</summary>
    public string? Module { get; set; }

    /// <summary>Icon (tùy chọn).</summary>
    public string? Icon { get; set; }

    /// <summary>Thứ tự trong cấp.</summary>
    public int ThuTu { get; set; }

    /// <summary>Bật/tắt node.</summary>
    public bool KichHoat { get; set; } = true;

    /// <summary>Dựng command từ body + Id (null=thêm) + userId.</summary>
    public UpsertMenuNodeCommand ToCommand(long? id, long userId)
        => new(id, NodeKind, Ten, ParentId, ObjectCode, Module, Icon, ThuTu, KichHoat, userId);
}
