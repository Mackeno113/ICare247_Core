// File    : AttachmentsController.cs
// Module  : Files
// Layer   : Api
// Purpose : Hệ đính kèm tổng quát — upload (streaming, spool ra đĩa → không nạp full RAM), stream tải về
//           (Content-Disposition: attachment + X-Content-Type-Options: nosniff + ETag/304), thumbnail
//           (inline cho <img>), liệt kê theo record, xóa. Đều [Authorize]. Validate + tối ưu ở handler.

using System.Security.Claims;
using ICare247.Application.Features.Files.DeleteAttachment;
using ICare247.Application.Features.Files.GetAttachment;
using ICare247.Application.Features.Files.LinkAttachments;
using ICare247.Application.Features.Files.ListAttachments;
using ICare247.Application.Features.Files.UploadAttachment;
using ICare247.Application.Files;
using ICare247.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICare247.Api.Controllers;

/// <summary>Quản lý tệp đính kèm tổng quát (mọi loại file, gắn record/field Form Engine). Bắt buộc JWT.</summary>
[ApiController]
[Route("api/v1/attachments")]
[Authorize]
public sealed class AttachmentsController : ControllerBase
{
    // Trần body cho endpoint upload — nới rộng để phủ MaxBytes (validator mới enforce mức thật từ config).
    private const long UploadBodyLimit = 110_000_000;

    private readonly IMediator _mediator;
    private readonly IFileStoreSelector _selector;

    public AttachmentsController(IMediator mediator, IFileStoreSelector selector)
    {
        _mediator = mediator;
        _selector = selector;
    }

    /// <summary>Upload 1 tệp đính kèm. multipart/form-data: file + (loai, ownerTable, ownerId, fieldMa) tuỳ chọn.</summary>
    /// <remarks>POST /api/v1/attachments — trả Id đính kèm + blob. Từ chối (400) nếu tệp không hợp lệ/không an toàn.</remarks>
    [HttpPost]
    [RequestSizeLimit(UploadBodyLimit)]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        [FromForm] string? loai,
        [FromForm] string? ownerTable,
        [FromForm] long? ownerId,
        [FromForm] string? fieldMa,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Chưa chọn tệp." });

        // Spool nội dung ra tệp tạm SEEKABLE (auto xóa khi đóng) — tránh nạp toàn bộ vào RAM khi file lớn.
        await using var src = file.OpenReadStream();
        await using var spooled = await SpoolToTempAsync(src, ct);

        var result = await _mediator.Send(new UploadAttachmentCommand(
            spooled, file.FileName, file.ContentType, loai, ownerTable, ownerId, fieldMa, GetUserId() ?? 0), ct);

