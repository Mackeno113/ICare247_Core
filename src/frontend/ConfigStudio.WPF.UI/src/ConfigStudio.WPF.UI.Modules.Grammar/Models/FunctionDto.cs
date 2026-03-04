// File    : FunctionDto.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : DTO hiển thị 1 function trong Grammar Library.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Grammar.Models;

/// <summary>
/// DTO cho 1 function trong whitelist (<c>Gram_Function</c>).
/// </summary>
public class FunctionDto : BindableBase
{
    public int FunctionId { get; set; }

    private string _functionName = "";
    public string FunctionName { get => _functionName; set => SetProperty(ref _functionName, value); }

    private string _category = "";
    /// <summary>Nhóm: String, Math, Date, Logic, ...</summary>
    public string Category { get => _category; set => SetProperty(ref _category, value); }

    private int _paramCount;
    /// <summary>Số tham số (-1 = variadic).</summary>
    public int ParamCount { get => _paramCount; set => SetProperty(ref _paramCount, value); }

    private string _returnType = "";
    public string ReturnType { get => _returnType; set => SetProperty(ref _returnType, value); }

    private string _description = "";
    public string Description { get => _description; set => SetProperty(ref _description, value); }

    private string _example = "";
    public string Example { get => _example; set => SetProperty(ref _example, value); }

    private bool _isActive = true;
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

    /// <summary>Hiển thị param count dạng text.</summary>
    public string ParamText => ParamCount == -1 ? "variadic" : ParamCount.ToString();
}
