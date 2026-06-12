// File    : I18nService.cs
// Module  : RuntimeCheck
// Purpose : i18n chuỗi UI tĩnh dùng chung (nút Lưu/Hủy/…) — load Sys_Resource scope 'common.*'
//           qua GET /api/v1/resources, cache theo ngôn ngữ. Fallback chuỗi truyền vào nếu thiếu.

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ICare247.Blazor.RuntimeCheck.Services;

/// <summary>
/// Resolve nhãn UI dùng chung theo key (vd <c>common.btn.save</c>). Gọi <see cref="EnsureCommonLoadedAsync"/>
/// một lần (khi mở form), sau đó <see cref="T"/> tra cứu đồng bộ từ cache.
/// </summary>
public sealed class I18nService
{
    private readonly HttpClient _http;
    private readonly ILogger<I18nService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    /// <summary>Tập key UI dùng chung — load 1 lần/ngôn ngữ.</summary>
    private static readonly string[] CommonKeys =
    {
        "common.btn.save", "common.btn.cancel", "common.btn.saving",
        "common.btn.add", "common.btn.delete",
        "common.action.create", "common.action.update",
        // Panel lọc nâng cao (Ui_View_Filter)
        "common.filter.search", "common.filter.reset", "common.filter.searching",
        "common.validation.required",
    };

    private readonly Dictionary<string, Dictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private string _lang = "vi";

    public I18nService(HttpClient http, ILogger<I18nService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Load (1 lần/ngôn ngữ) map resource 'common.*' và đặt làm ngôn ngữ hiện tại.</summary>
    public async Task EnsureCommonLoadedAsync(string lang, CancellationToken ct = default)
    {
        _lang = string.IsNullOrWhiteSpace(lang) ? "vi" : lang;
        if (_cache.ContainsKey(_lang)) return;

        try
        {
            var url = $"/api/v1/resources?lang={Uri.EscapeDataString(_lang)}"
                    + $"&keys={Uri.EscapeDataString(string.Join(",", CommonKeys))}";
            var map = await _http.GetFromJsonAsync<Dictionary<string, string>>(url, JsonOpts, ct)
                      ?? new Dictionary<string, string>();
            _cache[_lang] = new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "I18n: load common resource lỗi (dùng fallback).");
            _cache[_lang] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>Trả nhãn theo key ở ngôn ngữ hiện tại; nếu thiếu → <paramref name="fallback"/>.</summary>
    public string T(string key, string fallback)
        => _cache.TryGetValue(_lang, out var m)
           && m.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v)
            ? v : fallback;
}
