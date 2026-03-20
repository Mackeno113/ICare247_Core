// File    : GrammarDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Dapper implementation cho IGrammarDataService — Gram_Function + Gram_Operator.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// CRUD grammar functions + operators. Global data — không filter Tenant_Id.
/// </summary>
public sealed class GrammarDataService : IGrammarDataService
{
    private readonly IAppConfigService _config;

    public GrammarDataService(IAppConfigService config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FunctionRecord>> GetFunctionsAsync(CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT Function_Id     AS FunctionId,
                   Function_Code   AS FunctionCode,
                   Description,
                   Return_Net_Type AS ReturnNetType,
                   Param_Count_Min AS ParamCountMin,
                   Param_Count_Max AS ParamCountMax,
                   Is_System       AS IsSystem,
                   Is_Active       AS IsActive
            FROM   dbo.Gram_Function
            WHERE  Is_Active = 1
            ORDER BY Function_Code
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<FunctionRecord>(
            new CommandDefinition(sql, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OperatorRecord>> GetOperatorsAsync(CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        const string sql = """
            SELECT Operator_Symbol AS OperatorSymbol,
                   Operator_Type   AS OperatorType,
                   Precedence,
                   Description,
                   Is_Active       AS IsActive
            FROM   dbo.Gram_Operator
            WHERE  Is_Active = 1
            ORDER BY Precedence DESC, Operator_Symbol
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        var items = await conn.QueryAsync<OperatorRecord>(
            new CommandDefinition(sql, cancellationToken: ct));
        return items.AsList();
    }

    /// <inheritdoc />
    public async Task SaveFunctionAsync(FunctionRecord func, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        // MERGE: insert nếu chưa có, update nếu đã có
        const string sql = """
            IF @FunctionId = 0
                INSERT INTO dbo.Gram_Function
                       (Function_Code, Description, Return_Net_Type, Param_Count_Min, Param_Count_Max, Is_System, Is_Active)
                VALUES (@FunctionCode, @Description, @ReturnNetType, @ParamCountMin, @ParamCountMax, @IsSystem, 1)
            ELSE
                UPDATE dbo.Gram_Function
                SET    Function_Code   = @FunctionCode,
                       Description     = @Description,
                       Return_Net_Type = @ReturnNetType,
                       Param_Count_Min = @ParamCountMin,
                       Param_Count_Max = @ParamCountMax
                WHERE  Function_Id = @FunctionId
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, func, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task SaveOperatorAsync(OperatorRecord op, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        const string sql = """
            IF EXISTS (SELECT 1 FROM dbo.Gram_Operator WHERE Operator_Symbol = @OperatorSymbol)
                UPDATE dbo.Gram_Operator
                SET    Operator_Type = @OperatorType,
                       Precedence    = @Precedence,
                       Description   = @Description
                WHERE  Operator_Symbol = @OperatorSymbol
            ELSE
                INSERT INTO dbo.Gram_Operator
                       (Operator_Symbol, Operator_Type, Precedence, Description, Is_Active)
                VALUES (@OperatorSymbol, @OperatorType, @Precedence, @Description, 1)
            """;

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, op, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteFunctionAsync(int functionId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        const string sql = "UPDATE dbo.Gram_Function SET Is_Active = 0 WHERE Function_Id = @Id";

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = functionId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteOperatorAsync(string operatorSymbol, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return;

        const string sql = "UPDATE dbo.Gram_Operator SET Is_Active = 0 WHERE Operator_Symbol = @Symbol";

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Symbol = operatorSymbol }, cancellationToken: ct));
    }
}
