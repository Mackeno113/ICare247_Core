// File    : FunctionRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO grammar function từ Gram_Function.

namespace ConfigStudio.WPF.UI.Core.Data;

public sealed class FunctionRecord
{
    public int FunctionId { get; init; }
    public string FunctionCode { get; init; } = "";
    public string? Description { get; init; }
    public string ReturnNetType { get; init; } = "";
    public int ParamCountMin { get; init; }
    public int ParamCountMax { get; init; }
    public bool IsSystem { get; init; } = true;
    public bool IsActive { get; init; } = true;
}

public sealed class OperatorRecord
{
    public string OperatorSymbol { get; init; } = "";
    public string OperatorType { get; init; } = "";
    public int Precedence { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}
