// File    : MeCompanyRepository.cs
// Module  : Navigation
// Layer   : Infrastructure
// Purpose : Dapper impl IMeCompanyRepository — công ty user được chọn ở company-switcher (Data DB tenant).
//           Phòng thủ: bảng phân công chưa có / user chưa được phân công → trả mọi công ty active
//           (tránh switcher rỗng). Ranh giới bảo mật thật do @NguoiDungID enforce ở tầng lọc dữ liệu;
//           danh sách này chỉ là tiện UX (@CongTyID_Active được server validate lại — ADR-030).

using Dapper;
using ICare247.Application.Features.Navigation.Queries.GetMyCompanies;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Đọc công ty theo user: HT_NguoiDung_CongTy ⨝ TC_CongTy; LaMacDinh lên đầu.</summary>
public sealed class MeCompanyRepository : IMeCompanyRepository
{
    private readonly IDataDbConnectionFactory _db;

    public MeCompanyRepository(IDataDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<MyCompanyDto>> GetForUserAsync(long userId, CancellationToken ct = default)
    {
        // OBJECT_ID guard: tenant chưa chạy db/037 (chưa có bảng phân công) → mọi công ty active.
        // Có bảng nhưng user chưa được phân công công ty nào (@HasAssign=0) → cũng trả mọi công ty active.
        // Có phân công → chỉ công ty được giao. Công ty mặc định (LaMacDinh) xếp đầu để chọn sẵn.
        const string sql = """
            IF OBJECT_ID('dbo.HT_NguoiDung_CongTy', 'U') IS NULL
            BEGIN
                SELECT c.Id, c.Ma AS Code, c.Ten AS Name, CAST(0 AS BIT) AS IsDefault
                FROM   dbo.TC_CongTy c
                WHERE  c.IsDeleted = 0
                ORDER  BY c.Ten;
            END
            ELSE
            BEGIN
                DECLARE @HasAssign BIT =
                    CASE WHEN EXISTS (SELECT 1 FROM dbo.HT_NguoiDung_CongTy
                                      WHERE NguoiDung_Id = @UserId AND IsDeleted = 0)
                         THEN 1 ELSE 0 END;
                SELECT c.Id, c.Ma AS Code, c.Ten AS Name,
                       CAST(ISNULL(uc.LaMacDinh, 0) AS BIT) AS IsDefault
                FROM   dbo.TC_CongTy c
                LEFT JOIN dbo.HT_NguoiDung_CongTy uc
                       ON uc.CongTy_Id = c.Id AND uc.NguoiDung_Id = @UserId AND uc.IsDeleted = 0
                WHERE  c.IsDeleted = 0
                  AND  (@HasAssign = 0 OR uc.Id IS NOT NULL)
                ORDER  BY CAST(ISNULL(uc.LaMacDinh, 0) AS INT) DESC, c.Ten;
            END
            """;

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<MyCompanyDto>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.ToList();
    }
}
