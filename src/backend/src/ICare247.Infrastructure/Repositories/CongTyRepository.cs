// File    : CongTyRepository.cs
// Module  : Organization / Companies
// Layer   : Infrastructure
// Purpose : Dapper impl ICongTyRepository — đọc/ghi TC_CongTy + lookup (TC_CapCongTy, DM_NganHang,
//           DM_PhuongXa) trong Data DB tenant. Connection lấy qua IDataDbConnectionFactory (scoped,
//           tenant-aware). Parameterized 100%, KHÔNG SELECT *.

using Dapper;
using ICare247.Application.Features.Organization.Companies.Models;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Truy cập cây công ty + danh mục liên quan trong Data DB của tenant hiện tại.</summary>
public sealed class CongTyRepository : ICongTyRepository
{
    private readonly IDataDbConnectionFactory _db;

    public CongTyRepository(IDataDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<CompanyTreeNodeDto>> GetTreeAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT c.Id            AS Id,
                   c.CongTy_Cha_Id AS ChaId,
                   c.Ma            AS Ma,
                   c.Ten           AS Ten,
                   c.TenVietTat    AS TenVietTat,
                   cap.Ten         AS CapTen,
                   c.MaSoThue      AS MaSoThue,
                   c.TrangThai     AS TrangThai
            FROM dbo.TC_CongTy c
            LEFT JOIN dbo.TC_CapCongTy cap ON cap.Id = c.CapCongTy_Id
            WHERE c.IsDeleted = 0
            ORDER BY c.Ten;
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<CompanyTreeNodeDto>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<CompanyDetailDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT c.Id            AS Id,
                   c.Ma            AS Ma,
                   c.Ten           AS Ten,
                   c.TenVietTat    AS TenVietTat,
                   c.CongTy_Cha_Id AS CongTyChaId,
                   c.CapCongTy_Id  AS CapCongTyId,
                   c.MaSoThue      AS MaSoThue,
                   c.DiaChi        AS DiaChi,
                   c.PhuongXa_Id   AS PhuongXaId,
                   c.DienThoai     AS DienThoai,
                   c.Email         AS Email,
                   c.Website       AS Website,
                   c.NguoiDaiDien  AS NguoiDaiDien,
                   c.GiamDoc       AS GiamDoc,
                   c.KeToanTruong  AS KeToanTruong,
                   c.NganHang_Id   AS NganHangId,
                   c.SoTaiKhoan    AS SoTaiKhoan,
                   c.Logo_Id       AS LogoId,
                   c.TrangThai     AS TrangThai,
                   cap.Ten         AS CapTen,
                   px.Ten          AS PhuongXaTen,
                   tt.Ten          AS TinhTen,
                   nh.Ten          AS NganHangTen
            FROM dbo.TC_CongTy c
            LEFT JOIN dbo.TC_CapCongTy   cap ON cap.Id = c.CapCongTy_Id
            LEFT JOIN dbo.DM_PhuongXa    px  ON px.Id  = c.PhuongXa_Id
            LEFT JOIN dbo.DM_TinhThanhPho tt ON tt.Id  = px.TinhThanhPho_Id
            LEFT JOIN dbo.DM_NganHang    nh  ON nh.Id  = c.NganHang_Id
            WHERE c.Id = @Id AND c.IsDeleted = 0;
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<CompanyDetailDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LookupOptionDto>> GetCapCongTyOptionsAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id AS Id, Ten AS Text, NULL AS Extra
            FROM dbo.TC_CapCongTy
            WHERE IsDeleted = 0
            ORDER BY ThuTu, Ten;
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<LookupOptionDto>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LookupOptionDto>> GetNganHangOptionsAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id AS Id, Ten AS Text, TenVietTat AS Extra
            FROM dbo.DM_NganHang
            WHERE IsDeleted = 0
            ORDER BY Ten;
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<LookupOptionDto>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LookupOptionDto>> SearchPhuongXaAsync(string? term, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP (50) px.Id AS Id, px.Ten AS Text, tt.Ten AS Extra
            FROM dbo.DM_PhuongXa px
            LEFT JOIN dbo.DM_TinhThanhPho tt ON tt.Id = px.TinhThanhPho_Id
            WHERE px.IsDeleted = 0
              AND (@Like IS NULL OR px.Ten LIKE @Like OR tt.Ten LIKE @Like)
            ORDER BY px.Ten;
            """;
        var like = string.IsNullOrWhiteSpace(term) ? null : $"%{term.Trim()}%";
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<LookupOptionDto>(
            new CommandDefinition(sql, new { Like = like }, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsMaAsync(string ma, long? excludeId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.TC_CongTy
                WHERE Ma = @Ma AND IsDeleted = 0 AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
            ) THEN 1 ELSE 0 END;
            """;
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Ma = ma, ExcludeId = excludeId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> WouldCreateCycleAsync(long id, long? newParentId, CancellationToken ct = default)
    {
        if (newParentId is null) return false;
        if (newParentId == id) return true;

        // newParentId là con/cháu của id ⇒ đặt làm cha sẽ tạo vòng lặp.
        const string sql = """
            WITH Descendants AS (
                SELECT Id FROM dbo.TC_CongTy WHERE CongTy_Cha_Id = @Id AND IsDeleted = 0
                UNION ALL
                SELECT c.Id FROM dbo.TC_CongTy c
                JOIN Descendants d ON c.CongTy_Cha_Id = d.Id
                WHERE c.IsDeleted = 0
            )
            SELECT CASE WHEN @NewParentId IN (SELECT Id FROM Descendants) THEN 1 ELSE 0 END;
            """;
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Id = id, NewParentId = newParentId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<long> InsertAsync(CompanyInput input, long? userId, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.TC_CongTy
                (Ma, Ten, TenVietTat, CongTy_Cha_Id, CapCongTy_Id, MaSoThue, DiaChi, PhuongXa_Id,
                 DienThoai, Email, Website, NguoiDaiDien, GiamDoc, KeToanTruong, NganHang_Id, SoTaiKhoan,
                 TrangThai, CreatedBy, CreatedAt, IsDeleted, Ver)
            OUTPUT INSERTED.Id
            VALUES
                (@Ma, @Ten, @TenVietTat, @CongTyChaId, @CapCongTyId, @MaSoThue, @DiaChi, @PhuongXaId,
                 @DienThoai, @Email, @Website, @NguoiDaiDien, @GiamDoc, @KeToanTruong, @NganHangId, @SoTaiKhoan,
                 @TrangThai, @UserId, SYSUTCDATETIME(), 0, 0);
            """;
        var p = new DynamicParameters(input);
        p.Add("UserId", userId ?? 0L);
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, p, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task UpdateAsync(long id, CompanyInput input, long? userId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.TC_CongTy SET
                Ma = @Ma, Ten = @Ten, TenVietTat = @TenVietTat, CongTy_Cha_Id = @CongTyChaId,
                CapCongTy_Id = @CapCongTyId, MaSoThue = @MaSoThue, DiaChi = @DiaChi, PhuongXa_Id = @PhuongXaId,
                DienThoai = @DienThoai, Email = @Email, Website = @Website, NguoiDaiDien = @NguoiDaiDien,
                GiamDoc = @GiamDoc, KeToanTruong = @KeToanTruong, NganHang_Id = @NganHangId, SoTaiKhoan = @SoTaiKhoan,
                TrangThai = @TrangThai, UpdatedBy = @UserId, UpdatedAt = SYSUTCDATETIME(), Ver = Ver + 1
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        var p = new DynamicParameters(input);
        p.Add("Id", id);
        p.Add("UserId", userId);
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, p, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<(int Children, int Departments)> CountDependentsAsync(long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                (SELECT COUNT(1) FROM dbo.TC_CongTy  WHERE CongTy_Cha_Id = @Id AND IsDeleted = 0) AS Children,
                (SELECT COUNT(1) FROM dbo.TC_PhongBan WHERE CongTy_Id     = @Id AND IsDeleted = 0) AS Departments;
            """;
        using var conn = _db.CreateConnection();
        var row = await conn.QuerySingleAsync<DepCount>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return (row.Children, row.Departments);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long id, long? userId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.TC_CongTy
            SET IsDeleted = 1, UpdatedBy = @UserId, UpdatedAt = SYSUTCDATETIME(), Ver = Ver + 1
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: ct));
    }

    /// <summary>Dòng đếm phụ thuộc (map kết quả 2 cột của CountDependentsAsync).</summary>
    private sealed record DepCount(int Children, int Departments);
}
