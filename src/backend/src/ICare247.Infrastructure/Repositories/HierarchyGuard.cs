// File    : HierarchyGuard.cs
// Module  : MasterData
// Layer   : Infrastructure
// Purpose : Chống self-parent + vòng lặp cho MỌI bảng CÂY khi ghi (xem IHierarchyGuard).
//           Cột cha tự tham chiếu = Sys_Relation có Master_Table_Id = Detail_Table_Id (Config DB).
//           Kiểm vòng lặp gián tiếp bằng cách đi NGƯỢC chuỗi tổ tiên trên Data DB (tenant-isolated
//           ở tầng connection). KHÔNG đoán theo tên cột — đồng bộ ReferenceCheckService.

using Dapper;
using ICare247.Application.Common.Sql;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Repositories;

/// <inheritdoc cref="IHierarchyGuard"/>
public sealed class HierarchyGuard : IHierarchyGuard
{
    private readonly IDbConnectionFactory     _configDb;
    private readonly IDataDbConnectionFactory _dataDb;
    private readonly ILogger<HierarchyGuard>  _logger;

    /// <summary>Trần độ sâu khi dò tổ tiên — chặn kẹt nếu dữ liệu ĐÃ có vòng từ trước.</summary>
    private const int MaxDepth = 128;

    public HierarchyGuard(
        IDbConnectionFactory configDb,
        IDataDbConnectionFactory dataDb,
        ILogger<HierarchyGuard> logger)
    {
        _configDb = configDb;
        _dataDb   = dataDb;
        _logger   = logger;
    }

    /// <inheritdoc />
    public async Task<HierarchyViolation?> CheckSelfReferenceAsync(
        int tableId, string schemaName, string tableName, string pkColumn,
        object? id, IReadOnlyDictionary<string, object?> values, CancellationToken ct = default)
    {
        // Insert (chưa có khóa) không thể tự tham chiếu / tạo vòng — bỏ qua.
        var selfId = NormalizeId(id);
        if (selfId is null) return null;

        if (!SqlIdentifier.IsSafe(schemaName) || !SqlIdentifier.IsSafe(tableName) || !SqlIdentifier.IsSafe(pkColumn))
            return null;

        using var data = _dataDb.CreateConnection();

        // Cột cha TỰ THAM CHIẾU từ HAI nguồn khai báo tường minh (như ReferenceCheckService):
        //   (1) Sys_Relation Master=Detail (Config DB);  (2) FK vật lý self-ref (Data DB).
        // Union → phủ cả bảng chỉ khai FK vật lý (vd TC_CongTy: FK_TC_CongTy_Cha) mà chưa vào Sys_Relation.
        var parentCols = await GetSelfParentColumnsAsync(data, tableId, schemaName, tableName, ct);
        if (parentCols.Count == 0) return null;   // không phải bảng cây → không guard

        foreach (var col in parentCols)
        {
            if (!SqlIdentifier.IsSafe(col)) continue;
            if (!values.TryGetValue(col, out var raw)) continue;   // form không đụng cột cha → bỏ
            var parentId = NormalizeId(raw);
            if (parentId is null) continue;                        // đặt cha = NULL (gốc) → hợp lệ

            // (1) Trực tiếp: cha = chính nó.
            if (parentId.Value == selfId.Value)
                return new HierarchyViolation(col, HierarchyViolationKind.SelfParent);

            // (2) Gián tiếp: cha đề xuất là hậu duệ của node → tạo vòng.
            if (await WouldCreateCycleAsync(data, schemaName, tableName, pkColumn, col, selfId.Value, parentId.Value, ct))
                return new HierarchyViolation(col, HierarchyViolationKind.Cycle);
        }

        return null;
    }

