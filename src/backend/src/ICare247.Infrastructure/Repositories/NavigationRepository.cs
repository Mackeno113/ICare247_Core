// File    : NavigationRepository.cs
// Module  : Navigation
// Layer   : Infrastructure
// Purpose : Dapper impl INavigationRepository — đọc cây menu HT_ChucNang theo quyền user.
//           Recursive CTE: lấy node Xem=1 rồi leo lên gồm cả tổ tiên (giữ cây liền mạch);
//           cờ thao tác hợp (MAX) qua nhiều vai trò (OR). Trả về theo THỨ TỰ CÂY cha→con,
//           trên→dưới (mỗi cấp theo ThuTu, tiebreaker Id) — dựng SortPath top-down compute-on-read
//           theo cơ chế ADR-027 (docs/spec/17). Parameterized 100%, không SELECT *.

using Dapper;
using ICare247.Application.Features.Navigation;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Đọc menu động từ Data DB tenant (HT_ChucNang ⨝ HT_VaiTro_Quyen ⨝ HT_NguoiDung_VaiTro).</summary>
public sealed class NavigationRepository : INavigationRepository
{
    private readonly IDataDbConnectionFactory _db;

    public NavigationRepository(IDataDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<MeNavNodeDto>> GetForUserAsync(long userId, CancellationToken ct = default)
    {
        // Granted  : cờ quyền đã hợp (OR→MAX) theo các vai trò của user, theo từng chức năng.
        // Visible  : node user được Xem (đang KichHoat).
        // NodeSet  : Visible + leo lên tổ tiên (KichHoat) để sidebar/sub-nav liền mạch (chỉ tập Id).
        // NodeIds  : NodeSet đã DISTINCT — dùng JOIN trong nhánh đệ quy (SQL Server cấm subquery ở recursive member).
        // Ordered  : duyệt TOP-DOWN trong tập đã chọn, dựng SortPath theo cây (gốc theo ThuTu → con nằm dưới cha
        //            theo ThuTu, tiebreaker Id). Mỗi bậc = ThuTu+Id zero-pad ⇒ sort chuỗi = sort cây cha→con,
        //            trên→dưới (compute-on-read theo ADR-027; tương đương DuongDanCay khi cột này được migrate).
        //            Gốc forest = Cha NULL/0 HOẶC cha nằm ngoài tập (chuỗi tổ tiên bị cắt vì node inactive/deleted).
        const string sql = """
            WITH Granted AS (
                SELECT q.ChucNang_Id,
                       MAX(CAST(q.Xem  AS INT)) AS Xem,
                       MAX(CAST(q.Them AS INT)) AS Them,
                       MAX(CAST(q.Sua  AS INT)) AS Sua,
                       MAX(CAST(q.Xoa  AS INT)) AS Xoa,
                       MAX(CAST(q.InAn AS INT)) AS InAn
                FROM dbo.HT_VaiTro_Quyen q
                JOIN dbo.HT_NguoiDung_VaiTro uv
                     ON uv.VaiTro_Id = q.VaiTro_Id AND uv.IsDeleted = 0
                WHERE uv.NguoiDung_Id = @UserId AND q.IsDeleted = 0
                GROUP BY q.ChucNang_Id
            ),
            Visible AS (
                SELECT c.Id
                FROM dbo.HT_ChucNang c
                JOIN Granted g ON g.ChucNang_Id = c.Id AND g.Xem = 1
                WHERE c.IsDeleted = 0 AND c.KichHoat = 1
            ),
            NodeSet AS (
                SELECT c.Id, c.ChucNang_Cha_Id
                FROM dbo.HT_ChucNang c
                WHERE c.Id IN (SELECT Id FROM Visible)
                UNION ALL
                SELECT p.Id, p.ChucNang_Cha_Id
                FROM dbo.HT_ChucNang p
                JOIN NodeSet t ON p.Id = t.ChucNang_Cha_Id
                WHERE p.IsDeleted = 0 AND p.KichHoat = 1
            ),
            NodeIds AS (
                SELECT DISTINCT Id FROM NodeSet
            ),
            Ordered AS (
                -- Gốc forest: node thuộc tập mà cha NULL/0 hoặc cha không thuộc tập.
                SELECT c.Id, c.ChucNang_Cha_Id,
                       CAST(RIGHT('0000000000' + CAST(c.ThuTu AS VARCHAR(10)), 10)
                            + '.' + RIGHT('0000000000000000000' + CAST(c.Id AS VARCHAR(19)), 19)
                            AS NVARCHAR(4000)) AS SortPath
                FROM dbo.HT_ChucNang c
                JOIN NodeIds ni ON ni.Id = c.Id
                WHERE c.ChucNang_Cha_Id IS NULL
                   OR c.ChucNang_Cha_Id = 0
                   OR c.ChucNang_Cha_Id NOT IN (SELECT Id FROM NodeIds)
                UNION ALL
                -- Con: nối path cha + segment của chính nó; INNER JOIN NodeIds giữ đúng tập (không kéo node ngoài).
                SELECT c.Id, c.ChucNang_Cha_Id,
                       CAST(p.SortPath + '/'
                            + RIGHT('0000000000' + CAST(c.ThuTu AS VARCHAR(10)), 10)
                            + '.' + RIGHT('0000000000000000000' + CAST(c.Id AS VARCHAR(19)), 19)
                            AS NVARCHAR(4000)) AS SortPath
                FROM dbo.HT_ChucNang c
                JOIN Ordered p ON c.ChucNang_Cha_Id = p.Id
                JOIN NodeIds ni ON ni.Id = c.Id
            )
            SELECT
                c.Ma            AS Ma,
                c.Ten           AS Ten,
                pa.Ma           AS ChaMa,
                c.Loai          AS Loai,
                c.Module        AS Module,
                c.DuongDan      AS DuongDan,
                c.Icon          AS Icon,
                c.ViTriHienThi  AS ViTriHienThi,
                c.ThuTu         AS ThuTu,
                CAST(ISNULL(g.Xem,  0) AS BIT) AS Xem,
                CAST(ISNULL(g.Them, 0) AS BIT) AS Them,
                CAST(ISNULL(g.Sua,  0) AS BIT) AS Sua,
                CAST(ISNULL(g.Xoa,  0) AS BIT) AS Xoa,
                CAST(ISNULL(g.InAn, 0) AS BIT) AS InAn,
                c.DoiTuong      AS DoiTuong,
                c.LoaiDoiTuong  AS LoaiDoiTuong
            FROM Ordered o
            JOIN dbo.HT_ChucNang c ON c.Id = o.Id
            LEFT JOIN dbo.HT_ChucNang pa ON pa.Id = c.ChucNang_Cha_Id
            LEFT JOIN Granted g ON g.ChucNang_Id = c.Id
            ORDER BY o.SortPath;   -- thứ tự cây cha→con, trên→dưới (mỗi cấp theo ThuTu, tiebreaker Id)
            """;

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<MeNavNodeDto>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.ToList();
    }
}
