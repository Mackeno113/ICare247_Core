// File    : FormEditorView.xaml.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Code-behind cho Form Editor — bind TreeView.SelectedItem vào ViewModel.

using System.Windows.Controls;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

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
}
