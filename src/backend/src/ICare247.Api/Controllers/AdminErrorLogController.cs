// File    : AdminErrorLogController.cs
// Module  : Admin/ErrorLogs
// Layer   : Api
// Purpose : Endpoint màn Nhật ký lỗi (admin) — tra cứu lỗi 500 theo Mã lỗi hiện trên toast,
//           thay vì phải mở file logs/icare247-*.log. Bắt buộc JWT; enforce chức năng
//           "administration.errorlogs".

using ICare247.Api.Authorization;
using ICare247.Application.Features.Admin.ErrorLogs.GetErrorLogDetail;
using ICare247.Application.Features.Admin.ErrorLogs.GetErrorLogs;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICare247.Api.Controllers;

/// <summary>Nhật ký lỗi hệ thống (đọc, không sửa). Enforce: chức năng "administration.errorlogs".</summary>
[ApiController]
[Route("api/v1/admin/error-logs")]
[Authorize]
public sealed class AdminErrorLogController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminErrorLogController(IMediator mediator) => _mediator = mediator;

    /// <summary>Danh sách lỗi cho lưới, lọc theo khoảng thời gian / Mã lỗi / Nguồn.</summary>
    /// <remarks>GET /api/v1/admin/error-logs?tu=&amp;den=&amp;maLoi=&amp;nguon=</remarks>
    [HttpGet]
    [RequirePermission("administration.errorlogs", PermissionOp.Xem)]
    public async Task<IActionResult> GetErrorLogs(
        [FromQuery] DateTime? tu, [FromQuery] DateTime? den,
        [FromQuery] string? maLoi, [FromQuery] string? nguon, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetErrorLogsQuery(tu, den, maLoi, nguon), ct));

    /// <summary>Chi tiết 1 lỗi — message + stack trace đầy đủ (popup xem chi tiết).</summary>
    /// <remarks>GET /api/v1/admin/error-logs/{id}</remarks>
    [HttpGet("{id:long}")]
    [RequirePermission("administration.errorlogs", PermissionOp.Xem)]
    public async Task<IActionResult> GetErrorLogDetail(long id, CancellationToken ct = default)
    {
        var detail = await _mediator.Send(new GetErrorLogDetailQuery(id), ct);
        return detail is null ? NotFound() : Ok(detail);
    }
}
