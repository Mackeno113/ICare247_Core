// File    : MenuAdminRepository.cs
// Module  : Admin/Menu
// Layer   : Infrastructure
// Purpose : Dapper impl IMenuAdminRepository — đọc/ghi cây menu HT_ChucNang ở Data DB tenant.
//           Parameterized 100%, không SELECT *. Node tạo từ đây: LaHeThong=0, ViTriHienThi='Sidebar'.

using Dapper;
using ICare247.Application.Features.Admin.Menu;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Ghi/đọc node menu (HT_ChucNang) cho Menu Builder — chỉ chạm Data DB tenant.</summary>
public sealed class MenuAdminRepository : IMenuAdminRepository
{
    private readonly IDataDbConnectionFactory _db;

    public MenuAdminRepository(IDataDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<MenuNodeDto>> GetTreeAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT  c.Id              AS Id,
                    c.Ma              AS Ma,
                    c.Ten             AS Ten,
                    c.ChucNang_Cha_Id AS ChaId,
                    c.Loai            AS Loai,
                    c.Module          AS Module,
                    c.DuongDan        AS DuongDan,
                    c.Icon            AS Icon,
                    c.ThuTu           AS ThuTu,
                    c.KichHoat        AS KichHoat,
                    c.DoiTuong        AS DoiTuong,
                    c.LoaiDoiTuong    AS LoaiDoiTuong,
                    c.LaHeThong       AS LaHeThong
            FROM    dbo.HT_ChucNang c
            WHERE   c.IsDeleted = 0
            ORDER BY ISNULL(c.ChucNang_Cha_Id, 0), c.ThuTu, c.Id;
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<MenuNodeDto>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<long> InsertAsync(MenuNodeWrite n, long userId, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.HT_ChucNang
                (Ma, Ten, ChucNang_Cha_Id, Loai, Module, DuongDan, Icon, ThuTu,
                 Menu_Id, LaHeThong, KichHoat, ViTriHienThi, DoiTuong, LoaiDoiTuong,
                 CreatedBy, CreatedAt, IsDeleted, Ver)
            OUTPUT INSERTED.Id
            VALUES
                (@Ma, @Ten, @ChaId, @Loai, @Module, @DuongDan, @Icon, @ThuTu,
                 NULL, 0, @KichHoat, N'Sidebar', @DoiTuong, @LoaiDoiTuong,
                 @UserId, SYSUTCDATETIME(), 0, 0);
            """;
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            sql,
            new
            {
                n.Ma, n.Ten, n.ChaId, n.Loai, n.Module, n.DuongDan, n.Icon, n.ThuTu,
                n.KichHoat, n.DoiTuong, n.LoaiDoiTuong, UserId = userId
            }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(long id, MenuNodeWrite n, long userId, CancellationToken ct = default)
    {
        // Không đổi Ma/LaHeThong; tăng Ver. Chỉ sửa node chưa xóa.
        const string sql = """
            UPDATE dbo.HT_ChucNang
            SET    Ten = @Ten, ChucNang_Cha_Id = @ChaId, Loai = @Loai, Module = @Module,
                   DuongDan = @DuongDan, Icon = @Icon, ThuTu = @ThuTu, KichHoat = @KichHoat,
                   DoiTuong = @DoiTuong, LoaiDoiTuong = @LoaiDoiTuong,
                   UpdatedBy = @UserId, UpdatedAt = SYSUTCDATETIME(), Ver = Ver + 1
            WHERE  Id = @Id AND IsDeleted = 0;
            """;
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = id, n.Ten, n.ChaId, n.Loai, n.Module, n.DuongDan, n.Icon, n.ThuTu,
                n.KichHoat, n.DoiTuong, n.LoaiDoiTuong, UserId = userId
            }, cancellationToken: ct));
        return affected > 0;
    }

    /// <inheritdoc />
    public async Task<bool> SoftDeleteAsync(long id, long userId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.HT_ChucNang
            SET    IsDeleted = 1, UpdatedBy = @UserId, UpdatedAt = SYSUTCDATETIME()
            WHERE  Id = @Id AND IsDeleted = 0;
            """;
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id, UserId = userId }, cancellationToken: ct));
        return affected > 0;
    }

    /// <inheritdoc />
    public async Task<int> CountActiveChildrenAsync(long id, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.HT_ChucNang WHERE ChucNang_Cha_Id = @Id AND IsDeleted = 0;";
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool?> IsSystemNodeAsync(long id, CancellationToken ct = default)
    {
        const string sql = "SELECT LaHeThong FROM dbo.HT_ChucNang WHERE Id = @Id AND IsDeleted = 0;";
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<bool?>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> MaExistsAsync(string ma, CancellationToken ct = default)
    {
        const string sql = "SELECT 1 FROM dbo.HT_ChucNang WHERE Ma = @Ma AND IsDeleted = 0;";
        using var conn = _db.CreateConnection();
        var hit = await conn.ExecuteScalarAsync<int?>(new CommandDefinition(sql, new { Ma = ma }, cancellationToken: ct));
        return hit.HasValue;
    }

    /// <inheritdoc />
    public async Task<bool> IsDescendantAsync(long nodeId, long parentId, CancellationToken ct = default)
    {
        // Leo tổ tiên từ parentId; nếu gặp nodeId → parentId nằm trong nhánh con của nodeId → tạo vòng lặp.
        const string sql = """
            WITH Ancestors AS (
                SELECT Id, ChucNang_Cha_Id
                FROM   dbo.HT_ChucNang
                WHERE  Id = @ParentId AND IsDeleted = 0
                UNION ALL
                SELECT p.Id, p.ChucNang_Cha_Id
                FROM   dbo.HT_ChucNang p
                JOIN   Ancestors a ON p.Id = a.ChucNang_Cha_Id
                WHERE  p.IsDeleted = 0
            )
            SELECT CASE WHEN EXISTS (SELECT 1 FROM Ancestors WHERE Id = @NodeId) THEN 1 ELSE 0 END;
            """;
        using var conn = _db.CreateConnection();
        var hit = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            sql, new { NodeId = nodeId, ParentId = parentId }, cancellationToken: ct));
        return hit == 1;
    }
}
