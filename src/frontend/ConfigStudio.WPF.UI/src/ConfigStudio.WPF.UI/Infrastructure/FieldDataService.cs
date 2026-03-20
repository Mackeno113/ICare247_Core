// File    : FieldDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Dapper implementation cho IFieldDataService — Ui_Field + Sys_Column.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// CRUD field metadata + lookup columns. Tenant resolve qua Sys_Table.
/// </summary>
public sealed class FieldDataService : IFieldDataService
{
    private readonly IAppConfigService _config;

    public FieldDataService(IAppConfigService config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public async Task<FieldConfigRecord?> GetFieldDetailAsync(int fieldId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return null;

        const string sql = """
            SELECT fi.Field_Id           AS FieldId,
                   fi.Form_Id            AS FormId,
                   fi.Section_Id         AS SectionId,
                   fi.Column_Id          AS ColumnId,
                   sc.Column_Code        AS ColumnCode,
                   ISNULL(se.Section_Code, '') AS SectionCode,
                   fi.Editor_Type        AS EditorType,
                   fi.Label_Key          AS LabelKey,
                   fi.Placeholder_Key    AS PlaceholderKey,
                   fi.Tooltip_Key        AS TooltipKey,
                   fi.Is_Visible         AS IsVisible,
                   fi.Is_ReadOnly        AS IsReadOnly,
                   fi.Order_No           AS OrderNo,
                   fi.Control_Props_Json AS ControlPropsJson,
                   fi.Version,
                   fi.Description
            FROM   dbo.Ui_Field fi
            JOIN   dbo.Sys_Column sc ON sc.Column_Id = fi.Column_Id
            LEFT JOIN dbo.Ui_Section se ON se.Section_Id = fi.Section_Id
            JOIN   dbo.Ui_Form f ON f.Form_Id = fi.Form_Id
            JOIN   dbo.Sys_Table st ON st.Table_Id = f.Table_Id
            WHERE  fi.Field_Id = @FieldId
              AND  (st.Tenant_Id = @TenantId OR st.Tenant_Id IS NULL)
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        return await conn.QueryFirstOrDefaultAsync<FieldConfigRecord>(
            new CommandDefinition(sql, new { FieldId = fieldId, TenantId = tenantId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ColumnInfoRecord>> GetColumnsByTableAsync(int tableId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT Column_Id   AS ColumnId,
                   Column_Code AS ColumnCode,
                   Data_Type   AS DataType,
                   Net_Type    AS NetType,
                   Max_Length   AS MaxLength,
                   Is_Nullable AS IsNullable,
                   Is_PK       AS IsPk
            FROM   dbo.Sys_Column
            WHERE  Table_Id = @TableId AND Is_Active = 1
            ORDER BY Column_Code
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<ColumnInfoRecord>(
            new CommandDefinition(sql, new { TableId = tableId }, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<int> GetTableIdByFormAsync(int formId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return 0;

        const string sql = """
            SELECT f.Table_Id
            FROM   dbo.Ui_Form f
            JOIN   dbo.Sys_Table st ON st.Table_Id = f.Table_Id
            WHERE  f.Form_Id = @FormId
              AND  (st.Tenant_Id = @TenantId OR st.Tenant_Id IS NULL)
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        return await conn.QueryFirstOrDefaultAsync<int>(
            new CommandDefinition(sql, new { FormId = formId, TenantId = tenantId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task SaveFieldAsync(FieldConfigRecord field, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        if (field.FieldId == 0)
        {
            const string sql = """
                INSERT INTO dbo.Ui_Field
                       (Form_Id, Section_Id, Column_Id, Editor_Type, Label_Key, Placeholder_Key,
                        Tooltip_Key, Is_Visible, Is_ReadOnly, Order_No, Control_Props_Json,
                        Version, Updated_At, Description)
                VALUES (@FormId, @SectionId, @ColumnId, @EditorType, @LabelKey, @PlaceholderKey,
                        @TooltipKey, @IsVisible, @IsReadOnly, @OrderNo, @ControlPropsJson,
                        1, GETDATE(), @Description)
                """;

            await using var conn = new SqlConnection(_config.ConnectionString);
            await conn.ExecuteAsync(new CommandDefinition(sql, field, cancellationToken: ct));
        }
        else
        {
            const string sql = """
                UPDATE dbo.Ui_Field
                SET    Section_Id        = @SectionId,
                       Column_Id         = @ColumnId,
                       Editor_Type       = @EditorType,
                       Label_Key         = @LabelKey,
                       Placeholder_Key   = @PlaceholderKey,
                       Tooltip_Key       = @TooltipKey,
                       Is_Visible        = @IsVisible,
                       Is_ReadOnly       = @IsReadOnly,
                       Order_No          = @OrderNo,
                       Control_Props_Json = @ControlPropsJson,
                       Version           = Version + 1,
                       Updated_At        = GETDATE(),
                       Description       = @Description
                WHERE  Field_Id = @FieldId
                """;

            await using var conn = new SqlConnection(_config.ConnectionString);
            await conn.ExecuteAsync(new CommandDefinition(sql, field, cancellationToken: ct));
        }
    }
}
