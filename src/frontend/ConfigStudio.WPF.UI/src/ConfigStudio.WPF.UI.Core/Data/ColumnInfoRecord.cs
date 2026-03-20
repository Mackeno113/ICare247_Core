// File    : ColumnInfoRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO cột DB từ Sys_Column cho FieldConfig chọn bind column.

namespace ConfigStudio.WPF.UI.Core.Data;

public sealed class ColumnInfoRecord
{
    public int ColumnId { get; init; }
    public string ColumnCode { get; init; } = "";
    public string DataType { get; init; } = "";
    public string NetType { get; init; } = "";
    public int? MaxLength { get; init; }
    public bool IsNullable { get; init; }
    public bool IsPk { get; init; }
}
