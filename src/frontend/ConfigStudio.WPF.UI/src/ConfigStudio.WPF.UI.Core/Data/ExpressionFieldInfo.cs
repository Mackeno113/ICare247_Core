// File    : ExpressionFieldInfo.cs
// Module  : Data
// Layer   : Core
// Purpose : Thông tin field dùng trong Expression Builder palette — truyền qua DialogParameters.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Thông tin field trong form context.
/// Dùng để hiển thị danh sách field trong Expression Builder palette và validate Identifier node.
/// </summary>
public sealed class ExpressionFieldInfo
{
    public string Code { get; set; } = "";
    public string NetType { get; set; } = "";

    public string DisplayName => $"{Code} ({NetType})";

    public ExpressionFieldInfo() { }

    public ExpressionFieldInfo(string code, string netType)
    {
        Code = code;
        NetType = netType;
    }
}
