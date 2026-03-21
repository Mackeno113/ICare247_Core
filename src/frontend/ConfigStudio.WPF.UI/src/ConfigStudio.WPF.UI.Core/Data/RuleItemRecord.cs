// File    : RuleItemRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO validation rule từ Val_Rule (sau Migration 003: Field_Id gộp trực tiếp vào Val_Rule).

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Dữ liệu 1 validation rule đọc từ / ghi vào dbo.Val_Rule.
/// </summary>
public sealed class RuleItemRecord
{
    public int     RuleId         { get; init; }
    public int     FieldId        { get; init; }
    public string  RuleTypeCode   { get; init; } = "";
    public int     OrderNo        { get; init; }
    public string? ExpressionJson { get; init; }
    public string  ErrorKey       { get; init; } = "";
    public string  Severity       { get; init; } = "Error";
    public bool    IsActive       { get; init; } = true;
}

public sealed class RuleSummaryRecord
{
    public int     RuleId          { get; init; }
    public int     OrderNo         { get; init; }
    public string  RuleTypeCode    { get; init; } = "";
    public string? ExpressionPreview { get; init; }
    public string  ErrorKey        { get; init; } = "";
    public bool    IsActive        { get; init; }
}

public sealed class RuleTypeRecord
{
    public string  RuleTypeCode { get; init; } = "";
    public string? ParamSchema  { get; init; }
}
