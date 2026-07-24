// File    : MaRuleSegmentRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : DTO một ĐOẠN ghép mã — map từ Sys_Ma_Rule_Segment (db/089, MA-1/ADR-036).
//           Các đoạn xếp theo OrderNo ghép nên mã hoàn chỉnh (vd CT01-PHONG-007).

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>Một đoạn của mã: LITERAL / DATE / FIELD / LOOKUP / SEQ.</summary>
public sealed class MaRuleSegmentRecord
{
    public int SegmentId { get; init; }
    public int RuleId { get; init; }
    public int OrderNo { get; init; }

    /// <summary>LITERAL | DATE | FIELD | LOOKUP | SEQ.</summary>
    public string SegmentType { get; init; } = "LITERAL";

    /// <summary>LITERAL: chữ cố định · DATE: định dạng (yyyy/yy/MM/dd/yyyyMM).</summary>
    public string? TextValue { get; init; }

    /// <summary>FIELD/LOOKUP: mã cột nguồn trong payload.</summary>
    public string? FieldCode { get; init; }

    /// <summary>LOOKUP: bảng tra + cột khóa + cột lấy giá trị.</summary>
    public string? LookupTable { get; init; }
    public string? LookupKeyCol { get; init; }
    public string? LookupValCol { get; init; }

    // Chuẩn hóa giá trị đoạn
    public int? SubstringStart { get; init; }
    public int? Length { get; init; }
    public string? PadChar { get; init; }
    public string? PadSide { get; init; }        // 'L' | 'R'
    public string? TextTransform { get; init; }  // NONE | UPPER | LOWER
}
