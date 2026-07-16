// File    : UserAdminRepository.cs
// Module  : Admin/Users
// Layer   : Infrastructure
// Purpose : Dapper impl IUserAdminRepository — CRUD HT_NguoiDung + map vai trò (HT_NguoiDung_VaiTro)
//           + map công ty gán riêng (HT_NguoiDung_CongTy) trên Data DB tenant.
//           Parameterized 100%, không SELECT *; ghi map theo kiểu thêm-thiếu/xóa-mềm-thừa trong transaction.

using Dapper;
using ICare247.Application.Features.Admin.Users;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Đọc/ghi người dùng + phân công vai trò/công ty (Data DB tenant).</summary>
public sealed class UserAdminRepository : IUserAdminRepository
{
    private readonly IDataDbConnectionFactory _db;

    public UserAdminRepository(IDataDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(CancellationToken ct = default)
    {
        // STRING_AGG gộp tên vai trò cho cột hiển thị lưới (SQL Server 2017+).
        const string sql = """
            SELECT u.Id, u.Ma, u.TenDangNhap, u.LoaiTaiKhoan, u.TrangThai,
                   u.LaQuanTri, u.KichHoatMobile, u.HetHanTaiKhoan, u.LanDangNhapCuoi,
                   vt.Ten AS VaiTro
            FROM dbo.HT_NguoiDung u
            OUTER APPLY (
                SELECT STRING_AGG(v.Ten, N', ') WITHIN GROUP (ORDER BY v.Ten) AS Ten
                FROM dbo.HT_NguoiDung_VaiTro uv
                JOIN dbo.HT_VaiTro v ON v.Id = uv.VaiTro_Id AND v.IsDeleted = 0
                WHERE uv.NguoiDung_Id = u.Id AND uv.IsDeleted = 0
            ) vt
            WHERE u.IsDeleted = 0
            ORDER BY u.TenDangNhap;
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<UserListItemDto>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<UserDetailDto?> GetUserDetailAsync(long id, CancellationToken ct = default)
    {
        // 2 result set: thông tin user + toàn bộ vai trò với cờ đã gán (LEFT JOIN map).
        const string sql = """
            SELECT u.Id, u.Ma, u.TenDangNhap, u.LoaiTaiKhoan, u.TrangThai,
                   u.LaQuanTri, u.KichHoatMobile, u.HetHanTaiKhoan, u.DoiMatKhauLanSau
            FROM dbo.HT_NguoiDung u
            WHERE u.Id = @Id AND u.IsDeleted = 0;

            SELECT v.Id, v.Ma, v.Ten, v.MoTa,
                   CAST(CASE WHEN uv.Id IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS DaGan
            FROM dbo.HT_VaiTro v
            LEFT JOIN dbo.HT_NguoiDung_VaiTro uv
                 ON uv.VaiTro_Id = v.Id AND uv.NguoiDung_Id = @Id AND uv.IsDeleted = 0
            WHERE v.IsDeleted = 0
            ORDER BY v.LaHeThong DESC, v.Ten;
            """;
        using var conn = _db.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

        var info = await multi.ReadFirstOrDefaultAsync<UserInfoRow>();
        if (info is null) return null;

        var roles = (await multi.ReadAsync<UserRoleItemDto>()).ToList();
        return new UserDetailDto(
            info.Id, info.Ma, info.TenDangNhap, info.LoaiTaiKhoan, info.TrangThai,
            info.LaQuanTri, info.KichHoatMobile, info.HetHanTaiKhoan, info.DoiMatKhauLanSau, roles);
    }

    /// <inheritdoc />
    public async Task<(bool MaTrung, bool TenDangNhapTrung)> CheckDuplicateAsync(
        string ma, string tenDangNhap, long? excludeId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.HT_NguoiDung
                     WHERE Ma = @Ma AND IsDeleted = 0 AND (@ExcludeId IS NULL OR Id <> @ExcludeId))
                     THEN 1 ELSE 0 END AS BIT) AS MaTrung,
                CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.HT_NguoiDung
                     WHERE TenDangNhap = @TenDangNhap AND IsDeleted = 0 AND (@ExcludeId IS NULL OR Id <> @ExcludeId))
                     THEN 1 ELSE 0 END AS BIT) AS TenDangNhapTrung;
            """;
        using var conn = _db.CreateConnection();
        var row = await conn.QuerySingleAsync<(bool MaTrung, bool TenDangNhapTrung)>(
            new CommandDefinition(sql, new { Ma = ma, TenDangNhap = tenDangNhap, ExcludeId = excludeId },
                cancellationToken: ct));
        return row;
    }

    /// <inheritdoc />
    public async Task<long> CreateUserAsync(
        string ma, string tenDangNhap, string matKhauHash, string trangThai, bool laQuanTri,
        bool kichHoatMobile, DateTime? hetHanTaiKhoan, bool doiMatKhauLanSau, long actorId,
        CancellationToken ct = default)
    {
        // CreatedBy tường minh (không dựa DEFAULT); LoaiTaiKhoan='Local' — AD/SSO thuộc đợt sau.
        const string sql = """
            INSERT INTO dbo.HT_NguoiDung
                (Ma, TenDangNhap, LoaiTaiKhoan, MatKhauHash, TrangThai, LaQuanTri,
                 KichHoatMobile, HetHanTaiKhoan, DoiMatKhauLanSau, CreatedBy, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES
                (@Ma, @TenDangNhap, N'Local', @MatKhauHash, @TrangThai, @LaQuanTri,
                 @KichHoatMobile, @HetHanTaiKhoan, @DoiMatKhauLanSau, @ActorId, SYSUTCDATETIME());
            """;
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            Ma = ma, TenDangNhap = tenDangNhap, MatKhauHash = matKhauHash, TrangThai = trangThai,
            LaQuanTri = laQuanTri, KichHoatMobile = kichHoatMobile, HetHanTaiKhoan = hetHanTaiKhoan,
            DoiMatKhauLanSau = doiMatKhauLanSau, ActorId = actorId
        }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> UpdateUserAsync(
        long id, string ma, string tenDangNhap, string trangThai, bool laQuanTri,
        bool kichHoatMobile, DateTime? hetHanTaiKhoan, bool doiMatKhauLanSau, long actorId,
        CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.HT_NguoiDung
            SET Ma = @Ma, TenDangNhap = @TenDangNhap, TrangThai = @TrangThai, LaQuanTri = @LaQuanTri,
                KichHoatMobile = @KichHoatMobile, HetHanTaiKhoan = @HetHanTaiKhoan,
                DoiMatKhauLanSau = @DoiMatKhauLanSau,
                UpdatedBy = @ActorId, UpdatedAt = SYSUTCDATETIME(), Ver = Ver + 1
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = id, Ma = ma, TenDangNhap = tenDangNhap, TrangThai = trangThai, LaQuanTri = laQuanTri,
            KichHoatMobile = kichHoatMobile, HetHanTaiKhoan = hetHanTaiKhoan,
            DoiMatKhauLanSau = doiMatKhauLanSau, ActorId = actorId
        }, cancellationToken: ct));
        return affected > 0;
    }

    /// <inheritdoc />
    public async Task<bool> ResetPasswordAsync(
        long id, string matKhauHash, bool doiMatKhauLanSau, long actorId, CancellationToken ct = default)
    {
        // Reset kèm mở khóa đăng nhập sai (SoLanDangNhapSai/KhoaDenKhi) — admin cấp lại lối vào.
        const string sql = """
            UPDATE dbo.HT_NguoiDung
            SET MatKhauHash = @MatKhauHash, DoiMatKhauLanSau = @DoiMatKhauLanSau,
                SoLanDangNhapSai = 0, KhoaDenKhi = NULL,
                UpdatedBy = @ActorId, UpdatedAt = SYSUTCDATETIME(), Ver = Ver + 1
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = id, MatKhauHash = matKhauHash, DoiMatKhauLanSau = doiMatKhauLanSau, ActorId = actorId
        }, cancellationToken: ct));
        return affected > 0;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAsync(long id, long actorId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.HT_NguoiDung
            SET IsDeleted = 1, UpdatedBy = @ActorId, UpdatedAt = SYSUTCDATETIME(), Ver = Ver + 1
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, ActorId = actorId }, cancellationToken: ct));
        return affected > 0;
    }

    /// <inheritdoc />
    public async Task SaveUserRolesAsync(
        long id, IReadOnlyList<long> roleIds, long actorId, CancellationToken ct = default)
    {
        // Diff theo tập gửi lên: xóa mềm dòng thừa, revive/insert dòng thiếu — 1 transaction.
        const string sql = """
            DECLARE @ids TABLE (Id BIGINT PRIMARY KEY);
            INSERT INTO @ids (Id)
            SELECT DISTINCT value FROM OPENJSON(@RoleIdsJson) WITH (value BIGINT '$');

            UPDATE uv
            SET uv.IsDeleted = 1, uv.UpdatedBy = @ActorId, uv.UpdatedAt = SYSUTCDATETIME(), uv.Ver = uv.Ver + 1
            FROM dbo.HT_NguoiDung_VaiTro uv
            WHERE uv.NguoiDung_Id = @Id AND uv.IsDeleted = 0
              AND NOT EXISTS (SELECT 1 FROM @ids i WHERE i.Id = uv.VaiTro_Id);

            INSERT INTO dbo.HT_NguoiDung_VaiTro (NguoiDung_Id, VaiTro_Id, CreatedBy, CreatedAt)
            SELECT @Id, i.Id, @ActorId, SYSUTCDATETIME()
            FROM @ids i
            WHERE NOT EXISTS (SELECT 1 FROM dbo.HT_NguoiDung_VaiTro uv
                              WHERE uv.NguoiDung_Id = @Id AND uv.VaiTro_Id = i.Id AND uv.IsDeleted = 0);
            """;
        using var conn = _db.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = id,
            RoleIdsJson = System.Text.Json.JsonSerializer.Serialize(roleIds),
            ActorId = actorId
        }, tx, cancellationToken: ct));
        tx.Commit();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserCompanyNodeDto>> GetUserCompaniesAsync(
        long id, CancellationToken ct = default)
    {
        // Toàn bộ cây công ty active + cờ: gán riêng (sửa được), theo vai trò (readonly), mặc định.
        // OBJECT_ID guard cho HT_VaiTro_CongTy — tenant chưa chạy db/082 thì TheoVaiTro luôn 0.
        const string sql = """
            IF OBJECT_ID('dbo.HT_VaiTro_CongTy', 'U') IS NOT NULL
            BEGIN
                SELECT c.Id, c.Ma, c.Ten, c.CongTy_Cha_Id AS ParentId,
                       CAST(CASE WHEN uc.Id IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS GanRieng,
                       CAST(CASE WHEN EXISTS (
                           SELECT 1 FROM dbo.HT_VaiTro_CongTy vc
                           JOIN dbo.HT_NguoiDung_VaiTro uv
                                ON uv.VaiTro_Id = vc.VaiTro_Id AND uv.NguoiDung_Id = @Id AND uv.IsDeleted = 0
                           WHERE vc.CongTy_Id = c.Id AND vc.IsDeleted = 0) THEN 1 ELSE 0 END AS BIT) AS TheoVaiTro,
                       CAST(ISNULL(uc.LaMacDinh, 0) AS BIT) AS LaMacDinh
                FROM dbo.TC_CongTy c
                LEFT JOIN dbo.HT_NguoiDung_CongTy uc
                     ON uc.CongTy_Id = c.Id AND uc.NguoiDung_Id = @Id AND uc.IsDeleted = 0
                WHERE c.IsDeleted = 0
                ORDER BY c.Ten;
            END
            ELSE
            BEGIN
                SELECT c.Id, c.Ma, c.Ten, c.CongTy_Cha_Id AS ParentId,
                       CAST(CASE WHEN uc.Id IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS GanRieng,
                       CAST(0 AS BIT) AS TheoVaiTro,
                       CAST(ISNULL(uc.LaMacDinh, 0) AS BIT) AS LaMacDinh
                FROM dbo.TC_CongTy c
                LEFT JOIN dbo.HT_NguoiDung_CongTy uc
                     ON uc.CongTy_Id = c.Id AND uc.NguoiDung_Id = @Id AND uc.IsDeleted = 0
                WHERE c.IsDeleted = 0
                ORDER BY c.Ten;
            END
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<UserCompanyNodeDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task SaveUserCompaniesAsync(
        long id, IReadOnlyList<long> congTyIds, long? macDinhCongTyId, long actorId,
        CancellationToken ct = default)
    {
        // Diff tập gán riêng + đặt LaMacDinh duy nhất — 1 transaction. WYSIWYG từ cây checkbox.
        const string sql = """
            DECLARE @ids TABLE (Id BIGINT PRIMARY KEY);
            INSERT INTO @ids (Id)
            SELECT DISTINCT value FROM OPENJSON(@CongTyIdsJson) WITH (value BIGINT '$');

            UPDATE uc
            SET uc.IsDeleted = 1, uc.UpdatedBy = @ActorId, uc.UpdatedAt = SYSUTCDATETIME(), uc.Ver = uc.Ver + 1
            FROM dbo.HT_NguoiDung_CongTy uc
            WHERE uc.NguoiDung_Id = @Id AND uc.IsDeleted = 0
              AND NOT EXISTS (SELECT 1 FROM @ids i WHERE i.Id = uc.CongTy_Id);

            INSERT INTO dbo.HT_NguoiDung_CongTy (NguoiDung_Id, CongTy_Id, LaMacDinh, CreatedBy, CreatedAt)
            SELECT @Id, i.Id, 0, @ActorId, SYSUTCDATETIME()
            FROM @ids i
            WHERE NOT EXISTS (SELECT 1 FROM dbo.HT_NguoiDung_CongTy uc
                              WHERE uc.NguoiDung_Id = @Id AND uc.CongTy_Id = i.Id AND uc.IsDeleted = 0);

            UPDATE uc
            SET uc.LaMacDinh = CASE WHEN uc.CongTy_Id = @MacDinhCongTyId THEN 1 ELSE 0 END,
                uc.UpdatedBy = @ActorId, uc.UpdatedAt = SYSUTCDATETIME(), uc.Ver = uc.Ver + 1
            FROM dbo.HT_NguoiDung_CongTy uc
            WHERE uc.NguoiDung_Id = @Id AND uc.IsDeleted = 0
              AND uc.LaMacDinh <> CASE WHEN uc.CongTy_Id = @MacDinhCongTyId THEN 1 ELSE 0 END;

            -- Đồng bộ CongTyMacDinh_Id trên HT_NguoiDung (switcher chọn sẵn khi đăng nhập).
            UPDATE dbo.HT_NguoiDung
            SET CongTyMacDinh_Id = @MacDinhCongTyId,
                UpdatedBy = @ActorId, UpdatedAt = SYSUTCDATETIME(), Ver = Ver + 1
            WHERE Id = @Id AND IsDeleted = 0
              AND (CongTyMacDinh_Id IS NULL AND @MacDinhCongTyId IS NOT NULL
                   OR CongTyMacDinh_Id IS NOT NULL AND @MacDinhCongTyId IS NULL
                   OR CongTyMacDinh_Id <> @MacDinhCongTyId);
            """;
        using var conn = _db.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = id,
            CongTyIdsJson = System.Text.Json.JsonSerializer.Serialize(congTyIds),
            MacDinhCongTyId = macDinhCongTyId,
            ActorId = actorId
        }, tx, cancellationToken: ct));
        tx.Commit();
    }

    /// <summary>Row nội bộ đọc result set 1 của GetUserDetailAsync (Dapper map).</summary>
    private sealed record UserInfoRow(
        long Id, string Ma, string TenDangNhap, string LoaiTaiKhoan, string TrangThai,
        bool LaQuanTri, bool KichHoatMobile, DateTime? HetHanTaiKhoan, bool DoiMatKhauLanSau);
}
