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
    Task<IReadOnlyList<SectionDetailRecord>> GetSectionsByFormAsync(int formId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<FieldDetailRecord>> GetFieldsByFormAsync(int formId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<EventSummaryRecord>> GetEventsSummaryByFormAsync(int formId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<RuleSummaryRecord>> GetRulesSummaryByFormAsync(int formId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogRecord>> GetAuditLogAsync(string objectType, int objectId, CancellationToken ct = default);
    Task DeactivateFormAsync(int formId, int tenantId, CancellationToken ct = default);
    Task RestoreFormAsync(int formId, int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Upsert một Section vào Ui_Section.
    /// Khi SectionId=0 → INSERT và trả về Section_Id mới sinh.
    /// Khi SectionId>0 → UPDATE và trả về SectionId truyền vào.
    /// Khi OldTitleKey khác TitleKey → rename Resource_Key trong Sys_Resource.
    /// </summary>
    Task<int> UpsertSectionAsync(SectionUpsertRequest req, CancellationToken ct = default);

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
