// File    : DependencyRepository.cs
// Module  : Validation
// Layer   : Infrastructure
// Purpose : Dapper implementation của IDependencyRepository — đọc Sys_Dependency cho topological sort.

using Dapper;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Repository cho <c>Sys_Dependency</c>.
/// Chỉ đọc dependency giữa fields (Source_Type = 'Field', Target_Type = 'Field').
/// </summary>
public sealed class DependencyRepository : IDependencyRepository
{
    private readonly IDbConnectionFactory _db;

    public DependencyRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FieldDependency>> GetByFormAsync(
        int formId, int tenantId,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT src.Field_Code  AS SourceFieldCode,
                   tgt.Field_Code  AS TargetFieldCode
            FROM   dbo.Sys_Dependency d
            JOIN   dbo.Ui_Field src ON src.Field_Id = d.Source_Id
            JOIN   dbo.Ui_Field tgt ON tgt.Field_Id = d.Target_Id
            JOIN   dbo.Ui_Form f    ON f.Form_Id = d.Form_Id
            JOIN   dbo.Sys_Table st ON st.Table_Id = f.Table_Id
            WHERE  d.Form_Id = @FormId
              AND  d.Source_Type = 'Field'
              AND  d.Target_Type = 'Field'
              AND  d.Is_Active = 1
              AND  st.Tenant_Id = @TenantId
            """;

        using var conn = _db.CreateConnection();
        var cmd = new CommandDefinition(sql,
            new { FormId = formId, TenantId = tenantId },
            cancellationToken: ct);
        var results = await conn.QueryAsync<FieldDependency>(cmd);
        return results.ToList();
    }
}
