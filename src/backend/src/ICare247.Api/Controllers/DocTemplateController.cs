// File    : DocTemplateController.cs
// Module  : DocTemplate
// Layer   : Api
// Purpose : REST endpoint xuất Word/PDF theo mẫu — khám phá biến (proc) + render (ghép master/detail).
// Spec    : docs/spec/28_DOC_TEMPLATE_SPEC.md §7.3.

using System.Text.Json;
using ICare247.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICare247.Api.Controllers;

/// <summary>
/// Xuất tài liệu từ bộ mẫu <c>Doc_Template</c>. Mọi endpoint yêu cầu đăng nhập; proc bị chặn theo
/// whitelist <c>Doc_Proc_Registry</c> trong renderer (deny-by-default).
/// </summary>
[ApiController]
[Route("api/v1/doc-templates")]
[Authorize]
public sealed class DocTemplateController : ControllerBase
{
    private readonly IDocTemplateRenderer _renderer;
    private readonly ILogger<DocTemplateController> _logger;

    public DocTemplateController(IDocTemplateRenderer renderer, ILogger<DocTemplateController> logger)
    {
        _renderer = renderer;
        _logger = logger;
    }

    /// <summary>Khám phá biến (cột kết quả) của 1 stored proc — cho màn soạn kéo biến.</summary>
    [HttpGet("describe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Describe([FromQuery] string proc, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(proc))
            return Problem("Thiếu tham số 'proc'.", statusCode: StatusCodes.Status400BadRequest);
        try
        {
            var vars = await _renderer.DescribeVariablesAsync(proc, ct);
            return Ok(vars);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Sinh tài liệu từ bộ mẫu. Body = JSON tham số khóa (VD <c>{ "NhanVien_Id": 42 }</c>);
    /// <c>format</c> = <c>pdf</c> (mặc định) hoặc <c>docx</c>. Trả file tải xuống.
    /// </summary>
    [HttpPost("{id:long}/render")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Render(
        long id, [FromQuery] string format = "pdf",
        [FromBody] Dictionary<string, JsonElement>? keyParams = null, CancellationToken ct = default)
    {
        var fmt = string.Equals(format, "docx", StringComparison.OrdinalIgnoreCase)
            ? DocOutputFormat.Docx : DocOutputFormat.Pdf;
        var resolved = ToClrParams(keyParams);
        try
        {
            var result = await _renderer.RenderAsync(id, resolved, fmt, ct);
            return File(result.Bytes, result.ContentType, result.FileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Render bộ mẫu #{Id} thất bại.", id);
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Sinh tài liệu theo <c>Ma</c> bộ mẫu (thay vì Id) — dùng khi màn lưới gắn nút xuất qua
    /// <c>Ui_View_Action.Target = Ma</c>. Body = JSON dòng đang chọn (đầy đủ cột); <c>format</c> = pdf|docx.
    /// </summary>
    [HttpPost("by-code/{code}/render")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RenderByCode(
        string code, [FromQuery] string format = "pdf",
        [FromBody] Dictionary<string, JsonElement>? keyParams = null, CancellationToken ct = default)
    {
        var fmt = string.Equals(format, "docx", StringComparison.OrdinalIgnoreCase)
            ? DocOutputFormat.Docx : DocOutputFormat.Pdf;
        var resolved = ToClrParams(keyParams);
        try
        {
            var result = await _renderer.RenderByCodeAsync(code, resolved, fmt, ct);
            return File(result.Bytes, result.ContentType, result.FileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Render bộ mẫu mã '{Code}' thất bại.", code);
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Ép JsonElement (từ body) về CLR primitive để bind tham số Dapper. Không phát event.</summary>
    private static Dictionary<string, object?> ToClrParams(Dictionary<string, JsonElement>? src)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (src is null) return dict;
        foreach (var (k, v) in src)
        {
            dict[k] = v.ValueKind switch
            {
                JsonValueKind.Number => v.TryGetInt64(out var l) ? l : v.GetDouble(),
                JsonValueKind.String => v.GetString(),
                JsonValueKind.True   => true,
                JsonValueKind.False  => false,
                JsonValueKind.Null   => null,
                _                    => v.ToString()
            };
        }
        return dict;
    }
}
