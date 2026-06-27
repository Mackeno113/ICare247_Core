// File    : UploadFileCommandHandler.cs
// Module  : Files
// Layer   : Application
// Purpose : Persist tệp đã validate → IFileAttachmentRepository (Data DB). Trả metadata.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Files.UploadFile;

public sealed class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, FileUploadResultDto>
{
    private readonly IFileAttachmentRepository _repo;

    public UploadFileCommandHandler(IFileAttachmentRepository repo) => _repo = repo;

    /// <summary>Lưu tệp vào TT_TepDinhKem. Sự kiện theo sau: client nhận Id để gán vào FK (vd Logo_Id).</summary>
    public async Task<FileUploadResultDto> Handle(UploadFileCommand r, CancellationToken ct)
    {
        var id = await _repo.InsertAsync(
            r.FileName, r.ContentType, r.Content.LongLength, r.Content, r.Loai, r.Checksum, r.UserId, ct);
        return new FileUploadResultDto(id, r.FileName, r.ContentType, r.Content.LongLength);
    }
}
