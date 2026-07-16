// File    : AdminUserApiService.cs
// Module  : ICare247_UI
// Purpose : Gọi API /api/v1/admin/users cho màn Người dùng: CRUD tài khoản, đặt lại mật khẩu,
//           gán vai trò, gán công ty truy cập. Lỗi đọc → trả rỗng/null; lỗi ghi → trả thông báo
//           ProblemDetails (RFC 7807) để màn hiển thị.

using System.Net.Http.Json;
using System.Text.Json;
using ICare247_UI.Models;
using Microsoft.Extensions.Logging;

namespace ICare247_UI.Services;

/// <summary>Wrap endpoint quản trị người dùng (administration.users).</summary>
public sealed class AdminUserApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<AdminUserApiService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public AdminUserApiService(HttpClient http, ILogger<AdminUserApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Danh sách người dùng; lỗi → rỗng.</summary>
    public async Task<List<UserListVm>> GetUsersAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<UserListVm>>("/api/v1/admin/users", JsonOpts, ct) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không lấy được danh sách người dùng.");
            return [];
        }
    }

    /// <summary>Chi tiết user + vai trò; lỗi → null.</summary>
    public async Task<UserDetailVm?> GetUserAsync(long id, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<UserDetailVm>($"/api/v1/admin/users/{id}", JsonOpts, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không lấy được chi tiết người dùng {Id}.", id);
            return null;
        }
    }

    /// <summary>Tạo user mới. Trả (Id mới, null) khi thành công; (null, lỗi) khi thất bại.</summary>
    public async Task<(long? Id, string? Error)> CreateAsync(UserDetailVm u, string matKhau, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/admin/users", new
        {
            ma = u.Ma, tenDangNhap = u.TenDangNhap, matKhau,
            trangThai = u.TrangThai, laQuanTri = u.LaQuanTri, kichHoatMobile = u.KichHoatMobile,
            hetHanTaiKhoan = u.HetHanTaiKhoan, doiMatKhauLanSau = u.DoiMatKhauLanSau
        }, JsonOpts, ct);
        if (!resp.IsSuccessStatusCode) return (null, await ReadErrorAsync(resp, ct));

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, ct);
        return (body.GetProperty("id").GetInt64(), null);
    }

    /// <summary>Cập nhật thông tin user. Null = thành công; khác = thông báo lỗi.</summary>
    public async Task<string?> UpdateAsync(UserDetailVm u, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/api/v1/admin/users/{u.Id}", new
        {
            ma = u.Ma, tenDangNhap = u.TenDangNhap,
            trangThai = u.TrangThai, laQuanTri = u.LaQuanTri, kichHoatMobile = u.KichHoatMobile,
            hetHanTaiKhoan = u.HetHanTaiKhoan, doiMatKhauLanSau = u.DoiMatKhauLanSau
        }, JsonOpts, ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    /// <summary>Đặt lại mật khẩu. Null = thành công; khác = thông báo lỗi.</summary>
    public async Task<string?> ResetPasswordAsync(
        long id, string matKhauMoi, bool doiMatKhauLanSau, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/api/v1/admin/users/{id}/password",
            new { matKhauMoi, doiMatKhauLanSau }, JsonOpts, ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    /// <summary>Xóa mềm user. Null = thành công; khác = thông báo lỗi.</summary>
    public async Task<string?> DeleteAsync(long id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/api/v1/admin/users/{id}", ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    /// <summary>Ghi danh sách vai trò của user. Null = thành công; khác = thông báo lỗi.</summary>
    public async Task<string?> SaveRolesAsync(long id, IReadOnlyList<long> vaiTroIds, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/api/v1/admin/users/{id}/roles",
            new { vaiTroIds }, JsonOpts, ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    /// <summary>Cây công ty + trạng thái quyền của user; lỗi → rỗng.</summary>
    public async Task<List<UserCompanyNodeVm>> GetCompaniesAsync(long id, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<UserCompanyNodeVm>>(
                $"/api/v1/admin/users/{id}/companies", JsonOpts, ct) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không lấy được cây công ty của người dùng {Id}.", id);
            return [];
        }
    }

    /// <summary>Ghi tập công ty gán riêng + công ty mặc định. Null = thành công; khác = thông báo lỗi.</summary>
    public async Task<string?> SaveCompaniesAsync(
        long id, IReadOnlyList<long> congTyIds, long? macDinhCongTyId, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/api/v1/admin/users/{id}/companies",
            new { congTyIds, macDinhCongTyId }, JsonOpts, ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    /// <summary>Đọc Detail/Title từ ProblemDetails (RFC 7807) để hiển thị cho admin.</summary>
    private static async Task<string?> ReadErrorAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            var pd = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, ct);
            if (pd.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String)
                return d.GetString();
            if (pd.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                return t.GetString();
        }
        catch { /* body không phải JSON → rơi xuống mã status */ }
        return $"HTTP {(int)resp.StatusCode}";
    }
}
