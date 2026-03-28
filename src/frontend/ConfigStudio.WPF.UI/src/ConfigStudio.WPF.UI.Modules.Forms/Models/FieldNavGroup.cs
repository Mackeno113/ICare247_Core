// File    : FieldNavGroup.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Model nhóm section cho Left Panel Field Navigator trong FieldConfigView.

using System.Collections.ObjectModel;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Nhóm section trong Left Panel Field Navigator.
/// Mỗi group tương ứng 1 section, chứa danh sách field thuộc section đó.
/// </summary>
public sealed class FieldNavGroup
{
    public string SectionCode { get; set; } = "";
    public ObservableCollection<FieldNavItem> Fields { get; } = [];
}

/// <summary>
/// Item field trong Left Panel Field Navigator.
/// </summary>
public sealed class FieldNavItem
{
    public int FieldId { get; set; }
    public string ColumnCode { get; set; } = "";
    public string EditorType { get; set; } = "";

    /// <summary>True khi đây là field đang được chỉnh sửa hiện tại.</summary>
    public bool IsCurrentField { get; set; }
}
