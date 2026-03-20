// File    : IEventRepository.cs
// Module  : Event
// Layer   : Application
// Purpose : Repository interface cho Evt_Definition + Evt_Action — load event handlers theo form/field/trigger.

using ICare247.Domain.Entities.Event;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Repository cho <c>Evt_Definition</c> + <c>Evt_Action</c>.
/// Load event handlers kèm actions trong 1 query (multi-mapping).
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Lấy tất cả event definitions (active) cho một form + trigger code.
    /// Kèm theo danh sách actions, sắp xếp theo Order_No.
    /// </summary>
    /// <param name="formId">Form đang xử lý.</param>
    /// <param name="triggerCode">Trigger code: 'OnChange', 'OnBlur', 'OnLoad', 'OnSubmit', 'OnSectionToggle'.</param>
    /// <param name="fieldCode">
    /// Field code phát sinh event. NULL nếu form-level event (OnLoad, OnSubmit).
    /// Khi có fieldCode → filter thêm theo field.
    /// </param>
    /// <param name="tenantId">Tenant — bắt buộc.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<EventDefinition>> GetByTriggerAsync(
        int formId,
        string triggerCode,
        string? fieldCode,
        int tenantId,
        CancellationToken ct = default);
}
