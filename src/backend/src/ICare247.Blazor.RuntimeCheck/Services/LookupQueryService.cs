// File    : LookupQueryService.cs
// Module  : RuntimeCheck
// Purpose : Gọi POST /api/v1/lookups/query-dynamic và trả rows dạng Dictionary.
//           Không cache vì data dynamic theo contextValues (cascading lookup).

using System.Net.Http.Json;
using System.Text.Json;
using ICare247.Blazor.RuntimeCheck.Models;
using Microsoft.Extensions.Logging;

namespace ICare247.Blazor.RuntimeCheck.Services;

/// <summary>
/// Implementation của <see cref="ILookupQueryService"/>.
/// Gọi backend API và deserialize response thành <c>List&lt;Dictionary&lt;string, object?&gt;&gt;</c>.
/// </summary>
public sealed class LookupQueryService : ILookupQueryService
{
    private readonly HttpClient                   _http;
    private readonly ApiSettings                  _settings;
    private readonly ILogger<LookupQueryService>  _logger;

    // JSON options để deserialize Dictionary<string, object?> từ response
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LookupQueryService(
        HttpClient http, ApiSettings settings, ILogger<LookupQueryService> logger)
    {
        _http     = http;
        _settings = settings;
        _logger   = logger;
    }

    /// <inheritdoc />
    public async Task<List<Dictionary<string, object?>>> QueryAsync(
        int fieldId,
        Dictionary<string, object?> contextValues,
        CancellationToken ct = default)
    {
        const string url = "/api/v1/lookups/query-dynamic";

        var body = new { fieldId, contextValues };

        _logger.LogDebug(
            "LookupQueryService.QueryAsync → POST {Url} FieldId={FieldId} ContextKeys=[{Keys}]",
            url, fieldId, string.Join(", ", contextValues.Keys));

        try
        {
            var response = await _http.PostAsJsonAsync(url, body, ct);

            if (!response.IsSuccessStatusCode)
            {
                var detail = await TryReadErrorAsync(response);
                _logger.LogError(
                    "QueryDynamic lỗi {Status} — FieldId={FieldId} | {Detail}",
                    (int)response.StatusCode, fieldId, detail);
                return [];
            }

            // Backend trả List<IDictionary<string, object>> — deserialize sang List<Dictionary<...>>
            var json = await response.Content.ReadAsStringAsync(ct);

            var rows = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json, _jsonOpts);
            if (rows is null) return [];

            // Chuyển JsonElement → object? (string, number, bool, null)
            return rows.Select(r =>
                r.ToDictionary(
                    kv => kv.Key,
                    kv => (object?)UnwrapJsonElement(kv.Value),
                    StringComparer.OrdinalIgnoreCase))
                .ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LookupQueryService exception — FieldId={FieldId}", fieldId);
            return [];
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Chuyển JsonElement về .NET primitive để renderer dùng.</summary>
    private static object? UnwrapJsonElement(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String  => el.GetString(),
        JsonValueKind.Number  => el.TryGetInt64(out var i)  ? (object?)i
                               : el.TryGetDouble(out var d) ? d
                               : el.GetRawText(),
        JsonValueKind.True    => true,
        JsonValueKind.False   => false,
        JsonValueKind.Null    => null,
        _                     => el.GetRawText()
    };

    private static async Task<string> TryReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body)) return $"HTTP {(int)response.StatusCode}";
            using var doc = JsonDocument.Parse(body);
            var root  = doc.RootElement;
            var title  = root.TryGetProperty("title",  out var t) ? t.GetString() : null;
            var detail = root.TryGetProperty("detail", out var d) ? d.GetString() : null;
            return string.Join(" — ", new[] { title, detail }.Where(s => !string.IsNullOrWhiteSpace(s)))
                   is { Length: > 0 } msg ? msg : body[..Math.Min(body.Length, 200)];
        }
        catch { return $"HTTP {(int)response.StatusCode}"; }
    }
}
