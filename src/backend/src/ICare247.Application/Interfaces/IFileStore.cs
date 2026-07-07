// File    : IFileStore.cs
// Module  : Files
// Layer   : Application
// Purpose : Trừu tượng backend lưu nội dung tệp — Db | FileSystem | Object đứng sau một interface.
//           Store CHỈ lo I/O nội dung vật lý theo KEY TƯƠNG ĐỐI; repository lo dòng metadata TT_TepBlob.
//           Tách vậy để di dời gốc chứa (đổi BaseRoot/endpoint) không đụng DB, không đụng handler.

using ICare247.Application.Files;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Backend lưu trữ nội dung tệp. Mỗi implementation ứng với một <c>Storage_Kind</c>
/// (Db/FileSystem/Object). Nhận KEY TƯƠNG ĐỐI (đã dựng + guard bởi <see cref="IStorageKeyBuilder"/>),
/// KHÔNG bao giờ nhận đường dẫn tuyệt đối từ ngoài.
/// </summary>
public interface IFileStore
{
    /// <summary>Backend này đại diện: Db | FileSystem | Object (khớp <c>FileStorageProviders</c>).</summary>
    string Kind { get; }

    /// <summary>
    /// Lưu nội dung dưới <paramref name="relativeKey"/>. Trả mô tả nơi lưu để repository ghi TT_TepBlob.
    /// </summary>
    /// <param name="relativeKey">Key tương đối đã dựng (vd <c>42/2026/07/Logo/ab/abcd...png</c>).</param>
    /// <param name="content">Stream nội dung (đã validate); đọc hết trong hàm.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="StoredContent"/>: Db → bytes; FileSystem/Object → Storage_Key.</returns>
    /// <remarks>
    /// Sự kiện theo sau: FileSystem/Object ghi file/object ra backend (ghi tạm → rename để atomic);
    /// Db chỉ buffer bytes (repository mới thật sự ghi DB). Lỗi giữa chừng: hàm tự dọn file mồ côi.
    /// </remarks>
    Task<StoredContent> SaveAsync(string relativeKey, Stream content, CancellationToken ct = default);

    /// <summary>
    /// Mở stream đọc nội dung đã lưu. Db đọc từ <paramref name="stored"/>.Content; còn lại đọc từ Storage_Key.
    /// </summary>
    /// <param name="stored">Mô tả nơi lưu (từ TT_TepBlob).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Stream đọc-được; caller chịu trách nhiệm dispose.</returns>
    Task<Stream> OpenReadAsync(StoredContent stored, CancellationToken ct = default);

    /// <summary>
    /// Xóa nội dung vật lý (gọi khi RefCount về 0). Db là no-op (xóa dòng DB là đủ).
    /// </summary>
    /// <param name="stored">Mô tả nơi lưu cần xóa.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>Sự kiện theo sau: FileSystem xóa file trên đĩa/mount; Object xóa object khỏi bucket.</remarks>
    Task DeleteAsync(StoredContent stored, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra backend đọc/ghi được (dùng fail-fast lúc khởi động — tránh âm thầm ghi local sau LB).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="FileStoreHealth"/> — khỏe/không + chi tiết.</returns>
    Task<FileStoreHealth> CheckHealthAsync(CancellationToken ct = default);
}
