// File    : ViewApiService.cs
// Module  : ICare247_UI
// Purpose : Gọi API /api/v1/views — metadata View (Grid/TreeList) + dữ liệu lưới.
//           Mọi lỗi HTTP log ra ILogger (→ browser console F12).

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ICare247.UI.Shared.Services.Http;
using ICare247.UI.Shared.Services.I18n;
using Microsoft.Extensions.Logging;

namespace ICare247_UI.Services;

/// <summary>
/// Wrap endpoint cấu hình View: <c>{code}/info</c> (metadata) + <c>{code}/data</c> (dữ liệu).
/// </summary>
public sealed class ViewApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<ViewApiService> _logger;
    private readonly LocalizationService _loc;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ViewApiService(HttpClient http, ILogger<ViewApiService> logger, LocalizationService loc)
    {
        _http = http;
        _logger = logger;
        _loc = loc;
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
        await EnsureOkAsync(resp, "GetInfo", viewCode);
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
        await EnsureOkAsync(resp, "GetData", viewCode);

        var dto = await resp.Content.ReadFromJsonAsync<ViewDataResultDto>(JsonOpts, ct)
                  ?? new ViewDataResultDto();

        // Unwrap JsonElement → kiểu CLR thật để DxGrid sort/filter/format đúng kiểu.
        foreach (var row in dto.Items)
            foreach (var key in row.Keys.ToList())
                row[key] = UnwrapJson(row[key]);
        return dto;
    }

    /// <summary>
    /// Thực thi lưới nâng cao (Source_Type='Sp'/'Sql') — POST giá trị panel lọc (key=Filter_Code).
    /// Trả null nếu View không tồn tại; ném <see cref="HttpRequestException"/> kèm message khi 400 (tham số sai).
    /// </summary>
    public async Task<ViewDataResultDto?> SearchAsync(
        string viewCode, IReadOnlyDictionary<string, string?> filters, string lang = "vi",
        CancellationToken ct = default)
    {
        var url = $"/api/v1/views/{Uri.EscapeDataString(viewCode)}/search?lang={Uri.EscapeDataString(lang)}";
        var resp = await _http.PostAsJsonAsync(url, new { filters }, JsonOpts, ct);

        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        if (resp.StatusCode == HttpStatusCode.BadRequest)
        {
            // Tham số bắt buộc thiếu/sai → đọc message để hiển thị cho người dùng.
            var problem = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Search 400 [{Code}]: {Body}", viewCode, problem);
            throw new HttpRequestException(ExtractMessage(problem));
        }
        await EnsureOkAsync(resp, "Search", viewCode);

        var dto = await resp.Content.ReadFromJsonAsync<ViewDataResultDto>(JsonOpts, ct)
                  ?? new ViewDataResultDto();
        foreach (var row in dto.Items)
            foreach (var key in row.Keys.ToList())
                row[key] = UnwrapJson(row[key]);
        return dto;
    }

    /// <summary>
    /// Kéo-thả sắp xếp 1 node trong TreeList (ADR-027). Trả <c>(true, null)</c> khi thành công;
    /// <c>(false, message)</c> khi bị chặn (vd tạo vòng lặp) — KHÔNG ném, để caller tự hiện thông báo.
    /// </summary>
    public async Task<(bool Success, string? Error)> ReorderAsync(
        string viewCode, long id, long? newParentId, long? targetId, string dropPosition,
        CancellationToken ct = default)
    {
        var url = $"/api/v1/views/{Uri.EscapeDataString(viewCode)}/reorder";
        var resp = await _http.PostAsJsonAsync(url,
            new { id, newParentId, targetId, dropPosition }, JsonOpts, ct);

        if (resp.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await resp.Content.ReadAsStringAsync(ct);
            return (false, ExtractMessage(problem));
        }
        if (resp.StatusCode == HttpStatusCode.NotFound)
            return (false, "View không tồn tại.");

        await EnsureOkAsync(resp, "Reorder", viewCode);
        return (true, null);
    }

    /// <summary>
    /// Nạp options cho 1 control lọc cascade (Combo/MultiSelect/Radio). <paramref name="parents"/> = giá trị
    /// filter cha hiện tại (key = Filter_Code). Trả rỗng nếu lỗi/không có nguồn (không ném — UX không vỡ).
    /// </summary>
    public async Task<List<FilterOptionDto>> GetFilterOptionsAsync(
        string viewCode, string filterCode, IReadOnlyDictionary<string, string?> parents,
        string lang = "vi", CancellationToken ct = default)
    {
        var url = $"/api/v1/views/{Uri.EscapeDataString(viewCode)}/filter-options/{Uri.EscapeDataString(filterCode)}"
                  + $"?lang={Uri.EscapeDataString(lang)}";
        try
        {
            var resp = await _http.PostAsJsonAsync(url, new { parents }, JsonOpts, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("FilterOptions {Status} [{View}/{Filter}]", (int)resp.StatusCode, viewCode, filterCode);
                return [];
            }
            return await resp.Content.ReadFromJsonAsync<List<FilterOptionDto>>(JsonOpts, ct) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FilterOptions lỗi [{View}/{Filter}]", viewCode, filterCode);
            return [];
        }
    }

    /// <summary>Bóc trường "message" trong body lỗi JSON; fallback nguyên văn nếu không parse được.</summary>
    private static string ExtractMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                return m.GetString() ?? body;
        }
        catch { /* không phải JSON → trả nguyên văn */ }
        return body;
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

    /// <summary>
    /// Bảo đảm 2xx; nếu không, log chi tiết kỹ thuật (console) rồi ném <see cref="HttpRequestException"/>
    /// với thông điệp ĐÃ i18n + nêu rõ nguyên nhân cho người dùng (không lòi mã endpoint/HTTP thô).
    /// </summary>
    /// <param name="resp">Phản hồi HTTP.</param>
    /// <param name="ctx">Ngữ cảnh kỹ thuật cho log (vd "GetInfo").</param>
    /// <param name="target">Mã/đối tượng để chèn vào thông điệp (vd View_Code) — null nếu không có.</param>
    private async Task EnsureOkAsync(HttpResponseMessage resp, string ctx, string? target = null)
    {
        if (resp.IsSuccessStatusCode) return;
        var body = await resp.Content.ReadAsStringAsync();
        _logger.LogError("View API lỗi [{Ctx} {Target}] {Status}: {Body}", ctx, target, (int)resp.StatusCode, body);
        // Nối "Mã lỗi" (correlationId) vào thông điệp i18n để truy log server theo mã.
        throw new HttpRequestException(
            ApiErrorHelper.WithErrorCodeFromBody(BuildFriendlyMessage(resp.StatusCode, body, target), body));
    }

    /// <summary>
    /// Dựng thông điệp lỗi thân thiện + i18n theo mã trạng thái. Nêu rõ nguyên nhân:
    /// 401 = chưa/đã hết phiên · 403 = thiếu quyền xem màn · 404 = không tồn tại · timeout · 5xx.
    /// Mã khác → ưu tiên trường <c>detail</c> của ProblemDetails từ server, fallback thông điệp chung.
    /// </summary>
    private string BuildFriendlyMessage(HttpStatusCode status, string body, string? target)
    {
        // Đối tượng đưa vào câu (vd " 'Grid_DM_TinhThanhPho'") — rỗng nếu không có.
        var label = string.IsNullOrWhiteSpace(target) ? "" : $" '{target}'";
        return (int)status switch
        {
            401 => _loc.L("api.error.unauthorized",
                "Phiên đăng nhập đã hết hạn hoặc bạn chưa đăng nhập. Vui lòng đăng nhập lại."),
            403 => _loc.L("api.error.forbidden.view",
                "Bạn không có quyền xem màn hình{0}. Vui lòng liên hệ quản trị viên để được cấp quyền.", label),
            404 => _loc.L("api.error.notfound.view",
                "Không tìm thấy màn hình{0} hoặc màn đã bị ẩn.", label),
            408 or 504 => _loc.L("api.error.timeout",
                "Máy chủ phản hồi quá lâu. Vui lòng kiểm tra kết nối rồi thử lại."),
            >= 500 => _loc.L("api.error.server",
                "Máy chủ đang gặp sự cố. Vui lòng thử lại sau hoặc liên hệ quản trị viên."),
            _ => ProblemDetail(body)
                 ?? _loc.L("api.error.generic", "Đã xảy ra lỗi khi tải dữ liệu (mã {0}).", (int)status)
        };
    }

    /// <summary>Bóc trường "detail"/"title" trong ProblemDetails (RFC 7807); null nếu không phải JSON hợp lệ.</summary>
    private static string? ProblemDetail(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String)
                return d.GetString();
            if (doc.RootElement.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                return t.GetString();
        }
        catch { /* không phải JSON → để caller dùng fallback */ }
        return null;
    }
}

