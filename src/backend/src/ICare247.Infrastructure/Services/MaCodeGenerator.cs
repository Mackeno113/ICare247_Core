// File    : MaCodeGenerator.cs
// Module  : MasterData
// Layer   : Infrastructure
// Purpose : Dựng tiền tố/hậu tố mã từ các ĐOẠN của quy tắc rồi gọi proc cấp mã (spec 32 §5, MA-3).
//           Dùng CHUNG cho mọi đường ghi (MasterDataRepository, DynamicLookupRepository) — tránh
//           mỗi repo tự ghép mã một kiểu rồi lệch nhau.
//
// Phân vai (spec §5.1): C# lo phần cần payload + ngữ cảnh (LITERAL/DATE/FIELD/LOOKUP), proc lo phần
//   cần dữ liệu bảng đích (dò MAX + khóa phạm vi + ghép số). Proc KHÔNG đọc Sys_Ma_Rule vì bảng đó ở
//   CONFIG DB — truy vấn 3 phần tên sẽ đóng cứng tên database vào proc (vỡ 1-DB-1-tenant, ADR-018/025).

using System.Data;
using System.Text;
using Dapper;
using ICare247.Application.Common.Sql;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Services;

/// <summary>Sinh mã theo quy tắc — cấp thật (trong transaction) hoặc xem trước (chỉ đọc).</summary>
public sealed partial class MaCodeGenerator
{
    private readonly ILogger<MaCodeGenerator> _logger;

    public MaCodeGenerator(ILogger<MaCodeGenerator> logger) => _logger = logger;

    /// <summary>
    /// CẤP THẬT một mã mới. PHẢI gọi bên trong transaction đang ghi (proc từ chối khi
    /// <c>@@TRANCOUNT = 0</c> — không có transaction thì khóa phạm vi nhả sớm ⇒ cấp trùng).
    /// Sự kiện theo sau: caller gán mã vào payload rồi INSERT trong CÙNG transaction.
    /// </summary>
    public async Task<string?> GenerateAsync(
        MaCodeRule rule, IDbConnection data, IDbTransaction? tx,
        IReadOnlyDictionary<string, object?> values, string schemaName,
        CancellationToken ct = default)
    {
        var (prefix, suffix, seqWidth) = await BuildAffixesAsync(rule, data, tx, values, ct);
        if (prefix is null) return null;      // đoạn phụ thuộc field mà field chưa có giá trị → không sinh

        var dp = new DynamicParameters();
        dp.Add("Bang", rule.TableCode);
        dp.Add("Cot", rule.ColumnCode);
        dp.Add("TienTo", prefix);
        dp.Add("HauTo", suffix);
        dp.Add("DoRongSeq", seqWidth);
        dp.Add("Buoc", rule.Step);
        dp.Add("SchemaName", string.IsNullOrWhiteSpace(schemaName) ? "dbo" : schemaName);
        dp.Add("MaMoi", dbType: DbType.String, direction: ParameterDirection.Output, size: 100);

        await data.ExecuteAsync(new CommandDefinition(
            "dbo.sp_SinhMa", dp, transaction: tx,
            commandType: CommandType.StoredProcedure, cancellationToken: ct));

        var code = dp.Get<string?>("MaMoi");
        _logger.LogInformation("SinhMa {Table}.{Column} → '{Code}' (tiền tố '{Prefix}').",
            rule.TableCode, rule.ColumnCode, code, prefix);
        return code;
    }

