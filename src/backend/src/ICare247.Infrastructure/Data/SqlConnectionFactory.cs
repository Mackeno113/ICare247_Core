// File    : SqlConnectionFactory.cs
// Module  : Data
// Layer   : Infrastructure
// Purpose : Implement IDbConnectionFactory (Config DB) và IDataDbConnectionFactory (Data DB).
//           Mỗi instance giữ 1 connection string riêng.
//           DI đăng ký 2 singleton riêng biệt — xem Infrastructure/DependencyInjection.cs.

using System.Data;
using ICare247.Application.Interfaces;
using Microsoft.Data.SqlClient;

namespace ICare247.Infrastructure.Data;

/// <summary>
/// Factory tạo <see cref="SqlConnection"/>.
/// Dùng cho cả Config DB (<see cref="IDbConnectionFactory"/>)
/// lẫn Data DB (<see cref="IDataDbConnectionFactory"/>).
/// Connection trả về CHƯA mở — Dapper tự mở khi cần.
/// </summary>
public sealed class SqlConnectionFactory : IDbConnectionFactory, IDataDbConnectionFactory
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
