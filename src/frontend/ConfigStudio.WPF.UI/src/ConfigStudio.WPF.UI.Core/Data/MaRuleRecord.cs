// File    : MaRuleRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : DTO quy tắc sinh mã — map từ bảng Sys_Ma_Rule (db/089, MA-1/ADR-036).
//           Mỗi dòng = 1 cặp (bảng, cột) cần sinh mã; các ĐOẠN ghép mã ở MaRuleSegmentRecord.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>Quy tắc sinh mã tự động cho một cột (thường <c>Ma</c>) của một bảng dữ liệu.</summary>
public sealed class MaRuleRecord
{
    public int RuleId { get; init; }

    /// <summary>Bảng đích — khớp <c>Sys_Table.Table_Code</c>.</summary>
    public string TableCode { get; init; } = "";

    /// <summary>Cột đích — thường <c>Ma</c>.</summary>
    public string ColumnCode { get; init; } = "";

    /// <summary>Bước nhảy số thứ tự (thường 1).</summary>
    public int Step { get; init; } = 1;

    /// <summary>1 = cho phép người dùng gõ đè mã trên form.</summary>
    public bool AllowManual { get; init; }

    public bool IsActive { get; init; } = true;
    public string? Description { get; init; }

    // 4 cờ ConfigSync (db/050)
    public bool IsSystem { get; init; }
    public bool IsCustomized { get; init; }
    public DateTime? SyncedAt { get; init; }
    public int? SourceVer { get; init; }
}
