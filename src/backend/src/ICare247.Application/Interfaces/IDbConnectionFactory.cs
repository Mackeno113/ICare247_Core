// File    : IDbConnectionFactory.cs
// Module  : Common
// Layer   : Application
// Purpose : Factory tạo IDbConnection — ẩn chi tiết SqlConnection khỏi Application layer.

using System.Data;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Factory tạo <see cref="IDbConnection"/> cho Config DB — DB chứa metadata form engine.
/// Trỏ vào <c>ICare247_Config</c>: Ui_Form, Ui_Field, Sys_*, Val_*, Evt_*, Gram_*.
/// Infrastructure implement bằng <c>SqlConnectionFactory</c>.
/// Connection trả về ở trạng thái CHƯA mở — caller tự mở hoặc Dapper tự mở.
/// </summary>
/// <seealso cref="IDataDbConnectionFactory"/> — factory cho Data DB nghiệp vụ.
public interface IDbConnectionFactory
{
    /// <summary>Tạo connection mới tới Config DB.</summary>
    IDbConnection CreateConnection();
}
