// File    : ImportApiService.cs
// Module  : ICare247_UI
// Purpose : Gọi API /api/v1/views/{code}/import — tải template, validate (preview), commit.
//           Spec 25 §11–§14, ADR-034.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace ICare247_UI.Services;

/// <summary>Wrap endpoint import Excel của View: tải template · validate · commit (multipart upload).</summary>
public sealed class ImportApiService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly ILogger<ImportApiService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public ImportApiService(HttpClient http, IJSRuntime js, ILogger<ImportApiService> logger)
    {
        _http = http;
        _js = js;
        _logger = logger;
    }

    /// <summary>Tải workbook template import → trigger download qua JS (icare.downloadBytes).</summary>
    public async Task DownloadTemplateAsync(string viewCode, string lang = "vi", CancellationToken ct = default)
    {
        var url = $"/api/v1/views/{Uri.EscapeDataString(viewCode)}/import/template?lang={Uri.EscapeDataString(lang)}";
        var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
        var fileName = resp.Content.Headers.ContentDisposition?.FileNameStar
                       ?? resp.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? $"{viewCode}_template.xlsx";
        await _js.InvokeVoidAsync("icare.downloadBytes", ct, fileName, Convert.ToBase64String(bytes), XlsxMime);
    }

    /// <summary>Kiểm tra file (dry-run) → preview NEW/UPDATE/ERROR. KHÔNG ghi DB. <paramref name="mode"/>: upsert|update|insert.</summary>
    public async Task<ImportPreviewDto?> ValidateAsync(
        string viewCode, Stream file, string fileName, string mode = "upsert",
        string lang = "vi", CancellationToken ct = default)
    {
        var url = $"/api/v1/views/{Uri.EscapeDataString(viewCode)}/import/validate" +
                  $"?lang={Uri.EscapeDataString(lang)}&mode={Uri.EscapeDataString(mode)}";
        using var resp = await PostFileAsync(url, file, fileName, ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureOkAsync(resp, "Validate", viewCode);
        return await resp.Content.ReadFromJsonAsync<ImportPreviewDto>(JsonOpts, ct);
    }

    /// <summary>Ghi thật các dòng hợp lệ (partial commit) + log + hook. <paramref name="mode"/>: upsert|update|insert.</summary>
    public async Task<ImportCommitDto?> CommitAsync(
        string viewCode, Stream file, string fileName, string mode = "upsert",
        string lang = "vi", CancellationToken ct = default)
    {
        var url = $"/api/v1/views/{Uri.EscapeDataString(viewCode)}/import/commit" +
                  $"?lang={Uri.EscapeDataString(lang)}&mode={Uri.EscapeDataString(mode)}";
        using var resp = await PostFileAsync(url, file, fileName, ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureOkAsync(resp, "Commit", viewCode);
        return await resp.Content.ReadFromJsonAsync<ImportCommitDto>(JsonOpts, ct);
    }

    /// <summary>POST file dạng multipart/form-data (field name "file").</summary>
    private async Task<HttpResponseMessage> PostFileAsync(
        string url, Stream file, string fileName, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(XlsxMime);
        content.Add(fileContent, "file", fileName);
        return await _http.PostAsync(url, content, ct);
    }

    /// <summary>Bảo đảm 2xx; ném <see cref="HttpRequestException"/> kèm message thân thiện nếu lỗi.</summary>
    private async Task EnsureOkAsync(HttpResponseMessage resp, string ctx, string viewCode)
    {
        if (resp.IsSuccessStatusCode) return;
        var body = await resp.Content.ReadAsStringAsync();
        _logger.LogError("Import API lỗi [{Ctx} {View}] {Status}: {Body}", ctx, viewCode, (int)resp.StatusCode, body);
        throw new HttpRequestException(ExtractMessage(body, (int)resp.StatusCode));
    }

    /// <summary>Bóc "message"/"detail" trong body lỗi; fallback theo mã trạng thái.</summary>
    private static string ExtractMessage(string body, int status)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                return m.GetString() ?? body;
            if (doc.RootElement.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String)
                return d.GetString() ?? body;
        }
        catch { /* không phải JSON */ }
        return status switch
        {
            401 => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
            403 => "Bạn không có quyền thực hiện thao tác này.",
            _ => "Đã xảy ra lỗi khi xử lý tệp import."
        };
    }
}

// ── DTOs (map kết quả import backend, camelCase Web JSON) ───────────────────────

/// <summary>Kết quả preview (dry-run).</summary>
public sealed class ImportPreviewDto
{
    public ImportSummaryDto Summary { get; set; } = new();
    public List<string> FileErrors { get; set; } = [];
    public List<ImportRowDto> Rows { get; set; } = [];
}

/// <summary>Kết quả commit.</summary>
public sealed class ImportCommitDto
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = "";
    public ImportSummaryDto Summary { get; set; } = new();
    public List<string> FileErrors { get; set; } = [];
    public List<ImportRowDto> ErrorRows { get; set; } = [];
}

/// <summary>Thống kê mẻ import.</summary>
public sealed class ImportSummaryDto
{
    public int Total { get; set; }
    public int New { get; set; }
    public int Update { get; set; }
    public int Error { get; set; }
    public int Skipped { get; set; }
}

/// <summary>1 dòng preview/lỗi.</summary>
public sealed class ImportRowDto
{
    public int RowNumber { get; set; }
    public string Operation { get; set; } = "";
    public long? MatchedId { get; set; }
    public List<ImportRowErrorDto> Errors { get; set; } = [];
}

/// <summary>1 lỗi ô (đã dịch i18n).</summary>
public sealed class ImportRowErrorDto
{
    public string? Field { get; set; }
    public string Message { get; set; } = "";
}
