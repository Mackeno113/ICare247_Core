// File    : SqlClause.cs
// Module  : Common/Sql
// Layer   : Application
// Purpose : Hàm THUẦN dựng mảnh SQL lặp lại (danh sách cột, nhóm LIKE search) — tách khỏi repo để
//           unit-test không cần DB (AUDIT-1 ②). Đi kèm SqlIdentifier (whitelist + bracket).
//           Giá trị người dùng LUÔN qua @param — các hàm ở đây chỉ ghép identifier/biểu thức ĐÃ an toàn.

namespace ICare247.Application.Common.Sql;

/// <summary>
/// Bộ dựng mảnh mệnh đề SQL dùng chung cho repository (SELECT list, WHERE search…).
/// Thuần (không I/O) để test được; KHÔNG nội suy giá trị người dùng — chỉ ghép identifier/biểu thức
/// đã whitelist (qua <see cref="SqlIdentifier"/>) và tham chiếu <c>@param</c>.
/// </summary>
public static class SqlClause
{
    /// <summary>
    /// Ghép danh sách cột thành CSV có bọc <c>[]</c>: <c>["A","B"]</c> → <c>"[A], [B]"</c>.
    /// Cột phải ĐÃ whitelist (<see cref="SqlIdentifier.IsSafe"/>) trước khi gọi.
    /// </summary>
    public static string BracketedColumnList(IEnumerable<string> safeColumns)
        => string.Join(", ", safeColumns.Select(SqlIdentifier.Bracket));

    /// <summary>
    /// Dựng nhóm OR các điều kiện LIKE trên <paramref name="expressions"/> (đã an toàn), bọc trong
    /// ngoặc đơn: <c>"(expr1 LIKE @p OR expr2 LIKE @p)"</c>. Giá trị so khớp truyền qua <c>@paramName</c>
    /// (caller tự bind, thường bọc <c>%...%</c>) — KHÔNG nội suy vào chuỗi.
    /// <para>
    /// <paramref name="castLength"/> &gt; 0 → bọc mỗi biểu thức trong <c>CAST(expr AS NVARCHAR(n))</c>
    /// để LIKE chạy trên mọi kiểu cột. Trả <c>null</c> nếu danh sách rỗng (caller không thêm WHERE).
    /// </para>
    /// </summary>
    public static string? LikeOrGroup(IReadOnlyList<string> expressions, string paramName, int castLength = 0)
    {
        if (expressions.Count == 0) return null;

        var param = "@" + paramName;
        var terms = castLength > 0
            ? expressions.Select(e => $"CAST({e} AS NVARCHAR({castLength})) LIKE {param}")
            : expressions.Select(e => $"{e} LIKE {param}");

        return "(" + string.Join(" OR ", terms) + ")";
    }
}
