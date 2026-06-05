// File    : LookupController.cs
// Module  : Lookup
// Layer   : Api
// Purpose : REST endpoint lấy Sys_Lookup items và query dynamic lookup data.

using ICare247.Application.Features.Lookups.Commands.InsertLookup;
using ICare247.Application.Features.Lookups.Queries.GetLookupByCode;
using ICare247.Application.Features.Lookups.Queries.QueryDynamic;
using ICare247.Application.Features.Lookups.Queries.QueryTree;
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

    /// <summary>
    /// Truy vấn dữ liệu dạng cây cho TreeLookupBox.
    /// Trả flat list có thêm key <c>__parentId</c> để client build hierarchy.
    /// </summary>
    /// <remarks>
    /// POST /api/v1/lookups/query-tree
    /// Body: { "fieldId": 42, "contextValues": { "ChiNhanhId": 1 } }
    /// Response: [{ "PhongBan_Id": 1, "Ten_PhongBan": "Công ty", "Parent_Id": null, "__parentId": null }, ...]
    /// </remarks>
    [HttpPost("query-tree")]
    public async Task<IActionResult> QueryTree(
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

        var query  = new QueryTreeLookupQuery(body.FieldId, GetTenantId(), body.ContextValues);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Thêm mới một entity vào bảng nguồn của LookupBox (tính năng "➕ Thêm mới" trên control).
    /// Backend đọc bảng đích từ <c>Ui_Field_Lookup</c> theo <c>fieldId</c> — client chỉ gửi cặp cột↔giá trị.
    /// </summary>
    /// <remarks>
    /// POST /api/v1/lookups/insert
    /// Body: { "fieldId": 42, "values": { "Ten_Xa": "Xã ABC", "TinhId": 68 } }
    /// Response: { "value": 123, "display": "Xã ABC" }
    /// </remarks>
    [HttpPost("insert")]
    public async Task<IActionResult> Insert(
        [FromBody] InsertLookupRequest body,
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

        if (body.Values is null || body.Values.Count == 0)
            return BadRequest(new ProblemDetails
            {
                Type   = "https://tools.ietf.org/html/rfc7807",
                Title  = "Thiếu dữ liệu",
                Status = 400,
                Detail = "Cần ít nhất một cột để thêm mới."
            });

        try
        {
            var command = new InsertLookupCommand(body.FieldId, GetTenantId(), body.Values);
            var result  = await _mediator.Send(command, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Type   = "https://tools.ietf.org/html/rfc7807",
                Title  = "Không thể thêm mới",
                Status = 400,
                Detail = ex.Message
            });
        }
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

/// <summary>Request body cho POST /api/v1/lookups/insert.</summary>
public sealed class InsertLookupRequest
{
    /// <summary>Field_Id của LookupBox — xác định bảng nguồn để insert.</summary>
    [JsonPropertyName("fieldId")]
    public int FieldId { get; init; }

    /// <summary>Cặp Cột↔Giá trị từ dialog thêm mới (key = tên cột DB).</summary>
    [JsonPropertyName("values")]
    public Dictionary<string, object?> Values { get; init; } = [];
}
