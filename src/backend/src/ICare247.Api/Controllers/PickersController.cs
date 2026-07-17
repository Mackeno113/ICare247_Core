// File    : PickersController.cs
// Module  : Pickers
// Layer   : Api
// Purpose : Picker API dùng chung (spec 31 §3) — danh mục cho các control IcXxx (địa bàn…).
//           Chỉ đọc, bắt buộc JWT; dữ liệu danh mục chung tenant nên không gắn quyền chức năng riêng.

using ICare247.Application.Features.Pickers.GetDiaBan;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICare247.Api.Controllers;

/// <summary>Nguồn dữ liệu cho bộ picker dùng chung (IcAddressBlock…).</summary>
[ApiController]
[Route("api/v1/pickers")]
[Authorize]
public sealed class PickersController : ControllerBase
{
    private readonly IMediator _mediator;

    public PickersController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Địa bàn: không tham số → Tỉnh/Thành; parentId=tỉnh → Xã/Phường (kèm keyword, top);
    /// id=xã → resolve đúng 1 dòng (hiển thị giá trị đã lưu).
    /// </summary>
    /// <remarks>GET /api/v1/pickers/dia-ban?parentId=&amp;keyword=&amp;top=&amp;id=</remarks>
    [HttpGet("dia-ban")]
    public async Task<IActionResult> GetDiaBan(
        [FromQuery] long? id, [FromQuery] long? parentId, [FromQuery] string? keyword,
        [FromQuery] int top = 50, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetDiaBanQuery(id, parentId, keyword, top), ct));
}
