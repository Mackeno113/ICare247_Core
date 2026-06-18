// File    : ResourceController.cs
// Module  : Resources
// Layer   : Api
// Purpose : REST endpoint đọc Sys_Resource theo key + ngôn ngữ — cho i18n chuỗi UI tĩnh (nút, nhãn chung).

using ICare247.Api.Authorization;
using ICare247.Application.Features.Resources.Commands.UpsertResource;
using ICare247.Application.Features.Resources.Queries.GetResourceOverlay;
using ICare247.Application.Features.Resources.Queries.GetResourceTranslations;
using ICare247.Application.Features.Resources.Queries.GetResources;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

    /// <summary>
    /// Toàn bộ overlay (key→value) của 1 ngôn ngữ, lọc tùy chọn theo <paramref name="prefix"/> (vd "nav.").
    /// Cho LocalizationService gộp bản dịch DB lên lớp tĩnh JSON.
    /// </summary>
    /// <remarks>GET /api/v1/resources/overlay?lang=en&amp;prefix=nav.</remarks>
    [HttpGet("overlay")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverlay(
        [FromQuery] string lang = "vi",
        [FromQuery] string? prefix = null,
        CancellationToken ct = default)
    {
        var prefixArg = string.IsNullOrWhiteSpace(prefix) ? null : prefix;
        var result = await _mediator.Send(new GetResourceOverlayQuery(lang, prefixArg), ct);
        return Ok(result);
    }

    /// <summary>Bản dịch của 1 key theo mọi ngôn ngữ (Lang_Code→value) — cho màn sửa bản dịch.</summary>
    /// <remarks>GET /api/v1/resources/translations?key=nav.screen.organization.title</remarks>
    [HttpGet("translations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTranslations([FromQuery] string key = "", CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetResourceTranslationsQuery(key), ct));

    /// <summary>Thêm/sửa 1 bản dịch i18n. Yêu cầu quyền Sửa trên chức năng "administration.menu".</summary>
    /// <remarks>PUT /api/v1/resources</remarks>
    [HttpPut]
    [Authorize]
    [RequirePermission("administration.menu", PermissionOp.Sua)]
    public async Task<IActionResult> Upsert([FromBody] UpsertResourceRequest body, CancellationToken ct = default)
    {
        await _mediator.Send(new UpsertResourceCommand(body.Key, body.Lang, body.Value), ct);
        return NoContent();
    }
}

/// <summary>Body cập nhật 1 bản dịch i18n.</summary>
public sealed class UpsertResourceRequest
{
    /// <summary>Resource_Key (vd nav.screen.organization.title).</summary>
    public string Key { get; set; } = "";

    /// <summary>Mã ngôn ngữ (vd "en").</summary>
    public string Lang { get; set; } = "";

    /// <summary>Giá trị bản dịch.</summary>
    public string Value { get; set; } = "";
}
