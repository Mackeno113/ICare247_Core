// File    : ErrorLogNkWriter.cs
// Module  : Errors
// Layer   : Infrastructure
// Purpose : Ghi lô ErrorLogEvent vào NK_LoiHeThong của từng tenant — INSERT trực tiếp từng dòng
//           (KHÔNG SqlBulkCopy: lỗi hiếm hơn audit trail nhiều, không cần tối ưu batch throughput).
//           Resolve connection string theo Tenant_Id (KHÔNG nhận connstring từ event).

using ICare247.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Errors;

/// <summary>Bộ ghi nhật ký lỗi xuống DB. Gom lô theo tenant → resolve Audit DB → INSERT từng dòng.</summary>
public sealed class ErrorLogNkWriter
{
    private const string InsertSql = """
        INSERT INTO dbo.NK_LoiHeThong
            (ThoiGian, CorrelationId, Nguon, ThongDiep, ChiTiet, DuongDan, PhuongThuc,
             NguoiDung_Id, TenDangNhap, DiaChiIp, ThietBi)
        VALUES
            (@ThoiGian, @CorrelationId, @Nguon, @ThongDiep, @ChiTiet, @DuongDan, @PhuongThuc,
             @NguoiDung_Id, @TenDangNhap, @DiaChiIp, @ThietBi);
        """;

    private readonly ITenantConnectionResolver _resolver;
    private readonly ILogger<ErrorLogNkWriter> _logger;

    public ErrorLogNkWriter(ITenantConnectionResolver resolver, ILogger<ErrorLogNkWriter> logger)
    {
        _resolver = resolver;
        _logger = logger;
    }

    /// <summary>
    /// Ghi 1 lô sự kiện lỗi. Gom theo Tenant_Id, mỗi nhóm mở 1 connection tới Audit DB tương ứng
    /// rồi INSERT lần lượt. Lỗi 1 tenant không chặn các tenant khác (log rồi bỏ qua lô đó).
    /// </summary>
    public async Task WriteAsync(IReadOnlyList<ErrorLogEvent> batch, CancellationToken ct)
    {
        foreach (var group in batch.GroupBy(e => e.TenantId))
        {
            if (group.Key <= 0)
            {
                _logger.LogWarning("Bỏ qua {Count} bản ghi lỗi vì chưa xác định TenantId.", group.Count());
                continue;
            }

            try
            {
                var conns = await _resolver.ResolveByIdAsync(group.Key, ct);
                await InsertAsync(conns.AuditConnectionString, group, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Ghi NK_LoiHeThong thất bại cho TenantId={TenantId}, bỏ qua {Count} bản ghi.",
                    group.Key, group.Count());
            }
        }
    }

    private static async Task InsertAsync(string connectionString, IEnumerable<ErrorLogEvent> events, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        foreach (var e in events)
        {
            await using var cmd = new SqlCommand(InsertSql, conn);
            cmd.Parameters.AddWithValue("@ThoiGian", e.OccurredAtUtc);
            cmd.Parameters.AddWithValue("@CorrelationId", e.CorrelationId);
            cmd.Parameters.AddWithValue("@Nguon", (object?)e.Source ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ThongDiep", (object?)e.Message ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChiTiet", (object?)e.StackDetail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DuongDan", (object?)e.Path ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PhuongThuc", (object?)e.HttpMethod ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NguoiDung_Id", (object?)e.UserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TenDangNhap", (object?)e.Username ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DiaChiIp", (object?)e.IpAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ThietBi", (object?)e.Device ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
