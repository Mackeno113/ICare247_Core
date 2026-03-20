// File    : SqlConnectionFactory.cs
// Module  : Data
// Layer   : Infrastructure
// Purpose : Implement IDbConnectionFactory — tạo SqlConnection từ connection string.

using System.Data;
using ICare247.Application.Interfaces;
using Microsoft.Data.SqlClient;

namespace ICare247.Infrastructure.Data;

/// <summary>
/// Factory tạo <see cref="SqlConnection"/>.
/// Connection trả về CHƯA mở — Dapper tự mở khi cần.
/// Đăng ký DI: Singleton (connection string không đổi trong lifetime).
/// </summary>
public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
