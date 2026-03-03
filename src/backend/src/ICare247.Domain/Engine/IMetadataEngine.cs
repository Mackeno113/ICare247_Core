// File    : IMetadataEngine.cs
// Module  : Engine
// Layer   : Domain
// Purpose : Interface load và cache metadata form — L1 Memory + L2 Redis.

using ICare247.Domain.Entities.Form;

namespace ICare247.Domain.Engine;

/// <summary>
/// Engine load metadata form đầy đủ (form + sections + fields) với hybrid cache.
/// <para>Cache strategy: L1 MemoryCache (5 phút) → L2 Redis (30 phút) → DB.</para>
/// </summary>
public interface IMetadataEngine
{
    /// <summary>
    /// Load metadata đầy đủ của form — bao gồm sections và fields.
    /// Trả về từ cache nếu hit; load DB và cache lại nếu miss.
    /// </summary>
    /// <param name="formCode">Ui_Form.Form_Code — unique trong tenant.</param>
    /// <param name="langCode">Mã ngôn ngữ để load label đã localize, VD: 'vi', 'en'.</param>
    /// <param name="platform">Nền tảng: 'web' hoặc 'mobile'.</param>
    /// <param name="tenantId">Tenant — bắt buộc, dùng trong mọi query và cache key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="FormMetadata"/> nếu tìm thấy và active;
    /// <c>null</c> nếu không tồn tại hoặc Is_Active = 0.
    /// </returns>
    Task<FormMetadata?> GetFormMetadataAsync(
        string formCode,
        string langCode,
        string platform,
        int tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Xóa cache của form — gọi sau khi admin cập nhật metadata.
    /// Xóa cả L1 Memory lẫn L2 Redis.
    /// </summary>
    /// <param name="formCode">Ui_Form.Form_Code cần invalidate.</param>
    /// <param name="tenantId">Tenant sở hữu form.</param>
    Task InvalidateFormCacheAsync(string formCode, int tenantId);
}
