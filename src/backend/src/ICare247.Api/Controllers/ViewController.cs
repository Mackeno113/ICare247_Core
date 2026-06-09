// File    : ViewController.cs
// Module  : Views
// Layer   : Api
// Purpose : REST endpoint cho cấu hình hiển thị danh sách (Ui_View) — metadata cho Blazor DataView.

using ICare247.Application.Features.Views.Queries.GetViewByCode;
using ICare247.Application.Features.Views.Queries.GetViewData;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ICare247.Api.Controllers;

/// <summary>
/// Cấu hình View hiển thị danh sách (Grid/TreeList).
/// Mọi endpoint yêu cầu header <c>X-Tenant-Id</c>.
/// </summary>
[ApiController]
[Route("api/v1/views")]
public sealed class ViewController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfigCache _configCache;
    private readonly ILogger<ViewController> _logger;

    public ViewController(IMediator mediator, IConfigCache configCache, ILogger<ViewController> logger)
    {
        _mediator = mediator;
        _configCache = configCache;
        _logger = logger;
    }

    /// <summary>
    /// Lấy metadata đầy đủ của một View (header + cột + action) theo View_Code,
    /// đã resolve text i18n theo <paramref name="lang"/>.
    /// </summary>
    /// <param name="code">Ui_View.View_Code.</param>
    /// <param name="lang">Mã ngôn ngữ resolve resource (mặc định "vi").</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{code}/info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInfo(
        string code,
        [FromQuery] string lang = "vi",
        CancellationToken ct = default)
    {
        var query = new GetViewByCodeQuery(code, GetTenantId(), lang);
        var result = await _mediator.Send(query, ct);

        if (result is null)
            return NotFound(new { message = $"View '{code}' không tồn tại hoặc đã bị ẩn." });

        return Ok(result);
    }

    /// <summary>
    /// Lấy trang dữ liệu của View (Source_Type='Table') — SELECT cột Data từ bảng nguồn, có search + paging.
    /// </summary>
    /// <param name="code">Ui_View.View_Code.</param>
    /// <param name="lang">Ngôn ngữ resolve metadata (mặc định "vi").</param>
    /// <param name="search">Từ khóa lọc (LIKE trên các cột Data).</param>
    /// <param name="page">Trang (1-based).</param>
    /// <param name="pageSize">Số dòng mỗi trang.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{code}/data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetData(
        string code,
        [FromQuery] string lang = "vi",
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetViewDataQuery(code, GetTenantId(), lang, search, page, pageSize);
        var result = await _mediator.Send(query, ct);

        if (result is null)
            return NotFound(new { message = $"View '{code}' không tồn tại hoặc đã bị ẩn." });

        return Ok(result);
    }

    /// <summary>
    /// Xóa cache metadata của một View (mọi ngôn ngữ) — gọi sau khi admin sửa View ở ConfigStudio.
    /// </summary>
    /// <param name="code">Ui_View.View_Code cần invalidate.</param>
    [HttpPost("{code}/invalidate-cache")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> InvalidateCache(string code)
    {
        await _configCache.InvalidateViewAsync(code, GetTenantId());
        return Ok(new { message = $"Đã xóa cache View '{code}'." });
    }

    /// <summary>Lấy Tenant_Id từ header X-Tenant-Id (fallback 1 ở môi trường dev).</summary>
    private int GetTenantId()
    {
        if (Request.Headers.TryGetValue("X-Tenant-Id", out var values)
            && int.TryParse(values.FirstOrDefault(), out var tenantId)
            && tenantId > 0)
        {
            return tenantId;
        }

        _logger.LogWarning("Header X-Tenant-Id thiếu hoặc không hợp lệ — dùng default TenantId=1");
        return 1;
    }
}
