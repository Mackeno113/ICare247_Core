// File    : IAttachmentRepository.cs
// Module  : Files
// Layer   : Application
// Purpose : Repository bản ghi đính kèm (TT_TepDinhKem) — mỗi lần dùng 1 dòng trỏ tới TT_TepBlob,
//           gắn vào Owner_Table/Owner_Id/Field_Ma (record/field của Form Engine).

namespace ICare247.Application.Interfaces;

/// <summary>Truy cập TT_TepDinhKem (Data DB tenant) theo mô hình Blob⟂Attachment.</summary>
public interface IAttachmentRepository
{
    /// <summary>Chèn 1 bản ghi đính kèm trỏ tới blob (và thumbnail nếu có). Trả Id mới.</summary>
    /// <param name="data">Dữ liệu bản ghi đính kèm.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Id bản ghi TT_TepDinhKem mới.</returns>
    Task<long> InsertAsync(AttachmentInsert data, CancellationToken ct = default);

    /// <summary>Liệt kê đính kèm theo record chủ (và field nếu lọc). Chỉ dòng chưa xóa.</summary>
    /// <param name="ownerTable">Bảng chủ.</param>
    /// <param name="ownerId">Id record chủ.</param>
    /// <param name="fieldMa">Mã field (null = mọi field của record).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách metadata đính kèm (không kèm bytes).</returns>
    Task<IReadOnlyList<AttachmentInfo>> ListByOwnerAsync(
        string ownerTable, long ownerId, string? fieldMa, CancellationToken ct = default);

    /// <summary>Đọc 1 bản ghi đính kèm (gồm Blob_Id/ThumbBlob_Id để stream). Null nếu không có/đã xóa.</summary>
    /// <param name="id">Id bản ghi.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="AttachmentRow"/> hoặc null.</returns>
    Task<AttachmentRow?> GetAsync(long id, CancellationToken ct = default);

    /// <summary>Đọc metadata 1 đính kèm theo Id (cho chế độ 1-tệp/cột: hiển thị tệp đang lưu). Null nếu không có.</summary>
    /// <param name="id">Id bản ghi.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="AttachmentInfo"/> hoặc null.</returns>
    Task<AttachmentInfo?> GetInfoAsync(long id, CancellationToken ct = default);

    /// <summary>Soft-delete 1 bản ghi đính kèm (IsDeleted=1). Không đụng blob (caller tự giảm ref).</summary>
    /// <param name="id">Id bản ghi.</param>
    /// <param name="userId">Người thực hiện.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SoftDeleteAsync(long id, long userId, CancellationToken ct = default);

    /// <summary>
    /// Gắn loạt đính kèm "treo" (Owner_Id NULL) vào record vừa tạo. Dùng cho luồng đa-tệp-khi-thêm-mới:
    /// upload trước lúc chưa có Id → sau khi lưu record có Id → gọi hàm này set Owner.
    /// </summary>
    /// <param name="ids">Danh sách Id đính kèm cần gắn.</param>
    /// <param name="ownerTable">Bảng chủ.</param>
    /// <param name="ownerId">Id record vừa tạo.</param>
    /// <param name="fieldMa">Mã field chứa tệp.</param>
    /// <param name="userId">Người thực hiện — CHỈ gắn tệp do chính người này upload (an toàn).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Số bản ghi đã gắn.</returns>
    /// <remarks>Chỉ gắn dòng còn treo (Owner_Id IS NULL) + đúng CreatedBy — không cướp tệp đã gắn của record khác.</remarks>
    Task<int> LinkToOwnerAsync(
        IReadOnlyList<long> ids, string ownerTable, long ownerId, string? fieldMa, long userId,
        CancellationToken ct = default);
}

/// <summary>Dữ liệu chèn 1 bản ghi đính kèm.</summary>
/// <param name="BlobId">FK nội dung chính (TT_TepBlob).</param>
/// <param name="ThumbBlobId">FK thumbnail (ảnh); null nếu không có.</param>
/// <param name="TenFile">Tên gốc khi upload.</param>
/// <param name="ContentType">MIME (đã tối ưu nếu là ảnh).</param>
/// <param name="KichThuoc">Kích thước nội dung chính (bytes).</param>
/// <param name="Loai">Phân loại (vd 'Logo', 'HopDong').</param>
/// <param name="OwnerTable">Bảng chủ (null nếu đính kèm rời chưa gắn).</param>
/// <param name="OwnerId">Id record chủ.</param>
/// <param name="FieldMa">Mã field chứa tệp.</param>
/// <param name="Checksum">SHA256 hex nội dung chính (ETag).</param>
/// <param name="UserId">Người upload (CreatedBy).</param>
public sealed record AttachmentInsert(
    long BlobId, long? ThumbBlobId, string TenFile, string ContentType, long KichThuoc,
    string? Loai, string? OwnerTable, long? OwnerId, string? FieldMa, string Checksum, long UserId);

/// <summary>Metadata 1 đính kèm (danh sách) — không kèm bytes.</summary>
/// <param name="Id">Id bản ghi.</param>
/// <param name="TenFile">Tên gốc.</param>
/// <param name="ContentType">MIME.</param>
/// <param name="KichThuoc">Kích thước (bytes).</param>
/// <param name="HasThumbnail">Có thumbnail không.</param>
/// <param name="CreatedAt">Thời điểm tạo (UTC).</param>
public sealed record AttachmentInfo(
    long Id, string TenFile, string ContentType, long KichThuoc, bool HasThumbnail, DateTime CreatedAt);

/// <summary>Bản ghi đính kèm đủ khóa để stream nội dung/thumbnail.</summary>
/// <param name="Id">Id bản ghi.</param>
/// <param name="BlobId">FK nội dung chính.</param>
/// <param name="ThumbBlobId">FK thumbnail (nếu có).</param>
/// <param name="TenFile">Tên gốc.</param>
/// <param name="ContentType">MIME.</param>
public sealed record AttachmentRow(
    long Id, long? BlobId, long? ThumbBlobId, string TenFile, string ContentType);
