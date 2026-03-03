// File    : ControlPropDefinition.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Định nghĩa 1 prop trong Control_Props_Json schema, dùng để render dynamic form.

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Định nghĩa một thuộc tính của control (từ <c>Ui_Control_Map.Default_Props_Json</c>).
/// Dùng để render dynamic form trong tab "Control Props" của FieldConfig.
/// </summary>
public sealed class ControlPropDefinition
{
    public string PropName { get; set; } = "";

    /// <summary>
    /// Loại dữ liệu: String | Number | Boolean | Enum.
    /// </summary>
    public string PropType { get; set; } = "";

    public string Label { get; set; } = "";
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Danh sách giá trị cho phép khi <see cref="PropType"/> = "Enum".
    /// </summary>
    public List<string>? AllowedValues { get; set; }

    public string? Description { get; set; }
}
