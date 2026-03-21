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
}
