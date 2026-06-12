// File    : ITenantConnectionResolver.cs
// Module  : MultiTenancy
// Layer   : Application
// Purpose : Phân giải tenant → cặp connection string (Config DB + Data DB) từ Catalog DB.
//           Ẩn chi tiết catalog/giải mã khỏi tầng trên. Xem ADR-018.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Cặp connection string của một tenant (đã giải mã, sẵn sàng mở kết nối).
/// </summary>
/// <param name="TenantId">Khóa tenant trong catalog.</param>
/// <param name="ConfigConnectionString">Chuỗi kết nối Config DB (metadata) của tenant.</param>
/// <param name="DataConnectionString">Chuỗi kết nối Data DB (dữ liệu vận hành) của tenant.</param>
public sealed record TenantConnections(
    int TenantId,
    string ConfigConnectionString,
    string DataConnectionString);

/// <summary>
/// Phân giải tenant ra cặp connection string. Tra Catalog DB (cache in-memory) hoặc
/// fallback về connection string cố định trong config khi chưa cấu hình catalog
/// (giai đoạn 1 tenant / dev — app không vỡ).
/// </summary>
public interface ITenantConnectionResolver
{
    /// <summary>True nếu đã cấu hình Catalog DB (đa tenant); false = chế độ fallback 1 cấu hình cố định.</summary>
    bool IsCatalogConfigured { get; }

    /// <summary>
    /// Phân giải theo Tenant_Id. Sự kiện theo sau: trả cặp connection (từ cache/catalog
    /// hoặc fallback config).
    /// </summary>
    /// <param name="tenantId">Khóa tenant.</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>Cặp connection; ném nếu catalog bật mà không tìm thấy tenant.</returns>
    Task<TenantConnections> ResolveByIdAsync(int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Phân giải theo subdomain (vd 'congtyA'). Dùng ở pha nhận diện tenant trước login.
    /// </summary>
    /// <param name="subdomain">Nhãn subdomain (không gồm domain gốc).</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>Cặp connection, hoặc null nếu không có tenant khớp (catalog bật).</returns>
    Task<TenantConnections?> ResolveBySubdomainAsync(string subdomain, CancellationToken ct = default);
}
