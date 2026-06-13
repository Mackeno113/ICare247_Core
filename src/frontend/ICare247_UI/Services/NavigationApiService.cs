// File    : NavigationApiService.cs
// Module  : ICare247_UI
// Purpose : Gọi API /api/v1/me/navigation — cây menu đã lọc theo quyền của user hiện tại.
//           Lỗi/không có dữ liệu → trả rỗng để NavMenu fallback sang AppNav tĩnh.

using System.Net.Http.Json;
using System.Text.Json;
using ICare247_UI.Navigation;
using Microsoft.Extensions.Logging;

namespace ICare247_UI.Services;

/// <summary>Lấy menu động theo quyền (gồm cờ Xem/Thêm/Sửa/Xóa/In từng node).</summary>
public sealed class NavigationApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<NavigationApiService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public NavigationApiService(HttpClient http, ILogger<NavigationApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Lấy node menu user được thấy. Sự kiện theo sau: trả danh sách rỗng nếu lỗi/401/chưa
    /// seed DB → NavMenu tự fallback sang AppNav (menu tĩnh).
    /// </summary>
    public async Task<IReadOnlyList<MeNavNode>> GetNavigationAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<MeNavigationResult>("/api/v1/me/navigation", JsonOpts, ct);
            return result?.Nodes ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không lấy được menu động từ /me/navigation — fallback AppNav.");
            return [];
        }
    }
}