    /// <summary>
    /// Cột FK cha tự tham chiếu của bảng, gộp 2 nguồn KHAI BÁO tường minh (KHÔNG đoán theo tên):
    /// (1) <c>Sys_Relation</c> Master = Detail (Config DB); (2) FK vật lý self-ref (Data DB
    /// <c>sys.foreign_keys</c>, parent = referenced = bảng này). Mỗi nguồn try/catch riêng — nguồn lỗi
    /// (schema chưa migrate…) không chặn nguồn kia.
    /// </summary>
    private async Task<List<string>> GetSelfParentColumnsAsync(
        System.Data.IDbConnection data, int tableId, string schema, string table, CancellationToken ct)
    {
        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // (1) Sys_Relation Master = Detail = tableId
        const string relSql = """
            SELECT r.Detail_FK_Column
            FROM   dbo.Sys_Relation r
            WHERE  r.Master_Table_Id = @T
              AND  r.Detail_Table_Id = @T
              AND  r.Is_Active = 1
              AND  r.Detail_FK_Column IS NOT NULL
            """;
        try
        {
            using var cfg = _configDb.CreateConnection();
            foreach (var c in await cfg.QueryAsync<string>(
                         new CommandDefinition(relSql, new { T = tableId }, cancellationToken: ct)))
                if (!string.IsNullOrWhiteSpace(c)) cols.Add(c);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "HierarchyGuard: không đọc được Sys_Relation tự tham chiếu (Table_Id={TableId}) — thử FK vật lý.", tableId);
        }

        // (2) FK vật lý self-ref (parent_object_id = referenced_object_id = bảng này)
        const string fkSql = """
            SELECT pc.name
            FROM   sys.foreign_key_columns fkc
            JOIN   sys.tables  pt ON pt.object_id = fkc.parent_object_id
            JOIN   sys.schemas ps ON ps.schema_id = pt.schema_id
            JOIN   sys.columns pc ON pc.object_id = fkc.parent_object_id
                                 AND pc.column_id = fkc.parent_column_id
            WHERE  fkc.parent_object_id = fkc.referenced_object_id
              AND  ps.name = @Schema AND pt.name = @Table
            """;
        try
        {
            foreach (var c in await data.QueryAsync<string>(
                         new CommandDefinition(fkSql, new { Schema = schema, Table = table }, cancellationToken: ct)))
                if (!string.IsNullOrWhiteSpace(c)) cols.Add(c);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "HierarchyGuard: không đọc được FK vật lý self-ref của {Schema}.{Table}.", schema, table);
        }

        return cols.ToList();
    }

    /// <summary>
    /// Đi NGƯỢC chuỗi tổ tiên của <paramref name="proposedParentId"/>; gặp lại <paramref name="selfId"/>
    /// nghĩa là node đang lưu là tổ tiên của cha đề xuất ⇒ đặt cha này sẽ tạo VÒNG. Có visited-set +
    /// <see cref="MaxDepth"/> để không kẹt nếu dữ liệu đã có vòng sẵn (không phải do lần lưu này).
    /// </summary>
    private async Task<bool> WouldCreateCycleAsync(
        System.Data.IDbConnection data, string schema, string table, string pkCol, string parentCol,
        long selfId, long proposedParentId, CancellationToken ct)
    {
        var sql = $"SELECT [{parentCol}] FROM [{schema}].[{table}] WHERE [{pkCol}] = @Id";
        var visited = new HashSet<long>();
        long? current = proposedParentId;
        var depth = 0;

        while (current is not null && depth++ < MaxDepth)
        {
            if (current.Value == selfId) return true;       // node đang lưu là tổ tiên của cha → vòng
            if (!visited.Add(current.Value)) return false;  // vòng có sẵn (không do lần lưu này) → dừng an toàn
            try
            {
                current = await data.ExecuteScalarAsync<long?>(
                    new CommandDefinition(sql, new { Id = current.Value }, cancellationToken: ct));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "HierarchyGuard: dừng dò tổ tiên {Schema}.{Table} tại Id={Id} (truy vấn lỗi).",
                    schema, table, current);
                return false;
            }
        }
        return false;
    }

    /// <summary>Chuẩn hóa khóa về <c>long</c> (khóa cây là BIGINT). null/không phải số → null (coi như cha NULL).</summary>
    private static long? NormalizeId(object? v) => v switch
    {
        null            => null,
        long l          => l,
        int i           => i,
        short s         => s,
        byte b          => b,
        decimal d       => (long)d,
        System.Text.Json.JsonElement je
            when je.ValueKind == System.Text.Json.JsonValueKind.Number && je.TryGetInt64(out var jl) => jl,
        System.Text.Json.JsonElement je2
            when je2.ValueKind == System.Text.Json.JsonValueKind.String && long.TryParse(je2.GetString(), out var jsl) => jsl,
        string str      => long.TryParse(str, out var sl) ? sl : null,
        _               => long.TryParse(v.ToString(), out var pl) ? pl : null,
    };
}
