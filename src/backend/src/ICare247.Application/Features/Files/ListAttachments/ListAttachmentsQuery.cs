// File    : ListAttachmentsQuery.cs
// Module  : Files
// Layer   : Application
// Purpose : Liệt kê đính kèm của 1 record (và field nếu lọc) — metadata cho UI hiển thị danh sách.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Files.ListAttachments;

/// <param name="OwnerTable">Bảng chủ.</param>
/// <param name="OwnerId">Id record chủ.</param>
/// <param name="FieldMa">Mã field (null = mọi field của record).</param>
public sealed record ListAttachmentsQuery(string OwnerTable, long OwnerId, string? FieldMa)
    : IRequest<IReadOnlyList<AttachmentInfo>>;
