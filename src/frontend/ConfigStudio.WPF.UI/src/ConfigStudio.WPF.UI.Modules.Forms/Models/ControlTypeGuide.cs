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
/// <see cref="Steps"/> = quy trình cấu hình từng bước — hiện dạng ToolTip khi user
/// trỏ chuột vào banner tên control (sự kiện theo sau: WPF mở tooltip quy trình).
/// </summary>
public record ControlTypeGuide(
    string Icon,
    string Title,
    string WhenToUse,
    string ColumnType,
    IReadOnlyList<PropHintRow> Props,
    IReadOnlyList<string>? Steps = null
)
{
    /// <summary>True khi có ít nhất một property hint để hiển thị bảng.</summary>
    public bool HasProps => Props.Count > 0;

    /// <summary>True khi có quy trình từng bước để hiển thị tooltip trên banner.</summary>
    public bool HasSteps => Steps is { Count: > 0 };
};
