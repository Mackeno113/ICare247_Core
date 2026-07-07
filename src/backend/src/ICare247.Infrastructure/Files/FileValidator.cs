// File    : FileValidator.cs
// Module  : Files
// Layer   : Infrastructure
// Purpose : Impl IFileValidator — 4 lớp bảo vệ: (1) allowlist đuôi, (2) chặn đuôi nguy hiểm + double-extension,
//           (3) magic-byte khớp đuôi (chống đổi đuôi), (4) sniff nội dung mã thực thi/script/HTML-SVG.

using System.Text;
using ICare247.Application.Files;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace ICare247.Infrastructure.Files;

/// <summary>
/// Kiểm tra tệp upload theo allowlist + magic-byte + phát hiện mã thực thi. Cấu hình từ
/// <see cref="FileValidationOptions"/>; giới hạn kích thước từ <see cref="FileStorageOptions.MaxBytes"/>.
/// </summary>
public sealed class FileValidator : IFileValidator
{
    private readonly FileStorageOptions _opts;
    private readonly HashSet<string> _allowed;

    // ── Đuôi NGUY HIỂM — chặn tuyệt đối kể cả khi lọt allowlist (phòng thủ chiều sâu + double-extension) ──
    private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "exe", "dll", "com", "bat", "cmd", "msi", "scr", "cpl", "jar", "app", "deb", "rpm",
        "sh", "bash", "ps1", "psm1", "vbs", "vbe", "js", "jse", "wsf", "wsh", "hta", "reg",
        "php", "phtml", "php3", "php4", "php5", "asp", "aspx", "jsp", "cgi", "pl", "py", "rb",
        "html", "htm", "xhtml", "shtml", "svg", "svgz", "xml", "xhtm", "mht", "mhtml", "lnk", "iso",
    };

    /// <summary>Khởi tạo: nạp allowlist từ config (rỗng → default), chuẩn hóa chữ thường.</summary>
    /// <param name="opts">Tùy chọn lưu trữ + validation đã bind.</param>
    public FileValidator(IOptions<FileStorageOptions> opts)
    {
        _opts = opts.Value;
        var list = _opts.Validation.AllowedExtensions is { Length: > 0 } cfg
            ? cfg
            : FileValidationOptions.DefaultAllowedExtensions;
        _allowed = new HashSet<string>(
            list.Select(e => e.Trim().TrimStart('.').ToLowerInvariant()), StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public FileValidationResult Validate(
        string fileName, string? declaredContentType, ReadOnlySpan<byte> header, long sizeBytes)
    {
        // ── 1. Kích thước ────────────────────────────────────────────────────────
        if (sizeBytes <= 0)
            return FileValidationResult.Fail("EMPTY", "Tệp rỗng.");
        if (sizeBytes > _opts.MaxBytes)
            return FileValidationResult.Fail("TOO_LARGE",
                $"Tệp quá lớn (tối đa {_opts.MaxBytes / (1024 * 1024)}MB).");

        // ── 2. Đuôi + double-extension ───────────────────────────────────────────
        var segments = SplitExtensions(fileName);
        // Bất kỳ đoạn đuôi nào (kể cả đuôi giữa như .php.pdf) nằm trong danh sách nguy hiểm → chặn.
        foreach (var seg in segments)
            if (DangerousExtensions.Contains(seg))
                return FileValidationResult.Fail("DANGEROUS_EXT",
                    $"Định dạng tệp không được phép (chứa '.{seg}').");

        var ext = segments.Count > 0 ? segments[^1] : "";
        if (ext.Length == 0)
            return FileValidationResult.Fail("NO_EXT", "Tệp không có phần mở rộng hợp lệ.");
        if (!_allowed.Contains(ext))
            return FileValidationResult.Fail("EXT_NOT_ALLOWED",
                $"Định dạng '.{ext}' không nằm trong danh sách cho phép.");

        // ── 3. Sniff nội dung mã thực thi/script (chống polyglot & đổi đuôi ngược) ─
        if (LooksExecutableOrScript(header))
            return FileValidationResult.Fail("EXECUTABLE_CONTENT",
                "Nội dung tệp chứa dấu hiệu mã thực thi/script — bị từ chối.");

        // ── 4. Magic-byte khớp họ đuôi (nếu là loại nhị phân có chữ ký) ───────────
        var magic = MatchMagic(ext, header);
        if (magic.Required && !magic.Matched)
            return FileValidationResult.Fail("MAGIC_MISMATCH",
                "Nội dung tệp không khớp định dạng khai báo (nghi đổi đuôi).");

        return FileValidationResult.Ok(magic.ResolvedContentType);
    }

    /// <summary>Tách toàn bộ đoạn đuôi của tên tệp (vd "a.php.pdf" → ["php","pdf"]). Bỏ tên rỗng/khoảng trắng.</summary>
    private static List<string> SplitExtensions(string fileName)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(fileName)) return result;
        var name = fileName.Trim().Replace('\\', '/');
        name = name[(name.LastIndexOf('/') + 1)..]; // bỏ path nếu có
        var parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
        // parts[0] là tên gốc, còn lại là các đuôi
        for (int i = 1; i < parts.Length; i++)
            result.Add(parts[i].Trim().ToLowerInvariant());
        return result;
    }

    /// <summary>Phát hiện dấu hiệu file thực thi / script / HTML-SVG trong phần đầu nội dung.</summary>
    private static bool LooksExecutableOrScript(ReadOnlySpan<byte> h)
    {
        if (h.Length >= 2 && h[0] == 0x4D && h[1] == 0x5A) return true;                    // 'MZ' — PE/EXE/DLL
        if (h.Length >= 4 && h[0] == 0x7F && h[1] == 0x45 && h[2] == 0x4C && h[3] == 0x46)  // 0x7F 'ELF'
            return true;
        if (h.Length >= 2 && h[0] == 0x23 && h[1] == 0x21) return true;                    // '#!' shebang
        if (h.Length >= 4 && h[0] == 0xCA && h[1] == 0xFE && h[2] == 0xBA && h[3] == 0xBE)  // Mach-O / Java class
            return true;

        // Soi text: script/HTML nhúng (SVG-XSS, HTML sniffing). Chỉ quét phần đầu, không phân biệt hoa thường.
        var take = Math.Min(h.Length, 1024);
        var text = Encoding.ASCII.GetString(h[..take]).ToLowerInvariant();
        string[] markers =
        [
            "<script", "<?php", "<%", "<!doctype html", "<html", "<svg", "javascript:", "onerror=", "onload=",
        ];
        foreach (var m in markers)
            if (text.Contains(m, StringComparison.Ordinal)) return true;

        return false;
    }

    /// <summary>Kiểm magic-byte theo họ đuôi. Text (txt/csv) không có magic → không bắt buộc.</summary>
    private static (bool Required, bool Matched, string? ResolvedContentType) MatchMagic(
        string ext, ReadOnlySpan<byte> h)
    {
        switch (ext)
        {
            case "png":
                return (true, h.Length >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47,
                        "image/png");
            case "jpg":
            case "jpeg":
                return (true, h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF, "image/jpeg");
            case "gif":
                return (true, h.Length >= 6 && h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x38,
                        "image/gif");
            case "webp":
                return (true,
                        h.Length >= 12 && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
                        && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50,
                        "image/webp");
            case "pdf":
                return (true, h.Length >= 5 && h[0] == 0x25 && h[1] == 0x50 && h[2] == 0x44 && h[3] == 0x46,
                        "application/pdf"); // '%PDF'
            case "docx":
            case "xlsx":
            case "pptx":
            case "zip":
                // ZIP container: 'PK\x03\x04' (hoặc rỗng 'PK\x05\x06' / spanned 'PK\x07\x08').
                return (true,
                        h.Length >= 2 && h[0] == 0x50 && h[1] == 0x4B, ZipMime(ext));
            case "doc":
            case "xls":
            case "ppt":
                // OLE Compound File: D0 CF 11 E0 A1 B1 1A E1
                return (true,
                        h.Length >= 8 && h[0] == 0xD0 && h[1] == 0xCF && h[2] == 0x11 && h[3] == 0xE0,
                        "application/x-ole-storage");
            case "txt":
            case "csv":
                return (false, true, "text/plain"); // không có magic — hợp lệ nếu đã qua sniff mã thực thi
            default:
                return (false, true, null);
        }
    }

    /// <summary>MIME OOXML theo đuôi (docx/xlsx/pptx) — cùng vỏ ZIP nhưng khai MIME đúng.</summary>
    private static string ZipMime(string ext) => ext switch
    {
        "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        _ => "application/zip",
    };
}
