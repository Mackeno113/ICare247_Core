// File    : CacheSettings.cs
// Module  : Caching
// Layer   : Infrastructure
// Purpose : Tùy chọn cache đọc từ section "Cache" của appsettings — cờ bật/tắt cache toàn cục.

namespace ICare247.Infrastructure.Caching;

/// <summary>
/// Tùy chọn cache (section <c>"Cache"</c> trong appsettings).
/// </summary>
public sealed class CacheSettings
{
    /// <summary>
    /// Bật/tắt TOÀN BỘ cache (config qua HybridCacheService: form/view/lookup/resource + menu qua
    /// NavigationCache). Mặc định <c>true</c>. Đặt <c>false</c> (thường ở appsettings.Development.json)
    /// khi đang chỉnh cấu hình để mọi thay đổi hiện ngay, khỏi phải xóa cache thủ công.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
