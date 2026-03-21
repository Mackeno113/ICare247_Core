// File    : RuleRepository.cs
// Module  : Validation
// Layer   : Infrastructure
// Purpose : Dapper implementation của IRuleRepository — đọc Val_Rule.
//           Sau Migration 003: Field_Id nằm trực tiếp trong Val_Rule,
//           không còn JOIN bảng junction Val_Rule_Field.

using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Rule;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Repository cho <c>Val_Rule</c>.
/// Tenant resolve qua Ui_Field → Ui_Form → Sys_Table.Tenant_Id.
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
        // Query thẳng Val_Rule theo Field_Id — không cần JOIN junction
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
                   r.Order_No         AS SortOrder
            FROM   dbo.Val_Rule r
            JOIN   dbo.Ui_Field fi     ON fi.Field_Id = r.Field_Id
            JOIN   dbo.Ui_Form f       ON f.Form_Id   = fi.Form_Id
            JOIN   dbo.Sys_Table st    ON st.Table_Id  = f.Table_Id
            WHERE  fi.Form_Id    = @FormId
              AND  fi.Field_Code = @FieldCode
              AND  st.Tenant_Id  = @TenantId
              AND  r.Is_Active   = 1
            ORDER BY r.Order_No
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
        // Lấy toàn bộ rules của form — group theo FieldCode
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
                   r.Order_No         AS SortOrder
            FROM   dbo.Val_Rule r
            JOIN   dbo.Ui_Field fi     ON fi.Field_Id = r.Field_Id
            JOIN   dbo.Ui_Form f       ON f.Form_Id   = fi.Form_Id
            JOIN   dbo.Sys_Table st    ON st.Table_Id  = f.Table_Id
            WHERE  fi.Form_Id   = @FormId
              AND  st.Tenant_Id = @TenantId
              AND  r.Is_Active  = 1
            ORDER BY fi.Field_Code, r.Order_No
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
