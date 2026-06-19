// File    : GridLayoutService.cs
// Module  : ICare247_UI
// Purpose : Lưu/đọc layout lưới per-user. L0 = localStorage (đọc trước → 0 gọi server khi mở lại
//           cùng máy); miss → API server (Data DB qua cache). Ghi: localStorage ngay + PUT server.
//           "Hạn chế tối đa truy vấn DB": lazy theo View, cache localStorage, write-through ở server.

using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace ICare247_UI.Services;

/// <summary>
/// Quản lý layout lưới của user (DxGrid GridPersistentLayout JSON). Khóa localStorage gắn userId
/// để cùng trình duyệt khác user không lẫn nhau. Lỗi mạng → nuốt (log), grid vẫn chạy với mặc định.
/// </summary>
public sealed class GridLayoutService
{
    private const string Platform = "web";

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly AuthenticationStateProvider _auth;
    private readonly ILogger<GridLayoutService> _logger;

    public GridLayoutService(
        HttpClient http, IJSRuntime js, AuthenticationStateProvider auth, ILogger<GridLayoutService> logger)
    {
        _http   = http;
        _js     = js;
        _auth   = auth;
        _logger = logger;
    }

    /// <summary>Đọc layout: localStorage trước; miss → GET server → cache lại localStorage.</summary>
    public async Task<string?> GetAsync(string viewCode)
    {
        var key = await KeyAsync(viewCode);

        var local = await _js.InvokeAsync<string?>("localStorage.getItem", key);
        if (!string.IsNullOrEmpty(local)) return local;

        try
        {
            var resp = await _http.GetFromJsonAsync<LayoutResponse>(
                $"/api/v1/views/{Uri.EscapeDataString(viewCode)}/my-layout?platform={Platform}");
            var json = resp?.LayoutJson;
            if (!string.IsNullOrEmpty(json))
                await _js.InvokeVoidAsync("localStorage.setItem", key, json);
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetLayout lỗi — View={View}", viewCode);
            return null;
        }
    }

    /// <summary>Lưu layout: ghi localStorage ngay + PUT server (caller nên debounce trước khi gọi).</summary>
    public async Task SaveAsync(string viewCode, string layoutJson)
    {
        var key = await KeyAsync(viewCode);
        await _js.InvokeVoidAsync("localStorage.setItem", key, layoutJson);

        try
        {
            await _http.PutAsJsonAsync(
                $"/api/v1/views/{Uri.EscapeDataString(viewCode)}/my-layout?platform={Platform}",
                new { layoutJson });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SaveLayout lỗi — View={View}", viewCode);
        }
    }

    /// <summary>Khôi phục mặc định: xóa localStorage + DELETE server.</summary>
    public async Task ResetAsync(string viewCode)
    {
        var key = await KeyAsync(viewCode);
        await _js.InvokeVoidAsync("localStorage.removeItem", key);

        try
        {
            await _http.DeleteAsync(
                $"/api/v1/views/{Uri.EscapeDataString(viewCode)}/my-layout?platform={Platform}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ResetLayout lỗi — View={View}", viewCode);
        }
    }

    /// <summary>Khóa localStorage: gắn userId để tách layout giữa các user trên cùng trình duyệt.</summary>
    private async Task<string> KeyAsync(string viewCode)
    {
        var state = await _auth.GetAuthenticationStateAsync();
        var uid = state.User.FindFirst("sub")?.Value
                  ?? state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? "anon";
        return $"icare:gridlayout:{uid}:{viewCode}:{Platform}";
    }

    private sealed record LayoutResponse(string? LayoutJson);
}
