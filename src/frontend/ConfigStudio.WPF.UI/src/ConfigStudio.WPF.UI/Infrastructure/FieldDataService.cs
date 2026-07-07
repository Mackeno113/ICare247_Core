// File    : FieldDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Dapper implementation cho IFieldDataService.
//           Ui_Field (bao gồm Col_Span, Lookup_Source, Lookup_Code)
//           + Ui_Field_Lookup (FK dynamic config) trong cùng transaction.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// CRUD field metadata + Ui_Field_Lookup. Tenant resolve qua Sys_Table.
/// </summary>
public sealed class FieldDataService : IFieldDataService
{
    private readonly IAppConfigService _config;

    public FieldDataService(IAppConfigService config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public async Task<FieldConfigRecord?> GetFieldDetailAsync(
        int fieldId, int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return null;

        const string sql = """
            SELECT fi.Field_Id           AS FieldId,
                   fi.Form_Id            AS FormId,
                   fi.Section_Id         AS SectionId,
                   fi.Column_Id          AS ColumnId,
                   ISNULL(sc.Column_Code, '') AS ColumnCode,
                   fi.Field_Code              AS FieldCode,
                   ISNULL(se.Section_Code, '') AS SectionCode,
                   fi.Editor_Type        AS EditorType,
                   fi.Label_Key          AS LabelKey,
                   fi.Placeholder_Key    AS PlaceholderKey,
                   fi.Tooltip_Key        AS TooltipKey,
                   fi.Required_Error_Key AS RequiredErrorKey,
                   fi.Is_Visible         AS IsVisible,
                   fi.Is_ReadOnly        AS IsReadOnly,
                   fi.Is_Required        AS IsRequired,
                   fi.Lock_On_Edit       AS LockOnEdit,
                   fi.Is_Virtual         AS IsVirtual,
                   fi.Is_Unique          AS IsUnique,
                   fi.Order_No           AS OrderNo,
                   fi.Control_Props_Json AS ControlPropsJson,
                   fi.Col_Span           AS ColSpan,
                   fi.Show_In_List       AS ShowInList,
                   fi.Lookup_Source      AS LookupSource,
                   fi.Lookup_Code        AS LookupCode,
                   fi.Version,
                   fi.Description
            FROM   dbo.Ui_Field fi
            LEFT JOIN dbo.Sys_Column sc ON sc.Column_Id = fi.Column_Id
            LEFT JOIN dbo.Ui_Section se ON se.Section_Id = fi.Section_Id
            JOIN   dbo.Ui_Form f ON f.Form_Id = fi.Form_Id
            JOIN   dbo.Sys_Table st ON st.Table_Id = f.Table_Id
            WHERE  fi.Field_Id = @FieldId
              AND  (st.Tenant_Id = @TenantId OR st.Tenant_Id IS NULL)
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        return await conn.QueryFirstOrDefaultAsync<FieldConfigRecord>(
            new CommandDefinition(sql, new { FieldId = fieldId, TenantId = tenantId },
                cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<(bool IsMasked, string? MaskMode)> GetColumnMaskingAsync(
        int columnId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || columnId <= 0) return (false, null);
        const string sql = """
            SELECT ISNULL(Is_Log_Masked, 0) AS IsMasked, Log_Mask_Mode AS MaskMode
            FROM   dbo.Sys_Column WHERE Column_Id = @ColumnId
            """;
        try
        {
            await using var conn = new SqlConnection(_config.ConnectionString);
            var row = await conn.QueryFirstOrDefaultAsync<(bool IsMasked, string? MaskMode)>(
                new CommandDefinition(sql, new { ColumnId = columnId }, cancellationToken: ct));
            return row;
        }
        catch (SqlException)
        {
            return (false, null);   // cột Is_Log_Masked/Log_Mask_Mode chưa migrate (db/071) → không làm mờ
        }
    }

    /// <inheritdoc />
    public async Task SaveColumnMaskingAsync(
        int columnId, bool isMasked, string? maskMode, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || columnId <= 0) return;
        const string sql = """
            UPDATE dbo.Sys_Column
            SET    Is_Log_Masked = @IsMasked,
                   Log_Mask_Mode = @MaskMode
            WHERE  Column_Id = @ColumnId
            """;
        try
        {
            await using var conn = new SqlConnection(_config.ConnectionString);
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                ColumnId = columnId,
                IsMasked = isMasked,
                MaskMode = isMasked ? (maskMode ?? "Full") : null
            }, cancellationToken: ct));
        }
        catch (SqlException)
        {
            /* cột chưa migrate (db/071) → bỏ qua, không chặn lưu field */
        }
    }

    /// <inheritdoc />
    public async Task<FieldLookupConfigRecord?> GetFieldLookupConfigAsync(
        int fieldId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return null;

        const string sql = """
            SELECT fl.Field_Id              AS FieldId,
                   fl.Query_Mode            AS QueryMode,
                   fl.Source_Name           AS SourceName,
                   fl.Value_Column          AS ValueColumn,
                   fl.Display_Column        AS DisplayColumn,
                   fl.Filter_Sql            AS FilterSql,
                   fl.Order_By              AS OrderBy,
                   fl.Search_Enabled        AS SearchEnabled,
                   fl.Popup_Columns_Json    AS PopupColumnsJson,
                   ISNULL(fl.EditBox_Mode, N'TextOnly') AS EditBoxMode,
                   fl.Code_Field            AS CodeField,
                   ISNULL(fl.DropDown_Width,  600)      AS DropDownWidth,
                   ISNULL(fl.DropDown_Height, 400)      AS DropDownHeight,
                   fl.Reload_Trigger_Field  AS ReloadTriggerField,
                   fl.Reload_Trigger_Fields AS ReloadTriggerFields,
                   fl.Parent_Column         AS ParentColumn,
                   fl.Tree_Selectable_Level AS TreeSelectableLevel,
                   ISNULL(fl.Allow_Add_New, 0) AS AllowAddNew,
                   fl.Add_Form_Code         AS AddFormCode
            FROM   dbo.Ui_Field_Lookup fl
            WHERE  fl.Field_Id = @FieldId
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        return await conn.QueryFirstOrDefaultAsync<FieldLookupConfigRecord>(
            new CommandDefinition(sql, new { FieldId = fieldId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ColumnInfoRecord>> GetColumnsByTableAsync(
        int tableId, CancellationToken ct = default)
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
    public async Task<int> GetTableIdByFormAsync(
        int formId, int tenantId, CancellationToken ct = default)
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
            new CommandDefinition(sql, new { FormId = formId, TenantId = tenantId },
                cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<int> SaveFieldAsync(
        FieldConfigRecord field, int tenantId,
        FieldLookupConfigRecord? lookupConfig = null,
        bool shiftOnInsert = false,
        CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return 0;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            int fieldId;

            if (field.FieldId == 0)
            {
                // Chèn giữa danh sách: đẩy STT các field phía sau (cùng section, Order_No ≥ vị trí
                // chèn) lên +1 để nhường chỗ — giữ thứ tự liền mạch, không trùng STT. Field mới nối
                // cuối (Order_No = max+1) thì không có dòng nào khớp → không đổi gì.
                // Sự kiện theo sau: INSERT bên dưới ghi field mới đúng vị trí Order_No yêu cầu.
                if (shiftOnInsert && field.SectionId is > 0)
                {
                    const string sqlShift = """
                        UPDATE dbo.Ui_Field
                        SET    Order_No = Order_No + 1
                        WHERE  Section_Id = @SectionId AND Order_No >= @OrderNo
                        """;

                    await conn.ExecuteAsync(
                        new CommandDefinition(sqlShift,
                            new { field.SectionId, field.OrderNo },
                            transaction: tx, cancellationToken: ct));
                }

                // ── INSERT Ui_Field ──────────────────────────────────────────
                const string sqlInsert = """
                    INSERT INTO dbo.Ui_Field
                           (Form_Id, Section_Id, Column_Id, Editor_Type, Label_Key,
                            Placeholder_Key, Tooltip_Key, Required_Error_Key,
                            Is_Visible, Is_ReadOnly, Is_Required, Lock_On_Edit, Is_Virtual, Is_Unique,
                            Order_No, Control_Props_Json, Col_Span, Show_In_List,
                            Lookup_Source, Lookup_Code, Field_Code, Version, Updated_At, Description)
                    OUTPUT INSERTED.Field_Id
                    VALUES (@FormId, @SectionId, @ColumnId, @EditorType, @LabelKey,
                            @PlaceholderKey, @TooltipKey, @RequiredErrorKey,
                            @IsVisible, @IsReadOnly, @IsRequired, @LockOnEdit, @IsVirtual, @IsUnique,
                            @OrderNo, @ControlPropsJson, @ColSpan, @ShowInList,
                            @LookupSource, @LookupCode, @FieldCode, 1, GETDATE(), @Description)
                    """;

                fieldId = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(sqlInsert, BuildFieldParam(field),
                        transaction: tx, cancellationToken: ct));
            }
            else
            {
                // ── UPDATE Ui_Field ──────────────────────────────────────────
                const string sqlUpdate = """
                    UPDATE dbo.Ui_Field
                    SET    Section_Id        = @SectionId,
                           Column_Id         = @ColumnId,
                           Editor_Type       = @EditorType,
                           Label_Key         = @LabelKey,
                           Placeholder_Key   = @PlaceholderKey,
                           Tooltip_Key       = @TooltipKey,
                           Required_Error_Key = @RequiredErrorKey,
                           Is_Visible        = @IsVisible,
                           Is_ReadOnly       = @IsReadOnly,
                           Is_Required       = @IsRequired,
                           Lock_On_Edit      = @LockOnEdit,
                           Is_Virtual        = @IsVirtual,
                           Is_Unique         = @IsUnique,
                           Field_Code        = @FieldCode,
                           Order_No          = @OrderNo,
                           Control_Props_Json = @ControlPropsJson,
                           Col_Span          = @ColSpan,
                           Show_In_List      = @ShowInList,
                           Lookup_Source     = @LookupSource,
                           Lookup_Code       = @LookupCode,
                           Version           = Version + 1,
                           Updated_At        = GETDATE(),
                           Description       = @Description
                    WHERE  Field_Id = @FieldId
                    """;

                await conn.ExecuteAsync(
                    new CommandDefinition(sqlUpdate, BuildFieldParam(field, field.FieldId),
                        transaction: tx, cancellationToken: ct));

                fieldId = field.FieldId;
            }

            // ── Xử lý Ui_Field_Lookup ──────────────────────────────────────
            if (lookupConfig is not null)
            {
                // INSERT nếu chưa có (UNIQUE constraint Field_Id), UPDATE nếu đã có
                const string sqlUpsertLookup = """
                    IF EXISTS (SELECT 1 FROM dbo.Ui_Field_Lookup WHERE Field_Id = @FieldId)
                        UPDATE dbo.Ui_Field_Lookup
                        SET    Query_Mode            = @QueryMode,
                               Source_Name           = @SourceName,
                               Value_Column          = @ValueColumn,
                               Display_Column        = @DisplayColumn,
                               Filter_Sql            = @FilterSql,
                               Order_By              = @OrderBy,
                               Search_Enabled        = @SearchEnabled,
                               Popup_Columns_Json    = @PopupColumnsJson,
                               EditBox_Mode          = @EditBoxMode,
                               Code_Field            = @CodeField,
                               DropDown_Width        = @DropDownWidth,
                               DropDown_Height       = @DropDownHeight,
                               Reload_Trigger_Field  = @ReloadTriggerField,
                               Reload_Trigger_Fields = @ReloadTriggerFields,
                               Parent_Column         = @ParentColumn,
                               Tree_Selectable_Level = @TreeSelectableLevel,
                               Allow_Add_New         = @AllowAddNew,
                               Add_Form_Code         = @AddFormCode,
                               Updated_At            = GETDATE()
                        WHERE  Field_Id = @FieldId
                    ELSE
                        INSERT INTO dbo.Ui_Field_Lookup
                               (Field_Id, Query_Mode, Source_Name, Value_Column,
                                Display_Column, Filter_Sql, Order_By, Search_Enabled,
                                Popup_Columns_Json, EditBox_Mode, Code_Field,
                                DropDown_Width, DropDown_Height, Reload_Trigger_Field,
                                Reload_Trigger_Fields, Parent_Column, Tree_Selectable_Level,
                                Allow_Add_New, Add_Form_Code, Updated_At)
                        VALUES (@FieldId, @QueryMode, @SourceName, @ValueColumn,
                                @DisplayColumn, @FilterSql, @OrderBy, @SearchEnabled,
                                @PopupColumnsJson, @EditBoxMode, @CodeField,
                                @DropDownWidth, @DropDownHeight, @ReloadTriggerField,
                                @ReloadTriggerFields, @ParentColumn, @TreeSelectableLevel,
                                @AllowAddNew, @AddFormCode, GETDATE())
                    """;

                await conn.ExecuteAsync(
                    new CommandDefinition(sqlUpsertLookup, new
                    {
                        FieldId              = fieldId,
                        lookupConfig.QueryMode,
                        lookupConfig.SourceName,
                        lookupConfig.ValueColumn,
                        lookupConfig.DisplayColumn,
                        lookupConfig.FilterSql,
                        lookupConfig.OrderBy,
                        lookupConfig.SearchEnabled,
                        lookupConfig.PopupColumnsJson,
                        lookupConfig.EditBoxMode,
                        lookupConfig.CodeField,
                        lookupConfig.DropDownWidth,
                        lookupConfig.DropDownHeight,
                        lookupConfig.ReloadTriggerField,
                        lookupConfig.ReloadTriggerFields,
                        lookupConfig.ParentColumn,
                        lookupConfig.TreeSelectableLevel,
                        lookupConfig.AllowAddNew,
                        lookupConfig.AddFormCode
                    }, transaction: tx, cancellationToken: ct));
            }
            else
            {
                // Xóa lookup config nếu tồn tại (field đã đổi sang type không phải LookupBox)
                const string sqlDeleteLookup = """
                    DELETE FROM dbo.Ui_Field_Lookup WHERE Field_Id = @FieldId
                    """;

                await conn.ExecuteAsync(
                    new CommandDefinition(sqlDeleteLookup, new { FieldId = fieldId },
                        transaction: tx, cancellationToken: ct));
            }

            await tx.CommitAsync(ct);
            return fieldId;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task MarkFieldConfiguredAsync(int fieldId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || fieldId <= 0) return;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(
            new CommandDefinition(
                "UPDATE dbo.Ui_Field SET Is_Configured = 1 WHERE Field_Id = @FieldId",
                new { FieldId = fieldId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<int> EnsureColumnExistsAsync(
        int tableId, ColumnSchemaDto col, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || tableId <= 0) return 0;

        const string sql = """
            IF NOT EXISTS (
                SELECT 1 FROM dbo.Sys_Column
                WHERE  Table_Id = @TableId AND Column_Code = @ColumnCode
            )
            BEGIN
                INSERT INTO dbo.Sys_Column
                    (Table_Id, Column_Code, Data_Type, Net_Type, Max_Length,
                     Is_Nullable, Is_PK, Is_Identity, Is_Active, Version, Updated_At)
                VALUES
                    (@TableId, @ColumnCode, @DataType, @NetType, @MaxLength,
                     @IsNullable, @IsPK, @IsIdentity, 1, 1, GETDATE())
            END

            SELECT Column_Id FROM dbo.Sys_Column
            WHERE  Table_Id = @TableId AND Column_Code = @ColumnCode
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                TableId    = tableId,
                ColumnCode = col.ColumnName,
                DataType   = col.DataType,
                NetType    = col.NetType,
                MaxLength  = col.MaxLength,
                IsNullable = col.IsNullable ? 1 : 0,
                IsPK       = col.IsPrimaryKey ? 1 : 0,
                IsIdentity = col.IsIdentity ? 1 : 0,
            }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteFieldAsync(int fieldId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || fieldId <= 0) return;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            // Xóa lookup config trước (FK constraint)
            await conn.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM dbo.Ui_Field_Lookup WHERE Field_Id = @FieldId",
                    new { FieldId = fieldId }, transaction: tx, cancellationToken: ct));

            await conn.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM dbo.Ui_Field WHERE Field_Id = @FieldId",
                    new { FieldId = fieldId }, transaction: tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task UpdateFieldOrderAsync(IReadOnlyList<(int FieldId, int OrderNo)> items,
        CancellationToken ct = default)
    {
        if (!_config.IsConfigured || items.Count == 0) return;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            foreach (var (fieldId, orderNo) in items)
                await conn.ExecuteAsync(
                    new CommandDefinition(
                        "UPDATE dbo.Ui_Field SET Order_No = @OrderNo WHERE Field_Id = @FieldId",
                        new { FieldId = fieldId, OrderNo = orderNo },
                        transaction: tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task MoveFieldToSectionAsync(int fieldId, int sectionId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || fieldId <= 0 || sectionId <= 0) return;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(
            new CommandDefinition(
                "UPDATE dbo.Ui_Field SET Section_Id = @SectionId WHERE Field_Id = @FieldId",
                new { FieldId = fieldId, SectionId = sectionId }, cancellationToken: ct));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>Build anonymous param object cho INSERT/UPDATE Ui_Field.</summary>
    private static object BuildFieldParam(FieldConfigRecord f, int fieldId = 0) => new
    {
        FieldId          = fieldId,
        f.FormId,
        f.SectionId,
        // ColumnId = 0 nghĩa là chưa chọn cột (virtual field) → gửi NULL tránh FK violation
        ColumnId         = f.ColumnId > 0 ? (int?)f.ColumnId : null,
        // FieldCode: lưu trực tiếp cho virtual field; null cho field thường (dùng Sys_Column)
        FieldCode        = string.IsNullOrWhiteSpace(f.FieldCode) ? null : f.FieldCode,
        f.EditorType,
        f.LabelKey,
        f.PlaceholderKey,
        f.TooltipKey,
        f.RequiredErrorKey,
        f.IsVisible,
        f.IsReadOnly,
        f.IsRequired,
        f.LockOnEdit,
        f.IsVirtual,
        f.IsUnique,
        f.OrderNo,
        // LookupBox lưu config vào Ui_Field_Lookup — Control_Props_Json chứa props khác
        ControlPropsJson = f.LookupSource == "dynamic" ? null : f.ControlPropsJson,
        f.ColSpan,
        f.ShowInList,
        f.LookupSource,
        f.LookupCode,
        f.Description
    };
}
