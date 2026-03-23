// File    : RuntimeApiService.cs
// Module  : RuntimeCheck
// Purpose : Gọi runtime API — validate-field, validate form, handle-event.
//           Log chi tiết mọi lỗi ra ILogger (→ browser console F12).

using System.Net.Http.Json;
using System.Text.Json;
using ICare247.Blazor.RuntimeCheck.Models;
using Microsoft.Extensions.Logging;

namespace ICare247.Blazor.RuntimeCheck.Services;

/// <summary>
/// Wrap POST /api/v1/forms/{code}/validate-field, /validate, /handle-event.
/// Mọi lỗi HTTP đều được log ra ILogger trước khi trả fallback/throw.
/// </summary>
public sealed class RuntimeApiService
{
    private readonly HttpClient                _http;
    private readonly ILogger<RuntimeApiService> _logger;

    public RuntimeApiService(HttpClient http, ILogger<RuntimeApiService> logger)
    {
        _http   = http;
        _logger = logger;
    }

    /// <summary>Validate một field đơn lẻ, trả errors nếu có.</summary>
    public async Task<FieldValidationResponseDto> ValidateFieldAsync(
        string formCode,
        string fieldCode,
        object? value,
        Dictionary<string, object?> context,
        CancellationToken ct = default)
    {
        var url  = $"/api/v1/forms/{Uri.EscapeDataString(formCode)}/validate-field";
        var body = new ValidateFieldRequest
        {
            FieldCode       = fieldCode,
            Value           = value,
            ContextSnapshot = context
        };

        _logger.LogDebug("ValidateField → {FormCode}.{FieldCode}", formCode, fieldCode);

        var response = await _http.PostAsJsonAsync(url, body, ct);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            _logger.LogWarning(
                "ValidateField {Status} — {FormCode}.{FieldCode} | {Detail}",
                (int)response.StatusCode, formCode, fieldCode, detail);
            // Fallback — không crash form, chỉ bỏ qua validation
            return new FieldValidationResponseDto { IsValid = true };
        }

        return await response.Content
            .ReadFromJsonAsync<FieldValidationResponseDto>(ct)
               ?? new FieldValidationResponseDto { IsValid = true };
    }

    /// <summary>Validate toàn bộ form trước submit.</summary>
    public async Task<FormValidationResponseDto> ValidateFormAsync(
        string formCode,
        Dictionary<string, object?> context,
        CancellationToken ct = default)
    {
        var url  = $"/api/v1/forms/{Uri.EscapeDataString(formCode)}/validate";
        var body = new ValidateFormRequest { ContextSnapshot = context };

        _logger.LogDebug("ValidateForm → {FormCode}", formCode);

        var response = await _http.PostAsJsonAsync(url, body, ct);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            _logger.LogWarning(
                "ValidateForm {Status} — {FormCode} | {Detail}",
                (int)response.StatusCode, formCode, detail);
            return new FormValidationResponseDto { IsValid = true };
        }

        return await response.Content
            .ReadFromJsonAsync<FormValidationResponseDto>(ct)
               ?? new FormValidationResponseDto { IsValid = true };
    }

    /// <summary>
    /// Gửi event lên backend, nhận danh sách UiDelta để áp dụng lên form state.
    /// </summary>
    public async Task<UiDeltaResponseDto> HandleEventAsync(
        string formCode,
        string eventType,
        string? sourceField,
        Dictionary<string, object?> context,
        CancellationToken ct = default)
    {
        var url  = $"/api/v1/forms/{Uri.EscapeDataString(formCode)}/handle-event";
        var body = new HandleEventRequest
        {
            EventType       = eventType,
            SourceField     = sourceField,
            ContextSnapshot = context
        };

        _logger.LogDebug("HandleEvent → {FormCode} [{EventType}] field={SourceField}",
            formCode, eventType, sourceField ?? "(none)");

        var response = await _http.PostAsJsonAsync(url, body, ct);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            _logger.LogWarning(
                "HandleEvent {Status} — {FormCode} [{EventType}] | {Detail}",
                (int)response.StatusCode, formCode, eventType, detail);
            return new UiDeltaResponseDto();
        }

        var result = await response.Content
            .ReadFromJsonAsync<UiDeltaResponseDto>(ct)
               ?? new UiDeltaResponseDto();

        if (result.Delta.Count > 0)
            _logger.LogDebug(
                "HandleEvent OK — {FormCode} [{EventType}] → {DeltaCount} deltas",
                formCode, eventType, result.Delta.Count);

        return result;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Đọc ProblemDetails từ response body để lấy message rõ ràng.
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

            var title  = root.TryGetProperty("title",  out var t) ? t.GetString() : null;
            var detail = root.TryGetProperty("detail", out var d) ? d.GetString() : null;

            if (title is not null || detail is not null)
                return string.Join(" — ", new[] { title, detail }.Where(s => !string.IsNullOrWhiteSpace(s)));

            return body.Length > 300 ? body[..300] + "..." : body;
        }
        catch
        {
            return $"HTTP {(int)response.StatusCode}";
        }
    }
}
