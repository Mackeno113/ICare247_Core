// File    : LookupController.cs
// Module  : Lookup
// Layer   : Api
// Purpose : REST endpoint lấy danh sách Sys_Lookup items theo code.

using ICare247.Application.Features.Lookups.Queries.GetLookupByCode;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ICare247.Api.Controllers;

/// <summary>
/// Truy vấn danh mục dùng chung từ <c>Sys_Lookup</c>.
/// Dùng cho render RadioGroup / LookupComboBox trên UI.
/// </summary>
[ApiController]
[Route("api/v1/lookups")]
public sealed class LookupController : ControllerBase
{
    private readonly IMediator _mediator;

    public LookupController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Lấy danh sách items của một lookup code.
    /// Label đã resolve theo ngôn ngữ chỉ định.
    /// </summary>
    /// <param name="code">VD: GENDER, MARITAL_STATUS</param>
    /// <param name="lang">Ngôn ngữ: vi (mặc định), en, ja</param>
    /// <param name="ct"></param>
    /// <remarks>
    /// GET /api/v1/lookups/GENDER?lang=vi
    /// Response: [{ "itemCode": "NAM", "label": "Nam", "sortOrder": 1 }, ...]
    /// </remarks>
    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(
        string code,
        [FromQuery] string lang = "vi",
        CancellationToken ct = default)
    {
        var query  = new GetLookupByCodeQuery(code.ToUpperInvariant(), GetTenantId(), lang);
        var result = await _mediator.Send(query, ct);

        if (result.Count == 0)
            return NotFound(new ProblemDetails
            {
                Type   = "https://tools.ietf.org/html/rfc7807",
                Title  = "Lookup code not found",
                Status = 404,
                Detail = $"Không tìm thấy lookup '{code}' hoặc chưa có dữ liệu."
            });

        // Chỉ trả về fields cần thiết cho UI — không expose toàn bộ entity
        var dto = result.Select(i => new
        {
            i.ItemCode,
            i.Label,
            i.LabelKey,
            i.SortOrder
        });

        return Ok(dto);
    }

    private int GetTenantId()
    {
        if (Request.Headers.TryGetValue("X-Tenant-Id", out var values)
            && int.TryParse(values.FirstOrDefault(), out var tenantId)
            && tenantId > 0)
            return tenantId;
        return 1;
    }
}
