// File    : PickerRepository.cs
// Module  : Pickers
// Layer   : Infrastructure
// Purpose : Dapper impl IPickerRepository — danh mục địa bàn (DM_TinhThanhPho / DM_PhuongXa,
//           Data DB tenant; schema db/037). Parameterized 100%, không SELECT *.

using Dapper;
using ICare247.Application.Features.Pickers;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Đọc danh mục cho picker dùng chung (read-only).</summary>
public sealed class PickerRepository : IPickerRepository
{
    private readonly IDataDbConnectionFactory _db;

    public PickerRepository(IDataDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PickerItemDto>> GetTinhThanhAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, Ma, Ten, CAST(NULL AS BIGINT) AS ParentId
            FROM dbo.DM_TinhThanhPho
            WHERE IsDeleted = 0
            ORDER BY Ten;
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<PickerItemDto>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PickerItemDto>> SearchPhuongXaAsync(
        long tinhThanhPhoId, string? keyword, int top, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP (@Top) Id, Ma, Ten, TinhThanhPho_Id AS ParentId
            FROM dbo.DM_PhuongXa
            WHERE IsDeleted = 0
              AND TinhThanhPho_Id = @TinhId
              AND (@Keyword IS NULL OR Ten LIKE N'%' + @Keyword + N'%' OR Ma LIKE N'%' + @Keyword + N'%')
            ORDER BY Ten;
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<PickerItemDto>(new CommandDefinition(sql, new
        {
            TinhId = tinhThanhPhoId,
            Keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim(),
            Top = top
        }, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<PickerItemDto?> GetPhuongXaByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, Ma, Ten, TinhThanhPho_Id AS ParentId
            FROM dbo.DM_PhuongXa
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PickerItemDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
