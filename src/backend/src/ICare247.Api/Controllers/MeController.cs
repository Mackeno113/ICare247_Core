// File    : MeController.cs
// Module  : Navigation
// Layer   : Api
// Purpose : Endpoint ngữ cảnh người dùng hiện tại. GET /api/v1/me/navigation trả cây menu
//           đã lọc theo quyền (kèm cờ thao tác). userId suy từ claim JWT (sub), KHÔNG nhận từ client.

using ICare247.Application.Features.Navigation.Queries.GetMyNavigation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ICare247.Api.Controllers;

/// <summary>Thông tin theo người dùng đăng nhập (menu, quyền). Bắt buộc có JWT hợp lệ.</summary>
[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeController(IMediator mediator) => _mediator = mediator;

    /// <summary>Cây menu user được thấy (Xem=1) + cờ thao tác. Sự kiện theo sau: NavMenu vẽ menu.</summary>
    /// <remarks>GET /api/v1/me/navigation</remarks>
    [HttpGet("navigation")]
    public async Task<IActionResult> GetNavigation(CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _mediator.Send(new GetMyNavigationQuery(userId.Value), ct);
        return Ok(result);
    }

    /// <summary>Lấy Id người dùng từ claim sub (JWT map sang NameIdentifier).</summary>
    private long? GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return long.TryParse(raw, out var id) ? id : null;
    }
}
