// File    : TenantConnectionFactories.cs
// Module  : Data
// Layer   : Infrastructure
// Purpose : Factory connection TENANT-AWARE (scoped) — đọc connection string từ TenantContext
//           (do TenantMiddleware phân giải qua ITenantConnectionResolver). Thay cho
//           SqlConnectionFactory singleton-cố-định. Xem ADR-018.

using System.Data;
using ICare247.Application.Interfaces;
using Microsoft.Data.SqlClient;

namespace ICare247.Infrastructure.Data;

/// <summary>
/// Factory Config DB theo tenant của request hiện tại. Connection chưa mở.
/// </summary>
public sealed class ConfigDbConnectionFactory : IDbConnectionFactory
{
    private readonly TenantContext _tenant;

    /// <summary>Khởi tạo với TenantContext scoped của request.</summary>
    /// <param name="tenant">Context chứa connection string đã phân giải.</param>
    public ConfigDbConnectionFactory(TenantContext tenant) => _tenant = tenant;

    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_tenant.ConfigConnectionString))
            throw new InvalidOperationException(
                "Config connection string chưa được phân giải cho request " +
                "(TenantMiddleware chưa chạy hoặc tenant chưa xác định).");
        return new SqlConnection(_tenant.ConfigConnectionString);
    }
}

/// <summary>
/// Factory Data DB theo tenant của request hiện tại. Connection chưa mở.
/// </summary>
public sealed class DataDbConnectionFactory : IDataDbConnectionFactory
{
    private readonly TenantContext _tenant;

    /// <summary>Khởi tạo với TenantContext scoped của request.</summary>
    /// <param name="tenant">Context chứa connection string đã phân giải.</param>
    public DataDbConnectionFactory(TenantContext tenant) => _tenant = tenant;

    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_tenant.DataConnectionString))
            throw new InvalidOperationException(
                "Data connection string chưa được phân giải cho request " +
                "(TenantMiddleware chưa chạy hoặc tenant chưa xác định).");
        return new SqlConnection(_tenant.DataConnectionString);
    }
}
