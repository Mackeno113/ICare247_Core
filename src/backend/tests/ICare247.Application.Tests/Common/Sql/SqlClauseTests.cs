// File    : SqlClauseTests.cs
// Module  : Common/Sql
// Layer   : Tests
// Purpose : Unit tests cho SqlClause — hàm thuần dựng mảnh SQL (AUDIT-1 ②). Khóa chuỗi xuất đúng
//           bằng bản repo trước khi tách + khẳng định giá trị so khớp đi qua @param (không nội suy).

using ICare247.Application.Common.Sql;

namespace ICare247.Application.Tests.Common.Sql;

public sealed class SqlClauseTests
{
    // ── BracketedColumnList ─────────────────────────────────────────────────

    [Fact]
    public void BracketedColumnList_JoinsWithBrackets()
    {
        Assert.Equal("[A], [B], [C]", SqlClause.BracketedColumnList(new[] { "A", "B", "C" }));
    }

    [Fact]
    public void BracketedColumnList_SingleColumn_NoSeparator()
    {
        Assert.Equal("[Ten]", SqlClause.BracketedColumnList(new[] { "Ten" }));
    }

    [Fact]
    public void BracketedColumnList_Empty_ReturnsEmptyString()
    {
        Assert.Equal("", SqlClause.BracketedColumnList(Array.Empty<string>()));
    }

    [Fact]
    public void BracketedColumnList_EscapesClosingBracket()
    {
        // Dựa vào SqlIdentifier.Bracket — escape ] để không thoát ngoặc.
        Assert.Equal("[a]]b]", SqlClause.BracketedColumnList(new[] { "a]b" }));
    }

    // ── LikeOrGroup (không CAST) — khớp MasterDataRepository.GetListAsync ────

    [Fact]
    public void LikeOrGroup_NoCast_MatchesMasterDataOutput()
    {
        var exprs = new[] { "[Ten]", "[Email]" };
        Assert.Equal(
            "([Ten] LIKE @Search OR [Email] LIKE @Search)",
            SqlClause.LikeOrGroup(exprs, "Search"));
    }

    [Fact]
    public void LikeOrGroup_SingleExpr_NoOr()
    {
        Assert.Equal("([Ten] LIKE @Search)", SqlClause.LikeOrGroup(new[] { "[Ten]" }, "Search"));
    }

    // ── LikeOrGroup (có CAST) — khớp ViewRepository.BuildQueryContextAsync ──

    [Fact]
    public void LikeOrGroup_WithCast_MatchesViewRepositoryOutput()
    {
        var exprs = new[] { "b.[Ten]", "[_fk0].[TenTinh]" };
        Assert.Equal(
            "(CAST(b.[Ten] AS NVARCHAR(4000)) LIKE @Search OR CAST([_fk0].[TenTinh] AS NVARCHAR(4000)) LIKE @Search)",
            SqlClause.LikeOrGroup(exprs, "Search", castLength: 4000));
    }

    [Fact]
    public void LikeOrGroup_Empty_ReturnsNull()
    {
        Assert.Null(SqlClause.LikeOrGroup(Array.Empty<string>(), "Search"));
    }

    [Fact]
    public void LikeOrGroup_CustomParamName()
    {
        Assert.Equal("([Ma] LIKE @Kw)", SqlClause.LikeOrGroup(new[] { "[Ma]" }, "Kw"));
    }

    /// <summary>
    /// Ý ĐỊNH BẢO MẬT: giá trị so khớp luôn qua <c>@param</c>, hàm KHÔNG nội suy dữ liệu người dùng.
    /// Output chỉ chứa placeholder tham số, không có literal giá trị.
    /// </summary>
    [Fact]
    public void LikeOrGroup_UsesParameterPlaceholder_NotInterpolatedValue()
    {
        var result = SqlClause.LikeOrGroup(new[] { "[Ten]" }, "Search");

        Assert.Contains("LIKE @Search", result);
        // Không có dấu nháy/nội suy giá trị — chỉ identifier + placeholder.
        Assert.DoesNotContain("'", result);
        Assert.DoesNotContain("%", result);
    }
}
