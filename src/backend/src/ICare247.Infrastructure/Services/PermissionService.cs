// File    : PermissionService.cs
// Module  : Authorization
// Layer   : Infrastructure
// Purpose : Dapper impl IPermissionService — kiểm 1 cờ quyền của user trên 1 chức năng (Data DB tenant).
//           Cột cờ suy từ enum (an toàn, không phải input người dùng) → parameterized phần còn lại.

using Dapper;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Services;

/// <summary>Kiểm quyền runtime từ HT_VaiTro_Quyen theo vai trò người dùng.</summary>
public sealed class PermissionService : IPermissionService
{
    private readonly IDataDbConnectionFactory _db;

    public PermissionService(IDataDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(long userId, string funcCode, PermissionOp op, CancellationToken ct = default)
    {
        var col = Column(op);

        var sql = $"""
            SELECT TOP 1 1
            FROM dbo.HT_VaiTro_Quyen q
            JOIN dbo.HT_NguoiDung_VaiTro uv ON uv.VaiTro_Id = q.VaiTro_Id AND uv.IsDeleted = 0
            JOIN dbo.HT_ChucNang c          ON c.Id = q.ChucNang_Id AND c.Ma = @FuncCode AND c.IsDeleted = 0
            WHERE uv.NguoiDung_Id = @UserId AND q.IsDeleted = 0 AND q.{col} = 1;
            """;

        using var conn = _db.CreateConnection();
        var hit = await conn.ExecuteScalarAsync<int?>(
            new CommandDefinition(sql, new { UserId = userId, FuncCode = funcCode }, cancellationToken: ct));
        return hit == 1;
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionForTargetAsync(
        long userId, string targetType, string targetCode, PermissionOp op, CancellationToken ct = default)
    {
        var col = Column(op);

        // Enforce-if-mapped: chưa có node nào gắn đối tượng này → cho qua (=1).
        var sql = $"""
            SELECT CASE
              WHEN NOT EXISTS (
                  SELECT 1 FROM dbo.HT_ChucNang m
                  WHERE m.LoaiDoiTuong = @Type AND m.DoiTuong = @Code AND m.IsDeleted = 0) THEN 1
              WHEN EXISTS (
                  SELECT 1 FROM dbo.HT_VaiTro_Quyen q
                  JOIN dbo.HT_NguoiDung_VaiTro uv ON uv.VaiTro_Id = q.VaiTro_Id AND uv.IsDeleted = 0
                  JOIN dbo.HT_ChucNang c          ON c.Id = q.ChucNang_Id
                                                  AND c.LoaiDoiTuong = @Type AND c.DoiTuong = @Code AND c.IsDeleted = 0
                  WHERE uv.NguoiDung_Id = @UserId AND q.IsDeleted = 0 AND q.{col} = 1) THEN 1
              ELSE 0 END;
            """;

        using var conn = _db.CreateConnection();
        var allowed = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { UserId = userId, Type = targetType, Code = targetCode }, cancellationToken: ct));
        return allowed == 1;
    }

    /// <summary>Tên cột cờ theo enum (cố định — an toàn injection).</summary>
    private static string Column(PermissionOp op) => op switch
    {
        PermissionOp.Xem => "Xem",
        PermissionOp.Them => "Them",
        PermissionOp.Sua => "Sua",
        PermissionOp.Xoa => "Xoa",
        PermissionOp.InAn => "InAn",
        _ => "Xem"
    };
}
