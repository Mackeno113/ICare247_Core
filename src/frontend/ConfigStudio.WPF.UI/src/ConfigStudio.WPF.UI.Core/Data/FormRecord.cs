// File    : FormRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO nhận kết quả Dapper query từ bảng Ui_Form.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// POCO map kết quả Dapper từ <c>dbo.Ui_Form</c>.
/// Alias SQL phải khớp tên property: Form_Id AS FormId, v.v.
/// </summary>
public sealed class FormRecord
{
    public int      FormId      { get; init; }
    public string   FormCode    { get; init; } = "";
    public string   FormName    { get; init; } = "";
    public int      Version     { get; init; }
    public string   Platform    { get; init; } = "web";
    public string   TableName   { get; init; } = "";
    public bool     IsActive    { get; init; }
    public DateTime UpdatedAt   { get; init; }
    public string   UpdatedBy   { get; init; } = "";
    public int      SectionCount{ get; init; }
    public int      FieldCount  { get; init; }
    public int      EventCount  { get; init; }
}
