// File    : FkLookupResolver.cs
// Module  : Import
// Layer   : Infrastructure
// Purpose : Dapper implementation của IFkLookupResolver — dựng bảng tra Mã↔Id (lọc quyền) cho 1 định nghĩa FK,
//           dùng chung import + template (ADR-034). Định nghĩa FK do IImportMetadataProvider cấp (từ field Edit_Form).

using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Chạy truy vấn nguồn FK trên Data DB → <c>{Mã, Id, Tên}</c> đã lọc <c>Filter_Sql</c> + token ngữ cảnh
/// (đúng phạm vi quyền như form/lưới). An toàn injection: identifier whitelist + bọc <c>[]</c>; giá trị qua Dapper params.
/// </summary>
public sealed partial class FkLookupResolver : IFkLookupResolver
{
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
        IDataDbConnectionFactory dataDb, IContextParamResolver contextResolver, ILogger<FkLookupResolver> logger)
    {
        _dataDb = dataDb;
        _contextResolver = contextResolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FkCodeMap> BuildCodeMapAsync(FkLookupDefinition def, CancellationToken ct = default)
    {
        // Không có cầu Mã↔Id → không import/template được cột này (caller surface lỗi cấu hình).
        if (string.IsNullOrWhiteSpace(def.CodeField) || !SafeIdentifierRegex().IsMatch(def.CodeField))
            return new FkCodeMap([], hasCodeField: false);
        if (string.IsNullOrWhiteSpace(def.SourceName)
            || !SafeIdentifierRegex().IsMatch(def.ValueColumn) || !SafeIdentifierRegex().IsMatch(def.DisplayColumn))
            return new FkCodeMap([], hasCodeField: false);

        var source = ParseQualifiedName(def.SourceName);
        var codeCol = Bracket(def.CodeField);
        var valueCol = Bracket(def.ValueColumn);
        var displayCol = Bracket(def.DisplayColumn);

        // ── WHERE Filter_Sql + bind token ngữ cảnh (server-side, lọc đúng phạm vi quyền) ──
        //    ImportGlobalCode = bỏ Filter_Sql (lọc cha cascade) → tra Mã trên TOÀN bảng (§B). Trùng Mã sẽ
        //    bị FkCodeMap đánh dấu HasAmbiguousCode để engine từ chối.
        var dp = new DynamicParameters();
        var whereSql = "";
        if (!def.ImportGlobalCode && !string.IsNullOrWhiteSpace(def.FilterSql))
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

    /// <summary>
    /// Resolve + bind mọi token <c>@name</c> tham chiếu trong Filter_Sql vào <paramref name="dp"/>.
    /// Token ngữ cảnh (registry) → giá trị thật; token KHÔNG resolve được (vd @param field cha cascade — import
    /// không có ngữ cảnh cascade) → bind <c>NULL</c> để câu SQL không văng "Must declare @param" (điều kiện đó ⇒ 0 dòng).
    /// </summary>
    private async Task BindTokensAsync(DynamicParameters dp, string filterSql, CancellationToken ct)
    {
        var names = ParamRefRegex().Matches(filterSql)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (names.Count == 0)
            return;

        var values = await _contextResolver.ResolveAsync(names, ct);
        var resolved = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
            dp.Add(name, resolved.TryGetValue(name, out var v) ? v : null);   // không resolve → NULL (0 dòng, không crash)
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
}
