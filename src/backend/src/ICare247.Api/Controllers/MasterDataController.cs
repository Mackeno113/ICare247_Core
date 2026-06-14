// File    : MasterDataController.cs
// Module  : MasterData
// Layer   : Api
// Purpose : REST endpoint CRUD danh mục generic (metadata-driven) + soft-check tham chiếu.

using ICare247.Api.Authorization;
using ICare247.Application.Interfaces;
using ICare247.Application.Features.MasterData.Commands.DeleteMasterData;
using ICare247.Application.Features.MasterData.Commands.SaveMasterData;
using ICare247.Application.Features.MasterData.Queries.CheckMasterDataUsage;
using ICare247.Application.Features.MasterData.Queries.GetMasterDataFormInfo;
using ICare247.Application.Features.MasterData.Queries.GetMasterDataList;
using ICare247.Application.Features.MasterData.Queries.GetMasterDataRecord;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ICare247.Api.Controllers;

/// <summary>
/// CRUD dữ liệu danh mục (Master Data) — sinh động từ metadata Ui_Form/Ui_Field/Sys_Table.
/// Bảng đích đọc ở server theo Form_Code, client không gửi tên bảng.
/// </summary>
[ApiController]
[Route("api/v1/master-data")]
public sealed class MasterDataController : ControllerBase
{
    private readonly IMediator _mediator;

    public MasterDataController(IMediator mediator) => _mediator = mediator;

    /// <summary>Thông tin bảng đích + cột (cho Blazor render lưới + form).</summary>
    /// <remarks>GET /api/v1/master-data/{formCode}/info</remarks>
    [HttpGet("{formCode}/info")]
    [RequirePermissionForTarget("Form", PermissionOp.Xem)]
    public async Task<IActionResult> GetInfo(string formCode, CancellationToken ct = default)
    {
        var info = await _mediator.Send(new GetMasterDataFormInfoQuery(formCode, GetTenantId()), ct);
        return info is null ? FormNotFound(formCode) : Ok(info);
    }

