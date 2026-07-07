// File    : UploadAttachmentCommand.cs
// Module  : Files
// Layer   : Application
// Purpose : Upload 1 tệp đính kèm tổng quát. Controller đọc multipart theo stream (spool ra đĩa) rồi
//           truyền Stream seekable + metadata. Handler: validate → tối ưu ảnh → dedup → lưu → gắn record.

using MediatR;

namespace ICare247.Application.Features.Files.UploadAttachment;

/// <param name="Content">Stream nội dung (đã spool, SEEKABLE). Handler đọc header + hash + lưu.</param>
/// <param name="FileName">Tên gốc.</param>
/// <param name="DeclaredContentType">MIME client khai (tham khảo — không tin tuyệt đối).</param>
/// <param name="Loai">Phân loại (vd 'Logo', 'HopDong').</param>
/// <param name="OwnerTable">Bảng chủ record đính kèm gắn vào (null = đính kèm rời).</param>
/// <param name="OwnerId">Id record chủ.</param>
/// <param name="FieldMa">Mã field (control Attachment) chứa tệp.</param>
/// <param name="UserId">Người upload (claim sub) — CreatedBy.</param>
public sealed record UploadAttachmentCommand(
    Stream Content, string FileName, string? DeclaredContentType, string? Loai,
    string? OwnerTable, long? OwnerId, string? FieldMa, long UserId)
    : IRequest<UploadAttachmentResult>;

/// <summary>Kết quả upload đính kèm. Success=false → controller trả 400 kèm ErrorCode/Message.</summary>
/// <param name="Success">Hợp lệ và đã lưu chưa.</param>
/// <param name="ErrorCode">Mã lỗi khi từ chối (null khi thành công).</param>
/// <param name="Message">Thông báo tiếng Việt (null khi thành công).</param>
/// <param name="AttachmentId">Id bản ghi TT_TepDinhKem.</param>
/// <param name="BlobId">Id nội dung (TT_TepBlob).</param>
/// <param name="TenFile">Tên gốc.</param>
/// <param name="ContentType">MIME sau tối ưu.</param>
/// <param name="KichThuoc">Kích thước nội dung chính (bytes).</param>
/// <param name="HasThumbnail">Có thumbnail không.</param>
public sealed record UploadAttachmentResult(
    bool Success, string? ErrorCode, string? Message,
    long AttachmentId, long BlobId, string TenFile, string ContentType, long KichThuoc, bool HasThumbnail)
{
    /// <summary>Tạo kết quả từ chối (validate).</summary>
    public static UploadAttachmentResult Fail(string code, string message)
        => new(false, code, message, 0, 0, "", "", 0, false);

    /// <summary>Tạo kết quả thành công.</summary>
    public static UploadAttachmentResult Ok(
        long attachmentId, long blobId, string tenFile, string contentType, long size, bool hasThumb)
        => new(true, null, null, attachmentId, blobId, tenFile, contentType, size, hasThumb);
}
