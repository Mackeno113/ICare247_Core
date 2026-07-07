// File    : ObjectFileStore.cs
// Module  : Files
// Layer   : Infrastructure
// Purpose : IFileStore backend "Object" (MinIO/S3/Azure Blob) — STUB Phase 1. Interface + health-check
//           đã sẵn; I/O thật cắm ở phase sau khi chốt SDK. CheckHealth trả "chưa cấu hình" → fail-fast
//           nếu deployment chọn Provider=Object mà chưa cắm (không âm thầm chạy sai).

using ICare247.Application.Files;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace ICare247.Infrastructure.Files;

/// <summary>
/// Backend object storage — CHƯA cài đặt I/O (chờ chốt SDK MinIO/S3/Azure). Mọi thao tác lưu/đọc/xóa
/// ném <see cref="NotSupportedException"/>; <see cref="CheckHealthAsync"/> báo chưa sẵn sàng để chặn khởi động.
/// </summary>
public sealed class ObjectFileStore : IFileStore
{
    private const string NotReady = "Object storage chưa được cài đặt (Phase sau — cần chốt SDK MinIO/S3/Azure).";

    private readonly ObjectStorageOptions _obj;

    /// <summary>Khởi tạo với cấu hình object storage (đọc để báo trạng thái health).</summary>
    /// <param name="opts">Tùy chọn <see cref="FileStorageOptions"/> đã bind.</param>
    public ObjectFileStore(IOptions<FileStorageOptions> opts) => _obj = opts.Value.Object;

    /// <inheritdoc />
    public string Kind => FileStorageProviders.Object;

    /// <inheritdoc />
    public Task<StoredContent> SaveAsync(string relativeKey, Stream content, CancellationToken ct = default)
        => throw new NotSupportedException(NotReady);

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(StoredContent stored, CancellationToken ct = default)
        => throw new NotSupportedException(NotReady);

    /// <inheritdoc />
    public Task DeleteAsync(StoredContent stored, CancellationToken ct = default)
        => throw new NotSupportedException(NotReady);

    /// <inheritdoc />
    public Task<FileStoreHealth> CheckHealthAsync(CancellationToken ct = default)
    {
        var detail = string.IsNullOrWhiteSpace(_obj.Endpoint)
            ? "Object storage chưa cấu hình Endpoint."
            : NotReady;
        return Task.FromResult(new FileStoreHealth(false, detail));
    }
}
