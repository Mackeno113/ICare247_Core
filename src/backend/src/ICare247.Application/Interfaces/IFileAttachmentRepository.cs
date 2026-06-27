// File    : IFileAttachmentRepository.cs
// Module  : Files
// Layer   : Application
// Purpose : Lưu/đọc tệp đính kèm (TT_TepDinhKem) ở Data DB tenant — module upload (logo + file khác).
//           Phương án A: bytes trong DB (VARBINARY). Storage_Kind để mở rộng backend sau.

using ICare247.Application.Features.Files.GetFile;

namespace ICare247.Application.Interfaces;

/// <summary>Repository tệp đính kèm (Data DB). Bytes lưu trong cột VARBINARY (Storage_Kind='Db').</summary>
public interface IFileAttachmentRepository
{
    /// <summary>Chèn 1 tệp (bytes trong DB). Trả Id mới.</summary>
    Task<long> InsertAsync(
        string tenFile, string contentType, long kichThuoc, byte[] noiDung,
        string? loai, string? checksum, long userId, CancellationToken ct = default);

    /// <summary>Đọc nội dung + metadata 1 tệp (chỉ dòng chưa xóa). Null nếu không có.</summary>
    Task<FileContentDto?> GetAsync(long id, CancellationToken ct = default);
}
