// File    : MenuAdminApiService.cs
// Module  : ICare247_UI
// Purpose : Gọi API /api/v1/admin/menu/* cho màn Menu Builder: đọc cây + thêm/sửa/xóa node menu.
//           Picker View đọc Config DB qua ViewApiService (không thuộc service này).

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ICare247_UI.Services;

/// <summary>Wrap endpoint cấu hình cây menu (HT_ChucNang).</summary>
public sealed class MenuAdminApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<MenuAdminApiService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public MenuAdminApiService(HttpClient http, ILogger<MenuAdminApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Toàn bộ cây menu; lỗi → rỗng.</summary>
    public async Task<List<MenuNodeVm>> GetTreeAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<MenuNodeVm>>(
                "/api/v1/admin/menu/tree", JsonOpts, ct) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không lấy được cây menu.");
            return new();
        }
    }

    /// <summary>Thêm node. Trả (Ok, Error).</summary>
    public Task<MenuSaveResult> CreateAsync(MenuNodePayload body, CancellationToken ct = default)
        => SendAsync(HttpMethod.Post, "/api/v1/admin/menu", body, ct);

    /// <summary>Sửa node. Trả (Ok, Error).</summary>
    public Task<MenuSaveResult> UpdateAsync(long id, MenuNodePayload body, CancellationToken ct = default)
        => SendAsync(HttpMethod.Put, $"/api/v1/admin/menu/{id}", body, ct);

    /// <summary>Xóa node. Trả (Ok, Error) — server chặn nếu node hệ thống / còn con.</summary>
    public Task<MenuSaveResult> DeleteAsync(long id, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"/api/v1/admin/menu/{id}", null, ct);

    private async Task<MenuSaveResult> SendAsync(
        HttpMethod method, string url, MenuNodePayload? body, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(method, url);
            if (body is not null) req.Content = JsonContent.Create(body, options: JsonOpts);
            using var resp = await _http.SendAsync(req, ct);

            if (resp.IsSuccessStatusCode) return MenuSaveResult.Success;

            var detail = await ReadErrorAsync(resp, ct);
            return MenuSaveResult.Fail(detail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi gọi {Method} {Url}.", method, url);
            return MenuSaveResult.Fail(ex.Message);
        }
    }

    /// <summary>Trích thông điệp lỗi từ ProblemDetails (detail/title) hoặc body thô.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            var pd = await resp.Content.ReadFromJsonAsync<ProblemDetailsLite>(JsonOpts, ct);
            var msg = pd?.Detail ?? pd?.Title;
            if (!string.IsNullOrWhiteSpace(msg)) return msg!;
        }
        catch { /* không phải ProblemDetails */ }
        return $"Lỗi máy chủ ({(int)resp.StatusCode}).";
    }

    private sealed class ProblemDetailsLite
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
    }
}

/// <summary>Kết quả thao tác ghi menu.</summary>
public sealed record MenuSaveResult(bool Ok, string? Error)
{
    public static readonly MenuSaveResult Success = new(true, null);
    public static MenuSaveResult Fail(string error) => new(false, error);
}

/// <summary>Node menu trả về từ API (đọc).</summary>
public sealed class MenuNodeVm
{
    public long Id { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public long? ChaId { get; set; }
    public string Loai { get; set; } = "Menu";
    public string? Module { get; set; }
    public string? DuongDan { get; set; }
    public string? Icon { get; set; }
    public int ThuTu { get; set; }
    public bool KichHoat { get; set; } = true;
    public string? DoiTuong { get; set; }
    public string? LoaiDoiTuong { get; set; }
    public bool LaHeThong { get; set; }
}

/// <summary>Body thêm/sửa node menu gửi lên API.</summary>
public sealed class MenuNodePayload
{
    public string NodeKind { get; set; } = "Group";
    public string Ten { get; set; } = "";
    public long? ParentId { get; set; }
    public string? ObjectCode { get; set; }
    public string? Module { get; set; }
    public string? Icon { get; set; }
    public int ThuTu { get; set; }
    public bool KichHoat { get; set; } = true;
}
