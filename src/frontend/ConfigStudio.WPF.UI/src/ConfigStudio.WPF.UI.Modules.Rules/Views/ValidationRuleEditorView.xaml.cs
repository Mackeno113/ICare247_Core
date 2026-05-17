// File    : ValidationRuleEditorView.xaml.cs
// Module  : Rules
// Layer   : Presentation
// Purpose : Code-behind toi gian cho Validation Rule Editor view.

using System.Windows.Controls;
using ConfigStudio.WPF.UI.Modules.Rules.ViewModels;
using DevExpress.Xpf.Grid;

namespace ConfigStudio.WPF.UI.Modules.Rules.Views;

public partial class ValidationRuleEditorView : UserControl
{
    public ValidationRuleEditorView()
    {
        InitializeComponent();
    }

    // Double-click row trong rules grid -> mo edit panel (EditRuleCommand).
    private void OnRuleRowDoubleClick(object sender, RowDoubleClickEventArgs e)
    {
        if (DataContext is not ValidationRuleEditorViewModel vm) return;
        if (e.HitInfo is null || e.HitInfo.RowHandle < 0) return;
        if (vm.EditRuleCommand.CanExecute()) vm.EditRuleCommand.Execute();
    }
}
