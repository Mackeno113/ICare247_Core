// File    : IEventDataService.cs
// Module  : Data
// Layer   : Core
// Purpose : Interface CRUD events + actions cho EventEditorView.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// CRUD events (Evt_Definition) + actions (Evt_Action).
/// </summary>
public interface IEventDataService
{
    Task<IReadOnlyList<EventItemRecord>> GetEventsByFieldAsync(int fieldId, CancellationToken ct = default);
    Task<IReadOnlyList<ActionItemRecord>> GetActionsByEventAsync(int eventId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetTriggerTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ActionTypeRecord>> GetActionTypesAsync(CancellationToken ct = default);
    Task<int> SaveEventAsync(EventItemRecord evt, CancellationToken ct = default);
    Task SaveActionAsync(ActionItemRecord action, CancellationToken ct = default);
    Task DeleteEventAsync(int eventId, CancellationToken ct = default);
}
