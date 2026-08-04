// File    : SqlIdentifier.cs
// Module  : Common/Sql
// Layer   : Application
// Purpose : Guard CHUNG cho SQL identifier (tên bảng/cột) + blocklist fragment SQL thô.
//           Gom mọi bản copy trước đây (SafeIdentifierRegex/Bracket/ContainsDangerousKeyword rải
//           rác ≥11 file) về MỘT chỗ để guard bảo mật không drift. Xem .claude-rules/sql-safety.md.

using System.Text.RegularExpressions;

namespace ICare247.Application.Common.Sql;

/// <summary>
/// Helper an toàn SQL dùng chung cho MỌI repository/service dựng SQL động.
/// <para>
/// Quy tắc (sql-safety.md): giá trị → LUÔN qua Dapper param; identifier → whitelist + bracket;
/// fragment SQL thô (FilterSql/OrderBy/custom_sql) → blocklist DDL/DML như lớp phụ.
/// </para>
/// ⛔ KHÔNG tự chế lại regex/bracket riêng trong repo khác — gọi vào đây.
/// </summary>
public static partial class SqlIdentifier
{
    /// <summary>Identifier đơn (tên cột/bảng) — chữ/số/gạch dưới, KHÔNG dấu chấm.</summary>
    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex SimpleIdentifierRegex();

    /// <summary>Identifier có thể chấm hóa (schema.table) — cho phép dấu chấm.</summary>
    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_.]*$", RegexOptions.Compiled)]
    private static partial Regex QualifiedIdentifierRegex();

    /// <summary>Keyword DDL/DML nguy hiểm — chặn trong FilterSql / OrderBy / custom_sql (lớp phụ).</summary>
    private static readonly string[] DangerousKeywords =
        ["DROP", "DELETE", "INSERT", "UPDATE", "EXEC", "EXECUTE", "TRUNCATE", "ALTER", "CREATE", "MERGE", "--", ";"];

    /// <summary>
    /// True nếu là identifier đơn an toàn (không dấu chấm). Null/rỗng/whitespace → false (deny-by-default).
    /// </summary>
    public static bool IsSafe(string? identifier)
        => !string.IsNullOrWhiteSpace(identifier) && SimpleIdentifierRegex().IsMatch(identifier);

    /// <summary>
    /// True nếu là identifier chấm hóa an toàn (cho phép <c>schema.table</c>). Null/rỗng → false.
    /// Dùng cho <c>Source_Name</c> có thể là <c>schema.table</c>; cột đơn nên ưu tiên <see cref="IsSafe"/>.
    /// </summary>
    public static bool IsSafeQualified(string? identifier)
        => !string.IsNullOrWhiteSpace(identifier) && QualifiedIdentifierRegex().IsMatch(identifier);

    /// <summary>Bọc identifier bằng <c>[]</c>, escape <c>]</c> → <c>]]</c>. Gọi SAU khi đã <see cref="IsSafe"/>.</summary>
    public static string Bracket(string identifier) => "[" + identifier.Replace("]", "]]") + "]";

    /// <summary>
    /// Bọc tên có thể chấm hóa (tối đa 2 phần): <c>schema.table</c> → <c>[schema].[table]</c>;
    /// <c>table</c> → <c>[defaultSchema].[table]</c>. Trả <c>null</c> nếu &gt;2 phần hoặc phần nào không hợp lệ.
    /// </summary>
    public static string? BracketQualified(string? name, string defaultSchema = "dbo")
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var parts = name.Split('.');
        if (parts.Length is < 1 or > 2 || parts.Any(p => !IsSafe(p)))
            return null;
        return parts.Length == 2
            ? $"{Bracket(parts[0])}.{Bracket(parts[1])}"
            : $"{Bracket(defaultSchema)}.{Bracket(parts[0])}";
    }

    /// <summary>
    /// True nếu <paramref name="sql"/> chứa keyword DDL/DML nguy hiểm. Null/rỗng → false.
    /// Lớp PHỤ cho fragment SQL thô admin-trust; KHÔNG thay cho whitelist/tham số hóa.
    /// </summary>
    public static bool ContainsDangerousKeyword(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return false;
        var upper = sql.ToUpperInvariant();
        return DangerousKeywords.Any(kw => upper.Contains(kw));
    }
}
