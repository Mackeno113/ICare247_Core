// File    : EventEditorView.xaml.cs
// Module  : Events
// Layer   : Presentation
// Purpose : Code-behind toi gian cho Event Editor view.

using System.Windows.Controls;
using ConfigStudio.WPF.UI.Modules.Events.Models;
using ConfigStudio.WPF.UI.Modules.Events.ViewModels;
using DevExpress.Xpf.Grid;

namespace ConfigStudio.WPF.UI.Modules.Events.Views;

public partial class EventEditorView : UserControl
{
    public EventEditorView()
    {
        InitializeComponent();
    }

    // Double-click event row -> mo Expression Builder cho condition cua event do.
    private void OnEventRowDoubleClick(object sender, RowDoubleClickEventArgs e)
    {
        if (DataContext is not EventEditorViewModel vm) return;
        if (sender is not TableView tv || tv.DataControl is not GridControl grid) return;
        if (e.HitInfo is null || e.HitInfo.RowHandle < 0) return;

        if (grid.GetRow(e.HitInfo.RowHandle) is EventItemDto evt
            && vm.EditConditionCommand.CanExecute())
        {
            vm.SelectedEvent = evt;
            vm.EditConditionCommand.Execute();
        }
    }
}
