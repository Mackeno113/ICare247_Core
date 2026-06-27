// File    : FilesController.cs
// Module  : Files
// Layer   : Api
// Purpose : Upload + phục vụ tệp (module logo/đính kèm). POST nhận multipart, validate type/magic/size
//           rồi lưu bytes vào TT_TepDinhKem; GET stream bytes có ETag/Cache-Control. Đều [Authorize].
//           Bảo mật (Tầng 4): allowlist MIME + kiểm magic-byte (chống đổi đuôi) + giới hạn kích thước.

using System.Security.Claims;
using System.Security.Cryptography;
using ICare247.Application.Features.Files.GetFile;
using ICare247.Application.Features.Files.UploadFile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICare247.Api.Controllers;

/// <summary>Quản lý tệp đính kèm (logo công ty + file khác). Bắt buộc JWT.</summary>
[ApiController]
[Route("api/v1/files")]
[Authorize]
public sealed class FilesController : ControllerBase
{
    private const long MaxBytes = 2 * 1024 * 1024; // 2MB cho ảnh logo
    private static readonly string[] AllowedTypes = ["image/png", "image/jpeg", "image/webp"];

    private readonly IMediator _mediator;

    public FilesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Upload 1 ảnh (logo). Trả Id để gán vào FK (vd TC_CongTy.Logo_Id).</summary>
    /// <remarks>POST /api/v1/files (multipart/form-data: file, loai?)</remarks>
    [HttpPost]
    [RequestSizeLimit(3_000_000)]
    public async Task<IActionResult> Upload(IFormFile? file, [FromForm] string? loai, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Chưa chọn tệp." });
        if (file.Length > MaxBytes)
            return BadRequest(new { error = "Tệp quá lớn (tối đa 2MB)." });

        var contentType = (file.ContentType ?? "").ToLowerInvariant();
        if (!AllowedTypes.Contains(contentType))
            return BadRequest(new { error = "Định dạng không hỗ trợ (chỉ png, jpeg, webp)." });

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        if (!MagicBytesMatch(bytes, contentType))
            return BadRequest(new { error = "Nội dung tệp không khớp định dạng ảnh khai báo." });

        var checksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var userId = GetUserId() ?? 0;

        var result = await _mediator.Send(
            new UploadFileCommand(bytes, file.FileName, contentType, loai, checksum, userId), ct);
        return Ok(result);
    }

    /// <summary>Stream nội dung tệp (inline) + ETag/Cache-Control. 304 nếu client còn cache khớp.</summary>
    /// <remarks>GET /api/v1/files/{id}</remarks>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct = default)
    {
        var f = await _mediator.Send(new GetFileQuery(id), ct);
        if (f is null || f.NoiDung is null) return NotFound();

        if (!string.IsNullOrEmpty(f.Checksum))
        {
            var etag = $"\"{f.Checksum}\"";
            Response.Headers.CacheControl = "private, max-age=3600";
            Response.Headers.ETag = etag;
            if (Request.Headers.IfNoneMatch.ToString().Contains(f.Checksum, StringComparison.Ordinal))
                return StatusCode(StatusCodes.Status304NotModified);
        }

        return File(f.NoiDung, f.ContentType); // không kèm filename → hiển thị inline (dùng cho <img>)
    }

    /// <summary>Id người dùng từ claim sub (JWT). Dùng cho CreatedBy.</summary>
    private long? GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return long.TryParse(raw, out var id) ? id : null;
    }

    /// <summary>Kiểm magic-byte khớp MIME khai báo (chống đổi đuôi file).</summary>
    private static bool MagicBytesMatch(byte[] b, string contentType) => contentType switch
    {
        "image/png"  => b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47,
        "image/jpeg" => b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
        // WEBP: "RIFF"...."WEBP"
        "image/webp" => b.Length >= 12
                        && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
                        && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50,
        _ => false,
    };
}
