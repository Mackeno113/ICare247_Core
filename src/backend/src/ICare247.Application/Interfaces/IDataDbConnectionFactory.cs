// File    : IDataDbConnectionFactory.cs
// Module  : Common
// Layer   : Application
// Purpose : Factory tạo connection tới Data DB (DB nghiệp vụ thực tế).
//           Tách biệt hoàn toàn với IDbConnectionFactory (Config DB).

using System.Data;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Factory tạo <see cref="IDbConnection"/> cho Data DB — DB nghiệp vụ thực tế
/// (bệnh nhân, hồ sơ, phiếu, ...).
/// Phân biệt với <see cref="IDbConnectionFactory"/> trỏ vào Config DB.
/// </summary>
/// <remarks>
/// Repository nào thao tác dữ liệu nghiệp vụ → inject IDataDbConnectionFactory.
/// Repository nào đọc metadata (form, field, rule,...) → inject IDbConnectionFactory.
/// </remarks>
public interface IDataDbConnectionFactory
{
    /// <summary>Tạo connection mới tới Data DB.</summary>
    IDbConnection CreateConnection();
}
