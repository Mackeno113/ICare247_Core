// File    : FieldRepository.cs
// Module  : Form
// Layer   : Infrastructure
// Purpose : Dapper implementation của IFieldRepository — đọc Ui_Field.

using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Form;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Repository cho <c>Ui_Field</c>.
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
        const string sql = """
            SELECT fi.Field_Id          AS FieldId,
                   fi.Form_Id           AS FormId,
                   fi.Section_Id        AS SectionId,
                   fi.Editor_Type       AS FieldType,
                   fi.Label_Key         AS Label,
                   fi.Order_No          AS SortOrder,
                   fi.Control_Props_Json AS DefaultValueJson
            FROM   dbo.Ui_Field fi
            JOIN   dbo.Ui_Form f ON f.Form_Id = fi.Form_Id
            JOIN   dbo.Sys_Table t ON t.Table_Id = f.Table_Id
            WHERE  fi.Form_Id = @FormId
              AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
            ORDER BY fi.Order_No
            """;

        using var conn = _db.CreateConnection();
        var items = await conn.QueryAsync<FieldMetadata>(
            new CommandDefinition(sql, new { FormId = formId, TenantId = tenantId },
                cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<FieldMetadata?> GetByIdAsync(
        int fieldId, int tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT fi.Field_Id          AS FieldId,
                   fi.Form_Id           AS FormId,
                   fi.Section_Id        AS SectionId,
                   fi.Editor_Type       AS FieldType,
                   fi.Label_Key         AS Label,
                   fi.Order_No          AS SortOrder,
                   fi.Control_Props_Json AS DefaultValueJson
            FROM   dbo.Ui_Field fi
            JOIN   dbo.Ui_Form f ON f.Form_Id = fi.Form_Id
            JOIN   dbo.Sys_Table t ON t.Table_Id = f.Table_Id
            WHERE  fi.Field_Id = @FieldId
              AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
            """;

        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<FieldMetadata>(
            new CommandDefinition(sql, new { FieldId = fieldId, TenantId = tenantId },
                cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FieldMetadata>> GetBySectionIdAsync(
        int sectionId, int tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT fi.Field_Id          AS FieldId,
                   fi.Form_Id           AS FormId,
                   fi.Section_Id        AS SectionId,
                   fi.Editor_Type       AS FieldType,
                   fi.Label_Key         AS Label,
                   fi.Order_No          AS SortOrder,
                   fi.Control_Props_Json AS DefaultValueJson
            FROM   dbo.Ui_Field fi
            JOIN   dbo.Ui_Form f ON f.Form_Id = fi.Form_Id
            JOIN   dbo.Sys_Table t ON t.Table_Id = f.Table_Id
            WHERE  fi.Section_Id = @SectionId
              AND  (t.Tenant_Id = @TenantId OR t.Tenant_Id IS NULL)
            ORDER BY fi.Order_No
            """;

        using var conn = _db.CreateConnection();
        var items = await conn.QueryAsync<FieldMetadata>(
            new CommandDefinition(sql, new { SectionId = sectionId, TenantId = tenantId },
                cancellationToken: ct));
        return items.AsList();
    }
}
