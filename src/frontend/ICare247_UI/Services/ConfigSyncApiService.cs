// File    : ConfigSyncApiService.cs
// Module  : ICare247_UI
// Purpose : Gọi API /api/v1/admin/config-sync — xem trước (dry-run) + áp đồng bộ config master→tenant.

using System.Net.Http.Json;
using System.Text.Json;
using ICare247_UI.Models;
using Microsoft.Extensions.Logging;

namespace ICare247_UI.Services;

/// <summary>Wrap endpoint đồng bộ cấu hình (CFGSYNC-3). Trả kết quả + thông điệp lỗi (không nuốt lỗi).</summary>
public sealed class ConfigSyncApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<ConfigSyncApiService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ConfigSyncApiService(HttpClient http, ILogger<ConfigSyncApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Xem trước diff (dry-run) — không ghi.</summary>
    public Task<(ConfigSyncResultVm? Result, string? Error)> PreviewAsync(CancellationToken ct = default)
        => PostAsync("/api/v1/admin/config-sync/preview", ct);

    /// <summary>Áp đồng bộ thật. Sự kiện theo sau: server ghi config + Sys_Config_Sync_Log.</summary>
    public Task<(ConfigSyncResultVm? Result, string? Error)> ApplyAsync(CancellationToken ct = default)
        => PostAsync("/api/v1/admin/config-sync", ct);

    /// <summary>POST không body; thành công → parse kết quả, thất bại → trích thông điệp lỗi (ProblemDetails).</summary>
    private async Task<(ConfigSyncResultVm?, string?)> PostAsync(string url, CancellationToken ct)
    {
        try
        {
            using var resp = await _http.PostAsync(url, content: null, ct);
            if (resp.IsSuccessStatusCode)
            {
                var result = await resp.Content.ReadFromJsonAsync<ConfigSyncResultVm>(JsonOpts, ct);
                return (result, null);
            }

            var detail = await TryReadProblemDetailAsync(resp, ct);
            return (null, detail ?? $"HTTP {(int)resp.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gọi đồng bộ cấu hình thất bại ({Url}).", url);
            return (null, ex.Message);
        }
    }

    /// <summary>Trích "detail" từ ProblemDetails (RFC 7807); không parse được → text thô.</summary>
    private static async Task<string?> TryReadProblemDetailAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String)
                return d.GetString();
            if (doc.RootElement.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                return t.GetString();
        }
        catch { /* không phải JSON — bỏ qua */ }
        return null;
    }
}
