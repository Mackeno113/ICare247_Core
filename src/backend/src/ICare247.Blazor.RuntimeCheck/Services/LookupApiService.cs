// File    : LookupApiService.cs
// Module  : RuntimeCheck
// Purpose : Gọi GET /api/v1/lookups/{code} để lấy danh sách options cho select field.
//           Cache trong-session bằng Dictionary để tránh gọi lại cùng 1 code nhiều lần.

using System.Net.Http.Json;
using ICare247.Blazor.RuntimeCheck.Models;
using Microsoft.Extensions.Logging;

namespace ICare247.Blazor.RuntimeCheck.Services;

/// <summary>
/// Lấy Sys_Lookup options qua API. Kết quả được cache in-memory theo LookupCode
/// để các field cùng code (VD: nhiều form dùng GENDER) chỉ gọi API 1 lần/session.
/// </summary>
public sealed class LookupApiService
{
    private readonly HttpClient                          _http;
    private readonly ApiSettings                         _settings;
    private readonly ILogger<LookupApiService>           _logger;
    private readonly Dictionary<string, List<LookupOptionDto>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public LookupApiService(
        HttpClient http, ApiSettings settings, ILogger<LookupApiService> logger)
    {
        _http     = http;
        _settings = settings;
        _logger   = logger;
    }

    /// <summary>
    /// Lấy danh sách options của một lookup code.
    /// Trả danh sách rỗng nếu code không tồn tại hoặc lỗi network.
    /// </summary>
    public async Task<List<LookupOptionDto>> GetOptionsAsync(
        string lookupCode, CancellationToken ct = default)
    {
        // Trả từ cache nếu đã load
        if (_cache.TryGetValue(lookupCode, out var cached))
            return cached;

        var url = $"/api/v1/lookups/{Uri.EscapeDataString(lookupCode)}" +
                  $"?lang={_settings.LangCode}";

        _logger.LogDebug("GetLookupOptions → GET {Url}", url);

        try
        {
            var response = await _http.GetAsync(url, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("LookupCode không tồn tại: {Code}", lookupCode);
                _cache[lookupCode] = [];
                return [];
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "GetLookupOptions lỗi {Status} — Code={Code}",
                    (int)response.StatusCode, lookupCode);
                return [];
            }

            var options = await response.Content
                .ReadFromJsonAsync<List<LookupOptionDto>>(ct) ?? [];

            _cache[lookupCode] = options;
            _logger.LogDebug(
                "GetLookupOptions OK — Code={Code}, {Count} options", lookupCode, options.Count);

            return options;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetLookupOptions exception — Code={Code}", lookupCode);
            return [];
        }
    }

    /// <summary>
    /// Load options cho nhiều lookup codes cùng lúc (gọi song song).
    /// Trả Dictionary&lt;code, options&gt;.
    /// </summary>
    public async Task<Dictionary<string, List<LookupOptionDto>>> GetOptionsBatchAsync(
        IEnumerable<string> codes, CancellationToken ct = default)
    {
        // Loại bỏ duplicate, chỉ gọi API cho code chưa có trong cache
        var distinctCodes = codes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // Gọi song song — mỗi code 1 task
        var tasks = distinctCodes.Select(code => GetOptionsAsync(code, ct));
        var results = await Task.WhenAll(tasks);

        return distinctCodes
            .Zip(results)
            .ToDictionary(
                pair => pair.First,
                pair => pair.Second,
                StringComparer.OrdinalIgnoreCase);
    }
}
