// File    : EventRepository.cs
// Module  : Event
// Layer   : Infrastructure
// Purpose : Dapper implementation cho IEventRepository — load event handlers + actions từ DB.

using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Event;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// IEventRepository implementation — Dapper.
/// Multi-mapping: load Evt_Definition + Evt_Action trong 2 queries, merge in-memory.
/// </summary>
public sealed class EventRepository : IEventRepository
{
    private readonly IDbConnectionFactory _db;

    public EventRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventDefinition>> GetByTriggerAsync(
        int formId,
        string triggerCode,
        string? fieldCode,
        int tenantId,
        CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        // ── Query 1: Load event definitions ─────────────────────────
        // FieldCode được lưu trong Sys_Column.Column_Code (join qua Ui_Field.Column_Id)
        // Ui_Field không có cột Field_Code trực tiếp
        const string eventSql = """
            SELECT  ed.Event_Id       AS EventId,
                    ed.Form_Id        AS FormId,
                    ed.Field_Id       AS FieldId,
                    sc.Column_Code    AS FieldCode,
                    ed.Trigger_Code   AS TriggerCode,
                    ed.Condition_Expr AS ConditionExpr,
                    ed.Order_No       AS OrderNo
            FROM    dbo.Evt_Definition ed
            LEFT JOIN dbo.Ui_Field  uf ON uf.Field_Id  = ed.Field_Id
            LEFT JOIN dbo.Sys_Column sc ON sc.Column_Id = uf.Column_Id
            INNER JOIN dbo.Ui_Form  fm ON fm.Form_Id    = ed.Form_Id
            INNER JOIN dbo.Sys_Table st ON st.Table_Id  = fm.Table_Id
            WHERE   ed.Form_Id = @FormId
              AND   ed.Trigger_Code = @TriggerCode
              AND   ed.Is_Active = 1
              AND   st.Tenant_Id = @TenantId
              AND   (@FieldCode IS NULL OR sc.Column_Code = @FieldCode)
            ORDER BY ed.Order_No
            """;

        var events = (await conn.QueryAsync<EventDefinition>(
            new CommandDefinition(
                eventSql,
                new { FormId = formId, TriggerCode = triggerCode, FieldCode = fieldCode, TenantId = tenantId },
                cancellationToken: ct)))
            .ToList();

        if (events.Count == 0)
            return [];

        // ── Query 2: Load actions cho tất cả events (batch) ──────────
        var eventIds = events.Select(e => e.EventId).ToArray();

        const string actionSql = """
            SELECT  ea.Action_Id        AS ActionId,
                    ea.Event_Id         AS EventId,
                    ea.Action_Code      AS ActionCode,
                    ea.Action_Param_Json AS ActionParamJson,
                    ea.Order_No         AS OrderNo
            FROM    dbo.Evt_Action ea
            WHERE   ea.Event_Id IN @EventIds
            ORDER BY ea.Event_Id, ea.Order_No
            """;

        var actions = (await conn.QueryAsync<EventAction>(
            new CommandDefinition(
                actionSql,
                new { EventIds = eventIds },
                cancellationToken: ct)))
            .ToList();

        // ── Merge: gắn actions vào event definitions ──────────────────
        var actionsByEvent = actions
            .GroupBy(a => a.EventId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<EventAction>)g.ToList());

        var result = events.Select(e => e with
        {
            Actions = actionsByEvent.TryGetValue(e.EventId, out var acts) ? acts : []
        }).ToList();

        return result;
    }
}
