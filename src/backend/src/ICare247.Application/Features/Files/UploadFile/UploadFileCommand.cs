// File    : UploadFileCommand.cs
// Module  : Files
// Layer   : Application
// Purpose : Lưu 1 tệp đã được controller đọc + validate (type/magic/size). Bytes → TT_TepDinhKem.
//           Controller chịu trách nhiệm validate; handler chỉ persist (tách I/O HTTP khỏi nghiệp vụ lưu).

using MediatR;

namespace ICare247.Application.Features.Files.UploadFile;

/// <param name="Content">Bytes tệp (đã validate).</param>
/// <param name="FileName">Tên gốc.</param>
/// <param name="ContentType">MIME đã kiểm (image/png|jpeg|webp).</param>
/// <param name="Loai">Phân loại tuỳ chọn (vd 'Logo').</param>
/// <param name="Checksum">sha256 hex (ETag + toàn vẹn).</param>
/// <param name="UserId">Người upload (claim sub) — ghi CreatedBy.</param>
public sealed record UploadFileCommand(
    byte[] Content, string FileName, string ContentType,
    string? Loai, string? Checksum, long UserId) : IRequest<FileUploadResultDto>;

/// <summary>Kết quả upload trả về client (KHÔNG kèm bytes).</summary>
public sealed record FileUploadResultDto(long Id, string TenFile, string ContentType, long KichThuoc);
