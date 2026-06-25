// File    : LanguageController.cs
// Module  : Languages
// Layer   : Api
// Purpose : REST endpoint đọc Sys_Language — cho client dựng bộ chuyển ngôn ngữ + ô nhập bản dịch.
//           Không cần tenant/đăng nhập (danh sách ngôn ngữ là cấu hình hệ thống, đọc cả trước login).

using ICare247.Application.Features.Languages.GetLanguages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICare247.Api.Controllers;

/// <summary>Danh sách ngôn ngữ hệ thống (Sys_Language).</summary>
[ApiController]
[Route("api/v1/languages")]
[AllowAnonymous] // SEC1-2: danh sách ngôn ngữ là cấu hình hệ thống, đọc cả trước login (bộ chuyển ngôn ngữ màn login)
public sealed class LanguageController : ControllerBase
{
    private readonly IMediator _mediator;

    public LanguageController(IMediator mediator) => _mediator = mediator;

    /// <summary>Mọi ngôn ngữ (mặc định lên đầu).</summary>
    /// <remarks>GET /api/v1/languages</remarks>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetLanguagesQuery(), ct));
}
