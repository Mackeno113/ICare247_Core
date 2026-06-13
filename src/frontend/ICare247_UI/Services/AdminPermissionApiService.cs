// File    : AdminPermissionApiService.cs
// Module  : ICare247_UI
// Purpose : Gọi API /api/v1/admin/* cho màn Phân quyền: danh sách vai trò + đọc/lưu ma trận quyền.

using System.Net.Http.Json;
using System.Text.Json;
using ICare247_UI.Models;
using Microsoft.Extensions.Logging;

namespace ICare247_UI.Services;

/// <summary>Wrap endpoint cấu hình phân quyền theo vai trò.</summary>
public sealed class AdminPermissionApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<AdminPermissionApiService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public AdminPermissionApiService(HttpClient http, ILogger<AdminPermissionApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Danh sách vai trò; lỗi → rỗng.</summary>
    public async Task<List<RoleVm>> GetRolesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<RoleVm>>("/api/v1/admin/roles", JsonOpts, ct) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không lấy được danh sách vai trò.");
            return new();
        }
    }

    /// <summary>Cây chức năng + cờ quyền của vai trò; lỗi → rỗng.</summary>
    public async Task<List<PermNode>> GetPermissionsAsync(long roleId, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<PermNode>>(
                $"/api/v1/admin/roles/{roleId}/permissions", JsonOpts, ct) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không lấy được quyền vai trò {RoleId}.", roleId);
            return new();
        }
    }

    /// <summary>Lưu (upsert) quyền vai trò. Sự kiện theo sau: server ghi HT_VaiTro_Quyen.</summary>
    public async Task<bool> SaveAsync(long roleId, IReadOnlyList<PermNode> nodes, CancellationToken ct = default)
    {
        var body = new
        {
            items = nodes.Select(n => new
            {
                chucNangId = n.Id,
                xem = n.Xem, them = n.Them, sua = n.Sua, xoa = n.Xoa, inAn = n.InAn
            })
        };
        var resp = await _http.PutAsJsonAsync($"/api/v1/admin/roles/{roleId}/permissions", body, JsonOpts, ct);
        return resp.IsSuccessStatusCode;
    }
}
