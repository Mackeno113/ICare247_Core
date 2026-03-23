// File    : ConnectionChecker.cs
// Module  : Api
// Layer   : Api
// Purpose : Test kết nối tất cả connection string ngay khi khởi động —
//           hiển thị kết quả qua DebugLogger trước khi app bắt đầu nhận request.

using Microsoft.Data.SqlClient;
using StackExchange.Redis;

namespace ICare247.Api;

/// <summary>
/// Kiểm tra kết nối DB + Redis khi khởi động.
/// Gọi trong Program.cs sau <c>builder.Build()</c>.
/// Không throw — chỉ log kết quả, app vẫn chạy dù kết nối thất bại.
/// </summary>
public static class ConnectionChecker
{
    private const int TimeoutSeconds = 5;

    // ── Public entry point ────────────────────────────────────────────────────

    /// <summary>
    /// Kiểm tra tất cả connection string đã cấu hình.
    /// Gọi sau <c>var app = builder.Build()</c> trong Program.cs.
    /// </summary>
    public static async Task CheckAllAsync(IConfiguration configuration)
    {
        DebugLogger.Log("ConnectionChecker", "── Kiểm tra kết nối ──────────────────────");

        await CheckSqlAsync(configuration, "Config", "Config DB (ICare247_Config)");
        await CheckSqlAsync(configuration, "Data",   "Data DB  (nghiệp vụ)       ");
        await CheckRedisAsync(configuration);

        DebugLogger.Log("ConnectionChecker", "─────────────────────────────────────────");
    }

    // ── SQL Server ────────────────────────────────────────────────────────────

    /// <summary>
    /// Test SQL Server connection string theo tên key.
    /// Hiển thị: ✓ OK (Xms) hoặc ✗ lỗi ngắn gọn.
    /// </summary>
    private static async Task CheckSqlAsync(
        IConfiguration configuration,
        string key,
        string label)
    {
        // Lấy connection string — thử key chính, fallback "Default"
        var connStr = configuration.GetConnectionString(key)
                   ?? (key == "Config" ? configuration.GetConnectionString("Default") : null);

        // Chưa cấu hình
        if (string.IsNullOrWhiteSpace(connStr))
        {
            DebugLogger.Warn("ConnectionChecker", $"{label} → chưa cấu hình (để trống)");
            return;
        }

        // Rút gọn connection string để log: chỉ hiển thị Server + Database
        var shortConn = ShortenSqlConn(connStr);
        DebugLogger.Log("ConnectionChecker", $"{label} → {shortConn}");

        // Test mở connection với timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync(cts.Token);
            sw.Stop();

            DebugLogger.Log("ConnectionChecker",
                $"  ✓ Kết nối thành công ({sw.ElapsedMilliseconds}ms)" +
                $" — Server: {conn.DataSource}, DB: {conn.Database}");
        }
        catch (OperationCanceledException)
        {
            DebugLogger.Error("ConnectionChecker",
                $"  ✗ Timeout sau {TimeoutSeconds}s — kiểm tra Server name và firewall");
        }
        catch (Exception ex)
        {
            DebugLogger.Error("ConnectionChecker",
                $"  ✗ {ex.ToReadable()}");
        }
    }

    // ── Redis ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Test Redis connection string.
    /// </summary>
    private static async Task CheckRedisAsync(IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(connStr))
        {
            DebugLogger.Log("ConnectionChecker",
                "Redis cache      → không cấu hình → dùng MemoryCache local");
            return;
        }

        DebugLogger.Log("ConnectionChecker", $"Redis cache      → {connStr}");

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var redis  = await ConnectionMultiplexer.ConnectAsync(connStr);
            var db           = redis.GetDatabase();
            await db.PingAsync();
            sw.Stop();

            DebugLogger.Log("ConnectionChecker",
                $"  ✓ Redis OK ({sw.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            DebugLogger.Error("ConnectionChecker",
                $"  ✗ {ex.ToReadable()}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Rút gọn connection string để log an toàn — chỉ giữ Server + Database,
    /// bỏ password và các thông tin nhạy cảm.
    /// </summary>
    private static string ShortenSqlConn(string connStr)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connStr);
            return $"Server={builder.DataSource}; Database={builder.InitialCatalog}";
        }
        catch
        {
            // Nếu parse thất bại → chỉ hiển thị 40 ký tự đầu
            return connStr.Length > 40 ? connStr[..40] + "…" : connStr;
        }
    }
}
