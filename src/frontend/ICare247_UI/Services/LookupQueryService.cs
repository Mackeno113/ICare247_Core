// File    : LookupQueryService.cs
// Module  : ICare247_UI
// Purpose : Gọi POST /api/v1/lookups/query-dynamic và trả rows dạng Dictionary.
//           Không cache vì data dynamic theo contextValues (cascading lookup).

using System.Net.Http.Json;
using System.Text.Json;
using ICare247.UI.DynamicForms.Abstractions;
using ICare247.UI.DynamicForms.Models;
using ICare247_UI.Models;
using Microsoft.Extensions.Logging;

namespace ICare247_UI.Services;

/// <summary>
/// Implementation của <see cref="ILookupQueryService"/>.
/// Gọi backend API và deserialize response thành <c>List&lt;Dictionary&lt;string, object?&gt;&gt;</c>.
/// </summary>
public sealed class LookupQueryService : ILookupQueryService
{
    private readonly HttpClient                   _http;
    private readonly ApiSettings                  _settings;
    private readonly FormApiService               _formApi;
    private readonly LookupApiService             _lookupApi;
    private readonly ILogger<LookupQueryService>  _logger;

    // JSON options để deserialize Dictionary<string, object?> từ response
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LookupQueryService(
        HttpClient http, ApiSettings settings,
        FormApiService formApi, LookupApiService lookupApi,
        ILogger<LookupQueryService> logger)
    {
        _http      = http;
        _settings  = settings;
        _formApi   = formApi;
        _lookupApi = lookupApi;
        _logger    = logger;
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

    /// <inheritdoc />
    public async Task<List<Dictionary<string, object?>>> QueryTreeAsync(
        int fieldId,
        Dictionary<string, object?> contextValues,
        CancellationToken ct = default)
    {
        const string url = "/api/v1/lookups/query-tree";

        var body = new { fieldId, contextValues };

        _logger.LogDebug(
            "LookupQueryService.QueryTreeAsync → POST {Url} FieldId={FieldId}",
            url, fieldId);

        try
        {
            var response = await _http.PostAsJsonAsync(url, body, ct);

            if (!response.IsSuccessStatusCode)
            {
                var detail = await TryReadErrorAsync(response);
                _logger.LogError(
                    "QueryTree lỗi {Status} — FieldId={FieldId} | {Detail}",
                    (int)response.StatusCode, fieldId, detail);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var rows = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json, _jsonOpts);
            if (rows is null) return [];

            return rows.Select(r =>
                r.ToDictionary(
                    kv => kv.Key,
                    kv => (object?)UnwrapJsonElement(kv.Value),
                    StringComparer.OrdinalIgnoreCase))
                .ToList();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LookupQueryService.QueryTreeAsync exception — FieldId={FieldId}", fieldId);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<LookupInsertResult> InsertAsync(
        int fieldId,
        Dictionary<string, object?> values,
        CancellationToken ct = default)
    {
        const string url = "/api/v1/lookups/insert";

        var body = new { fieldId, values };

        try
        {
            var response = await _http.PostAsJsonAsync(url, body, ct);

            if (!response.IsSuccessStatusCode)
            {
                var detail = await TryReadErrorAsync(response);
                _logger.LogError(
                    "InsertLookup lỗi {Status} — FieldId={FieldId} | {Detail}",
                    (int)response.StatusCode, fieldId, detail);
                return new LookupInsertResult(null, null, detail);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var value = root.TryGetProperty("value", out var v) && v.ValueKind != JsonValueKind.Null
                ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText())
                : null;
            var display = root.TryGetProperty("display", out var d) && d.ValueKind == JsonValueKind.String
                ? d.GetString()
                : null;

            return new LookupInsertResult(value, display, null);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LookupQueryService.InsertAsync exception — FieldId={FieldId}", fieldId);
            return new LookupInsertResult(null, null, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<LookupAddForm?> GetAddFormAsync(string formCode, CancellationToken ct = default)
    {
        try
        {
            var form = await _formApi.GetFormAsync(formCode);
            if (form is null)
            {
                _logger.LogWarning("GetAddFormAsync: form '{FormCode}' không tồn tại.", formCode);
                return null;
            }

            var title = form.FormName.Length > 0 ? form.FormName : form.FormCode;
            var fields = form.Fields.Select(f => new FieldState
            {
                FieldId          = f.FieldId,
                FieldCode        = f.FieldCode.Length > 0 ? f.FieldCode : $"field_{f.FieldId}",
                FieldType        = NormalizeFieldType(f.FieldType, f.LookupSource),
                Label            = f.Label.Length > 0 ? f.Label : f.FieldCode,
                IsVisible        = f.IsVisible,
                IsReadOnly       = f.IsReadOnly,
                IsRequired       = f.IsRequired,
                IsVirtual        = f.IsVirtual,
                IsEditMode       = false,   // dialog luôn ở chế độ thêm mới
                Value            = null,
                ColSpan          = f.ColSpan > 0 ? f.ColSpan : (byte)1,
                LookupSource     = f.LookupSource,
                LookupCode       = f.LookupSource == "static"  ? f.LookupCode   : null,
                LookupConfig     = f.LookupSource == "dynamic" ? f.LookupConfig : null,
                ControlPropsJson = f.ControlPropsJson,
            }).ToList();

            await LoadStaticOptionsAsync(fields);
            return new LookupAddForm(title, fields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAddFormAsync exception — FormCode={FormCode}", formCode);
            return null;
        }
    }

    /// <summary>Nạp options cho các field static (Sys_Lookup) theo lô — gán vào FieldState.Options.</summary>
    private async Task LoadStaticOptionsAsync(List<FieldState> fields)
    {
        var codes = fields
            .Where(fs => !string.IsNullOrWhiteSpace(fs.LookupCode))
            .Select(fs => fs.LookupCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (codes.Count == 0) return;

        var optionsMap = await _lookupApi.GetOptionsBatchAsync(codes);
        foreach (var fs in fields)
            if (!string.IsNullOrWhiteSpace(fs.LookupCode)
                && optionsMap.TryGetValue(fs.LookupCode, out var opts))
                fs.Options = opts;
    }

    /// <summary>Chuyển Editor_Type DB → type lowercase cho FieldRenderer (đồng bộ FormRunner).</summary>
    private static string NormalizeFieldType(string dbType, string? lookupSource)
        => dbType.ToLower() switch
    {
        "textbox"      or "text"                              => "text",
        "textarea"     or "memo"                              => "textarea",
        "numberedit"   or "number" or "numericbox"            => "number",
        "dateedit"     or "date"   or "datepicker"            => "date",
        "datetimeedit" or "datetime" or "datetimepicker"      => "datetime",
        "checkbox"     or "bool"                              => "bool",
        "toggleswitch" or "toggle"                            => "switch",
        "combobox"     => lookupSource == "dynamic" ? "combobox" : "select",
        "select" or "lookupedit" or "dropdown" or "multiselect"
            or "radiogroup" or "lookupcombobox"               => "select",
        "lookupbox"                                           => "fklookup",
        "treelookupbox" or "treelookup"                       => "treelookup",
        "addressbox" or "address"                             => "address",
        _                                                     => "text"
    };

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
