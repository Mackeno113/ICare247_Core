// File    : FormEditorView.xaml.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Code-behind cho Form Editor — bind TreeView.SelectedItem vào ViewModel.

using System.Windows.Controls;
using System.Windows.Input;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using ConfigStudio.WPF.UI.Modules.Forms.ViewModels;
using DevExpress.Xpf.Grid;

namespace ConfigStudio.WPF.UI.Modules.Forms.Views;

public partial class FormEditorView : UserControl
{
    public FormEditorView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// TreeView không hỗ trợ SelectedItem binding 2 chiều.
    /// Dùng event SelectedItemChanged để đồng bộ vào ViewModel.SelectedNode.
    /// </summary>
    private void OnTreeSelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is FormEditorViewModel vm)
        {
            vm.SelectedNode = e.NewValue as FormTreeNode;
        }
    }

    /// <summary>
    /// Double-click trên field trong cây → mở nhanh màn Field Config.
    /// Bỏ qua nếu đang chọn Section (double-click section chỉ expand/collapse).
    /// </summary>
    private void OnTreeMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is FormEditorViewModel vm
            && vm.IsFieldSelected
            && vm.OpenFieldConfigCommand.CanExecute())
        {
            vm.OpenFieldConfigCommand.Execute();
            e.Handled = true;
        }
    }

    // D2 — CheckBox click tren field tree item: forward sang ToggleBulkSelectionCommand.
    private void OnFieldBulkChecked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is CheckBox { DataContext: FormTreeNode node }
            && DataContext is FormEditorViewModel vm
            && vm.ToggleBulkSelectionCommand.CanExecute(node))
        {
            vm.ToggleBulkSelectionCommand.Execute(node);
        }
    }

    // D3 — Grid-edit tab: forward cell value change sang VM de hydrate cache + trigger debounced save.
    private async void OnFieldsGridCellValueChanged(object sender, CellValueChangedEventArgs e)
    {
        if (DataContext is not FormEditorViewModel vm) return;
        if (e.Row is not FormTreeNode node) return;
        await vm.OnGridCellChangedAsync(node);
    }
}
