// File    : AdminUserController.cs
// Module  : Admin/Users
// Layer   : Api
// Purpose : Endpoint màn Người dùng (admin): CRUD tài khoản, đặt lại mật khẩu, gán vai trò,
//           gán công ty truy cập (gán riêng — quyền kế thừa theo vai trò chỉ hiển thị).
//           Bắt buộc JWT; enforce chức năng "administration.users"; actorId suy từ claim sub.

using ICare247.Api.Authorization;
using ICare247.Application.Features.Admin.Users.DeleteUser;
using ICare247.Application.Features.Admin.Users.GetUserCompanies;
using ICare247.Application.Features.Admin.Users.GetUserDetail;
using ICare247.Application.Features.Admin.Users.GetUsers;
using ICare247.Application.Features.Admin.Users.ResetUserPassword;
using ICare247.Application.Features.Admin.Users.SaveUser;
using ICare247.Application.Features.Admin.Users.SaveUserCompanies;
using ICare247.Application.Features.Admin.Users.SaveUserRoles;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ICare247.Api.Controllers;

/// <summary>Quản trị người dùng tenant. Enforce: chức năng "administration.users".</summary>
[ApiController]
[Route("api/v1/admin/users")]
[Authorize]
public sealed class AdminUserController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminUserController(IMediator mediator) => _mediator = mediator;

    /// <summary>Danh sách người dùng cho lưới.</summary>
    /// <remarks>GET /api/v1/admin/users</remarks>
    [HttpGet]
    [RequirePermission("administration.users", PermissionOp.Xem)]
    public async Task<IActionResult> GetUsers(CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetUsersQuery(), ct));

    /// <summary>Chi tiết user + toàn bộ vai trò với cờ đã gán.</summary>
    /// <remarks>GET /api/v1/admin/users/{id}</remarks>
    [HttpGet("{id:long}")]
    [RequirePermission("administration.users", PermissionOp.Xem)]
    public async Task<IActionResult> GetUser(long id, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetUserDetailQuery(id), ct));

    /// <summary>Tạo user mới (bắt buộc mật khẩu — băm PBKDF2 server-side).</summary>
    /// <remarks>POST /api/v1/admin/users</remarks>
    [HttpPost]
    [RequirePermission("administration.users", PermissionOp.Them)]
    public async Task<IActionResult> CreateUser([FromBody] SaveUserRequest body, CancellationToken ct = default)
    {
        var actorId = GetUserId();
        if (actorId is null) return Unauthorized();

        var id = await _mediator.Send(new SaveUserCommand(
            null, body.Ma ?? "", body.TenDangNhap ?? "", body.MatKhau, body.TrangThai ?? "HoatDong",
            body.LaQuanTri, body.KichHoatMobile, body.HetHanTaiKhoan, body.DoiMatKhauLanSau,
            actorId.Value), ct);
        return Ok(new { id });
    }

    /// <summary>Cập nhật thông tin user (không đụng mật khẩu).</summary>
    /// <remarks>PUT /api/v1/admin/users/{id}</remarks>
    [HttpPut("{id:long}")]
    [RequirePermission("administration.users", PermissionOp.Sua)]
    public async Task<IActionResult> UpdateUser(
        long id, [FromBody] SaveUserRequest body, CancellationToken ct = default)
    {
        var actorId = GetUserId();
        if (actorId is null) return Unauthorized();

        await _mediator.Send(new SaveUserCommand(
            id, body.Ma ?? "", body.TenDangNhap ?? "", null, body.TrangThai ?? "HoatDong",
            body.LaQuanTri, body.KichHoatMobile, body.HetHanTaiKhoan, body.DoiMatKhauLanSau,
            actorId.Value), ct);
        return NoContent();
    }

    /// <summary>Đặt lại mật khẩu (kèm mở khóa đăng nhập sai).</summary>
    /// <remarks>PUT /api/v1/admin/users/{id}/password</remarks>
    [HttpPut("{id:long}/password")]
    [RequirePermission("administration.users", PermissionOp.Sua)]
    public async Task<IActionResult> ResetPassword(
        long id, [FromBody] AdminResetPasswordRequest body, CancellationToken ct = default)
    {
        var actorId = GetUserId();
        if (actorId is null) return Unauthorized();

        await _mediator.Send(new ResetUserPasswordCommand(
            id, body.MatKhauMoi ?? "", body.DoiMatKhauLanSau, actorId.Value), ct);
        return NoContent();
    }

    /// <summary>Xóa mềm user (chặn tự xóa chính mình).</summary>
    /// <remarks>DELETE /api/v1/admin/users/{id}</remarks>
    [HttpDelete("{id:long}")]
    [RequirePermission("administration.users", PermissionOp.Xoa)]
    public async Task<IActionResult> DeleteUser(long id, CancellationToken ct = default)
    {
        var actorId = GetUserId();
        if (actorId is null) return Unauthorized();

        await _mediator.Send(new DeleteUserCommand(id, actorId.Value), ct);
        return NoContent();
    }

    /// <summary>Ghi lại danh sách vai trò của user (tab Vai trò).</summary>
    /// <remarks>PUT /api/v1/admin/users/{id}/roles</remarks>
    [HttpPut("{id:long}/roles")]
    [RequirePermission("administration.users", PermissionOp.Sua)]
    public async Task<IActionResult> SaveUserRoles(
        long id, [FromBody] SaveUserRolesRequest body, CancellationToken ct = default)
    {
        var actorId = GetUserId();
        if (actorId is null) return Unauthorized();

        await _mediator.Send(new SaveUserRolesCommand(id, body.VaiTroIds ?? [], actorId.Value), ct);
        return NoContent();
    }

    /// <summary>Cây công ty + trạng thái quyền (gán riêng / theo vai trò / mặc định) của user.</summary>
    /// <remarks>GET /api/v1/admin/users/{id}/companies</remarks>
    [HttpGet("{id:long}/companies")]
    [RequirePermission("administration.users", PermissionOp.Xem)]
    public async Task<IActionResult> GetUserCompanies(long id, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetUserCompaniesQuery(id), ct));

    /// <summary>Ghi lại tập công ty gán riêng + công ty mặc định của user (tab Công ty truy cập).</summary>
    /// <remarks>PUT /api/v1/admin/users/{id}/companies</remarks>
    [HttpPut("{id:long}/companies")]
    [RequirePermission("administration.users", PermissionOp.Sua)]
    public async Task<IActionResult> SaveUserCompanies(
        long id, [FromBody] SaveUserCompaniesRequest body, CancellationToken ct = default)
    {
        var actorId = GetUserId();
        if (actorId is null) return Unauthorized();

        await _mediator.Send(new SaveUserCompaniesCommand(
            id, body.CongTyIds ?? [], body.MacDinhCongTyId, actorId.Value), ct);
        return NoContent();
    }

    private long? GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return long.TryParse(raw, out var id) ? id : null;
    }
}

/// <summary>Body tạo/cập nhật user. MatKhau chỉ dùng khi tạo mới.</summary>
public sealed class SaveUserRequest
{
    public string? Ma { get; set; }
    public string? TenDangNhap { get; set; }
    public string? MatKhau { get; set; }
    public string? TrangThai { get; set; }
    public bool LaQuanTri { get; set; }
    public bool KichHoatMobile { get; set; }
    public DateTime? HetHanTaiKhoan { get; set; }
    public bool DoiMatKhauLanSau { get; set; }
}

/// <summary>Body đặt lại mật khẩu (admin đặt hộ — khác ResetPasswordRequest của luồng Auth tự phục vụ).</summary>
public sealed class AdminResetPasswordRequest
{
    public string? MatKhauMoi { get; set; }
    public bool DoiMatKhauLanSau { get; set; }
}

/// <summary>Body ghi danh sách vai trò của user.</summary>
public sealed class SaveUserRolesRequest
{
    public List<long>? VaiTroIds { get; set; }
}

/// <summary>Body ghi tập công ty gán riêng của user.</summary>
public sealed class SaveUserCompaniesRequest
{
    public List<long>? CongTyIds { get; set; }
    public long? MacDinhCongTyId { get; set; }
}
