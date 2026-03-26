// File    : I18nManagerView.xaml.cs
// Module  : I18n
// Layer   : Presentation
// Purpose : Code-behind I18nManagerView — bridge CellValueChanged (DevExpress event) sang SaveCellCommand.
//           Không chứa business logic — mapping FieldName→lang do ViewModel xử lý.

using System.Windows.Controls;
using ConfigStudio.WPF.UI.Modules.I18n.Models;
using ConfigStudio.WPF.UI.Modules.I18n.ViewModels;
using DevExpress.Xpf.Grid;

namespace ConfigStudio.WPF.UI.Modules.I18n.Views;

public partial class I18nManagerView : UserControl
{
    public I18nManagerView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// DevExpress CellValueChanged không bind được command → bridge qua code-behind.
    /// Chỉ forward raw data (ResourceKey, FieldName, Value) — không xử lý logic.
    /// </summary>
    private void OnCellValueChanged(object sender, CellValueChangedEventArgs e)
    {
        if (DataContext is not I18nManagerViewModel vm) return;
        if (e.Row is not I18nEntryDto entry) return;

        vm.SaveCellCommand.Execute(new CellSaveArgs(entry.ResourceKey, e.Column.FieldName, e.Value?.ToString()));
    }
}
