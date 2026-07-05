// File    : MoveTargetItem.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Item đích trong context-menu "Chuyển field đã chọn sang…" của FormEditor.

using System.Windows.Input;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Một đích chuyển field trong context-menu của TreeView cấu trúc form.
/// Đích luôn là 1 <b>Section</b> (field gắn qua <c>Ui_Field.Section_Id</c>); khi form dùng Tab
/// thì <see cref="Header"/> thêm tiền tố tên tab để user "chuyển sang Tab khác" bằng cách chọn
/// section thuộc tab đó (field không gắn trực tiếp Tab — Tab_Id nằm trên Ui_Section).
/// Mang sẵn <see cref="MoveCommand"/> để MenuItem bind thẳng, tránh lỗi RelativeSource qua Popup submenu.
/// </summary>
public sealed class MoveTargetItem
{
    /// <summary>Nhãn hiển thị trên MenuItem — "{Tab} ▸ {Section}" khi có tab, ngược lại chỉ tên section.</summary>
    public string Header { get; }

    /// <summary>Section đích (node NodeType = Section trong cây).</summary>
    public FormTreeNode Section { get; }

    /// <summary>Lệnh chuyển bulk — cùng 1 instance của VM; CommandParameter = chính item này.</summary>
    public ICommand MoveCommand { get; }

    public MoveTargetItem(string header, FormTreeNode section, ICommand moveCommand)
    {
        Header      = header;
        Section     = section;
        MoveCommand = moveCommand;
    }
}
