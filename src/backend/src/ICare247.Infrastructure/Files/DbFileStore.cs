// File    : DbFileStore.cs
// Module  : Files
// Layer   : Infrastructure
// Purpose : IFileStore backend "Db" — nội dung nằm trong cột VARBINARY của TT_TepBlob.
//           Store chỉ buffer bytes; repository mới thật sự ghi/đọc DB. Luôn node-safe (chung Data DB).

using ICare247.Application.Files;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Files;

/// <summary>
/// Backend lưu bytes trong DB. Dùng cho file nhỏ (≤ ngưỡng) hoặc deployment không có shared storage.
/// Không tự truy cập DB — trả bytes qua <see cref="StoredContent"/> để repository ghi TT_TepBlob.
/// </summary>
public sealed class DbFileStore : IFileStore
{
    /// <inheritdoc />
    public string Kind => FileStorageProviders.Db;

    /// <inheritdoc />
    public async Task<StoredContent> SaveAsync(string relativeKey, Stream content, CancellationToken ct = default)
    {
        // Db không dùng key vật lý — chỉ gom bytes để repository lưu vào cột NoiDung.
        await using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        return new StoredContent(FileStorageProviders.Db, StorageKey: null, Content: bytes, SizeBytes: bytes.LongLength);
    }

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(StoredContent stored, CancellationToken ct = default)
    {
        // Bytes đã nằm trong stored.Content (repository đọc từ TT_TepBlob rồi truyền vào).
        var bytes = stored.Content ?? [];
        return Task.FromResult<Stream>(new MemoryStream(bytes, writable: false));
    }

    /// <inheritdoc />
    public Task DeleteAsync(StoredContent stored, CancellationToken ct = default)
        => Task.CompletedTask; // No-op: xóa dòng TT_TepBlob là đủ (không có file vật lý ngoài DB).

    /// <inheritdoc />
    public Task<FileStoreHealth> CheckHealthAsync(CancellationToken ct = default)
        => Task.FromResult(new FileStoreHealth(true, "Db store sẵn sàng (bytes trong DB)."));
}
