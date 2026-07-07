// File    : FileStoreSelector.cs
// Module  : Files
// Layer   : Infrastructure
// Purpose : Định tuyến IFileStore (impl IFileStoreSelector). Ghi: ≤ ngưỡng → Db, lớn hơn → provider cấu hình.
//           Đọc/xóa: theo Storage_Kind đã lưu. Gom mọi store đã đăng ký DI vào map theo Kind.

using ICare247.Application.Files;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace ICare247.Infrastructure.Files;

/// <summary>
/// Chọn backend đúng theo chính sách: kích thước (khi ghi) và Storage_Kind (khi đọc/xóa).
/// Provider cho file lớn lấy từ <see cref="FileStorageOptions.Provider"/>.
/// </summary>
public sealed class FileStoreSelector : IFileStoreSelector
{
    private readonly IReadOnlyDictionary<string, IFileStore> _byKind;
    private readonly FileStorageOptions _opts;

    /// <summary>Khởi tạo: gom mọi <see cref="IFileStore"/> đã đăng ký thành map theo Kind.</summary>
    /// <param name="stores">Tất cả store DI (Db/FileSystem/Object).</param>
    /// <param name="opts">Tùy chọn lưu trữ (ngưỡng + provider).</param>
    public FileStoreSelector(IEnumerable<IFileStore> stores, IOptions<FileStorageOptions> opts)
    {
        _byKind = stores.ToDictionary(s => s.Kind, StringComparer.OrdinalIgnoreCase);
        _opts = opts.Value;
    }

    /// <inheritdoc />
    public IFileStore SelectForSize(long sizeBytes)
    {
        // File nhỏ luôn vào DB (logo/thumbnail/icon) — bất kể provider cấu hình cho file lớn.
        if (sizeBytes <= _opts.DbThresholdBytes)
            return _byKind[FileStorageProviders.Db];

        return SelectForKind(_opts.Provider);
    }

    /// <inheritdoc />
    public IFileStore SelectForKind(string storageKind)
    {
        if (_byKind.TryGetValue(storageKind, out var store))
            return store;
        throw new NotSupportedException($"Storage_Kind không nhận diện: '{storageKind}'.");
    }
}
