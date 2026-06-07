// File    : IFormDetailDataService.cs
// Module  : Data
// Layer   : Core
// Purpose : Interface truy vấn chi tiết form (sections, fields, events, rules, audit).

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// Truy vấn chi tiết form cho FormDetailView (read-only) + Deactivate/Restore.
/// </summary>
public interface IFormDetailDataService
{
    Task<FormDetailRecord?> GetFormDetailAsync(int formId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TabDetailRecord>> GetTabsByFormAsync(int formId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<SectionDetailRecord>> GetSectionsByFormAsync(int formId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<FieldDetailRecord>> GetFieldsByFormAsync(int formId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<EventSummaryRecord>> GetEventsSummaryByFormAsync(int formId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<RuleSummaryRecord>> GetRulesSummaryByFormAsync(int formId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogRecord>> GetAuditLogAsync(string objectType, int objectId, CancellationToken ct = default);
    Task DeactivateFormAsync(int formId, int tenantId, CancellationToken ct = default);
    Task RestoreFormAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>Đọc phân quyền form theo role (Sys_Permission, Object_Type='Form').</summary>
    Task<IReadOnlyList<FormPermissionRecord>> GetFormPermissionsAsync(int formId, CancellationToken ct = default);

    /// <summary>
    /// Lưu phân quyền form theo role (upsert Sys_Permission Object_Type='Form', Object_Id=formId).
    /// Row mọi quyền = false vẫn lưu (ghi nhận đã cấu hình "không cho phép").
    /// </summary>
    Task SaveFormPermissionsAsync(int formId, IReadOnlyList<FormPermissionRecord> permissions,
        CancellationToken ct = default);

    /// <summary>
    /// Upsert một Section vào Ui_Section.
    /// Khi SectionId=0 → INSERT và trả về Section_Id mới sinh.
    /// Khi SectionId>0 → UPDATE và trả về SectionId truyền vào.
    /// Khi OldTitleKey khác TitleKey → rename Resource_Key trong Sys_Resource.
    /// </summary>
    Task<int> UpsertSectionAsync(SectionUpsertRequest req, CancellationToken ct = default);

    /// <summary>
    /// Upsert một Tab vào Ui_Tab.
    /// Khi TabId=0 → INSERT và trả về Tab_Id mới sinh.
    /// Khi TabId>0 → UPDATE và trả về TabId truyền vào.
    /// Khi OldTitleKey khác TitleKey → rename Resource_Key trong Sys_Resource.
    /// Khi IsDefault=true → gỡ cờ default của các tab khác cùng form.
    /// </summary>
    Task<int> UpsertTabAsync(TabUpsertRequest req, CancellationToken ct = default);

    /// <summary>
    /// Xóa một Tab khỏi Ui_Tab. Các section đang gán tab này sẽ được set Tab_Id = NULL
    /// (không xóa section) trong cùng một transaction.
    /// </summary>
    Task DeleteTabAsync(int tabId, CancellationToken ct = default);

    /// <summary>
    /// Xóa hẳn một Section khỏi Ui_Section cùng toàn bộ field con
    /// (Ui_Field + Ui_Field_Lookup) trong một transaction.
    /// </summary>
    Task DeleteSectionAsync(int sectionId, CancellationToken ct = default);

    /// <summary>
    /// Cập nhật Order_No hàng loạt cho danh sách section trong cùng form.
    /// items = [(sectionId, newOrder)] — thứ tự bắt đầu từ 1.
    /// </summary>
    Task UpdateSectionOrderAsync(IReadOnlyList<(int SectionId, int OrderNo)> items,
        CancellationToken ct = default);
}
