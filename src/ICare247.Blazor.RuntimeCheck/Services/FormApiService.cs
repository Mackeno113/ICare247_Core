// File    : FormApiService.cs
// Module  : RuntimeCheck
// Purpose : Gọi API config để load FormMetadata và danh sách form.

using System.Net.Http.Json;
using ICare247.Blazor.RuntimeCheck.Models;

namespace ICare247.Blazor.RuntimeCheck.Services;

/// <summary>
/// Wrap GET /api/v1/config/forms — load metadata và danh sách form.
/// </summary>
public sealed class FormApiService
{
    private readonly HttpClient _http;
    private readonly ApiSettings _settings;

    public FormApiService(HttpClient http, ApiSettings settings)
    {
        _http     = http;
        _settings = settings;
    }

    /// <summary>
    /// Load FormMetadata theo Form_Code.
    /// Trả null nếu 404 (form không tồn tại).
    /// </summary>
    public async Task<FormMetadataDto?> GetFormAsync(
        string formCode, CancellationToken ct = default)
    {
        var url = $"/api/v1/config/forms/{Uri.EscapeDataString(formCode)}" +
                  $"?lang={_settings.LangCode}&platform={_settings.Platform}";

        var response = await _http.GetAsync(url, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FormMetadataDto>(ct);
    }

    /// <summary>
    /// Lấy danh sách form (page 1, 50 records).
    /// </summary>
    public async Task<List<FormListItemDto>> GetFormsListAsync(CancellationToken ct = default)
    {
        var url = $"/api/v1/config/forms?isActive=true&pageSize=50&platform={_settings.Platform}";

        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<PagedResultDto<FormListItemDto>>(ct);

        return result?.Items ?? [];
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
