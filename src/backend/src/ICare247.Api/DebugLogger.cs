// File    : DebugLogger.cs
// Module  : Api
// Layer   : Api
// Purpose : Logger debug đơn giản — ghi ra Console + tuỳ chọn ra file.
//           Dùng trước khi Serilog sẵn sàng (startup, LocalConfigLoader)
//           hoặc bất kỳ chỗ nào cần log nhanh có thể bật/tắt.
//
// Rule    : Mọi Console.WriteLine("[X] ...") phải thay bằng DebugLogger.Log/Warn/Error.
//           Xem .claude-rules/debug-logger.md

namespace ICare247.Api;

/// <summary>
/// Logger debug tĩnh — hoạt động độc lập, không phụ thuộc Serilog/DI.
/// Bật/tắt qua <see cref="Enabled"/> hoặc <see cref="Configure"/>.
/// </summary>
/// <remarks>
/// Format: <c>[HH:mm:ss.fff] [Module] Message</c><br/>
/// Thread-safe: dùng lock khi ghi file.
/// </remarks>
public static class DebugLogger
{
    // ── Trạng thái ────────────────────────────────────────────────────────────

    /// <summary>Bật/tắt toàn bộ logger. Default: true.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Ghi thêm ra file khi true. Default: false.</summary>
    public static bool WriteToFile { get; set; } = false;

    /// <summary>
    /// Đường dẫn file log. Để trống → dùng %APPDATA%\ICare247\Api\debug.log.
    /// </summary>
    public static string FilePath { get; set; } = DefaultFilePath();

    // ── Lock ghi file (thread-safe) ──────────────────────────────────────────
    private static readonly object _fileLock = new();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Log mức Info — màu trắng trên console.</summary>
    public static void Log(string module, string message)
        => Write(module, message, level: null, ConsoleColor.Gray);

    /// <summary>Log mức Warning — màu vàng trên console.</summary>
    public static void Warn(string module, string message)
        => Write(module, message, level: "WARN", ConsoleColor.Yellow);

    /// <summary>Log mức Error — màu đỏ trên console.</summary>
    public static void Error(string module, string message)
        => Write(module, message, level: "ERROR", ConsoleColor.Red);

    /// <summary>
    /// Log exception — tự động dùng <see cref="ExceptionExtensions.ToReadable"/>
    /// để tóm tắt thành 1 dòng ngắn gọn thay vì dump cả stack trace.
    /// </summary>
    /// <example>
    /// catch (Exception ex) { DebugLogger.Error("DB", ex); }
    /// → [10:25:33] [DB] ERROR SqlException: Cannot connect... | Win32Exception: File not found
    /// </example>
    public static void Error(string module, Exception ex)
        => Write(module, ex.ToReadable(), level: "ERROR", ConsoleColor.Red);

    /// <summary>
    /// Log exception với thêm context message.
    /// </summary>
    /// <example>
    /// DebugLogger.Error("LocalConfig", "Đọc file thất bại", ex);
    /// → [10:25:33] [LocalConfig] ERROR Đọc file thất bại → SqlException: ...
    /// </example>
    public static void Error(string module, string context, Exception ex)
        => Write(module, $"{context} → {ex.ToReadable()}", level: "ERROR", ConsoleColor.Red);

    // ── Cấu hình từ IConfiguration ───────────────────────────────────────────

    /// <summary>
    /// Đọc section "DebugLog" từ IConfiguration và cập nhật trạng thái.
    /// Gọi sau khi builder.Configuration đã nạp xong (sau AddLocalConfig).
    /// </summary>
    /// <example>
    /// appsettings.local.json:
    /// <code>
    /// "DebugLog": { "Enabled": true, "WriteToFile": true, "FilePath": "" }
    /// </code>
    /// </example>
    public static void Configure(IConfiguration configuration)
    {
        var section = configuration.GetSection("DebugLog");
        if (!section.Exists()) return;

        // Enabled
        if (bool.TryParse(section["Enabled"], out var enabled))
            Enabled = enabled;

        // WriteToFile
        if (bool.TryParse(section["WriteToFile"], out var writeToFile))
            WriteToFile = writeToFile;

        // FilePath — để trống → giữ default
        var filePath = section["FilePath"];
        if (!string.IsNullOrWhiteSpace(filePath))
            FilePath = filePath;

        // Log trạng thái sau khi cấu hình
        if (Enabled)
            Log("DebugLogger", $"Enabled={Enabled}, WriteToFile={WriteToFile}, File={FilePath}");
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Ghi một dòng log ra Console (màu tương ứng level)
    /// và tuỳ chọn ra file.
    /// </summary>
    private static void Write(string module, string message, string? level, ConsoleColor color)
    {
        if (!Enabled) return;

        // Format: [10:25:33.142] [Module] LEVEL? Message
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var levelPart = level is not null ? $" {level}" : string.Empty;
        var line      = $"[{timestamp}] [{module}]{levelPart} {message}";

        // Ghi ra Console với màu
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(line);
        Console.ForegroundColor = prev;

        // Ghi ra file nếu bật
        if (WriteToFile)
            AppendToFile(line);
    }

    /// <summary>
    /// Ghi dòng log vào file — thread-safe qua lock.
    /// Tự tạo thư mục nếu chưa có.
    /// </summary>
    private static void AppendToFile(string line)
    {
        try
        {
            lock (_fileLock)
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                File.AppendAllText(FilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Không crash app nếu ghi file thất bại (disk full, permission,...)
        }
    }

    /// <summary>
    /// Đường dẫn file log mặc định:
    /// %APPDATA%\ICare247\Api\debug.log
    /// </summary>
    private static string DefaultFilePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ICare247", "Api", "debug.log");
}
