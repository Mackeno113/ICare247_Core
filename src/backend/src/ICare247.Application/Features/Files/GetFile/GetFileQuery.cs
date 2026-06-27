// File    : GetFileQuery.cs
// Module  : Files
// Layer   : Application
// Purpose : Đọc nội dung + metadata 1 tệp (TT_TepDinhKem) để stream về client.

using MediatR;

namespace ICare247.Application.Features.Files.GetFile;

/// <param name="Id">Id tệp.</param>
public sealed record GetFileQuery(long Id) : IRequest<FileContentDto?>;

/// <summary>Nội dung + metadata tệp (kèm bytes để stream).</summary>
public sealed record FileContentDto(
    long Id, string TenFile, string ContentType, long KichThuoc, byte[] NoiDung, string? Checksum);
