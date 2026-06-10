// File    : ViewApiService.cs
// Module  : RuntimeCheck
// Purpose : Gọi API /api/v1/views — metadata View (Grid/TreeList) + dữ liệu lưới.
//           Mọi lỗi HTTP log ra ILogger (→ browser console F12).

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ICare247.Blazor.RuntimeCheck.Services;

/// <summary>
/// Wrap endpoint cấu hình View: <c>{code}/info</c> (metadata) + <c>{code}/data</c> (dữ liệu).
/// </summary>
public sealed class ViewApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<ViewApiService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ViewApiService(HttpClient http, ILogger<ViewApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Danh sách View (header tóm tắt) cho màn chọn — có filter active + search.</summary>
    public async Task<List<ViewListItemDto>> GetListAsync(
        string lang = "vi", string? search = null, int pageSize = 100, CancellationToken ct = default)
    {
        var url = $"/api/v1/views?lang={Uri.EscapeDataString(lang)}&isActive=true&page=1&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";

        var resp = await _http.GetAsync(url, ct);
        await EnsureOkAsync(resp, "GetList");
        var dto = await resp.Content.ReadFromJsonAsync<ViewListResultDto>(JsonOpts, ct);
        return dto?.Items ?? [];
    }

    /// <summary>
    /// Lấy JSON thô của một endpoint View (info/data) để hỗ trợ debug cấu hình.
    /// Trả về chuỗi JSON đã format đẹp; nếu lỗi trả về thông điệp lỗi (không throw).
    /// </summary>
    /// <param name="viewCode">View_Code.</param>
    /// <param name="kind">"info" (metadata) hoặc "data" (dữ liệu lưới).</param>
    /// <param name="lang">Ngôn ngữ resolve i18n.</param>
    public async Task<string> GetRawJsonAsync(
        string viewCode, string kind = "info", string lang = "vi", CancellationToken ct = default)
    {
        var url = kind.Equals("data", StringComparison.OrdinalIgnoreCase)
            ? $"/api/v1/views/{Uri.EscapeDataString(viewCode)}/data?lang={Uri.EscapeDataString(lang)}&page=1&pageSize=50"
            : $"/api/v1/views/{Uri.EscapeDataString(viewCode)}/info?lang={Uri.EscapeDataString(lang)}";

        try
        {
            var resp = await _http.GetAsync(url, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                return $"HTTP {(int)resp.StatusCode} — {url}\n\n{body}";

            // Re-serialize để format đẹp (indent) cho dễ đọc.
            using var doc = JsonDocument.Parse(body);
            return JsonSerializer.Serialize(doc.RootElement,
                new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRawJson lỗi — {Url}", url);
            return $"Lỗi tải JSON: {ex.Message}";
        }
    }

    /// <summary>Metadata View (header + cột + action) — đã resolve i18n theo lang.</summary>
    public async Task<ViewMetadataDto?> GetInfoAsync(string viewCode, string lang = "vi", CancellationToken ct = default)
    {
        var url = $"/api/v1/views/{Uri.EscapeDataString(viewCode)}/info?lang={Uri.EscapeDataString(lang)}";
        var resp = await _http.GetAsync(url, ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureOkAsync(resp, $"GetInfo {viewCode}");
        return await resp.Content.ReadFromJsonAsync<ViewMetadataDto>(JsonOpts, ct);
    }

    /// <summary>Dữ liệu lưới (search + paging). Trả null nếu View không tồn tại.</summary>
    public async Task<ViewDataResultDto?> GetDataAsync(
        string viewCode, string? search = null, string lang = "vi",
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var url = $"/api/v1/views/{Uri.EscapeDataString(viewCode)}/data?lang={Uri.EscapeDataString(lang)}&page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";

        var resp = await _http.GetAsync(url, ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureOkAsync(resp, $"GetData {viewCode}");

        var dto = await resp.Content.ReadFromJsonAsync<ViewDataResultDto>(JsonOpts, ct)
                  ?? new ViewDataResultDto();

        // Unwrap JsonElement → kiểu CLR thật để DxGrid sort/filter/format đúng kiểu.
        foreach (var row in dto.Items)
            foreach (var key in row.Keys.ToList())
                row[key] = UnwrapJson(row[key]);
        return dto;
    }

    private static object? UnwrapJson(object? value)
    {
        if (value is not JsonElement je) return value;
        return je.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => je.GetString(),
            JsonValueKind.True   => true,
            JsonValueKind.False  => false,
            JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDecimal(),
            _                    => je.ToString()
        };
    }

    private async Task EnsureOkAsync(HttpResponseMessage resp, string ctx)
    {
        if (resp.IsSuccessStatusCode) return;
        var body = await resp.Content.ReadAsStringAsync();
        _logger.LogError("View API lỗi [{Ctx}] {Status}: {Body}", ctx, (int)resp.StatusCode, body);
        throw new HttpRequestException($"{ctx} → {(int)resp.StatusCode}");
    }
}

// ── DTOs (map ViewMetadata backend, camelCase Web JSON) ────────────────────────

public sealed class ViewMetadataDto
{
    public int ViewId { get; set; }
    public string ViewCode { get; set; } = "";
    public string ViewType { get; set; } = "Grid";
    public string TableCode { get; set; } = "";
    public string? Title { get; set; }
    public string? EditFormCode { get; set; }
    public string? ExportFileName { get; set; }

    public int PageSize { get; set; } = 20;
    public bool AllowPaging { get; set; } = true;
    public bool VirtualScroll { get; set; }
    public bool ShowFilterRow { get; set; } = true;
    public bool ShowGroupPanel { get; set; }
    public bool ShowSearchBox { get; set; } = true;
    public bool ShowColumnChooser { get; set; }
    public string SelectionMode { get; set; } = "none";
    public bool AllowAdd { get; set; } = true;
    public bool AllowEdit { get; set; } = true;
    public bool AllowDelete { get; set; } = true;
    public bool AllowExport { get; set; } = true;

    public string? KeyField { get; set; }
    public string? ParentField { get; set; }
    public int? ExpandLevel { get; set; }

    public List<ViewColumnDto> Columns { get; set; } = [];
    public List<ViewActionDto> Actions { get; set; } = [];
}

public sealed class ViewColumnDto
{
    public string FieldName { get; set; } = "";
    public string? Caption { get; set; }
    public string ColumnKind { get; set; } = "Data";
    public string? Width { get; set; }
    public int? MinWidth { get; set; }
    public string? TextAlign { get; set; }
    public string? DisplayFormat { get; set; }
    public string RenderMode { get; set; } = "Text";
    public bool IsVisible { get; set; } = true;
    public int OrderNo { get; set; }
    public bool AllowSort { get; set; } = true;
    public bool AllowFilter { get; set; } = true;
    public bool AllowGroup { get; set; }
    public string? SummaryType { get; set; }
    public bool AllowExport { get; set; } = true;

    /// <summary>Ghim cột: none | left | right (đóng băng khi cuộn ngang).</summary>
    public string? FixedPosition { get; set; }

    /// <summary>Sắp xếp mặc định: asc | desc (null = không sắp).</summary>
    public string? SortOrder { get; set; }

    /// <summary>Thứ tự ưu tiên khi sort nhiều cột (null = không tham gia sort mặc định).</summary>
    public int? SortIndex { get; set; }

    /// <summary>
    /// Conditional formatting JSON — mảng rule <c>{ "when": {field,op,value}, "style": {color,background,fontWeight} }</c>.
    /// Rule đầu khớp được áp style cho ô. (Định dạng đơn giản client-eval; Grammar V1 AST đầy đủ làm sau.)
    /// </summary>
    public string? StyleRuleJson { get; set; }

    /// <summary>Nhãn hiển thị: Caption (i18n) hoặc fallback Field_Name.</summary>
    public string DisplayCaption => string.IsNullOrWhiteSpace(Caption) ? FieldName : Caption!;
}

public sealed class ViewActionDto
{
    public string ActionCode { get; set; } = "";
    public string ActionType { get; set; } = "BuiltIn";
    public string Scope { get; set; } = "Toolbar";
    public string? Label { get; set; }
    public string? Tooltip { get; set; }
    public string? Confirm { get; set; }
    public string? Icon { get; set; }
    public string? ExportFormat { get; set; }
    public string? ExportEngine { get; set; }
    public string? Target { get; set; }
    public bool RequireSelection { get; set; }
    public int OrderNo { get; set; }
}

public sealed class ViewDataResultDto
{
    public List<Dictionary<string, object?>> Items { get; set; } = [];
    public int TotalCount { get; set; }
}

/// <summary>Item tóm tắt trong danh sách View (màn chọn View_Code).</summary>
public sealed class ViewListItemDto
{
    public int ViewId { get; set; }
    public string ViewCode { get; set; } = "";
    public string ViewType { get; set; } = "Grid";
    public string TableCode { get; set; } = "";
    public string? Title { get; set; }
    public string? EditFormCode { get; set; }
    public int ColumnCount { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Wrapper phân trang danh sách View từ API.</summary>
public sealed class ViewListResultDto
{
    public List<ViewListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
