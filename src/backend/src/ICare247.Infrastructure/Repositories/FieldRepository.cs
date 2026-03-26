// File    : FieldRepository.cs
// Module  : Form
// Layer   : Infrastructure
// Purpose : Dapper implementation của IFieldRepository — đọc Ui_Field + Ui_Field_Lookup.

using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Form;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Repository cho <c>Ui_Field</c> + <c>Ui_Field_Lookup</c>.
/// Tenant resolve qua Form → Sys_Table.Tenant_Id.
/// </summary>
public sealed class FieldRepository : IFieldRepository
{
    private readonly IDbConnectionFactory _db;

    public FieldRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FieldMetadata>> GetByFormIdAsync(
        int formId, int tenantId, CancellationToken ct = default)
    {
        // ── SELECT đủ cột, resolve FieldCode qua Sys_Column ─────────────────
        const string sql = """
            SELECT fi.Field_Id             AS FieldId,
                   fi.Form_Id              AS FormId,
                   fi.Section_Id           AS SectionId,
                   sc.Column_Code          AS FieldCode,
                   fi.Editor_Type          AS FieldType,
                   fi.Label_Key            AS Label,
                   fi.Order_No             AS SortOrder,
                   fi.Is_Visible           AS IsVisible,
                   fi.Is_ReadOnly          AS IsReadOnly,
                   fi.Is_Required          AS IsRequired,
                   fi.Is_Enabled           AS IsEnabled,
                   fi.Control_Props_Json   AS ControlPropsJson,
                   fi.Col_Span             AS ColSpan,
                   fi.Lookup_Source        AS LookupSource,
                   fi.Lookup_Code          AS LookupCode
            FROM   dbo.Ui_Field fi
            JOIN   dbo.Ui_Form f   ON f.Form_Id   = fi.Form_Id
            JOIN   dbo.Sys_Table t ON t.Table_Id  = f.Table_Id
            LEFT JOIN dbo.Sys_Column sc ON sc.Column_Id = fi.Column_Id
            WHERE  fi.Form_Id = @FormId
              AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
            ORDER BY fi.Order_No
            """;

        using var conn = _db.CreateConnection();

        var fields = (await conn.QueryAsync<FieldMetadata>(
            new CommandDefinition(sql, new { FormId = formId, TenantId = tenantId },
                cancellationToken: ct))).AsList();

        // Gán LookupConfig cho các field dynamic
        await EnrichLookupConfigsAsync(conn, fields, ct);

        return fields;
    }

