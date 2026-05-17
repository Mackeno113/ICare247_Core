// File    : SysLookupManagerView.xaml.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Code-behind tối giản cho SysLookupManagerView.

using System.Windows.Controls;
using ConfigStudio.WPF.UI.Modules.Forms.ViewModels;
using DevExpress.Xpf.Grid;

namespace ConfigStudio.WPF.UI.Modules.Forms.Views;

public partial class SysLookupManagerView : UserControl
{
    public SysLookupManagerView()
    {
        InitializeComponent();
    }

    // Double-click row trong grid Items -> mo editor inline (EditItemCommand).
    private void OnItemRowDoubleClick(object sender, RowDoubleClickEventArgs e)
    {
        if (DataContext is not SysLookupManagerViewModel vm) return;
        if (e.HitInfo is null || e.HitInfo.RowHandle < 0) return;
        if (vm.EditItemCommand.CanExecute()) vm.EditItemCommand.Execute();
    }
}
