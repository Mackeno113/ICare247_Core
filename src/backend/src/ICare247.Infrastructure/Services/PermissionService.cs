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
        // Tên cột lấy từ enum (cố định) → không có nguy cơ SQL injection.
        var col = op switch
        {
            PermissionOp.Xem => "Xem",
            PermissionOp.Them => "Them",
            PermissionOp.Sua => "Sua",
            PermissionOp.Xoa => "Xoa",
            PermissionOp.InAn => "InAn",
            _ => "Xem"
        };

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
}
