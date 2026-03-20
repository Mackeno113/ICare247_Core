// File    : RuleDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Dapper implementation cho IRuleDataService — Val_Rule + Val_Rule_Field.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// CRUD validation rules. Delete chỉ unlink (Val_Rule_Field), không xóa Val_Rule.
/// </summary>
public sealed class RuleDataService : IRuleDataService
{
    private readonly IAppConfigService _config;

    public RuleDataService(IAppConfigService config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuleItemRecord>> GetRulesByFieldAsync(int fieldId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT vr.Rule_Id        AS RuleId,
                   vr.Rule_Type_Code AS RuleTypeCode,
                   vrf.Order_No      AS OrderNo,
                   vr.Expression_Json AS ExpressionJson,
                   vr.Error_Key      AS ErrorKey,
                   vr.Is_Active      AS IsActive
            FROM   dbo.Val_Rule vr
            JOIN   dbo.Val_Rule_Field vrf ON vrf.Rule_Id = vr.Rule_Id
            WHERE  vrf.Field_Id = @FieldId
            ORDER BY vrf.Order_No
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
    public async Task<int> SaveRuleAsync(RuleItemRecord rule, int fieldId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return 0;

        await using var conn = new SqlConnection(_config.ConnectionString);
        conn.Open();

        int ruleId;

        if (rule.RuleId == 0)
        {
            // Tạo rule mới
            const string sqlInsert = """
                INSERT INTO dbo.Val_Rule
                       (Rule_Type_Code, Error_Key, Expression_Json, Is_Active, Updated_At)
                OUTPUT INSERTED.Rule_Id
                VALUES (@RuleTypeCode, @ErrorKey, @ExpressionJson, 1, GETDATE())
                """;

            ruleId = await conn.QuerySingleAsync<int>(
                new CommandDefinition(sqlInsert, rule, cancellationToken: ct));

            // Liên kết với field
            const string sqlLink = """
                INSERT INTO dbo.Val_Rule_Field (Field_Id, Rule_Id, Order_No)
                VALUES (@FieldId, @RuleId, @OrderNo)
                """;

            await conn.ExecuteAsync(
                new CommandDefinition(sqlLink, new { FieldId = fieldId, RuleId = ruleId, rule.OrderNo }, cancellationToken: ct));
        }
        else
        {
            ruleId = rule.RuleId;

            const string sqlUpdate = """
                UPDATE dbo.Val_Rule
                SET    Rule_Type_Code  = @RuleTypeCode,
                       Error_Key       = @ErrorKey,
                       Expression_Json = @ExpressionJson,
                       Updated_At      = GETDATE()
                WHERE  Rule_Id = @RuleId
                """;

            await conn.ExecuteAsync(
                new CommandDefinition(sqlUpdate, rule, cancellationToken: ct));
        }

        return ruleId;
    }

    /// <inheritdoc />
    public async Task DeleteRuleFieldAsync(int fieldId, int ruleId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        // Chỉ unlink — không xóa Val_Rule (rule có thể dùng chung nhiều fields)
        const string sql = "DELETE FROM dbo.Val_Rule_Field WHERE Field_Id = @FieldId AND Rule_Id = @RuleId";

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { FieldId = fieldId, RuleId = ruleId }, cancellationToken: ct));
    }
}
