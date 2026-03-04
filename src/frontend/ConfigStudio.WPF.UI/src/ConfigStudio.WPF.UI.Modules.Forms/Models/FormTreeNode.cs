// File    : FormTreeNode.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Node dùng chung cho TreeView hiển thị cấu trúc Form > Section > Field.

using System.Collections.ObjectModel;
using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Loại node trong cây cấu trúc form.
/// </summary>
public enum FormNodeType
{
    Section,
    Field
}

/// <summary>
/// Node đại diện cho 1 Section hoặc 1 Field trong TreeView.
/// <c>Children</c> chỉ có ý nghĩa khi <see cref="NodeType"/> = Section.
/// </summary>
public class FormTreeNode : BindableBase
{
    private int _id;
    /// <summary>Section_Id hoặc Field_Id.</summary>
    public int Id { get => _id; set => SetProperty(ref _id, value); }

    private FormNodeType _nodeType;
    public FormNodeType NodeType { get => _nodeType; set => SetProperty(ref _nodeType, value); }

    private string _code = "";
    /// <summary>Section_Code hoặc Field_Code.</summary>
    public string Code { get => _code; set => SetProperty(ref _code, value); }

    private string _displayName = "";
    /// <summary>Tên hiển thị (resolve từ i18n hoặc fallback về Code).</summary>
    public string DisplayName { get => _displayName; set => SetProperty(ref _displayName, value); }

    private string _fieldType = "";
    /// <summary>Loại field (text, number, date, ...). Chỉ có ý nghĩa khi NodeType = Field.</summary>
    public string FieldType { get => _fieldType; set => SetProperty(ref _fieldType, value); }

    private string _editorType = "";
    /// <summary>Control type (TextBox, NumericBox, ...). Chỉ có ý nghĩa khi NodeType = Field.</summary>
    public string EditorType { get => _editorType; set => SetProperty(ref _editorType, value); }

    private bool _isRequired;
    public bool IsRequired { get => _isRequired; set => SetProperty(ref _isRequired, value); }

    private bool _isActive = true;
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

    private int _sortOrder;
    public int SortOrder { get => _sortOrder; set => SetProperty(ref _sortOrder, value); }

    private bool _isExpanded = true;
    /// <summary>Trạng thái expand/collapse trong TreeView (chỉ Section).</summary>
    public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }

    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

    /// <summary>Danh sách field con (chỉ dùng khi NodeType = Section).</summary>
    public ObservableCollection<FormTreeNode> Children { get; } = [];

    // ── Computed properties ──────────────────────────────────

    /// <summary>Icon PackIcon Kind tương ứng với loại node.</summary>
    public string IconKind => NodeType switch
    {
        FormNodeType.Section => "FolderOutline",
        FormNodeType.Field => EditorType switch
        {
            "TextBox" => "FormTextbox",
            "NumericBox" => "Numeric",
            "ComboBox" => "FormDropdown",
            "DatePicker" => "CalendarMonth",
            "LookupBox" => "Magnify",
            "TextArea" => "TextLong",
            "CheckBox" => "CheckboxMarkedOutline",
            "ToggleSwitch" => "ToggleSwitch",
            _ => "FormTextbox"
        },
        _ => "HelpCircleOutline"
    };

    /// <summary>Text hiển thị kèm type info cho field.</summary>
    public string SubText => NodeType == FormNodeType.Field
        ? $"{FieldType} · {EditorType}"
        : $"{Children.Count} fields";
}
