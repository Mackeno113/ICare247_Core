// File    : CodeRuleCatalog.cs
// Module  : MasterData
// Layer   : Infrastructure
// Purpose : Impl ICodeRuleCatalog — đọc Sys_Ma_Rule + Sys_Ma_Rule_Segment (Config DB) QUA CACHE
//           (L1/L2). Save path đọc cache → 0 query khi lưu (cùng khuôn HookStoreCatalog, ADR-029).

using System.Text.RegularExpressions;
using Dapper;
using ICare247.Application.Constants;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Services;

/// <summary>Cache-aside catalog cho quy tắc sinh mã theo (tenant, bảng).</summary>
public sealed partial class CodeRuleCatalog : ICodeRuleCatalog
{
    private readonly IDbConnectionFactory _configDb;
    private readonly ICacheService _cache;
    private readonly ICacheVersion _version;
    private readonly ILogger<CodeRuleCatalog> _logger;

    // TTL: quy tắc hiếm đổi (DEV cấu hình trên ConfigStudio) → cache lâu; flush cache vô hiệu ngay.
    private static readonly TimeSpan MemTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RedisTtl = TimeSpan.FromMinutes(60);

    /// <summary>Sentinel "bảng này KHÔNG có quy tắc" — cache cả trường hợp rỗng để khỏi query lại mỗi lần lưu.</summary>
    private static readonly MaCodeRule NoRule = new() { TableCode = "", ColumnCode = "" };

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex SafeIdentifierRegex();

    public CodeRuleCatalog(
        IDbConnectionFactory configDb,
        ICacheService cache,
        ICacheVersion version,
        ILogger<CodeRuleCatalog> logger)
    {
        _configDb = configDb;
        _cache = cache;
        _version = version;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MaCodeRule?> GetAsync(string tableCode, int tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tableCode) || !SafeIdentifierRegex().IsMatch(tableCode))
            return null;

        var key = CacheKeys.CodeRule(tableCode, tenantId, _version.Get(tenantId));

        var cached = await _cache.GetAsync<MaCodeRule>(key, ct);
        if (cached is not null)
            return cached.ColumnCode.Length == 0 ? null : cached;   // sentinel → không có quy tắc

        MaCodeRule? rule;
        try
        {
            rule = await LoadAsync(tableCode, ct);
        }
        catch (Exception ex)
        {
            // Tenant chưa chạy db/089 (thiếu bảng) hoặc lỗi đọc config → coi như KHÔNG có quy tắc.
            // Ném ở đây sẽ chặn toàn bộ đường lưu danh mục — hành vi cũ phải giữ nguyên (spec §11).
            _logger.LogWarning(ex,
                "CodeRuleCatalog: không đọc được quy tắc sinh mã cho '{Table}' (tenant {Tenant}) — bỏ qua sinh mã.",
                tableCode, tenantId);
            return null;
        }

        await _cache.SetAsync(key, rule ?? NoRule, MemTtl, RedisTtl, ct);
        return rule;
    }

    /// <summary>Đọc 1 quy tắc + đoạn con từ Config DB (2 câu, 1 connection).</summary>
    private async Task<MaCodeRule?> LoadAsync(string tableCode, CancellationToken ct)
    {
        using var cfg = _configDb.CreateConnection();

        const string ruleSql = """
            SELECT TOP (1)
                   Rule_Id AS RuleId, Table_Code AS TableCode, Column_Code AS ColumnCode,
                   Step AS Step, Allow_Manual AS AllowManual
            FROM   dbo.Sys_Ma_Rule
            WHERE  Table_Code = @TableCode AND Is_Active = 1
            """;
        var head = await cfg.QueryFirstOrDefaultAsync<RuleRow>(
            new CommandDefinition(ruleSql, new { TableCode = tableCode }, cancellationToken: ct));
        if (head is null) return null;

        const string segSql = """
            SELECT Order_No AS OrderNo, Segment_Type AS SegmentType, Text_Value AS TextValue,
                   Field_Code AS FieldCode, Lookup_Table AS LookupTable,
                   Lookup_Key_Col AS LookupKeyCol, Lookup_Val_Col AS LookupValCol,
                   Substring_Start AS SubstringStart, Length AS Length,
                   Pad_Char AS PadChar, Pad_Side AS PadSide, Text_Transform AS TextTransform
            FROM   dbo.Sys_Ma_Rule_Segment
            WHERE  Rule_Id = @RuleId AND Is_Active = 1
            ORDER BY Order_No
            """;
        var segments = (await cfg.QueryAsync<MaCodeSegment>(
            new CommandDefinition(segSql, new { head.RuleId }, cancellationToken: ct))).ToList();

        // Quy tắc không có đoạn nào = cấu hình dở dang → coi như chưa bật (không sinh mã rỗng).
        if (segments.Count == 0)
        {
            _logger.LogWarning(
                "CodeRuleCatalog: quy tắc '{Table}.{Column}' không có đoạn nào — bỏ qua.",
                head.TableCode, head.ColumnCode);
            return null;
        }

        var sourceFields = segments
            .Where(s => s.SegmentType is "FIELD" or "LOOKUP" && !string.IsNullOrWhiteSpace(s.FieldCode))
            .Select(s => s.FieldCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new MaCodeRule
        {
            TableCode = head.TableCode,
            ColumnCode = head.ColumnCode,
            Step = head.Step < 1 ? 1 : head.Step,
            AllowManual = head.AllowManual,
            Segments = segments,
            SourceFields = sourceFields,
        };
    }

    /// <summary>Dapper map cho dòng quy tắc (đầu).</summary>
    private sealed class RuleRow
    {
        public int RuleId { get; init; }
        public string TableCode { get; init; } = "";
        public string ColumnCode { get; init; } = "";
        public int Step { get; init; } = 1;
        public bool AllowManual { get; init; }
    }
}