    /// <summary>
    /// XEM TRƯỚC mã dự kiến — KHÔNG khóa, KHÔNG giữ chỗ, KHÔNG ghi gì (spec §2).
    /// Hai người mở form cùng lúc sẽ thấy CÙNG một mã: đây là mã *dự kiến*, không phải cam kết.
    /// </summary>
    public async Task<string?> PreviewAsync(
        MaCodeRule rule, IDbConnection data,
        IReadOnlyDictionary<string, object?> values, string schemaName,
        CancellationToken ct = default)
    {
        var (prefix, suffix, seqWidth) = await BuildAffixesAsync(rule, data, tx: null, values, ct);
        if (prefix is null) return null;      // chưa đủ field nguồn → để ô trống, KHÔNG hiện mã sai

        var dp = new DynamicParameters();
        dp.Add("Bang", rule.TableCode);
        dp.Add("Cot", rule.ColumnCode);
        dp.Add("TienTo", prefix);
        dp.Add("HauTo", suffix);
        dp.Add("DoRongSeq", seqWidth);
        dp.Add("Buoc", rule.Step);
        dp.Add("SchemaName", string.IsNullOrWhiteSpace(schemaName) ? "dbo" : schemaName);
        dp.Add("MaDuKien", dbType: DbType.String, direction: ParameterDirection.Output, size: 100);

        await data.ExecuteAsync(new CommandDefinition(
            "dbo.sp_XemTruocMa", dp, transaction: null,
            commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return dp.Get<string?>("MaDuKien");
    }

    // ── Dựng tiền tố / hậu tố ────────────────────────────────────────────────

    /// <summary>
    /// Ghép các đoạn thành (tiền tố, hậu tố, độ rộng SEQ). Đoạn trước SEQ → tiền tố; sau SEQ → hậu tố.
    /// Trả tiền tố <c>null</c> = KHÔNG sinh được (thiếu giá trị field nguồn / LOOKUP không ra kết quả).
    /// </summary>
    private async Task<(string? Prefix, string Suffix, int SeqWidth)> BuildAffixesAsync(
        MaCodeRule rule, IDbConnection data, IDbTransaction? tx,
        IReadOnlyDictionary<string, object?> values, CancellationToken ct)
    {
        var prefix = new StringBuilder();
        var suffix = new StringBuilder();
        var seqWidth = 0;
        var seqSeen = false;

        // Giờ ĐỊA PHƯƠNG, KHÔNG UTC (spec §5.0): VN = UTC+7 ⇒ dùng UTC sẽ cấp mã mang năm CŨ
        // suốt 7 tiếng đầu mỗi năm, và dãy số reset-theo-năm rơi nhầm khóa.
        var now = DateTime.Now;

        foreach (var seg in rule.Segments)
        {
            if (seg.SegmentType == "SEQ")
            {
                seqSeen = true;
                seqWidth = seg.Length is > 0 ? seg.Length.Value : 0;
                continue;
            }

            var raw = seg.SegmentType switch
            {
                "LITERAL" => seg.TextValue ?? "",
                "DATE" => FormatDate(seg.TextValue, now),
                "FIELD" => ReadField(values, seg.FieldCode),
                "LOOKUP" => await ResolveLookupAsync(seg, data, tx, values, ct),
                _ => null,
            };

            if (raw is null)
            {
                _logger.LogDebug(
                    "SinhMa {Table}: đoạn {Order} ({Type}) chưa có giá trị → không sinh mã lần này.",
                    rule.TableCode, seg.OrderNo, seg.SegmentType);
                return (null, "", 0);
            }

            var text = ApplyFormat(seg, raw);
            (seqSeen ? suffix : prefix).Append(text);
        }

        // Không có đoạn SEQ = cấu hình sai (DB đã chặn ≥2, WPF chặn 0) → không sinh, tránh mọi bản ghi cùng mã.
        if (!seqSeen)
        {
            _logger.LogWarning("SinhMa {Table}: quy tắc không có đoạn SEQ — bỏ qua sinh mã.", rule.TableCode);
            return (null, "", 0);
        }

        return (prefix.ToString(), suffix.ToString(), seqWidth);
    }

    /// <summary>Định dạng ngày theo token cấu hình (yyyy/yy/MM/dd và ghép, vd 'yyyyMM').</summary>
    private static string FormatDate(string? format, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(format)) return "";
        // ToString(format) hiểu sẵn yyyy/yy/MM/dd — dùng InvariantCulture để không dính lịch phi-Gregorian.
        try { return now.ToString(format, System.Globalization.CultureInfo.InvariantCulture); }
        catch (FormatException) { return ""; }
    }

    /// <summary>Giá trị thô của 1 field trong payload; null = chưa có (không sinh mã).</summary>
    private static string? ReadField(IReadOnlyDictionary<string, object?> values, string? fieldCode)
    {
        if (string.IsNullOrWhiteSpace(fieldCode)) return null;
        if (!values.TryGetValue(fieldCode, out var v) || v is null or DBNull) return null;
        var s = v.ToString();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    /// <summary>
    /// Đoạn LOOKUP: lấy giá trị field nguồn rồi tra sang bảng khác —
    /// <c>SELECT [Val] FROM [Table] WHERE [Key] = @v</c>. Chạy trên CÙNG connection/transaction đang
    /// ghi nên thấy được cả dữ liệu vừa ghi trong transaction đó.
    /// Identifier validate + bọc ngoặc; giá trị luôn qua tham số.
    /// </summary>
    private async Task<string?> ResolveLookupAsync(
        MaCodeSegment seg, IDbConnection data, IDbTransaction? tx,
        IReadOnlyDictionary<string, object?> values, CancellationToken ct)
    {
        var key = ReadField(values, seg.FieldCode);
        if (key is null) return null;

        if (!IsSafe(seg.LookupTable) || !IsSafe(seg.LookupKeyCol) || !IsSafe(seg.LookupValCol))
        {
            _logger.LogWarning(
                "SinhMa: đoạn LOOKUP có identifier không hợp lệ ({Table}/{Key}/{Val}) — bỏ qua sinh mã.",
                seg.LookupTable, seg.LookupKeyCol, seg.LookupValCol);
            return null;
        }

        var sql = $"SELECT TOP (1) [{seg.LookupValCol}] FROM [{seg.LookupTable}] WHERE [{seg.LookupKeyCol}] = @v";
        var result = await data.ExecuteScalarAsync<object?>(new CommandDefinition(
            sql, new { v = values[seg.FieldCode!] }, transaction: tx, cancellationToken: ct));

        if (result is null or DBNull) return null;
        var s = result.ToString();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    /// <summary>Áp cắt / độ rộng cố định / đệm / biến đổi chữ cho giá trị thô của đoạn.</summary>
    private static string ApplyFormat(MaCodeSegment seg, string value)
    {
        var v = value;

        if (seg.SubstringStart is > 1 && v.Length >= seg.SubstringStart.Value)
            v = v[(seg.SubstringStart.Value - 1)..];

        if (seg.TextTransform == "UPPER") v = v.ToUpperInvariant();
        else if (seg.TextTransform == "LOWER") v = v.ToLowerInvariant();

        if (seg.Length is > 0)
        {
            var width = seg.Length.Value;
            var pad = string.IsNullOrEmpty(seg.PadChar) ? '0' : seg.PadChar[0];
            if (v.Length < width)
                v = seg.PadSide == "R" ? v.PadRight(width, pad) : v.PadLeft(width, pad);
            else if (v.Length > width)
                // Đoạn phi-SEQ khai độ rộng cố định thì phải đúng độ rộng đó — nếu không, vị trí chữ số
                // đầu tiên xê dịch giữa các bản ghi và proc cắt sai phần số khi dò MAX (spec §3.3).
                v = v[..width];
        }
        return v;
    }

    private static bool IsSafe(string? identifier)
        => !string.IsNullOrWhiteSpace(identifier) && SqlIdentifier.IsSafe(identifier);
}
