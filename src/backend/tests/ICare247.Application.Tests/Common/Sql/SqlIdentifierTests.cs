// File    : SqlIdentifierTests.cs
// Module  : Common/Sql
// Layer   : Tests
// Purpose : Unit tests cho SqlIdentifier — guard CHUNG chống SQL injection (AUDIT-2).
//           Khóa hành vi whitelist identifier + bracket escape + blocklist, kèm ca injection,
//           để mọi thay đổi sau này lộ ngay nếu nới lỏng guard. Xem .claude-rules/sql-safety.md.

using ICare247.Application.Common.Sql;

namespace ICare247.Application.Tests.Common.Sql;

public sealed class SqlIdentifierTests
{
    // ── IsSafe — identifier đơn (không dấu chấm) ────────────────────────────

    [Theory]
    // Hợp lệ
    [InlineData("TenDangNhap", true)]
    [InlineData("Ma_KH", true)]
    [InlineData("_hidden", true)]
    [InlineData("a", true)]
    [InlineData("Col123", true)]
    [InlineData("TABLE_NAME", true)]
    // Null/rỗng → deny-by-default
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    // Bắt đầu bằng số
    [InlineData("1abc", false)]
    // Ký tự không hợp lệ / khoảng trắng
    [InlineData("a b", false)]
    [InlineData("a-b", false)]
    [InlineData("a.b", false)]   // dấu chấm KHÔNG được với identifier đơn
    // Ca injection
    [InlineData("a;DROP TABLE Users", false)]
    [InlineData("col--", false)]
    [InlineData("a']b", false)]
    [InlineData("1=1", false)]
    [InlineData("café", false)]  // ký tự ngoài ASCII
    public void IsSafe_ValidatesSimpleIdentifier(string? input, bool expected)
    {
        Assert.Equal(expected, SqlIdentifier.IsSafe(input));
    }

    // ── IsSafeQualified — cho phép schema.table (có dấu chấm) ────────────────

    [Theory]
    // Hợp lệ
    [InlineData("dbo.Bang", true)]
    [InlineData("Bang", true)]
    [InlineData("schema.Table_1", true)]
    [InlineData("_x", true)]
    // Null/rỗng
    [InlineData(null, false)]
    [InlineData("", false)]
    // Bắt đầu bằng dấu chấm / số
    [InlineData(".table", false)]
    [InlineData("1.a", false)]
    // Ca injection
    [InlineData("dbo.Bang;DROP", false)]
    [InlineData("a b.c", false)]
    [InlineData("a'.b", false)]
    public void IsSafeQualified_AllowsDottedName(string? input, bool expected)
    {
        Assert.Equal(expected, SqlIdentifier.IsSafeQualified(input));
    }

    /// <summary>Identifier đơn hợp lệ cũng luôn qua IsSafeQualified (superset).</summary>
    [Theory]
    [InlineData("Col")]
    [InlineData("Ma_KH")]
    public void IsSafeQualified_AcceptsWhatIsSafeAccepts(string input)
    {
        Assert.True(SqlIdentifier.IsSafe(input));
        Assert.True(SqlIdentifier.IsSafeQualified(input));
    }

    // ── Bracket — bọc [] + escape ] ─────────────────────────────────────────

    [Theory]
    [InlineData("Col", "[Col]")]
    [InlineData("Ten_Dang_Nhap", "[Ten_Dang_Nhap]")]
    public void Bracket_WrapsIdentifier(string input, string expected)
    {
        Assert.Equal(expected, SqlIdentifier.Bracket(input));
    }

    /// <summary>
    /// Escape <c>]</c> → <c>]]</c> — chống thoát khỏi cặp ngoặc (chính chỗ bản Bracket cũ ở
    /// MasterDataRepository <c>$"[{id}]"</c> thiếu, AUDIT-2 đã sửa).
    /// </summary>
    [Fact]
    public void Bracket_EscapesClosingBracket()
    {
        Assert.Equal("[a]]b]", SqlIdentifier.Bracket("a]b"));
        Assert.Equal("[]]]", SqlIdentifier.Bracket("]"));
    }

    // ── BracketQualified — bọc schema.table ─────────────────────────────────

    [Theory]
    [InlineData("dbo.Bang", "[dbo].[Bang]")]
    [InlineData("Bang", "[dbo].[Bang]")]            // 1 phần → thêm schema mặc định
    [InlineData("sys.Objects", "[sys].[Objects]")]
    // Không hợp lệ → null
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("a.b.c", null)]                     // >2 phần
    [InlineData("dbo.Bang;DROP", null)]             // phần chứa injection
    [InlineData("1abc.def", null)]                  // phần không hợp lệ
    public void BracketQualified_HandlesSchemaTable(string? input, string? expected)
    {
        Assert.Equal(expected, SqlIdentifier.BracketQualified(input));
    }

    [Fact]
    public void BracketQualified_UsesCustomDefaultSchema()
    {
        Assert.Equal("[app].[Bang]", SqlIdentifier.BracketQualified("Bang", "app"));
    }

    // ── ContainsDangerousKeyword — blocklist DDL/DML ────────────────────────

    [Theory]
    [InlineData("DROP TABLE Users")]
    [InlineData("x; DELETE FROM a")]
    [InlineData("EXEC sp_who")]
    [InlineData("EXECUTE(@s)")]
    [InlineData("TRUNCATE TABLE t")]
    [InlineData("ALTER TABLE t ADD c INT")]
    [InlineData("CREATE TABLE t(x INT)")]
    [InlineData("MERGE INTO t")]
    [InlineData("INSERT INTO t VALUES(1)")]
    [InlineData("UPDATE t SET x=1")]
    [InlineData("col -- comment")]
    [InlineData("a=1;")]
    [InlineData("drop table t")]   // case-insensitive
    public void ContainsDangerousKeyword_DetectsDdlDml(string sql)
    {
        Assert.True(SqlIdentifier.ContainsDangerousKeyword(sql));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Status = 1")]
    [InlineData("Name = @p")]
    [InlineData("Age > 18 AND Active = 1")]
    [InlineData("MaKH = @code")]
    public void ContainsDangerousKeyword_AllowsCleanFragment(string? sql)
    {
        Assert.False(SqlIdentifier.ContainsDangerousKeyword(sql));
    }

    /// <summary>
    /// Giới hạn ĐÃ BIẾT: blocklist khớp chuỗi con nên tên chứa keyword (VD "UpdatedAt" chứa
    /// "UPDATE") bị cờ dương giả. Chấp nhận vì đây chỉ là lớp PHỤ cho fragment admin-trust;
    /// test này khóa hành vi để mọi thay đổi (VD chuyển sang khớp ranh giới từ) là có chủ đích.
    /// </summary>
    [Theory]
    [InlineData("UpdatedAt")]
    [InlineData("CreatedBy")]
    public void ContainsDangerousKeyword_SubstringFalsePositive_IsKnownBehavior(string columnName)
    {
        Assert.True(SqlIdentifier.ContainsDangerousKeyword(columnName));
    }
}
