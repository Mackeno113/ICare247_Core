// File    : IUserGridLayoutStore.cs
// Module  : UserPreference
// Layer   : Application
// Purpose : Facade cache-aside cho layout lưới per-user (L1+L2 trên repo Data DB).
//           tenantId chỉ dùng để tách key cache (cache server dùng chung nhiều tenant);
//           repo Data DB không cần tenant (db-per-tenant).

namespace ICare247.Application.Interfaces;

/// <summary>
/// Đọc/ghi layout lưới per-user qua cache (L1 Memory + L2 Redis) — single-writer nên write-through,
/// không cần invalidate chéo. Key cache RIÊNG, tách khỏi <see cref="IConfigCache"/> (config).
/// </summary>
public interface IUserGridLayoutStore
{
    /// <summary>Lấy layout JSON (cache → DB); <c>null</c> nếu chưa có.</summary>
    Task<string?> GetAsync(long userId, string viewCode, string platform, int tenantId, CancellationToken ct = default);

    /// <summary>Lưu layout: UPSERT DB + cập nhật cache (write-through).</summary>
    Task SaveAsync(long userId, string viewCode, string platform, int tenantId, string layoutJson, CancellationToken ct = default);

    /// <summary>Khôi phục mặc định: xóa DB + xóa cache.</summary>
    Task ResetAsync(long userId, string viewCode, string platform, int tenantId, CancellationToken ct = default);
}
