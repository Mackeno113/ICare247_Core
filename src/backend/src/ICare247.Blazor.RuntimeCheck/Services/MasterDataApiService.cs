// File    : MasterDataApiService.cs
// Module  : RuntimeCheck
// Purpose : Gọi API /api/v1/master-data — CRUD danh mục + soft-check tham chiếu.
//           Mọi lỗi HTTP log ra ILogger (→ browser console F12).

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ICare247.Blazor.RuntimeCheck.Services;

/// <summary>
/// Wrap các endpoint CRUD master-data. Trả kết quả typed cho List page / Form / Delete dialog.
/// </summary>
public sealed class MasterDataApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<MasterDataApiService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public MasterDataApiService(HttpClient http, ILogger<MasterDataApiService> logger)
    {
        _http   = http;
        _logger = logger;
    }

    /// <summary>Metadata bảng đích + cột (cho lưới + form).</summary>
    public async Task<MasterDataFormInfoDto?> GetInfoAsync(string formCode, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"/api/v1/master-data/{Uri.EscapeDataString(formCode)}/info", ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureOkAsync(resp, $"GetInfo {formCode}");
        return await resp.Content.ReadFromJsonAsync<MasterDataFormInfoDto>(JsonOpts, ct);
    }

    /// <summary>Danh sách bản ghi (search + active filter + paging).</summary>
    public async Task<MasterDataListResultDto> GetListAsync(
        string formCode, string? search, bool? activeOnly, int page = 1, int pageSize = 50,
        CancellationToken ct = default)
    {
        var url = $"/api/v1/master-data/{Uri.EscapeDataString(formCode)}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
        if (activeOnly == true) url += "&activeOnly=true";

        var resp = await _http.GetAsync(url, ct);
        await EnsureOkAsync(resp, $"GetList {formCode}");
        return await resp.Content.ReadFromJsonAsync<MasterDataListResultDto>(JsonOpts, ct)
               ?? new MasterDataListResultDto();
    }

    /// <summary>Lấy 1 bản ghi theo PK (cho form Sửa).</summary>
    public async Task<Dictionary<string, object?>?> GetByIdAsync(
        string formCode, object id, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync(
            $"/api/v1/master-data/{Uri.EscapeDataString(formCode)}/{Uri.EscapeDataString(id.ToString() ?? "")}", ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureOkAsync(resp, $"GetById {formCode}/{id}");
        return await resp.Content.ReadFromJsonAsync<Dictionary<string, object?>>(JsonOpts, ct);
    }

    /// <summary>Soft-check tham chiếu (cho dialog xóa).</summary>
    public async Task<List<ReferenceUsageDto>> GetUsageAsync(
        string formCode, object id, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync(
            $"/api/v1/master-data/{Uri.EscapeDataString(formCode)}/{Uri.EscapeDataString(id.ToString() ?? "")}/usage", ct);
        await EnsureOkAsync(resp, $"GetUsage {formCode}/{id}");
        var body = await resp.Content.ReadFromJsonAsync<UsageResponseDto>(JsonOpts, ct);
        return body?.Usages ?? [];
    }

    /// <summary>Thêm (id null) hoặc Sửa (id có giá trị). Trả kết quả gồm validation errors nếu fail.</summary>
    public async Task<MasterDataSaveResultDto> SaveAsync(
        string formCode, object? id, Dictionary<string, object?> values, CancellationToken ct = default)
    {
        var payload = new { values };
        HttpResponseMessage resp;
        if (id is null)
            resp = await _http.PostAsJsonAsync(
                $"/api/v1/master-data/{Uri.EscapeDataString(formCode)}", payload, JsonOpts, ct);
        else
            resp = await _http.PutAsJsonAsync(
                $"/api/v1/master-data/{Uri.EscapeDataString(formCode)}/{Uri.EscapeDataString(id.ToString() ?? "")}",
                payload, JsonOpts, ct);

        // 200 (success) hoặc 422 (validation fail) đều trả MasterDataSaveResult body
        if (resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.UnprocessableEntity)
            return await resp.Content.ReadFromJsonAsync<MasterDataSaveResultDto>(JsonOpts, ct)
                   ?? new MasterDataSaveResultDto(false, null, []);

        await EnsureOkAsync(resp, $"Save {formCode}");
        return new MasterDataSaveResultDto(false, null, []);
    }

    /// <summary>Xóa cứng. Trả kết quả; nếu bị tham chiếu (409) → Success=false + BlockedBy.</summary>
    public async Task<MasterDataDeleteResultDto> DeleteAsync(
        string formCode, object id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync(
            $"/api/v1/master-data/{Uri.EscapeDataString(formCode)}/{Uri.EscapeDataString(id.ToString() ?? "")}", ct);

        if (resp.StatusCode == HttpStatusCode.OK)
            return await resp.Content.ReadFromJsonAsync<MasterDataDeleteResultDto>(JsonOpts, ct)
                   ?? new MasterDataDeleteResultDto(true, []);

        // 409 Conflict — ProblemDetails có extension blockedBy[]
        if (resp.StatusCode == HttpStatusCode.Conflict)
        {
            var blocked = await ReadBlockedByAsync(resp);
            return new MasterDataDeleteResultDto(false, blocked);
        }

        await EnsureOkAsync(resp, $"Delete {formCode}/{id}");
        return new MasterDataDeleteResultDto(false, []);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task EnsureOkAsync(HttpResponseMessage resp, string ctx)
    {
        if (resp.IsSuccessStatusCode) return;
        var detail = await SafeBodyAsync(resp);
        _logger.LogError("MasterData {Ctx} lỗi {Status} | {Detail}", ctx, (int)resp.StatusCode, detail);
        throw new HttpRequestException($"[{(int)resp.StatusCode}] {detail}");
    }

    private static async Task<List<ReferenceUsageDto>> ReadBlockedByAsync(HttpResponseMessage resp)
    {
        try
        {
            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("blockedBy", out var arr) && arr.ValueKind == JsonValueKind.Array)
                return JsonSerializer.Deserialize<List<ReferenceUsageDto>>(arr.GetRawText(), JsonOpts) ?? [];
        }
        catch { /* fallthrough */ }
        return [];
    }

    private static async Task<string> SafeBodyAsync(HttpResponseMessage resp)
    {
        try
        {
            var body = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body)) return $"HTTP {(int)resp.StatusCode}";
            using var doc = JsonDocument.Parse(body);
            var detail = doc.RootElement.TryGetProperty("detail", out var d) ? d.GetString() : null;
            return detail ?? (body.Length > 300 ? body[..300] : body);
        }
        catch { return $"HTTP {(int)resp.StatusCode}"; }
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed class MasterDataFormInfoDto
{
    public int    FormId      { get; set; }
    public string FormCode    { get; set; } = "";
    public int    TableId     { get; set; }
    public string SchemaName  { get; set; } = "dbo";
    public string TableName   { get; set; } = "";
    public string PkColumn    { get; set; } = "";
    public string DisplayMode { get; set; } = "Popup";
    public List<MasterDataColumnDto> Columns { get; set; } = [];
}

public sealed class MasterDataColumnDto
{
    public string ColumnCode { get; set; } = "";
    public string NetType    { get; set; } = "string";
    public string EditorType { get; set; } = "TextBox";
    public string Label      { get; set; } = "";
    public bool   ShowInList { get; set; }
    public bool   IsReadOnly { get; set; }
    public int    OrderNo    { get; set; }
}

public sealed class MasterDataListResultDto
{
    public List<Dictionary<string, object?>> Items { get; set; } = [];
    public int TotalCount { get; set; }
}

public sealed record MasterDataSaveResultDto(
    bool Success, object? Id, List<MasterDataFieldErrorDto> Errors);

public sealed record MasterDataFieldErrorDto(string FieldCode, string Message);

public sealed record MasterDataDeleteResultDto(bool Success, List<ReferenceUsageDto> BlockedBy);

public sealed record ReferenceUsageDto(
    string Schema, string Table, string Column, int RowCount, bool IsLegacy);

public sealed class UsageResponseDto
{
    public bool Used { get; set; }
    public List<ReferenceUsageDto> Usages { get; set; } = [];
}
