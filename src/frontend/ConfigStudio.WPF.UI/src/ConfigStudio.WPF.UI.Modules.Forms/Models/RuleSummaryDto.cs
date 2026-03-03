// File    : RuleSummaryDto.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : DTO tóm tắt thông tin rule gắn vào field, hiển thị trong tab Validation Rules.

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// DTO hiển thị tóm tắt rule trong DataGrid tab "Validation Rules" của FieldConfig.
/// </summary>
public sealed class RuleSummaryDto
{
    public int RuleId { get; set; }
    public int OrderNo { get; set; }
    public string RuleTypeCode { get; set; } = "";
    public string ExpressionPreview { get; set; } = "";
    public string ErrorKey { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
