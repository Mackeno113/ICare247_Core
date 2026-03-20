// File    : RuleRepository.cs
// Module  : Validation
// Layer   : Infrastructure
// Purpose : Dapper implementation của IRuleRepository — đọc Val_Rule + Val_Rule_Field.

using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Rule;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Repository cho <c>Val_Rule</c> + <c>Val_Rule_Field</c>.
/// Tenant resolve qua Form → Sys_Table.Tenant_Id.
/// </summary>
public sealed class RuleRepository : IRuleRepository
{
    private readonly IDbConnectionFactory _db;

    public RuleRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuleMetadata>> GetByFieldAsync(
        int formId, string fieldCode, int tenantId,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT r.Rule_Id          AS RuleId,
                   f.Form_Id          AS FormId,
                   fi.Field_Code      AS FieldCode,
                   st.Tenant_Id       AS TenantId,
                   r.Rule_Type_Code   AS RuleType,
                   COALESCE(r.Severity, 'error') AS Severity,
                   r.Expression_Json  AS ExpressionJson,
                   r.Condition_Expr   AS ConditionExpr,
                   r.Error_Key        AS ErrorMessage,
                   rf.Order_No        AS SortOrder
            FROM   dbo.Val_Rule_Field rf
            JOIN   dbo.Val_Rule r      ON r.Rule_Id = rf.Rule_Id
            JOIN   dbo.Ui_Field fi     ON fi.Field_Id = rf.Field_Id
            JOIN   dbo.Ui_Form f       ON f.Form_Id = fi.Form_Id
            JOIN   dbo.Sys_Table st    ON st.Table_Id = f.Table_Id
            WHERE  fi.Form_Id = @FormId
              AND  fi.Field_Code = @FieldCode
              AND  st.Tenant_Id = @TenantId
              AND  r.Is_Active = 1
            ORDER BY rf.Order_No
            """;

        using var conn = _db.CreateConnection();
        var cmd = new CommandDefinition(sql,
            new { FormId = formId, FieldCode = fieldCode, TenantId = tenantId },
            cancellationToken: ct);
        var results = await conn.QueryAsync<RuleMetadata>(cmd);
        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<RuleMetadata>>> GetByFormAsync(
        int formId, int tenantId,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT r.Rule_Id          AS RuleId,
                   f.Form_Id          AS FormId,
                   fi.Field_Code      AS FieldCode,
                   st.Tenant_Id       AS TenantId,
                   r.Rule_Type_Code   AS RuleType,
                   COALESCE(r.Severity, 'error') AS Severity,
                   r.Expression_Json  AS ExpressionJson,
                   r.Condition_Expr   AS ConditionExpr,
                   r.Error_Key        AS ErrorMessage,
                   rf.Order_No        AS SortOrder
            FROM   dbo.Val_Rule_Field rf
            JOIN   dbo.Val_Rule r      ON r.Rule_Id = rf.Rule_Id
            JOIN   dbo.Ui_Field fi     ON fi.Field_Id = rf.Field_Id
            JOIN   dbo.Ui_Form f       ON f.Form_Id = fi.Form_Id
            JOIN   dbo.Sys_Table st    ON st.Table_Id = f.Table_Id
            WHERE  fi.Form_Id = @FormId
              AND  st.Tenant_Id = @TenantId
              AND  r.Is_Active = 1
            ORDER BY fi.Field_Code, rf.Order_No
            """;

        using var conn = _db.CreateConnection();
        var cmd = new CommandDefinition(sql,
            new { FormId = formId, TenantId = tenantId },
            cancellationToken: ct);
        var results = await conn.QueryAsync<RuleMetadata>(cmd);

        // Group theo FieldCode
        var grouped = results
            .GroupBy(r => r.FieldCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<RuleMetadata>)g.ToList(),
                StringComparer.OrdinalIgnoreCase);

        return grouped;
    }
}
