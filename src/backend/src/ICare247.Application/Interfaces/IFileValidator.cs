// File    : IFileValidator.cs
// Module  : Files
// Layer   : Application
// Purpose : Kiểm tra một tệp có hợp lệ + an toàn để nhận không: allowlist đuôi, magic-byte khớp đuôi
//           (chống đổi đuôi), và phát hiện nội dung mã thực thi/script (chống upload mã độc).

namespace ICare247.Application.Interfaces;

/// <summary>
/// Bộ kiểm tra tệp upload. Trả kết quả hợp lệ/không kèm mã lỗi để controller dịch ra thông báo.
/// Không truy cập DB/IO — chỉ soi tên + phần đầu nội dung + kích thước.
/// </summary>
public interface IFileValidator
{
    /// <summary>
    /// Kiểm tra tệp: đuôi trong allowlist, không phải đuôi nguy hiểm, magic-byte khớp đuôi, không chứa
    /// dấu hiệu mã thực thi/script/HTML-SVG.
    /// </summary>
    /// <param name="fileName">Tên gốc (suy đuôi; kiểm double-extension).</param>
    /// <param name="declaredContentType">MIME client khai (chỉ tham khảo — KHÔNG tin tuyệt đối).</param>
    /// <param name="header">Vài KB đầu của nội dung để soi magic-byte + sniff mã thực thi.</param>
    /// <param name="sizeBytes">Tổng kích thước tệp (byte) để chặn vượt <c>MaxBytes</c>.</param>
    /// <returns><see cref="FileValidationResult"/>: hợp lệ, hoặc mã lỗi + thông báo tiếng Việt.</returns>
    /// <remarks>Không side-effect. Gọi được trong lúc streaming (header lấy từ chunk đầu).</remarks>
    FileValidationResult Validate(
        string fileName, string? declaredContentType, ReadOnlySpan<byte> header, long sizeBytes);
}

/// <summary>Kết quả kiểm tra tệp.</summary>
/// <param name="IsValid">Hợp lệ hay không.</param>
/// <param name="ErrorCode">Mã lỗi (khi không hợp lệ) — dùng phân loại/i18n. Null khi hợp lệ.</param>
/// <param name="Message">Thông báo tiếng Việt cho người dùng. Null khi hợp lệ.</param>
/// <param name="ResolvedContentType">MIME xác định từ magic-byte (đáng tin hơn client khai). Null nếu không suy được.</param>
public sealed record FileValidationResult(
    bool IsValid, string? ErrorCode, string? Message, string? ResolvedContentType = null)
{
    /// <summary>Tạo kết quả hợp lệ (kèm MIME đã xác định nếu có).</summary>
    public static FileValidationResult Ok(string? resolvedContentType = null)
        => new(true, null, null, resolvedContentType);

    /// <summary>Tạo kết quả từ chối với mã lỗi + thông báo.</summary>
    public static FileValidationResult Fail(string code, string message)
        => new(false, code, message);
}
