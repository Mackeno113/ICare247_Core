// File    : SkiaImageOptimizer.cs
// Module  : Files
// Layer   : Infrastructure
// Purpose : Impl IImageOptimizer bằng SkiaSharp (MIT) — resize ảnh theo cạnh dài tối đa + nén + sinh
//           thumbnail. Ảnh có alpha → giữ PNG (không mất trong suốt); còn lại → JPEG (nhẹ hơn). Lỗi
//           giải mã → trả null để pipeline dùng nội dung gốc (không chặn upload).

using ICare247.Application.Files;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace ICare247.Infrastructure.Files;

/// <summary>Tối ưu ảnh raster (png/jpeg/webp) server-side. GIF (động) và SVG không xử lý ở đây.</summary>
public sealed class SkiaImageOptimizer : IImageOptimizer
{
    private readonly ImageOptimizationOptions _opts;
    private readonly ILogger<SkiaImageOptimizer> _logger;

    public SkiaImageOptimizer(IOptions<FileStorageOptions> opts, ILogger<SkiaImageOptimizer> logger)
    {
        _opts = opts.Value.Image;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool CanOptimize(string? contentType)
    {
        if (!_opts.Enabled || string.IsNullOrEmpty(contentType)) return false;
        return contentType is "image/png" or "image/jpeg" or "image/webp";
    }

    /// <inheritdoc />
    public async Task<OptimizedImage?> OptimizeAsync(Stream image, CancellationToken ct = default)
    {
        // Nạp toàn bộ vào RAM: chỉ chạy cho ẢNH (đã chặn bởi MaxBytes) — SkiaSharp cần dữ liệu đầy đủ để decode.
        byte[] src;
        await using (var ms = new MemoryStream())
        {
            await image.CopyToAsync(ms, ct);
            src = ms.ToArray();
        }

        try
        {
            return Optimize(src);
        }
        catch (Exception ex)
        {
            // Không chặn upload nếu tối ưu lỗi — pipeline dùng nội dung gốc.
            _logger.LogWarning(ex, "Tối ưu ảnh thất bại — dùng ảnh gốc.");
            return null;
        }
    }

    /// <summary>Giải mã → resize/nén ảnh chính + thumbnail. Null nếu không decode được.</summary>
    private OptimizedImage? Optimize(byte[] src)
    {
        using var data = SKData.CreateCopy(src);
        using var codec = SKCodec.Create(data);
        if (codec is null) return null;
        using var bitmap = SKBitmap.Decode(codec);
        if (bitmap is null) return null;

        var hasAlpha = bitmap.AlphaType != SKAlphaType.Opaque;

        var main = ResizeEncode(bitmap, _opts.MaxDimension, _opts.Quality, hasAlpha);
        if (main is null) return null;

        byte[]? thumb = null;
        string? thumbCt = null;
        if (_opts.ThumbnailDimension > 0)
        {
            var t = ResizeEncode(bitmap, _opts.ThumbnailDimension, _opts.ThumbnailQuality, hasAlpha);
            if (t is not null)
            {
                thumb = t.Value.Bytes;
                thumbCt = t.Value.ContentType;
            }
        }

        return new OptimizedImage(main.Value.Bytes, main.Value.ContentType, thumb, thumbCt);
    }

    /// <summary>Resize (nếu vượt maxDim, giữ tỷ lệ) rồi encode. Null nếu encode thất bại.</summary>
    private static (byte[] Bytes, string ContentType)? ResizeEncode(
        SKBitmap src, int maxDim, int quality, bool hasAlpha)
    {
        var (w, h) = Scaled(src.Width, src.Height, maxDim);

        SKBitmap? resized = null;
        try
        {
            var toEncode = src;
            if (w != src.Width || h != src.Height)
            {
                resized = src.Resize(new SKImageInfo(w, h), SKFilterQuality.High);
                if (resized is not null) toEncode = resized;
            }

            using var img = SKImage.FromBitmap(toEncode);
            var format = hasAlpha ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
            using var encoded = img.Encode(format, quality);
            if (encoded is null) return null;

            return (encoded.ToArray(), hasAlpha ? "image/png" : "image/jpeg");
        }
        finally
        {
            resized?.Dispose();
        }
    }

    /// <summary>Tính kích thước mới giữ tỷ lệ sao cho cạnh dài ≤ maxDim (không phóng to ảnh nhỏ).</summary>
    private static (int W, int H) Scaled(int w, int h, int maxDim)
    {
        if (maxDim <= 0 || (w <= maxDim && h <= maxDim)) return (w, h);
        var scale = (double)maxDim / Math.Max(w, h);
        return (Math.Max(1, (int)Math.Round(w * scale)), Math.Max(1, (int)Math.Round(h * scale)));
    }
}
