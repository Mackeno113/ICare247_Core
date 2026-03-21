// File    : AutoGenerateColumnItem.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Observable item đại diện cho một cột trong dialog Auto-generate Fields.
//           Cho phép user check/uncheck từng cột muốn tạo field.

using ConfigStudio.WPF.UI.Core.Data;
using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Wrapper observable cho ColumnSchemaDto trong AutoGenerateFieldsDialog.
/// Mỗi item tương ứng một cột trong Target DB.
/// </summary>
public sealed class AutoGenerateColumnItem : BindableBase
{
    /// <summary>Dữ liệu cột gốc từ INFORMATION_SCHEMA.</summary>
    public ColumnSchemaDto Column { get; }

    private bool _isSelected;
    /// <summary>
    /// Có được chọn để tạo field không.
    /// Mặc định true cho cột bình thường, false cho cột ShouldSkip (PK/Identity).
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// True nếu cột này bị ẩn khỏi danh sách (PK / Identity — không nên tạo field).
    /// </summary>
    public bool IsHidden => Column.ShouldSkip;

    /// <summary>
    /// Badge màu hiển thị tương ứng với EditorType:
    /// TextBox → xanh dương, NumericBox → tím, CheckBox → xanh lá, DatePicker → cam, TextArea → xám.
    /// </summary>
    public string EditorTypeBadgeColor => Column.DefaultEditorType switch
    {
        "TextBox"    => "#3B82F6",
        "NumericBox" => "#8B5CF6",
        "CheckBox"   => "#10B981",
        "DatePicker" => "#F59E0B",
        "TextArea"   => "#64748B",
        _            => "#94A3B8"
    };

    public AutoGenerateColumnItem(ColumnSchemaDto column)
    {
        Column      = column;
        // NOTE: Mặc định chọn tất cả trừ PK/Identity
        _isSelected = !column.ShouldSkip;
    }
}
