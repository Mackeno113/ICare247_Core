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

        // ── Lấy bảng user-defined + VIEW (engine-driven: thiết kế trên view, vd vw_TC_CongTy) ──
        // ORG-CFG-1: gồm cả VIEW để cấu hình màn nghiệp vụ đọc qua SQL View (ADR-024).
        const string sql = """
            SELECT TABLE_SCHEMA + '.' + TABLE_NAME
            FROM   INFORMATION_SCHEMA.TABLES
            WHERE  TABLE_TYPE IN ('BASE TABLE', 'VIEW')
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

    // ── GetProcedureColumnsAsync ──────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<ColumnSchemaDto>> GetProcedureColumnsAsync(
        string connectionString,
        string schemaName,
        string procName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString)
         || string.IsNullOrWhiteSpace(procName))
            return [];

        await using var conn = new SqlConnection(connectionString);

        // ── Phân tích result-set đầu tiên của SP (không thực thi) ──
        // sys.dm_exec_describe_first_result_set(@tsql, @params, @browse) — @browse=0.
        // system_type_name dạng "nvarchar(50)" / "decimal(18,2)" / "int" → parse base + length.
        const string sql = """
            SELECT
                r.column_ordinal   AS OrdinalPosition,
                r.name             AS ColumnName,
                r.system_type_name AS SystemTypeName,
                r.is_nullable      AS IsNullable
            FROM sys.dm_exec_describe_first_result_set(@Tsql, NULL, 0) AS r
            WHERE  r.is_hidden = 0
              AND  r.name IS NOT NULL
              AND  r.name <> ''
            ORDER BY r.column_ordinal
            """;

        var schema = string.IsNullOrWhiteSpace(schemaName) ? "dbo" : schemaName;
        var tsql = $"EXEC [{schema}].[{procName}]";

        var rows = await conn.QueryAsync<RawProcColumnRow>(
            new CommandDefinition(sql, new { Tsql = tsql }, cancellationToken: ct));

        var result = rows.Select(r =>
        {
            var (baseType, maxLength) = ParseSystemTypeName(r.SystemTypeName);
            return new ColumnSchemaDto
            {
                ColumnName        = r.ColumnName,
                DataType          = baseType,
                NetType           = DataTypeMapper.ToNetType(baseType),
                DefaultEditorType = DataTypeMapper.ToEditorType(baseType),
                IsNullable        = r.IsNullable,
                IsIdentity        = false,   // cột dẫn xuất từ SP — không có identity/PK
                IsPrimaryKey      = false,
                OrdinalPosition   = r.OrdinalPosition,
                MaxLength         = maxLength,
            };
        }).ToList();

        return result.AsReadOnly();
    }

    // ── ProcedureExistsAsync ──────────────────────────────────

    /// <inheritdoc />
    public async Task<bool> ProcedureExistsAsync(
        string connectionString,
        string schemaName,
        string procName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString)
         || string.IsNullOrWhiteSpace(procName))
            return false;

        await using var conn = new SqlConnection(connectionString);

        var schema = string.IsNullOrWhiteSpace(schemaName) ? "dbo" : schemaName;
        const string sql = "SELECT CASE WHEN OBJECT_ID(@FullName, 'P') IS NULL THEN 0 ELSE 1 END";

        var exists = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { FullName = $"{schema}.{procName}" }, cancellationToken: ct));

        return exists == 1;
    }

    /// <summary>
    /// Tách "nvarchar(50)" → ("nvarchar", 50); "decimal(18,2)" → ("decimal", null);
    /// "nvarchar(max)" → ("nvarchar", null); "int" → ("int", null).
    /// </summary>
    /// <param name="systemTypeName">Giá trị system_type_name từ dm_exec_describe_first_result_set.</param>
    /// <returns>Cặp (kiểu cơ sở, độ dài tối đa nếu là chuỗi 1 tham số).</returns>
    private static (string BaseType, int? MaxLength) ParseSystemTypeName(string? systemTypeName)
    {
        if (string.IsNullOrWhiteSpace(systemTypeName))
            return ("", null);

        var open = systemTypeName.IndexOf('(');
        if (open < 0) return (systemTypeName.Trim(), null);

        var baseType = systemTypeName[..open].Trim();
        var close = systemTypeName.IndexOf(')', open);
        if (close <= open) return (baseType, null);

        // Chỉ lấy MaxLength khi tham số là 1 số nguyên (char types); bỏ qua "max" và "p,s".
        var inner = systemTypeName[(open + 1)..close].Trim();
        return int.TryParse(inner, out var len) ? (baseType, len) : (baseType, null);
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

    /// <summary>
    /// POCO nội bộ — map từ result của sys.dm_exec_describe_first_result_set.
    /// </summary>
    private sealed class RawProcColumnRow
    {
        public int    OrdinalPosition { get; init; }
        public string ColumnName      { get; init; } = "";
        public string SystemTypeName  { get; init; } = "";
        public bool   IsNullable      { get; init; }
    }
}
