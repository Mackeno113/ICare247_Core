// File    : LinkAttachmentsCommand.cs
// Module  : Files
// Layer   : Application
// Purpose : Gắn loạt đính kèm "treo" (Owner_Id NULL) vào record vừa tạo — luồng đa-tệp-khi-thêm-mới.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Files.LinkAttachments;

/// <param name="AttachmentIds">Id các đính kèm cần gắn (client đã upload lúc thêm mới).</param>
/// <param name="OwnerTable">Bảng chủ.</param>
/// <param name="OwnerId">Id record vừa tạo.</param>
/// <param name="FieldMa">Mã field chứa tệp.</param>
/// <param name="UserId">Người thực hiện (chỉ gắn tệp do chính họ upload).</param>
public sealed record LinkAttachmentsCommand(
    IReadOnlyList<long> AttachmentIds, string OwnerTable, long OwnerId, string? FieldMa, long UserId)
    : IRequest<int>;

/// <summary>Delegate gắn Owner sang repository. Trả số bản ghi đã gắn.</summary>
public sealed class LinkAttachmentsCommandHandler : IRequestHandler<LinkAttachmentsCommand, int>
{
    private readonly IAttachmentRepository _attachments;

    public LinkAttachmentsCommandHandler(IAttachmentRepository attachments) => _attachments = attachments;

    /// <summary>Sự kiện theo sau: các dòng treo được set Owner_Table/Owner_Id/Field_Ma → thuộc về record.</summary>
    public Task<int> Handle(LinkAttachmentsCommand c, CancellationToken ct)
        => _attachments.LinkToOwnerAsync(c.AttachmentIds, c.OwnerTable, c.OwnerId, c.FieldMa, c.UserId, ct);
}
