// File    : LookupController.cs
// Module  : Lookup
// Layer   : Api
// Purpose : REST endpoint lấy Sys_Lookup items và query dynamic lookup data.

using ICare247.Application.Features.Lookups.Queries.GetLookupByCode;
using ICare247.Application.Features.Lookups.Queries.QueryDynamic;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

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

    /// <summary>
    /// Truy vấn dữ liệu nguồn cho một dynamic field (ComboBox / LookupBox).
    /// Backend tự đọc cấu hình từ <c>Ui_Field_Lookup</c> theo <c>fieldId</c> — không nhận SQL từ client.
    /// </summary>
    /// <remarks>
    /// POST /api/v1/lookups/query-dynamic
    /// Body: { "fieldId": 42, "contextValues": { "PhongBanId": 5 } }
    /// Response: [{ "Id": 1, "Ten": "Phòng A" }, ...]
    /// </remarks>
    [HttpPost("query-dynamic")]
    public async Task<IActionResult> QueryDynamic(
        [FromBody] QueryDynamicRequest body,
        CancellationToken ct = default)
    {
        if (body.FieldId <= 0)
            return BadRequest(new ProblemDetails
            {
                Type   = "https://tools.ietf.org/html/rfc7807",
                Title  = "FieldId không hợp lệ",
                Status = 400,
                Detail = "FieldId phải lớn hơn 0."
            });

        var query  = new QueryDynamicLookupQuery(body.FieldId, GetTenantId(), body.ContextValues);
        var result = await _mediator.Send(query, ct);

        return Ok(result);
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

/// <summary>Request body cho POST /api/v1/lookups/query-dynamic.</summary>
public sealed class QueryDynamicRequest
{
    /// <summary>Field_Id trong Ui_Field — xác định cấu hình lookup cần chạy.</summary>
    [JsonPropertyName("fieldId")]
    public int FieldId { get; init; }

    /// <summary>
    /// Snapshot giá trị các field trong form — dùng cho cascading filter.
    /// Ví dụ: { "PhongBanId": 5, "NamHoc": "2024" }.
    /// </summary>
    [JsonPropertyName("contextValues")]
    public Dictionary<string, object?> ContextValues { get; init; } = [];
}
