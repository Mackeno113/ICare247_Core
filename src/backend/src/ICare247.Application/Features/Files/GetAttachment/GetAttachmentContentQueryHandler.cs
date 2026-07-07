// File    : GetAttachmentContentQueryHandler.cs
// Module  : Files
// Layer   : Application
// Purpose : Phân giải đính kèm → blob (chính/thumbnail) → nội dung để stream. Null nếu không tồn tại.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Files.GetAttachment;

/// <summary>Đọc bản ghi đính kèm rồi blob tương ứng (chính hoặc thumbnail).</summary>
public sealed class GetAttachmentContentQueryHandler
    : IRequestHandler<GetAttachmentContentQuery, AttachmentContentDto?>
{
    private readonly IAttachmentRepository _attachments;
    private readonly ITepBlobRepository _blobs;

    public GetAttachmentContentQueryHandler(IAttachmentRepository attachments, ITepBlobRepository blobs)
    {
        _attachments = attachments;
        _blobs = blobs;
    }

    /// <summary>Trả nội dung blob của đính kèm. Không side-effect.</summary>
    /// <param name="q">Query (Id + cờ thumbnail).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="AttachmentContentDto"/> hoặc null nếu đính kèm/blob không tồn tại.</returns>
    public async Task<AttachmentContentDto?> Handle(GetAttachmentContentQuery q, CancellationToken ct)
    {
        var att = await _attachments.GetAsync(q.AttachmentId, ct);
        if (att is null) return null;

        var blobId = q.Thumbnail ? att.ThumbBlobId : att.BlobId;
        if (blobId is null) return null;

        var blob = await _blobs.GetContentAsync(blobId.Value, ct);
        if (blob is null) return null;

        return new AttachmentContentDto(att.TenFile, blob);
    }
}