    /// <summary>Danh sách bản ghi danh mục (search + active filter + paging).</summary>
    /// <remarks>GET /api/v1/master-data/{formCode}?search=&amp;activeOnly=true&amp;page=1&amp;pageSize=50</remarks>
    [HttpGet("{formCode}")]
    [RequirePermissionForTarget("Form", PermissionOp.Xem)]
    public async Task<IActionResult> GetList(
        string formCode,
        [FromQuery] string? search = null,
        [FromQuery] bool? activeOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetMasterDataListQuery(formCode, GetTenantId(), search, activeOnly, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Lấy 1 bản ghi theo PK (cho form Sửa).</summary>
    /// <remarks>GET /api/v1/master-data/{formCode}/{id}</remarks>
    [HttpGet("{formCode}/{id}")]
    [RequirePermissionForTarget("Form", PermissionOp.Xem)]
    public async Task<IActionResult> GetById(string formCode, string id, CancellationToken ct = default)
    {
        var record = await _mediator.Send(
            new GetMasterDataRecordQuery(formCode, GetTenantId(), CoerceId(id)), ct);
        return record is null
            ? NotFound(Problem404($"Không tìm thấy bản ghi id={id}."))
            : Ok(record);
    }

    /// <summary>Soft-check: bản ghi có đang bị tham chiếu ở đâu không (cho dialog xóa).</summary>
    /// <remarks>GET /api/v1/master-data/{formCode}/{id}/usage</remarks>
    [HttpGet("{formCode}/{id}/usage")]
    [RequirePermissionForTarget("Form", PermissionOp.Xem)]
    public async Task<IActionResult> GetUsage(string formCode, string id, CancellationToken ct = default)
    {
        var usages = await _mediator.Send(
            new CheckMasterDataUsageQuery(formCode, GetTenantId(), CoerceId(id)), ct);
        return Ok(new { used = usages.Count > 0, usages });
    }

    /// <summary>Thêm mới 1 bản ghi danh mục.</summary>
    /// <remarks>POST /api/v1/master-data/{formCode} — Body: { "values": { ... } }</remarks>
    [HttpPost("{formCode}")]
    [RequirePermissionForTarget("Form", PermissionOp.Them)]
    public async Task<IActionResult> Create(
        string formCode, [FromBody] SaveMasterDataRequest body, CancellationToken ct = default)
    {
        if (body.Values is null || body.Values.Count == 0)
            return BadRequest(Problem400("Cần ít nhất một cột để thêm mới."));

        var result = await _mediator.Send(
            new SaveMasterDataCommand(formCode, GetTenantId(), Id: null, NormalizeValues(body.Values)), ct);

        // Validation fail → 422 kèm danh sách lỗi field
        return result.Success ? Ok(result) : UnprocessableEntity(result);
    }

    /// <summary>Cập nhật 1 bản ghi danh mục theo PK.</summary>
    /// <remarks>PUT /api/v1/master-data/{formCode}/{id} — Body: { "values": { ... } }</remarks>
    [HttpPut("{formCode}/{id}")]
    [RequirePermissionForTarget("Form", PermissionOp.Sua)]
    public async Task<IActionResult> Update(
        string formCode, string id, [FromBody] SaveMasterDataRequest body, CancellationToken ct = default)
    {
        if (body.Values is null || body.Values.Count == 0)
            return BadRequest(Problem400("Cần ít nhất một cột để cập nhật."));

        var result = await _mediator.Send(
            new SaveMasterDataCommand(formCode, GetTenantId(), CoerceId(id), NormalizeValues(body.Values)), ct);

        return result.Success ? Ok(result) : UnprocessableEntity(result);
    }

    /// <summary>Xóa cứng 1 bản ghi — chặn (409) nếu đang bị tham chiếu.</summary>
    /// <remarks>DELETE /api/v1/master-data/{formCode}/{id}</remarks>
    [HttpDelete("{formCode}/{id}")]
    [RequirePermissionForTarget("Form", PermissionOp.Xoa)]
    public async Task<IActionResult> Delete(string formCode, string id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new DeleteMasterDataCommand(formCode, GetTenantId(), CoerceId(id)), ct);

        if (result.Success) return Ok(result);

        // Bị tham chiếu → 409 Conflict kèm danh sách nơi đang dùng
        return Conflict(new ProblemDetails
        {
            Type   = "https://tools.ietf.org/html/rfc7807",
            Title  = "Không thể xóa — đang được sử dụng",
            Status = 409,
            Detail = "Bản ghi đang được tham chiếu ở nơi khác. Gỡ tham chiếu trước khi xóa.",
            Extensions = { ["blockedBy"] = result.BlockedBy }
        });
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    /// <summary>Ép id từ route string sang số nếu có thể (PK thường là int/bigint).</summary>
    private static object CoerceId(string id)
    {
        if (long.TryParse(id, out var l)) return l;
        return id;
    }

    /// <summary>
    /// Unwrap giá trị <see cref="JsonElement"/> (do bind JSON vào Dictionary&lt;string,object&gt;) → kiểu CLR
    /// nguyên thủy. Dapper KHÔNG bind được JsonElement làm tham số → phải chuyển trước khi xuống repo.
    /// </summary>
    private static Dictionary<string, object?> NormalizeValues(Dictionary<string, object?> values)
        => values.ToDictionary(kv => kv.Key, kv => UnwrapJson(kv.Value), StringComparer.OrdinalIgnoreCase);

    private static object? UnwrapJson(object? value)
    {
        if (value is not JsonElement je) return value;
        return je.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => je.GetString(),
            JsonValueKind.True   => true,
            JsonValueKind.False  => false,
            JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDecimal(),
            _                    => je.ToString()
        };
    }

    // Tenant_Id từ TenantContext (TenantMiddleware phân giải) — không đọc header trực tiếp. ADR-018.
    private int GetTenantId()
        => HttpContext.RequestServices.GetRequiredService<Application.Interfaces.ITenantContext>().TenantId;

    private NotFoundObjectResult FormNotFound(string formCode) =>
        NotFound(Problem404($"Không tìm thấy form danh mục '{formCode}'."));

    private static ProblemDetails Problem400(string detail) => new()
    { Type = "https://tools.ietf.org/html/rfc7807", Title = "Yêu cầu không hợp lệ", Status = 400, Detail = detail };

    private static ProblemDetails Problem404(string detail) => new()
    { Type = "https://tools.ietf.org/html/rfc7807", Title = "Không tìm thấy", Status = 404, Detail = detail };
}

/// <summary>Request body cho POST/PUT master-data.</summary>
public sealed class SaveMasterDataRequest
{
    /// <summary>Cặp Cột↔Giá trị (key = tên cột DB = Field_Code).</summary>
    [JsonPropertyName("values")]
    public Dictionary<string, object?> Values { get; init; } = [];
}
