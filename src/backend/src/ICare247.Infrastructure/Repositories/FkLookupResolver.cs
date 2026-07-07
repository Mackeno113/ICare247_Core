// File    : FkLookupResolver.cs
// Module  : Import
// Layer   : Infrastructure
// Purpose : Dapper implementation của IFkLookupResolver — cầu FK dùng chung import + template (ADR-034).
//           Đọc định nghĩa FK từ Ui_Field_Lookup (Config DB) rồi tra Mã↔Id trên Data DB, lọc token ngữ cảnh.

using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.View;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Resolve định nghĩa khóa ngoại của cột View (tường minh <c>Props_Json.fkLookup.fieldId</c> → ngầm
/// <c>Edit_Form_Id + Column_Id</c>) và dựng bảng tra <c>Mã→Id</c> lọc theo phân quyền. Spec 25 §11–§14.
/// An toàn injection: identifier whitelist qua <see cref="SafeIdentifierRegex"/> + bọc <c>[]</c>; giá trị qua Dapper params;
/// token ngữ cảnh trong <c>Filter_Sql</c> bind server-side qua <see cref="IContextParamResolver"/>.
/// </summary>
public sealed partial class FkLookupResolver : IFkLookupResolver
{
    private readonly IDbConnectionFactory _db;               // Config DB — metadata
    private readonly IDataDbConnectionFactory _dataDb;       // Data DB tenant — bảng nguồn FK
    private readonly IContextParamResolver _contextResolver; // token ngữ cảnh cho Filter_Sql
    private readonly ILogger<FkLookupResolver> _logger;

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex SafeIdentifierRegex();

    /// <summary>Trích tên tham số <c>@name</c> tham chiếu trong Filter_Sql (whitelist token cần bind).</summary>
    [GeneratedRegex(@"@([a-zA-Z_][a-zA-Z0-9_]*)", RegexOptions.Compiled)]
    private static partial Regex ParamRefRegex();

    /// <summary>Cú pháp Order_By an toàn: danh sách "Cột [ASC|DESC]" phân tách bởi dấu phẩy.</summary>
    [GeneratedRegex(@"^\s*[a-zA-Z_][a-zA-Z0-9_]*(\s+(asc|desc))?\s*(,\s*[a-zA-Z_][a-zA-Z0-9_]*(\s+(asc|desc))?\s*)*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex SafeOrderByRegex();

    public FkLookupResolver(
        IDbConnectionFactory db, IDataDbConnectionFactory dataDb,
        IContextParamResolver contextResolver, ILogger<FkLookupResolver> logger)
    {
        _db = db;
        _dataDb = dataDb;
        _contextResolver = contextResolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FkLookupDefinition>> GetFkColumnsAsync(
        ViewMetadata view, CancellationToken ct = default)
    {
        using var cfg = _db.CreateConnection();

        // ── (1) Tường minh: cột khai Props_Json.fkLookup.fieldId ─────────────────
        const string colSql = """
            SELECT Field_Name AS FieldName, Props_Json AS PropsJson
            FROM   dbo.Ui_View_Column
            WHERE  View_Id = @ViewId AND Is_Active = 1
              AND  Props_Json IS NOT NULL AND Props_Json LIKE '%fkLookup%'
            """;
        var colRows = (await cfg.QueryAsync<ColPropsRow>(
            new CommandDefinition(colSql, new { view.ViewId }, cancellationToken: ct))).AsList();

        // FieldName → Field_Id (tường minh thắng khi trùng).
        var fieldToFieldId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in colRows)
        {
            if (string.IsNullOrWhiteSpace(r.FieldName) || !SafeIdentifierRegex().IsMatch(r.FieldName)) continue;
            if (ParseFkLookupFieldId(r.PropsJson) is int id && id > 0)
                fieldToFieldId[r.FieldName] = id;
        }

        // ── (2) Ngầm: cột Data map Sys_Column + Edit_Form có field lookup cùng Column_Id ──
        if (view.EditFormId is int formId)
        {
            // Ứng viên = cột Data có Column_Id, CHƯA resolve tường minh.
            var candidateCols = view.Columns
                .Where(c => string.Equals(c.ColumnKind, "Data", StringComparison.OrdinalIgnoreCase)
                            && c.ColumnId is int
                            && !string.IsNullOrWhiteSpace(c.FieldName)
                            && SafeIdentifierRegex().IsMatch(c.FieldName)
                            && !fieldToFieldId.ContainsKey(c.FieldName))
                .ToList();
            var colIdToField = candidateCols
                .GroupBy(c => c.ColumnId!.Value)
                .ToDictionary(g => g.Key, g => g.First().FieldName, EqualityComparer<int>.Default);

            if (colIdToField.Count > 0)
            {
                const string implicitSql = """
                    SELECT fi.Column_Id AS ColumnId, fi.Field_Id AS FieldId
                    FROM   dbo.Ui_Field fi
                    JOIN   dbo.Ui_Field_Lookup fl ON fl.Field_Id = fi.Field_Id
                    WHERE  fi.Form_Id = @FormId AND fi.Column_Id IN @ColIds
                    """;
                var implicitRows = await cfg.QueryAsync<ImplicitFieldRow>(
                    new CommandDefinition(implicitSql,
                        new { FormId = formId, ColIds = colIdToField.Keys.ToArray() }, cancellationToken: ct));
                foreach (var r in implicitRows)
                {
                    if (colIdToField.TryGetValue(r.ColumnId, out var fieldName)
                        && !fieldToFieldId.ContainsKey(fieldName))
                        fieldToFieldId[fieldName] = r.FieldId;
                }
            }
        }

        if (fieldToFieldId.Count == 0)
            return [];

        // ── (3) Nạp định nghĩa từ Ui_Field_Lookup (kèm Code_Field/Filter_Sql/Order_By) ──
        const string defSql = """
            SELECT fl.Field_Id       AS FieldId,
                   fl.Query_Mode     AS QueryMode,
                   fl.Source_Name    AS SourceName,
                   fl.Value_Column   AS ValueColumn,
                   fl.Display_Column AS DisplayColumn,
                   fl.Code_Field     AS CodeField,
                   fl.Filter_Sql     AS FilterSql,
                   fl.Order_By       AS OrderBy
            FROM   dbo.Ui_Field_Lookup fl
            WHERE  fl.Field_Id IN @Ids
            """;
        var byId = (await cfg.QueryAsync<FkDefRow>(new CommandDefinition(
                defSql, new { Ids = fieldToFieldId.Values.Distinct().ToArray() }, cancellationToken: ct)))
            .ToDictionary(x => x.FieldId);

        var result = new List<FkLookupDefinition>();
        foreach (var (fieldName, fieldId) in fieldToFieldId)
        {
            if (!byId.TryGetValue(fieldId, out var d)) continue;
            // v1 chỉ nhận nguồn bảng/view + cột Value/Display đơn (tvf/custom_sql/expression → escape-hatch, bỏ qua).
            if (!string.Equals(d.QueryMode ?? "table", "table", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrWhiteSpace(d.SourceName)
                || string.IsNullOrWhiteSpace(d.ValueColumn) || string.IsNullOrWhiteSpace(d.DisplayColumn)) continue;
            if (!SafeIdentifierRegex().IsMatch(d.ValueColumn) || !SafeIdentifierRegex().IsMatch(d.DisplayColumn)) continue;

            result.Add(new FkLookupDefinition(
                FieldName: fieldName,
                FieldId: fieldId,
                SourceName: d.SourceName,
                ValueColumn: d.ValueColumn,
                DisplayColumn: d.DisplayColumn,
                CodeField: d.CodeField,
                FilterSql: d.FilterSql,
                OrderBy: d.OrderBy));
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<FkCodeMap> BuildCodeMapAsync(FkLookupDefinition def, CancellationToken ct = default)
    {
        // Không có cầu Mã↔Id → không import/template được cột này (caller surface lỗi cấu hình).
        if (string.IsNullOrWhiteSpace(def.CodeField) || !SafeIdentifierRegex().IsMatch(def.CodeField))
            return new FkCodeMap([], hasCodeField: false);

        var source = ParseQualifiedName(def.SourceName);
        var codeCol = Bracket(def.CodeField);
        var valueCol = Bracket(def.ValueColumn);
        var displayCol = Bracket(def.DisplayColumn);

        // ── WHERE Filter_Sql + bind token ngữ cảnh (server-side, lọc đúng phạm vi quyền) ──
        var dp = new DynamicParameters();
        var whereSql = "";
        if (!string.IsNullOrWhiteSpace(def.FilterSql))
        {
            whereSql = " WHERE " + def.FilterSql;
            await BindTokensAsync(dp, def.FilterSql, ct);
        }

        // ── ORDER BY (chỉ khi cú pháp an toàn) ──
        var orderSql = "";
        if (!string.IsNullOrWhiteSpace(def.OrderBy) && SafeOrderByRegex().IsMatch(def.OrderBy))
            orderSql = " ORDER BY " + def.OrderBy;

        var sql =
            $"SELECT {codeCol} AS Code, {valueCol} AS Id, {displayCol} AS Display " +
            $"FROM {source}{whereSql}{orderSql}";

        using var data = _dataDb.CreateConnection();
        var rows = await data.QueryAsync(new CommandDefinition(sql, dp, cancellationToken: ct));

        var items = new List<FkLookupItem>();
        foreach (var row in rows)
        {
            var d = (IDictionary<string, object?>)row;
            var code = d.TryGetValue("Code", out var c) ? c?.ToString() : null;
            if (string.IsNullOrWhiteSpace(code)) continue;   // Mã rỗng → bỏ (không dùng làm cầu)
            var id = d.TryGetValue("Id", out var i) ? i : null;
            var display = d.TryGetValue("Display", out var s) ? s?.ToString() : null;
            items.Add(new FkLookupItem(code.Trim(), id, display));
        }

        return new FkCodeMap(items, hasCodeField: true);
    }

    /// <summary>Resolve + bind token <c>@name</c> tham chiếu trong Filter_Sql vào <paramref name="dp"/>.</summary>
    private async Task BindTokensAsync(DynamicParameters dp, string filterSql, CancellationToken ct)
    {
        var names = ParamRefRegex().Matches(filterSql)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (names.Count == 0)
            return;

        var values = await _contextResolver.ResolveAsync(names, ct);
        foreach (var (name, value) in values)
            dp.Add(name, value);
    }

    /// <summary>Bóc <c>fkLookup.fieldId</c> từ Props_Json của cột (null nếu không có/không hợp lệ). Không phát sự kiện.</summary>
    private static int? ParseFkLookupFieldId(string? propsJson)
    {
        if (string.IsNullOrWhiteSpace(propsJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(propsJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("fkLookup", out var fk)
                && fk.ValueKind == JsonValueKind.Object
                && fk.TryGetProperty("fieldId", out var fid)
                && fid.TryGetInt32(out var id))
                return id;
        }
        catch (JsonException) { /* Props_Json sai cú pháp → bỏ qua */ }
        return null;
    }

    /// <summary>Tách 'schema.object' hoặc 'object' → '[schema].[object]' (mặc định dbo); mỗi phần whitelist.</summary>
    private static string ParseQualifiedName(string name)
    {
        var parts = name.Trim().Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2 || parts.Any(p => !SafeIdentifierRegex().IsMatch(p)))
            throw new InvalidOperationException($"FK lookup: tên nguồn '{name}' không hợp lệ.");
        return parts.Length == 2
            ? $"{Bracket(parts[0])}.{Bracket(parts[1])}"
            : $"[dbo].{Bracket(parts[0])}";
    }

    /// <summary>Bọc identifier trong <c>[]</c>, escape <c>]</c> → <c>]]</c>.</summary>
    private static string Bracket(string ident) => "[" + ident.Replace("]", "]]") + "]";

    /// <summary>Dapper row: Column_Id → Field_Id khi dò FK ngầm qua Edit_Form.</summary>
    private sealed class ImplicitFieldRow
    {
        public int ColumnId { get; init; }
        public int FieldId { get; init; }
    }

    /// <summary>Dapper row: Field_Name + Props_Json của cột View (bóc fkLookup).</summary>
    private sealed class ColPropsRow
    {
        public string FieldName { get; init; } = string.Empty;
        public string? PropsJson { get; init; }
    }

    /// <summary>Dapper row: định nghĩa FK đầy đủ đọc từ Ui_Field_Lookup.</summary>
    private sealed class FkDefRow
    {
        public int FieldId { get; init; }
        public string? QueryMode { get; init; }
        public string? SourceName { get; init; }
        public string? ValueColumn { get; init; }
        public string? DisplayColumn { get; init; }
        public string? CodeField { get; init; }
        public string? FilterSql { get; init; }
        public string? OrderBy { get; init; }
    }
}
