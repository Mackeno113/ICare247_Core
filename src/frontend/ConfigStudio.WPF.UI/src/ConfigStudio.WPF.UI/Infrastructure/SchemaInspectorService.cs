// File    : SchemaInspectorService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Đọc INFORMATION_SCHEMA của Target DB để lấy cấu trúc cột.
//           Dùng Dapper + Microsoft.Data.SqlClient, KHÔNG dùng Config DB.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Helpers;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// Truy vấn INFORMATION_SCHEMA.COLUMNS và KEY_COLUMN_USAGE từ Target DB.
/// Kết quả được map sang ColumnSchemaDto với NetType và DefaultEditorType đã tính sẵn.
/// </summary>
public sealed class SchemaInspectorService : ISchemaInspectorService
{
    // ── GetTableNamesAsync ────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetTableNamesAsync(
        string connectionString,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return [];

        await using var conn = new SqlConnection(connectionString);

        // ── Lấy tất cả bảng user-defined, bỏ system tables ──
        const string sql = """
            SELECT TABLE_SCHEMA + '.' + TABLE_NAME
            FROM   INFORMATION_SCHEMA.TABLES
            WHERE  TABLE_TYPE = 'BASE TABLE'
            ORDER  BY TABLE_SCHEMA, TABLE_NAME
            """;

        var rows = await conn.QueryAsync<string>(
            new CommandDefinition(sql, cancellationToken: ct));

        return rows.ToList().AsReadOnly();
    }

    // ── GetColumnsAsync ───────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<ColumnSchemaDto>> GetColumnsAsync(
        string connectionString,
        string schemaName,
        string tableName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString)
         || string.IsNullOrWhiteSpace(tableName))
            return [];

        await using var conn = new SqlConnection(connectionString);

        // ── Query cột + identity flag + primary key flag ─────
        // COLUMNPROPERTY trả 1 nếu là Identity, 0 nếu không.
        // LEFT JOIN KEY_COLUMN_USAGE để detect Primary Key.
        const string sql = """
            SELECT
                c.ORDINAL_POSITION                                        AS OrdinalPosition,
                c.COLUMN_NAME                                             AS ColumnName,
                c.DATA_TYPE                                               AS DataType,
                c.CHARACTER_MAXIMUM_LENGTH                                AS MaxLength,
                c.NUMERIC_PRECISION                                       AS NumericPrecision,
                c.NUMERIC_SCALE                                           AS NumericScale,
                CASE c.IS_NULLABLE WHEN 'YES' THEN 1 ELSE 0 END          AS IsNullable,
                ISNULL(
                    COLUMNPROPERTY(
                        OBJECT_ID(@SchemaName + '.' + @TableName),
                        c.COLUMN_NAME,
                        'IsIdentity'),
                    0)                                                    AS IsIdentity,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END   AS IsPrimaryKey
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.COLUMN_NAME
                FROM   INFORMATION_SCHEMA.TABLE_CONSTRAINTS  tc
                JOIN   INFORMATION_SCHEMA.KEY_COLUMN_USAGE   ku
                       ON  tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                       AND tc.TABLE_SCHEMA    = ku.TABLE_SCHEMA
                       AND tc.TABLE_NAME      = ku.TABLE_NAME
                WHERE  tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                  AND  tc.TABLE_SCHEMA    = @SchemaName
                  AND  tc.TABLE_NAME      = @TableName
            ) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
            WHERE  c.TABLE_SCHEMA = @SchemaName
              AND  c.TABLE_NAME   = @TableName
            ORDER  BY c.ORDINAL_POSITION
            """;

        var param = new { SchemaName = schemaName, TableName = tableName };

        // ── Dapper map vào raw record trước, tính NetType/EditorType sau ──
        var rows = await conn.QueryAsync<RawColumnRow>(
            new CommandDefinition(sql, param, cancellationToken: ct));

        // ── Build ColumnSchemaDto với các trường computed ────
        var result = rows.Select(r => new ColumnSchemaDto
        {
            ColumnName       = r.ColumnName,
            DataType         = r.DataType,
            NetType          = DataTypeMapper.ToNetType(r.DataType),
            DefaultEditorType = DataTypeMapper.ToEditorType(r.DataType),
            IsNullable       = r.IsNullable,
            IsIdentity       = r.IsIdentity,
            IsPrimaryKey     = r.IsPrimaryKey,
            OrdinalPosition  = r.OrdinalPosition,
            MaxLength        = r.MaxLength,
            NumericPrecision = r.NumericPrecision,
            NumericScale     = r.NumericScale,
        }).ToList();

        return result.AsReadOnly();
    }

    // ── Internal raw record — chỉ dùng để Dapper map ─────────

    /// <summary>
    /// POCO nội bộ — map thẳng từ SQL result set trước khi convert sang DTO.
    /// Dapper cần class/record có setter hoặc constructor match.
    /// </summary>
    private sealed class RawColumnRow
    {
        public int     OrdinalPosition  { get; init; }
        public string  ColumnName       { get; init; } = "";
        public string  DataType         { get; init; } = "";
        public int?    MaxLength        { get; init; }
        public int?    NumericPrecision { get; init; }
        public int?    NumericScale     { get; init; }
        public bool    IsNullable       { get; init; }
        public bool    IsIdentity       { get; init; }
        public bool    IsPrimaryKey     { get; init; }
    }
}
