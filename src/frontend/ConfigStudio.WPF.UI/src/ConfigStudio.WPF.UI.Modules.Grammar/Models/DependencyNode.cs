// File    : DependencyNode.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : Node trong dependency graph — đại diện cho Field, Rule, hoặc Event.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Grammar.Models;

/// <summary>
/// Một node trong dependency graph.
/// Có thể là Field, Rule, hoặc Event — hiển thị trên Canvas với vị trí X/Y.
/// </summary>
public sealed class DependencyNode : BindableBase
{
    /// <summary>
    /// ID duy nhất — vd: "Field_42", "Rule_18", "Event_5".
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// Loại node: "Field" | "Rule" | "Event".
    /// </summary>
    public string NodeType { get; set; } = "";

    /// <summary>
    /// Nhãn chính — Column_Code / Rule type code / Trigger code.
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Nhãn phụ — Net_Type / Error_Key / Field_Code.
    /// </summary>
    public string SubLabel { get; set; } = "";

    private double _x;
    public double X { get => _x; set => SetProperty(ref _x, value); }

    private double _y;
    public double Y { get => _y; set => SetProperty(ref _y, value); }

    /// <summary>
    /// True nếu node nằm trong circular dependency.
    /// </summary>
    public bool HasWarning { get; set; }

    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

    /// <summary>
    /// Màu node dựa trên <see cref="NodeType"/>.
    /// Field=Indigo, Rule=Teal, Event=Amber.
    /// </summary>
    public string NodeColor => NodeType switch
    {
        "Field" => "#3F51B5",
        "Rule" => "#009688",
        "Event" => "#FF8F00",
        _ => "#757575"
    };

    /// <summary>
    /// Chiều rộng node trên Canvas.
    /// </summary>
    public double Width => 140;

    /// <summary>
    /// Chiều cao node trên Canvas.
    /// </summary>
    public double Height => 60;
}
