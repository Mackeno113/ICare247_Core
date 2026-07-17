// File    : PickerApiService.cs
// Module  : ICare247_UI
// Purpose : Gọi Picker API dùng chung (/api/v1/pickers — spec 31 §3). Cài IDiaBanPickerSource
//           cho IcAddressBlock; nguồn picker mới (nhân viên…) thêm interface + method tại đây.
//           Lỗi đọc → trả rỗng/null (control hiện "Không có kết quả", không vỡ form).

using System.Net.Http.Json;
using System.Text.Json;
using ICare247.UI.Shared.Components.Pickers;
using Microsoft.Extensions.Logging;

namespace ICare247_UI.Services;

/// <summary>Nguồn dữ liệu địa bàn cho IcAddressBlock (Tỉnh/Thành + Xã/Phường).</summary>
public sealed class PickerApiService : IDiaBanPickerSource
{
    private readonly HttpClient _http;
    private readonly ILogger<PickerApiService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // Tỉnh/Thành gần như bất biến trong phiên → cache L0 (spec 31 §4.4), gọi server 1 lần.
    private IReadOnlyList<IcPickerItem>? _tinhCache;

    public PickerApiService(HttpClient http, ILogger<PickerApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IcPickerItem>> GetTinhThanhAsync(CancellationToken ct = default)
    {
        if (_tinhCache is not null) return _tinhCache;
        var rows = await GetAsync("/api/v1/pickers/dia-ban", ct);
        if (rows.Count > 0) _tinhCache = rows; // lỗi/rỗng thì không cache — lần sau thử lại
        return rows;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IcPickerItem>> SearchPhuongXaAsync(
        long tinhThanhPhoId, string? keyword, CancellationToken ct = default)
        => GetAsync($"/api/v1/pickers/dia-ban?parentId={tinhThanhPhoId}" +
                    (string.IsNullOrWhiteSpace(keyword) ? "" : $"&keyword={Uri.EscapeDataString(keyword)}"), ct);

    /// <inheritdoc />
    public async Task<IcPickerItem?> GetPhuongXaAsync(long id, CancellationToken ct = default)
    {
        var rows = await GetAsync($"/api/v1/pickers/dia-ban?id={id}", ct);
        return rows.Count > 0 ? rows[0] : null;
    }

    /// <summary>Gọi 1 URL picker, map {id, ma, ten, parentId} → IcPickerItem; lỗi → rỗng.</summary>
    private async Task<IReadOnlyList<IcPickerItem>> GetAsync(string url, CancellationToken ct)
    {
        try
        {
            var rows = await _http.GetFromJsonAsync<List<PickerRow>>(url, JsonOpts, ct) ?? [];
            return rows.Select(r => new IcPickerItem(r.Id, r.Ma, r.Ten, r.ParentId)).ToList();
        }
        catch (OperationCanceledException) { throw; } // debounce hủy — để component bỏ lượt này
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không lấy được dữ liệu picker từ {Url}.", url);
            return [];
        }
    }

    /// <summary>Row JSON từ Picker API (PickerItemDto phía server).</summary>
    private sealed record PickerRow(long Id, string? Ma, string Ten, long? ParentId);
}
