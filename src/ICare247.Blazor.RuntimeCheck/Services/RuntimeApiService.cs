// File    : RuntimeApiService.cs
// Module  : RuntimeCheck
// Purpose : Gọi runtime API — validate-field, validate form, handle-event.

using System.Net.Http.Json;
using ICare247.Blazor.RuntimeCheck.Models;

namespace ICare247.Blazor.RuntimeCheck.Services;

/// <summary>
/// Wrap POST /api/v1/forms/{code}/validate-field, /validate, /handle-event.
/// </summary>
public sealed class RuntimeApiService
{
    private readonly HttpClient _http;

    public RuntimeApiService(HttpClient http) => _http = http;

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

        var response = await _http.PostAsJsonAsync(url, body, ct);

        if (!response.IsSuccessStatusCode)
            return new FieldValidationResponseDto { IsValid = true };

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

        var response = await _http.PostAsJsonAsync(url, body, ct);

        if (!response.IsSuccessStatusCode)
            return new FormValidationResponseDto { IsValid = true };

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

        var response = await _http.PostAsJsonAsync(url, body, ct);

        if (!response.IsSuccessStatusCode)
            return new UiDeltaResponseDto();

        return await response.Content
            .ReadFromJsonAsync<UiDeltaResponseDto>(ct)
               ?? new UiDeltaResponseDto();
    }
}
