// File    : UserGridLayoutRepository.cs
// Module  : UserPreference
// Layer   : Infrastructure
// Purpose : Dapper impl IUserGridLayoutRepository trên Data DB (HT_NguoiDung_LuoiLayout).
//           Data DB per-tenant → connection đã xác định tenant, không truyền Tenant_Id.

using System.Data;
using Dapper;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <inheritdoc cref="IUserGridLayoutRepository" />
public sealed class UserGridLayoutRepository : IUserGridLayoutRepository
{
    private readonly IDataDbConnectionFactory _dataDb;

    public UserGridLayoutRepository(IDataDbConnectionFactory dataDb) => _dataDb = dataDb;

    /// <inheritdoc />
    public async Task<string?> GetAsync(
        long userId, string viewCode, string platform, CancellationToken ct = default)
    {
        using IDbConnection conn = _dataDb.CreateConnection();
        const string sql = """
            SELECT TOP 1 Layout_Json
            FROM   dbo.HT_NguoiDung_LuoiLayout
            WHERE  NguoiDung_Id = @UserId AND View_Code = @ViewCode AND Platform = @Platform
            """;
        return await conn.QueryFirstOrDefaultAsync<string?>(
            new CommandDefinition(sql,
                new { UserId = userId, ViewCode = viewCode, Platform = platform },
                cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task UpsertAsync(
        long userId, string viewCode, string platform, string layoutJson, CancellationToken ct = default)
    {
        using IDbConnection conn = _dataDb.CreateConnection();
        // MERGE theo bộ khóa unique (NguoiDung_Id, View_Code, Platform).
        const string sql = """
            MERGE dbo.HT_NguoiDung_LuoiLayout AS t
            USING (SELECT @UserId AS NguoiDung_Id, @ViewCode AS View_Code, @Platform AS Platform) AS s
              ON (t.NguoiDung_Id = s.NguoiDung_Id AND t.View_Code = s.View_Code AND t.Platform = s.Platform)
            WHEN MATCHED THEN
                UPDATE SET Layout_Json = @LayoutJson, UpdatedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT (NguoiDung_Id, View_Code, Platform, Layout_Json)
                VALUES (@UserId, @ViewCode, @Platform, @LayoutJson);
            """;
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { UserId = userId, ViewCode = viewCode, Platform = platform, LayoutJson = layoutJson },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        long userId, string viewCode, string platform, CancellationToken ct = default)
    {
        using IDbConnection conn = _dataDb.CreateConnection();
        const string sql = """
            DELETE FROM dbo.HT_NguoiDung_LuoiLayout
            WHERE  NguoiDung_Id = @UserId AND View_Code = @ViewCode AND Platform = @Platform
            """;
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { UserId = userId, ViewCode = viewCode, Platform = platform },
            cancellationToken: ct));
    }
}
