// File    : IImageOptimizer.cs
// Module  : Files
// Layer   : Application
// Purpose : Tối ưu ảnh server-side — resize theo cạnh dài tối đa + nén + sinh thumbnail. Là nguồn chuẩn
//           (không tin nén phía client). Không phải ảnh → trả null (pipeline giữ nguyên nội dung gốc).

namespace ICare247.Application.Interfaces;

/// <summary>Tối ưu ảnh raster (png/jpeg/webp). Impl dùng SkiaSharp (MIT).</summary>
public interface IImageOptimizer
{
    /// <summary>Có tối ưu được MIME này không (bật cấu hình + là ảnh raster hỗ trợ).</summary>
    /// <param name="contentType">MIME xác định (từ magic-byte).</param>
    /// <returns>true nếu nên chạy tối ưu.</returns>
    bool CanOptimize(string? contentType);

    /// <summary>
    /// Đọc ảnh từ stream, resize/nén ảnh chính + sinh thumbnail. Trả null nếu không giải mã được
    /// (để pipeline dùng nội dung gốc, không chặn upload).
    /// </summary>
    /// <param name="image">Stream ảnh gốc (seekable).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="OptimizedImage"/> (bytes ảnh chính + thumbnail) hoặc null.</returns>
    /// <remarks>Không side-effect ngoài đọc stream; caller sở hữu/định đoạt stream gốc.</remarks>
    Task<OptimizedImage?> OptimizeAsync(Stream image, CancellationToken ct = default);
}

/// <summary>Kết quả tối ưu ảnh.</summary>
/// <param name="Main">Bytes ảnh chính đã tối ưu.</param>
/// <param name="ContentType">MIME ảnh chính sau tối ưu (vd image/jpeg | image/png).</param>
/// <param name="Thumbnail">Bytes thumbnail (JPEG); null nếu không sinh.</param>
/// <param name="ThumbnailContentType">MIME thumbnail; null nếu không có.</param>
public sealed record OptimizedImage(
    byte[] Main, string ContentType, byte[]? Thumbnail, string? ThumbnailContentType);
