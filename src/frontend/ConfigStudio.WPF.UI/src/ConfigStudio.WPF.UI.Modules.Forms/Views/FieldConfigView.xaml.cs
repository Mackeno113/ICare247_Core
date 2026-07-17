// File    : FieldConfigView.xaml.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Code-behind cho FieldConfigView — forward bulk-select checkbox + rebuild move-targets.

using System.Windows.Controls;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

namespace ConfigStudio.WPF.UI.Modules.Forms.Views;

public partial class FieldConfigView : UserControl
{
    public FieldConfigView()
    {
        InitializeComponent();
    }

    // CheckBox click trên field trong navigator → forward sang ToggleBulkSelectionCommand.
    // (giống FormEditorView.OnFieldBulkChecked; VM con Navigator — REFACTOR-B2)
    private void OnFieldBulkChecked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is CheckBox { DataContext: FieldNavItem item }
            && DataContext is FieldConfigViewModel vm
            && vm.Navigator.ToggleBulkSelectionCommand.CanExecute(item))
        {
            vm.Navigator.ToggleBulkSelectionCommand.Execute(item);
        }
    }

    // Trước khi mở context-menu navigator → dựng lại danh sách section đích theo trạng thái hiện tại.
    private void OnNavigatorContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (DataContext is FieldConfigViewModel vm)
            vm.Navigator.RefreshMoveTargets();
    }
}
