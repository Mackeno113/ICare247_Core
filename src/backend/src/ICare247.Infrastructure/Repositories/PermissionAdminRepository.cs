// File    : PermissionAdminRepository.cs
// Module  : Admin/Permissions
// Layer   : Infrastructure
// Purpose : Dapper impl IPermissionAdminRepository — đọc vai trò + ma trận quyền, lưu upsert
//           HT_VaiTro_Quyen trong transaction. Parameterized 100%, không SELECT *.

using Dapper;
using ICare247.Application.Features.Admin.Permissions;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Cấu hình phân quyền trên Data DB tenant (HT_VaiTro / HT_ChucNang / HT_VaiTro_Quyen).</summary>
public sealed class PermissionAdminRepository : IPermissionAdminRepository
{
    private readonly IDataDbConnectionFactory _db;

    public PermissionAdminRepository(IDataDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, Ma, Ten, MoTa, LaHeThong
            FROM dbo.HT_VaiTro
            WHERE IsDeleted = 0
            ORDER BY LaHeThong DESC, Ten;
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<RoleDto>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RolePermNodeDto>> GetRolePermissionsAsync(long roleId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                c.Id              AS Id,
                c.Ma              AS Ma,
                c.Ten             AS Ten,
                c.ChucNang_Cha_Id AS ChaId,
                c.Loai            AS Loai,
                c.ThuTu           AS ThuTu,
                CAST(ISNULL(q.Xem,  0) AS BIT) AS Xem,
                CAST(ISNULL(q.Them, 0) AS BIT) AS Them,
                CAST(ISNULL(q.Sua,  0) AS BIT) AS Sua,
                CAST(ISNULL(q.Xoa,  0) AS BIT) AS Xoa,
                CAST(ISNULL(q.InAn, 0) AS BIT) AS InAn
            FROM dbo.HT_ChucNang c
            LEFT JOIN dbo.HT_VaiTro_Quyen q
                 ON q.ChucNang_Id = c.Id AND q.VaiTro_Id = @RoleId AND q.IsDeleted = 0
            WHERE c.IsDeleted = 0 AND c.KichHoat = 1
            ORDER BY c.ThuTu;
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<RolePermNodeDto>(
            new CommandDefinition(sql, new { RoleId = roleId }, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task SaveRolePermissionsAsync(
        long roleId, IReadOnlyList<SavePermItem> items, long userId, CancellationToken ct = default)
    {
        if (items.Count == 0) return;

        // Upsert từng node; Duyet giữ nguyên 0 (thuộc workflow, không cấu hình ở đây).
        const string sql = """
            MERGE dbo.HT_VaiTro_Quyen AS tgt
            USING (SELECT @RoleId AS VaiTro_Id, @ChucNangId AS ChucNang_Id) AS src
            ON tgt.VaiTro_Id = src.VaiTro_Id AND tgt.ChucNang_Id = src.ChucNang_Id AND tgt.IsDeleted = 0
            WHEN MATCHED THEN
                UPDATE SET Xem = @Xem, Them = @Them, Sua = @Sua, Xoa = @Xoa, InAn = @InAn,
                           UpdatedBy = @UserId, UpdatedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT (VaiTro_Id, ChucNang_Id, Xem, Them, Sua, Xoa, Duyet, InAn, CreatedBy, CreatedAt)
                VALUES (@RoleId, @ChucNangId, @Xem, @Them, @Sua, @Xoa, 0, @InAn, @UserId, SYSUTCDATETIME());
            """;

        var rows = items.Select(i => new
        {
            RoleId = roleId,
            i.ChucNangId,
            i.Xem, i.Them, i.Sua, i.Xoa, i.InAn,
            UserId = userId
        }).ToList();

        using var conn = _db.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await conn.ExecuteAsync(new CommandDefinition(sql, rows, tx, cancellationToken: ct));
        tx.Commit();
    }
}
