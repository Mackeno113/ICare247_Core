// File    : ViewController.cs
// Module  : Views
// Layer   : Api
// Purpose : REST endpoint cho cấu hình hiển thị danh sách (Ui_View) — metadata cho Blazor DataView.

using ICare247.Api.Authorization;
using ICare247.Application.Features.Views.Queries.GetFilterOptions;
using ICare247.Application.Features.Views.Queries.GetViewByCode;
using ICare247.Application.Features.Views.Queries.GetViewData;
using ICare247.Application.Features.Views.Queries.GetViewFilteredData;
using ICare247.Application.Features.Views.Queries.GetViewsList;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    private readonly IUserGridLayoutStore _layoutStore;
    private readonly ILogger<ViewController> _logger;

    public ViewController(
        IMediator mediator, IConfigCache configCache,
        IUserGridLayoutStore layoutStore, ILogger<ViewController> logger)
    {
        _mediator = mediator;
        _configCache = configCache;
        _layoutStore = layoutStore;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách View (header tóm tắt) có phân trang + filter — cho màn chọn View.
    /// </summary>
    /// <param name="lang">Mã ngôn ngữ resolve Title (mặc định "vi").</param>
    /// <param name="isActive">Lọc theo trạng thái (mặc định chỉ active).</param>
    /// <param name="search">Từ khóa lọc View_Code/Title.</param>
    /// <param name="page">Trang (1-based).</param>
    /// <param name="pageSize">Số dòng mỗi trang.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] string lang = "vi",
        [FromQuery] bool? isActive = true,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetViewsListQuery(GetTenantId(), lang, isActive, search, page, pageSize);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Lấy metadata đầy đủ của một View (header + cột + action) theo View_Code,
    /// đã resolve text i18n theo <paramref name="lang"/>.
    /// </summary>
    /// <param name="code">Ui_View.View_Code.</param>
    /// <param name="lang">Mã ngôn ngữ resolve resource (mặc định "vi").</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{code}/info")]
    [RequirePermissionForTarget("View", PermissionOp.Xem, "code")]
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
    [RequirePermissionForTarget("View", PermissionOp.Xem, "code")]
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
    /// Thực thi lưới nâng cao (Source_Type='Sp'/'Sql') với tham số từ panel lọc trái.
    /// Body chứa map Filter_Code → giá trị; engine bind whitelist tham số rồi gọi SP/SQL.
    /// </summary>
    /// <param name="code">Ui_View.View_Code.</param>
    /// <param name="body">Bộ giá trị lọc người dùng nhập (key = Filter_Code).</param>
    /// <param name="lang">Ngôn ngữ resolve metadata (mặc định "vi").</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{code}/search")]
    [RequirePermissionForTarget("View", PermissionOp.Xem, "code")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search(
        string code,
        [FromBody] ViewSearchRequest body,
        [FromQuery] string lang = "vi",
        CancellationToken ct = default)
    {
        var filterValues = body?.Filters ?? new Dictionary<string, string?>();

        try
        {
            var query = new GetViewFilteredDataQuery(code, GetTenantId(), filterValues, lang);
            var result = await _mediator.Send(query, ct);

            if (result is null)
                return NotFound(new { message = $"View '{code}' không tồn tại hoặc đã bị ẩn." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            // Tham số bắt buộc thiếu hoặc sai định dạng → 400 (client hiển thị thông báo).
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Nạp options cho 1 control lọc cascade (Combo/MultiSelect/Radio) — ADR-030. Body chứa giá trị
    /// filter cha hiện tại (key = Filter_Code); engine bind whitelist (Depends_On) + token ngữ cảnh.
    /// </summary>
    /// <param name="code">Ui_View.View_Code.</param>
    /// <param name="filterCode">Ui_View_Filter.Filter_Code của control cần options.</param>
    /// <param name="body">Giá trị filter cha hiện tại (cascade).</param>
    /// <param name="lang">Ngôn ngữ resolve metadata + nhãn lookup (mặc định "vi").</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{code}/filter-options/{filterCode}")]
    [RequirePermissionForTarget("View", PermissionOp.Xem, "code")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FilterOptions(
        string code, string filterCode,
        [FromBody] FilterOptionsRequest? body,
        [FromQuery] string lang = "vi",
        CancellationToken ct = default)
    {
        var parents = body?.Parents ?? new Dictionary<string, string?>();
        try
        {
            var query = new GetFilterOptionsQuery(code, filterCode, GetTenantId(), parents, lang);
            var result = await _mediator.Send(query, ct);

            if (result is null)
                return NotFound(new { message = $"View '{code}' không tồn tại hoặc đã bị ẩn." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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

    // ── Layout lưới per-user (sở thích người dùng — Data DB, cache riêng) ───────────

    /// <summary>Lấy layout lưới đã lưu của user hiện tại cho View (null nếu chưa tùy chỉnh).</summary>
    [HttpGet("{code}/my-layout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyLayout(
        string code, [FromQuery] string platform = "web", CancellationToken ct = default)
    {
        var json = await _layoutStore.GetAsync(GetUserId(), code, platform, GetTenantId(), ct);
        return Ok(new { layoutJson = json });
    }

    /// <summary>Lưu (UPSERT) layout lưới của user cho View — write-through cache.</summary>
    [HttpPut("{code}/my-layout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveMyLayout(
        string code, [FromBody] GridLayoutRequest body,
        [FromQuery] string platform = "web", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(body?.LayoutJson))
            return BadRequest(new { message = "layoutJson rỗng." });

        await _layoutStore.SaveAsync(GetUserId(), code, platform, GetTenantId(), body.LayoutJson, ct);
        return NoContent();
    }

    /// <summary>Khôi phục mặc định: xóa layout của user cho View (quay về Ui_View).</summary>
    [HttpDelete("{code}/my-layout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetMyLayout(
        string code, [FromQuery] string platform = "web", CancellationToken ct = default)
    {
        await _layoutStore.ResetAsync(GetUserId(), code, platform, GetTenantId(), ct);
        return NoContent();
    }

    /// <summary>
    /// Tenant_Id lấy từ TenantContext (TenantMiddleware đã phân giải qua subdomain/header).
    /// KHÔNG đọc header trực tiếp nữa để khớp đúng tenant đã mở DB (tránh rò cache chéo). ADR-018.
    /// </summary>
    private int GetTenantId()
        => HttpContext.RequestServices.GetRequiredService<Application.Interfaces.ITenantContext>().TenantId;

    /// <summary>NguoiDung_Id từ JWT (sub / NameIdentifier) — cho layout per-user.</summary>
    private long GetUserId()
    {
        var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return long.TryParse(raw, out var id) ? id : 0;
    }
}

/// <summary>Body cho PUT my-layout: chuỗi JSON serialize từ DxGrid GridPersistentLayout.</summary>
public sealed class GridLayoutRequest
{
    public string? LayoutJson { get; init; }
}

/// <summary>Body cho endpoint Search: map Filter_Code → giá trị người dùng nhập (chuỗi thô).</summary>
public sealed class ViewSearchRequest
{
    /// <summary>Key = Ui_View_Filter.Filter_Code; value = giá trị thô (engine ép kiểu theo Param_Type).</summary>
    public Dictionary<string, string?> Filters { get; init; } = new();
}

/// <summary>Body cho endpoint filter-options: map Filter_Code cha → giá trị hiện tại (cascade).</summary>
public sealed class FilterOptionsRequest
{
    /// <summary>Key = Filter_Code của control CHA; value = giá trị đang chọn (engine ép kiểu).</summary>
    public Dictionary<string, string?> Parents { get; init; } = new();
}
