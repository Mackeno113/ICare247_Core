// File    : FormManagerView.xaml.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Code-behind toi gian cho FormManager view.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using ConfigStudio.WPF.UI.Modules.Forms.ViewModels;
using DevExpress.Xpf.Grid;

namespace ConfigStudio.WPF.UI.Modules.Forms.Views;

public partial class FormManagerView : UserControl
{
    public FormManagerView()
    {
        InitializeComponent();
        // Ctrl+F focus search box — UI concern, xu ly bang code-behind cho gon.
        InputBindings.Add(new KeyBinding(
            new RoutedFocusCommand(() => SearchBox?.Focus()),
            new KeyGesture(Key.F, ModifierKeys.Control)));
    }

    // Double-click row -> mo man hinh Cau hinh Field (OpenFieldConfigCommand). Forward-only, khong co business logic.
    private void OnRowDoubleClick(object sender, RowDoubleClickEventArgs e)
    {
        if (DataContext is not FormManagerViewModel vm) return;
        if (sender is not TableView tv || tv.DataControl is not GridControl grid) return;
        if (e.HitInfo is null || e.HitInfo.RowHandle < 0) return;

        if (grid.GetRow(e.HitInfo.RowHandle) is FormSummaryDto form
            && vm.OpenFieldConfigCommand.CanExecute(form))
        {
            vm.OpenFieldConfigCommand.Execute(form);
        }
    }

    // ICommand inline cho focus actions — tranh phai bind tu VM cho action thuan UI.
    private sealed class RoutedFocusCommand(System.Action action) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => action();
#pragma warning disable CS0067
        public event System.EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
    }
}
