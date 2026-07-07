// File    : UploadAttachmentCommandHandler.cs
// Module  : Files
// Layer   : Application
// Purpose : Orchestrate upload đính kèm: validate (allowlist/magic/mã thực thi) → tối ưu ảnh (+thumbnail)
//           → tính checksum → lưu qua IFileStore (dedup RefCount) → chèn bản ghi gắn record/field.

using System.Security.Cryptography;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Files.UploadAttachment;

/// <summary>
/// Handler upload đính kèm. Chỉ phụ thuộc interface Application; nội dung vật lý đi qua IFileStore
/// (Db/FileSystem/Object) do selector chọn theo kích thước. Dedup theo checksum ở TT_TepBlob.
/// </summary>
public sealed class UploadAttachmentCommandHandler
    : IRequestHandler<UploadAttachmentCommand, UploadAttachmentResult>
{
    private const int HeaderBytes = 8192; // đủ để soi magic-byte + sniff mã thực thi

    private readonly ITenantContext _tenant;
    private readonly IFileValidator _validator;
    private readonly IImageOptimizer _optimizer;
    private readonly IFileStoreSelector _selector;
    private readonly IStorageKeyBuilder _keyBuilder;
    private readonly ITepBlobRepository _blobs;
    private readonly IAttachmentRepository _attachments;

    public UploadAttachmentCommandHandler(
        ITenantContext tenant, IFileValidator validator, IImageOptimizer optimizer,
        IFileStoreSelector selector, IStorageKeyBuilder keyBuilder,
        ITepBlobRepository blobs, IAttachmentRepository attachments)
    {
        _tenant = tenant;
        _validator = validator;
        _optimizer = optimizer;
        _selector = selector;
        _keyBuilder = keyBuilder;
        _blobs = blobs;
        _attachments = attachments;
    }

    /// <summary>Xử lý upload. Sự kiện theo sau: chèn TT_TepBlob (hoặc RefCount++) + chèn TT_TepDinhKem.</summary>
    /// <param name="cmd">Lệnh upload (Stream seekable + metadata).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Kết quả upload; Success=false khi validate từ chối.</returns>
    public async Task<UploadAttachmentResult> Handle(UploadAttachmentCommand cmd, CancellationToken ct)
    {
        var content = cmd.Content;
        if (!content.CanSeek)
            throw new InvalidOperationException("Stream upload phải seekable (controller spool ra đĩa/bộ nhớ).");

        // ── 1. Đọc header + kích thước ───────────────────────────────────────────
        var header = await ReadHeaderAsync(content, ct);
        var size = content.Length;

        // ── 2. Validate (allowlist đuôi + magic-byte + sniff mã thực thi) ─────────
        var vr = _validator.Validate(cmd.FileName, cmd.DeclaredContentType, header, size);
        if (!vr.IsValid)
            return UploadAttachmentResult.Fail(vr.ErrorCode!, vr.Message!);

        var contentType = vr.ResolvedContentType ?? cmd.DeclaredContentType ?? "application/octet-stream";

        // ── 3. Tối ưu ảnh (resize/nén + thumbnail) — nguồn chuẩn phía server ──────
        byte[]? mainBytes = null;
        byte[]? thumbBytes = null;
        string? thumbContentType = null;
        if (_optimizer.CanOptimize(contentType))
        {
            content.Position = 0;
            var opt = await _optimizer.OptimizeAsync(content, ct);
            if (opt is not null)
            {
                mainBytes = opt.Main;
                contentType = opt.ContentType;
                thumbBytes = opt.Thumbnail;
                thumbContentType = opt.ThumbnailContentType;
                size = mainBytes.LongLength;
            }
        }

        // ── 4. Checksum + lưu nội dung chính (dedup) ─────────────────────────────
        string checksum;
        long mainBlobId;
        if (mainBytes is not null)
        {
            checksum = HexSha256(mainBytes);
            await using var ms = new MemoryStream(mainBytes, writable: false);
            mainBlobId = await StoreBlobAsync(checksum, contentType, size, ms, cmd, ct);
        }
        else
        {
            content.Position = 0;
            checksum = await HexSha256Async(content, ct);
            content.Position = 0;
            mainBlobId = await StoreBlobAsync(checksum, contentType, size, content, cmd, ct);
        }

        // ── 5. Thumbnail (nếu có) — cũng dedup như blob thường ────────────────────
        long? thumbBlobId = null;
        if (thumbBytes is not null)
        {
            var thumbSum = HexSha256(thumbBytes);
            await using var tms = new MemoryStream(thumbBytes, writable: false);
            thumbBlobId = await StoreBlobAsync(
                thumbSum, thumbContentType ?? "image/jpeg", thumbBytes.LongLength, tms, cmd, ct);
        }

        // ── 6. Chèn bản ghi đính kèm gắn record/field ────────────────────────────
        var attId = await _attachments.InsertAsync(new AttachmentInsert(
            mainBlobId, thumbBlobId, cmd.FileName, contentType, size,
            cmd.Loai, cmd.OwnerTable, cmd.OwnerId, cmd.FieldMa, checksum, cmd.UserId), ct);

        return UploadAttachmentResult.Ok(attId, mainBlobId, cmd.FileName, contentType, size, thumbBlobId is not null);
    }

    /// <summary>Lưu 1 khối nội dung: chọn store theo kích thước, dựng key tương đối, upsert blob (dedup).</summary>
    private async Task<long> StoreBlobAsync(
        string checksum, string contentType, long size, Stream stream, UploadAttachmentCommand cmd, CancellationToken ct)
    {
        var store = _selector.SelectForSize(size);
        var key = _keyBuilder.Build(_tenant.TenantId, cmd.Loai, checksum, cmd.FileName);
        var stored = await store.SaveAsync(key, stream, ct);
        return await _blobs.UpsertAsync(
            checksum, contentType, stored.SizeBytes, stored.StorageKind, stored.Content, stored.StorageKey, cmd.UserId, ct);
    }

    /// <summary>Đọc tối đa <see cref="HeaderBytes"/> byte đầu (không tiêu thụ stream — trả về vị trí 0).</summary>
    private static async Task<byte[]> ReadHeaderAsync(Stream s, CancellationToken ct)
    {
        s.Position = 0;
        var buf = new byte[HeaderBytes];
        var read = await ReadUpToAsync(s, buf, ct);
        s.Position = 0;
        return read == buf.Length ? buf : buf[..read];
    }

    /// <summary>Đọc lấp đầy buffer (hoặc tới hết stream). Trả số byte thực đọc.</summary>
    private static async Task<int> ReadUpToAsync(Stream s, byte[] buf, CancellationToken ct)
    {
        int total = 0;
        while (total < buf.Length)
        {
            var n = await s.ReadAsync(buf.AsMemory(total), ct);
            if (n == 0) break;
            total += n;
        }
        return total;
    }

    /// <summary>SHA256 hex (chữ thường) của mảng byte.</summary>
    private static string HexSha256(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    /// <summary>SHA256 hex (chữ thường) của stream — đọc hết, không nạp toàn bộ vào RAM.</summary>
    private static async Task<string> HexSha256Async(Stream s, CancellationToken ct)
    {
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(s, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
