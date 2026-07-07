// File    : ImportController.cs
// Module  : Import
// Layer   : Api
// Purpose : REST endpoint import Excel theo cấu hình View — tải template · validate (preview) · commit.
//           Spec 25 §11–§14, ADR-034.

using ICare247.Api.Authorization;
using ICare247.Application.Features.Import;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICare247.Api.Controllers;

/// <summary>
/// Import dữ liệu Excel vào bảng đích của View. Tải template + dry-run preview + commit ghi thật.
/// Mọi endpoint yêu cầu quyền View·Xem; commit thêm kiểm Form·Thêm/Sửa trong handler (deny-by-default).
/// </summary>
[ApiController]
[Route("api/v1/views")]
[Authorize]
public sealed class ImportController : ControllerBase
{
    private const long MaxUploadBytes = 20L * 1024 * 1024;   // 20MB — chặn file quá lớn

    private readonly IMediator _mediator;
    private readonly ILogger<ImportController> _logger;

    public ImportController(IMediator mediator, ILogger<ImportController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Tải workbook template import (sheet chính + sheet phụ FK + dropdown chọn Mã).</summary>
    [HttpGet("{code}/import/template")]
    [RequirePermissionForTarget("View", PermissionOp.Xem, "code")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate(
        string code, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ExportImportTemplateQuery(code, GetTenantId(), lang), ct);
        if (result is null)
            return NotFound(new { message = "View không tồn tại hoặc chưa có form Thêm/Sửa để import." });

        return File(result.Content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
    }

    /// <summary>Kiểm tra file import (dry-run) → preview NEW/UPDATE/ERROR. KHÔNG ghi DB.</summary>
    [HttpPost("{code}/import/validate")]
    [RequirePermissionForTarget("View", PermissionOp.Xem, "code")]
    [RequestSizeLimit(MaxUploadBytes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Validate(
        string code, IFormFile? file, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var bytes = await ReadFileAsync(file, ct);
        if (bytes is null)
            return BadRequest(new { message = "Chưa chọn tệp import." });

        var result = await _mediator.Send(
            new ValidateImportFileCommand(code, GetTenantId(), lang, bytes), ct);
        return result is null
            ? NotFound(new { message = "View không tồn tại hoặc chưa có form Thêm/Sửa để import." })
            : Ok(result);
    }

    /// <summary>Ghi thật các dòng hợp lệ (partial commit) + log + hook sau import.</summary>
    [HttpPost("{code}/import/commit")]
    [RequirePermissionForTarget("View", PermissionOp.Xem, "code")]
    [RequestSizeLimit(MaxUploadBytes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Commit(
        string code, IFormFile? file, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var bytes = await ReadFileAsync(file, ct);
        if (bytes is null)
            return BadRequest(new { message = "Chưa chọn tệp import." });

        var correlationId = HttpContext.TraceIdentifier;
        var result = await _mediator.Send(
            new CommitImportCommand(code, GetTenantId(), GetUserId(), lang, bytes, file!.FileName, correlationId), ct);
        return result is null
            ? NotFound(new { message = "View không tồn tại hoặc chưa có form Thêm/Sửa để import." })
            : Ok(result);
    }

    /// <summary>Đọc IFormFile → byte[] (spool bộ nhớ, đã chặn kích thước qua RequestSizeLimit). Null nếu rỗng.</summary>
    private static async Task<byte[]?> ReadFileAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return null;
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    /// <summary>Tenant từ TenantContext (subdomain/header — ADR-018).</summary>
    private int GetTenantId()
        => HttpContext.RequestServices.GetRequiredService<ITenantContext>().TenantId;

    /// <summary>NguoiDung_Id từ JWT (sub / NameIdentifier).</summary>
    private long GetUserId()
    {
        var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return long.TryParse(raw, out var id) ? id : 0;
    }
}
