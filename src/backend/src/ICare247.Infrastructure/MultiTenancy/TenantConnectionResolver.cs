// File    : TenantConnectionResolver.cs
// Module  : MultiTenancy
// Layer   : Infrastructure
// Purpose : Phân giải tenant → cặp connection string từ Catalog DB (cache in-memory),
//           fallback connection cố định khi chưa cấu hình catalog. Xem ADR-018.

using System.Collections.Concurrent;
using Dapper;
using ICare247.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.MultiTenancy;

/// <summary>
/// Cài đặt <see cref="ITenantConnectionResolver"/>. Tra bảng <c>dbo.Tenant</c> trong Catalog DB
/// (<c>ConnectionStrings:Catalog</c>), giải mã connection string, cache theo Tenant_Id + Subdomain.
/// Chưa cấu hình catalog → fallback dùng <c>ConnectionStrings:Config</c>/<c>:Data</c> cố định
/// (giai đoạn 1 tenant / dev — app không vỡ).
/// </summary>
public sealed class TenantConnectionResolver : ITenantConnectionResolver
{
    /// <summary>Tenant_Id quy ước cho chế độ fallback (1 cấu hình cố định).</summary>
    private const int FallbackTenantId = 1;

    private readonly string? _catalogConn;
    private readonly string _defaultConfigConn;
    private readonly string _defaultDataConn;
    private readonly string _defaultAuditConn;
    private readonly TenantConnectionProtector _protector;
    private readonly ILogger<TenantConnectionResolver> _logger;

    private readonly ConcurrentDictionary<int, TenantConnections> _byId = new();
    private readonly ConcurrentDictionary<string, TenantConnections> _bySubdomain = new(StringComparer.OrdinalIgnoreCase);

    public TenantConnectionResolver(IConfiguration configuration, ILogger<TenantConnectionResolver> logger)
    {
        _logger = logger;
        _catalogConn = configuration.GetConnectionString("Catalog");

        var configConn = configuration.GetConnectionString("Config")
                      ?? configuration.GetConnectionString("Default") ?? "";
        _defaultConfigConn = configConn;
        // Data DB nghiệp vụ (chế độ fallback 1 cấu hình): ưu tiên LiveData (DB vận hành thật,
        // chứa HT_NguoiDung), rồi Data (tên cũ — tương thích ngược), cuối cùng dùng chung Config.
        _defaultDataConn = configuration.GetConnectionString("LiveData") is { Length: > 0 } live ? live
                         : configuration.GetConnectionString("Data") is { Length: > 0 } d ? d
                         : configConn;

        // DB nhật ký (audit) RIÊNG: ưu tiên ConnectionStrings:Audit; chưa cấu hình → dùng chung Data DB
        // (kèm log cảnh báo ở nơi ghi). Mỗi tenant 1 audit DB → catalog mang Audit_Conn_Encrypted riêng.
        _defaultAuditConn = configuration.GetConnectionString("Audit") is { Length: > 0 } au ? au : _defaultDataConn;

        _protector = new TenantConnectionProtector(configuration["Catalog:EncryptionKey"]);
    }

    /// <inheritdoc />
    public bool IsCatalogConfigured => !string.IsNullOrWhiteSpace(_catalogConn);

    /// <inheritdoc />
    public async Task<TenantConnections> ResolveByIdAsync(int tenantId, CancellationToken ct = default)
    {
        if (!IsCatalogConfigured)
            return new TenantConnections(tenantId, _defaultConfigConn, _defaultDataConn, _defaultAuditConn);

        if (_byId.TryGetValue(tenantId, out var cached)) return cached;

        const string sql =
            "SELECT Tenant_Id, Subdomain, Config_Conn_Encrypted, Data_Conn_Encrypted, Audit_Conn_Encrypted\n" +
            "FROM dbo.Tenant WHERE Tenant_Id = @Id AND Is_Active = 1";
        var row = await QueryTenantAsync(sql, new { Id = tenantId }, ct);
        if (row is null)
            throw new InvalidOperationException($"Không tìm thấy tenant đang hoạt động với Tenant_Id={tenantId} trong catalog.");

        return Cache(row);
    }

    /// <inheritdoc />
    public async Task<TenantConnections?> ResolveBySubdomainAsync(string subdomain, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(subdomain)) return null;

        if (!IsCatalogConfigured)
            return new TenantConnections(FallbackTenantId, _defaultConfigConn, _defaultDataConn, _defaultAuditConn);

        if (_bySubdomain.TryGetValue(subdomain, out var cached)) return cached;

        const string sql =
            "SELECT Tenant_Id, Subdomain, Config_Conn_Encrypted, Data_Conn_Encrypted, Audit_Conn_Encrypted\n" +
            "FROM dbo.Tenant WHERE Subdomain = @Sub AND Is_Active = 1";
        var row = await QueryTenantAsync(sql, new { Sub = subdomain }, ct);
        return row is null ? null : Cache(row);
    }

    /// <summary>Tra 1 dòng tenant từ Catalog DB.</summary>
    /// <param name="sql">Câu SELECT đã parameterized.</param>
    /// <param name="param">Tham số Dapper.</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>Dòng tenant hoặc null.</returns>
    private async Task<TenantRow?> QueryTenantAsync(string sql, object param, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_catalogConn);
        return await conn.QueryFirstOrDefaultAsync<TenantRow>(
            new CommandDefinition(sql, param, cancellationToken: ct));
    }

    /// <summary>Giải mã + nạp dòng tenant vào cache (id + subdomain). Sự kiện theo sau: lần sau hit cache.</summary>
    /// <param name="row">Dòng đọc từ catalog.</param>
    /// <returns>Cặp connection đã giải mã.</returns>
    private TenantConnections Cache(TenantRow row)
    {
        var dataConn = _protector.Decrypt(row.Data_Conn_Encrypted);
        // Audit DB riêng theo tenant; nếu catalog chưa khai (null/rỗng) → tạm dùng chung Data DB.
        var auditConn = string.IsNullOrWhiteSpace(row.Audit_Conn_Encrypted)
            ? dataConn
            : _protector.Decrypt(row.Audit_Conn_Encrypted);

        var result = new TenantConnections(
            row.Tenant_Id,
            _protector.Decrypt(row.Config_Conn_Encrypted),
            dataConn,
            auditConn);

        _byId[row.Tenant_Id] = result;
        if (!string.IsNullOrWhiteSpace(row.Subdomain))
            _bySubdomain[row.Subdomain] = result;

        _logger.LogDebug("TenantConnectionResolver: nạp tenant {TenantId} ({Subdomain}) vào cache.",
            row.Tenant_Id, row.Subdomain);
        return result;
    }

    private sealed class TenantRow
    {
        public int Tenant_Id { get; init; }
        public string Subdomain { get; init; } = "";
        public string Config_Conn_Encrypted { get; init; } = "";
        public string Data_Conn_Encrypted { get; init; } = "";
        public string? Audit_Conn_Encrypted { get; init; }
    }
}
