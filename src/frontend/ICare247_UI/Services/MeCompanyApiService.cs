// File    : MeCompanyApiService.cs
// Module  : ICare247_UI
// Purpose : Gọi API /api/v1/me/companies — công ty user được chọn ở company-switcher (VFILTER-ACTIVE).
//           Lỗi/không có dữ liệu → trả rỗng (switcher hiện "tất cả công ty").

using System.Net.Http.Json;
using System.Text.Json;
using ICare247.UI.Shared.Components.Pickers;
using ICare247.UI.Shared.State;
using Microsoft.Extensions.Logging;

namespace ICare247_UI.Services;

/// <summary>Lấy danh sách công ty user được phép chọn (đổ vào AppState/CompanySwitcher).
/// Kiêm nguồn dữ liệu cho IcCompanyPicker tự nạp (ICompanyPickerSource — spec 31).</summary>
public sealed class MeCompanyApiService : ICompanyPickerSource
{
    private readonly HttpClient _http;
    private readonly ILogger<MeCompanyApiService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public MeCompanyApiService(HttpClient http, ILogger<MeCompanyApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Công ty user được chọn. Lỗi/401 → rỗng (switcher mặc định "tất cả công ty").</summary>
    public async Task<IReadOnlyList<CompanyOption>> GetMyCompaniesAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<CompanyOption>>(
                "/api/v1/me/companies", JsonOpts, ct);
            return result ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không lấy được danh sách công ty từ /me/companies.");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IcPickerItem>> GetCompaniesAsync(CancellationToken ct = default)
    {
        var companies = await GetMyCompaniesAsync(ct);
        return companies.Select(c => new IcPickerItem(c.Id, c.Code, c.Name, c.ParentId, c.CanAccess)).ToList();
    }
}
