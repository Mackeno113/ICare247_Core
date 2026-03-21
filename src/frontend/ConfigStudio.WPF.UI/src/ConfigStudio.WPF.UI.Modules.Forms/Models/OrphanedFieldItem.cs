// File    : OrphanedFieldItem.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Observable item đại diện cho field mồ côi (cột không còn trong Target DB).
//           Dùng trong tab "Cảnh báo" của SyncSchemaDialog.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Wrapper observable cho field đang tham chiếu cột đã biến mất khỏi Target DB.
/// User có thể check để đánh dấu xóa field này.
/// </summary>
public sealed class OrphanedFieldItem : BindableBase
{
    /// <summary>Field node trong TreeView.</summary>
    public FormTreeNode Field { get; }

    /// <summary>Tên section chứa field này.</summary>
    public string SectionName { get; init; } = "";

    private bool _isMarkedForRemoval;
    /// <summary>True nếu user muốn xóa field này khỏi form.</summary>
    public bool IsMarkedForRemoval
    {
        get => _isMarkedForRemoval;
        set => SetProperty(ref _isMarkedForRemoval, value);
    }

    public OrphanedFieldItem(FormTreeNode field)
    {
        Field = field;
        // NOTE: Mặc định KHÔNG đánh dấu xóa — để user chủ động quyết định
        _isMarkedForRemoval = false;
    }
}
