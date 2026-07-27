// File    : AdminErrorLogApiService.cs
// Module  : ICare247_UI
// Purpose : Gọi API /api/v1/admin/error-logs cho màn Nhật ký lỗi — tra lỗi 500 theo Mã lỗi
//           hiện trên toast, xem message + stack trace đầy đủ. Chỉ đọc (không CRUD).

using System.Net.Http.Json;
using System.Text.Json;
using ICare247_UI.Models;
using Microsoft.Extensions.Logging;

namespace ICare247_UI.Services;

/// <summary>Wrap endpoint quản trị nhật ký lỗi (administration.errorlogs).</summary>
public sealed class AdminErrorLogApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<AdminErrorLogApiService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public AdminErrorLogApiService(HttpClient http, ILogger<AdminErrorLogApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Danh sách lỗi trong khoảng thời gian, lọc thêm Mã lỗi/Nguồn nếu có; lỗi → rỗng.</summary>
    public async Task<List<ErrorLogListVm>> GetErrorLogsAsync(
        DateTime? tu, DateTime? den, string? maLoi, string? nguon, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (tu is not null) qs.Add($"tu={Uri.EscapeDataString(tu.Value.ToString("o"))}");
        if (den is not null) qs.Add($"den={Uri.EscapeDataString(den.Value.ToString("o"))}");
        if (!string.IsNullOrWhiteSpace(maLoi)) qs.Add($"maLoi={Uri.EscapeDataString(maLoi)}");
        if (!string.IsNullOrWhiteSpace(nguon)) qs.Add($"nguon={Uri.EscapeDataString(nguon)}");
        var url = "/api/v1/admin/error-logs" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");

        try
        {
            return await _http.GetFromJsonAsync<List<ErrorLogListVm>>(url, JsonOpts, ct) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không lấy được danh sách nhật ký lỗi.");
            return [];
        }
    }

    /// <summary>Chi tiết 1 lỗi (message + stack trace); lỗi → null.</summary>
    public async Task<ErrorLogDetailVm?> GetErrorLogDetailAsync(long id, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<ErrorLogDetailVm>($"/api/v1/admin/error-logs/{id}", JsonOpts, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không lấy được chi tiết lỗi {Id}.", id);
            return null;
        }
    }
}
