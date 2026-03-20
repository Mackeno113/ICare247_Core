// File    : IDbConnectionFactory.cs
// Module  : Common
// Layer   : Application
// Purpose : Factory tạo IDbConnection — ẩn chi tiết SqlConnection khỏi Application layer.

using System.Data;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Factory tạo <see cref="IDbConnection"/> cho Dapper queries.
/// Infrastructure implement bằng <c>SqlConnectionFactory</c>.
/// Connection trả về ở trạng thái CHƯA mở — caller tự mở hoặc Dapper tự mở.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>Tạo connection mới tới database mặc định.</summary>
    IDbConnection CreateConnection();
}