// ── DTOs (map ViewMetadata backend, camelCase Web JSON) ────────────────────────

public sealed class ViewMetadataDto
{
    public int ViewId { get; set; }
    public string ViewCode { get; set; } = "";
    public string ViewType { get; set; } = "Grid";
    public string TableCode { get; set; } = "";
    public string SourceType { get; set; } = "Table";
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
    public bool AllowReorder { get; set; }

    // ── Panel lọc trái (lưới nâng cao) ────────────────────────
    public bool FilterPanelEnabled { get; set; }
    public string FilterPanelPosition { get; set; } = "left";
    public bool FilterCollapsible { get; set; } = true;
    public bool AutoSearchOnLoad { get; set; }
    public string? SearchLabel { get; set; }
    public string? ResetLabel { get; set; }

    public List<ViewColumnDto> Columns { get; set; } = [];
    public List<ViewActionDto> Actions { get; set; } = [];
    public List<ViewFilterDto> Filters { get; set; } = [];

    /// <summary>Nguồn SP/SQL (cho phép panel lọc tham số).</summary>
    public bool IsQuerySource =>
        SourceType.Equals("Sp", StringComparison.OrdinalIgnoreCase)
        || SourceType.Equals("Sql", StringComparison.OrdinalIgnoreCase);

    /// <summary>Panel lọc trái thực sự hiển thị: bật + nguồn query + có ≥1 control.</summary>
    public bool HasFilterPanel => FilterPanelEnabled && IsQuerySource && Filters.Count > 0;
}

