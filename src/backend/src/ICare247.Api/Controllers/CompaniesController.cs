// File    : CompaniesController.cs
// Module  : Organization / Companies
// Layer   : Api
// Purpose : REST endpoint cây công ty (TC_CongTy) — tree/detail/options/lookup + CRUD.
//           Bắt buộc JWT ([Authorize]); tenant suy từ TenantContext (ADR-018), userId từ claim sub.

using ICare247.Application.Features.Organization.Companies.Commands;
using ICare247.Application.Features.Organization.Companies.Models;
using ICare247.Application.Features.Organization.Companies.Queries;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ICare247.Api.Controllers;

/// <summary>Quản lý công ty (cây tổ chức) trong Data DB tenant.</summary>
[ApiController]
[Route("api/v1/organization/companies")]
[Authorize]
public sealed class CompaniesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CompaniesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Cây công ty (phẳng — UI dựng cây qua Id/ChaId).</summary>
    /// <remarks>GET /api/v1/organization/companies/tree</remarks>
    [HttpGet("tree")]
    public async Task<IActionResult> GetTree(CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCompanyTreeQuery(), ct));

    /// <summary>Bộ option tham chiếu cho form (cấp công ty + ngân hàng).</summary>
    /// <remarks>GET /api/v1/organization/companies/options</remarks>
    [HttpGet("options")]
    public async Task<IActionResult> GetOptions(CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCompanyFormOptionsQuery(), ct));

    /// <summary>Tìm phường-xã cho cascade địa chỉ (Tỉnh suy ra).</summary>
    /// <remarks>GET /api/v1/organization/companies/phuong-xa?term=</remarks>
    [HttpGet("phuong-xa")]
    public async Task<IActionResult> SearchPhuongXa([FromQuery] string? term = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new SearchPhuongXaQuery(term), ct));

    /// <summary>Chi tiết 1 công ty (form Sửa).</summary>
    /// <remarks>GET /api/v1/organization/companies/{id}</remarks>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct = default)
    {
        var dto = await _mediator.Send(new GetCompanyByIdQuery(id), ct);
        return dto is null
            ? NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Không tìm thấy", Status = 404, Detail = $"Không tìm thấy công ty id={id}."
            })
            : Ok(dto);
    }

    /// <summary>Thêm mới công ty.</summary>
    /// <remarks>POST /api/v1/organization/companies</remarks>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CompanyInput body, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SaveCompanyCommand(null, body, GetTenantId(), GetUserId()), ct);
        return result.Success ? Ok(result) : UnprocessableEntity(result);
    }

    /// <summary>Cập nhật công ty theo Id.</summary>
    /// <remarks>PUT /api/v1/organization/companies/{id}</remarks>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] CompanyInput body, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SaveCompanyCommand(id, body, GetTenantId(), GetUserId()), ct);
        return result.Success ? Ok(result) : UnprocessableEntity(result);
    }

    /// <summary>Xóa mềm công ty — chặn (409) nếu còn công ty con/phòng ban.</summary>
    /// <remarks>DELETE /api/v1/organization/companies/{id}</remarks>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteCompanyCommand(id, GetTenantId(), GetUserId()), ct);
        if (result.Success) return Ok(result);

        return Conflict(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Không thể xóa — đang được sử dụng",
            Status = 409,
            Detail = "Công ty còn công ty con hoặc phòng ban đang gắn.",
            Extensions = { ["reason"] = result.Reason }
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private int GetTenantId()
        => HttpContext.RequestServices.GetRequiredService<ITenantContext>().TenantId;

    private long? GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return long.TryParse(raw, out var id) ? id : null;
    }
}
