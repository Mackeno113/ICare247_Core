// File    : FileStorageOptions.cs
// Module  : Files
// Layer   : Application
// Purpose : Cấu hình lưu trữ tệp (bind từ section "FileStorage"). Chọn backend + ngưỡng + gốc chứa.
//           Gốc (BaseRoot) đặt theo DEPLOYMENT, dùng chung mọi node sau load-balancer; di dời nơi
//           chứa (D: → \\nas → object) = đổi giá trị ở đây, KHÔNG đụng đường dẫn tương đối trong DB.

namespace ICare247.Application.Files;

/// <summary>
/// Tùy chọn lưu trữ tệp cho hệ đính kèm. Bind từ appsettings section <c>FileStorage</c>.
/// </summary>
public sealed class FileStorageOptions
{
    /// <summary>Tên section trong appsettings.</summary>
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Backend cho file lớn: <c>Db</c> | <c>FileSystem</c> | <c>Object</c>. Mặc định <c>Db</c>
    /// (an toàn nhất — luôn node-safe vì mọi node chung 1 Data DB). File nhỏ (≤ <see cref="DbThresholdBytes"/>)
    /// LUÔN vào DB bất kể giá trị này.
    /// </summary>
    public string Provider { get; set; } = FileStorageProviders.Db;

    /// <summary>
    /// Gốc chứa tuyệt đối khi <see cref="Provider"/> = FileSystem (vd <c>\\nas01\icare247</c> hoặc
    /// <c>D:\ICare247Files</c>). Mọi node phải trỏ CÙNG một giá trị dùng chung. Rỗng khi Provider = Db/Object.
    /// </summary>
    public string BaseRoot { get; set; } = "";

    /// <summary>
    /// Đoạn khóa cô lập vật lý tùy chọn (vd mã site) — chèn đầu Storage_Key khi nhiều site khác nhau
    /// cùng trỏ về một gốc vật lý. Rỗng = không chèn. Không ảnh hưởng khi mỗi site có DB/gốc riêng.
    /// </summary>
    public string? SiteKey { get; set; }

    /// <summary>Ngưỡng byte: tệp ≤ giá trị này lưu thẳng DB (logo/thumbnail/icon). Mặc định 256KB.</summary>
    public long DbThresholdBytes { get; set; } = 262_144;

    /// <summary>Giới hạn kích thước 1 tệp upload (bytes). Mặc định 50MB.</summary>
    public long MaxBytes { get; set; } = 52_428_800;

    /// <summary>Cấu hình object storage (chỉ dùng khi <see cref="Provider"/> = Object).</summary>
    public ObjectStorageOptions Object { get; set; } = new();

    /// <summary>Cấu hình kiểm tra tệp hợp lệ (allowlist đuôi + kiểm mã thực thi).</summary>
    public FileValidationOptions Validation { get; set; } = new();

    /// <summary>Cấu hình tối ưu ảnh server-side (resize/nén + thumbnail).</summary>
    public ImageOptimizationOptions Image { get; set; } = new();
}

/// <summary>Tùy chọn tối ưu ảnh server-side (SkiaSharp). Áp cho ảnh raster (png/jpeg/webp).</summary>
public sealed class ImageOptimizationOptions
{
    /// <summary>Bật/tắt tối ưu ảnh phía server. Mặc định bật.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Cạnh dài tối đa (px) của ảnh chính — vượt sẽ resize giữ tỷ lệ. Mặc định 2000.</summary>
    public int MaxDimension { get; set; } = 2000;

    /// <summary>Chất lượng nén ảnh chính (JPEG/WebP, 1–100). Mặc định 82.</summary>
    public int Quality { get; set; } = 82;

    /// <summary>Cạnh dài tối đa (px) của thumbnail. Mặc định 256; ≤ 0 = không sinh thumbnail.</summary>
    public int ThumbnailDimension { get; set; } = 256;

    /// <summary>Chất lượng nén thumbnail (1–100). Mặc định 72.</summary>
    public int ThumbnailQuality { get; set; } = 72;
}

/// <summary>Tùy chọn kiểm tra tệp hợp lệ. Allowlist là hàng rào chính; blocklist là phòng thủ chiều sâu.</summary>
public sealed class FileValidationOptions
{
    /// <summary>
    /// Đuôi tệp CHO PHÉP (không dấu chấm, chữ thường). Rỗng → dùng <see cref="DefaultAllowedExtensions"/>.
    /// Tệp có đuôi ngoài danh sách bị từ chối (allowlist).
    /// </summary>
    public string[] AllowedExtensions { get; set; } = DefaultAllowedExtensions;

    /// <summary>Đuôi mặc định: ảnh + tài liệu văn phòng + text + zip. KHÔNG gồm loại thực thi/script.</summary>
    public static readonly string[] DefaultAllowedExtensions =
    [
        "png", "jpg", "jpeg", "webp", "gif",                 // ảnh
        "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx",  // tài liệu văn phòng
        "csv", "txt",                                         // text
        "zip"                                                 // nén (lưu ý: có thể chứa file khác — cân nhắc quét sâu ở phase sau)
    ];
}

/// <summary>Cấu hình kết nối object storage (MinIO / S3 / Azure Blob). Đặt qua appsettings.local.json.</summary>
public sealed class ObjectStorageOptions
{
    /// <summary>Endpoint dịch vụ (vd <c>https://minio.local:9000</c>). Rỗng = chưa cấu hình.</summary>
    public string Endpoint { get; set; } = "";

    /// <summary>Tên bucket chứa tệp.</summary>
    public string Bucket { get; set; } = "";

    /// <summary>Access key.</summary>
    public string AccessKey { get; set; } = "";

    /// <summary>Secret key.</summary>
    public string SecretKey { get; set; } = "";
}

/// <summary>Hằng tên backend lưu trữ — dùng chung DB (Storage_Kind), config và IFileStore.Kind.</summary>
public static class FileStorageProviders
{
    /// <summary>Bytes trong DB (cột VARBINARY).</summary>
    public const string Db = "Db";

    /// <summary>File trên hệ thống tệp (shared mount / ổ đĩa), Storage_Key = path tương đối.</summary>
    public const string FileSystem = "FileSystem";

    /// <summary>Object storage (MinIO/S3/Azure Blob), Storage_Key = object key.</summary>
    public const string Object = "Object";
}
