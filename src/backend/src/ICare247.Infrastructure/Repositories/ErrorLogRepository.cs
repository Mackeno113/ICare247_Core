// File    : ErrorLogRepository.cs
// Module  : Admin/ErrorLogs
// Layer   : Infrastructure
// Purpose : Dapper impl IErrorLogRepository — đọc NK_LoiHeThong (Audit DB tenant).
//           Parameterized 100%, không SELECT *.

using Dapper;
using ICare247.Application.Features.Admin.ErrorLogs;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Đọc nhật ký lỗi 500 (Audit DB tenant).</summary>
public sealed class ErrorLogRepository : IErrorLogRepository
{
    private readonly IAuditDbConnectionFactory _db;

    public ErrorLogRepository(IAuditDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ErrorLogListItemDto>> GetErrorLogsAsync(
        DateTime tuUtc, DateTime denUtc, string? maLoi, string? nguon, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, ThoiGian, LEFT(CorrelationId, 8) AS MaLoi, Nguon, ThongDiep,
                   DuongDan, PhuongThuc, TenDangNhap
            FROM dbo.NK_LoiHeThong
            WHERE ThoiGian >= @TuUtc AND ThoiGian < @DenUtc
              AND (@MaLoi IS NULL OR CorrelationId LIKE @MaLoi + '%')
              AND (@Nguon IS NULL OR Nguon LIKE '%' + @Nguon + '%')
            ORDER BY ThoiGian DESC;
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<ErrorLogListItemDto>(new CommandDefinition(
            sql, new { TuUtc = tuUtc, DenUtc = denUtc, MaLoi = maLoi, Nguon = nguon }, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<ErrorLogDetailDto?> GetErrorLogDetailAsync(long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, ThoiGian, CorrelationId, Nguon, ThongDiep, ChiTiet,
                   DuongDan, PhuongThuc, NguoiDung_Id AS NguoiDungId, TenDangNhap, DiaChiIp, ThietBi
            FROM dbo.NK_LoiHeThong
            WHERE Id = @Id;
            """;
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<ErrorLogDetailDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
