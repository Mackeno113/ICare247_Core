// File    : IConfigCache.cs
// Module  : Config
// Layer   : Application
// Purpose : Facade DUY NHẤT đọc mọi config (metadata, i18n, lookup, permission) qua cache L1+L2.

using ICare247.Domain.Entities.Form;
using ICare247.Domain.Entities.Lookup;
using ICare247.Domain.Entities.Permission;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Facade đọc <b>config</b> (đổi hiếm) qua cache cache-aside L1(Memory)+L2(Redis) — xem ADR-014.
/// <para>
/// Nguyên tắc cứng: Web/Handler <b>CẤM</b> inject repository config trực tiếp
/// (<c>IResourceRepository</c>, <c>IMetadataEngine</c>, lookup/permission repo) — chỉ đi qua facade này.
/// Repo config chỉ được implementation của facade gọi.
/// </para>
/// <para>
/// Phân biệt: <b>Config</b> (metadata form, i18n, lookup options, permission) → cache;
/// <b>Data</b> (bản ghi nghiệp vụ) → KHÔNG cache (đọc thẳng repository data).
/// </para>
/// </summary>
/// <remarks>
/// Mọi cache key gắn <c>{tenant}</c> (+ <c>{lang}</c> nếu i18n) (+ slot <c>:v{n}</c> version-stamp ready).
/// Invalidation: version-stamp (scale-out) + event-remove (bổ trợ) + TTL (lưới an toàn).
/// Implementation (CC-0b) bọc <c>IMetadataEngine</c> + các repo config, thêm stampede lock + negative cache.
/// </remarks>
public interface IConfigCache
{
    /// <summary>
    /// Lấy metadata đầy đủ của form (form + tabs + sections + fields), đã localize theo ngôn ngữ.
    /// <para>Cache hit → trả ngay; miss → load DB qua engine, cache lại rồi trả.</para>
    /// </summary>
    /// <param name="formCode">Ui_Form.Form_Code — unique trong tenant.</param>
    /// <param name="langCode">Mã ngôn ngữ để resolve label, VD 'vi', 'en'.</param>
    /// <param name="platform">Nền tảng: 'web' hoặc 'mobile'.</param>
    /// <param name="tenantId">Tenant — bắt buộc, vào cache key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="FormMetadata"/> nếu tồn tại và active; <c>null</c> nếu không.</returns>
    Task<FormMetadata?> GetFormMetadataAsync(
        string formCode,
        string langCode,
        string platform,
        int tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy toàn bộ resource map (key → text) cho một scope theo ngôn ngữ.
    /// Dùng khi cần resolve nhiều key một lúc (label, validation, message của một form).
    /// </summary>
    /// <param name="scope">
    /// Phạm vi key: thường là <c>Form_Code</c> (gồm cả <c>{scope}.*</c> + global <c>sys.*</c>).
    /// </param>
    /// <param name="langCode">Ngôn ngữ: 'vi' hoặc 'en'.</param>
    /// <param name="tenantId">Tenant hiện tại.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary key → value (OrdinalIgnoreCase); rỗng nếu không có resource nào.</returns>
    Task<IReadOnlyDictionary<string, string>> GetResourceMapAsync(
        string scope,
        string langCode,
        int tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Resolve <b>một</b> resource key thành text theo ngôn ngữ — dùng cho message trùng/validation runtime.
    /// Thay cho việc handler gọi thẳng <c>IResourceRepository</c> (anti-pattern cần dọn — CC-1a).
    /// </summary>
    /// <param name="key">Resource key, VD <c>qlns_nhanvien.val.ma.unique</c> hoặc <c>sys.val.unique</c>.</param>
    /// <param name="langCode">Ngôn ngữ: 'vi' hoặc 'en'.</param>
    /// <param name="tenantId">Tenant hiện tại.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Text đã resolve; <c>null</c> nếu key không tồn tại (để caller fallback).</returns>
    Task<string?> ResolveKeyAsync(
        string key,
        string langCode,
        int tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách option của một danh mục <c>Sys_Lookup</c> theo code + ngôn ngữ (đã resolve label).
    /// Phục vụ render ComboBox/LookupBox tĩnh; invalidate khi sửa Sys_Lookup (CC-2).
    /// </summary>
    /// <param name="lookupCode">Sys_Lookup.Lookup_Code.</param>
    /// <param name="langCode">Ngôn ngữ: 'vi' hoặc 'en'.</param>
    /// <param name="tenantId">Tenant hiện tại.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách <see cref="LookupItem"/> đã sắp theo SortOrder; rỗng nếu không có.</returns>
    Task<IReadOnlyList<LookupItem>> GetLookupOptionsAsync(
        string lookupCode,
        string langCode,
        int tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Xóa cache options của một <c>Sys_Lookup</c> code (mọi ngôn ngữ đã biết) — gọi sau khi admin
    /// sửa danh mục ở ConfigStudio (CC-4b sẽ wire WPF gọi endpoint này).
    /// </summary>
    /// <param name="lookupCode">Sys_Lookup.Lookup_Code cần invalidate.</param>
    /// <param name="tenantId">Tenant sở hữu danh mục.</param>
    Task InvalidateLookupAsync(string lookupCode, int tenantId);

    /// <summary>
    /// Lấy quyền của một form theo tenant để runtime enforce (xem/thêm/sửa/xóa) — CC-3.
    /// </summary>
    /// <param name="formId">Ui_Form.Form_Id.</param>
    /// <param name="tenantId">Tenant hiện tại.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="FormPermission"/> nếu có cấu hình; <c>null</c> nếu chưa cấu hình
    /// (caller xử lý deny-by-default).
    /// </returns>
    Task<FormPermission?> GetFormPermissionsAsync(
        int formId,
        int tenantId,
        CancellationToken ct = default);
}
