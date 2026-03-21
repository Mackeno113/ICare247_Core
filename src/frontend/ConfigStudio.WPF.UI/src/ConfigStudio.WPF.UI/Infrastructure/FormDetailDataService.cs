// File    : FormDetailDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Dapper implementation cho IFormDetailDataService — chi tiết form read-only.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// Truy vấn chi tiết form: header, sections, fields, events, rules, audit log.
/// Tenant resolve qua Sys_Table.Tenant_Id.
/// </summary>
public sealed class FormDetailDataService : IFormDetailDataService
{
    private readonly IAppConfigService _config;

    public FormDetailDataService(IAppConfigService config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public async Task<FormDetailRecord?> GetFormDetailAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return null;

        const string sql = """
            SELECT f.Form_Id       AS FormId,
                   f.Form_Code     AS FormCode,
                   st.Table_Name   AS TableName,
                   f.Table_Id      AS TableId,
                   f.Platform,
                   f.Layout_Engine AS LayoutEngine,
                   f.Version,
                   f.Checksum,
                   f.Is_Active     AS IsActive,
                   f.Updated_At    AS UpdatedAt,
                   f.Description
            FROM   dbo.Ui_Form f
            JOIN   dbo.Sys_Table st ON st.Table_Id = f.Table_Id
            WHERE  f.Form_Id = @FormId
              AND  (st.Tenant_Id = @TenantId OR st.Tenant_Id IS NULL)
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        return await conn.QueryFirstOrDefaultAsync<FormDetailRecord>(
            new CommandDefinition(sql, new { FormId = formId, TenantId = tenantId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SectionDetailRecord>> GetSectionsByFormAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT s.Section_Id   AS SectionId,
                   s.Section_Code AS SectionCode,
                   s.Title_Key    AS TitleKey,
                   s.Order_No     AS OrderNo,
                   s.Layout_Json  AS LayoutJson,
                   (SELECT COUNT(*) FROM dbo.Ui_Field fi WHERE fi.Section_Id = s.Section_Id) AS FieldCount
            FROM   dbo.Ui_Section s
            WHERE  s.Form_Id = @FormId AND s.Is_Active = 1
            ORDER BY s.Order_No
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<SectionDetailRecord>(
            new CommandDefinition(sql, new { FormId = formId }, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FieldDetailRecord>> GetFieldsByFormAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT fi.Field_Id     AS FieldId,
                   fi.Order_No     AS OrderNo,
                   sc.Column_Code  AS ColumnCode,
                   ISNULL(se.Section_Code, '') AS SectionCode,
                   fi.Editor_Type  AS EditorType,
                   ISNULL(fi.Label_Key, '')    AS LabelKey,
                   fi.Is_Visible   AS IsVisible,
                   fi.Is_ReadOnly  AS IsReadOnly,
                   (SELECT COUNT(*) FROM dbo.Val_Rule vr WHERE vr.Field_Id = fi.Field_Id) AS RuleCount
            FROM   dbo.Ui_Field fi
            JOIN   dbo.Sys_Column sc ON sc.Column_Id = fi.Column_Id
            LEFT JOIN dbo.Ui_Section se ON se.Section_Id = fi.Section_Id
            WHERE  fi.Form_Id = @FormId
            ORDER BY fi.Order_No
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<FieldDetailRecord>(
            new CommandDefinition(sql, new { FormId = formId }, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventSummaryRecord>> GetEventsSummaryByFormAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT ed.Event_Id      AS EventId,
                   ed.Order_No      AS OrderNo,
                   ed.Trigger_Code  AS TriggerCode,
                   ISNULL(sc.Column_Code, '') AS FieldTarget,
                   LEFT(ISNULL(ed.Condition_Expr, ''), 100) AS ConditionPreview,
                   (SELECT COUNT(*) FROM dbo.Evt_Action ea WHERE ea.Event_Id = ed.Event_Id) AS ActionsCount,
                   ed.Is_Active     AS IsActive
            FROM   dbo.Evt_Definition ed
            LEFT JOIN dbo.Ui_Field fi ON fi.Field_Id = ed.Field_Id
            LEFT JOIN dbo.Sys_Column sc ON sc.Column_Id = fi.Column_Id
            WHERE  ed.Form_Id = @FormId
            ORDER BY ed.Order_No
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<EventSummaryRecord>(
            new CommandDefinition(sql, new { FormId = formId }, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuleSummaryRecord>> GetRulesSummaryByFormAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT vr.Rule_Id        AS RuleId,
                   vr.Order_No       AS OrderNo,
                   vr.Rule_Type_Code AS RuleTypeCode,
                   LEFT(ISNULL(vr.Expression_Json, ''), 100) AS ExpressionPreview,
                   vr.Error_Key      AS ErrorKey,
                   vr.Is_Active      AS IsActive
            FROM   dbo.Val_Rule vr
            JOIN   dbo.Ui_Field fi ON fi.Field_Id = vr.Field_Id
            WHERE  fi.Form_Id = @FormId
            ORDER BY vr.Order_No
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<RuleSummaryRecord>(
            new CommandDefinition(sql, new { FormId = formId }, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogRecord>> GetAuditLogAsync(string objectType, int objectId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT Audit_Id       AS AuditId,
                   Action,
                   Changed_By     AS ChangedBy,
                   Changed_At     AS ChangedAt,
                   Correlation_Id AS CorrelationId,
                   LEFT(ISNULL(New_Value_Json, Old_Value_Json), 200) AS ChangeSummary
            FROM   dbo.Sys_Audit_Log
            WHERE  Object_Type = @ObjectType AND Object_Id = @ObjectId
            ORDER BY Changed_At DESC
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<AuditLogRecord>(
            new CommandDefinition(sql, new { ObjectType = objectType, ObjectId = objectId }, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<int> UpsertSectionAsync(SectionUpsertRequest req, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return 0;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            // ── Rename Resource_Key nếu Title_Key thay đổi (user đổi Section Code) ──
            if (!string.IsNullOrEmpty(req.OldTitleKey) && req.OldTitleKey != req.TitleKey)
            {
                const string renameSql = """
                    UPDATE dbo.Sys_Resource
                    SET    Resource_Key = @NewKey, Updated_At = GETDATE()
                    WHERE  Resource_Key = @OldKey
                    """;
                await conn.ExecuteAsync(
                    new CommandDefinition(renameSql, new { OldKey = req.OldTitleKey, NewKey = req.TitleKey },
                        transaction: (System.Data.IDbTransaction)tx, cancellationToken: ct));
            }

            int resultId;

            if (req.SectionId == 0)
            {
                // ── INSERT mới ──────────────────────────────────────────────────────
                const string insertSql = """
                    INSERT INTO dbo.Ui_Section (Form_Id, Section_Code, Title_Key, Order_No, Is_Active)
                    OUTPUT INSERTED.Section_Id
                    VALUES (@FormId, @SectionCode, @TitleKey, @OrderNo, @IsActive)
                    """;
                resultId = await conn.QuerySingleAsync<int>(
                    new CommandDefinition(insertSql,
                        new { req.FormId, req.SectionCode, req.TitleKey, req.OrderNo, req.IsActive },
                        transaction: (System.Data.IDbTransaction)tx, cancellationToken: ct));
            }
            else
            {
                // ── UPDATE bản ghi hiện có ──────────────────────────────────────────
                const string updateSql = """
                    UPDATE dbo.Ui_Section
                    SET    Section_Code = @SectionCode,
                           Title_Key    = @TitleKey,
                           Order_No     = @OrderNo,
                           Is_Active    = @IsActive
                    WHERE  Section_Id   = @SectionId
                    """;
                await conn.ExecuteAsync(
                    new CommandDefinition(updateSql,
                        new { req.SectionCode, req.TitleKey, req.OrderNo, req.IsActive, req.SectionId },
                        transaction: (System.Data.IDbTransaction)tx, cancellationToken: ct));
                resultId = req.SectionId;
            }

            await tx.CommitAsync(ct);
            return resultId;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeactivateFormAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        const string sql = "UPDATE dbo.Ui_Form SET Is_Active = 0, Updated_At = GETDATE() WHERE Form_Id = @FormId";

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { FormId = formId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task RestoreFormAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        const string sql = "UPDATE dbo.Ui_Form SET Is_Active = 1, Updated_At = GETDATE() WHERE Form_Id = @FormId";

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { FormId = formId }, cancellationToken: ct));
    }
}
