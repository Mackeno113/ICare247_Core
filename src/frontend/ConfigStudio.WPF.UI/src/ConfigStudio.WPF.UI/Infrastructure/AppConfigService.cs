// File    : AppConfigService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Đọc / ghi appsettings.json từ %APPDATA%\ICare247\ConfigStudio\.
//           File này KHÔNG nằm trong source code — không commit vào git.

using System.IO;
using System.Text.Json;
using ConfigStudio.WPF.UI.Core.Interfaces;
using Microsoft.Data.SqlClient;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// Load / lưu cấu hình DB từ file ngoài source code để bảo mật connection string.
/// Nếu file chưa tồn tại → tạo template để người dùng điền vào.
/// </summary>
public sealed class AppConfigService : IAppConfigService
{
    // ── Đường dẫn cố định — ngoài thư mục source ─────────────
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ICare247", "ConfigStudio");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "appsettings.json");

    // ── IAppConfigService ────────────────────────────────────
    public string? ConnectionString       { get; private set; }
    public string? TargetConnectionString { get; private set; }
    public int     TenantId              { get; private set; } = 1;
    public bool    IsConfigured          => !string.IsNullOrWhiteSpace(ConnectionString);
    public bool    IsTargetConfigured    => !string.IsNullOrWhiteSpace(TargetConnectionString);
    public string  ConfigFilePath        => ConfigPath;

    /// <summary>
    /// Đọc file JSON. Nếu chưa có → tạo template → trả false.
    /// </summary>
    public async Task<bool> LoadAsync()
    {
        if (!File.Exists(ConfigPath))
        {
            await CreateTemplateAsync();
            return false;
        }

        try
        {
            var json = await File.ReadAllTextAsync(ConfigPath);
            ApplyJson(json);
            return IsConfigured;
        }
        catch (Exception)
        {
            // NOTE: JSON lỗi → trả false, không crash app
            return false;
        }
    }

    /// <summary>
    /// Thử mở kết nối SQL Server với connection string cho trước.
    /// Trả null nếu thành công; message lỗi nếu thất bại.
    /// Timeout 5 giây để tránh treo UI.
    /// </summary>
    public async Task<string?> TestConnectionAsync(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "Connection string không được để trống.";

        try
        {
            await using var conn = new SqlConnection(connectionString);
            using var cts  = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await conn.OpenAsync(cts.Token);
            return null; // thành công
        }
        catch (OperationCanceledException)
        {
            return "Timeout: không thể kết nối sau 5 giây.";
        }
        catch (Exception ex)
        {
            // NOTE: Trả message gốc để admin biết lỗi cụ thể (sai pass, sai host, ...)
            return ex.Message;
        }
    }

    /// <summary>
    /// Ghi toàn bộ cấu hình vào file và cập nhật state ngay lập tức.
    /// targetConnectionString là optional — nếu null thì giữ nguyên giá trị cũ.
    /// </summary>
    public async Task SaveAsync(string connectionString, int tenantId, string? targetConnectionString = null)
    {
        Directory.CreateDirectory(ConfigDir);

        // ── Dùng giá trị mới nếu có, giữ cũ nếu null ───────
        var targetCs = targetConnectionString ?? TargetConnectionString;

        // ── Serialize JSON đẹp ──────────────────────────────
        var obj = new
        {
            ConnectionStrings = new
            {
                ConfigStudio = connectionString,
                TargetDb     = targetCs ?? ""
            },
            TenantId = tenantId
        };

        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(ConfigPath, json);

        // ── Cập nhật state không cần đọc lại file ───────────
        ConnectionString       = connectionString;
        TargetConnectionString = string.IsNullOrWhiteSpace(targetCs) ? null : targetCs;
        TenantId               = tenantId;
    }

    // ── Helpers ──────────────────────────────────────────────

    /// <summary>Parse JSON và cập nhật properties.</summary>
    private void ApplyJson(string json)
    {
        using var doc  = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("ConnectionStrings", out var cs))
        {
            if (cs.TryGetProperty("ConfigStudio", out var connEl))
                ConnectionString = connEl.GetString();

            // ── Target DB — optional, có thể chưa có trong file cũ ──
            if (cs.TryGetProperty("TargetDb", out var targetEl))
            {
                var val = targetEl.GetString();
                TargetConnectionString = string.IsNullOrWhiteSpace(val) ? null : val;
            }
        }

        if (root.TryGetProperty("TenantId", out var tenantEl))
            TenantId = tenantEl.GetInt32();
    }

    /// <summary>
    /// Tạo file template với connection string placeholder.
    /// </summary>
    private static async Task CreateTemplateAsync()
    {
        Directory.CreateDirectory(ConfigDir);

        const string template = """
            {
              "ConnectionStrings": {
                "ConfigStudio": "Server=localhost;Database=ICare247_Config;User Id=sa;Password=YourPassword;TrustServerCertificate=true",
                "TargetDb": ""
              },
              "TenantId": 1
            }
            """;

        await File.WriteAllTextAsync(ConfigPath, template);
    }
}
