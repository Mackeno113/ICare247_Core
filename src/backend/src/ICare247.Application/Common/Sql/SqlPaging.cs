// File    : SqlPaging.cs
// Module  : Common/Sql
// Layer   : Application
// Purpose : Hàm THUẦN dựng câu SELECT phân trang / đếm — tách khỏi repo để unit-test không cần DB
//           (AUDIT-1 ②). Quy ước tham số: @Skip/@Take (phân trang), @Cap (trần export) do caller bind.

namespace ICare247.Application.Common.Sql;

/// <summary>
/// Bộ dựng câu truy vấn phân trang MS SQL Server (OFFSET/FETCH, TOP) + đếm tổng.
/// Thuần (không I/O); <paramref name="orderColumn"/> được bọc <c>[]</c> qua <see cref="SqlIdentifier"/>.
/// <c>selectList</c>/<c>fromClause</c> phải ĐÃ an toàn (identifier whitelist); <c>whereClause</c> đã gồm
/// tiền tố <c>" WHERE "</c> hoặc rỗng.
/// </summary>
public static class SqlPaging
{
    /// <summary>
    /// Câu phân trang: <c>SELECT … FROM …[WHERE…] ORDER BY [col] OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY</c>.
    /// Caller bind <c>@Skip</c>/<c>@Take</c>.
    /// </summary>
    public static string OffsetFetch(string selectList, string fromClause, string whereClause, string orderColumn)
        => $"SELECT {selectList} FROM {fromClause}{whereClause} " +
           $"ORDER BY {SqlIdentifier.Bracket(orderColumn)} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

    /// <summary>
    /// Câu xuất toàn bộ có trần: <c>SELECT TOP (@Cap) … FROM …[WHERE…] ORDER BY [col]</c> (không phân trang).
    /// Caller bind <c>@Cap</c>.
    /// </summary>
    public static string TopWithOrder(string selectList, string fromClause, string whereClause, string orderColumn)
        => $"SELECT TOP (@Cap) {selectList} FROM {fromClause}{whereClause} " +
           $"ORDER BY {SqlIdentifier.Bracket(orderColumn)}";

    /// <summary>Câu đếm tổng khớp cùng FROM/WHERE: <c>SELECT COUNT(*) FROM …[WHERE…]</c>.</summary>
    public static string Count(string fromClause, string whereClause)
        => $"SELECT COUNT(*) FROM {fromClause}{whereClause}";
}
