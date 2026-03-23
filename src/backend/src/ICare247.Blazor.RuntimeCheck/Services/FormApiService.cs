// File    : FormApiService.cs
// Module  : RuntimeCheck
// Purpose : Gọi API config để load FormMetadata và danh sách form.
//           Log chi tiết mọi lỗi ra ILogger (→ browser console F12).

using System.Net.Http.Json;
using System.Text.Json;
using ICare247.Blazor.RuntimeCheck.Models;
using Microsoft.Extensions.Logging;

namespace ICare247.Blazor.RuntimeCheck.Services;

/// <summary>
/// Wrap GET /api/v1/config/forms — load metadata và danh sách form.
/// Mọi lỗi HTTP đều được log ra ILogger trước khi throw.
/// </summary>
public sealed class FormApiService
{
    private readonly HttpClient            _http;
    private readonly ApiSettings           _settings;
    private readonly ILogger<FormApiService> _logger;

    public FormApiService(HttpClient http, ApiSettings settings, ILogger<FormApiService> logger)
    {
        _http     = http;
        _settings = settings;
        _logger   = logger;
    }

    /// <summary>
    /// Load FormMetadata theo Form_Code.
    /// Trả null nếu 404 (form không tồn tại).
    /// Throw HttpRequestException kèm message rõ ràng nếu lỗi khác.
    /// </summary>
    public async Task<FormMetadataDto?> GetFormAsync(
        string formCode, CancellationToken ct = default)
    {
        var url = $"/api/v1/config/forms/{Uri.EscapeDataString(formCode)}" +
                  $"?lang={_settings.LangCode}&platform={_settings.Platform}";

        _logger.LogDebug("GetForm → GET {Url}", url);

        var response = await _http.GetAsync(url, ct);

        // 404 = form không tồn tại — không phải lỗi
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("GetForm 404 — FormCode={FormCode}", formCode);
            return null;
        }

        // Các lỗi khác — đọc ProblemDetails để log message rõ
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            _logger.LogError(
                "GetForm lỗi {Status} — FormCode={FormCode} | {Detail}",
                (int)response.StatusCode, formCode, detail);
            throw new HttpRequestException(
                $"[{(int)response.StatusCode}] {detail}");
        }

        _logger.LogDebug("GetForm OK — FormCode={FormCode}", formCode);
        return await response.Content.ReadFromJsonAsync<FormMetadataDto>(ct);
    }

    /// <summary>
    /// Lấy danh sách form (page 1, 50 records).
    /// </summary>
    public async Task<List<FormListItemDto>> GetFormsListAsync(CancellationToken ct = default)
    {
        var url = $"/api/v1/config/forms?isActive=true&pageSize=50&platform={_settings.Platform}";

        _logger.LogDebug("GetFormsList → GET {Url}", url);

        var response = await _http.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            _logger.LogError(
                "GetFormsList lỗi {Status} | {Detail}",
                (int)response.StatusCode, detail);
            throw new HttpRequestException(
                $"[{(int)response.StatusCode}] {detail}");
        }

        var result = await response.Content
            .ReadFromJsonAsync<PagedResultDto<FormListItemDto>>(ct);

        _logger.LogDebug("GetFormsList OK — {Count} forms", result?.Items.Count ?? 0);
        return result?.Items ?? [];
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Đọc ProblemDetails từ response body để lấy message rõ ràng.
    /// Fallback về status code nếu không parse được.
    /// </summary>
    private static async Task<string> ReadProblemDetailAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return $"HTTP {(int)response.StatusCode} — body rỗng";

            using var doc  = JsonDocument.Parse(body);
            var root       = doc.RootElement;

            // RFC 7807 ProblemDetails: title + detail
            var title  = root.TryGetProperty("title",  out var t) ? t.GetString() : null;
            var detail = root.TryGetProperty("detail", out var d) ? d.GetString() : null;

            if (title is not null || detail is not null)
                return string.Join(" — ", new[] { title, detail }.Where(s => !string.IsNullOrWhiteSpace(s)));

            // Không phải ProblemDetails — trả raw body (truncate 300 ký tự)
            return body.Length > 300 ? body[..300] + "..." : body;
        }
        catch
        {
            return $"HTTP {(int)response.StatusCode}";
        }
    }
}

/// <summary>Item tóm tắt trong danh sách form.</summary>
public sealed class FormListItemDto
{
    public int    FormId    { get; set; }
    public string FormCode  { get; set; } = "";
    public string FormName  { get; set; } = "";
    public string Platform  { get; set; } = "";
    public string TableName { get; set; } = "";
    public int    Version   { get; set; }
    public bool   IsActive  { get; set; }
    public int    FieldCount { get; set; }
}

/// <summary>Wrapper phân trang từ API.</summary>
public sealed class PagedResultDto<T>
{
    public List<T> Items      { get; set; } = [];
    public int     TotalCount { get; set; }
    public int     Page       { get; set; }
    public int     PageSize   { get; set; }
}
