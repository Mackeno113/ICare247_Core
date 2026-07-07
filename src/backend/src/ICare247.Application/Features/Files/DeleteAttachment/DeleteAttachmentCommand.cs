// File    : DeleteAttachmentCommand.cs
// Module  : Files
// Layer   : Application
// Purpose : Xóa 1 đính kèm: soft-delete bản ghi + giảm RefCount blob chính/thumbnail; ref về 0 → dọn vật lý.

using MediatR;

namespace ICare247.Application.Features.Files.DeleteAttachment;

/// <param name="AttachmentId">Id bản ghi TT_TepDinhKem cần xóa.</param>
/// <param name="UserId">Người thực hiện (UpdatedBy).</param>
public sealed record DeleteAttachmentCommand(long AttachmentId, long UserId) : IRequest<bool>;
