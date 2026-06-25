// File    : LookupController.cs
// Module  : Lookup
// Layer   : Api
// Purpose : REST endpoint lấy Sys_Lookup items và query dynamic lookup data.

using ICare247.Application.Features.Lookups.Commands.InsertLookup;
using ICare247.Application.Features.Lookups.Queries.GetLookupByCode;
using ICare247.Application.Features.Lookups.Queries.QueryDynamic;
using ICare247.Application.Features.Lookups.Queries.QueryTree;
using ICare247.Application.Interfaces;
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
    private readonly IMediator    _mediator;
    private readonly IConfigCache _config;

    public LookupController(IMediator mediator, IConfigCache config)
    {
        _mediator = mediator;
        _config   = config;
    }

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
    // TODO(SEC1-4): tạm để mức "chỉ cần đăng nhập" (qua FallbackPolicy). Rủi ro thấp: user đã khóa
    //   đúng tenant (TenantClaimGuard) chỉ thêm 1 option danh mục của chính tenant mình.
    //   Tinh chỉnh sau (đường resolve ĐÃ xác minh là có sẵn — không cần đổi schema):
    //   1) Trong DynamicLookupRepository.InsertAsync, từ cfg.Source_Name (bảng đích) → Sys_Table →
    //      Ui_Form lấy Form_Code (pattern y như truy vấn unique-cols dòng ~403-412 cùng file).
    //   2) Gọi IPermissionService.HasPermissionForTargetAsync("Form", formCode, PermissionOp.Them).
    //   CẦN CHỐT (quyết định nghiệp vụ, không rẻ đi nếu làm ngay): (a) bảng nguồn có 0/nhiều Ui_Form →
    //   lấy form nào? (b) bảng tham chiếu thuần không có form → enforce-if-mapped tự cho qua (chấp nhận?).
    //   (c) "thêm 1 option" buộc quyền Thêm cả form có hợp lý không, hay quyền nhẹ hơn?
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

    /// <summary>
    /// Xóa cache options của một lookup code (L1 + L2, mọi ngôn ngữ đã biết).
    /// Gọi sau khi admin sửa danh mục <c>Sys_Lookup</c> ở ConfigStudio.
    /// </summary>
    /// <remarks>
    /// POST /api/v1/lookups/GENDER/invalidate-cache
    /// Sự kiện theo sau: lần GET tiếp theo load lại từ DB và cache với dữ liệu mới.
    /// </remarks>
    [HttpPost("{code}/invalidate-cache")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> InvalidateCache(string code)
    {
        await _config.InvalidateLookupAsync(code.ToUpperInvariant(), GetTenantId());
        return NoContent();
    }

    // Tenant_Id từ TenantContext (TenantMiddleware phân giải) — không đọc header trực tiếp. ADR-018.
    private int GetTenantId()
        => HttpContext.RequestServices.GetRequiredService<Application.Interfaces.ITenantContext>().TenantId;
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
