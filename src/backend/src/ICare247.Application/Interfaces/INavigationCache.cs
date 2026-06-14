// File    : INavigationCache.cs
// Module  : Navigation
// Layer   : Application
// Purpose : Cache cây menu /me/navigation theo (tenant, user) + invalidate cả tenant khi đổi quyền/menu.

using ICare247.Application.Features.Navigation;

namespace ICare247.Application.Interfaces;

/// <summary>Cache menu user (config đổi hiếm). Invalidate theo tenant khi sửa HT_VaiTro_Quyen/HT_ChucNang.</summary>
public interface INavigationCache
{
    /// <summary>Lấy từ cache; thiếu → gọi <paramref name="load"/> rồi cache (gắn token theo tenant).</summary>
    Task<MeNavigationDto> GetOrLoadAsync(int tenantId, long userId, Func<Task<MeNavigationDto>> load);

    /// <summary>Xóa toàn bộ cache menu của 1 tenant (gọi sau khi lưu phân quyền).</summary>
    void InvalidateTenant(int tenantId);
}
