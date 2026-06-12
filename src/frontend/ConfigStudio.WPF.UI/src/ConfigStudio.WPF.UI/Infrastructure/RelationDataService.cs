// File    : RelationDataService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Truy vấn / ghi bảng Sys_Relation (registry quan hệ) qua Dapper trên Config DB.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// Implementation <see cref="IRelationDataService"/> dùng Dapper trên Config DB.
/// Mọi query parameterized; kiểm tra schema mở rộng (migration 035) trước khi đọc/ghi.
/// </summary>
public sealed class RelationDataService : IRelationDataService
{
    private readonly IAppConfigService _config;

    /// <summary>Khởi tạo với cấu hình DB hiện hành.</summary>
    /// <param name="config">Dịch vụ cung cấp ConnectionString + Tenant_Id.</param>
    public RelationDataService(IAppConfigService config) => _config = config;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RelationRecord>> GetRelationsAsync(
        int tenantId, bool includeInactive = false, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        await using var conn = new SqlConnection(_config.ConnectionString);
        await EnsureSchemaAsync(conn, ct);

        var whereActive = includeInactive ? "" : "WHERE r.Is_Active = 1\n";

        var sql =
            "SELECT r.Relation_Id AS RelationId, r.Relation_Code AS RelationCode,\n" +
            "       r.Master_Table_Id AS MasterTableId, ISNULL(mt.Table_Code, '') AS MasterTableCode,\n" +
            "       r.Master_Key_Column AS MasterKeyColumn,\n" +
            "       r.Detail_Table_Id AS DetailTableId, ISNULL(dt.Table_Code, '') AS DetailTableCode,\n" +
            "       r.Detail_FK_Column AS DetailFkColumn, r.Relation_Type AS RelationType,\n" +
            "       r.On_Delete AS OnDelete, r.Display_Column AS DisplayColumn, r.Value_Column AS ValueColumn,\n" +
            "       r.Is_Active AS IsActive\n" +
            "FROM   dbo.Sys_Relation r\n" +
            "LEFT JOIN dbo.Sys_Table mt ON mt.Table_Id = r.Master_Table_Id\n" +
            "LEFT JOIN dbo.Sys_Table dt ON dt.Table_Id = r.Detail_Table_Id\n" +
            whereActive +
            "ORDER BY mt.Table_Code, dt.Table_Code";

        var result = await conn.QueryAsync<RelationRecord>(
            new CommandDefinition(sql, cancellationToken: ct));
        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TableLookupRecord>> GetTablesAsync(
        int tenantId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured) return [];

        await using var conn = new SqlConnection(_config.ConnectionString);

        const string sql =
            "SELECT Table_Id AS TableId, Table_Code AS TableCode, ISNULL(Table_Name, '') AS TableName,\n" +
            "       ISNULL(Schema_Name, 'dbo') AS SchemaName\n" +
            "FROM   dbo.Sys_Table\n" +
            "WHERE  Is_Active = 1 AND (Tenant_Id = @TenantId OR Tenant_Id IS NULL)\n" +
            "ORDER BY Table_Code";

        var result = await conn.QueryAsync<TableLookupRecord>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: ct));
        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetColumnsAsync(int tableId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured || tableId <= 0) return [];

        await using var conn = new SqlConnection(_config.ConnectionString);

        const string sql =
            "SELECT Column_Code FROM dbo.Sys_Column\n" +
            "WHERE  Table_Id = @TableId AND Is_Active = 1\n" +
            "ORDER BY Column_Code";

        var result = await conn.QueryAsync<string>(
            new CommandDefinition(sql, new { TableId = tableId }, cancellationToken: ct));
        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<int> SaveRelationAsync(RelationRecord r, CancellationToken ct = default)
    {
        if (!_config.IsConfigured)
            throw new InvalidOperationException(
                "DB chưa được cấu hình. Kiểm tra %APPDATA%\\ICare247\\ConfigStudio\\appsettings.json");
        if (r.MasterTableId <= 0)
            throw new InvalidOperationException("Phải chọn bảng cha (Master).");
        if (r.DetailTableId <= 0)
            throw new InvalidOperationException("Phải chọn bảng con (Detail).");
        if (string.IsNullOrWhiteSpace(r.DetailFkColumn))
            throw new InvalidOperationException("Phải chọn cột FK ở bảng con (Detail_FK_Column).");

        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync(ct);
        await EnsureSchemaAsync(conn, ct);

        // ── Chống trùng Relation_Code (chỉ khi có khai mã) ──────────
        var code = NullIfEmpty(r.RelationCode);
        if (code is not null)
        {
            const string dupSql =
                "SELECT TOP (1) 1 FROM dbo.Sys_Relation\n" +
                "WHERE Relation_Code = @Code AND Relation_Id <> @RelationId";
            var dup = await conn.ExecuteScalarAsync<int?>(new CommandDefinition(
                dupSql, new { Code = code, RelationId = r.RelationId }, cancellationToken: ct));
            if (dup.HasValue)
                throw new InvalidOperationException($"Relation_Code '{code}' đã tồn tại.");
        }

        var p = new
        {
            RelationCode = code,
            r.MasterTableId,
            MasterKeyColumn = string.IsNullOrWhiteSpace(r.MasterKeyColumn) ? "Id" : r.MasterKeyColumn.Trim(),
            r.DetailTableId,
            DetailFkColumn = r.DetailFkColumn!.Trim(),
            RelationType = string.IsNullOrWhiteSpace(r.RelationType) ? "OneToMany" : r.RelationType,
            OnDelete = string.IsNullOrWhiteSpace(r.OnDelete) ? "Restrict" : r.OnDelete,
            DisplayColumn = NullIfEmpty(r.DisplayColumn),
            ValueColumn = NullIfEmpty(r.ValueColumn),
            r.IsActive,
            r.RelationId,
        };

        if (r.RelationId <= 0)
        {
            const string insertSql =
                "INSERT INTO dbo.Sys_Relation (Relation_Code, Master_Table_Id, Master_Key_Column,\n" +
                "    Detail_Table_Id, Detail_FK_Column, Relation_Type, On_Delete, Display_Column, Value_Column, Is_Active)\n" +
                "VALUES (@RelationCode, @MasterTableId, @MasterKeyColumn, @DetailTableId, @DetailFkColumn,\n" +
                "    @RelationType, @OnDelete, @DisplayColumn, @ValueColumn, @IsActive);\n" +
                "SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(insertSql, p, cancellationToken: ct));
        }

        const string updateSql =
            "UPDATE dbo.Sys_Relation SET Relation_Code = @RelationCode, Master_Table_Id = @MasterTableId,\n" +
            "    Master_Key_Column = @MasterKeyColumn, Detail_Table_Id = @DetailTableId,\n" +
            "    Detail_FK_Column = @DetailFkColumn, Relation_Type = @RelationType, On_Delete = @OnDelete,\n" +
            "    Display_Column = @DisplayColumn, Value_Column = @ValueColumn, Is_Active = @IsActive\n" +
            "WHERE Relation_Id = @RelationId";
        var affected = await conn.ExecuteAsync(new CommandDefinition(updateSql, p, cancellationToken: ct));
        if (affected == 0)
            throw new InvalidOperationException($"Không tìm thấy Relation_Id={r.RelationId} để cập nhật.");
        return r.RelationId;
    }

    /// <inheritdoc />
    public async Task DeactivateRelationAsync(int relationId, CancellationToken ct = default)
    {
        if (!_config.IsConfigured)
            throw new InvalidOperationException("DB chưa được cấu hình.");

        await using var conn = new SqlConnection(_config.ConnectionString);

        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.Sys_Relation SET Is_Active = 0 WHERE Relation_Id = @RelationId",
            new { RelationId = relationId }, cancellationToken: ct));
        if (affected == 0)
            throw new InvalidOperationException($"Không tìm thấy Relation_Id={relationId} để ẩn.");
    }

    // ── Helpers ────────────────────────────────────────────────

    /// <summary>
    /// Kiểm tra Sys_Relation đã mở rộng (có cột Detail_FK_Column) chưa — bảo vệ khi
    /// migration 035 chưa chạy trên DB tenant.
    /// </summary>
    /// <param name="conn">Kết nối SQL (Dapper tự mở nếu chưa mở).</param>
    /// <param name="ct">Token hủy.</param>
    private static async Task EnsureSchemaAsync(SqlConnection conn, CancellationToken ct)
    {
        const string sql =
            "SELECT COUNT(*) FROM sys.columns\n" +
            "WHERE object_id = OBJECT_ID('dbo.Sys_Relation') AND name = 'Detail_FK_Column'";
        var exists = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: ct));
        if (exists == 0)
            throw new InvalidOperationException(
                "Sys_Relation chưa có cột Detail_FK_Column. Cần chạy migration 035_extend_sys_relation.sql trước.");
    }

    /// <summary>Chuẩn hóa chuỗi rỗng/space về null để cột nullable lưu NULL thay vì ''.</summary>
    /// <param name="value">Chuỗi đầu vào.</param>
    /// <returns>null nếu rỗng/space, ngược lại chuỗi đã trim.</returns>
    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
