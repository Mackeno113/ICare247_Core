// File    : SqlPagingTests.cs
// Module  : Common/Sql
// Layer   : Tests
// Purpose : Unit tests cho SqlPaging — khóa chuỗi câu phân trang/đếm đúng bằng bản repo trước khi tách,
//           và khẳng định cột ORDER BY được bọc [] (whitelist) + phân trang qua @Skip/@Take/@Cap.

using ICare247.Application.Common.Sql;

namespace ICare247.Application.Tests.Common.Sql;

public sealed class SqlPagingTests
{
    // ── OffsetFetch — khớp MasterData.GetListAsync + ViewRepository.GetDataAsync ──

    [Fact]
    public void OffsetFetch_WithWhere_BuildsPagedQuery()
    {
        Assert.Equal(
            "SELECT [A], [B] FROM dbo.[T] WHERE [x] = @p ORDER BY [Id] OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            SqlPaging.OffsetFetch("[A], [B]", "dbo.[T]", " WHERE [x] = @p", "Id"));
    }

    [Fact]
    public void OffsetFetch_EmptyWhere_NoDoubleSpace()
    {
        Assert.Equal(
            "SELECT [A] FROM [T] ORDER BY [Id] OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            SqlPaging.OffsetFetch("[A]", "[T]", "", "Id"));
    }

    [Fact]
    public void OffsetFetch_BracketsOrderColumn()
    {
        // Cột ORDER BY phải được bọc [] (escape ]) — không nội suy thô.
        var sql = SqlPaging.OffsetFetch("[A]", "[T]", "", "a]b");
        Assert.Contains("ORDER BY [a]]b] OFFSET", sql);
    }

    // ── TopWithOrder — khớp ViewRepository.GetAllDataAsync (export) ──

    [Fact]
    public void TopWithOrder_BuildsCappedExportQuery()
    {
        Assert.Equal(
            "SELECT TOP (@Cap) [A], [B] FROM [T] WHERE [x] = @p ORDER BY [Id]",
            SqlPaging.TopWithOrder("[A], [B]", "[T]", " WHERE [x] = @p", "Id"));
    }

    [Fact]
    public void TopWithOrder_EmptyWhere()
    {
        Assert.Equal(
            "SELECT TOP (@Cap) [A] FROM [T] ORDER BY [Id]",
            SqlPaging.TopWithOrder("[A]", "[T]", "", "Id"));
    }

    // ── Count ──

    [Fact]
    public void Count_WithWhere()
    {
        Assert.Equal(
            "SELECT COUNT(*) FROM [T] WHERE [x] = @p",
            SqlPaging.Count("[T]", " WHERE [x] = @p"));
    }

    [Fact]
    public void Count_EmptyWhere()
    {
        Assert.Equal("SELECT COUNT(*) FROM [T]", SqlPaging.Count("[T]", ""));
    }
}
