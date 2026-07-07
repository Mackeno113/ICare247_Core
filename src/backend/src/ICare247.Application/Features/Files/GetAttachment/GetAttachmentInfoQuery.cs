// File    : GetAttachmentInfoQuery.cs
// Module  : Files
// Layer   : Application
// Purpose : Lấy metadata 1 đính kèm theo Id (chế độ 1-tệp/cột — hiển thị tệp đang lưu trong cột record).

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Files.GetAttachment;

/// <param name="AttachmentId">Id bản ghi TT_TepDinhKem.</param>
public sealed record GetAttachmentInfoQuery(long AttachmentId) : IRequest<AttachmentInfo?>;

/// <summary>Delegate đọc metadata 1 đính kèm sang repository.</summary>
public sealed class GetAttachmentInfoQueryHandler : IRequestHandler<GetAttachmentInfoQuery, AttachmentInfo?>
{
    private readonly IAttachmentRepository _attachments;

    public GetAttachmentInfoQueryHandler(IAttachmentRepository attachments) => _attachments = attachments;

    /// <summary>Không side-effect. Null nếu đính kèm không tồn tại/đã xóa.</summary>
    public Task<AttachmentInfo?> Handle(GetAttachmentInfoQuery q, CancellationToken ct)
        => _attachments.GetInfoAsync(q.AttachmentId, ct);
}
