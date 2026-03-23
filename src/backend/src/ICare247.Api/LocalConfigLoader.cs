// File    : LocalConfigLoader.cs
// Module  : Api
// Layer   : Api
// Purpose : Nạp file cấu hình local nằm ngoài repo — pattern giống WPF ConfigStudio.
//           File: %APPDATA%\ICare247\Api\appsettings.local.json
//           Nếu chưa tồn tại → tự tạo template lần đầu khởi động.

namespace ICare247.Api;

/// <summary>
/// Quản lý file cấu hình local nằm ngoài repo.
/// Giống cách WPF ConfigStudio dùng %APPDATA%\ICare247\ConfigStudio\appsettings.json.
/// </summary>
public static class LocalConfigLoader
{
    // ── Đường dẫn file local ─────────────────────────────────────────────────

    /// <summary>
    /// Thư mục lưu file local: %APPDATA%\ICare247\Api\
    /// Ví dụ: C:\Users\ICare247\AppData\Roaming\ICare247\Api\
    /// </summary>
    public static string ConfigDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ICare247", "Api");

    /// <summary>
    /// Đường dẫn đầy đủ file cấu hình local.
    /// Ví dụ: C:\Users\ICare247\AppData\Roaming\ICare247\Api\appsettings.local.json
    /// </summary>
    public static string ConfigFilePath =>
        Path.Combine(ConfigDirectory, "appsettings.local.json");

    // ── Template nội dung file ────────────────────────────────────────────────

    /// <summary>
    /// Nội dung template khi tạo file lần đầu.
    /// User cần sửa các giá trị placeholder trước khi chạy.
    /// </summary>
    private const string Template = """
        {
          "_readme": "File này chứa cấu hình thật cho ICare247 API. KHÔNG commit vào git.",

          "ConnectionStrings": {

            "_readme_Config": "Config DB: ICare247_Config — chứa metadata form engine (Ui_Form, Sys_*, Val_*, Evt_*,...)",
            "Config": "Server=localhost;Database=ICare247_Config;Trusted_Connection=True;TrustServerCertificate=True;",

            "_readme_Data": "Data DB: DB nghiệp vụ thực tế (bệnh nhân, hồ sơ,...). Để trống = dùng chung Config DB (dev).",
            "Data": "",

            "_readme_Redis": "Redis cache (tuỳ chọn). Để trống = dùng MemoryCache local.",
            "Redis": "",

            "_example_SqlExpress": "Server=localhost\\SQLEXPRESS;Database=ICare247_Config;Trusted_Connection=True;TrustServerCertificate=True;"
          },

          "Jwt": {
            "Issuer": "https://icare247.vn",
            "Audience": "icare247-api",
            "SecretKey": "CHANGE_ME_32CHARS_MINIMUM_SECRET_KEY",
            "ExpirationMinutes": 480
          },

          "DebugLog": {
            "_readme": "Bật/tắt DebugLogger — log debug trước Serilog. Xem .claude-rules/debug-logger.md",
            "Enabled": true,
            "WriteToFile": false,
            "FilePath": ""
          },

          "Serilog": {
            "MinimumLevel": {
              "Default": "Debug",
              "Override": {
                "Microsoft.AspNetCore": "Information",
                "System": "Warning"
              }
            }
          }
        }
        """;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Đăng ký file cấu hình local vào WebApplicationBuilder.
    /// Gọi trong Program.cs TRƯỚC khi builder.Build().
    /// </summary>
    /// <param name="builder">WebApplicationBuilder của app.</param>
    /// <returns>True nếu file tồn tại và được nạp; false nếu mới tạo template.</returns>
    public static bool AddLocalConfig(this WebApplicationBuilder builder)
    {
        // Tạo thư mục nếu chưa có
        Directory.CreateDirectory(ConfigDirectory);

        var fileExists = File.Exists(ConfigFilePath);

        if (!fileExists)
        {
            // Tạo template — user cần cấu hình trước khi dùng
            File.WriteAllText(ConfigFilePath, Template);

            DebugLogger.Warn("LocalConfig", $"File chưa tồn tại — đã tạo template tại: {ConfigFilePath}");
            DebugLogger.Warn("LocalConfig", "→ Mở file, điền connection string thật, rồi khởi động lại.");
        }
        else
        {
            DebugLogger.Log("LocalConfig", $"Nạp cấu hình từ: {ConfigFilePath}");
        }

        // Thêm vào configuration pipeline
        // optional: true  → không crash nếu file bị xóa sau khi start
        // reloadOnChange: true → app tự reload khi file thay đổi (không cần restart)
        builder.Configuration.AddJsonFile(
            ConfigFilePath,
            optional: true,
            reloadOnChange: true);

        return fileExists;
    }

    /// <summary>
    /// Mở file cấu hình bằng Notepad (Windows) để user chỉnh sửa.
    /// Dùng khi template mới được tạo.
    /// </summary>
    public static void OpenConfigFileForEdit()
    {
        if (!OperatingSystem.IsWindows()) return;
        if (!File.Exists(ConfigFilePath)) return;

        try
        {
            System.Diagnostics.Process.Start("notepad.exe", ConfigFilePath);
        }
        catch
        {
            // Không quan trọng nếu không mở được
        }
    }
}
