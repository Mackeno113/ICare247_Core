// File    : MeCompanyRepository.cs
// Module  : Navigation
// Layer   : Infrastructure
// Purpose : Dapper impl IMeCompanyRepository — cây công ty user được chọn ở company-switcher (Data DB tenant).
//           Quyền hiệu lực = gán riêng (HT_NguoiDung_CongTy) ∪ theo vai trò (HT_VaiTro_CongTy ⨝
//           HT_NguoiDung_VaiTro) — kế thừa ĐỘNG cùng cơ chế với trục chức năng (HT_VaiTro_Quyen).
//           Phòng thủ: bảng phân công chưa có / user chưa được phân công gì → trả mọi công ty active
//           (tránh switcher rỗng). Ranh giới bảo mật thật do @NguoiDungID enforce ở tầng lọc dữ liệu;
//           danh sách này chỉ là tiện UX (@CongTyID_Active được server validate lại — ADR-030).

using Dapper;
using ICare247.Application.Features.Navigation.Queries.GetMyCompanies;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Đọc cây công ty theo user: gán riêng ∪ theo vai trò + tổ tiên giữ cấu trúc cây.</summary>
public sealed class MeCompanyRepository : IMeCompanyRepository
{
    private readonly IDataDbConnectionFactory _db;

    public MeCompanyRepository(IDataDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<MyCompanyDto>> GetForUserAsync(long userId, CancellationToken ct = default)
    {
        // Gom quyền vào #granted (OBJECT_ID guard từng nguồn — tenant chưa chạy db/037 hoặc db/082
        // thì bỏ qua nguồn đó). #granted rỗng → default-open: mọi công ty active (user chưa phân công).
        // Sau cùng leo CTE lấy tổ tiên của các node được cấp để cây không đứt nhánh: node tổ tiên
        // không có quyền trả về CanAccess=0 (FE hiển thị disabled). Sự kiện theo sau: FE dựng cây switcher.
        const string sql = """
            CREATE TABLE #granted (Id BIGINT PRIMARY KEY, IsDefault BIT NOT NULL DEFAULT 0);

            -- Nguồn 1: gán riêng từng user (mang cờ LaMacDinh — công ty chọn sẵn khi đăng nhập).
            IF OBJECT_ID('dbo.HT_NguoiDung_CongTy', 'U') IS NOT NULL
                INSERT INTO #granted (Id, IsDefault)
                SELECT uc.CongTy_Id, MAX(CAST(uc.LaMacDinh AS INT))
                FROM   dbo.HT_NguoiDung_CongTy uc
                WHERE  uc.NguoiDung_Id = @UserId AND uc.IsDeleted = 0
                GROUP  BY uc.CongTy_Id;

            -- Nguồn 2: theo vai trò (kế thừa động — không copy). Không mang LaMacDinh.
            IF OBJECT_ID('dbo.HT_VaiTro_CongTy', 'U') IS NOT NULL
                INSERT INTO #granted (Id)
                SELECT DISTINCT vc.CongTy_Id
                FROM   dbo.HT_VaiTro_CongTy vc
                JOIN   dbo.HT_NguoiDung_VaiTro uv
                       ON uv.VaiTro_Id = vc.VaiTro_Id AND uv.NguoiDung_Id = @UserId AND uv.IsDeleted = 0
                WHERE  vc.IsDeleted = 0
                  AND  NOT EXISTS (SELECT 1 FROM #granted g WHERE g.Id = vc.CongTy_Id);

            -- Phòng thủ: chưa được phân công gì → mọi công ty active (switcher không rỗng).
            IF NOT EXISTS (SELECT 1 FROM #granted)
                INSERT INTO #granted (Id)
                SELECT c.Id FROM dbo.TC_CongTy c WHERE c.IsDeleted = 0;

            -- Leo tổ tiên để cây không đứt nhánh; node leo thêm (không trong #granted) → CanAccess=0.
            WITH anc AS (
                SELECT c.Id, c.CongTy_Cha_Id
                FROM   dbo.TC_CongTy c
                JOIN   #granted g ON g.Id = c.Id
                WHERE  c.IsDeleted = 0
                UNION ALL
                SELECT p.Id, p.CongTy_Cha_Id
                FROM   dbo.TC_CongTy p
                JOIN   anc a ON p.Id = a.CongTy_Cha_Id
                WHERE  p.IsDeleted = 0
            )
            SELECT c.Id,
                   c.Ma            AS Code,
                   c.Ten           AS Name,
                   c.CongTy_Cha_Id AS ParentId,
                   CAST(CASE WHEN g.Id IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS CanAccess,
                   CAST(ISNULL(g.IsDefault, 0) AS BIT)                       AS IsDefault
            FROM   (SELECT DISTINCT Id FROM anc) x
            JOIN   dbo.TC_CongTy c ON c.Id = x.Id
            LEFT JOIN #granted g   ON g.Id = c.Id
            ORDER  BY c.Ten;

            DROP TABLE #granted;
            """;

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<MyCompanyDto>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.ToList();
    }
}
