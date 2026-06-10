// File    : ResourceController.cs
// Module  : Resources
// Layer   : Api
// Purpose : REST endpoint đọc Sys_Resource theo key + ngôn ngữ — cho i18n chuỗi UI tĩnh (nút, nhãn chung).

using ICare247.Application.Features.Resources.Queries.GetResources;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ICare247.Api.Controllers;

/// <summary>Resource i18n dùng chung (vd <c>common.btn.save</c>). Không cần tenant.</summary>
[ApiController]
[Route("api/v1/resources")]
public sealed class ResourceController : ControllerBase
{
    private readonly IMediator _mediator;

    public ResourceController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Lấy map key→value cho danh sách key (phẩy ngăn cách) theo <paramref name="lang"/>.
    /// Ví dụ: <c>/api/v1/resources?lang=vi&amp;keys=common.btn.save,common.btn.cancel</c>.
    /// </summary>
    /// <param name="keys">Danh sách Resource_Key, ngăn cách bằng dấu phẩy.</param>
    /// <param name="lang">Ngôn ngữ resolve (mặc định "vi").</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] string keys = "",
        [FromQuery] string lang = "vi",
        CancellationToken ct = default)
    {
        var keyList = keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = await _mediator.Send(new GetResourcesQuery(keyList, lang), ct);
        return Ok(result);
    }
}
