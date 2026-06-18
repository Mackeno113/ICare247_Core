// File    : CacheAdminController.cs
// Module  : Admin/Cache
// Layer   : Api
// Purpose : Endpoint "Cưỡng chế làm mới" — xóa toàn bộ cache của tenant hiện tại (dùng chung +
//           per-user trên server). Yêu cầu đăng nhập. Cache per-user phía trình duyệt do client
//           tự xóa khi tải lại cưỡng chế.

using ICare247.Application.Features.Admin.Cache.FlushTenantCache;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICare247.Api.Controllers;

/// <summary>Thao tác cache cấp tenant (admin). Yêu cầu đăng nhập.</summary>
[ApiController]
[Route("api/v1/admin/cache")]
[Authorize]
public sealed class CacheAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public CacheAdminController(IMediator mediator) => _mediator = mediator;

    /// <summary>Xóa toàn bộ cache của tenant hiện tại (config dùng chung + menu per-user trên server).</summary>
    /// <remarks>POST /api/v1/admin/cache/flush</remarks>
    [HttpPost("flush")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Flush(CancellationToken ct = default)
    {
        await _mediator.Send(new FlushTenantCacheCommand(), ct);
        return NoContent();
    }
}
