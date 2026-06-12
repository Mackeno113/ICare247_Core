// File    : ReferenceCheckService.cs
// Module  : MasterData
// Layer   : Infrastructure
// Purpose : Soft-check tham chiếu khóa ngoại trước khi xóa cứng (DB không có FK vật lý).
//           ƯU TIÊN registry tường minh Sys_Relation (Detail_FK_Column) — đọc đúng cột FK,
//           xử lý được nhiều FK cùng nguồn (NoiSinh_/ThuongTru_...). Bảng CHƯA khai quan hệ
//           → fallback dò theo quy ước tên (tương thích ngược trong giai đoạn chuyển tiếp).

using System.Text.RegularExpressions;
using Dapper;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Kiểm tra tham chiếu "mềm" trước khi xóa cứng 1 bản ghi danh mục.
/// </summary>
/// <remarks>
/// Thứ tự: (1) tra <c>Sys_Relation</c> theo Master_Table_Id → mỗi quan hệ cho cột FK ở bảng con
/// (chính xác, không đoán). (2) Nếu bảng chưa khai quan hệ nào → fallback quy ước tên PK
/// (<c>CongTyID</c> → cột <c>CongTyID</c> hoặc <c>%_CongTyID</c>). Mỗi candidate query trong
/// try/catch để cột/bảng đã drop vật lý không làm hỏng cả thao tác.
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

        // ── ① ƯU TIÊN: candidate từ Sys_Relation (tường minh) ──────────────────
        var candidates = await GetCandidatesFromRelationsAsync(cfg, catalogTableId, ct);

        // ── ② FALLBACK: chưa khai quan hệ nào → dò theo quy ước tên PK ──────────
        if (candidates.Count == 0)
            candidates = await GetCandidatesByNameConventionAsync(cfg, catalogTableId, ct);

        if (candidates.Count == 0) return [];

        // ── ③ Đếm usage từng candidate trên Data DB ─────────────────────────────
        using var data = _dataDb.CreateConnection();
        var usages = await CountUsagesAsync(data, candidates, pkValue, ct);

        if (usages.Count > 0)
        {
            var totalRows = usages.Sum(u => u.RowCount);
            _logger.LogWarning(
                "ReferenceCheck: Table_Id={TableId} value={Value} bị khóa bởi {Places} nơi, tổng {Rows} dòng.",
                catalogTableId, pkValue, usages.Count, totalRows);
        }

        return usages;
    }

    /// <summary>
    /// Lấy candidate tham chiếu từ <c>Sys_Relation</c> — mỗi quan hệ active có Detail_FK_Column
    /// cho ra (schema + bảng con + cột FK). Đây là đường chính xác (không đoán theo tên).
    /// </summary>
    /// <param name="cfg">Kết nối Config DB.</param>
    /// <param name="catalogTableId">Table_Id của bảng đang xóa (vai trò master).</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>Danh sách candidate; rỗng nếu chưa khai quan hệ hoặc schema chưa migrate.</returns>
    /// <remarks>
    /// Bọc try/catch: nếu cột Detail_FK_Column chưa tồn tại (migration 035 chưa chạy trên DB này)
    /// → trả rỗng để caller fallback sang quy ước tên, không làm hỏng thao tác xóa.
    /// </remarks>
    private async Task<List<CandidateRow>> GetCandidatesFromRelationsAsync(
        System.Data.IDbConnection cfg, int catalogTableId, CancellationToken ct)
    {
        const string sql = """
            SELECT dt.Schema_Name  AS SchemaName,
                   dt.Table_Code   AS TableName,
                   r.Detail_FK_Column AS ColumnName,
                   CASE WHEN dt.Is_Active = 0 THEN 1 ELSE 0 END AS IsLegacy
            FROM   dbo.Sys_Relation r
            JOIN   dbo.Sys_Table   dt ON dt.Table_Id = r.Detail_Table_Id
            WHERE  r.Master_Table_Id = @TableId
              AND  r.Is_Active = 1
              AND  r.Detail_FK_Column IS NOT NULL
            """;
        try
        {
            var rows = await cfg.QueryAsync<CandidateRow>(
                new CommandDefinition(sql, new { TableId = catalogTableId }, cancellationToken: ct));
            return rows.ToList();
        }
        catch (Exception ex)
        {
            // Sys_Relation chưa mở rộng (migration 035) hoặc lỗi khác → để caller fallback.
            _logger.LogWarning(ex,
                "ReferenceCheck: không đọc được Sys_Relation cho Table_Id={TableId} — fallback quy ước tên.",
                catalogTableId);
            return [];
        }
    }

    /// <summary>
    /// Fallback: dò candidate theo quy ước tên PK — cột trùng tên PK HOẶC hậu tố <c>_&lt;PK&gt;</c>.
    /// Dùng khi bảng chưa khai quan hệ nào trong Sys_Relation (tương thích ngược).
    /// </summary>
    /// <param name="cfg">Kết nối Config DB.</param>
    /// <param name="catalogTableId">Table_Id của bảng đang xóa.</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>Danh sách candidate; rỗng nếu không xác định được PK.</returns>
    private async Task<List<CandidateRow>> GetCandidatesByNameConventionAsync(
        System.Data.IDbConnection cfg, int catalogTableId, CancellationToken ct)
    {
        // ① Schema + tên bảng danh mục (để đọc PK vật lý nếu cần)
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

        if (string.IsNullOrWhiteSpace(pkName))
        {
            using var data = _dataDb.CreateConnection();
            pkName = await MasterDataRepository.GetPhysicalPkAsync(
                data, tbl.SchemaName, tbl.TableName, ct);
        }

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
        var candidates = await cfg.QueryAsync<CandidateRow>(
            new CommandDefinition(candSql,
                new { Pk = pkName, TableId = catalogTableId }, cancellationToken: ct));
        return candidates.ToList();
    }

    /// <summary>
    /// Đếm số dòng tham chiếu của từng candidate trên Data DB.
    /// </summary>
    /// <param name="data">Kết nối Data DB.</param>
    /// <param name="candidates">Danh sách (schema, bảng, cột) cần đếm.</param>
    /// <param name="pkValue">Giá trị khóa của bản ghi đang xóa.</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>Danh sách nơi đang dùng (RowCount &gt; 0).</returns>
    /// <remarks>Mỗi candidate query trong try/catch — cột/bảng có thể đã drop vật lý dù metadata còn.</remarks>
    private async Task<List<ReferenceUsage>> CountUsagesAsync(
        System.Data.IDbConnection data, List<CandidateRow> candidates, object pkValue, CancellationToken ct)
    {
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
