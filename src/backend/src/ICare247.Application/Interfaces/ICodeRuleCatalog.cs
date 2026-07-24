// File    : ICodeRuleCatalog.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Tra "bảng này có quy tắc sinh mã không" QUA CACHE (spec 32 / ADR-036, MA-3).
//           Quy tắc nằm ở CONFIG DB (Sys_Ma_Rule + Sys_Ma_Rule_Segment) — đọc mỗi lần lưu là
//           2 round-trip thừa, nên cache-aside L1/L2 gắn version-stamp tenant như IHookStoreCatalog.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Catalog (có cache) cho biết một bảng đích có quy tắc sinh mã hay không.
/// Save path đọc cache → KHÔNG truy vấn Config DB khi lưu (chỉ cold-miss mới query 1 lần).
/// </summary>
public interface ICodeRuleCatalog
{
    /// <summary>
    /// Quy tắc sinh mã của bảng <paramref name="tableCode"/>, hoặc <c>null</c> nếu bảng không có
    /// quy tắc / quy tắc tắt / tenant chưa chạy migration db/089 (thiếu bảng → coi như không có,
    /// KHÔNG ném — hành vi cũ giữ nguyên, không vỡ màn).
    /// </summary>
    Task<MaCodeRule?> GetAsync(string tableCode, int tenantId, CancellationToken ct = default);
}

/// <summary>Quy tắc sinh mã đã gộp đoạn con — dạng cache được (immutable).</summary>
public sealed class MaCodeRule
{
    public string TableCode { get; init; } = "";

    /// <summary>Cột đích (thường <c>Ma</c>).</summary>
    public string ColumnCode { get; init; } = "";

    /// <summary>Bước nhảy số thứ tự.</summary>
    public int Step { get; init; } = 1;

    /// <summary>Cho phép người dùng gõ đè mã (engine vẫn tôn trọng giá trị đã có).</summary>
    public bool AllowManual { get; init; }

    /// <summary>Các đoạn theo thứ tự ghép.</summary>
    public IReadOnlyList<MaCodeSegment> Segments { get; init; } = [];

    /// <summary>
    /// Field mà mã phụ thuộc (đoạn FIELD/LOOKUP) — client dùng để biết khi nào phải xin lại
    /// mã dự kiến (spec §10.3). Rỗng = mã không phụ thuộc field nào.
    /// </summary>
    public IReadOnlyList<string> SourceFields { get; init; } = [];
}

/// <summary>Một đoạn của mã.</summary>
public sealed class MaCodeSegment
{
    public int OrderNo { get; init; }

    /// <summary>LITERAL | DATE | FIELD | LOOKUP | SEQ.</summary>
    public string SegmentType { get; init; } = "LITERAL";

    /// <summary>LITERAL: chữ cố định · DATE: định dạng (yyyy/yy/MM/dd/…).</summary>
    public string? TextValue { get; init; }

    public string? FieldCode { get; init; }
    public string? LookupTable { get; init; }
    public string? LookupKeyCol { get; init; }
    public string? LookupValCol { get; init; }

    public int? SubstringStart { get; init; }
    public int? Length { get; init; }
    public string? PadChar { get; init; }
    public string? PadSide { get; init; }
    public string? TextTransform { get; init; }
}
