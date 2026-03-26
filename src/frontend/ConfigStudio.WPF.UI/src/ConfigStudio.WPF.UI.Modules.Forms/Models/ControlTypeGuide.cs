// File    : ControlTypeGuide.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Dữ liệu hướng dẫn hiển thị inline khi user chọn EditorType.

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>Một dòng trong bảng key properties của guide.</summary>
public record PropHintRow(string Name, string Hint);

/// <summary>
/// Nội dung hướng dẫn cho từng EditorType.
/// Hiển thị inline ngay bên dưới ComboBox chọn loại control.
/// </summary>
public record ControlTypeGuide(
    string Icon,
    string Title,
    string WhenToUse,
    string ColumnType,
    IReadOnlyList<PropHintRow> Props
)
{
    /// <summary>True khi có ít nhất một property hint để hiển thị bảng.</summary>
    public bool HasProps => Props.Count > 0;
};
