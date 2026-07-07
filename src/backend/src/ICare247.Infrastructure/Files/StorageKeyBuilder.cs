// File    : StorageKeyBuilder.cs
// Module  : Files
// Layer   : Infrastructure
// Purpose : Dựng Storage_Key tương đối, ổn định, an toàn cho nội dung tệp (impl IStorageKeyBuilder).
//           Toàn bộ thành phần đều được làm sạch ở server → không thể chèn path traversal từ client.

using System.Text;
using ICare247.Application.Files;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace ICare247.Infrastructure.Files;

/// <summary>
/// Dựng key tương đối theo cấu trúc bất biến: <c>[siteKey/]{tenantId}/{yyyy}/{MM}/{loai}/{shard}/{sha}.{ext}</c>.
/// Sharding 2 ký tự đầu của checksum để tránh 1 thư mục chứa quá nhiều file + giữ path ngắn.
/// </summary>
public sealed class StorageKeyBuilder : IStorageKeyBuilder
{
    private readonly FileStorageOptions _opts;

    /// <summary>Khởi tạo với tùy chọn lưu trữ (đọc SiteKey để chèn tiền tố cô lập vật lý).</summary>
    /// <param name="opts">Tùy chọn <see cref="FileStorageOptions"/> đã bind.</param>
    public StorageKeyBuilder(IOptions<FileStorageOptions> opts) => _opts = opts.Value;

    /// <inheritdoc />
    public string Build(int tenantId, string? loai, string checksum, string fileName)
    {
        // ── 1. Làm sạch từng thành phần (server-side) — không tin dữ liệu client ──
        var sha = SanitizeHex(checksum);
        var shard = sha.Length >= 2 ? sha[..2] : "00";
        var loaiSafe = SanitizeSegment(loai, fallback: "chung", maxLen: 30);
        var ext = SanitizeExtension(fileName);
        var now = DateTime.UtcNow;

        // ── 2. Ghép key tương đối (luôn dùng '/') ────────────────────────────────
        var sb = new StringBuilder(96);
        var siteKey = SanitizeSegment(_opts.SiteKey, fallback: "", maxLen: 40);
        if (siteKey.Length > 0) sb.Append(siteKey).Append('/');

        sb.Append(tenantId).Append('/')
          .Append(now.Year).Append('/')
          .Append(now.Month.ToString("D2")).Append('/')
          .Append(loaiSafe).Append('/')
          .Append(shard).Append('/')
          .Append(sha);
        if (ext.Length > 0) sb.Append('.').Append(ext);

        return sb.ToString();
    }

    /// <summary>Giữ lại ký tự hex (a-f, 0-9), hạ chữ thường; rỗng → "00". Chống ký tự path lạ.</summary>
    private static string SanitizeHex(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "00";
        var sb = new StringBuilder(s.Length);
        foreach (var c in s.ToLowerInvariant())
            if (c is (>= '0' and <= '9') or (>= 'a' and <= 'f')) sb.Append(c);
        return sb.Length > 0 ? sb.ToString() : "00";
    }

    /// <summary>Giữ [A-Za-z0-9_-], cắt độ dài; rỗng → fallback. Loại '/', '\\', '.', ký tự điều khiển.</summary>
    private static string SanitizeSegment(string? s, string fallback, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(s)) return fallback;
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
            if (c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '-')
                sb.Append(c);
        var r = sb.ToString();
        if (r.Length == 0) return fallback;
        return r.Length > maxLen ? r[..maxLen] : r;
    }

    /// <summary>Suy phần mở rộng an toàn từ tên gốc: [a-z0-9], tối đa 10 ký tự; không có → rỗng.</summary>
    private static string SanitizeExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return "";
        var dot = fileName.LastIndexOf('.');
        if (dot < 0 || dot == fileName.Length - 1) return "";
        var sb = new StringBuilder(10);
        foreach (var c in fileName[(dot + 1)..].ToLowerInvariant())
        {
            if (c is (>= 'a' and <= 'z') or (>= '0' and <= '9')) sb.Append(c);
            if (sb.Length >= 10) break;
        }
        return sb.ToString();
    }
}
