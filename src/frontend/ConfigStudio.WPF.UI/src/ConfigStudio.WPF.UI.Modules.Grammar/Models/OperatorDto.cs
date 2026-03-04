// File    : OperatorDto.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : DTO hiển thị 1 operator trong Grammar Library.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Grammar.Models;

/// <summary>
/// DTO cho 1 operator trong whitelist (<c>Gram_Operator</c>).
/// </summary>
public class OperatorDto : BindableBase
{
    public int OperatorId { get; set; }

    private string _symbol = "";
    public string Symbol { get => _symbol; set => SetProperty(ref _symbol, value); }

    private string _operatorName = "";
    /// <summary>Tên đầy đủ: Addition, Subtraction, Equal, ...</summary>
    public string OperatorName { get => _operatorName; set => SetProperty(ref _operatorName, value); }

    private string _category = "";
    /// <summary>Nhóm: Arithmetic, Comparison, Logical.</summary>
    public string Category { get => _category; set => SetProperty(ref _category, value); }

    private int _precedence;
    public int Precedence { get => _precedence; set => SetProperty(ref _precedence, value); }

    private string _description = "";
    public string Description { get => _description; set => SetProperty(ref _description, value); }

    private bool _isActive = true;
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
}
