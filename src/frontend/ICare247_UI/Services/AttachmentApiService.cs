// File    : AttachmentApiService.cs
// Module  : ICare247_UI
// Purpose : Gọi API /api/v1/attachments — liệt kê/xóa qua HttpClient (đã gắn Bearer + X-Tenant-Id +
//           X-Active-CongTy), lấy thumbnail dạng data-URL (giữ auth cho <img>), và dựng option cho JS
//           uploader (XHR có progress cần token + URL tuyệt đối).

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ICare247.UI.Shared.Services.Auth;
using ICare247_UI.Models;
using Microsoft.Extensions.Logging;

namespace ICare247_UI.Services;

/// <summary>Wrap endpoint đính kèm tổng quát cho control AttachmentRenderer.</summary>
public sealed class AttachmentApiService
{
    private readonly HttpClient _http;
    private readonly TokenStore _tokens;
    private readonly ApiSettings _settings;
    private readonly ILogger<AttachmentApiService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public AttachmentApiService(
        HttpClient http, TokenStore tokens, ApiSettings settings, ILogger<AttachmentApiService> logger)
    {
        _http = http;
        _tokens = tokens;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>Liệt kê đính kèm của 1 record/field. Trả list rỗng nếu chưa có/ lỗi.</summary>
    public async Task<List<AttachmentInfoDto>> ListAsync(
        string ownerTable, long ownerId, string? fieldMa, CancellationToken ct = default)
    {
        try
        {
            var url = $"/api/v1/attachments?ownerTable={Uri.EscapeDataString(ownerTable)}&ownerId={ownerId}";
            if (!string.IsNullOrWhiteSpace(fieldMa)) url += $"&fieldMa={Uri.EscapeDataString(fieldMa)}";
            var items = await _http.GetFromJsonAsync<List<AttachmentInfoDto>>(url, JsonOpts, ct);
            return items ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Liệt kê đính kèm thất bại ({OwnerTable}#{OwnerId}).", ownerTable, ownerId);
            return [];
        }
    }

    /// <summary>Metadata 1 đính kèm theo Id (chế độ 1-tệp/cột). Null nếu không có/lỗi.</summary>
    public async Task<AttachmentInfoDto?> GetInfoAsync(long id, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync($"/api/v1/attachments/{id}/info", ct);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<AttachmentInfoDto>(JsonOpts, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lấy metadata đính kèm #{Id} thất bại.", id);
            return null;
        }
    }

    /// <summary>Xóa 1 đính kèm. true nếu thành công.</summary>
    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/api/v1/attachments/{id}", ct);
        return resp.IsSuccessStatusCode;
    }

    /// <summary>Gắn loạt đính kèm treo vào record vừa tạo (đa-tệp-khi-thêm-mới). Trả số bản ghi đã gắn.</summary>
    public async Task<int> LinkAsync(
        string ownerTable, long ownerId, string? fieldMa, IReadOnlyList<long> ids, CancellationToken ct = default)
    {
        if (ids is null || ids.Count == 0) return 0;
        try
        {
            var resp = await _http.PostAsJsonAsync("/api/v1/attachments/link",
                new { attachmentIds = ids, ownerTable, ownerId, fieldMa }, JsonOpts, ct);
            if (!resp.IsSuccessStatusCode) return 0;
            var body = await resp.Content.ReadFromJsonAsync<LinkResultDto>(JsonOpts, ct);
            return body?.Linked ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gắn đính kèm vào record {OwnerTable}#{OwnerId} thất bại.", ownerTable, ownerId);
            return 0;
        }
    }

    /// <summary>Lấy thumbnail dưới dạng data-URL (base64) để &lt;img&gt; hiển thị mà vẫn giữ auth. Null nếu không có.</summary>
    public async Task<string?> GetThumbnailDataUrlAsync(long id, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"/api/v1/attachments/{id}/thumbnail", ct);
        if (resp.StatusCode == HttpStatusCode.NotFound || !resp.IsSuccessStatusCode) return null;
        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
        var mime = resp.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }

    /// <summary>URL tải nội dung chính (dùng cho JS fetch kèm token, không nhúng token vào query).</summary>
    public string DownloadUrl(long id) => $"{_settings.BaseUrl.TrimEnd('/')}/api/v1/attachments/{id}";

    /// <summary>Access token hiện tại (cho JS fetch tải về). Rỗng nếu chưa đăng nhập.</summary>
    public string Token => _tokens.AccessToken ?? "";

    /// <summary>Giá trị header X-Tenant-Id (cho JS fetch tải về).</summary>
    public string TenantIdHeader => _settings.TenantId.ToString();

    /// <summary>Dựng option cho JS uploader: URL tuyệt đối + token + tenant (XHR tự set header).</summary>
    public AttachmentUploadOptions BuildUploadOptions(
        string? loai, string? ownerTable, long? ownerId, string? fieldMa,
        int maxDimension, double quality)
        => new()
        {
            Url = $"{_settings.BaseUrl.TrimEnd('/')}/api/v1/attachments",
            Token = _tokens.AccessToken ?? "",
            TenantId = _settings.TenantId.ToString(),
            Loai = loai,
            OwnerTable = ownerTable,
            OwnerId = ownerId,
            FieldMa = fieldMa,
            CompressImages = true,
            MaxDimension = maxDimension,
            Quality = quality,
        };
}

/// <summary>Kết quả gắn đính kèm (POST /link).</summary>
public sealed class LinkResultDto
{
    public int Linked { get; set; }
}

/// <summary>Metadata 1 đính kèm (khớp AttachmentInfo backend, camelCase).</summary>
public sealed class AttachmentInfoDto
{
    public long Id { get; set; }
    public string TenFile { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long KichThuoc { get; set; }
    public bool HasThumbnail { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Option truyền sang JS uploader (XHR + nén ảnh client + progress).</summary>
public sealed class AttachmentUploadOptions
{
    public string Url { get; set; } = "";
    public string Token { get; set; } = "";
    public string TenantId { get; set; } = "";
    public string? Loai { get; set; }
    public string? OwnerTable { get; set; }
    public long? OwnerId { get; set; }
    public string? FieldMa { get; set; }
    public bool CompressImages { get; set; } = true;
    public int MaxDimension { get; set; } = 2000;
    public double Quality { get; set; } = 0.85;
}