        if (!result.Success)
            return BadRequest(new { error = result.Message, code = result.ErrorCode });
        return Ok(result);
    }

    /// <summary>Tải nội dung chính (attachment). ETag/304 + nosniff + Content-Disposition attachment (không render inline).</summary>
    /// <remarks>GET /api/v1/attachments/{id}</remarks>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Download(long id, CancellationToken ct = default)
        => await StreamAsync(id, thumbnail: false, asAttachment: true, ct);

    /// <summary>Xem thumbnail (inline, dùng cho &lt;img&gt;). nosniff + ETag/304. 404 nếu không có thumbnail.</summary>
    /// <remarks>GET /api/v1/attachments/{id}/thumbnail</remarks>
    [HttpGet("{id:long}/thumbnail")]
    public async Task<IActionResult> Thumbnail(long id, CancellationToken ct = default)
        => await StreamAsync(id, thumbnail: true, asAttachment: false, ct);

    /// <summary>Metadata 1 đính kèm theo Id (chế độ 1-tệp/cột — hiển thị tệp đang lưu). 404 nếu không có.</summary>
    /// <remarks>GET /api/v1/attachments/{id}/info</remarks>
    [HttpGet("{id:long}/info")]
    public async Task<IActionResult> Info(long id, CancellationToken ct = default)
    {
        var info = await _mediator.Send(new GetAttachmentInfoQuery(id), ct);
        return info is null ? NotFound() : Ok(info);
    }

    /// <summary>Liệt kê đính kèm của 1 record (lọc field nếu truyền).</summary>
    /// <remarks>GET /api/v1/attachments?ownerTable=..&amp;ownerId=..&amp;fieldMa=..</remarks>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? ownerTable, [FromQuery] long ownerId, [FromQuery] string? fieldMa,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ownerTable) || ownerId <= 0)
            return BadRequest(new { error = "Thiếu ownerTable/ownerId." });

        var items = await _mediator.Send(new ListAttachmentsQuery(ownerTable, ownerId, fieldMa), ct);
        return Ok(items);
    }

    /// <summary>Gắn loạt đính kèm "treo" vào record vừa tạo (đa-tệp-khi-thêm-mới). Gọi SAU khi lưu record.</summary>
    /// <remarks>POST /api/v1/attachments/link — body: attachmentIds[], ownerTable, ownerId, fieldMa?.</remarks>
    [HttpPost("link")]
    public async Task<IActionResult> Link([FromBody] LinkAttachmentsRequest? req, CancellationToken ct = default)
    {
        if (req?.AttachmentIds is null || req.AttachmentIds.Length == 0
            || string.IsNullOrWhiteSpace(req.OwnerTable) || req.OwnerId <= 0)
            return BadRequest(new { error = "Thiếu dữ liệu gắn đính kèm." });

        var linked = await _mediator.Send(new LinkAttachmentsCommand(
            req.AttachmentIds, req.OwnerTable, req.OwnerId, req.FieldMa, GetUserId() ?? 0), ct);
        return Ok(new { linked });
    }

    /// <summary>Xóa 1 đính kèm (soft-delete + giảm ref blob; ref=0 → dọn vật lý).</summary>
    /// <remarks>DELETE /api/v1/attachments/{id}</remarks>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct = default)
    {
        var ok = await _mediator.Send(new DeleteAttachmentCommand(id, GetUserId() ?? 0), ct);
        return ok ? NoContent() : NotFound();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Stream nội dung/thumbnail của đính kèm với ETag/304 + nosniff; mở stream qua IFileStore theo Storage_Kind.</summary>
    private async Task<IActionResult> StreamAsync(long id, bool thumbnail, bool asAttachment, CancellationToken ct)
    {
        var dto = await _mediator.Send(new GetAttachmentContentQuery(id, thumbnail), ct);
        if (dto is null) return NotFound();
        var blob = dto.Blob;

        // Chống MIME-sniffing (trình duyệt không tự đoán HTML/JS từ nội dung).
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        // ETag theo checksum — 304 nếu client còn cache khớp.
        if (!string.IsNullOrEmpty(blob.Checksum))
        {
            var etag = $"\"{blob.Checksum}\"";
            Response.Headers.CacheControl = "private, max-age=3600";
            Response.Headers.ETag = etag;
            if (Request.Headers.IfNoneMatch.ToString().Contains(blob.Checksum, StringComparison.Ordinal))
                return StatusCode(StatusCodes.Status304NotModified);
        }

        var stored = new StoredContent(blob.StorageKind, blob.StorageKey, blob.NoiDung, blob.KichThuoc);
        var stream = await _selector.SelectForKind(blob.StorageKind).OpenReadAsync(stored, ct);

        // Tải về = attachment (không cho trình duyệt render inline → an toàn); thumbnail = inline cho <img>.
        return asAttachment
            ? File(stream, blob.ContentType, fileDownloadName: dto.TenFile, enableRangeProcessing: true)
            : File(stream, blob.ContentType, enableRangeProcessing: true);
    }

    /// <summary>Copy stream nguồn ra tệp tạm seekable (FileOptions.DeleteOnClose) rồi tua về đầu.</summary>
    private static async Task<FileStream> SpoolToTempAsync(Stream src, CancellationToken ct)
    {
        var path = Path.Combine(Path.GetTempPath(), "icare_up_" + Guid.NewGuid().ToString("N") + ".tmp");
        var fs = new FileStream(
            path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
            bufferSize: 81920, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        try
        {
            await src.CopyToAsync(fs, ct);
            fs.Position = 0;
            return fs;
        }
        catch
        {
            await fs.DisposeAsync(); // xóa tệp tạm (DeleteOnClose) nếu copy lỗi
            throw;
        }
    }

    /// <summary>Id người dùng từ claim (JWT) — dùng cho CreatedBy/UpdatedBy.</summary>
    private long? GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return long.TryParse(raw, out var id) ? id : null;
    }
}

/// <summary>Body cho POST /attachments/link — gắn loạt đính kèm treo vào record vừa tạo.</summary>
/// <param name="AttachmentIds">Id các đính kèm cần gắn.</param>
/// <param name="OwnerTable">Bảng chủ.</param>
/// <param name="OwnerId">Id record vừa tạo.</param>
/// <param name="FieldMa">Mã field chứa tệp.</param>
public sealed record LinkAttachmentsRequest(long[] AttachmentIds, string OwnerTable, long OwnerId, string? FieldMa);
