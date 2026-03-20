// File    : FormController.cs
// Module  : Forms
// Layer   : Api
// Purpose : REST endpoints cho quản lý Ui_Form — Phase 1 + Phase 6 CRUD.

using ICare247.Application.Features.Forms.Commands.CloneForm;
using ICare247.Application.Features.Forms.Commands.CreateForm;
using ICare247.Application.Features.Forms.Commands.DeactivateForm;
using ICare247.Application.Features.Forms.Commands.RestoreForm;
using ICare247.Application.Features.Forms.Commands.UpdateForm;
using ICare247.Application.Features.Forms.Queries.GetFormAuditLog;
using ICare247.Application.Features.Forms.Queries.GetFormByCode;
using ICare247.Application.Features.Forms.Queries.GetFormsList;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ICare247.Api.Controllers;

/// <summary>
/// Quản lý form metadata.
/// Mọi endpoint yêu cầu header <c>X-Tenant-Id</c>.
/// </summary>
[ApiController]
[Route("api/v1/config/forms")]
public sealed class FormController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FormController> _logger;

    public FormController(IMediator mediator, ILogger<FormController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // ── Queries ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy danh sách form có phân trang và filter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? platform = null,
        [FromQuery] int? tableId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetFormsListQuery(
            GetTenantId(), platform, tableId, isActive, search, page, pageSize);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Lấy form metadata đầy đủ theo Form_Code.
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode(
        string code,
        [FromQuery] string lang = "vi",
        [FromQuery] string platform = "web",
        CancellationToken ct = default)
    {
        var tenantId = GetTenantId();
        var query = new GetFormByCodeQuery(code, tenantId, lang, platform);
        var result = await _mediator.Send(query, ct);

        if (result is null)
            return NotFound(new ProblemDetails
            {
                Type = "https://icare247.vn/errors/form-not-found",
                Title = "Form không tồn tại",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Không tìm thấy form với mã '{code}' trong tenant {tenantId}."
            });

        return Ok(result);
    }

    /// <summary>
    /// Lấy audit log của form theo Form_Id.
    /// </summary>
    [HttpGet("{code}/audit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditLog(
        string code,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var tenantId = GetTenantId();

        // Resolve FormCode → FormId
        var form = await _mediator.Send(new GetFormByCodeQuery(code, tenantId), ct);
        if (form is null)
            return NotFound(new ProblemDetails
            {
                Type = "https://icare247.vn/errors/form-not-found",
                Title = "Form không tồn tại",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Không tìm thấy form với mã '{code}'."
            });

        var query = new GetFormAuditLogQuery(form.FormId, tenantId, page, pageSize);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Tạo form mới.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFormRequest body,
        CancellationToken ct = default)
    {
        var tenantId = GetTenantId();
        var command = new CreateFormCommand(
            body.FormCode, body.TableId, body.Platform,
            body.LayoutEngine ?? "Grid", body.Description,
            tenantId, GetCurrentUser());

        var formId = await _mediator.Send(command, ct);

        return CreatedAtAction(
            nameof(GetByCode),
            new { code = body.FormCode },
            new { FormId = formId, body.FormCode });
    }

    /// <summary>
    /// Cập nhật form theo Form_Code.
    /// </summary>
    [HttpPut("{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string code,
        [FromBody] UpdateFormRequest body,
        CancellationToken ct = default)
    {
        var command = new UpdateFormCommand(
            code, body.TableId, body.Platform,
            body.LayoutEngine ?? "Grid", body.Description,
            GetTenantId(), GetCurrentUser());

        await _mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Vô hiệu hóa form (soft delete).
    /// </summary>
    [HttpPost("{code}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(string code, CancellationToken ct = default)
    {
        var command = new DeactivateFormCommand(code, GetTenantId(), GetCurrentUser());
        await _mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Khôi phục form đã bị vô hiệu hóa.
    /// </summary>
    [HttpPost("{code}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(string code, CancellationToken ct = default)
    {
        var command = new RestoreFormCommand(code, GetTenantId(), GetCurrentUser());
        await _mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Nhân bản form sang Form_Code mới.
    /// </summary>
    [HttpPost("{code}/clone")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Clone(
        string code,
        [FromBody] CloneFormRequest body,
        CancellationToken ct = default)
    {
        var command = new CloneFormCommand(
            code, body.NewFormCode, GetTenantId(), GetCurrentUser());

        var newFormId = await _mediator.Send(command, ct);

        return CreatedAtAction(
            nameof(GetByCode),
            new { code = body.NewFormCode },
            new { FormId = newFormId, FormCode = body.NewFormCode });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Lấy Tenant_Id từ header X-Tenant-Id.</summary>
    private int GetTenantId()
    {
        if (Request.Headers.TryGetValue("X-Tenant-Id", out var values)
            && int.TryParse(values.FirstOrDefault(), out var tenantId)
            && tenantId > 0)
        {
            return tenantId;
        }

        // Development fallback — sẽ bỏ khi có TenantMiddleware
        _logger.LogWarning("Header X-Tenant-Id thiếu hoặc không hợp lệ — dùng default TenantId=1");
        return 1;
    }

    /// <summary>Lấy username từ JWT claims. Development fallback = "admin".</summary>
    private string GetCurrentUser()
    {
        return User.Identity?.Name ?? "admin";
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>Request body cho POST /api/v1/config/forms.</summary>
public sealed class CreateFormRequest
{
    public string FormCode { get; init; } = string.Empty;
    public int TableId { get; init; }
    public string Platform { get; init; } = "web";
    public string? LayoutEngine { get; init; }
    public string? Description { get; init; }
}

/// <summary>Request body cho PUT /api/v1/config/forms/{code}.</summary>
public sealed class UpdateFormRequest
{
    public int TableId { get; init; }
    public string Platform { get; init; } = "web";
    public string? LayoutEngine { get; init; }
    public string? Description { get; init; }
}

/// <summary>Request body cho POST /api/v1/config/forms/{code}/clone.</summary>
public sealed class CloneFormRequest
{
    public string NewFormCode { get; init; } = string.Empty;
}
