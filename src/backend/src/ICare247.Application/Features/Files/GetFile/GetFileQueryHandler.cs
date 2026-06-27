// File    : GetFileQueryHandler.cs
// Module  : Files
// Layer   : Application
// Purpose : Delegate đọc tệp sang IFileAttachmentRepository.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Files.GetFile;

public sealed class GetFileQueryHandler : IRequestHandler<GetFileQuery, FileContentDto?>
{
    private readonly IFileAttachmentRepository _repo;

    public GetFileQueryHandler(IFileAttachmentRepository repo) => _repo = repo;

    public Task<FileContentDto?> Handle(GetFileQuery r, CancellationToken ct) => _repo.GetAsync(r.Id, ct);
}
