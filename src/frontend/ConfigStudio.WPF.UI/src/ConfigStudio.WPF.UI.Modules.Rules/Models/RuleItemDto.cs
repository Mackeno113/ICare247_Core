// File    : RuleItemDto.cs
// Module  : Rules
// Layer   : Presentation
// Purpose : DTO hiển thị 1 validation rule trong DataGrid.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Rules.Models;

/// <summary>
/// DTO đại diện cho 1 validation rule gắn với field.
/// </summary>
public class RuleItemDto : BindableBase
{
    public int RuleId { get; set; }
    public int FieldId { get; set; }
    public string FieldCode { get; set; } = "";

    private string _ruleTypeCode = "";
    public string RuleTypeCode { get => _ruleTypeCode; set => SetProperty(ref _ruleTypeCode, value); }

    private int _orderNo;
    public int OrderNo { get => _orderNo; set => SetProperty(ref _orderNo, value); }

    private string _expressionJson = "";
    /// <summary>Expression JSON gốc (từ DB).</summary>
    public string ExpressionJson { get => _expressionJson; set => SetProperty(ref _expressionJson, value); }

    private string _expressionPreview = "";
    /// <summary>Mô tả ngắn gọn expression (human-readable).</summary>
    public string ExpressionPreview { get => _expressionPreview; set => SetProperty(ref _expressionPreview, value); }

    private string _errorKey = "";
    /// <summary>i18n key cho thông báo lỗi.</summary>
    public string ErrorKey { get => _errorKey; set => SetProperty(ref _errorKey, value); }

    private string _severity = "Error";
    public string Severity { get => _severity; set => SetProperty(ref _severity, value); }

    private bool _isActive = true;
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

    /// <summary>Icon theo severity.</summary>
    public string SeverityIcon => Severity switch
    {
        "Error" => "AlertCircle",
        "Warning" => "AlertOutline",
        "Info" => "InformationOutline",
        _ => "AlertCircle"
    };
}
