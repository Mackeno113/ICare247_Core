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
using ICare247.Api.Authorization;
using ICare247.Application.Interfaces;
using ICare247.Domain.Engine;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    private readonly IMetadataEngine _metadataEngine;
    private readonly ILogger<FormController> _logger;

    public FormController(IMediator mediator, IMetadataEngine metadataEngine, ILogger<FormController> logger)
    {
        _mediator        = mediator;
        _metadataEngine  = metadataEngine;
        _logger          = logger;
    }

    // ── Queries ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy danh sách form có phân trang và filter.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SUPERADMIN")] // SEC1-4: quản lý cấu hình form = việc builder → chỉ SUPERADMIN
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
    [RequirePermissionForTarget("Form", PermissionOp.Xem, "code")]
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
    [Authorize(Roles = "SUPERADMIN")] // SEC1-4: audit log cấu hình form → chỉ SUPERADMIN
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
    [Authorize(Roles = "SUPERADMIN")] // SEC1-4: tạo định nghĩa form → chỉ SUPERADMIN
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
    [Authorize(Roles = "SUPERADMIN")] // SEC1-4: sửa định nghĩa form → chỉ SUPERADMIN
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
    [Authorize(Roles = "SUPERADMIN")] // SEC1-4: vô hiệu hóa form → chỉ SUPERADMIN
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
    [Authorize(Roles = "SUPERADMIN")] // SEC1-4: khôi phục form → chỉ SUPERADMIN
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
    [Authorize(Roles = "SUPERADMIN")] // SEC1-4: nhân bản định nghĩa form → chỉ SUPERADMIN
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

    /// <summary>
    /// Xóa cache metadata của form — L1 MemoryCache + L2 Redis.
    /// Gọi sau khi admin thay đổi cấu hình field, i18n resource, hoặc popup columns.
    /// </summary>
    /// <remarks>
    /// Sự kiện theo sau: lần gọi <c>GetFormMetadata</c> tiếp theo sẽ load lại từ DB
    /// và cache lại với dữ liệu mới nhất.
    /// </remarks>
    [HttpPost("{code}/invalidate-cache")]
    [Authorize(Roles = "SUPERADMIN")] // SEC1-4: xóa cache cấu hình form → chỉ SUPERADMIN
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> InvalidateCache(string code)
    {
        var tenantId = GetTenantId();
        await _metadataEngine.InvalidateFormCacheAsync(code, tenantId);
        _logger.LogInformation("Cache invalidated qua API — FormCode={FormCode}, TenantId={TenantId}", code, tenantId);
        return NoContent();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Tenant_Id lấy từ TenantContext (TenantMiddleware đã phân giải qua subdomain/header).
    /// KHÔNG đọc header trực tiếp nữa để khớp đúng tenant đã mở DB (tránh rò cache chéo). ADR-018.
    /// </summary>
    private int GetTenantId()
        => HttpContext.RequestServices.GetRequiredService<Application.Interfaces.ITenantContext>().TenantId;

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
