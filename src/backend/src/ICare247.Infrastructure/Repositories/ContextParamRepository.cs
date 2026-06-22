// File    : ContextParamRepository.cs
// Module  : Context
// Layer   : Infrastructure
// Purpose : Dapper đọc Sys_Context_Param (Config DB) — registry token ngữ cảnh đang bật.

using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Context;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Đọc <c>Sys_Context_Param</c> (Config DB) — token <c>Is_Active=1</c>.</summary>
public sealed class ContextParamRepository : IContextParamRepository
{
    private readonly IDbConnectionFactory _db;

    public ContextParamRepository(IDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContextParam>> GetActiveAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT Param_Id      AS ParamId,
                   Param_Name    AS ParamName,
                   Sql_Type      AS SqlType,
                   Source_Kind   AS SourceKind,
                   Source_Key    AS SourceKey,
                   Validate_Sql  AS ValidateSql,
                   Default_Value AS DefaultValue,
                   Description,
                   Is_System     AS IsSystem
            FROM   dbo.Sys_Context_Param
            WHERE  Is_Active = 1
            """;

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<ContextParam>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }
}
