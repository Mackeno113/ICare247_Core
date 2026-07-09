// File    : DocTemplateApiService.cs
// Module  : ICare247_UI
// Purpose : Gọi API /api/v1/doc-templates/by-code/{code}/render — xuất Word/PDF theo mẫu từ màn lưới.
//           Nhận mã bộ mẫu (Ui_View_Action.Target) + dòng đang chọn (keyParams) → POST lấy bytes →
//           trigger download qua JS (icare.downloadBytes). HttpClient đã tự gắn Bearer + X-Tenant-Id.
//           Spec 28 §7.3 (endpoint by-code); Spec 14 §2.3 (Ui_View_Action Export/Server).

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace ICare247_UI.Services;

/// <summary>Wrap endpoint xuất tài liệu server-side theo mẫu (docx/pdf) cho nút toolbar/row của View.</summary>
public sealed class DocTemplateApiService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly ILogger<DocTemplateApiService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string PdfMime = "application/pdf";

    public DocTemplateApiService(HttpClient http, IJSRuntime js, ILogger<DocTemplateApiService> logger)
    {
        _http = http;
        _js = js;
        _logger = logger;
    }

    /// <summary>
    /// Xuất 1 tài liệu theo mã bộ mẫu + dòng dữ liệu đang chọn (làm keyParams). Trả thông báo lỗi thân thiện
    /// (null nếu thành công). Sự kiện theo sau: tải file về máy qua JS nếu render OK.
    /// </summary>
    /// <param name="templateCode">Mã bộ mẫu (Ui_View_Action.Target).</param>
    /// <param name="format">docx | pdf.</param>
    /// <param name="keyParams">Cột dòng đang chọn — server nhặt theo Doc_Template_Param (Nguon='key').</param>
    public async Task<string?> RenderAsync(
        string templateCode, string format,
        IReadOnlyDictionary<string, object?> keyParams, CancellationToken ct = default)
    {
        var fmt = string.Equals(format, "docx", StringComparison.OrdinalIgnoreCase) ? "docx" : "pdf";
        var url = $"/api/v1/doc-templates/by-code/{Uri.EscapeDataString(templateCode)}/render?format={fmt}";
        try
        {
            using var resp = await _http.PostAsJsonAsync(url, keyParams, JsonOpts, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var detail = await TryReadProblemAsync(resp, ct);
                _logger.LogWarning("Xuất mẫu '{Code}' lỗi {Status}: {Detail}", templateCode, resp.StatusCode, detail);
                return detail ?? $"Xuất tài liệu thất bại (HTTP {(int)resp.StatusCode}).";
            }

            var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
            var fileName = resp.Content.Headers.ContentDisposition?.FileNameStar
                           ?? resp.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                           ?? $"{templateCode}.{fmt}";
            var mime = fmt == "docx" ? DocxMime : PdfMime;
            await _js.InvokeVoidAsync("icare.downloadBytes", ct, fileName, Convert.ToBase64String(bytes), mime);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Xuất mẫu '{Code}' thất bại.", templateCode);
            return "Không xuất được tài liệu: " + ex.Message;
        }
    }

    /// <summary>Đọc ProblemDetails.detail/title từ body lỗi (RFC 7807) nếu có. Null nếu không đọc được.</summary>
    private static async Task<string?> TryReadProblemAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            if (resp.StatusCode == HttpStatusCode.NoContent) return null;
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String)
                return d.GetString();
            if (doc.RootElement.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                return t.GetString();
        }
        catch { /* body không phải JSON — bỏ qua */ }
        return null;
    }
}
