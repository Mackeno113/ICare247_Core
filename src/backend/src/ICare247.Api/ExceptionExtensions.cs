// File    : ExceptionExtensions.cs
// Module  : Api
// Layer   : Api
// Purpose : Extension methods trích xuất thông tin quan trọng từ Exception —
//           thay vì hiển thị cả stack trace dài, chỉ lấy phần cần debug.

namespace ICare247.Api;

/// <summary>
/// Extension methods cho <see cref="Exception"/> — tóm tắt lỗi ngắn gọn.
/// </summary>
public static class ExceptionExtensions
{
    // ── ToReadable ────────────────────────────────────────────────────────────

    /// <summary>
    /// Tóm tắt exception thành 1-3 dòng có thể đọc được.
    /// Bỏ stack trace, chỉ giữ: loại lỗi + message + inner message.
    /// </summary>
    /// <example>
    /// ex.ToReadable()
    /// → "SqlException: A network-related error... | Win32Exception: The system cannot find the file"
    /// </example>
    public static string ToReadable(this Exception ex)
    {
        var parts = new List<string>();
        var current = ex;

        while (current is not null)
        {
            // Lấy tên class ngắn (không namespace): "SqlException", "Win32Exception",...
            var typeName = current.GetType().Name;
            var message  = current.Message
                // Bỏ phần "(provider: ..., error: XX - ...)" trong SqlException cho gọn
                .Split('\n')[0]   // chỉ lấy dòng đầu
                .Trim();

            parts.Add($"{typeName}: {message}");

            // Tránh lặp vô tận nếu InnerException trỏ về chính nó
            current = current.InnerException == current ? null : current.InnerException;
        }

        return string.Join(" | ", parts);
    }

    // ── ToShort ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Chỉ lấy message của exception gốc (outermost) — 1 dòng duy nhất.
    /// Dùng khi chỉ cần hiển thị lỗi cho user.
    /// </summary>
    public static string ToShort(this Exception ex)
        => ex.Message.Split('\n')[0].Trim();

    // ── ToDetail ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Tóm tắt đầy đủ: loại lỗi + message + inner chain + dòng code xảy ra lỗi.
    /// Dùng khi cần debug — ghi vào log file.
    /// </summary>
    public static string ToDetail(this Exception ex)
    {
        var sb = new System.Text.StringBuilder();

        // Chain exception (không có stack trace)
        sb.AppendLine(ex.ToReadable());

        // Dòng code xảy ra lỗi (lấy từ stack trace, chỉ lấy frame đầu tiên có số dòng)
        var sourceLine = ExtractSourceLine(ex);
        if (sourceLine is not null)
            sb.AppendLine($"  at {sourceLine}");

        return sb.ToString().TrimEnd();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Tìm dòng code đầu tiên trong stack trace có số dòng (line XX).
    /// Bỏ qua các frame thuộc framework/Dapper/Microsoft.
    /// </summary>
    private static string? ExtractSourceLine(Exception ex)
    {
        if (ex.StackTrace is null) return null;

        // Tìm frame đầu tiên thuộc ICare247 code (có "line XX")
        var frame = ex.StackTrace
            .Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line =>
                line.StartsWith("at ICare247.") &&
                line.Contains(":line "));

        if (frame is null) return null;

        // Rút gọn: bỏ "at ", chỉ giữ "Class.Method(...) in file.cs:line XX"
        return frame.Length > 120 ? frame[3..120] + "…" : frame[3..];
    }
}
