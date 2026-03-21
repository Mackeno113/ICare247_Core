// File    : RuleDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Dapper implementation cho IRuleDataService — Val_Rule.
//           Sau Migration 003: Field_Id nằm trực tiếp trong Val_Rule,
//           không còn bảng junction Val_Rule_Field.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// CRUD validation rules trực tiếp trên dbo.Val_Rule.
/// </summary>
public sealed class RuleDataService : IRuleDataService
{
    private readonly IAppConfigService _config;

    public RuleDataService(IAppConfigService config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuleItemRecord>> GetRulesByFieldAsync(
        int fieldId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        // Query thẳng Val_Rule theo Field_Id — không cần JOIN junction nữa
        const string sql = """
            SELECT Rule_Id         AS RuleId,
                   Field_Id        AS FieldId,
                   Rule_Type_Code  AS RuleTypeCode,
                   Order_No        AS OrderNo,
                   Expression_Json AS ExpressionJson,
                   Error_Key       AS ErrorKey,
                   Severity,
                   Is_Active       AS IsActive
            FROM   dbo.Val_Rule
            WHERE  Field_Id = @FieldId
            ORDER BY Order_No
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<RuleItemRecord>(
            new CommandDefinition(sql, new { FieldId = fieldId }, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuleTypeRecord>> GetRuleTypesAsync(CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT Rule_Type_Code AS RuleTypeCode,
                   Param_Schema   AS ParamSchema
            FROM   dbo.Val_Rule_Type
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<RuleTypeRecord>(
            new CommandDefinition(sql, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<int> SaveRuleAsync(RuleItemRecord rule, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return 0;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync(ct);

        if (rule.RuleId == 0)
        {
            // Thêm mới — INSERT INTO Val_Rule
            const string sqlInsert = """
                INSERT INTO dbo.Val_Rule
                       (Field_Id, Rule_Type_Code, Error_Key, Severity,
                        Expression_Json, Order_No, Is_Active, Updated_At)
                OUTPUT INSERTED.Rule_Id
                VALUES (@FieldId, @RuleTypeCode, @ErrorKey, @Severity,
                        @ExpressionJson, @OrderNo, 1, GETDATE())
                """;

            return await conn.QuerySingleAsync<int>(
                new CommandDefinition(sqlInsert, rule, cancellationToken: ct));
        }
        else
        {
            // Cập nhật — UPDATE theo Rule_Id + Field_Id (safety check)
            const string sqlUpdate = """
                UPDATE dbo.Val_Rule
                SET    Rule_Type_Code  = @RuleTypeCode,
                       Error_Key       = @ErrorKey,
                       Severity        = @Severity,
                       Expression_Json = @ExpressionJson,
                       Order_No        = @OrderNo,
                       Updated_At      = GETDATE()
                WHERE  Rule_Id  = @RuleId
                  AND  Field_Id = @FieldId
                """;

            await conn.ExecuteAsync(
                new CommandDefinition(sqlUpdate, rule, cancellationToken: ct));
            return rule.RuleId;
        }
    }

    /// <inheritdoc />
    public async Task DeleteRuleAsync(int ruleId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        // Xóa trực tiếp Val_Rule — không còn junction table
        const string sql = "DELETE FROM dbo.Val_Rule WHERE Rule_Id = @RuleId";

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { RuleId = ruleId }, cancellationToken: ct));
    }
}
