// File    : ListAttachmentsQueryHandler.cs
// Module  : Files
// Layer   : Application
// Purpose : Delegate liệt kê đính kèm theo record chủ sang IAttachmentRepository.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Files.ListAttachments;

/// <summary>Trả danh sách metadata đính kèm theo (Owner_Table, Owner_Id, Field_Ma?).</summary>
public sealed class ListAttachmentsQueryHandler
    : IRequestHandler<ListAttachmentsQuery, IReadOnlyList<AttachmentInfo>>
{
    private readonly IAttachmentRepository _attachments;

    public ListAttachmentsQueryHandler(IAttachmentRepository attachments) => _attachments = attachments;

    /// <summary>Không side-effect. Trả list rỗng nếu record chưa có đính kèm.</summary>
    public Task<IReadOnlyList<AttachmentInfo>> Handle(ListAttachmentsQuery q, CancellationToken ct)
        => _attachments.ListByOwnerAsync(q.OwnerTable, q.OwnerId, q.FieldMa, ct);
}
