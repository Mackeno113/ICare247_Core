// File    : AuditLogRepository.cs
// Module  : System
// Layer   : Infrastructure
// Purpose : Dapper implementation của IAuditLogRepository — ghi và đọc Sys_Audit_Log.

using Dapper;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Repository cho <c>Sys_Audit_Log</c>.
/// Ghi log mọi thay đổi trên objects quan trọng (Form, Field, Rule,...).
/// </summary>
public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly IDbConnectionFactory _db;

    public AuditLogRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task InsertAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.Sys_Audit_Log
                   (Object_Type, Object_Id, Action, Changed_By, Changed_At,
                    Old_Value_Json, New_Value_Json, Correlation_Id)
            VALUES (@ObjectType, @ObjectId, @Action, @ChangedBy, GETDATE(),
                    @OldValueJson, @NewValueJson, @CorrelationId);
            """;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                entry.ObjectType,
                entry.ObjectId,
                entry.Action,
                entry.ChangedBy,
                entry.OldValueJson,
                entry.NewValueJson,
                entry.CorrelationId
            }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<AuditLogItem> Items, int TotalCount)> GetByObjectAsync(
        string objectType,
        int objectId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        const string sql = """
            -- Đếm tổng
            SELECT COUNT(*)
            FROM   dbo.Sys_Audit_Log
            WHERE  Object_Type = @ObjectType
              AND  Object_Id   = @ObjectId;

            -- Lấy danh sách có phân trang
            SELECT Audit_Id        AS AuditId,
                   Object_Type     AS ObjectType,
                   Object_Id       AS ObjectId,
                   Action,
                   Changed_By      AS ChangedBy,
                   Changed_At      AS ChangedAt,
                   Old_Value_Json   AS OldValueJson,
                   New_Value_Json   AS NewValueJson,
                   Correlation_Id   AS CorrelationId
            FROM   dbo.Sys_Audit_Log
            WHERE  Object_Type = @ObjectType
              AND  Object_Id   = @ObjectId
            ORDER BY Changed_At DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var param = new
        {
            ObjectType = objectType,
            ObjectId = objectId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        using var conn = _db.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(
            new CommandDefinition(sql, param, cancellationToken: ct));

        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<AuditLogItem>()).AsList();

        return (items, totalCount);
    }
}
