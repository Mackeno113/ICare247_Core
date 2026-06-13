// File    : RefreshTokenRepository.cs
// Module  : Auth
// Layer   : Infrastructure
// Purpose : Dapper implementation IRefreshTokenRepository — quản lý HT_RefreshToken trong Data DB tenant.

using Dapper;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Lưu trữ vòng đời refresh token trong Data DB của tenant. Chỉ lưu hash (TokenHash),
/// không lưu token gốc. CreatedBy = chính người dùng (cột audit NOT NULL).
/// </summary>
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDataDbConnectionFactory _db;

    public RefreshTokenRepository(IDataDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task InsertAsync(long userId, string tokenHash, DateTime expiresAtUtc,
        string? ipAddress, string? device, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.HT_RefreshToken
                (NguoiDung_Id, TokenHash, HetHan, DaThuHoi, DiaChiIp, ThietBi, CreatedBy, CreatedAt)
            VALUES
                (@UserId, @TokenHash, @HetHan, 0, @Ip, @Device, @UserId, SYSUTCDATETIME())
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            UserId = userId,
            TokenHash = tokenHash,
            HetHan = expiresAtUtc,
            Ip = ipAddress,
            Device = device
        }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<RefreshTokenRecord?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id,
                   NguoiDung_Id AS NguoiDungId,
                   HetHan       AS HetHanUtc,
                   DaThuHoi
            FROM dbo.HT_RefreshToken
            WHERE TokenHash = @TokenHash AND IsDeleted = 0
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<RefreshTokenRecord>(
            new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task RevokeAsync(long tokenId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.HT_RefreshToken
            SET DaThuHoi  = 1,
                ThuHoiLuc = SYSUTCDATETIME(),
                UpdatedBy = NguoiDung_Id,
                UpdatedAt = SYSUTCDATETIME(),
                Ver       = Ver + 1
            WHERE Id = @Id AND DaThuHoi = 0
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = tokenId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task RevokeAllForUserAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.HT_RefreshToken
            SET DaThuHoi  = 1,
                ThuHoiLuc = SYSUTCDATETIME(),
                UpdatedBy = @UserId,
                UpdatedAt = SYSUTCDATETIME(),
                Ver       = Ver + 1
            WHERE NguoiDung_Id = @UserId AND DaThuHoi = 0 AND IsDeleted = 0
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
    }
}
