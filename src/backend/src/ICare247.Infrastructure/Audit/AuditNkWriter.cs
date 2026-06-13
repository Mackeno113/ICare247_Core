// File    : AuditNkWriter.cs
// Module  : Audit
// Layer   : Infrastructure
// Purpose : Ghi lô AuditEvent vào NK_NhatKyHoatDong của từng tenant bằng SqlBulkCopy.
//           Resolve connection string theo Tenant_Id (KHÔNG nhận connstring từ event → tránh
//           lộ secret qua Redis).

using System.Data;
using ICare247.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Audit;

/// <summary>
/// Bộ ghi nhật ký xuống DB. Gom lô theo tenant → resolve Data DB → SqlBulkCopy 1 phát/tenant.
/// </summary>
public sealed class AuditNkWriter
{
    private const string TableName = "dbo.NK_NhatKyHoatDong";

    private readonly ITenantConnectionResolver _resolver;
    private readonly ILogger<AuditNkWriter> _logger;

    public AuditNkWriter(ITenantConnectionResolver resolver, ILogger<AuditNkWriter> logger)
    {
        _resolver = resolver;
        _logger = logger;
    }

    /// <summary>
    /// Ghi 1 lô sự kiện. Gom theo Tenant_Id, mỗi nhóm bulk-copy vào Data DB tương ứng.
    /// Lỗi 1 tenant không chặn các tenant khác (log rồi bỏ qua lô đó).
    /// </summary>
    public async Task WriteAsync(IReadOnlyList<AuditEvent> batch, CancellationToken ct)
    {
        foreach (var group in batch.GroupBy(e => e.TenantId))
        {
            try
            {
                var conns = await _resolver.ResolveByIdAsync(group.Key, ct);
                // Ghi vào DB audit RIÊNG của tenant (tách khỏi Data DB nghiệp vụ).
                await BulkCopyAsync(conns.AuditConnectionString, group.ToList(), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Ghi nhật ký thất bại cho TenantId={TenantId}, bỏ qua {Count} bản ghi.",
                    group.Key, group.Count());
            }
        }
    }

    /// <summary>Bulk copy 1 nhóm bản ghi (cùng tenant) vào bảng nhật ký.</summary>
    private static async Task BulkCopyAsync(string connectionString, List<AuditEvent> events, CancellationToken ct)
    {
        var table = BuildTable(events);

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        using var bulk = new SqlBulkCopy(conn) { DestinationTableName = TableName };
        foreach (DataColumn col in table.Columns)
            bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

        await bulk.WriteToServerAsync(table, ct);
    }

    /// <summary>Dựng DataTable khớp cột NK_ (bỏ Id IDENTITY).</summary>
    private static DataTable BuildTable(List<AuditEvent> events)
    {
        var t = new DataTable();
        t.Columns.Add("ThoiGian", typeof(DateTime));
        t.Columns.Add("Loai", typeof(string));
        t.Columns.Add("HanhDong", typeof(string));
        t.Columns.Add("KetQua", typeof(string));
        t.Columns.Add("NguoiDung_Id", typeof(long));
        t.Columns.Add("TenDangNhap", typeof(string));
        t.Columns.Add("DoiTuong", typeof(string));
        t.Columns.Add("DoiTuong_Id", typeof(string));
        t.Columns.Add("GiaTriCu", typeof(string));
        t.Columns.Add("GiaTriMoi", typeof(string));
        t.Columns.Add("DiaChiIp", typeof(string));
        t.Columns.Add("ThietBi", typeof(string));
        t.Columns.Add("CorrelationId", typeof(string));

        foreach (var e in events)
        {
            var row = t.NewRow();
            row["ThoiGian"] = e.OccurredAtUtc;
            row["Loai"] = e.Category;
            row["HanhDong"] = e.Action;
            row["KetQua"] = (object?)e.Result ?? DBNull.Value;
            row["NguoiDung_Id"] = (object?)e.UserId ?? DBNull.Value;
            row["TenDangNhap"] = (object?)e.Username ?? DBNull.Value;
            row["DoiTuong"] = (object?)e.ObjectType ?? DBNull.Value;
            row["DoiTuong_Id"] = (object?)e.ObjectId ?? DBNull.Value;
            row["GiaTriCu"] = (object?)e.OldValueJson ?? DBNull.Value;
            row["GiaTriMoi"] = (object?)e.NewValueJson ?? DBNull.Value;
            row["DiaChiIp"] = (object?)e.IpAddress ?? DBNull.Value;
            row["ThietBi"] = (object?)e.Device ?? DBNull.Value;
            row["CorrelationId"] = (object?)e.CorrelationId ?? DBNull.Value;
            t.Rows.Add(row);
        }

        return t;
    }
}
