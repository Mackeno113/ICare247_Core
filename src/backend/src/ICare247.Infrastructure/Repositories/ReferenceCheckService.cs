// File    : ReferenceCheckService.cs
// Module  : MasterData
// Layer   : Infrastructure
// Purpose : Soft-check tham chiếu khóa ngoại theo quy ước tên (DB không có FK vật lý).
//           Quét Sys_Column tìm cột trùng/hậu tố tên PK, đếm usage trên Data DB.

using System.Text.RegularExpressions;
using Dapper;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Kiểm tra tham chiếu "mềm" trước khi xóa cứng 1 bản ghi danh mục.
/// </summary>
/// <remarks>
/// Quy ước: PK <c>CongTyID</c> → cột tham chiếu <c>CongTyID</c> hoặc <c>%_CongTyID</c>.
/// KHÔNG lọc Is_Active khi quét (bắt cả dữ liệu cũ). Mỗi candidate query trong try/catch
/// để cột/bảng đã drop vật lý không làm hỏng cả thao tác.
/// </remarks>
public sealed partial class ReferenceCheckService : IReferenceCheckService
{
    private readonly IDbConnectionFactory     _configDb;
    private readonly IDataDbConnectionFactory _dataDb;
    private readonly ILogger<ReferenceCheckService> _logger;

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex SafeIdentifierRegex();

    public ReferenceCheckService(
        IDbConnectionFactory configDb,
        IDataDbConnectionFactory dataDb,
        ILogger<ReferenceCheckService> logger)
    {
        _configDb = configDb;
        _dataDb   = dataDb;
        _logger   = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReferenceUsage>> CheckUsageAsync(
        int catalogTableId, object? pkValue, CancellationToken ct = default)
    {
        if (pkValue is null) return [];

        using var cfg = _configDb.CreateConnection();

        // ① Schema + tên bảng danh mục (để đọc PK vật lý)
        const string tblSql = "SELECT Schema_Name AS SchemaName, Table_Code AS TableName FROM dbo.Sys_Table WHERE Table_Id = @TableId";
        var tbl = await cfg.QueryFirstOrDefaultAsync<CandidateRow>(
            new CommandDefinition(tblSql, new { TableId = catalogTableId }, cancellationToken: ct));
        if (tbl is null) return [];

        // ① Tên PK — ưu tiên Sys_Column.Is_PK; rỗng → đọc PK vật lý từ Data DB
        const string pkSql = """
            SELECT TOP 1 Column_Code
            FROM   dbo.Sys_Column
            WHERE  Table_Id = @TableId AND Is_PK = 1
            ORDER  BY Column_Id
            """;
        var pkName = await cfg.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(pkSql, new { TableId = catalogTableId }, cancellationToken: ct));

        using var data = _dataDb.CreateConnection();
        if (string.IsNullOrWhiteSpace(pkName))
            pkName = await MasterDataRepository.GetPhysicalPkAsync(
                data, tbl.SchemaName, tbl.TableName, ct);

        if (string.IsNullOrWhiteSpace(pkName) || !SafeIdentifierRegex().IsMatch(pkName))
        {
            _logger.LogWarning(
                "ReferenceCheck: bảng Table_Id={TableId} không có PK hợp lệ ('{Pk}') — bỏ qua soft-check.",
                catalogTableId, pkName);
            return [];
        }

        // ② Cột tham chiếu theo quy ước: trùng tên PK HOẶC hậu tố _<PK>.
        //    KHÔNG lọc Is_Active (bắt cả dữ liệu cũ). Loại chính cột PK của bảng danh mục.
        //    '[_]' để escape underscore (wildcard trong LIKE).
        const string candSql = """
            SELECT t.Schema_Name AS SchemaName,
                   t.Table_Code  AS TableName,
                   sc.Column_Code AS ColumnName,
                   CASE WHEN t.Is_Active = 0 OR sc.Is_Active = 0 THEN 1 ELSE 0 END AS IsLegacy
            FROM   dbo.Sys_Column sc
            JOIN   dbo.Sys_Table  t ON t.Table_Id = sc.Table_Id
            WHERE  (sc.Column_Code = @Pk OR sc.Column_Code LIKE '%[_]' + @Pk)
              AND  NOT (sc.Table_Id = @TableId AND sc.Is_PK = 1)
            """;
        var candidates = (await cfg.QueryAsync<CandidateRow>(
            new CommandDefinition(candSql,
                new { Pk = pkName, TableId = catalogTableId }, cancellationToken: ct))).ToList();

        if (candidates.Count == 0) return [];

        // ③ Đếm usage từng candidate trên Data DB (try/catch — cột/bảng có thể đã drop vật lý)
        var usages = new List<ReferenceUsage>();

        foreach (var c in candidates)
        {
            if (!SafeIdentifierRegex().IsMatch(c.SchemaName)
                || !SafeIdentifierRegex().IsMatch(c.TableName)
                || !SafeIdentifierRegex().IsMatch(c.ColumnName))
                continue;

            var sql = $"SELECT COUNT(*) FROM [{c.SchemaName}].[{c.TableName}] WHERE [{c.ColumnName}] = @V";
            try
            {
                var count = await data.ExecuteScalarAsync<int>(
                    new CommandDefinition(sql, new { V = pkValue }, cancellationToken: ct));
                if (count > 0)
                {
                    usages.Add(new ReferenceUsage(
                        c.SchemaName, c.TableName, c.ColumnName, count, c.IsLegacy == 1));
                    _logger.LogInformation(
                        "ReferenceCheck BLOCK: {Schema}.{Table}.{Column} = {Value} → {Count} dòng{Legacy}.",
                        c.SchemaName, c.TableName, c.ColumnName, pkValue, count,
                        c.IsLegacy == 1 ? " [dữ liệu cũ]" : "");
                }
            }
            catch (Exception ex)
            {
                // Cột/bảng có thể đã bị drop vật lý dù metadata còn → log + bỏ qua, không nổ
                _logger.LogWarning(ex,
                    "ReferenceCheck: bỏ qua {Schema}.{Table}.{Column} (truy vấn lỗi — có thể đã drop).",
                    c.SchemaName, c.TableName, c.ColumnName);
            }
        }

        if (usages.Count > 0)
        {
            var totalRows = usages.Sum(u => u.RowCount);
            _logger.LogWarning(
                "ReferenceCheck: Table_Id={TableId} PK={Pk}={Value} bị khóa bởi {Places} nơi, tổng {Rows} dòng.",
                catalogTableId, pkName, pkValue, usages.Count, totalRows);
        }

        return usages;
    }

    private sealed class CandidateRow
    {
        public string SchemaName { get; init; } = "dbo";
        public string TableName  { get; init; } = "";
        public string ColumnName { get; init; } = "";
        public int    IsLegacy   { get; init; }
    }
}
