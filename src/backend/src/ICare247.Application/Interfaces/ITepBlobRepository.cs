// File    : ITepBlobRepository.cs
// Module  : Files
// Layer   : Application
// Purpose : Repository nội dung vật lý (TT_TepBlob) — đơn vị dedup theo Checksum + đếm tham chiếu (RefCount).
//           Upsert race-safe (MERGE HOLDLOCK); giảm ref về 0 → đánh dấu xóa để dọn vật lý.

namespace ICare247.Application.Interfaces;

/// <summary>Truy cập TT_TepBlob (Data DB tenant). Dedup theo Checksum, quản lý RefCount.</summary>
public interface ITepBlobRepository
{
    /// <summary>
    /// Upsert blob theo Checksum: đã có → RefCount++ trả Id cũ; chưa có → chèn mới RefCount=1.
    /// </summary>
    /// <param name="checksum">SHA256 hex của nội dung (khóa dedup).</param>
    /// <param name="contentType">MIME nội dung.</param>
    /// <param name="kichThuoc">Kích thước (bytes).</param>
    /// <param name="storageKind">Db | FileSystem | Object.</param>
    /// <param name="noiDung">Bytes (khi Db); null khi FileSystem/Object.</param>
    /// <param name="storageKey">Key/path tương đối (khi FileSystem/Object); null khi Db.</param>
    /// <param name="userId">Người thực hiện (CreatedBy/UpdatedBy).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Id của blob (mới hoặc đã tồn tại).</returns>
    /// <remarks>Race-safe bằng MERGE HOLDLOCK — 2 upload cùng checksum đồng thời không tạo 2 dòng.</remarks>
    Task<long> UpsertAsync(
        string checksum, string contentType, long kichThuoc, string storageKind,
        byte[]? noiDung, string? storageKey, long userId, CancellationToken ct = default);

    /// <summary>Đọc nội dung + mô tả nơi lưu 1 blob (chỉ dòng chưa xóa). Null nếu không có.</summary>
    /// <param name="blobId">Id blob.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="TepBlobContent"/> hoặc null.</returns>
    Task<TepBlobContent?> GetContentAsync(long blobId, CancellationToken ct = default);

    /// <summary>Giảm RefCount 1 đơn vị (không âm). Trả RefCount MỚI để caller quyết định dọn vật lý.</summary>
    /// <param name="blobId">Id blob.</param>
    /// <param name="userId">Người thực hiện.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>RefCount sau khi giảm; -1 nếu blob không tồn tại.</returns>
    /// <remarks>Về 0 → caller gọi <see cref="MarkDeletedAsync"/> + xóa file vật lý qua IFileStore.</remarks>
    Task<int> DecrementRefAsync(long blobId, long userId, CancellationToken ct = default);

    /// <summary>Đánh dấu blob đã xóa (IsDeleted=1) sau khi nội dung vật lý đã được dọn.</summary>
    /// <param name="blobId">Id blob.</param>
    /// <param name="userId">Người thực hiện.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkDeletedAsync(long blobId, long userId, CancellationToken ct = default);
}

/// <summary>Nội dung + mô tả nơi lưu 1 blob (đọc từ TT_TepBlob để stream/xóa).</summary>
/// <param name="Id">Id blob.</param>
/// <param name="StorageKind">Db | FileSystem | Object.</param>
/// <param name="NoiDung">Bytes (khi Db); null khác.</param>
/// <param name="StorageKey">Key tương đối (khi FileSystem/Object); null khi Db.</param>
/// <param name="ContentType">MIME.</param>
/// <param name="KichThuoc">Kích thước (bytes).</param>
/// <param name="Checksum">SHA256 hex (ETag + toàn vẹn).</param>
public sealed record TepBlobContent(
    long Id, string StorageKind, byte[]? NoiDung, string? StorageKey,
    string ContentType, long KichThuoc, string Checksum);
