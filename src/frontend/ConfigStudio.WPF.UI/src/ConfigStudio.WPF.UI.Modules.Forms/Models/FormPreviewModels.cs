// File    : FormPreviewModels.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Model classes cho FormPreviewDialog — section + field preview.

using System.Collections.ObjectModel;
using System.Windows.Media;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Section trong Form Preview — tiêu đề + danh sách fields.
/// </summary>
public sealed class SectionPreviewModel
{
    public string SectionCode { get; init; } = "";
    public string DisplayTitle { get; init; } = "";
    public int FieldCount => Fields.Count;

    public ObservableCollection<FieldPreviewModel> Fields { get; } = [];
}

/// <summary>
/// Field card trong Form Preview — hiển thị Editor Type, label, badges.
/// </summary>
public sealed class FieldPreviewModel
{
    public int    FieldId    { get; init; }
    public string LabelKey   { get; init; } = "";
    public string ColumnCode { get; init; } = "";
    public string EditorType { get; init; } = "";
    public bool   IsReadOnly { get; init; }
    public bool   IsVisible  { get; init; }
    public int    RuleCount  { get; init; }

    /// <summary>Màu nền badge EditorType.</summary>
    public SolidColorBrush EditorTypeBg => EditorType switch
    {
        "TextBox"                         => new SolidColorBrush(Color.FromRgb(0xEF, 0xF6, 0xFF)),
        "NumericBox"                       => new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4)),
        "DatePicker"                       => new SolidColorBrush(Color.FromRgb(0xF5, 0xF3, 0xFF)),
        "ComboBox" or "LookupComboBox"     => new SolidColorBrush(Color.FromRgb(0xFF, 0xF7, 0xED)),
        "LookupBox"                        => new SolidColorBrush(Color.FromRgb(0xFF, 0xFB, 0xEB)),
        "CheckBox" or "ToggleSwitch"       => new SolidColorBrush(Color.FromRgb(0xEC, 0xFE, 0xFF)),
        "TextArea"                         => new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC)),
        "RadioGroup"                       => new SolidColorBrush(Color.FromRgb(0xFD, 0xF2, 0xF8)),
        _                                  => new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9))
    };

    /// <summary>Màu chữ badge EditorType.</summary>
    public SolidColorBrush EditorTypeFg => EditorType switch
    {
        "TextBox"                         => new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0xD8)),
        "NumericBox"                       => new SolidColorBrush(Color.FromRgb(0x15, 0x80, 0x3D)),
        "DatePicker"                       => new SolidColorBrush(Color.FromRgb(0x6D, 0x28, 0xD9)),
        "ComboBox" or "LookupComboBox"     => new SolidColorBrush(Color.FromRgb(0xC2, 0x41, 0x0C)),
        "LookupBox"                        => new SolidColorBrush(Color.FromRgb(0x92, 0x40, 0x0E)),
        "CheckBox" or "ToggleSwitch"       => new SolidColorBrush(Color.FromRgb(0x0E, 0x74, 0x90)),
        "TextArea"                         => new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
        "RadioGroup"                       => new SolidColorBrush(Color.FromRgb(0xBE, 0x18, 0x5D)),
        _                                  => new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B))
    };
}