/// <summary>Một control lọc trên panel trái (Ui_View_Filter) — text i18n đã resolve.</summary>
public sealed class ViewFilterDto
{
    public string FilterCode { get; set; } = "";
    public string ControlType { get; set; } = "Text";
    public string? Label { get; set; }
    public string? Placeholder { get; set; }
    public string? Tooltip { get; set; }
    public string ParamName { get; set; } = "";
    public string ParamType { get; set; } = "string";
    public string Operator { get; set; } = "=";
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public bool IsVisible { get; set; } = true;
    public int OrderNo { get; set; }
    public byte ColSpan { get; set; } = 1;
    public string? LookupSource { get; set; }
    public string? LookupCode { get; set; }

    // ── Cascade + prefill (ADR-030) ───────────────────────────
    /// <summary>CSV Filter_Code cha (cascade) — cha đổi → nạp lại options control này. NULL = độc lập.</summary>
    public string? DependsOn { get; set; }

    /// <summary>Field_Code trên form Thêm/Sửa nhận giá trị filter khi Thêm mới (prefill). NULL = không.</summary>
    public string? DefaultToField { get; set; }

    /// <summary>Prefill: true = khóa (read-only) · false = cho sửa lại.</summary>
    public bool DefaultLock { get; set; }

    /// <summary>Nhãn hiển thị: Label (i18n) hoặc fallback Filter_Code.</summary>
    public string DisplayLabel => string.IsNullOrWhiteSpace(Label) ? FilterCode : Label!;

    /// <summary>Control có nạp options (Combo/MultiSelect/Radio + có Lookup_Source).</summary>
    public bool HasOptions =>
        !string.IsNullOrWhiteSpace(LookupSource)
        && (ControlType.Equals("Combo", StringComparison.OrdinalIgnoreCase)
            || ControlType.Equals("MultiSelect", StringComparison.OrdinalIgnoreCase)
            || ControlType.Equals("Radio", StringComparison.OrdinalIgnoreCase));

    /// <summary>Các Filter_Code cha tách từ DependsOn (CSV) — rỗng nếu độc lập.</summary>
    public IReadOnlyList<string> ParentFilterCodes =>
        string.IsNullOrWhiteSpace(DependsOn)
            ? []
            : DependsOn.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>Một option của control lọc cascade (value gửi lên + nhãn hiển thị).</summary>
public sealed class FilterOptionDto
{
    public string Value { get; set; } = "";
    public string Display { get; set; } = "";
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
