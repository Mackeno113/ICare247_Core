// File    : MaRuleUpsertRequest.cs
// Module  : Data
// Layer   : Core
// Purpose : Payload ghi một quy tắc sinh mã + toàn bộ đoạn con (ghi trong 1 transaction).

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>Yêu cầu ghi 1 dòng <c>Sys_Ma_Rule</c> kèm danh sách đoạn (xóa-ghi lại theo cha).</summary>
public sealed class MaRuleUpsertRequest
{
    public int? RuleId { get; set; }
    public string TableCode { get; set; } = "";
    public string ColumnCode { get; set; } = "Ma";
    public int Step { get; set; } = 1;
    public bool AllowManual { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    /// <summary>Các đoạn theo đúng thứ tự hiển thị (OrderNo gán lại 1..n khi ghi).</summary>
    public IReadOnlyList<MaRuleSegmentRecord> Segments { get; set; } = [];
}
