// File    : CacheAdminApiService.cs
// Module  : ICare247_UI
// Purpose : Gọi các endpoint xóa cache config phía server (View/Form/Lookup) + bust cache lookup
//           phía client. Dùng cho nút "Xóa cache" tại màn và màn quản trị Xóa cache.

using Microsoft.Extensions.Logging;

namespace ICare247_UI.Services;

/// <summary>Loại đối tượng cache có thể xóa.</summary>
public enum CacheTargetKind { View, Form, Lookup }

/// <summary>
/// Xóa cache config qua API server (đã có sẵn các endpoint <c>{code}/invalidate-cache</c>) và
/// đồng thời bust cache lookup phía client (<see cref="LookupApiService"/>) để combobox nạp lại.
/// </summary>
public sealed class CacheAdminApiService
{
    private readonly HttpClient _http;
    private readonly LookupApiService _lookup;
    private readonly ILogger<CacheAdminApiService> _logger;

    public CacheAdminApiService(HttpClient http, LookupApiService lookup, ILogger<CacheAdminApiService> logger)
    {
        _http = http;
        _lookup = lookup;
        _logger = logger;
    }

    /// <summary>Xóa cache metadata 1 màn View (server). Trả (Ok, Error).</summary>
    public Task<CacheClearResult> InvalidateViewAsync(string code, CancellationToken ct = default)
        => PostAsync(CacheTargetKind.View, $"/api/v1/views/{Uri.EscapeDataString(code)}/invalidate-cache", ct);

    /// <summary>Xóa cache metadata 1 form (server). Trả (Ok, Error).</summary>
    public Task<CacheClearResult> InvalidateFormAsync(string code, CancellationToken ct = default)
        => PostAsync(CacheTargetKind.Form, $"/api/v1/forms/{Uri.EscapeDataString(code)}/invalidate-cache", ct);

    /// <summary>Xóa cache options 1 lookup: server + client (combobox tĩnh nạp lại). Trả (Ok, Error).</summary>
    public async Task<CacheClearResult> InvalidateLookupAsync(string code, CancellationToken ct = default)
    {
        var res = await PostAsync(CacheTargetKind.Lookup,
            $"/api/v1/lookups/{Uri.EscapeDataString(code)}/invalidate-cache", ct);
        _lookup.Invalidate(code); // bust client dù server lỗi — tránh client giữ bản cũ
        return res;
    }

    /// <summary>Xóa toàn bộ cache lookup phía client (không gọi server).</summary>
    public void ClearClientLookupCache() => _lookup.Clear();

    private async Task<CacheClearResult> PostAsync(CacheTargetKind kind, string url, CancellationToken ct)
    {
        try
        {
            using var resp = await _http.PostAsync(url, content: null, ct);
            if (resp.IsSuccessStatusCode) return CacheClearResult.Success;
            return CacheClearResult.Fail($"Lỗi máy chủ ({(int)resp.StatusCode}).");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Xóa cache {Kind} lỗi: {Url}", kind, url);
            return CacheClearResult.Fail(ex.Message);
        }
    }
}

/// <summary>Kết quả thao tác xóa cache.</summary>
public sealed record CacheClearResult(bool Ok, string? Error)
{
    public static readonly CacheClearResult Success = new(true, null);
    public static CacheClearResult Fail(string error) => new(false, error);
}
