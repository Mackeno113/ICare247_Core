// File    : FileSystemFileStore.cs
// Module  : Files
// Layer   : Infrastructure
// Purpose : IFileStore backend "FileSystem" — file trên đĩa/shared mount, Storage_Key = path TƯƠNG ĐỐI.
//           Gốc chứa = FileStorageOptions.BaseRoot (dùng chung mọi node sau LB). Di dời = đổi BaseRoot.
//           An toàn: guard path-traversal (resolved phải nằm trong BaseRoot) + ghi tạm→rename (atomic).

using ICare247.Application.Files;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace ICare247.Infrastructure.Files;

/// <summary>
/// Lưu nội dung ra hệ thống tệp theo key tương đối, resolve dưới <c>BaseRoot</c> (shared mount / ổ đĩa).
/// Mọi đường dẫn tuyệt đối được tính runtime từ BaseRoot + key → DB không giữ path tuyệt đối.
/// </summary>
public sealed class FileSystemFileStore : IFileStore
{
    private readonly FileStorageOptions _opts;

    /// <summary>Khởi tạo với tùy chọn lưu trữ (đọc BaseRoot).</summary>
    /// <param name="opts">Tùy chọn <see cref="FileStorageOptions"/> đã bind.</param>
    public FileSystemFileStore(IOptions<FileStorageOptions> opts) => _opts = opts.Value;

    /// <inheritdoc />
    public string Kind => FileStorageProviders.FileSystem;

    /// <inheritdoc />
    public async Task<StoredContent> SaveAsync(string relativeKey, Stream content, CancellationToken ct = default)
    {
        var fullPath = Resolve(relativeKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // Dedup: nội dung cùng checksum → key trùng → file đã tồn tại, không cần ghi lại. Vẫn tính size.
        if (File.Exists(fullPath))
        {
            var existing = new FileInfo(fullPath).Length;
            return new StoredContent(Kind, relativeKey, Content: null, SizeBytes: existing);
        }

        // Ghi ra file tạm cùng thư mục rồi rename → tránh file dở khi ghi lỗi (atomic trên cùng volume/mount).
        var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        long size;
        try
        {
            await using (var fs = new FileStream(
                tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
            {
                await content.CopyToAsync(fs, ct);
                size = fs.Length;
            }
            File.Move(tempPath, fullPath, overwrite: true);
        }
        catch
        {
            // Dọn file tạm mồ côi khi lỗi/hủy — không để rác trên mount.
            TryDelete(tempPath);
            throw;
        }

        return new StoredContent(Kind, relativeKey, Content: null, SizeBytes: size);
    }

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(StoredContent stored, CancellationToken ct = default)
    {
        var fullPath = Resolve(stored.StorageKey ?? throw new InvalidOperationException("Storage_Key rỗng với FileSystem."));
        Stream fs = new FileStream(
            fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
        return Task.FromResult(fs);
    }

    /// <inheritdoc />
    public Task DeleteAsync(StoredContent stored, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(stored.StorageKey))
            TryDelete(Resolve(stored.StorageKey));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<FileStoreHealth> CheckHealthAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opts.BaseRoot))
            return new FileStoreHealth(false, "FileStorage:BaseRoot chưa cấu hình (Provider=FileSystem cần gốc chứa).");

        try
        {
            Directory.CreateDirectory(_opts.BaseRoot);
            // Probe đọc/ghi: node nào không ghi được BaseRoot → fail-fast (tránh âm thầm ghi local sau LB).
            var probe = Path.Combine(_opts.BaseRoot, ".icare_probe_" + Guid.NewGuid().ToString("N"));
            await File.WriteAllTextAsync(probe, "ok", ct);
            TryDelete(probe);
            return new FileStoreHealth(true, $"FileSystem store sẵn sàng tại '{_opts.BaseRoot}'.");
        }
        catch (Exception ex)
        {
            return new FileStoreHealth(false, $"Không đọc/ghi được BaseRoot '{_opts.BaseRoot}': {ex.Message}");
        }
    }

    /// <summary>
    /// Resolve key tương đối → path tuyệt đối dưới BaseRoot, CHẶN path traversal (../, ổ khác, UNC lạ).
    /// </summary>
    /// <param name="relativeKey">Key tương đối (dùng '/').</param>
    /// <returns>Đường dẫn tuyệt đối đã chuẩn hóa, đảm bảo nằm trong BaseRoot.</returns>
    /// <exception cref="InvalidOperationException">BaseRoot chưa cấu hình.</exception>
    /// <exception cref="UnauthorizedAccessException">Key thoát ra ngoài BaseRoot (path traversal).</exception>
    private string Resolve(string relativeKey)
    {
        if (string.IsNullOrWhiteSpace(_opts.BaseRoot))
            throw new InvalidOperationException("FileStorage:BaseRoot chưa cấu hình.");

        var rootFull = Path.GetFullPath(_opts.BaseRoot);
        var rel = relativeKey.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var combined = Path.GetFullPath(Path.Combine(rootFull, rel));

        // GUARD: sau khi chuẩn hóa, path phải còn nằm trong BaseRoot (chống '..\..\' thoát ra ngoài).
        var rootWithSep = rootFull.EndsWith(Path.DirectorySeparatorChar)
            ? rootFull
            : rootFull + Path.DirectorySeparatorChar;
        if (!combined.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Storage_Key thoát ra ngoài BaseRoot: '{relativeKey}'.");

        return combined;
    }

    /// <summary>Xóa file nếu tồn tại, nuốt lỗi (dùng cho dọn rác / rollback — không chặn luồng chính).</summary>
    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best-effort cleanup */ }
    }
}
