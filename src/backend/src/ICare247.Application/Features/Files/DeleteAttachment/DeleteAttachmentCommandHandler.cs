// File    : DeleteAttachmentCommandHandler.cs
// Module  : Files
// Layer   : Application
// Purpose : Xóa đính kèm an toàn với dedup: soft-delete dòng đính kèm, giảm ref blob chính + thumbnail,
//           blob nào ref về 0 thì xóa nội dung vật lý (IFileStore) rồi đánh dấu blob đã xóa.

using ICare247.Application.Files;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Files.DeleteAttachment;

/// <summary>Xử lý xóa đính kèm. Tôn trọng dedup: chỉ dọn blob khi không còn đính kèm nào trỏ tới.</summary>
public sealed class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand, bool>
{
    private readonly IAttachmentRepository _attachments;
    private readonly ITepBlobRepository _blobs;
    private readonly IFileStoreSelector _selector;

    public DeleteAttachmentCommandHandler(
        IAttachmentRepository attachments, ITepBlobRepository blobs, IFileStoreSelector selector)
    {
        _attachments = attachments;
        _blobs = blobs;
        _selector = selector;
    }

    /// <summary>
    /// Soft-delete đính kèm + giảm ref blob. Sự kiện theo sau: blob ref=0 → xóa file vật lý + IsDeleted=1.
    /// </summary>
    /// <param name="cmd">Lệnh xóa.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>true nếu đã xóa; false nếu đính kèm không tồn tại.</returns>
    public async Task<bool> Handle(DeleteAttachmentCommand cmd, CancellationToken ct)
    {
        var att = await _attachments.GetAsync(cmd.AttachmentId, ct);
        if (att is null) return false;

        await _attachments.SoftDeleteAsync(cmd.AttachmentId, cmd.UserId, ct);

        // Giảm ref blob chính + thumbnail; blob nào về 0 → dọn nội dung vật lý.
        await ReleaseBlobAsync(att.BlobId, cmd.UserId, ct);
        await ReleaseBlobAsync(att.ThumbBlobId, cmd.UserId, ct);
        return true;
    }

    /// <summary>Giảm RefCount 1 blob; nếu về 0 thì xóa vật lý (theo Storage_Kind) rồi đánh dấu đã xóa.</summary>
    private async Task ReleaseBlobAsync(long? blobId, long userId, CancellationToken ct)
    {
        if (blobId is null) return;

        var newRef = await _blobs.DecrementRefAsync(blobId.Value, userId, ct);
        if (newRef > 0) return; // còn đính kèm khác dùng chung nội dung → giữ lại (dedup)

        // Ref về 0: đọc mô tả nơi lưu → xóa file/object → đánh dấu blob đã xóa.
        var blob = await _blobs.GetContentAsync(blobId.Value, ct);
        if (blob is not null)
        {
            var store = _selector.SelectForKind(blob.StorageKind);
            await store.DeleteAsync(
                new StoredContent(blob.StorageKind, blob.StorageKey, blob.NoiDung, blob.KichThuoc), ct);
        }
        await _blobs.MarkDeletedAsync(blobId.Value, userId, ct);
    }
}
