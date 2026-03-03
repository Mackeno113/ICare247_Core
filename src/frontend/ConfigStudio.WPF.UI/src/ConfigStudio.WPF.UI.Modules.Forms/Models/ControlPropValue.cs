// File    : ControlPropValue.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Giá trị thực tế của 1 prop trong Control_Props_Json, bind vào dynamic form.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Giá trị thực tế của một <see cref="ControlPropDefinition"/>.
/// Mỗi instance tương ứng 1 dòng trong dynamic form tab "Control Props".
/// </summary>
public sealed class ControlPropValue : BindableBase
{
    public ControlPropDefinition Definition { get; set; } = null!;

    private object? _value;

    /// <summary>
    /// Giá trị hiện tại của prop. Thay đổi sẽ trigger rebuild <c>ControlPropsJson</c>.
    /// </summary>
    public object? Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}
