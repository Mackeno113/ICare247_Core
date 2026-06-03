// File    : FieldNavGroup.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Model nhóm section cho Left Panel Field Navigator trong FieldConfigView.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
/// Param cho ReorderFieldCommand — tránh tuple value-type (Prism DelegateCommand yêu cầu reference type).
/// </summary>
public sealed class FieldReorderArgs
{
    public FieldNavItem    Dragged     { get; init; } = null!;
    public FieldNavGroup   TargetGroup { get; init; } = null!;
    public int             InsertIndex { get; init; }
}

/// <summary>
/// Item field trong Left Panel Field Navigator.
/// </summary>
public sealed class FieldNavItem : INotifyPropertyChanged
{
    public int FieldId { get; set; }
    public int SortOrder { get; set; }
    public string ColumnCode { get; set; } = "";
    public string? FieldCode { get; set; }
    public string EditorType { get; set; } = "";
    public bool IsVirtual { get; set; }

    private bool _isCurrentField;
    /// <summary>True khi đây là field đang được chỉnh sửa hiện tại.</summary>
    public bool IsCurrentField
    {
        get => _isCurrentField;
        set { if (_isCurrentField != value) { _isCurrentField = value; OnPropertyChanged(); } }
    }

    /// <summary>Mã hiệu lực: FieldCode cho virtual field, ColumnCode cho field thường.</summary>
    public string EffectiveCode => !string.IsNullOrEmpty(FieldCode) ? FieldCode : ColumnCode;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
