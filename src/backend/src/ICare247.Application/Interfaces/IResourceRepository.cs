// File    : IResourceRepository.cs
// Module  : Resource
// Layer   : Application
// Purpose : Repository interface cho Sys_Resource — load resource map theo form + ngôn ngữ.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository đọc <c>Sys_Resource</c> — trả về resource map để ValidationEngine resolve messages.
/// <para>
/// Load toàn bộ key của form (<c>{formCode}.%</c>) + global sys keys (<c>sys.val.%</c>)
/// trong 1 query duy nhất.
/// </para>
/// </summary>
public interface IResourceRepository
{
    /// <summary>
    /// Lấy tất cả resource key/value cho một form theo ngôn ngữ.
    /// Bao gồm cả keys dạng <c>{formCode}.val.*</c> và global <c>sys.val.*</c>.
    /// </summary>
    /// <param name="formCode">Form_Code — dùng làm scope prefix khi filter keys.</param>
    /// <param name="langCode">Ngôn ngữ: 'vi' hoặc 'en'.</param>
    /// <param name="tenantId">Tenant hiện tại.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Dictionary Resource_Key → Resource_Value (OrdinalIgnoreCase).
    /// Trả về rỗng nếu không có resource nào cho form/lang combo này.
    /// </returns>
    Task<IReadOnlyDictionary<string, string>> GetByFormAsync(
        string formCode,
        string langCode,
        int tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Batch-load resource values cho một tập key cụ thể.
    /// Dùng để resolve <c>captionKey</c> trong <c>PopupColumnsJson</c> của LookupBox.
    /// </summary>
    /// <param name="keys">Danh sách resource key cần resolve.</param>
    /// <param name="langCode">Ngôn ngữ: 'vi' hoặc 'en'.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Dictionary Resource_Key → Resource_Value (OrdinalIgnoreCase).
    /// Key không tìm thấy trong Sys_Resource sẽ không xuất hiện trong kết quả.
    /// </returns>
    Task<IReadOnlyDictionary<string, string>> GetByKeysAsync(
        IEnumerable<string> keys,
        string langCode,
        CancellationToken ct = default);

    /// <summary>
    /// Toàn bộ overlay (Resource_Key → Resource_Value) của một ngôn ngữ, lọc tùy chọn theo prefix key
    /// (vd "nav."). Dùng cho LocalizationService gộp bản dịch DB lên trên lớp tĩnh JSON.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetOverlayAsync(
        string langCode,
        string? keyPrefix = null,
        CancellationToken ct = default);

    /// <summary>Bản dịch của 1 key theo MỌI ngôn ngữ (Lang_Code → Resource_Value) — cho màn sửa bản dịch.</summary>
    Task<IReadOnlyDictionary<string, string>> GetByKeyAllLangsAsync(
        string key,
        CancellationToken ct = default);

    /// <summary>Thêm/sửa 1 bản dịch (MERGE theo PK Resource_Key+Lang_Code). Tăng Version, cập nhật Updated_At.</summary>
    Task UpsertAsync(
        string key,
        string langCode,
        string value,
        CancellationToken ct = default);
}
