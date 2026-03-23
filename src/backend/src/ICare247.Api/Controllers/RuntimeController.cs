// File    : RuntimeController.cs
// Module  : Runtime
// Layer   : Api
// Purpose : REST endpoints cho runtime form processing — validate field, handle event.

using ICare247.Application.Interfaces;
using ICare247.Domain.Engine;
using ICare247.Domain.Engine.Models;
using ICare247.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace ICare247.Api.Controllers;

/// <summary>
/// Runtime form processing endpoints.
/// Dùng bởi Blazor frontend khi user tương tác với form.
/// </summary>
[ApiController]
[Route("api/v1/forms/{formCode}")]
public sealed class RuntimeController : ControllerBase
{
    private readonly IValidationEngine _validationEngine;
    private readonly IEventEngine _eventEngine;
    private readonly IFormRepository _formRepository;
    private readonly ITenantContext _tenant;
    private readonly ILogger<RuntimeController> _logger;

    public RuntimeController(
        IValidationEngine validationEngine,
        IEventEngine eventEngine,
        IFormRepository formRepository,
        ITenantContext tenant,
        ILogger<RuntimeController> logger)
    {
        _validationEngine = validationEngine;
        _eventEngine = eventEngine;
        _formRepository = formRepository;
        _tenant = tenant;
        _logger = logger;
    }

    // ── Validate Field ──────────────────────────────────────────────

    /// <summary>
    /// Validate một field đơn lẻ — trả về kết quả validation (pass/fail + errors).
    /// </summary>
    [HttpPost("validate-field")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateField(
        string formCode,
        [FromBody] ValidateFieldRequest body,
        CancellationToken ct = default)
    {
        // Resolve FormCode → FormId
        var form = await _formRepository.GetByCodeAsync(formCode, _tenant.TenantId, ct: ct);
        if (form is null)
            return FormNotFound(formCode);

        var context = BuildContext(body.ContextSnapshot);

        var result = await _validationEngine.ValidateFieldAsync(
            form.FormId, body.FieldCode, body.Value, context,
            _tenant.TenantId, ct);

        return Ok(result);
    }

    // ── Validate Form ───────────────────────────────────────────────

    /// <summary>
    /// Validate toàn bộ form — trả về kết quả validation cho từng field.
    /// Dùng trước submit để kiểm tra tất cả required/custom rules.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateForm(
        string formCode,
        [FromBody] ValidateFormRequest body,
        CancellationToken ct = default)
    {
        var form = await _formRepository.GetByCodeAsync(formCode, _tenant.TenantId, ct: ct);
        if (form is null)
            return FormNotFound(formCode);

        var context = BuildContext(body.ContextSnapshot);

        var results = await _validationEngine.ValidateFormAsync(
            form.FormId, context, _tenant.TenantId, ct);

        return Ok(results);
    }

    // ── Handle Event ────────────────────────────────────────────────

    /// <summary>
    /// Xử lý event từ form — trả về danh sách UI delta để client áp dụng.
    /// Event types: FIELD_CHANGED, FIELD_BLUR, FORM_LOAD, FORM_SUBMIT, SECTION_TOGGLE.
    /// </summary>
    [HttpPost("handle-event")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HandleEvent(
        string formCode,
        [FromBody] HandleEventRequest body,
        CancellationToken ct = default)
    {
        var form = await _formRepository.GetByCodeAsync(formCode, _tenant.TenantId, ct: ct);
        if (form is null)
            return FormNotFound(formCode);

        var context = BuildContext(body.ContextSnapshot);

        var formEvent = new FormEvent(
            body.EventType,
            body.SourceField,
            form.FormId,
            _tenant.TenantId,
            context);

        var result = await _eventEngine.HandleEventAsync(formEvent, ct);

        _logger.LogDebug(
            "HandleEvent — FormCode={FormCode}, EventType={EventType}, DeltaCount={DeltaCount}",
            formCode, body.EventType, result.Delta.Count);

        return Ok(result);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>Build EvaluationContext từ client snapshot.</summary>
    private static EvaluationContext BuildContext(
        Dictionary<string, object?>? snapshot)
    {
        if (snapshot is null || snapshot.Count == 0)
            return EvaluationContext.Empty;

        return new EvaluationContext(snapshot);
    }

    /// <summary>Trả 404 ProblemDetails khi form không tồn tại.</summary>
    private NotFoundObjectResult FormNotFound(string formCode)
    {
        return NotFound(new ProblemDetails
        {
            Type = "https://icare247.vn/errors/form-not-found",
            Title = "Form không tồn tại",
            Status = StatusCodes.Status404NotFound,
            Detail = $"Không tìm thấy form với mã '{formCode}' trong tenant {_tenant.TenantId}."
        });
    }
}

// ── Request DTOs ────────────────────────────────────────────────────────────

/// <summary>Request body cho POST /api/v1/forms/{formCode}/validate-field.</summary>
public sealed class ValidateFieldRequest
{
    /// <summary>Field code cần validate.</summary>
    public string FieldCode { get; init; } = string.Empty;

    /// <summary>Giá trị hiện tại của field.</summary>
    public object? Value { get; init; }

    /// <summary>Snapshot toàn bộ giá trị field trên form.</summary>
    public Dictionary<string, object?>? ContextSnapshot { get; init; }
}

/// <summary>Request body cho POST /api/v1/forms/{formCode}/validate.</summary>
public sealed class ValidateFormRequest
{
    /// <summary>Snapshot toàn bộ giá trị field trên form.</summary>
    public Dictionary<string, object?>? ContextSnapshot { get; init; }
}

/// <summary>Request body cho POST /api/v1/forms/{formCode}/handle-event.</summary>
public sealed class HandleEventRequest
{
    /// <summary>
    /// Loại event: 'FIELD_CHANGED' | 'FIELD_BLUR' | 'FORM_LOAD' | 'FORM_SUBMIT' | 'SECTION_TOGGLE'.
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>Field phát sinh event (Field_Code). Null với FORM_LOAD/FORM_SUBMIT.</summary>
    public string? SourceField { get; init; }

    /// <summary>Snapshot toàn bộ giá trị field trên form.</summary>
    public Dictionary<string, object?>? ContextSnapshot { get; init; }
}