    /// <inheritdoc />
    public async Task<FieldMetadata?> GetByIdAsync(
        int fieldId, int tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT fi.Field_Id             AS FieldId,
                   fi.Form_Id              AS FormId,
                   fi.Section_Id           AS SectionId,
                   sc.Column_Code          AS FieldCode,
                   fi.Editor_Type          AS FieldType,
                   fi.Label_Key            AS Label,
                   fi.Order_No             AS SortOrder,
                   fi.Is_Visible           AS IsVisible,
                   fi.Is_ReadOnly          AS IsReadOnly,
                   fi.Is_Required          AS IsRequired,
                   fi.Is_Enabled           AS IsEnabled,
                   fi.Control_Props_Json   AS ControlPropsJson,
                   fi.Col_Span             AS ColSpan,
                   fi.Lookup_Source        AS LookupSource,
                   fi.Lookup_Code          AS LookupCode
            FROM   dbo.Ui_Field fi
            JOIN   dbo.Ui_Form f   ON f.Form_Id   = fi.Form_Id
            JOIN   dbo.Sys_Table t ON t.Table_Id  = f.Table_Id
            LEFT JOIN dbo.Sys_Column sc ON sc.Column_Id = fi.Column_Id
            WHERE  fi.Field_Id = @FieldId
              AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
            """;

        using var conn = _db.CreateConnection();

        var field = await conn.QueryFirstOrDefaultAsync<FieldMetadata>(
            new CommandDefinition(sql, new { FieldId = fieldId, TenantId = tenantId },
                cancellationToken: ct));

        if (field is null || field.LookupSource != "dynamic") return field;

        // Lấy lookup config cho field dynamic
        var cfg = await LoadLookupConfigAsync(conn, fieldId, ct);
        if (cfg is null) return field;

        return new FieldMetadata
        {
            FieldId = field.FieldId, FormId = field.FormId, SectionId = field.SectionId,
            TenantId = field.TenantId, FieldCode = field.FieldCode, FieldType = field.FieldType,
            Label = field.Label, ControlPropsJson = field.ControlPropsJson,
            DefaultValueJson = field.DefaultValueJson, IsVisible = field.IsVisible,
            IsReadOnly = field.IsReadOnly, IsRequired = field.IsRequired, IsEnabled = field.IsEnabled,
            SortOrder = field.SortOrder, ColSpan = field.ColSpan,
            LookupSource = field.LookupSource, LookupCode = field.LookupCode,
            LookupConfig = cfg
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FieldMetadata>> GetBySectionIdAsync(
        int sectionId, int tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT fi.Field_Id             AS FieldId,
                   fi.Form_Id              AS FormId,
                   fi.Section_Id           AS SectionId,
                   sc.Column_Code          AS FieldCode,
                   fi.Editor_Type          AS FieldType,
                   fi.Label_Key            AS Label,
                   fi.Order_No             AS SortOrder,
                   fi.Is_Visible           AS IsVisible,
                   fi.Is_ReadOnly          AS IsReadOnly,
                   fi.Is_Required          AS IsRequired,
                   fi.Is_Enabled           AS IsEnabled,
                   fi.Control_Props_Json   AS ControlPropsJson,
                   fi.Col_Span             AS ColSpan,
                   fi.Lookup_Source        AS LookupSource,
                   fi.Lookup_Code          AS LookupCode
            FROM   dbo.Ui_Field fi
            JOIN   dbo.Ui_Form f   ON f.Form_Id   = fi.Form_Id
            JOIN   dbo.Sys_Table t ON t.Table_Id  = f.Table_Id
            LEFT JOIN dbo.Sys_Column sc ON sc.Column_Id = fi.Column_Id
            WHERE  fi.Section_Id = @SectionId
              AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
            ORDER BY fi.Order_No
            """;

        using var conn = _db.CreateConnection();

        var fields = (await conn.QueryAsync<FieldMetadata>(
            new CommandDefinition(sql, new { SectionId = sectionId, TenantId = tenantId },
                cancellationToken: ct))).AsList();

        await EnrichLookupConfigsAsync(conn, fields, ct);

        return fields;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Truy vấn Ui_Field_Lookup cho tất cả dynamic fields trong danh sách,
    /// sau đó tạo lại instance FieldMetadata với LookupConfig được gán vào.
    /// </summary>
    private static async Task EnrichLookupConfigsAsync(
        System.Data.IDbConnection conn,
        List<FieldMetadata> fields,
        CancellationToken ct)
    {
        // Chỉ query khi có ít nhất 1 dynamic field
        var dynamicIds = fields
            .Where(f => f.LookupSource == "dynamic")
            .Select(f => f.FieldId)
            .ToList();

        if (dynamicIds.Count == 0) return;

        const string sql = """
            SELECT fl.Lookup_Cfg_Id      AS LookupCfgId,
                   fl.Field_Id           AS FieldId,
                   fl.Query_Mode         AS QueryMode,
                   fl.Source_Name        AS SourceName,
                   fl.Value_Column       AS ValueColumn,
                   fl.Display_Column     AS DisplayColumn,
                   fl.Filter_Sql         AS FilterSql,
                   fl.Order_By           AS OrderBy,
                   fl.Search_Enabled     AS SearchEnabled,
                   fl.Popup_Columns_Json AS PopupColumnsJson
            FROM   dbo.Ui_Field_Lookup fl
            WHERE  fl.Field_Id IN @FieldIds
            """;

        var configMap = (await conn.QueryAsync<FieldLookupConfig>(
            new CommandDefinition(sql, new { FieldIds = dynamicIds }, cancellationToken: ct)))
            .ToDictionary(c => c.FieldId);

        // Cập nhật in-place: thay thế phần tử có LookupConfig
        for (var i = 0; i < fields.Count; i++)
        {
            var f = fields[i];
            if (f.LookupSource == "dynamic" && configMap.TryGetValue(f.FieldId, out var cfg))
                fields[i] = new FieldMetadata
                {
                    FieldId = f.FieldId, FormId = f.FormId, SectionId = f.SectionId,
                    TenantId = f.TenantId, FieldCode = f.FieldCode, FieldType = f.FieldType,
                    Label = f.Label, ControlPropsJson = f.ControlPropsJson,
                    DefaultValueJson = f.DefaultValueJson, IsVisible = f.IsVisible,
                    IsReadOnly = f.IsReadOnly, IsRequired = f.IsRequired, IsEnabled = f.IsEnabled,
                    SortOrder = f.SortOrder, ColSpan = f.ColSpan,
                    LookupSource = f.LookupSource, LookupCode = f.LookupCode,
                    LookupConfig = cfg
                };
        }
    }

    /// <summary>Load FieldLookupConfig cho 1 field đơn. Trả null nếu không có.</summary>
    private static async Task<FieldLookupConfig?> LoadLookupConfigAsync(
        System.Data.IDbConnection conn, int fieldId, CancellationToken ct)
    {
        const string sql = """
            SELECT fl.Lookup_Cfg_Id      AS LookupCfgId,
                   fl.Field_Id           AS FieldId,
                   fl.Query_Mode         AS QueryMode,
                   fl.Source_Name        AS SourceName,
                   fl.Value_Column       AS ValueColumn,
                   fl.Display_Column     AS DisplayColumn,
                   fl.Filter_Sql         AS FilterSql,
                   fl.Order_By           AS OrderBy,
                   fl.Search_Enabled     AS SearchEnabled,
                   fl.Popup_Columns_Json AS PopupColumnsJson
            FROM   dbo.Ui_Field_Lookup fl
            WHERE  fl.Field_Id = @FieldId
            """;

        return await conn.QueryFirstOrDefaultAsync<FieldLookupConfig>(
            new CommandDefinition(sql, new { FieldId = fieldId }, cancellationToken: ct));
    }
}
