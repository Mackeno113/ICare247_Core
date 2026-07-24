// File    : MaRuleManagerView.xaml.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Code-behind tối thiểu cho màn Quy tắc sinh mã (MA-6) — chỉ forward double-click sang
//           OpenEditor của ViewModel, không có business logic.

using System.Windows.Controls;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Modules.Forms.ViewModels;
using DevExpress.Xpf.Grid;

namespace ConfigStudio.WPF.UI.Modules.Forms.Views;

public partial class MaRuleManagerView : UserControl
{
    public MaRuleManagerView() => InitializeComponent();

    // Double-click dòng -> mở popup Sửa. Forward-only, không có business logic.
    private void OnRowDoubleClick(object sender, RowDoubleClickEventArgs e)
    {
        if (DataContext is not MaRuleManagerViewModel vm) return;
        if (sender is not TableView tv || tv.DataControl is not GridControl grid) return;
        if (e.HitInfo is null || e.HitInfo.RowHandle < 0) return;

        if (grid.GetRow(e.HitInfo.RowHandle) is MaRuleRecord rule)
            vm.OpenEditor(rule);
    }
}
