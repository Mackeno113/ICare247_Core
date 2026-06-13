// File    : AuthRepository.cs
// Module  : Auth
// Layer   : Infrastructure
// Purpose : Dapper implementation IAuthRepository — đọc/ghi HT_NguoiDung + vai trò trong Data DB tenant.

using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Auth;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Truy cập dữ liệu xác thực người dùng trong Data DB của tenant hiện tại
/// (connection lấy qua <see cref="IDataDbConnectionFactory"/>). Parameterized 100%.
/// </summary>
public sealed class AuthRepository : IAuthRepository
{
    private readonly IDataDbConnectionFactory _db;

    public AuthRepository(IDataDbConnectionFactory db) => _db = db;

    /// <summary>Danh sách cột auth cần đọc — KHÔNG dùng SELECT *.</summary>
    private const string SelectColumns = """
        Id, Ma, TenDangNhap, LoaiTaiKhoan, MatKhauHash, CongTyMacDinh_Id,
        TrangThai, LaQuanTri, HetHanTaiKhoan, HinhThuc2FA,
        SoLanDangNhapSai, KhoaDenKhi, DoiMatKhauLanSau, IsDeleted
        """;

    /// <inheritdoc />
    public async Task<NguoiDung?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var sql = $"SELECT {SelectColumns} FROM dbo.HT_NguoiDung WHERE TenDangNhap = @Username AND IsDeleted = 0";
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<NguoiDung>(
            new CommandDefinition(sql, new { Username = username }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<NguoiDung?> GetByIdAsync(long userId, CancellationToken ct = default)
    {
        var sql = $"SELECT {SelectColumns} FROM dbo.HT_NguoiDung WHERE Id = @Id AND IsDeleted = 0";
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<NguoiDung>(
            new CommandDefinition(sql, new { Id = userId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetRoleCodesAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT vt.Ma
            FROM dbo.HT_NguoiDung_VaiTro ndvt
            JOIN dbo.HT_VaiTro vt ON vt.Id = ndvt.VaiTro_Id AND vt.IsDeleted = 0
            WHERE ndvt.NguoiDung_Id = @UserId AND ndvt.IsDeleted = 0
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<string>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task RecordLoginSuccessAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.HT_NguoiDung
            SET SoLanDangNhapSai = 0,
                KhoaDenKhi       = NULL,
                LanDangNhapCuoi  = SYSUTCDATETIME(),
                UpdatedBy        = @UserId,
                UpdatedAt        = SYSUTCDATETIME(),
                Ver              = Ver + 1
            WHERE Id = @UserId
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task RecordLoginFailureAsync(long userId, int newFailCount, DateTime? lockUntilUtc,
        CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.HT_NguoiDung
            SET SoLanDangNhapSai = @Count,
                KhoaDenKhi       = @LockUntil,
                UpdatedBy        = @UserId,
                UpdatedAt        = SYSUTCDATETIME(),
                Ver              = Ver + 1
            WHERE Id = @UserId
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { UserId = userId, Count = newFailCount, LockUntil = lockUntilUtc }, cancellationToken: ct));
    }
}
