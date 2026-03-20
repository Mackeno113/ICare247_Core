// File    : EventDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Dapper implementation cho IEventDataService — Evt_Definition + Evt_Action.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// CRUD events + actions. Delete event cascade xóa actions.
/// </summary>
public sealed class EventDataService : IEventDataService
{
    private readonly IAppConfigService _config;

    public EventDataService(IAppConfigService config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventItemRecord>> GetEventsByFieldAsync(int fieldId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT ed.Event_Id      AS EventId,
                   ed.Form_Id       AS FormId,
                   ed.Field_Id      AS FieldId,
                   ed.Trigger_Code  AS TriggerCode,
                   ed.Condition_Expr AS ConditionExpr,
                   ed.Order_No      AS OrderNo,
                   ed.Is_Active     AS IsActive,
                   (SELECT COUNT(*) FROM dbo.Evt_Action ea WHERE ea.Event_Id = ed.Event_Id) AS ActionsCount
            FROM   dbo.Evt_Definition ed
            WHERE  ed.Field_Id = @FieldId AND ed.Is_Active = 1
            ORDER BY ed.Order_No
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<EventItemRecord>(
            new CommandDefinition(sql, new { FieldId = fieldId }, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ActionItemRecord>> GetActionsByEventAsync(int eventId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT Action_Id        AS ActionId,
                   Event_Id         AS EventId,
                   Action_Code      AS ActionCode,
                   Action_Param_Json AS ParamJson,
                   Order_No         AS OrderNo
            FROM   dbo.Evt_Action
            WHERE  Event_Id = @EventId
            ORDER BY Order_No
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<ActionItemRecord>(
            new CommandDefinition(sql, new { EventId = eventId }, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetTriggerTypesAsync(CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = "SELECT Trigger_Code FROM dbo.Evt_Trigger_Type ORDER BY Trigger_Code";

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<string>(
            new CommandDefinition(sql, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ActionTypeRecord>> GetActionTypesAsync(CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT Action_Code  AS ActionCode,
                   Param_Schema AS ParamSchema
            FROM   dbo.Evt_Action_Type
            ORDER BY Action_Code
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<ActionTypeRecord>(
            new CommandDefinition(sql, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<int> SaveEventAsync(EventItemRecord evt, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return 0;

        if (evt.EventId == 0)
        {
            const string sql = """
                INSERT INTO dbo.Evt_Definition
                       (Form_Id, Field_Id, Trigger_Code, Condition_Expr, Order_No, Is_Active, Updated_At)
                OUTPUT INSERTED.Event_Id
                VALUES (@FormId, @FieldId, @TriggerCode, @ConditionExpr, @OrderNo, 1, GETDATE())
                """;

            await using var conn = new SqlConnection(_config.ConnectionString);
            return await conn.QuerySingleAsync<int>(
                new CommandDefinition(sql, evt, cancellationToken: ct));
        }
        else
        {
            const string sql = """
                UPDATE dbo.Evt_Definition
                SET    Trigger_Code  = @TriggerCode,
                       Condition_Expr = @ConditionExpr,
                       Order_No      = @OrderNo,
                       Updated_At    = GETDATE()
                WHERE  Event_Id = @EventId
                """;

            await using var conn = new SqlConnection(_config.ConnectionString);
            await conn.ExecuteAsync(new CommandDefinition(sql, evt, cancellationToken: ct));
            return evt.EventId;
        }
    }

    /// <inheritdoc />
    public async Task SaveActionAsync(ActionItemRecord action, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        if (action.ActionId == 0)
        {
            const string sql = """
                INSERT INTO dbo.Evt_Action
                       (Event_Id, Action_Code, Action_Param_Json, Order_No)
                VALUES (@EventId, @ActionCode, @ParamJson, @OrderNo)
                """;

            await using var conn = new SqlConnection(_config.ConnectionString);
            await conn.ExecuteAsync(new CommandDefinition(sql, action, cancellationToken: ct));
        }
        else
        {
            const string sql = """
                UPDATE dbo.Evt_Action
                SET    Action_Code      = @ActionCode,
                       Action_Param_Json = @ParamJson,
                       Order_No         = @OrderNo
                WHERE  Action_Id = @ActionId
                """;

            await using var conn = new SqlConnection(_config.ConnectionString);
            await conn.ExecuteAsync(new CommandDefinition(sql, action, cancellationToken: ct));
        }
    }

    /// <inheritdoc />
    public async Task DeleteEventAsync(int eventId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        // Xóa actions trước, rồi xóa event
        const string sql = """
            DELETE FROM dbo.Evt_Action WHERE Event_Id = @EventId;
            DELETE FROM dbo.Evt_Definition WHERE Event_Id = @EventId;
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { EventId = eventId }, cancellationToken: ct));
    }
}
