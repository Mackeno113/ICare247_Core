// File    : DbErrorClassifier.cs
// Module  : Infrastructure.Logging
// Layer   : Presentation
// Purpose : Phân loại exception là lỗi SQL Server hay lỗi C#/.NET.
//           Mọi lỗi từ SQL Server (constraint, deadlock, timeout, mất kết nối...)
//           đều ném SqlException — chỉ cần dò chuỗi inner để phân loại.

using Microsoft.Data.SqlClient;

namespace ConfigStudio.WPF.UI.Infrastructure.Logging;

/// <summary>
/// Tách lỗi SQL khỏi lỗi C#. Dùng chung cho logger và (tùy chọn) tầng map
/// mã lỗi SQL → thông báo tiếng Việt thân thiện cho người dùng.
/// </summary>
public static class DbErrorClassifier
{
    /// <summary>
    /// Dò toàn bộ chuỗi InnerException tìm <see cref="SqlException"/>.
    /// Dapper/ADO đôi khi bọc SqlException trong exception khác.
    /// </summary>
    /// <returns><c>true</c> nếu là lỗi SQL — <paramref name="sql"/> chứa exception gốc.</returns>
    public static bool TryGetSqlException(Exception ex, out SqlException? sql)
    {
        for (Exception? cur = ex; cur is not null; cur = cur.InnerException)
        {
            if (cur is SqlException se)
            {
                sql = se;
                return true;
            }
        }

        sql = null;
        return false;
    }

    /// <summary>
    /// Map một số mã lỗi SQL Server phổ biến → thông báo tiếng Việt.
    /// Trả về <c>null</c> nếu mã lỗi chưa có mapping (caller dùng message mặc định).
    /// </summary>
    public static string? ToFriendlyMessage(SqlException sql) => sql.Number switch
    {
        2627 or 2601 => "Dữ liệu bị trùng (vi phạm ràng buộc duy nhất).",
        547          => "Thao tác vi phạm ràng buộc khóa ngoại / kiểm tra dữ liệu.",
        1205         => "Hệ thống đang bận (deadlock). Vui lòng thử lại.",
        -2           => "Kết nối hoặc truy vấn quá thời gian chờ (timeout).",
        18456        => "Đăng nhập SQL Server thất bại (sai tài khoản/mật khẩu).",
        4060         => "Không mở được database (sai tên DB hoặc không có quyền).",
        53 or 40 or 10060 or 10061
                     => "Không kết nối được tới SQL Server (kiểm tra mạng/instance).",
        _            => null
    };
}
